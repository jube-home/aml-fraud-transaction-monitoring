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
    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelTtlCounterVersion
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelTtlCounterId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }

        [Column("CreatedDate")]
        [Nullable]
        [Key(6)]
        public DateTime? CreatedDate { get; set; }

        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public int? Version { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public string TtlCounterInterval { get; set; }
        [Column] [Nullable] [Key(13)] public int? TtlCounterValue { get; set; }
        [Column] [Nullable] [Key(14)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(15)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(16)] public string TtlCounterDataName { get; set; }
        [Column] [Nullable] [Key(17)] public byte? OnlineAggregation { get; set; }
        [Column] [Nullable] [Key(18)] public byte? EnableLiveForever { get; set; }
        [Column] [Nullable] [Key(19)] public byte? EnableSum { get; set; }
        [Column] [Nullable] [Key(20)] public string ResolutionInterval { get; set; }
        [Column] [Nullable] [Key(21)] public string TtlCounterDataValue { get; set; }
    }
}