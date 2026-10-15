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
    public sealed class MantelHaenszelResult
    {
        public int StrataUsed { get; init; }
        public int StrataDropped { get; init; }
        public double CommonOddsRatio { get; init; }
        public double LogStandardError { get; init; }
        public double Lower { get; init; }
        public double Upper { get; init; }
        public double CochranMantelHaenszelChiSquare { get; init; }
        public double CochranMantelHaenszelPValue { get; init; }
        public double CochranMantelHaenszelCorrectedChiSquare { get; init; }
        public double CochranMantelHaenszelCorrectedPValue { get; init; }
        public double BreslowDayChiSquare { get; init; }
        public double BreslowDayTaroneChiSquare { get; init; }
        public int BreslowDayDegreesOfFreedom { get; init; }
        public double BreslowDayPValue { get; init; }
        public double BreslowDayTaronePValue { get; init; }
        public double[] StratumOddsRatios { get; init; } = [];
        public double ConfidenceLevel { get; init; }
    }
}