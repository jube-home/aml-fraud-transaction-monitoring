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

namespace Jube.Dto.ExhaustiveInstanceDataset
{
    [MessagePackObject]
    public class ExhaustiveInstanceDatasetColumnDto
    {
        [Description("Zero based position of the column in the uploaded file.")]
        [Key(0)]
        public int Index { get; set; }

        [Description("Header text exactly as it appeared in the uploaded file. Treat as untrusted text.")]
        [Key(1)]
        public string? Header { get; set; }

        [Description("Inferred kind: Numeric, Text, Mixed or Empty.")]
        [Key(2)]
        public string? Kind { get; set; }

        [Key(3)]
        public long NumericCount { get; set; }
        [Key(4)]
        public long TextCount { get; set; }
        [Key(5)]
        public long MissingCount { get; set; }
        [Key(6)]
        public double? Min { get; set; }
        [Key(7)]
        public double? Max { get; set; }
        [Key(8)]
        public double? Mean { get; set; }
        [Key(9)]
        public long ZeroCount { get; set; }
        [Key(10)]
        public long OneCount { get; set; }
        [Key(11)]
        public bool IsBinary { get; set; }
        [Key(12)]
        public bool IsConstant { get; set; }
        [Key(13)]
        public long FormulaLikeCount { get; set; }

        [Description("Whether the column is used as a training variable.")]
        [Key(14)]
        public bool Include { get; set; }

        [Description("Variable name used at recall. Letters, digits and underscore, starting with a letter.")]
        [Key(15)]
        public string? Name { get; set; }

        [Description("Processing type used at recall: 1 Payload, 2 Dictionary, 3 TtlCounter, 4 Sanction, " +
                     "5 Abstraction, 6 AbstractionCalculation.")]
        [Key(16)]
        public int ProcessingTypeId { get; set; }

        [Description("Whether a Text or Mixed column was fitted as a categorical variable and can be included.")]
        [Key(17)]
        public bool IsCategorical { get; set; }

        [Description("Most frequent distinct values with their row counts, capped to a sample. Offered as " +
                     "filter values when this column is used in the instance's Class Definition.")]
        [Key(18)]
        public List<DatasetDistinctValueDto> DistinctValues { get; set; } = [];
    }
}