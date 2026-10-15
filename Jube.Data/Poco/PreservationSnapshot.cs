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
using LinqToDB;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class PreservationSnapshot
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public byte SnapshotSourceId { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }

        [Column]
        [Nullable]
        [DataType(DataType.BinaryJson)]
        [Key(5)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(6)] public int? ExportVersion { get; set; }
        [Column] [Nullable] [Key(7)] public Guid? ExportGuid { get; set; }
        [Column] [Nullable] [Key(8)] public int? EntityAnalysisModelCount { get; set; }
        [Column] [Nullable] [Key(9)] public long? Bytes { get; set; }
        [Column] [Nullable] [Key(10)] public byte? InError { get; set; }
        [Column] [Nullable] [Key(11)] public string ErrorStack { get; set; }
        [Column] [Nullable] [Key(12)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(14)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(15)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(17)] public string DeletedUser { get; set; }
    }
}