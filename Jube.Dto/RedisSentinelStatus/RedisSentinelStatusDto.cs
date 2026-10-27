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

namespace Jube.Dto.RedisSentinelStatus
{
    [GridEndpoint("RedisSentinelStatus", Title = "Redis sentinel status", DefaultSort = "occurredDate")]
    [GridFilter(Param = "entityTypeId", Label = "Entity", Choices = ["1=Master", "2=Slave", "3=Sentinel"])]
    [GridSearch(Label = "Name, IP or flags contains")]
    public sealed record RedisSentinelStatusDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp this Sentinel topology snapshot was taken.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property:
            Description("Which kind of entity this row describes -- see EntityTypeName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int EntityTypeId,
        [property: Description("Name of the entity kind this row describes: 'Master', 'Slave' or 'Sentinel'.")]
        [property: ListColumn(Order = 20, Title = "Entity Type")]
        string EntityTypeName,
        [property:
            Description(
                "Sentinel's name for this entity -- the monitored master's configured name for a master row, or host:port for a slave/sentinel row.")]
        [property: ListColumn(Order = 30)]
        string Name,
        [property: Description("IP address Sentinel is using to reach this entity.")]
        [property: ListColumn(Order = 40)]
        string Ip,
        [property: Description("Port Sentinel is using to reach this entity.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Code)]
        int Port,
        [property:
            Description(
                "Sentinel's raw flags for this entity (e.g. 'master', 'slave', 's_down', 'o_down', 'failover_in_progress') -- the most direct signal of an ongoing problem.")]
        [property: ListColumn(Order = 60)]
        string Flags,
        [property:
            Description(
                "For a slave row: the replication link status Sentinel observed ('ok' or 'err'). Null for master/sentinel rows.")]
        [property: ListColumn(Order = 70)]
        string MasterLinkStatus,
        [property:
            Description(
                "For a slave row: the host of the master it is replicating from, as Sentinel sees it. Null for master/sentinel rows.")]
        [property: ListColumn(Order = 80)]
        string MasterHost,
        [property:
            Description(
                "For a slave row: the port of the master it is replicating from. Null for master/sentinel rows.")]
        [property: ListColumn(Order = 90, Format = GridFormat.Code)]
        int? MasterPort,
        [property:
            Description(
                "For a slave row: its last known replication offset, for comparing how caught-up different replicas are. Null for master/sentinel rows.")]
        [property: ListColumn(Order = 100, Format = GridFormat.Integer)]
        long? SlaveReplOffset,
        [property:
            Description(
                "For a slave row: seconds since this replica's last ACK to its master, read directly from the master's own INFO replication reply (Sentinel itself reports no lag/time figure, only the raw offset above) and matched to this row by ip:port. Null for master/sentinel rows, or if the master couldn't be reached when this row was sampled.")]
        [property: ListColumn(Order = 110, Title = "Replication Lag", Unit = "s", Format = GridFormat.Decimal)]
        double? ReplicationLagSeconds,
        [property:
            Description(
                "For a master row: how many slaves Sentinel currently sees for it. Null for slave/sentinel rows.")]
        [property: ListColumn(Order = 120, Format = GridFormat.Integer)]
        int? NumSlaves,
        [property:
            Description("For a master row: how many other Sentinels are monitoring it. Null for slave/sentinel rows.")]
        [property: ListColumn(Order = 130, Format = GridFormat.Integer)]
        int? NumOtherSentinels,
        [property:
            Description(
                "For a master row: the quorum needed to mark it objectively down. Null for slave/sentinel rows.")]
        [property: ListColumn(Order = 140, Format = GridFormat.Integer)]
        int? Quorum,
        [property:
            Description("Milliseconds of unresponsiveness before Sentinel considers this entity subjectively down.")]
        [property: ListColumn(Order = 150, Title = "Down After", Unit = "ms", Format = GridFormat.Integer)]
        long? DownAfterMilliseconds,
        [property: Description("This entity's Redis/Sentinel run id.")]
        [property: ListColumn(Order = 160)]
        string RunId,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 180, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that took this sample.")]
        [property: ListColumn(Order = 170)]
        string Instance);
}