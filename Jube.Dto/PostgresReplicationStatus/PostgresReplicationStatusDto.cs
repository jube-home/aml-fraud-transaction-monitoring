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

namespace Jube.Dto.PostgresReplicationStatus
{
    [GridEndpoint("PostgresReplicationStatus", Title = "Postgres replication status", DefaultSort = "occurredDate")]
    [GridSearch(Label = "User, application, address or state contains")]
    public sealed record PostgresReplicationStatusDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp this snapshot of pg_stat_replication was taken.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Description("Process id of the WAL sender backend on the primary serving this standby.")]
        [property: ListColumn(Order = 20, Format = GridFormat.Code)]
        int Pid,
        [property: Description("Replication user the standby connected as.")]
        [property: ListColumn(Order = 30)]
        string UserName,
        [property:
            Description(
                "The standby's application_name, as set in its connection string -- typically how you tell replicas apart.")]
        [property: ListColumn(Order = 40)]
        string ApplicationName,
        [property: Description("Network address the standby connected from.")]
        [property: ListColumn(Order = 50)]
        string ClientAddress,
        [property:
            Description(
                "Replication state (e.g. streaming, catchup, backup) reported by the primary for this standby.")]
        [property: ListColumn(Order = 60)]
        string State,
        [property: Description("Last WAL location sent to this standby.")]
        [property: ListColumn(Order = 70)]
        string SentLsn,
        [property: Description("Last WAL location written by this standby.")]
        [property: ListColumn(Order = 80)]
        string WriteLsn,
        [property: Description("Last WAL location flushed to durable storage by this standby.")]
        [property: ListColumn(Order = 90)]
        string FlushLsn,
        [property: Description("Last WAL location replayed (applied) by this standby.")]
        [property: ListColumn(Order = 100)]
        string ReplayLsn,
        [property: Description("Estimated seconds between the primary writing WAL and the standby receiving it.")]
        [property: ListColumn(Order = 110, Title = "Write Lag", Unit = "s", Format = GridFormat.Decimal)]
        double WriteLagSeconds,
        [property:
            Description("Estimated seconds between the primary writing WAL and the standby flushing it to disk.")]
        [property: ListColumn(Order = 120, Title = "Flush Lag", Unit = "s", Format = GridFormat.Decimal)]
        double FlushLagSeconds,
        [property:
            Description(
                "Estimated seconds between the primary writing WAL and the standby replaying it -- the closest analogue to end-to-end replication lag.")]
        [property: ListColumn(Order = 130, Title = "Replay Lag", Unit = "s", Format = GridFormat.Decimal)]
        double ReplayLagSeconds,
        [property: Description("Synchronous replication state (async, potential, sync, quorum) for this standby.")]
        [property: ListColumn(Order = 140)]
        string SyncState,
        [property:
            Description(
                "Priority for synchronous replica selection, when synchronous_standby_names uses priority-based selection.")]
        [property: ListColumn(Order = 150, Format = GridFormat.Integer)]
        int SyncPriority,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 170, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the primary node that took this sample.")]
        [property: ListColumn(Order = 160)]
        string Instance);
}