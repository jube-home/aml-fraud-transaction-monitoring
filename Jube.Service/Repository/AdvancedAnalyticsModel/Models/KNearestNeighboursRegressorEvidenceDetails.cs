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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public sealed class KNearestNeighboursRegressorEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("The number of nearest training rows (k) whose targets are averaged to form the prediction.")]
        public int NeighbourCount { get; set; }

        [Description("The distance measure used to find the nearest training rows. Always Euclidean for this family.")]
        public string DistanceMetric { get; set; } = "Euclidean";

        [Description("The number of standardised, imputed training rows carried in the model snapshot itself, since " +
                     "recalling a k nearest neighbours model means searching the stored training rows directly; there " +
                     "is no separate compact parameter set the way there is for a fitted line. A larger training split " +
                     "therefore makes a larger snapshot; this is an expected characteristic of this family, not a " +
                     "defect.")]
        public int StoredTrainingRows { get; set; }

        [Description("The plain mean of the target across every stored training row, used as the reference value " +
                     "an explanation measures a prediction's pull away from, in the absence of any other information.")]
        public double TrainingMeanTarget { get; set; }

        [Description("Metrics computed on the training split.")]
        public KNearestNeighboursRegressorSplitMetrics? TrainMetrics { get; set; }

        [Description("Metrics computed on the validation split.")]
        public KNearestNeighboursRegressorSplitMetrics? ValidationMetrics { get; set; }

        [Description("Metrics computed on the held-out test split, or null when there is no test split.")]
        public KNearestNeighboursRegressorSplitMetrics? TestMetrics { get; set; }
    }
}