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
    public class GraphPattern
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public long Id { get; set; }

        [Column] [Key(1)] public int TenantRegistryId { get; set; }
        [Column] [Key(2)] public byte KindId { get; set; }
        [Column] [Key(3)] public long AnchorNodeId { get; set; }
        [Column] [Key(4)] public double Score { get; set; }
        [Column] [Key(5)] public DateTime WindowFrom { get; set; }
        [Column] [Key(6)] public DateTime WindowTo { get; set; }
        [Column] [Nullable] [DataType(DataType.BinaryJson)] [Key(7)] public string Parameters { get; set; }
        [Column] [Nullable] [Key(8)] public long[] ArchiveIds { get; set; }
        [Column] [Key(9)] public int MemberCount { get; set; }
        [Column] [Nullable] [Key(10)] public int? CaseId { get; set; }
        [Column] [Key(11)] public byte StateId { get; set; }
        [Column] [Key(12)] public DateTime FirstDetectedDate { get; set; }
        [Column] [Key(13)] public DateTime LastDetectedDate { get; set; }
        [Column] [Nullable] [Key(14)] public string DecidedUser { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? DecidedDate { get; set; }
    }
}
