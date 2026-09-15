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

namespace Jube.Dto.PatroniClusterEvent
{
    public sealed record PatroniClusterEventDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property:
            Description(
                "UTC timestamp this event was recorded. For RoleChanged/StateChanged/TimelineChanged this is the one-minute sample the change was first detected in; for Failover it is Patroni's own history timestamp when available, otherwise also the detecting sample's time.")]
        DateTime OccurredDate,
        [property: Description("The Patroni cluster scope this event belongs to.")]
        string Scope,
        [property:
            Description(
                "For RoleChanged/StateChanged/TimelineChanged: the member this happened to. For Failover: the new leader that took over.")]
        string Name,
        [property:
            Description("Which kind of change this row captures -- see EventTypeName for the human-readable form.")]
        int EventTypeId,
        [property:
            Description(
                "Name of what happened: 'RoleChanged' (a member's leader/replica/standby_leader role changed), 'StateChanged' (running/streaming/stopped/etc. changed), 'TimelineChanged' (the member moved to a new Postgres timeline, typically after a failover), or 'Failover' -- a precise, Patroni-recorded failover/switchover read directly from Patroni's own DCS history, not inferred from polling.")]
        string EventTypeName,
        [property:
            Description(
                "The value before the change (RoleChanged/StateChanged/TimelineChanged), or the leader that was replaced (Failover, from the previous history entry -- null for the earliest entry Patroni still retains).")]
        string PreviousValue,
        [property:
            Description(
                "The value after the change (RoleChanged/StateChanged/TimelineChanged), or the new leader (Failover).")]
        string NewValue,
        [property:
            Description(
                "Patroni's own recorded reason for the failover/switchover (e.g. 'no recovery target specified', 'manual failover'). Only populated for EventType 'Failover'.")]
        string Reason,
        [property:
            Description(
                "The Postgres timeline that began at this failover. Only populated for EventType 'Failover' (see also the generic TimelineChanged event, which is detected independently by polling).")]
        int? TimelineId,
        [property:
            Description(
                "The WAL position, in bytes, at which this failover occurred. Only populated for EventType 'Failover'.")]
        long? LsnBytes,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that recorded this event.")]
        string Instance);
}