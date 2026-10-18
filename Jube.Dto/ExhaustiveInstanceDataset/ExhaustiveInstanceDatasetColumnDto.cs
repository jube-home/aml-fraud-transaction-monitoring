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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.ExhaustiveInstanceDataset
{
    public class ExhaustiveInstanceDatasetColumnDto
    {
        [Description("Zero based position of the column in the uploaded file.")]
        public int Index { get; set; }

        [Description("Header text exactly as it appeared in the uploaded file. Treat as untrusted text.")]
        public string? Header { get; set; }

        [Description("Inferred kind: Numeric, Text, Mixed or Empty.")]
        public string? Kind { get; set; }

        public long NumericCount { get; set; }
        public long TextCount { get; set; }
        public long MissingCount { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Mean { get; set; }
        public long ZeroCount { get; set; }
        public long OneCount { get; set; }
        public bool IsBinary { get; set; }
        public bool IsConstant { get; set; }
        public long FormulaLikeCount { get; set; }

        [Description("Whether the column is used as a training variable.")]
        public bool Include { get; set; }

        [Description("Variable name used at recall. Letters, digits and underscore, starting with a letter.")]
        public string? Name { get; set; }

        [Description("Processing type used at recall: 1 Payload, 2 Dictionary, 3 TtlCounter, 4 Sanction, " +
                     "5 Abstraction, 6 AbstractionCalculation.")]
        public int ProcessingTypeId { get; set; }

        [Description("Whether a Text or Mixed column was fitted as a categorical variable and can be included.")]
        public bool IsCategorical { get; set; }

        [Description("Most frequent distinct values with their row counts, capped to a sample. Offered as " +
                     "filter values when this column is used in the instance's Class Definition.")]
        public List<DatasetDistinctValueDto> DistinctValues { get; set; } = [];
    }
}