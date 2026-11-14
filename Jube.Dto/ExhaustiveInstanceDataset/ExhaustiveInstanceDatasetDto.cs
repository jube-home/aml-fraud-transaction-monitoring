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
    public class ExhaustiveInstanceDatasetDto
    {
        [Description("Server-assigned identifier. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Server-assigned globally unique identifier. Read-only.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("Display name of the dataset.")]
        [Key(2)]
        public string? Name { get; set; }

        [Description(
            "Csv or Json: which store holds the dataset. Together with Id it identifies the dataset. Read-only.")]
        [Key(3)]
        public string? Kind { get; set; }

        [Description("Csv only: file name as supplied by the client, reduced to its final path segment. Read-only.")]
        [Key(4)]
        public string? FileName { get; set; }

        [Description("Json only: caller supplied label describing where the JSON came from. Read-only.")]
        [Key(5)]
        public string? SourceLabel { get; set; }

        [Description("Json only: SHA-256 of the received JSON bytes as lower case hex. Read-only.")]
        [Key(6)]
        public string? ContentHash { get; set; }

        [Key(7)]
        public long RowCount { get; set; }
        [Key(8)]
        public long SizeBytes { get; set; }
        [Key(9)]
        public int ColumnCount { get; set; }

        [Description("0 Awaiting mapping confirmation, 1 Confirmed and usable as training data.")]
        [Key(10)]
        public byte StatusId { get; set; }

        [Key(11)]
        public char? Delimiter { get; set; }
        [Key(12)]
        public List<ExhaustiveInstanceDatasetColumnDto> Columns { get; set; } = [];
        [Key(13)]
        public List<string> Warnings { get; set; } = [];
        [Key(14)]
        public string? CreatedUser { get; set; }
        [Key(15)]
        public DateTimeOffset? CreatedDate { get; set; }
        [Key(16)]
        public int Version { get; set; }
    }
}