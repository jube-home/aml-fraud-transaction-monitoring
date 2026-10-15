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
    public class PatroniMemberStatus
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? OccurredDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public string Host { get; set; }
        [Column] [Nullable] [Key(4)] public int? Port { get; set; }
        [Column] [Nullable] [Key(5)] public string ApiUrl { get; set; }
        [Column] [Nullable] [Key(6)] public string Role { get; set; }
        [Column] [Nullable] [Key(7)] public string State { get; set; }
        [Column] [Nullable] [Key(8)] public int? TimelineId { get; set; }
        [Column] [Nullable] [Key(9)] public long? LagBytes { get; set; }
        [Column] [Nullable] [Key(10)] public bool? PendingRestart { get; set; }
        [Column] [Nullable] [Key(11)] public string PatroniVersion { get; set; }
        [Column] [Nullable] [Key(12)] public string Scope { get; set; }
        [Column] [Nullable] [Key(13)] public int? PostgresServerVersion { get; set; }
        [Column] [Nullable] [Key(14)] public string DatabaseSystemIdentifier { get; set; }
        [Column] [Nullable] [Key(15)] public long? XlogLocationBytes { get; set; }
        [Column] [Nullable] [Key(16)] public long? ReceivedLocationBytes { get; set; }
        [Column] [Nullable] [Key(17)] public bool? ReplayPaused { get; set; }
        [Column] [Nullable] [Key(18)] public bool? ClusterUnlocked { get; set; }
        [Column] [Nullable] [Key(19)] public bool? SyncStandby { get; set; }
        [Column] [Nullable] [Key(20)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(21)] public string Instance { get; set; }
    }
}