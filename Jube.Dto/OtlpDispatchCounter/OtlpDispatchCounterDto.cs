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

namespace Jube.Dto.OtlpDispatchCounter
{
    [GridEndpoint("OtlpDispatchCounter", Title = "Otlp dispatch counter", DefaultSort = "createdDate")]
    [GridFilter(Param = "signalId", Label = "Signal", Choices = ["1=Traces", "2=Metrics", "3=Logs"])]
    [GridRatio("averageMicroseconds", nameof(TotalMicroseconds), nameof(Count), Title = "Average", Order = 120, Unit = "µs", Format = GridFormat.Integer)]
    [GridRatio("failureRate", nameof(Count), nameof(FailureCount), Title = "Failure Rate", Order = 60, Format = GridFormat.Integer)]
    public sealed record OtlpDispatchCounterDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property:
            Description(
                "Which OTLP signal this row's dispatch attempts were for -- see SignalName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int SignalId,
        [property:
            Description(
                "Name of the OTLP signal this row's dispatch attempts were for -- \"Traces\", \"Metrics\" or \"Logs\".")]
        [property: ListColumn(Order = 10, Title = "Signal")]
        string SignalName,
        [property:
            Description(
                "How many Export() calls to the configured OpenTelemetryBackendEndpoint (or the OTel SDK's own " +
                "OTEL_EXPORTER_OTLP_ENDPOINT resolution) completed during this one-minute window, successful or " +
                "not.")]
        [property: ListColumn(Order = 30, Title = "Dispatch Count", Format = GridFormat.Integer)]
        long Count,
        [property: Description("Of Count, how many completed with ExportResult.Success.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        long SuccessCount,
        [property:
            Description("Of Count, how many completed with ExportResult.Failure -- e.g. the backend was " +
                        "unreachable or timed out. A dead backend shows up here, not as a growing queue.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        long FailureCount,
        [property:
            Description("Total spans/metric points/log records attempted across every dispatch counted in " +
                        "Count, successful or not.")]
        [property: ListColumn(Order = 70, Format = GridFormat.Integer)]
        long ItemCount,
        [property:
            Description("How many spans/log records were dropped because the export queue was already full " +
                        "when they arrived (the OTel SDK's own buffer-full signal, captured via its self-" +
                        "diagnostics EventSource) -- i.e. queue size truncation, not send failure. Always 0 " +
                        "for the \"metrics\" Signal, which has no queue to overflow (see " +
                        "docs/Concepts/API/OpenTelemetryLogCounter/index.html).")]
        [property: ListColumn(Order = 80, Title = "Dropped Count (Queue Full)", Format = GridFormat.Integer)]
        long DroppedCount,
        [property: Description("Sum of the elapsed time, in microseconds, of every dispatch counted in this row.")]
        [property: ListColumn(Order = 90, Title = "Total", Unit = "µs", Format = GridFormat.Integer)]
        long TotalMicroseconds,
        [property: Description("The fastest single dispatch counted in this row, in microseconds.")]
        [property: ListColumn(Order = 100, Title = "Min", Unit = "µs", Format = GridFormat.Integer)]
        long MinMicroseconds,
        [property: Description("The slowest single dispatch counted in this row, in microseconds.")]
        [property: ListColumn(Order = 110, Title = "Max", Unit = "µs", Format = GridFormat.Integer)]
        long MaxMicroseconds,
        [property:
            Description(
                "UTC timestamp this row was flushed to the database (the end of the one-minute window it covers).")]
        [property: ListColumn(Order = 20)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that recorded this row.")]
        [property: ListColumn(Order = 130)]
        string Instance);
}