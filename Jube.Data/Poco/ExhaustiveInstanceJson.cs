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

using System;
using Jube.Data.Repository.Interfaces;
using LinqToDB;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class ExhaustiveInstanceJson : IDatasetStore
    {
        [Column] [Nullable] [Key(4)] public string SourceLabel { get; set; }
        [Column] [Nullable] [Key(7)] public string ContentHash { get; set; }

        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Key(1)] public Guid Guid { get; set; }
        [Column] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Key(3)] public string Name { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(5)]
        public string ColumnsJson { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(6)]
        public string ReportJson { get; set; }

        [Obsolete(
            "Superseded by the instance Filter (Class Definition). Retained, unused, for Preservation compatibility.")]
        [Column]
        [Nullable]
        [Key(8)]
        public int? DependentColumnIndex { get; set; }

        [Column] [Key(9)] public long RowCount { get; set; }
        [Column] [Key(10)] public long SizeBytes { get; set; }
        [Column] [Key(11)] public byte StatusId { get; set; }
        [Column] [Nullable] [Key(12)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(14)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(15)] public int? Version { get; set; }
        [Column] [Nullable] [Key(16)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(17)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(18)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(19)] public int? ImportId { get; set; }
    }
}