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
    public sealed class InformationTheoryAssociationResult
    {
        public double RowEntropy { get; init; }
        public double ColumnEntropy { get; init; }
        public double JointEntropy { get; init; }
        public double MutualInformation { get; init; }
        public double NormalisedMutualInformation { get; init; }
        public double UncertaintyCoefficientRowGivenColumn { get; init; }
        public double UncertaintyCoefficientColumnGivenRow { get; init; }
        public double UncertaintyCoefficientSymmetric { get; init; }
        public string Unit { get; init; } = string.Empty;
    }
}