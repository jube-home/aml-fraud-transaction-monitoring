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

using System.Linq;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Models
{
    public sealed class AdvancedAnalyticsBalancedKMeansClustererPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public bool Standardise { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Means { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] StandardDeviations { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[][] Centroids { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public BalancedKMeansSnapshotCluster[] Clusters { get; set; } = [];

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);
            RequireLength(nameof(Means), Means, inputCount);
            RequireLength(nameof(StandardDeviations), StandardDeviations, inputCount);

            if (StandardDeviations.Any(v => v < 0))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot standard deviations must not be negative.");
            }

            foreach (var (name, values) in new[]
                     {
                         (nameof(FillValues), FillValues), (nameof(Means), Means),
                         (nameof(StandardDeviations), StandardDeviations)
                     })
            {
                if (values.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException($"Snapshot array '{name}' must be finite.");
                }
            }

            if (Centroids == null || Centroids.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot has no centroids.");
            }

            foreach (var centroid in Centroids)
            {
                if (centroid == null || centroid.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Every centroid must have {inputCount} elements.");
                }

                if (centroid.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot centroids must be finite.");
                }
            }

            if (Clusters == null || Clusters.Length != Centroids.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'Clusters' must list exactly one entry per centroid.");
            }

            for (var i = 0; i < Clusters.Length; i++)
            {
                var cluster = Clusters[i];

                if (cluster == null)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot 'Clusters' contains a missing entry.");
                }

                if (cluster.Index < 0 || cluster.Index >= Clusters.Length)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot cluster index is out of range.");
                }

                if (double.IsNaN(cluster.Proportion) || cluster.Proportion < 0 || cluster.Proportion > 1)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Snapshot cluster proportion must be between 0 and 1.");
                }

                if (cluster.Centroid == null || cluster.Centroid.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Every cluster centroid must have {inputCount} elements.");
                }

                if (cluster.Centroid.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot cluster centroids must be finite.");
                }
            }

            if (Clusters.Select(c => c.Index).Distinct().Count() != Clusters.Length)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot 'Clusters' indices must be distinct.");
            }
        }
    }
}