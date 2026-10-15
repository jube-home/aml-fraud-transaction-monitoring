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
    public class HypothesisAnovaResult
    {
        public string TestName { get; set; } = string.Empty;
        public double FStatistic { get; set; }
        public double PValue { get; set; }
        public double DegreesOfFreedomBetween { get; set; }
        public double DegreesOfFreedomWithin { get; set; }
        public double Alpha { get; set; }
        public bool RejectNull { get; set; }
        public double EtaSquared { get; set; }
        public double[] GroupMeans { get; set; } = Array.Empty<double>();
        public int[] GroupSizes { get; set; } = Array.Empty<int>();
        public HypothesisAnovaRow[] Table { get; set; } = Array.Empty<HypothesisAnovaRow>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public string Interpretation { get; set; } = string.Empty;
    }
}