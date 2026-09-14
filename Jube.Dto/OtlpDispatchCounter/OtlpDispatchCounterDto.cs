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

namespace Jube.Dto.OtlpDispatchCounter
{
    public sealed record OtlpDispatchCounterDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property:
            Description(
                "Which OTLP signal this row's dispatch attempts were for -- see SignalName for the human-readable form.")]
        int SignalId,
        [property:
            Description(
                "Name of the OTLP signal this row's dispatch attempts were for -- \"Traces\", \"Metrics\" or \"Logs\".")]
        string SignalName,
        [property:
            Description(
                "How many Export() calls to the configured OpenTelemetryBackendEndpoint (or the OTel SDK's own " +
                "OTEL_EXPORTER_OTLP_ENDPOINT resolution) completed during this one-minute window, successful or " +
                "not.")]
        long Count,
        [property: Description("Of Count, how many completed with ExportResult.Success.")]
        long SuccessCount,
        [property:
            Description("Of Count, how many completed with ExportResult.Failure -- e.g. the backend was " +
                        "unreachable or timed out. A dead backend shows up here, not as a growing queue.")]
        long FailureCount,
        [property:
            Description("Total spans/metric points/log records attempted across every dispatch counted in " +
                        "Count, successful or not.")]
        long ItemCount,
        [property:
            Description("How many spans/log records were dropped because the export queue was already full " +
                        "when they arrived (the OTel SDK's own buffer-full signal, captured via its self-" +
                        "diagnostics EventSource) -- i.e. queue size truncation, not send failure. Always 0 " +
                        "for the \"metrics\" Signal, which has no queue to overflow (see " +
                        "docs/Concepts/API/OpenTelemetryLogCounter/index.html).")]
        long DroppedCount,
        [property: Description("Sum of the elapsed time, in microseconds, of every dispatch counted in this row.")]
        long TotalMicroseconds,
        [property: Description("The fastest single dispatch counted in this row, in microseconds.")]
        long MinMicroseconds,
        [property: Description("The slowest single dispatch counted in this row, in microseconds.")]
        long MaxMicroseconds,
        [property:
            Description(
                "UTC timestamp this row was flushed to the database (the end of the one-minute window it covers).")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that recorded this row.")]
        string Instance);
}