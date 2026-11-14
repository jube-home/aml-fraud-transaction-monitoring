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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.PatroniMemberStatus
{
    [GridEndpoint("PatroniMemberStatus", Title = "Patroni member status", DefaultSort = "occurredDate")]
    [GridSearch(Label = "Name, host, role, state, scope contains")]
    [MessagePackObject]
    public sealed record PatroniMemberStatusDto(
        [property: Key(0)] [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Key(1)] [property: Description("UTC timestamp this Patroni member was sampled.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Key(2)] [property: Description("This member's Patroni name, as configured in its patroni.yml.")]
        [property: ListColumn(Order = 20)]
        string Name,
        [property: Key(3)] [property: Description("The Postgres host for this member.")]
        [property: ListColumn(Order = 30)]
        string Host,
        [property: Key(4)] [property: Description("The Postgres port for this member.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Code)]
        int? Port,
        [property: Key(5)] [property: Description("Base URL of this member's Patroni REST API.")]
        [property: ListColumn(Order = 50)]
        string ApiUrl,
        [property: Key(6)] [property:
            Description(
                "Patroni's own view of this member's role: 'leader', 'replica', 'standby_leader' or 'sync_standby' -- the authoritative, cluster-wide answer, from GET /cluster rather than this member's own possibly-stale opinion of itself.")]
        [property: ListColumn(Order = 60)]
        string Role,
        [property: Key(7)] [property:
            Description(
                "This member's state (e.g. 'running', 'streaming', 'stopped', 'starting', 'creating replica').")]
        [property: ListColumn(Order = 70)]
        string State,
        [property: Key(8)] [property:
            Description(
                "The Postgres timeline this member is on -- differs from the leader's after a failover until the replica catches up and rejoins the new timeline.")]
        [property: ListColumn(Order = 80, Title = "Timeline")]
        int? TimelineId,
        [property: Key(9)] [property:
            Description(
                "Patroni's own computed replication lag for this member, in bytes; null for the leader itself or when Patroni could not compute one (e.g. immediately after a role change).")]
        [property: ListColumn(Order = 90, Title = "Lag", Unit = "B", Format = GridFormat.Integer)]
        long? LagBytes,
        [property: Key(10)] [property:
            Description(
                "Whether this member has a pending restart -- true typically means a configuration change requires a restart to take effect.")]
        [property: ListColumn(Order = 100)]
        bool? PendingRestart,
        [property: Key(11)] [property: Description("The Patroni version running on this member.")]
        [property: ListColumn(Order = 110)]
        string PatroniVersion,
        [property: Key(12)] [property: Description("The Patroni cluster scope (cluster name) this member belongs to.")]
        [property: ListColumn(Order = 120)]
        string Scope,
        [property: Key(13)] [property:
            Description(
                "The Postgres server_version this member reports, Postgres' own packed integer form (e.g. 150004 means 15.4).")]
        [property: ListColumn(Order = 130, Format = GridFormat.Integer)]
        int? PostgresServerVersion,
        [property: Key(14)] [property:
            Description(
                "This member's Postgres database system identifier -- every member of a healthy cluster should report the same value; a member reporting a different one has diverged (e.g. it was reinitialised without rejoining properly).")]
        [property: ListColumn(Order = 140)]
        string DatabaseSystemIdentifier,
        [property: Key(15)] [property:
            Description(
                "Current WAL location in bytes -- the leader's own write position, or a replica's last replayed position when it has no distinct received/replayed split reported.")]
        [property: ListColumn(Order = 150, Title = "Xlog Location", Unit = "B", Format = GridFormat.Integer)]
        long? XlogLocationBytes,
        [property: Key(16)] [property:
            Description(
                "For a replica: WAL bytes received from the leader but not yet replayed -- the gap between this and XlogLocationBytes is WAL sitting in the replica's receive buffer, distinct from LagBytes (which Patroni computes against the leader's position).")]
        [property: ListColumn(Order = 160, Title = "Received Location", Unit = "B", Format = GridFormat.Integer)]
        long? ReceivedLocationBytes,
        [property: Key(17)] [property:
            Description(
                "For a replica: whether WAL replay is currently paused (e.g. recovery_target_action or a manual pg_wal_replay_pause). Null for the leader.")]
        [property: ListColumn(Order = 170)]
        bool? ReplayPaused,
        [property: Key(18)] [property:
            Description(
                "Whether the cluster currently has no leader lock held in the DCS -- true is a red flag (no member is safely writable) usually seen only mid-failover.")]
        [property: ListColumn(Order = 180)]
        bool? ClusterUnlocked,
        [property: Key(19)] [property:
            Description(
                "Whether this member is the leader's designated synchronous standby, derived from the leader's own GET /patroni 'replication' array at sample time. Null when this member is the leader itself or synchronous replication isn't configured; false for an async replica.")]
        [property: ListColumn(Order = 190)]
        bool? SyncStandby,
        [property: Key(20)] [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 210, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Key(21)] [property: Description("Hostname of the Jube node that took this sample.")]
        [property: ListColumn(Order = 200)]
        string Instance);
}