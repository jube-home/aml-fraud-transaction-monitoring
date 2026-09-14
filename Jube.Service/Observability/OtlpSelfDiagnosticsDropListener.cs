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

using System.Diagnostics.Tracing;
using OpenTelemetry;

namespace Jube.Service.Observability
{
    public sealed class OtlpSelfDiagnosticsDropListener : EventListener
    {
        private const string ExistsDroppedEventName = "ExistsDroppedExportProcessorItems";

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "OpenTelemetry-Sdk")
            {
                EnableEvents(eventSource, EventLevel.Warning);
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName != ExistsDroppedEventName)
            {
                return;
            }

            var payload = eventData.Payload;
            if (payload is not { Count: >= 3 })
            {
                return;
            }

            if (payload[0] is not string exportProcessorName || string.IsNullOrEmpty(exportProcessorName))
            {
                return;
            }

            var signal = exportProcessorName switch
            {
                nameof(BatchActivityExportProcessor) => "traces",
                nameof(BatchLogRecordExportProcessor) => "logs",
                _ => null
            };

            if (signal == null)
            {
                return;
            }

            var droppedCount = payload[2] switch
            {
                long l => l,
                int i => i,
                _ => 0L
            };

            if (droppedCount <= 0)
            {
                return;
            }

            ServiceDiagnostics.OtlpDroppedCount.Add(droppedCount,
                new KeyValuePair<string, object?>("signal", signal));
            OtlpDispatchCounters.OtlpDispatchCounters.RecordDropped(signal, droppedCount);
        }
    }
}