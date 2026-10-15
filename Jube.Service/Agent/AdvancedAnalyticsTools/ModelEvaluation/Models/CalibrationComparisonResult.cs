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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models
{
    public sealed class CalibrationComparisonResult
    {
        public required string Family { get; init; }
        public required int Seed { get; init; }
        public required int CalibrationRows { get; init; }
        public required int TestRows { get; init; }
        public required double TestPositiveRate { get; init; }
        public required CalibrationMethodRow[] Methods { get; init; }
        public required string BestMethod { get; init; }
        public required EvaluationWarning[] Warnings { get; init; }
        public required string Interpretation { get; init; }
    }
}