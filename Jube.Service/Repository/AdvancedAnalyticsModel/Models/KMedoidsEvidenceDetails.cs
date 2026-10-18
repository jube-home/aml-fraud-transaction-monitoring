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
    public sealed class KMedoidsEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of clusters the model was fitted with.")]
        public int Clusters { get; set; }

        [Description("Whether the fit converged before the iteration cap.")]
        public bool Converged { get; set; }

        [Description("Number of iterations the fit took.")]
        public int Iterations { get; set; }

        [Description("Total distance (not squared) of every training row to its assigned medoid, the quantity the " +
                     "partitioning-around-medoids search minimises. Lower is tighter.")]
        public double TrainTotalDistance { get; set; }

        [Description("Total distance of every validation row to its nearest medoid, or null when it has no rows.")]
        public double? ValidationTotalDistance { get; set; }

        [Description("Total distance of every held-out test row to its nearest medoid, or null when it has no rows.")]
        public double? TestTotalDistance { get; set; }

        [Description("Silhouette score on the training split, between -1 and 1. Null when there is one cluster or " +
                     "too many rows to compute cheaply.")]
        public double? TrainSilhouette { get; set; }

        [Description("Silhouette score on the held-out test split. Null when there is one cluster, the test split is " +
                     "empty, or there are too many rows to compute cheaply.")]
        public double? TestSilhouette { get; set; }

        [Description("Per-cluster size, proportion, medoid and total distance, computed on the training split.")]
        public KMedoidsClusterSummary[] ClusterSummaries { get; set; } = [];
    }
}