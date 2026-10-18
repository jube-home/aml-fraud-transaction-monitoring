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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table(Name = "HAProxyServerStatus")]
    [MessagePackObject]
    public class HaProxyServerStatus
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? OccurredDate { get; set; }
        [Column] [Nullable] [Key(2)] public string PxName { get; set; }
        [Column] [Nullable] [Key(3)] public string SvName { get; set; }
        [Column] [Nullable] [Key(4)] public string Status { get; set; }
        [Column] [Nullable] [Key(5)] public string Addr { get; set; }
        [Column] [Nullable] [Key(6)] public string CheckStatus { get; set; }
        [Column] [Nullable] [Key(7)] public int? CheckCode { get; set; }
        [Column] [Nullable] [Key(8)] public int? ChkFail { get; set; }
        [Column] [Nullable] [Key(9)] public int? ChkDown { get; set; }
        [Column] [Nullable] [Key(10)] public int? LastChg { get; set; }
        [Column] [Nullable] [Key(11)] public int? Scur { get; set; }
        [Column] [Nullable] [Key(12)] public int? Qcur { get; set; }
        [Column] [Nullable] [Key(13)] public int? Weight { get; set; }
        [Column] [Nullable] [Key(14)] public int? Act { get; set; }
        [Column] [Nullable] [Key(15)] public int? Bck { get; set; }
        [Column] [Nullable] [Key(16)] public long? Hrsp2Xx { get; set; }
        [Column] [Nullable] [Key(17)] public long? Hrsp5Xx { get; set; }
        [Column] [Nullable] [Key(18)] public string Mode { get; set; }
        [Column] [Nullable] [Key(19)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(20)] public string Instance { get; set; }
    }
}