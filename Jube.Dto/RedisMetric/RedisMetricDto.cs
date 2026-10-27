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

namespace Jube.Dto.RedisMetric
{
    [GridEndpoint("RedisMetric", Title = "Redis metric", DefaultSort = "createdDate")]
    [GridSearch(Label = "Instance (hostname) contains")]
    public sealed record RedisMetricDto(
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
        [property: Description("Number of client connections currently connected.")]
        [property: ListColumn(Order = 30, Format = GridFormat.Integer)]
        int ConnectedClients,
        [property: Description("Number of clients currently blocked on a blocking command.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        int BlockedClients,
        [property: Description("Total memory Redis is using, in bytes.")]
        [property: ListColumn(Order = 50, Title = "Used Memory", Unit = "B", Format = GridFormat.Integer)]
        long UsedMemoryBytes,
        [property: Description("Resident set size Redis is using as reported by the OS, in bytes.")]
        [property: ListColumn(Order = 60, Title = "Used Memory RSS", Unit = "B", Format = GridFormat.Integer)]
        long UsedMemoryRssBytes,
        [property: Description("Configured maximum memory limit, in bytes (0 means no limit).")]
        [property: ListColumn(Order = 70, Title = "Max Memory", Unit = "B", Format = GridFormat.Integer)]
        long MaxMemoryBytes,
        [property: Description("Commands processed per second at sample time.")]
        [property: ListColumn(Order = 80, Title = "Ops/Sec", Format = GridFormat.Integer)]
        int InstantaneousOpsPerSecond,
        [property: Description("Cumulative commands processed since server start.")]
        [property: ListColumn(Order = 90, Format = GridFormat.Integer)]
        long TotalCommandsProcessed,
        [property: Description("Cumulative client connections received since server start.")]
        [property: ListColumn(Order = 100, Format = GridFormat.Integer)]
        long TotalConnectionsReceived,
        [property: Description("Cumulative keyspace hits since server start.")]
        [property: ListColumn(Order = 110, Format = GridFormat.Integer)]
        long KeyspaceHits,
        [property: Description("Cumulative keyspace misses since server start.")]
        [property: ListColumn(Order = 120, Format = GridFormat.Integer)]
        long KeyspaceMisses,
        [property:
            Description(
                "Keyspace hit rate over the interval since the previous sample, as a percentage; null on the first sample after startup.")]
        [property: ListColumn(Order = 130, Title = "Hit Rate %", Unit = "%", Format = GridFormat.Decimal)]
        double? HitRatePercent,
        [property: Description("Cumulative keys evicted due to the maxmemory policy, since server start.")]
        [property: ListColumn(Order = 140, Format = GridFormat.Integer)]
        long EvictedKeys,
        [property: Description("Cumulative keys expired via TTL, since server start.")]
        [property: ListColumn(Order = 150, Format = GridFormat.Integer)]
        long ExpiredKeys,
        [property: Description("Number of connected replicas.")]
        [property: ListColumn(Order = 160, Format = GridFormat.Integer)]
        int ConnectedReplicas,
        [property: Description("This node's master replication offset.")]
        [property: ListColumn(Order = 170, Format = GridFormat.Integer)]
        long MasterReplicationOffset,
        [property: Description("Seconds the Redis server has been running.")]
        [property: ListColumn(Order = 180, Title = "Uptime", Unit = "s", Format = GridFormat.Integer)]
        long UptimeSeconds,
        [property: Description("Total number of keys across every logical database.")]
        [property: ListColumn(Order = 190, Format = GridFormat.Integer)]
        long TotalKeys,
        [property:
            Description(
                "Ratio of RSS memory to used memory; well above 1.0 indicates fragmentation, well below 1.0 (with swap) indicates the OS has swapped some of Redis' memory out.")]
        [property: ListColumn(Order = 200, Format = GridFormat.Decimal)]
        double? MemoryFragmentationRatio,
        [property:
            Description(
                "Seconds since the last successful RDB snapshot; null if none has ever succeeded. A large or climbing value with persistence enabled means snapshotting is stale or failing.")]
        [property: ListColumn(Order = 210, Title = "RDB Last Save Age", Unit = "s", Format = GridFormat.Integer)]
        long? RdbLastSaveAgeSeconds,
        [property: Description("Whether the append-only file (AOF) persistence log is enabled.")]
        [property: ListColumn(Order = 220)]
        bool AofEnabled,
        [property:
            Description(
                "UTC timestamp of the last completed AOF rewrite (background fork that compacts the AOF file), as observed since this sampler started -- null until the first rewrite is observed after startup, even if earlier rewrites happened before then. AOF rewrites and RDB background saves both fork the process; overlapping or frequent forks are a source of latency/memory contention, so compare this against LastBgSaveDate to spot the two colliding.")]
        [property: ListColumn(Order = 230, Title = "Last AOF Rewrite")]
        DateTime? LastAofRewriteDate,
        [property:
            Description(
                "UTC timestamp of the last successful RDB background save (BGSAVE) -- the fork-based snapshot copy, sometimes called a 'background copy'; null if none has ever succeeded. Compare against LastAofRewriteDate to spot overlapping forks.")]
        [property: ListColumn(Order = 240, Title = "Last BGSAVE (BgCopy)")]
        DateTime? LastBgSaveDate);
}