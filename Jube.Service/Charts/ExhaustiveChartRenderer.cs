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

using System.Globalization;
using Jube.Charting;
using Jube.Charting.Charts;
using Jube.Charting.Core.Models;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Dto.Query.ExhaustiveSearchInstanceVariable;

namespace Jube.Service.Charts
{
    public static class ExhaustiveChartRenderer
    {
        public const string PositiveSeriesName = "Positive class";
        public const string NegativeSeriesName = "Negative class";
        public const string EmptyMessage = ChartEmptyState.Message;
        public const int MaximumScatterPointsPerSeries = 5000;
        public const int MaximumSensitivityBars = 20;
        private const double PositiveThreshold = 0.5;

        private static readonly string[] prescriptionSummaryCategories =
            ["Mean", "Standard Deviation", "Minimum", "Maximum"];

        public static string LearningCurve(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto> points,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            const string title = "Learning Curve";

            if (points.Count == 0)
            {
                return Empty(title, "Promotion", "Score", styleMode: styleMode, id: id);
            }

            return Chart.Line()
                .Title(title)
                .Subtitle("Score of each promoted trial instance, in order of promotion")
                .XAxisTitle("Promotion")
                .YAxisTitle("Score")
                .Size(1000, 500)
                .Legend(LegendPosition.None)
                .Smooth()
                .DataLabels()
                .Series("Score", points.Select((_, i) => (double)(i + 1)), points.Select(p => p.Score))
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Roc(IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstanceRocDto> points,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            const string title = "ROC";

            if (points.Count == 0)
            {
                return Empty(title, "False Positive Rate", "True Positive Rate", styleMode: styleMode, id: id);
            }

            var ordered = points.OrderBy(p => p.Fpr).ThenBy(p => p.Tpr).ToList();
            var fpr = new List<double>();
            var tpr = new List<double>();
            var thresholds = new List<double>();

            if (ordered[0].Fpr > 0 || ordered[0].Tpr > 0)
            {
                fpr.Add(0);
                tpr.Add(0);
                thresholds.Add(double.PositiveInfinity);
            }

            foreach (var p in ordered)
            {
                fpr.Add(p.Fpr);
                tpr.Add(p.Tpr);
                thresholds.Add(p.Score);
            }

            if (ordered[^1].Fpr < 1 || ordered[^1].Tpr < 1)
            {
                fpr.Add(1);
                tpr.Add(1);
                thresholds.Add(double.NegativeInfinity);
            }

            return Chart.Roc()
                .Title(title)
                .XAxisTitle("False Positive Rate")
                .YAxisTitle("True Positive Rate")
                .Legend(LegendPosition.Bottom)
                .Curve("Model", fpr, tpr, thresholds, CurveMath.Auc(fpr, tpr))
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string PredictedActual(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> points,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null, bool calibrated = false)
        {
            const string title = "Predicted vs. Actual";

            if (points.Count == 0)
            {
                return Empty(title, "Predicted", "Actual", styleMode: styleMode, id: id);
            }

            var positive = points.Where(p => p.Actual >= PositiveThreshold).ToList();
            var negative = points.Where(p => p.Actual < PositiveThreshold).ToList();

            var builder = Chart.Scatter()
                .Title(title)
                .Subtitle(calibrated
                    ? "Actual 0.5+ is positive, below is negative. Predicted is calibrated at the sample prior."
                    : "Actual 0.5+ is positive, below is negative")
                .XAxisTitle("Predicted")
                .YAxisTitle("Actual")
                .Legend(LegendPosition.Bottom);

            if (positive.Count > 0)
            {
                var sample = Thin(positive);
                builder.Series(PositiveSeriesName, sample.Select(p => p.Predicted), sample.Select(p => p.Actual));
            }

            if (negative.Count > 0)
            {
                var sample = Thin(negative);
                builder.Series(NegativeSeriesName, sample.Select(p => p.Predicted), sample.Select(p => p.Actual));
            }

            return builder.StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string ErrorHistogram(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto> bins,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            const string title = "Errors";

            if (bins.Count == 0)
            {
                return Empty(title, "Error (actual minus predicted)", "Frequency", styleMode: styleMode, id: id);
            }

            return Chart.Column()
                .Title(title)
                .Subtitle("Prediction error, actual minus predicted")
                .XAxisTitle("Error (actual minus predicted)")
                .YAxisTitle("Frequency")
                .Size(640, 420)
                .Legend(LegendPosition.None)
                .Categories(bins.Select(b => Label(b.Bin)))
                .Series("Frequency", bins.Select(b => (double)b.Frequency))
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string VariableHistogram(ExhaustiveSearchInstanceVariableDto? variable,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            if (variable == null || variable.HistogramValues.Count == 0)
            {
                return Empty(variable?.Name ?? "Histogram", "Bin", "Frequency", 300, 150, styleMode, id);
            }

            return Chart.Column()
                .Title(string.IsNullOrWhiteSpace(variable.Name) ? "Histogram" : variable.Name)
                .XAxisTitle("Bin")
                .YAxisTitle("Frequency")
                .Size(300, 150)
                .Legend(LegendPosition.None)
                .Categories(variable.HistogramValues.Select(h => Label(h.Bin)))
                .Series("Frequency", variable.HistogramValues.Select(h => (double)h.Frequency))
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string PrescriptionHistogramByClass(
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto? positive,
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto? negative,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            var title = positive?.Name ?? negative?.Name;
            title = string.IsNullOrWhiteSpace(title) ? "Histogram" : title;

            var positiveEdges = PrescriptionHistogramEdges(positive);
            var negativeEdges = PrescriptionHistogramEdges(negative);

            if (positiveEdges == null && negativeEdges == null)
            {
                return Empty(title, "Value", "Frequency", 300, 150, styleMode, id);
            }

            var builder = Chart.Histogram()
                .Title(title)
                .XAxisTitle("Value")
                .YAxisTitle("Frequency")
                .Size(300, 150)
                .Legend(LegendPosition.Bottom);

            if (negativeEdges != null)
            {
                builder.Bins(NegativeSeriesName, negativeEdges,
                    negative!.HistogramValues.Select(h => (double)h.Frequency));
            }

            if (positiveEdges != null)
            {
                builder.Bins(PositiveSeriesName, positiveEdges,
                    positive!.HistogramValues.Select(h => (double)h.Frequency));
            }

            return builder.StyleMode(styleMode).Id(id).Render();
        }

        public static string PrescriptionSummaryByClass(
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto? positive,
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto? negative,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            var title = positive?.Name ?? negative?.Name;
            title = string.IsNullOrWhiteSpace(title) ? "Summary" : title;

            var origin = positive?.VariableMean ?? negative?.VariableMean ?? 0;
            var scale = positive?.VariableStandardDeviation ?? negative?.VariableStandardDeviation ?? 0;

            if ((positive == null && negative == null) || !(scale > 0))
            {
                return Empty(title, "Statistic", "Standard deviations", 300, 150, styleMode, id);
            }

            var builder = Chart.Column()
                .Title(title)
                .XAxisTitle("Statistic")
                .YAxisTitle("Standard deviations")
                .Size(300, 150)
                .Legend(LegendPosition.Bottom)
                .Categories(prescriptionSummaryCategories);

            if (negative != null)
            {
                builder.Series(NegativeSeriesName, PrescriptionSummaryValues(negative, origin, scale));
            }

            if (positive != null)
            {
                builder.Series(PositiveSeriesName, PrescriptionSummaryValues(positive, origin, scale));
            }

            return builder.StyleMode(styleMode).Id(id).Render();
        }

        private static double[] PrescriptionSummaryValues(
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto variable, double origin,
            double scale)
        {
            return
            [
                (variable.PrescriptionMean - origin) / scale,
                variable.PrescriptionStandardDeviation / scale,
                (variable.PrescriptionMinimum - origin) / scale,
                (variable.PrescriptionMaximum - origin) / scale
            ];
        }

        private static double[]? PrescriptionHistogramEdges(
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto? variable)
        {
            if (variable == null || variable.HistogramValues.Count == 0)
            {
                return null;
            }

            var edges = new double[variable.HistogramValues.Count + 1];

            for (var i = 0; i < variable.HistogramValues.Count; i++)
            {
                edges[i] = variable.HistogramValues[i].Bin;
            }

            edges[^1] = variable.HistogramValues[^1].BinEnd;

            return edges;
        }

        public static string Confusion(ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto? matrix,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            if (matrix == null ||
                matrix.TrueNegative + matrix.FalsePositive + matrix.FalseNegative + matrix.TruePositive == 0)
            {
                return Empty("Confusion Matrix", "Predicted", "Actual", styleMode: styleMode, id: id);
            }

            return Chart.ConfusionMatrix()
                .Title("Confusion Matrix")
                .Binary(matrix.TrueNegative, matrix.FalsePositive, matrix.FalseNegative, matrix.TruePositive)
                .XAxisTitle("Predicted")
                .YAxisTitle("Actual")
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Sensitivity(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto> variables,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            const string title = "Variable Sensitivity";

            if (variables.Count == 0)
            {
                return Empty(title, "Sensitivity", "Variable", styleMode: styleMode, id: id);
            }

            var top = variables.OrderByDescending(v => v.Sensitivity).Take(MaximumSensitivityBars).ToList();

            return Chart.Bar()
                .Title(title)
                .Subtitle("Most sensitive variables first")
                .XAxisTitle("Variable")
                .YAxisTitle("Sensitivity")
                .Legend(LegendPosition.None)
                .Categories(top.Select(v => v.Name ?? string.Empty))
                .Series("Sensitivity", top.Select(v => v.Sensitivity))
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Empty(string title, string xAxisTitle, string yAxisTitle, double width = 640,
            double height = 420, ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            return ChartEmptyState.Render(title, EmptyMessage, EmptyMessage, xAxisTitle, yAxisTitle, width, height,
                styleMode, id);
        }

        private static string Label(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static List<T> Thin<T>(List<T> source)
        {
            if (source.Count <= MaximumScatterPointsPerSeries)
            {
                return source;
            }

            var stride = (source.Count + MaximumScatterPointsPerSeries - 1) / MaximumScatterPointsPerSeries;

            return source.Where((_, i) => i % stride == 0).ToList();
        }
    }
}