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

namespace Jube.Dto.RedisMetric
{
    public sealed record RedisMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this row's sample was taken.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node this sample was taken from.")]
        string Instance,
        [property: Description("Number of client connections currently connected.")]
        int ConnectedClients,
        [property: Description("Number of clients currently blocked on a blocking command.")]
        int BlockedClients,
        [property: Description("Total memory Redis is using, in bytes.")]
        long UsedMemoryBytes,
        [property: Description("Resident set size Redis is using as reported by the OS, in bytes.")]
        long UsedMemoryRssBytes,
        [property: Description("Configured maximum memory limit, in bytes (0 means no limit).")]
        long MaxMemoryBytes,
        [property: Description("Commands processed per second at sample time.")]
        int InstantaneousOpsPerSecond,
        [property: Description("Cumulative commands processed since server start.")]
        long TotalCommandsProcessed,
        [property: Description("Cumulative client connections received since server start.")]
        long TotalConnectionsReceived,
        [property: Description("Cumulative keyspace hits since server start.")]
        long KeyspaceHits,
        [property: Description("Cumulative keyspace misses since server start.")]
        long KeyspaceMisses,
        [property:
            Description(
                "Keyspace hit rate over the interval since the previous sample, as a percentage; null on the first sample after startup.")]
        double? HitRatePercent,
        [property: Description("Cumulative keys evicted due to the maxmemory policy, since server start.")]
        long EvictedKeys,
        [property: Description("Cumulative keys expired via TTL, since server start.")]
        long ExpiredKeys,
        [property: Description("Number of connected replicas.")]
        int ConnectedReplicas,
        [property: Description("This node's master replication offset.")]
        long MasterReplicationOffset,
        [property: Description("Seconds the Redis server has been running.")]
        long UptimeSeconds,
        [property: Description("Total number of keys across every logical database.")]
        long TotalKeys,
        [property:
            Description(
                "Ratio of RSS memory to used memory; well above 1.0 indicates fragmentation, well below 1.0 (with swap) indicates the OS has swapped some of Redis' memory out.")]
        double? MemoryFragmentationRatio,
        [property:
            Description(
                "Seconds since the last successful RDB snapshot; null if none has ever succeeded. A large or climbing value with persistence enabled means snapshotting is stale or failing.")]
        long? RdbLastSaveAgeSeconds,
        [property: Description("Whether the append-only file (AOF) persistence log is enabled.")]
        bool AofEnabled,
        [property:
            Description(
                "UTC timestamp of the last completed AOF rewrite (background fork that compacts the AOF file), as observed since this sampler started -- null until the first rewrite is observed after startup, even if earlier rewrites happened before then. AOF rewrites and RDB background saves both fork the process; overlapping or frequent forks are a source of latency/memory contention, so compare this against LastBgSaveDate to spot the two colliding.")]
        DateTime? LastAofRewriteDate,
        [property:
            Description(
                "UTC timestamp of the last successful RDB background save (BGSAVE) -- the fork-based snapshot copy, sometimes called a 'background copy'; null if none has ever succeeded. Compare against LastAofRewriteDate to spot overlapping forks.")]
        DateTime? LastBgSaveDate);
}