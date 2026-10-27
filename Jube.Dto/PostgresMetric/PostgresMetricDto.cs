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
using Jube.Dto.Forms;
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.PostgresMetric
{
    [GridEndpoint("PostgresMetric", Title = "Postgres metric", DefaultSort = "createdDate")]
    [GridSearch(Label = "Instance (hostname) contains")]
    public sealed record PostgresMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp this row's sample was taken.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Description("Hostname of the node this sample was taken from.")]
        [property: ListColumn(Order = 20)]
        string Instance,
        [property: Description("Active backend connections to the database.")]
        [property: ListColumn(Order = 30, Format = GridFormat.Integer)]
        int ActiveConnections,
        [property: Description("Cumulative transactions committed, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        long TransactionsCommitted,
        [property: Description("Cumulative transactions rolled back, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        long TransactionsRolledBack,
        [property: Description("Cumulative disk blocks read, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 60, Format = GridFormat.Integer)]
        long BlocksRead,
        [property:
            Description(
                "Cumulative buffer cache blocks hit (no disk read needed), since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 70, Format = GridFormat.Integer)]
        long BlocksHit,
        [property:
            Description(
                "Buffer cache hit ratio over the interval since the previous sample, as a percentage; null on the first sample after startup.")]
        [property: ListColumn(Order = 80, Title = "Cache Hit Ratio %", Unit = "%", Format = GridFormat.Decimal)]
        double? CacheHitRatioPercent,
        [property: Description("Cumulative rows returned by scans, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 90, Format = GridFormat.Integer)]
        long RowsReturned,
        [property: Description("Cumulative rows fetched from scans, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 100, Format = GridFormat.Integer)]
        long RowsFetched,
        [property: Description("Cumulative rows inserted, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 110, Format = GridFormat.Integer)]
        long RowsInserted,
        [property: Description("Cumulative rows updated, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 120, Format = GridFormat.Integer)]
        long RowsUpdated,
        [property: Description("Cumulative rows deleted, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 130, Format = GridFormat.Integer)]
        long RowsDeleted,
        [property: Description("Cumulative deadlocks detected, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 140, Format = GridFormat.Integer)]
        long Deadlocks,
        [property: Description("Cumulative temporary files created, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 150, Format = GridFormat.Integer)]
        long TempFilesCreated,
        [property:
            Description("Cumulative bytes written to temporary files, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 160, Format = GridFormat.Integer)]
        long TempBytesWritten,
        [property:
            Description(
                "Cumulative queries cancelled due to conflicts with recovery, since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 170, Format = GridFormat.Integer)]
        long Conflicts,
        [property: Description("Whether this node is a streaming replica (in recovery) at sample time.")]
        [property: ListColumn(Order = 180, Title = "In Recovery")]
        bool IsInRecovery,
        [property:
            Description(
                "If this node is a replica, its own replay lag behind the primary; if this node is a primary, the maximum lag reported by any of its replicas.")]
        [property: ListColumn(Order = 190, Title = "Replication Lag", Unit = "s", Format = GridFormat.Decimal)]
        double? ReplicationLagSeconds,
        [property:
            Description("Number of streaming replicas connected to this node (0 if this node is itself a replica).")]
        [property: ListColumn(Order = 200, Format = GridFormat.Integer)]
        int ReplicaCount,
        [property: Description("On-disk size of the current database, in bytes.")]
        [property: ListColumn(Order = 210, Title = "Database Size", Unit = "B", Format = GridFormat.Integer)]
        long DatabaseSizeBytes,
        [property:
            Description(
                "How long the single longest-running active query has been running, in seconds; null if no query is currently active.")]
        [property: ListColumn(Order = 220, Title = "Longest Running Query", Unit = "s", Format = GridFormat.Decimal)]
        double? LongestRunningQuerySeconds,
        [property:
            Description(
                "Number of backends currently waiting to acquire a lock -- a rising count points at lock contention.")]
        [property: ListColumn(Order = 230, Format = GridFormat.Integer)]
        int WaitingBackends,
        [property:
            Description(
                "Cumulative WAL bytes generated (or received, on a replica) since the current WAL position was first measurable; diff between consecutive rows for a per-minute WAL generation rate.")]
        [property: ListColumn(Order = 240, Format = GridFormat.Integer)]
        long WalBytesGenerated);
}