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
    public sealed class GaussianMixtureDensityPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "GaussianMixtureDensityPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var stored = ModelJson.Strings(envelope, "variablesInOrder");
            var inputCount = stored?.Length ?? ModelJson.Integer(envelope, "inputCount") ?? 0;

            if (inputCount < 1)
            {
                throw ModelJson.Inconsistent("the snapshot lists no variables");
            }

            var components = ModelJson.Get(payload, "components") as JArray ??
                             throw ModelJson.Inconsistent("the payload has no components");

            if (components.Count == 0)
            {
                throw ModelJson.Inconsistent("the payload has no components");
            }

            var first = ModelJson.Doubles(components[0], "mean") ??
                        throw ModelJson.Inconsistent("a component has no mean");

            var active = SnapshotMatrix.ActiveInputs(ModelJson.Integers(payload, "activeInputs"), inputCount,
                first.Length);

            options.RequireWithin((long)components.Count * active.Length * active.Length, "covariance cells");
            var names = options.Variables(stored, inputCount);
            var groups = new List<GaussianGroup>();

            for (var k = 0; k < components.Count; k++)
            {
                var mean = ModelJson.Doubles(components[k], "mean") ??
                           throw ModelJson.Inconsistent("a component has no mean");

                var precision = SnapshotMatrix.Square(ModelJson.Matrix(components[k], "inverseCovariance"),
                    active.Length,
                    "a component inverse covariance");

                var covariance = SnapshotMatrix.Invert(precision) ??
                                 throw ModelJson.Inconsistent("a component inverse covariance is singular");

                var weight = ModelJson.Number(components[k], "weight") ?? 1.0 / components.Count;

                groups.Add(new GaussianGroup(
                    "Component " + (k + 1) + " (" + ModelJson.Format(Math.Round(weight * 100, 1)) + "%)", weight, mean,
                    GaussianPanels.Deviations(covariance.Select((r, i) => r[i]).ToArray(), "the component variances")));
            }

            var standardise = ModelJson.Get(payload, "standardise") is { Type: JTokenType.Boolean } s &&
                              s.Value<bool>();

            var omitted = inputCount - active.Length;

            var note = omitted > 0
                ? " " + omitted + " constant variable" + (omitted == 1 ? " is" : "s are") + " not part of the density."
                : "";

            return GaussianPanels.Build(active.Select(i => names[i]).ToArray(), groups, true, options,
                "Gaussian mixture density",
                groups.Count + " components; each variable's density split into its weighted components",
                "A Gaussian mixture density of " + groups.Count + " components over " + active.Length +
                " variables on the " + (standardise ? "standardised" : "original") + " scale; rows in its thin tails " +
                "score as unusual. Each panel is the marginal density of one variable: the mixture total and each " +
                "component's normal curve scaled by its weight." + note);
        }
    }
}