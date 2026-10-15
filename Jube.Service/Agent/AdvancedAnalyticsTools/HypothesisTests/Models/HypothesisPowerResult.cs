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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models
{
    public class HypothesisPowerResult
    {
        public string TestName { get; set; } = string.Empty;
        public HypothesisPowerSolveFor SolvedFor { get; set; }
        public double Power { get; set; }
        public double EffectSize { get; set; }
        public double Alpha { get; set; }
        public HypothesisTail Tail { get; set; }
        public double SampleSize1 { get; set; }
        public double SampleSize2 { get; set; }
        public double TotalSampleSize { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }
}