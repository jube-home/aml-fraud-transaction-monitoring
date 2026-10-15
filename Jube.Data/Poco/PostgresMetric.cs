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
    public class PostgresMetric
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Instance { get; set; }
        [Column] [Nullable] [Key(3)] public int? ActiveConnections { get; set; }
        [Column] [Nullable] [Key(4)] public long? TransactionsCommitted { get; set; }
        [Column] [Nullable] [Key(5)] public long? TransactionsRolledBack { get; set; }
        [Column] [Nullable] [Key(6)] public long? BlocksRead { get; set; }
        [Column] [Nullable] [Key(7)] public long? BlocksHit { get; set; }
        [Column] [Nullable] [Key(8)] public double? CacheHitRatioPercent { get; set; }
        [Column] [Nullable] [Key(9)] public long? RowsReturned { get; set; }
        [Column] [Nullable] [Key(10)] public long? RowsFetched { get; set; }
        [Column] [Nullable] [Key(11)] public long? RowsInserted { get; set; }
        [Column] [Nullable] [Key(12)] public long? RowsUpdated { get; set; }
        [Column] [Nullable] [Key(13)] public long? RowsDeleted { get; set; }
        [Column] [Nullable] [Key(14)] public long? Deadlocks { get; set; }
        [Column] [Nullable] [Key(15)] public long? TempFilesCreated { get; set; }
        [Column] [Nullable] [Key(16)] public long? TempBytesWritten { get; set; }
        [Column] [Nullable] [Key(17)] public long? Conflicts { get; set; }
        [Column] [Nullable] [Key(18)] public bool? IsInRecovery { get; set; }
        [Column] [Nullable] [Key(19)] public double? ReplicationLagSeconds { get; set; }
        [Column] [Nullable] [Key(20)] public int? ReplicaCount { get; set; }
        [Column] [Nullable] [Key(21)] public long? DatabaseSizeBytes { get; set; }
        [Column] [Nullable] [Key(22)] public double? LongestRunningQuerySeconds { get; set; }
        [Column] [Nullable] [Key(23)] public int? WaitingBackends { get; set; }
        [Column] [Nullable] [Key(24)] public long? WalBytesGenerated { get; set; }
    }
}