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
    public class EntityAnalysisModelBacktestInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Key(1)] public Guid Guid { get; set; }
        [Column] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Key(3)] public int EntityAnalysisModelId { get; set; }
        [Column] [Key(4)] public string RuleType { get; set; }
        [Column] [Nullable] [Key(5)] public int? RuleId { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Key(6)]
        public string Request { get; set; }

        [Column] [Key(7)] public short Status { get; set; }
        [Column] [Key(8)] public long Scanned { get; set; }
        [Column] [Key(9)] public long Evaluated { get; set; }
        [Column] [Key(10)] public double Progress { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(11)]
        public string Result { get; set; }

        [Column] [Nullable] [Key(12)] public string Error { get; set; }
        [Column] [Key(13)] public DateTime CreatedDate { get; set; }
        [Column] [Nullable] [Key(14)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? StartedDate { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? HeartbeatDate { get; set; }
        [Column] [Nullable] [Key(18)] public string ClaimedBy { get; set; }
        [Column] [Nullable] [Key(19)] public DateTime? ClaimedDate { get; set; }
        [Column] [Key(20)] public short Deleted { get; set; }
        [Column] [Nullable] [Key(21)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(22)] public string DeletedUser { get; set; }
    }
}