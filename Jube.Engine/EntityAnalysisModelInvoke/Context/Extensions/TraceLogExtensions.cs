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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Logs;
using Jube.Engine.Observability;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class TraceLogExtensions
    {
        private const int DefaultWarnThresholdMilliseconds = 250;

        internal static bool WantsTraceLog(Context context)
        {
            var flags = context.EntityAnalysisModel.Flags;
            return context.Log.IsInfoEnabled ||
                   (flags.EnableLogs &&
                    ((flags.EnableLogsInfo && context.LogSampled) || flags.EnableLogsWarnThreshold));
        }

        public static void TraceLog(this Context context,
            [InterpolatedStringHandlerArgument("context")]
            ref TraceLogInterpolatedStringHandler message)
        {
            if (!message.IsEnabled)
            {
                return;
            }

            var flags = context.EntityAnalysisModel.Flags;
            var wantsLog4Net = context.Log.IsInfoEnabled;
            var wantsInfoSampling = flags.EnableLogs && flags.EnableLogsInfo && context.LogSampled;
            var wantsWarnThresholdCheck = flags.EnableLogs && flags.EnableLogsWarnThreshold;

            var elapsedMicroseconds = (long)(context.Stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));

            var thresholdMicroseconds =
                (flags.LogsWarnThresholdMilliseconds ?? DefaultWarnThresholdMilliseconds) * 1000L;

            var capturedByInfoSampling = wantsInfoSampling;
            long sinceLastEntryMicroseconds;
            bool capturedByWarnThreshold;

            if (capturedByInfoSampling)
            {
                var previousElapsedMicroseconds =
                    Interlocked.Exchange(ref context.LastLogEntryElapsedMicroseconds, elapsedMicroseconds);
                sinceLastEntryMicroseconds = elapsedMicroseconds - previousElapsedMicroseconds;
                capturedByWarnThreshold =
                    wantsWarnThresholdCheck && sinceLastEntryMicroseconds >= thresholdMicroseconds;
            }
            else if (wantsWarnThresholdCheck)
            {
                capturedByWarnThreshold = TryClaimWarnThresholdGap(context, elapsedMicroseconds,
                    thresholdMicroseconds, out sinceLastEntryMicroseconds);
            }
            else
            {
                sinceLastEntryMicroseconds =
                    elapsedMicroseconds - Volatile.Read(ref context.LastLogEntryElapsedMicroseconds);
                capturedByWarnThreshold = false;
            }

            var wantsPayloadCapture = capturedByInfoSampling || capturedByWarnThreshold;

            if (!wantsLog4Net && !wantsPayloadCapture)
            {
                return;
            }

            var messageText = message.GetFormattedTextAndClear();

            if (wantsLog4Net)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} {messageText}");
            }

            if (wantsPayloadCapture)
            {
                var log = context.EntityAnalysisModelInstanceEntryPayload.Logs ??=
                    new InvocationLog { AnchorDate = context.EntityAnalysisModelInstanceEntryPayload.CreatedDate };

                log.AddEntry(new InvocationLogEntry
                {
                    ElapsedMicroseconds = elapsedMicroseconds,
                    SinceLastEntryMicroseconds = sinceLastEntryMicroseconds,
                    CapturedByInfoSampling = capturedByInfoSampling,
                    CapturedByWarnThreshold = capturedByWarnThreshold,
                    ThreadId = Environment.CurrentManagedThreadId,
                    Message = messageText
                });

                var modelTag = new KeyValuePair<string, object>("model", context.EntityAnalysisModel.Instance.Name);

                if (capturedByInfoSampling)
                {
                    EngineDiagnostics.LogsInfoSampledCount.Add(1, modelTag);
                }

                if (capturedByWarnThreshold)
                {
                    EngineDiagnostics.LogsWarnThresholdExceeded.Add(1, modelTag);

                    ModelInvokeWarningCapture.Enqueue(new ModelInvokeWarningCaptureRecord(
                        DateTime.UtcNow,
                        context.EntityAnalysisModel.Instance.Guid,
                        context.EntityAnalysisModel.Instance.Name,
                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                        messageText,
                        elapsedMicroseconds,
                        sinceLastEntryMicroseconds,
                        Environment.CurrentManagedThreadId));
                }
            }
        }

        private static bool TryClaimWarnThresholdGap(Context context, long elapsedMicroseconds,
            long thresholdMicroseconds, out long sinceLastEntryMicroseconds)
        {
            while (true)
            {
                var previous = Volatile.Read(ref context.LastLogEntryElapsedMicroseconds);
                var gap = elapsedMicroseconds - previous;
                if (gap < thresholdMicroseconds)
                {
                    sinceLastEntryMicroseconds = gap;
                    return false;
                }

                if (Interlocked.CompareExchange(ref context.LastLogEntryElapsedMicroseconds, elapsedMicroseconds,
                        previous) == previous)
                {
                    sinceLastEntryMicroseconds = gap;
                    return true;
                }
            }
        }
    }
}