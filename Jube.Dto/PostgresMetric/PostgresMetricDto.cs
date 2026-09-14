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

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.PostgresMetric
{
    public sealed record PostgresMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this row's sample was taken.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node this sample was taken from.")]
        string Instance,
        [property: Description("Active backend connections to the database.")]
        int ActiveConnections,
        [property: Description("Cumulative transactions committed, since Postgres statistics were last reset.")]
        long TransactionsCommitted,
        [property: Description("Cumulative transactions rolled back, since Postgres statistics were last reset.")]
        long TransactionsRolledBack,
        [property: Description("Cumulative disk blocks read, since Postgres statistics were last reset.")]
        long BlocksRead,
        [property:
            Description(
                "Cumulative buffer cache blocks hit (no disk read needed), since Postgres statistics were last reset.")]
        long BlocksHit,
        [property:
            Description(
                "Buffer cache hit ratio over the interval since the previous sample, as a percentage; null on the first sample after startup.")]
        double? CacheHitRatioPercent,
        [property: Description("Cumulative rows returned by scans, since Postgres statistics were last reset.")]
        long RowsReturned,
        [property: Description("Cumulative rows fetched from scans, since Postgres statistics were last reset.")]
        long RowsFetched,
        [property: Description("Cumulative rows inserted, since Postgres statistics were last reset.")]
        long RowsInserted,
        [property: Description("Cumulative rows updated, since Postgres statistics were last reset.")]
        long RowsUpdated,
        [property: Description("Cumulative rows deleted, since Postgres statistics were last reset.")]
        long RowsDeleted,
        [property: Description("Cumulative deadlocks detected, since Postgres statistics were last reset.")]
        long Deadlocks,
        [property: Description("Cumulative temporary files created, since Postgres statistics were last reset.")]
        long TempFilesCreated,
        [property:
            Description("Cumulative bytes written to temporary files, since Postgres statistics were last reset.")]
        long TempBytesWritten,
        [property:
            Description(
                "Cumulative queries cancelled due to conflicts with recovery, since Postgres statistics were last reset.")]
        long Conflicts,
        [property: Description("Whether this node is a streaming replica (in recovery) at sample time.")]
        bool IsInRecovery,
        [property:
            Description(
                "If this node is a replica, its own replay lag behind the primary; if this node is a primary, the maximum lag reported by any of its replicas.")]
        double? ReplicationLagSeconds,
        [property:
            Description("Number of streaming replicas connected to this node (0 if this node is itself a replica).")]
        int ReplicaCount,
        [property: Description("On-disk size of the current database, in bytes.")]
        long DatabaseSizeBytes,
        [property:
            Description(
                "How long the single longest-running active query has been running, in seconds; null if no query is currently active.")]
        double? LongestRunningQuerySeconds,
        [property:
            Description(
                "Number of backends currently waiting to acquire a lock -- a rising count points at lock contention.")]
        int WaitingBackends,
        [property:
            Description(
                "Cumulative WAL bytes generated (or received, on a replica) since the current WAL position was first measurable; diff between consecutive rows for a per-minute WAL generation rate.")]
        long WalBytesGenerated);
}