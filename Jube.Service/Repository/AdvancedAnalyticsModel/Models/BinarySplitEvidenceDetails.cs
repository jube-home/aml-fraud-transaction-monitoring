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

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public sealed class BinarySplitEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of clusters the model was fitted with.")]
        public int Clusters { get; set; }

        [Description("Number of times the largest (or worst) existing cluster was bisected with a 2-means split to " +
                     "reach the requested cluster count. Always one fewer than Clusters.")]
        public int Splits { get; set; }

        [Description("Total within-cluster sum of squares (inertia) on the training split. Lower is tighter.")]
        public double TrainInertia { get; set; }

        [Description("Total within-cluster sum of squares on the validation split, or null when it has no rows.")]
        public double? ValidationInertia { get; set; }

        [Description("Total within-cluster sum of squares on the held-out test split, or null when it has no rows.")]
        public double? TestInertia { get; set; }

        [Description("Silhouette score on the training split, between -1 and 1. Null when there is one cluster or " +
                     "too many rows to compute cheaply.")]
        public double? TrainSilhouette { get; set; }

        [Description("Silhouette score on the held-out test split. Null when there is one cluster, the test split is " +
                     "empty, or there are too many rows to compute cheaply.")]
        public double? TestSilhouette { get; set; }

        [Description("Per-cluster size, proportion, centroid and within-cluster sum of squares, computed on the " +
                     "training split.")]
        public BinarySplitClusterSummary[] ClusterSummaries { get; set; } = [];
    }
}