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

namespace Jube.Dto.CaptureQueueHealth
{
    public sealed record CaptureQueueHealthDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property:
            Description(
                "Which bounded in-memory capture queue this row reports on -- see QueueName for the human-readable form.")]
        int QueueId,
        [property:
            Description(
                "Name of the capture queue: ModelInvokeWarning, CaseCreationWarning, ArchiverWarning, RedisSentinelEvent, RedisConnectionEvent, or OpenTelemetryMetric.")]
        string QueueName,
        [property:
            Description(
                "How many entries were sitting in the queue at the moment it was flushed -- normally near zero, since it drains every ~60s cycle; a value that never falls is a sign the queue isn't draining.")]
        int QueueDepth,
        [property:
            Description(
                "How many entries were dropped because the queue was at capacity (20000, or 10000 distinct series for OpenTelemetryMetric) since the previous flush -- always 0 in healthy operation; any non-zero value here means real data was silently discarded and is worth investigating immediately.")]
        long DroppedCount,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this row.")]
        string Instance);
}