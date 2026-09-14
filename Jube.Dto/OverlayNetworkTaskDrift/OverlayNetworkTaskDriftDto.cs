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

namespace Jube.Dto.OverlayNetworkTaskDrift
{
    public sealed record OverlayNetworkTaskDriftDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this reconciliation check was performed.")]
        DateTime? OccurredDate,
        [property: Description("The Swarm service name checked (e.g. jube-ui, haproxy, patroni1).")]
        string ServiceName,
        [property:
            Description(
                "Comma-separated overlay addresses returned by resolving \"tasks.<ServiceName>\" (Swarm's per-task DNS convention, bypassing any VIP) at the time of this check.")]
        string DnsResolvedAddresses,
        [property:
            Description(
                "Comma-separated overlay addresses of every task the Swarm API currently reports as running for this service.")]
        string SwarmTaskAddresses,
        [property:
            Description(
                "Addresses DNS returned that no running Swarm task currently has -- a stale/cached DNS answer pointing at a task that no longer exists.")]
        string AddressesOnlyInDns,
        [property:
            Description(
                "Addresses a running Swarm task currently has that DNS did not return -- a newly-scheduled task the DNS layer has not caught up to yet.")]
        string AddressesOnlyInSwarm,
        [property:
            Description(
                "True when DnsResolvedAddresses and SwarmTaskAddresses are exactly the same set -- the headline column for spotting drift at a glance.")]
        bool? IsConsistent,
        [property: Description("UTC timestamp this sample was captured and flushed to the database.")]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring instance that ran this check -- only a manager-node instance ever produces rows, since the Swarm API is manager-only.")]
        string Instance);
}