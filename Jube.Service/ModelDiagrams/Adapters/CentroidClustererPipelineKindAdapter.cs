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
    public sealed class CentroidClustererPipelineKindAdapter : ISnapshotKindAdapter
    {
        private readonly string method;
        private readonly string title;
        private readonly string vectorKey;
        private readonly string vectorName;

        private CentroidClustererPipelineKindAdapter(string kind, string vectorKey, string vectorName, string title,
            string method)
        {
            Kind = kind;
            this.vectorKey = vectorKey;
            this.vectorName = vectorName;
            this.title = title;
            this.method = method;
        }

        public static IReadOnlyList<ISnapshotKindAdapter> All { get; } =
        [
            new CentroidClustererPipelineKindAdapter("BalancedKMeansClustererPipeline", "centroid", "centroid",
                "Balanced k-means clusters", "balanced k-means, which keeps the clusters close to equal in size"),
            new CentroidClustererPipelineKindAdapter("BinarySplitClustererPipeline", "centroid", "centroid",
                "Bisecting k-means clusters", "bisecting k-means, which splits the widest cluster in two until done"),
            new CentroidClustererPipelineKindAdapter("MiniBatchKMeansClustererPipeline", "centroid", "centroid",
                "Mini-batch k-means clusters", "mini-batch k-means"),
            new CentroidClustererPipelineKindAdapter("KMedoidsClustererPipeline", "medoid", "medoid",
                "K-medoids clusters", "k-medoids, where each cluster is represented by one real training row"),
            new CentroidClustererPipelineKindAdapter("MeanShiftClustererPipeline", "centroid", "mode",
                "Mean-shift clusters", "mean shift, where each cluster is a peak of the estimated density"),
            new CentroidClustererPipelineKindAdapter("KModesClustererPipeline", "mode", "mode", "K-modes clusters",
                "k-modes over categorical codes")
        ];

        public string Kind { get; }

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var clusters = ModelJson.Get(payload, "clusters") as JArray ??
                           throw ModelJson.Inconsistent("the payload has no clusters");

            var stored = ModelJson.Strings(envelope, "variablesInOrder");
            var dimension = ModelJson.Doubles(clusters.FirstOrDefault(), vectorKey)?.Length ?? 0;

            var standardise = ModelJson.Get(payload, "standardise") is { Type: JTokenType.Boolean } s &&
                              s.Value<bool>();

            var count = clusters.Count + (clusters.Count == 1 ? " cluster" : " clusters");
            var scale = standardise ? "standardised" : "original";

            var summary = "Cluster " + vectorName + "s of " + count + " from " + method + ", over " + dimension +
                          " variables. The size bar is the share of training rows in each cluster.";

            if (clusters.Count == 1)
            {
                return ClusterProfiles.Build(clusters, vectorKey, stored, options, title,
                    "1 cluster; its " + vectorName + " on the " + scale + " scale",
                    summary + " With a single cluster there is nothing to contrast, so the " + vectorName +
                    " itself is shown.", char.ToUpperInvariant(vectorName[0]) + vectorName[1..] + " value",
                    (vectors, _) => vectors);
            }

            if (vectorKey == "mode")
            {
                return ClusterProfiles.Build(clusters, vectorKey, stored, options, title,
                    count + "; how far each cluster's modal category stands apart",
                    summary + " Each cell is the share of the other clusters whose modal category differs on that " +
                    "variable, the same simple matching the model uses to assign rows: 1 means no other cluster " +
                    "shares the category, 0 means every cluster does.",
                    "Share of other clusters with a different mode", ClusterProfiles.Mismatch);
            }

            return ClusterProfiles.Build(clusters, vectorKey, stored, options, title,
                count + ", " + vectorName + "s standardised across clusters",
                summary + " Each cell is the " + vectorName + " value on the " + scale +
                " scale, restated as a z-score across the clusters so that variables on different scales compare.",
                char.ToUpperInvariant(vectorName[0]) + vectorName[1..] + " z-score across clusters",
                KMeansAdapter.Standardise);
        }
    }
}