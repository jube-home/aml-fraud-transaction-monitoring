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

namespace Jube.Dto.RedisSentinelStatus
{
    public sealed record RedisSentinelStatusDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this Sentinel topology snapshot was taken.")]
        DateTime OccurredDate,
        [property:
            Description("Which kind of entity this row describes -- see EntityTypeName for the human-readable form.")]
        int EntityTypeId,
        [property: Description("Name of the entity kind this row describes: 'Master', 'Slave' or 'Sentinel'.")]
        string EntityTypeName,
        [property:
            Description(
                "Sentinel's name for this entity -- the monitored master's configured name for a master row, or host:port for a slave/sentinel row.")]
        string Name,
        [property: Description("IP address Sentinel is using to reach this entity.")]
        string Ip,
        [property: Description("Port Sentinel is using to reach this entity.")]
        int Port,
        [property:
            Description(
                "Sentinel's raw flags for this entity (e.g. 'master', 'slave', 's_down', 'o_down', 'failover_in_progress') -- the most direct signal of an ongoing problem.")]
        string Flags,
        [property:
            Description(
                "For a slave row: the replication link status Sentinel observed ('ok' or 'err'). Null for master/sentinel rows.")]
        string MasterLinkStatus,
        [property:
            Description(
                "For a slave row: the host of the master it is replicating from, as Sentinel sees it. Null for master/sentinel rows.")]
        string MasterHost,
        [property:
            Description(
                "For a slave row: the port of the master it is replicating from. Null for master/sentinel rows.")]
        int? MasterPort,
        [property:
            Description(
                "For a slave row: its last known replication offset, for comparing how caught-up different replicas are. Null for master/sentinel rows.")]
        long? SlaveReplOffset,
        [property:
            Description(
                "For a slave row: seconds since this replica's last ACK to its master, read directly from the master's own INFO replication reply (Sentinel itself reports no lag/time figure, only the raw offset above) and matched to this row by ip:port. Null for master/sentinel rows, or if the master couldn't be reached when this row was sampled.")]
        double? ReplicationLagSeconds,
        [property:
            Description(
                "For a master row: how many slaves Sentinel currently sees for it. Null for slave/sentinel rows.")]
        int? NumSlaves,
        [property:
            Description("For a master row: how many other Sentinels are monitoring it. Null for slave/sentinel rows.")]
        int? NumOtherSentinels,
        [property:
            Description(
                "For a master row: the quorum needed to mark it objectively down. Null for slave/sentinel rows.")]
        int? Quorum,
        [property:
            Description("Milliseconds of unresponsiveness before Sentinel considers this entity subjectively down.")]
        long? DownAfterMilliseconds,
        [property: Description("This entity's Redis/Sentinel run id.")]
        string RunId,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that took this sample.")]
        string Instance);
}