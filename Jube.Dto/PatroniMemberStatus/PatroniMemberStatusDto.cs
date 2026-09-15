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

namespace Jube.Dto.PatroniMemberStatus
{
    public sealed record PatroniMemberStatusDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this Patroni member was sampled.")]
        DateTime OccurredDate,
        [property: Description("This member's Patroni name, as configured in its patroni.yml.")]
        string Name,
        [property: Description("The Postgres host for this member.")]
        string Host,
        [property: Description("The Postgres port for this member.")]
        int? Port,
        [property: Description("Base URL of this member's Patroni REST API.")]
        string ApiUrl,
        [property:
            Description(
                "Patroni's own view of this member's role: 'leader', 'replica', 'standby_leader' or 'sync_standby' -- the authoritative, cluster-wide answer, from GET /cluster rather than this member's own possibly-stale opinion of itself.")]
        string Role,
        [property:
            Description(
                "This member's state (e.g. 'running', 'streaming', 'stopped', 'starting', 'creating replica').")]
        string State,
        [property:
            Description(
                "The Postgres timeline this member is on -- differs from the leader's after a failover until the replica catches up and rejoins the new timeline.")]
        int? TimelineId,
        [property:
            Description(
                "Patroni's own computed replication lag for this member, in bytes; null for the leader itself or when Patroni could not compute one (e.g. immediately after a role change).")]
        long? LagBytes,
        [property:
            Description(
                "Whether this member has a pending restart -- true typically means a configuration change requires a restart to take effect.")]
        bool? PendingRestart,
        [property: Description("The Patroni version running on this member.")]
        string PatroniVersion,
        [property: Description("The Patroni cluster scope (cluster name) this member belongs to.")]
        string Scope,
        [property:
            Description(
                "The Postgres server_version this member reports, Postgres' own packed integer form (e.g. 150004 means 15.4).")]
        int? PostgresServerVersion,
        [property:
            Description(
                "This member's Postgres database system identifier -- every member of a healthy cluster should report the same value; a member reporting a different one has diverged (e.g. it was reinitialised without rejoining properly).")]
        string DatabaseSystemIdentifier,
        [property:
            Description(
                "Current WAL location in bytes -- the leader's own write position, or a replica's last replayed position when it has no distinct received/replayed split reported.")]
        long? XlogLocationBytes,
        [property:
            Description(
                "For a replica: WAL bytes received from the leader but not yet replayed -- the gap between this and XlogLocationBytes is WAL sitting in the replica's receive buffer, distinct from LagBytes (which Patroni computes against the leader's position).")]
        long? ReceivedLocationBytes,
        [property:
            Description(
                "For a replica: whether WAL replay is currently paused (e.g. recovery_target_action or a manual pg_wal_replay_pause). Null for the leader.")]
        bool? ReplayPaused,
        [property:
            Description(
                "Whether the cluster currently has no leader lock held in the DCS -- true is a red flag (no member is safely writable) usually seen only mid-failover.")]
        bool? ClusterUnlocked,
        [property:
            Description(
                "Whether this member is the leader's designated synchronous standby, derived from the leader's own GET /patroni 'replication' array at sample time. Null when this member is the leader itself or synchronous replication isn't configured; false for an async replica.")]
        bool? SyncStandby,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that took this sample.")]
        string Instance);
}