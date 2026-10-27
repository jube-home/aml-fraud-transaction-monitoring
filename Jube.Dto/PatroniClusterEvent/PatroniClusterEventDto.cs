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

namespace Jube.Dto.PatroniClusterEvent
{
    [GridEndpoint("PatroniClusterEvent", Title = "Patroni cluster event", DefaultSort = "occurredDate")]
    [GridFilter(Param = "eventTypeId", Label = "Event", Choices = ["1=RoleChanged", "2=StateChanged", "3=TimelineChanged", "4=Failover"])]
    [GridSearch(Label = "Scope, name, reason contains")]
    public sealed record PatroniClusterEventDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property:
            Description(
                "UTC timestamp this event was recorded. For RoleChanged/StateChanged/TimelineChanged this is the one-minute sample the change was first detected in; for Failover it is Patroni's own history timestamp when available, otherwise also the detecting sample's time.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Description("The Patroni cluster scope this event belongs to.")]
        [property: ListColumn(Order = 20)]
        string Scope,
        [property:
            Description(
                "For RoleChanged/StateChanged/TimelineChanged: the member this happened to. For Failover: the new leader that took over.")]
        [property: ListColumn(Order = 30)]
        string Name,
        [property:
            Description("Which kind of change this row captures -- see EventTypeName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int EventTypeId,
        [property:
            Description(
                "Name of what happened: 'RoleChanged' (a member's leader/replica/standby_leader role changed), 'StateChanged' (running/streaming/stopped/etc. changed), 'TimelineChanged' (the member moved to a new Postgres timeline, typically after a failover), or 'Failover' -- a precise, Patroni-recorded failover/switchover read directly from Patroni's own DCS history, not inferred from polling.")]
        [property: ListColumn(Order = 40, Title = "Event Type")]
        string EventTypeName,
        [property:
            Description(
                "The value before the change (RoleChanged/StateChanged/TimelineChanged), or the leader that was replaced (Failover, from the previous history entry -- null for the earliest entry Patroni still retains).")]
        [property: ListColumn(Order = 50)]
        string PreviousValue,
        [property:
            Description(
                "The value after the change (RoleChanged/StateChanged/TimelineChanged), or the new leader (Failover).")]
        [property: ListColumn(Order = 60)]
        string NewValue,
        [property:
            Description(
                "Patroni's own recorded reason for the failover/switchover (e.g. 'no recovery target specified', 'manual failover'). Only populated for EventType 'Failover'.")]
        [property: ListColumn(Order = 70)]
        string Reason,
        [property:
            Description(
                "The Postgres timeline that began at this failover. Only populated for EventType 'Failover' (see also the generic TimelineChanged event, which is detected independently by polling).")]
        [property: ListColumn(Order = 80, Title = "Timeline")]
        int? TimelineId,
        [property:
            Description(
                "The WAL position, in bytes, at which this failover occurred. Only populated for EventType 'Failover'.")]
        [property: ListColumn(Order = 90, Title = "LSN", Unit = "B", Format = GridFormat.Integer)]
        long? LsnBytes,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 110, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that recorded this event.")]
        [property: ListColumn(Order = 100)]
        string Instance);
}