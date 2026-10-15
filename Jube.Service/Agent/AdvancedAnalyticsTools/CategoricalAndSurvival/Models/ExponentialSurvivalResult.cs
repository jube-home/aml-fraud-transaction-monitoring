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
    public sealed class ExponentialSurvivalResult
    {
        public double Rate { get; init; }
        public double RateLower { get; init; }
        public double RateUpper { get; init; }
        public double MeanLifetime { get; init; }
        public double MeanLifetimeLower { get; init; }
        public double MeanLifetimeUpper { get; init; }
        public double MedianLifetime { get; init; }
        public int Events { get; init; }
        public int Subjects { get; init; }
        public double TotalTimeAtRisk { get; init; }
        public ExponentialSurvivalPoint[] SurvivalAtTimes { get; init; } = [];
        public double MaximumAbsoluteDifferenceFromKaplanMeier { get; init; }
        public string Fit { get; init; } = string.Empty;
        public int SkippedRows { get; init; }
    }
}