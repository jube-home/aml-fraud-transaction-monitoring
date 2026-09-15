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

namespace Jube.Dto.PostgresReplicationStatus
{
    public sealed record PostgresReplicationStatusDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this snapshot of pg_stat_replication was taken.")]
        DateTime OccurredDate,
        [property: Description("Process id of the WAL sender backend on the primary serving this standby.")]
        int Pid,
        [property: Description("Replication user the standby connected as.")]
        string UserName,
        [property:
            Description(
                "The standby's application_name, as set in its connection string -- typically how you tell replicas apart.")]
        string ApplicationName,
        [property: Description("Network address the standby connected from.")]
        string ClientAddress,
        [property:
            Description(
                "Replication state (e.g. streaming, catchup, backup) reported by the primary for this standby.")]
        string State,
        [property: Description("Last WAL location sent to this standby.")]
        string SentLsn,
        [property: Description("Last WAL location written by this standby.")]
        string WriteLsn,
        [property: Description("Last WAL location flushed to durable storage by this standby.")]
        string FlushLsn,
        [property: Description("Last WAL location replayed (applied) by this standby.")]
        string ReplayLsn,
        [property: Description("Estimated seconds between the primary writing WAL and the standby receiving it.")]
        double WriteLagSeconds,
        [property:
            Description("Estimated seconds between the primary writing WAL and the standby flushing it to disk.")]
        double FlushLagSeconds,
        [property:
            Description(
                "Estimated seconds between the primary writing WAL and the standby replaying it -- the closest analogue to end-to-end replication lag.")]
        double ReplayLagSeconds,
        [property: Description("Synchronous replication state (async, potential, sync, quorum) for this standby.")]
        string SyncState,
        [property:
            Description(
                "Priority for synchronous replica selection, when synchronous_standby_names uses priority-based selection.")]
        int SyncPriority,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the primary node that took this sample.")]
        string Instance);
}