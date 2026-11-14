/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.Charting;
using Jube.Charting.Charts;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Jube.Engine.AdvancedAnalytics;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.ModelDiagrams;
using Jube.Service.ModelDiagrams.Exceptions;
using Jube.Service.Query.AdvancedAnalyticsChart;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Charts
{
    public static class AdvancedAnalyticsChartRenderer
    {
        public const string EmptyMessage = ChartEmptyState.Message;
        public const int MaximumTitleCharacters = 80;

        public static string Empty(string title, string xAxisTitle, string yAxisTitle, string? message = null,
            double width = 640, double height = 420)
        {
            var text = message ?? EmptyMessage;

            return ChartEmptyState.Render(title, text, text, xAxisTitle, yAxisTitle, width, height,
                ChartStyleMode.External, null);
        }

        internal static string ModelDiagram(AdvancedAnalyticsChartSource? source)
        {
            const string title = "Model diagram";

            if (source == null)
            {
                return Empty(title, "Variable", "Coefficient");
            }

            try
            {
                var json = WithTransformedVariableNames(WithEvidenceSideCar(source), source.VariablesJson);

                var options = new ModelDiagramOptions
                {
                    Title = Truncate((source.ModelName ?? "Model") + " version " + source.VersionNumber),
                    Theme = ThemeMode.Auto,
                    Interactive = true
                };

                var chart = ModelDiagramRenderer.Build(json, options);

                return chart is IStyleModeChart styled
                    ? styled.WithStyleMode(ChartStyleMode.External).Render()
                    : chart.Render();
            }
            catch (ModelDiagramException e)
            {
                return Empty(title, "Variable", "Coefficient", e.Message);
            }
        }

        internal static string Calibration(AdvancedAnalyticsChartSource? source)
        {
            const string title = "Calibration";
            var metrics = (source?.Evidence as
                Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AdvancedAnalyticsLogisticEvidence)?.TestMetrics;

            var bins = metrics?.ReliabilityBins
                .Where(b => b.Count > 0 && b.MeanPredicted.HasValue && b.ObservedRate.HasValue
                            && double.IsFinite(b.MeanPredicted.Value) && double.IsFinite(b.ObservedRate.Value))
                .ToList();

            if (metrics == null || bins == null || bins.Count == 0)
            {
                return Empty(title, "Mean predicted probability", "Observed rate", null, 520, 560);
            }

            return Chart.Calibration()
                .Title(title)
                .Subtitle("Held-out test split, one point per non empty probability bin")
                .Curve("Test", bins.Select(b => b.MeanPredicted!.Value), bins.Select(b => b.ObservedRate!.Value),
                    bins.Select(b => (double)b.Count), null,
                    metrics.ExpectedCalibrationError is { } ece && double.IsFinite(ece) ? ece : null)
                .StyleMode(ChartStyleMode.External)
                .Render();
        }

        internal static string Roc(AdvancedAnalyticsChartSource? source)
        {
            const string title = "ROC";

            if (source == null || source.Roc.Count < 2)
            {
                return Empty(title, "False Positive Rate", "True Positive Rate");
            }

            var ordered = source.Roc.Where(p => double.IsFinite(p.FalsePositiveRate)
                                                && double.IsFinite(p.TruePositiveRate))
                .OrderBy(p => p.FalsePositiveRate).ThenBy(p => p.TruePositiveRate).ToList();

            if (ordered.Count < 2)
            {
                return Empty(title, "False Positive Rate", "True Positive Rate");
            }

            var fpr = ordered.Select(p => p.FalsePositiveRate).ToList();
            var tpr = ordered.Select(p => p.TruePositiveRate).ToList();

            var thresholds = ordered.Select((p, i) => p.Threshold is { } t && double.IsFinite(t)
                ? t
                : i == 0
                    ? double.PositiveInfinity
                    : double.NegativeInfinity).ToList();

            return Chart.Roc()
                .Title(title)
                .Subtitle("Held-out test split")
                .XAxisTitle("False Positive Rate")
                .YAxisTitle("True Positive Rate")
                .Legend(LegendPosition.Bottom)
                .Curve("Test", fpr, tpr, thresholds, CurveMath.Auc(fpr, tpr))
                .StyleMode(ChartStyleMode.External)
                .Render();
        }

        private static string WithEvidenceSideCar(AdvancedAnalyticsChartSource source)
        {
            if (string.IsNullOrWhiteSpace(source.MetricsJson))
            {
                return source.ModelJson;
            }

            try
            {
                if (JToken.Parse(source.ModelJson) is not JObject envelope || envelope["evidence"] != null
                                                                           || JToken.Parse(source.MetricsJson) is not
                                                                               JObject metrics
                                                                           || metrics["coefficients"] is not JArray
                                                                               coefficients)
                {
                    return source.ModelJson;
                }

                envelope["evidence"] = new JObject { ["coefficients"] = coefficients };

                return envelope.ToString(Formatting.None);
            }
            catch (JsonException)
            {
                return source.ModelJson;
            }
        }

        private static string WithTransformedVariableNames(string json, string? variablesJson)
        {
            if (string.IsNullOrWhiteSpace(variablesJson))
            {
                return json;
            }

            try
            {
                var rows = AdvancedAnalyticsEvidenceJson.Deserialise<List<AdvancedAnalyticsVariableRow>>(
                    variablesJson);

                if (rows == null || rows.Count == 0 || JToken.Parse(json) is not JObject envelope)
                {
                    return json;
                }

                var transformations = rows.Where(r => !string.IsNullOrEmpty(r.Name))
                    .GroupBy(r => r.Name, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First().TransformationId, StringComparer.Ordinal);

                var changed = false;
                changed |= DecorateNames(envelope["variablesInOrder"] as JArray, transformations);
                changed |= DecorateCoefficientNames(envelope["evidence"]?["coefficients"] as JArray, transformations);

                return changed ? envelope.ToString(Formatting.None) : json;
            }
            catch (JsonException)
            {
                return json;
            }
        }

        private static bool DecorateNames(JArray? names,
            IReadOnlyDictionary<string, VariableTransformationKind> transformations)
        {
            if (names == null)
            {
                return false;
            }

            var changed = false;

            foreach (var token in names.ToList())
            {
                if (token.Type != JTokenType.String)
                {
                    continue;
                }

                var name = token.Value<string>();

                if (name != null && transformations.TryGetValue(name, out var kind) &&
                    kind != VariableTransformationKind.None)
                {
                    token.Replace(VariableTransformationDisplayName.DisplayName(name, kind));
                    changed = true;
                }
            }

            return changed;
        }

        private static bool DecorateCoefficientNames(JArray? coefficients,
            IReadOnlyDictionary<string, VariableTransformationKind> transformations)
        {
            if (coefficients == null)
            {
                return false;
            }

            var changed = false;

            foreach (var coefficient in coefficients)
            {
                if (coefficient["name"] is not { Type: JTokenType.String } nameToken)
                {
                    continue;
                }

                var name = nameToken.Value<string>();

                if (name != null && transformations.TryGetValue(name, out var kind) &&
                    kind != VariableTransformationKind.None)
                {
                    coefficient["name"] = VariableTransformationDisplayName.DisplayName(name, kind);
                    changed = true;
                }
            }

            return changed;
        }

        private static string Truncate(string value)
        {
            return value.Length <= MaximumTitleCharacters ? value : value[..MaximumTitleCharacters];
        }
    }
}