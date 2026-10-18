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
    public sealed class TwoByTwoAnalysisResult
    {
        public double ExposedEvents { get; init; }
        public double ExposedNonEvents { get; init; }
        public double UnexposedEvents { get; init; }
        public double UnexposedNonEvents { get; init; }
        public OddsRatioResult Odds { get; init; } = new();
        public RiskRatioResult Risk { get; init; } = new();
        public RiskDifferenceResult Difference { get; init; } = new();
        public YuleAssociationResult Yule { get; init; } = new();
        public double ChiSquare { get; init; }
        public double ChiSquarePValue { get; init; }
        public double YatesChiSquare { get; init; }
        public double YatesPValue { get; init; }
        public double SmallestExpectedCount { get; init; }
    }
}