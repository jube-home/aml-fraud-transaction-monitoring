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

namespace Jube.Dto.CaptureQueueHealth
{
    [GridEndpoint("CaptureQueueHealth", Title = "Capture queue health", DefaultSort = "createdDate")]
    [GridFilter(Param = "queueId", Label = "Queue", Choices = ["1=ModelInvokeWarning", "2=CaseCreationWarning", "3=ArchiverWarning", "4=RedisSentinelEvent", "5=RedisConnectionEvent", "6=OpenTelemetryMetric"])]
    public sealed record CaptureQueueHealthDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property:
            Description(
                "Which bounded in-memory capture queue this row reports on -- see QueueName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int QueueId,
        [property:
            Description(
                "Name of the capture queue: ModelInvokeWarning, CaseCreationWarning, ArchiverWarning, RedisSentinelEvent, RedisConnectionEvent, or OpenTelemetryMetric.")]
        [property: ListColumn(Order = 20, Title = "Queue")]
        string QueueName,
        [property:
            Description(
                "How many entries were sitting in the queue at the moment it was flushed -- normally near zero, since it drains every ~60s cycle; a value that never falls is a sign the queue isn't draining.")]
        [property: ListColumn(Order = 30, Title = "Depth", Format = GridFormat.Integer)]
        int QueueDepth,
        [property:
            Description(
                "How many entries were dropped because the queue was at capacity (20000, or 10000 distinct series for OpenTelemetryMetric) since the previous flush -- always 0 in healthy operation; any non-zero value here means real data was silently discarded and is worth investigating immediately.")]
        [property: ListColumn(Order = 40, Title = "Dropped", Format = GridFormat.Integer)]
        long DroppedCount,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this row.")]
        [property: ListColumn(Order = 50)]
        string Instance);
}