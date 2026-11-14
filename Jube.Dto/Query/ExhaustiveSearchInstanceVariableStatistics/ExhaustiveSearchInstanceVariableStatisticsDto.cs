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

namespace Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics
{
    [Description(
        "One variable of an Exhaustive Search Instance with every stored histogram: all rows, the positive class " +
        "as binned separately, the anomaly rows, the negative and positive class binned on the bin edges of all rows, " +
        "and the Monte Carlo prescription of the promoted model.")]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariableStatisticsDto
    {
        [Description("Integer Id of the Exhaustive Search Instance Variable.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Name of the variable.")] [Key(1)] public string? Name { get; set; }

        [Description("Statistics of all rows.")]
        [Key(2)]
        public ExhaustiveSearchInstanceVariableStatisticsSummaryDto Population { get; set; } = new();

        [Description("Histogram of all rows.")]
        [Key(3)]
        public List<ExhaustiveSearchInstanceVariableStatisticsBinDto> PopulationBins { get; set; } = [];

        [Description("Histogram of the classification (positive class) rows, binned on its own edges.")]
        [Key(4)]
        public List<ExhaustiveSearchInstanceVariableStatisticsBinDto> ClassificationBins { get; set; } = [];

        [Description("Histogram of the anomaly rows, binned on its own edges.")]
        [Key(5)]
        public List<ExhaustiveSearchInstanceVariableStatisticsBinDto> AnomalyBins { get; set; } = [];

        [Description("Histogram of the negative class on the bin edges of all rows.")]
        [Key(6)]
        public List<ExhaustiveSearchInstanceVariableStatisticsBinDto> NegativeBins { get; set; } = [];

        [Description("Histogram of the positive class on the bin edges of all rows.")]
        [Key(7)]
        public List<ExhaustiveSearchInstanceVariableStatisticsBinDto> PositiveBins { get; set; } = [];

        [Description(
            "Histogram of the Monte Carlo prescription: simulated inputs the promoted model scores above 0.5.")]
        [Key(8)]
        public List<ExhaustiveSearchInstanceVariableStatisticsBinDto> PrescriptionBins { get; set; } = [];

        [Description("Statistics of the classification rows, when there is a classification.")]
        [Key(9)]
        public ExhaustiveSearchInstanceVariableStatisticsSummaryDto? Classification { get; set; }

        [Description("Statistics of the anomaly rows, when anomaly detection ran.")]
        [Key(10)]
        public ExhaustiveSearchInstanceVariableStatisticsSummaryDto? Anomaly { get; set; }

        [Description("Statistics of the Monte Carlo prescription, when the promoted model has one.")]
        [Key(11)]
        public ExhaustiveSearchInstanceVariableStatisticsSummaryDto? Prescription { get; set; }
    }
}