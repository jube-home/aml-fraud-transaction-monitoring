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

using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class GaussianMixtureClustererPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "GaussianMixtureClustererPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var components = ModelJson.Get(payload, "components") as JArray ??
                             throw ModelJson.Inconsistent("the payload has no components");

            if (components.Count == 0)
            {
                throw ModelJson.Inconsistent("the payload has no components");
            }

            var groups = new List<GaussianGroup>();

            for (var k = 0; k < components.Count; k++)
            {
                var mean = ModelJson.Doubles(components[k], "mean") ??
                           throw ModelJson.Inconsistent("a component has no mean");

                var covariance = SnapshotMatrix.Square(ModelJson.Matrix(components[k], "covariance"), mean.Length,
                    "a component covariance");

                var weight = ModelJson.Number(components[k], "mixtureWeight") ?? 1.0 / components.Count;
                var index = ModelJson.Integer(components[k], "index") ?? k;

                groups.Add(new GaussianGroup(
                    "Cluster " + (index + 1) + " (" + ModelJson.Format(Math.Round(weight * 100, 1)) + "%)",
                    weight, mean,
                    GaussianPanels.Deviations(covariance.Select((r, i) => r[i]).ToArray(), "the component variances")));
            }

            var dimension = groups[0].Means.Length;
            var names = options.Variables(ModelJson.Strings(envelope, "variablesInOrder"), dimension);

            var standardise = ModelJson.Get(payload, "standardise") is { Type: JTokenType.Boolean } s &&
                              s.Value<bool>();

            var scale = standardise ? "standardised" : "original";

            return GaussianPanels.Build(names, groups, true, options, "Gaussian mixture clusters",
                groups.Count + " clusters; each variable's mixture density split into its weighted clusters",
                "A Gaussian mixture of " + groups.Count + " clusters over " + dimension + " variables on the " + scale +
                " scale. Each panel is the marginal density of one variable: the mixture total and each cluster's " +
                "normal curve scaled by its mixing weight.");
        }
    }
}