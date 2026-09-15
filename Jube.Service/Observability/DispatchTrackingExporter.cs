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

using System.Diagnostics;
using log4net;
using OpenTelemetry;

namespace Jube.Service.Observability
{
    public sealed class DispatchTrackingExporter<T>(BaseExporter<T> inner, string signal, ILog log)
        : BaseExporter<T>
        where T : class
    {
        public override ExportResult Export(in Batch<T> batch)
        {
            var itemCount = batch.Count;
            var stopwatch = Stopwatch.StartNew();
            ExportResult result;

            try
            {
                result = inner.Export(batch);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"DispatchTrackingExporter: {signal} export threw and is being counted as a failure: {ex.Message}");
                }

                Record(false, itemCount, stopwatch.Elapsed.TotalMilliseconds);
                return ExportResult.Failure;
            }

            stopwatch.Stop();
            Record(result == ExportResult.Success, itemCount, stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }

        public override string ToString()
        {
            return signal;
        }

        protected override bool OnForceFlush(int timeoutMilliseconds)
        {
            return inner.ForceFlush(timeoutMilliseconds);
        }

        protected override bool OnShutdown(int timeoutMilliseconds)
        {
            return inner.Shutdown(timeoutMilliseconds);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Record(bool success, long itemCount, double elapsedMilliseconds)
        {
            ServiceDiagnostics.OtlpDispatchCount.Add(1,
                new KeyValuePair<string, object?>("signal", signal),
                new KeyValuePair<string, object?>("outcome", success ? "success" : "failure"));
            ServiceDiagnostics.OtlpDispatchDuration.Record(elapsedMilliseconds,
                new KeyValuePair<string, object?>("signal", signal));

            OtlpDispatchCounters.OtlpDispatchCounters.RecordDispatch(signal, success, itemCount, elapsedMilliseconds);
        }
    }
}