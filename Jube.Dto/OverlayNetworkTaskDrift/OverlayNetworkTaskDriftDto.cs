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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.OverlayNetworkTaskDrift
{
    [GridEndpoint("OverlayNetworkTaskDrift", Title = "Overlay network task drift", DefaultSort = "createdDate")]
    [GridSearch(Label = "Service name contains")]
    public sealed record OverlayNetworkTaskDriftDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp this reconciliation check was performed.")]
        [property: ListColumn(Hidden = true)]
        DateTime? OccurredDate,
        [property: Description("The Swarm service name checked (e.g. jube-ui, haproxy, patroni1).")]
        [property: ListColumn(Order = 30, Title = "Service")]
        string ServiceName,
        [property:
            Description(
                "Comma-separated overlay addresses returned by resolving \"tasks.<ServiceName>\" (Swarm's per-task DNS convention, bypassing any VIP) at the time of this check.")]
        [property: ListColumn(Order = 50, Title = "DNS Addresses")]
        string DnsResolvedAddresses,
        [property:
            Description(
                "Comma-separated overlay addresses of every task the Swarm API currently reports as running for this service.")]
        [property: ListColumn(Order = 60)]
        string SwarmTaskAddresses,
        [property:
            Description(
                "Addresses DNS returned that no running Swarm task currently has -- a stale/cached DNS answer pointing at a task that no longer exists.")]
        [property: ListColumn(Order = 70, Title = "Only In DNS")]
        string AddressesOnlyInDns,
        [property:
            Description(
                "Addresses a running Swarm task currently has that DNS did not return -- a newly-scheduled task the DNS layer has not caught up to yet.")]
        [property: ListColumn(Order = 80, Title = "Only In Swarm")]
        string AddressesOnlyInSwarm,
        [property:
            Description(
                "True when DnsResolvedAddresses and SwarmTaskAddresses are exactly the same set -- the headline column for spotting drift at a glance.")]
        [property: ListColumn(Order = 40, Title = "Consistent")]
        bool? IsConsistent,
        [property: Description("UTC timestamp this sample was captured and flushed to the database.")]
        [property: ListColumn(Order = 10, Title = "Sampled Date")]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring instance that ran this check -- only a manager-node instance ever produces rows, since the Swarm API is manager-only.")]
        [property: ListColumn(Order = 20)]
        string Instance);
}