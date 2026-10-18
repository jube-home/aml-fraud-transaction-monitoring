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

namespace Jube.Engine.AdvancedAnalytics.Regression.Models
{
    public sealed class GradientBoostingRegressorEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        public int Rounds { get; set; }
        public int TreesFitted { get; set; }
        public int MaxDepth { get; set; }
        public double LearningRate { get; set; }
        public int MinimumLeafSize { get; set; }
        public double SampleFraction { get; set; }
        public double BaseValue { get; set; }
        public double[] TrainingLossByRound { get; set; } = [];
        public GradientBoostingRegressorSplitMetrics TrainMetrics { get; set; }
        public GradientBoostingRegressorSplitMetrics ValidationMetrics { get; set; }
        public GradientBoostingRegressorSplitMetrics TestMetrics { get; set; }
        public double[] FeatureImportances { get; set; } = [];
    }
}