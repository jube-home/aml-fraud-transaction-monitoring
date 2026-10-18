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

using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class PrincipalComponentPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "PrincipalComponentPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var components = (JArray)ModelJson.Get(payload, "components")!;
            var vectors = components.Select(c => ModelJson.Doubles(c, "eigenvector")!).ToArray();
            var eigenvalues = components.Select(c => ModelJson.Number(c, "eigenvalue")!.Value).ToArray();
            var dimension = vectors[0].Length;

            if (vectors.Any(v => v.Length != dimension || v.Any(x => !double.IsFinite(x))) ||
                eigenvalues.Any(e => !double.IsFinite(e) || e < 0))
            {
                throw ModelJson.Inconsistent(
                    "the eigenvectors must be finite, of one length, with non-negative eigenvalues");
            }

            options.RequireWithin((long)vectors.Length * dimension, "loadings");
            var total = eigenvalues.Sum();
            var items = new List<ComponentItem>();

            for (var c = 0; c < vectors.Length; c++)
            {
                var proportion = ModelJson.Number(components[c], "proportion") ??
                                 (total > 0 ? eigenvalues[c] / total : 0);

                items.Add(new ComponentItem("PC" + (c + 1), Math.Clamp(proportion, 0, 1), vectors[c]));
            }

            var names = options.Variables(null, dimension);
            var covered = items.Sum(i => i.ExplainedVariance);

            return new ComponentsDiagram(new ComponentsDiagramSpec
            {
                Options = options.ToChartOptions("Principal components",
                    items.Count + " components explaining " + ModelJson.Format(Math.Round(covered * 100, 1)) +
                    "% of the variance",
                    "Scree plot and loadings of " + items.Count + " principal components over " + dimension +
                    " features."),
                Features = names,
                Components = items,
                ValueName = "Loading"
            });
        }
    }
}