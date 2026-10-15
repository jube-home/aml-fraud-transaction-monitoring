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
    public sealed class KMeansAdapter : IModelDiagramAdapter
    {
        public string Name => "KMeans";

        public double? Detect(JToken model)
        {
            var clusters = Clusters(model);

            if (clusters is not { Count: > 0 })
            {
                return null;
            }

            return clusters.All(c => c is JObject && ModelJson.Doubles(c, "centroid") is { Length: > 0 }) ? 0.95 : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            return BuildFromClusters(Clusters(model)!, null, options);
        }

        private static JArray? Clusters(JToken model)
        {
            return model as JArray ?? ModelJson.Get(model, "clusters") as JArray;
        }

        internal static ISvgChart BuildFromClusters(JArray clusters, string[]? storedNames, ModelDiagramOptions options)
        {
            var dimension = ModelJson.Doubles(clusters.FirstOrDefault(), "centroid")?.Length ?? 0;

            return ClusterProfiles.Build(clusters, "centroid", storedNames, options, "K-means clusters",
                clusters.Count + " clusters, centroids standardised across clusters",
                "Centroids of " + clusters.Count + " k-means clusters over " + dimension + " features.",
                "Centroid z-score across clusters", Standardise);
        }

        internal static double[][] Standardise(double[][] centroids, int dimension)
        {
            var result = centroids.Select(_ => new double[dimension]).ToArray();

            for (var f = 0; f < dimension; f++)
            {
                var mean = centroids.Average(c => c[f]);
                var variance = centroids.Sum(c => (c[f] - mean) * (c[f] - mean)) / centroids.Length;
                var deviation = Math.Sqrt(variance);

                for (var k = 0; k < centroids.Length; k++)
                {
                    result[k][f] = deviation > 1e-12 ? (centroids[k][f] - mean) / deviation : 0;
                }
            }

            return result;
        }
    }
}