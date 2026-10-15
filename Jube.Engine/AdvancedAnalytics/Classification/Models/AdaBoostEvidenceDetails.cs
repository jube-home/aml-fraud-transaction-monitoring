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

using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Classification.Models
{
    public sealed class AdaBoostEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        public int Rounds { get; set; }
        public int RoundsFitted { get; set; }
        public int MaxDepth { get; set; }
        public int MinimumLeafSize { get; set; }
        public double[] RoundErrors { get; set; } = [];
        public double[] RoundAlphas { get; set; } = [];
        public AdaBoostSplitMetrics TrainMetrics { get; set; }
        public AdaBoostSplitMetrics ValidationMetrics { get; set; }
        public AdaBoostSplitMetrics TestMetrics { get; set; }
        public bool Calibrated { get; set; }
        public double? CalibrationSlope { get; set; }
        public double? CalibrationIntercept { get; set; }
        public double[] FeatureImportances { get; set; } = [];
    }
}