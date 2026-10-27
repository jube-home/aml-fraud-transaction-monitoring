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

namespace Jube.Dto.RedisSentinelEvent
{
    [GridEndpoint("RedisSentinelEvent", Title = "Redis sentinel event", DefaultSort = "occurredDate")]
    [GridSearch(Label = "Channel or message contains")]
    public sealed record RedisSentinelEventDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp the event was received from Sentinel's pub/sub.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property:
            Description(
                "The Sentinel pub/sub channel the event was published on (e.g. '+sdown', '-sdown', '+odown', '+switch-master', '+failover-state', '+slave', '+sentinel').")]
        [property: ListColumn(Order = 20)]
        string Channel,
        [property:
            Description(
                "Sentinel's raw event payload for this channel -- format varies by channel, typically space-separated fields describing the entity involved.")]
        [property: ListColumn(Order = 30)]
        string Message,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 50, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this event.")]
        [property: ListColumn(Order = 40)]
        string Instance);
}