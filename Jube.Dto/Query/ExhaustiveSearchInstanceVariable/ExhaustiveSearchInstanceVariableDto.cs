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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.ExhaustiveSearchInstanceVariable
{
    [Description("One variable of an Exhaustive Search Instance with its distribution statistics and histogram.")]
    public class ExhaustiveSearchInstanceVariableDto
    {
        [Description("Integer Id of the Exhaustive Search Instance Variable.")]
        public int Id { get; set; }

        [Description("Mean of the variable.")] public double Mean { get; set; }

        [Description("Standard deviation of the variable.")]
        public double StandardDeviation { get; set; }

        [Description("Name of the variable.")] public string? Name { get; set; }

        [Description("Kurtosis of the variable.")]
        public double Kurtosis { get; set; }

        [Description("Skewness of the variable.")]
        public double Skewness { get; set; }

        [Description("Maximum of the variable.")]
        public double Maximum { get; set; }

        [Description(
            "Minimum of the variable. Legacy contract: the underlying query never populates this, so it is always zero.")]
        public double Minimum { get; set; }

        [Description("Interquartile range of the variable.")]
        public double Iqr { get; set; }

        [Description("Display name of the normalisation type: No, Binary, Z Score or Default.")]
        public string? NormalisationType { get; set; }

        [Description("Count of distinct values of the variable.")]
        public int DistinctValues { get; set; }

        [Description("Correlation of the variable with the target.")]
        public double Correlation { get; set; }

        [Description("Rank of the variable by absolute correlation.")]
        public int CorrelationAbsRank { get; set; }

        [Description("Histogram of the variable.")]
        public List<ExhaustiveSearchInstanceVariableHistogramValueDto> HistogramValues { get; set; } = [];
    }
}