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
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class MahalanobisAnomalyPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "MahalanobisAnomalyPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var stored = ModelJson.Strings(envelope, "variablesInOrder");
            var inputCount = stored?.Length ?? ModelJson.Integer(envelope, "inputCount") ?? 0;

            if (inputCount < 1)
            {
                throw ModelJson.Inconsistent("the snapshot lists no variables");
            }

            var names = options.Variables(stored, inputCount);

            var centre = ModelJson.Doubles(payload, "centre") ??
                         throw ModelJson.Inconsistent("the payload has no centre");

            var active = SnapshotMatrix.ActiveInputs(ModelJson.Integers(payload, "activeInputs"), inputCount,
                centre.Length);

            if (active.Length == 0)
            {
                throw ModelJson.Inconsistent("the model has no active variables");
            }

            options.RequireWithin((long)active.Length * active.Length, "covariance cells");

            var precision = SnapshotMatrix.Square(ModelJson.Matrix(payload, "inverseCovariance"), active.Length,
                "the inverse covariance");

            var standardise = ModelJson.Get(payload, "standardise") is { Type: JTokenType.Boolean } s &&
                              s.Value<bool>();

            var labels = active.Select(i => names[i]).ToArray();
            var omitted = inputCount - active.Length;
            var scale = standardise ? "standardised" : "original";

            var note = omitted > 0
                ? " " + omitted + " constant variable" + (omitted == 1 ? " was" : "s were") +
                  " left out of the distance."
                : "";

            if (active.Length == 1)
            {
                if (!(precision[0][0] > 0))
                {
                    throw ModelJson.Inconsistent("the inverse variance must be positive");
                }

                var deviation = 1 / Math.Sqrt(precision[0][0]);

                return new DistributionPanels(new DistributionPanelsSpec
                {
                    Options = options.ToChartOptions("Mahalanobis anomaly model",
                        "Normal profile of the one active variable on the " + scale + " scale",
                        "The single active variable " + labels[0] + " is modelled as normal with centre " +
                        ModelJson.Format(centre[0]) + " and deviation " + ModelJson.Format(deviation) +
                        "; the anomaly score grows with the distance from the centre in deviations." + note),
                    Panels =
                    [
                        new DistributionPanel(labels[0],
                            [DistributionSeries.Normal("Normal profile", centre[0], deviation)])
                    ]
                });
            }

            var covariance = SnapshotMatrix.Invert(precision);
            var partial = covariance is null;

            var matrix = partial
                ? SnapshotMatrix.PartialCorrelation(precision)
                : SnapshotMatrix.Correlation(covariance!);

            var measure = partial ? "Partial correlation" : "Correlation";

            var spec = Chart.Heatmap()
                .Rows(labels)
                .Columns(labels)
                .Values(matrix)
                .Diverging()
                .Range(-1, 1)
                .ShowValues(active.Length <= 12)
                .Decimals(2)
                .LegendTitle(measure)
                .Build()
                .Spec;

            return new HeatmapChart(spec with
            {
                Options = options.ToChartOptions("Mahalanobis anomaly model: fitted covariance",
                    measure + " structure of the normal profile over " + active.Length + " active variables",
                    "The anomaly score is the Mahalanobis distance of a row from the centre of a multivariate normal " +
                    "fitted on the " + scale + " scale. Each cell is the " + measure.ToLowerInvariant() +
                    " of two variables in that fitted covariance: a row whose values break a strong correlation is " +
                    "far from the centre even when each value alone is ordinary." + note)
            });
        }
    }
}