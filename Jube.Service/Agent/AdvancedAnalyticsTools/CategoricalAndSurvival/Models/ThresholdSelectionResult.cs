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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models
{
    public sealed class ThresholdSelectionResult
    {
        public string Objective { get; init; } = string.Empty;
        public double Threshold { get; init; }
        public double ObjectiveValue { get; init; }
        public bool Achievable { get; init; }
        public string Note { get; init; } = string.Empty;
        public ConfusionMetricsResult Metrics { get; init; } = new();
        public double BayesOptimalThreshold { get; init; }
        public double ExpectedCostFlagNone { get; init; }
        public double ExpectedCostFlagAll { get; init; }
        public int SkippedRows { get; init; }
    }
}