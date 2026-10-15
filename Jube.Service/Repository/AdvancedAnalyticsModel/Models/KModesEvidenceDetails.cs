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
    public sealed class KModesEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of clusters the model was fitted with.")]
        public int Clusters { get; set; }

        [Description("Whether the fit converged before the iteration cap, meaning no row's assigned cluster changed " +
                     "on the final pass.")]
        public bool Converged { get; set; }

        [Description("Number of iterations the fit took.")]
        public int Iterations { get; set; }

        [Description("Total category mismatch count on the training split: the sum, over every row, of how many " +
                     "variables disagree with that row's assigned cluster mode. Lower is tighter.")]
        public double TrainInertia { get; set; }

        [Description("Total category mismatch count on the validation split, or null when it has no rows.")]
        public double? ValidationInertia { get; set; }

        [Description("Total category mismatch count on the held-out test split, or null when it has no rows.")]
        public double? TestInertia { get; set; }

        [Description("Silhouette score on the training split, computed with the category mismatch count standing " +
                     "in for a Euclidean distance, between -1 and 1. Null when there is one cluster or too many rows " +
                     "to compute cheaply.")]
        public double? TrainSilhouette { get; set; }

        [Description("Silhouette score on the held-out test split, computed the same way. Null when there is one " +
                     "cluster, the test split is empty, or there are too many rows to compute cheaply.")]
        public double? TestSilhouette { get; set; }

        [Description("Per-cluster size, proportion, mode and total mismatch count, computed on the training split.")]
        public KModesClusterSummary[] ClusterSummaries { get; set; } = [];
    }
}