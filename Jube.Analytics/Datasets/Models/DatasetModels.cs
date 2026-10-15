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

using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Analytics.Datasets.Models
{
    public enum DatasetCellKind : byte
    {
        Missing = 0,
        Number = 1,
        Text = 2
    }

    public readonly record struct DatasetCell(DatasetCellKind Kind, double Number, string? Text);

    public sealed record DatasetRow(long RowNumber, DatasetCell[] Cells);

    public sealed record DatasetDistinctValue(string Value, long Count);

    public sealed class DatasetColumnDefinition
    {
        public int Index { get; set; }
        public string Header { get; set; } = string.Empty;
        public string Kind { get; set; } = DatasetColumnKinds.Empty;
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
        public bool Include { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProcessingTypeId { get; set; } = DatasetProcessingTypes.Payload;
        public bool IsCategorical { get; set; }
        public string? CategoricalEncoderJson { get; set; }
        public List<DatasetDistinctValue> DistinctValues { get; set; } = [];

        [JsonIgnore] public bool IsNumeric => Kind == DatasetColumnKinds.Numeric;
        [JsonIgnore] public string StorageKey => DatasetRowJson.Key(Index);
    }

    public sealed record DatasetParseError(
        string Code,
        string Message,
        long? Row = null,
        int? Column = null,
        long? Line = null);
}