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

using System.ComponentModel;
using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public sealed class BalancedKMeansEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of clusters the model was fitted with.")]
        public int Clusters { get; set; }

        [Description("Largest training cluster size minus the smallest, from the balanced assignment computed " +
                     "during training. Zero means every cluster received exactly the same number of rows; this is " +
                     "usually 0 or 1 for balanced k-means, since it distributes rows as evenly as the row count and " +
                     "cluster count allow.")]
        public int TrainSizeImbalance { get; set; }

        [Description("Total within-cluster sum of squares (inertia) on the training split, using the balanced " +
                     "assignment. Lower is tighter.")]
        public double TrainInertia { get; set; }

        [Description("Total within-cluster sum of squares on the validation split, or null when it has no rows. " +
                     "Computed by assigning each row to its nearest centroid, since the balanced assignment used for " +
                     "training does not extend to rows outside the training split.")]
        public double? ValidationInertia { get; set; }

        [Description("Total within-cluster sum of squares on the held-out test split, or null when it has no rows. " +
                     "Computed by assigning each row to its nearest centroid, for the same reason as the validation " +
                     "figure.")]
        public double? TestInertia { get; set; }

        [Description("Silhouette score on the training split, between -1 and 1, computed from the balanced " +
                     "assignment. Null when there is one cluster or too many rows to compute cheaply.")]
        public double? TrainSilhouette { get; set; }

        [Description("Silhouette score on the held-out test split, computed from a nearest-centroid assignment. Null " +
                     "when there is one cluster, the test split is empty, or there are too many rows to compute " +
                     "cheaply.")]
        public double? TestSilhouette { get; set; }

        [Description("Per-cluster size, proportion, centroid and within-cluster sum of squares from the balanced " +
                     "assignment computed on the training split.")]
        public BalancedKMeansClusterSummary[] ClusterSummaries { get; set; } = [];
    }
}