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
    public sealed class LiftGainRow
    {
        public int Group { get; init; }
        public double ScoreMax { get; init; }
        public double ScoreMin { get; init; }
        public int Count { get; init; }
        public int Events { get; init; }
        public double EventRate { get; init; }
        public double Lift { get; init; }
        public int CumulativeCount { get; init; }
        public int CumulativeEvents { get; init; }
        public double CumulativeEventRate { get; init; }
        public double CumulativeLift { get; init; }
        public double CumulativeGain { get; init; }
        public double CumulativeNonEventShare { get; init; }
        public double KolmogorovSmirnov { get; init; }
    }
}