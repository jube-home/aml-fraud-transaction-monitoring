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

using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceVariableVariance
{
    [Description("One multi-collinearity correlation between an Exhaustive Search Instance Variable and another " +
                 "(test) variable of the same Exhaustive Search Instance.")]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceTrialInstanceVariableVarianceDto
    {
        [Description("Name of the test variable the correlation is measured against.")]
        [Key(0)]
        public string? Name { get; set; }

        [Description("Correlation coefficient between the variable and the test variable.")]
        [Key(1)]
        public double Correlation { get; set; }

        [Description("Rank of the absolute correlation. Results are ordered by this rank ascending.")]
        [Key(2)]
        public int CorrelationAbsRank { get; set; }
    }
}