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

namespace Jube.Dto.EtcdClusterEvent
{
    [GridEndpoint("EtcdClusterEvent", Title = "Etcd cluster event", DefaultSort = "occurredDate")]
    [GridFilter(Param = "eventTypeId", Label = "Event", Choices = ["1=LeaderChanged", "2=HealthChanged", "3=AlarmRaised", "4=AlarmCleared"])]
    [GridSearch(Label = "Endpoint, name contains")]
    public sealed record EtcdClusterEventDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property:
            Description(
                "UTC timestamp the change was detected -- the one-minute sample this change first appeared in, not necessarily the exact moment it happened.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Description("The etcd endpoint this event was detected on.")]
        [property: ListColumn(Order = 20)]
        string Endpoint,
        [property: Description("The reporting member's own etcd cluster member id.")]
        [property: ListColumn(Order = 30)]
        string MemberId,
        [property: Description("The reporting member's configured name.")]
        [property: ListColumn(Order = 40)]
        string Name,
        [property:
            Description("Which kind of change this row captures -- see EventTypeName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int EventTypeId,
        [property:
            Description(
                "Name of what changed: 'LeaderChanged' (this member's view of the raft leader changed), 'HealthChanged' (GET /health flipped ok/not-ok), 'AlarmRaised' or 'AlarmCleared' (an alarm such as NOSPACE or CORRUPT appeared or cleared). Detected by comparing each one-minute EtcdMemberStatus sample to the previous one for the same endpoint -- etcd has no push notification for these the way Redis Sentinel does.")]
        [property: ListColumn(Order = 50, Title = "Event Type")]
        string EventTypeName,
        [property:
            Description(
                "The value before the change; null for AlarmRaised (there was nothing there before) or when this is the first sample seen for this endpoint.")]
        [property: ListColumn(Order = 60)]
        string PreviousValue,
        [property: Description("The value after the change; null for AlarmCleared (there is nothing there now).")]
        [property: ListColumn(Order = 70)]
        string NewValue,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 90, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that detected this event.")]
        [property: ListColumn(Order = 80)]
        string Instance);
}