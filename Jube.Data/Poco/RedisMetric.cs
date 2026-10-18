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
    public class RedisMetric
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Instance { get; set; }
        [Column] [Nullable] [Key(3)] public int? ConnectedClients { get; set; }
        [Column] [Nullable] [Key(4)] public int? BlockedClients { get; set; }
        [Column] [Nullable] [Key(5)] public long? UsedMemoryBytes { get; set; }
        [Column] [Nullable] [Key(6)] public long? UsedMemoryRssBytes { get; set; }
        [Column] [Nullable] [Key(7)] public long? MaxMemoryBytes { get; set; }
        [Column] [Nullable] [Key(8)] public int? InstantaneousOpsPerSecond { get; set; }
        [Column] [Nullable] [Key(9)] public long? TotalCommandsProcessed { get; set; }
        [Column] [Nullable] [Key(10)] public long? TotalConnectionsReceived { get; set; }
        [Column] [Nullable] [Key(11)] public long? KeyspaceHits { get; set; }
        [Column] [Nullable] [Key(12)] public long? KeyspaceMisses { get; set; }
        [Column] [Nullable] [Key(13)] public double? HitRatePercent { get; set; }
        [Column] [Nullable] [Key(14)] public long? EvictedKeys { get; set; }
        [Column] [Nullable] [Key(15)] public long? ExpiredKeys { get; set; }
        [Column] [Nullable] [Key(16)] public int? ConnectedReplicas { get; set; }
        [Column] [Nullable] [Key(17)] public long? MasterReplicationOffset { get; set; }
        [Column] [Nullable] [Key(18)] public long? UptimeSeconds { get; set; }
        [Column] [Nullable] [Key(19)] public long? TotalKeys { get; set; }
        [Column] [Nullable] [Key(20)] public double? MemoryFragmentationRatio { get; set; }
        [Column] [Nullable] [Key(21)] public long? RdbLastSaveAgeSeconds { get; set; }
        [Column] [Nullable] [Key(22)] public bool? AofEnabled { get; set; }
        [Column] [Nullable] [Key(23)] public DateTime? LastAofRewriteDate { get; set; }
        [Column] [Nullable] [Key(24)] public DateTime? LastBgSaveDate { get; set; }
    }
}