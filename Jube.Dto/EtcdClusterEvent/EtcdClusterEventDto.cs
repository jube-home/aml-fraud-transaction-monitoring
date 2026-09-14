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

namespace Jube.Dto.EtcdClusterEvent
{
    public sealed record EtcdClusterEventDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property:
            Description(
                "UTC timestamp the change was detected -- the one-minute sample this change first appeared in, not necessarily the exact moment it happened.")]
        DateTime OccurredDate,
        [property: Description("The etcd endpoint this event was detected on.")]
        string Endpoint,
        [property: Description("The reporting member's own etcd cluster member id.")]
        string MemberId,
        [property: Description("The reporting member's configured name.")]
        string Name,
        [property:
            Description("Which kind of change this row captures -- see EventTypeName for the human-readable form.")]
        int EventTypeId,
        [property:
            Description(
                "Name of what changed: 'LeaderChanged' (this member's view of the raft leader changed), 'HealthChanged' (GET /health flipped ok/not-ok), 'AlarmRaised' or 'AlarmCleared' (an alarm such as NOSPACE or CORRUPT appeared or cleared). Detected by comparing each one-minute EtcdMemberStatus sample to the previous one for the same endpoint -- etcd has no push notification for these the way Redis Sentinel does.")]
        string EventTypeName,
        [property:
            Description(
                "The value before the change; null for AlarmRaised (there was nothing there before) or when this is the first sample seen for this endpoint.")]
        string PreviousValue,
        [property: Description("The value after the change; null for AlarmCleared (there is nothing there now).")]
        string NewValue,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that detected this event.")]
        string Instance);
}