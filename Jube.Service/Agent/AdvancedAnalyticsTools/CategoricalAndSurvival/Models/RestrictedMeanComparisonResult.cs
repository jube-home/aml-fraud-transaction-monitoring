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
    public sealed class RestrictedMeanComparisonResult
    {
        public string FirstGroup { get; init; } = string.Empty;
        public string SecondGroup { get; init; } = string.Empty;
        public double Horizon { get; init; }
        public double RestrictedMeanFirst { get; init; }
        public double RestrictedMeanSecond { get; init; }
        public double Difference { get; init; }
        public double DifferenceStandardError { get; init; }
        public double DifferenceLower { get; init; }
        public double DifferenceUpper { get; init; }
        public double Ratio { get; init; }
        public double ZStatistic { get; init; }
        public double PValue { get; init; }
        public bool HorizonBeyondLastObservation { get; init; }
        public int SkippedRows { get; init; }
    }
}