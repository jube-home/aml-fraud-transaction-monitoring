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
using Jube.Engine.AdvancedAnalytics.Classification.Models;
using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public sealed class KNearestNeighboursEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("The number of nearest training rows (k) that a vote is taken over.")]
        public int NeighbourCount { get; set; }

        [Description("The distance measure used to find the nearest training rows. Always Euclidean for this family.")]
        public string DistanceMetric { get; set; } = "Euclidean";

        [Description("The number of standardised, imputed training rows carried in the model snapshot itself, since " +
                     "recalling a k nearest neighbours model means searching the stored training rows directly; there " +
                     "is no separate compact parameter set the way there is for a fitted line or a set of centroids. " +
                     "A larger training split therefore makes a larger snapshot; this is an expected characteristic " +
                     "of this family, not a defect.")]
        public int StoredTrainingRows { get; set; }

        [Description("Whether, and how, the raw vote fraction was Platt-calibrated into a probability on the " +
                     "validation split.")]
        public KNearestNeighboursCalibrationInfo Calibration { get; set; } = null!;

        [Description("Metrics computed on the training split.")]
        public KNearestNeighboursSplitMetrics? TrainMetrics { get; set; }

        [Description("Metrics computed on the validation split.")]
        public KNearestNeighboursSplitMetrics? ValidationMetrics { get; set; }

        [Description("Metrics computed on the held-out test split, or null when there is no test split.")]
        public KNearestNeighboursSplitMetrics? TestMetrics { get; set; }
    }
}