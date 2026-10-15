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
    public class ExhaustiveInstanceDatasetDto
    {
        [Description("Server-assigned identifier. Read-only.")]
        public int Id { get; set; }

        [Description("Server-assigned globally unique identifier. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Display name of the dataset.")]
        public string? Name { get; set; }

        [Description(
            "Csv or Json: which store holds the dataset. Together with Id it identifies the dataset. Read-only.")]
        public string? Kind { get; set; }

        [Description("Csv only: file name as supplied by the client, reduced to its final path segment. Read-only.")]
        public string? FileName { get; set; }

        [Description("Json only: caller supplied label describing where the JSON came from. Read-only.")]
        public string? SourceLabel { get; set; }

        [Description("Json only: SHA-256 of the received JSON bytes as lower case hex. Read-only.")]
        public string? ContentHash { get; set; }

        public long RowCount { get; set; }
        public long SizeBytes { get; set; }
        public int ColumnCount { get; set; }

        [Description("0 Awaiting mapping confirmation, 1 Confirmed and usable as training data.")]
        public byte StatusId { get; set; }

        public char? Delimiter { get; set; }
        public List<ExhaustiveInstanceDatasetColumnDto> Columns { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
        public string? CreatedUser { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public int Version { get; set; }
    }
}