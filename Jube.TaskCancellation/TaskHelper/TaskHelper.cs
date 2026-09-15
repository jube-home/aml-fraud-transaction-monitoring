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

namespace Jube.TaskCancellation.TaskHelper
{
    public static class TaskHelper
    {
        [ThreadStatic] private static long lastSeenBytes;

        public static Task<TimedTaskResult> MeasureTaskTimeAndMemoryAllocatedAsync(TaskType taskType,
            Func<Task> taskFunc, ILog? log = null, bool rethrowOnFault = false)
        {
            return Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                var startBytes = SafeGetAllocatedBytes();

                if (log == null)
                {
                    await taskFunc().ConfigureAwait(false);
                    return BuildResult(taskType, sw, startBytes, false);
                }

                try
                {
                    await taskFunc().ConfigureAwait(false);
                    return BuildResult(taskType, sw, startBytes, false);
                }
                catch (Exception ex)
                {
                    log.Error($"TaskHelper: {taskType} faulted after {sw.Elapsed.TotalMilliseconds:F1}ms, as {ex}.");

                    if (rethrowOnFault)
                    {
                        throw;
                    }

                    return BuildResult(taskType, sw, startBytes, true);
                }
            });
        }

        public static TimedTaskResult MeasureTimeAndMemoryAllocated(TaskType taskType, Action action, ILog? log = null)
        {
            var sw = Stopwatch.StartNew();
            var startBytes = SafeGetAllocatedBytes();

            if (log == null)
            {
                action();
                return BuildResult(taskType, sw, startBytes, false);
            }

            try
            {
                action();
                return BuildResult(taskType, sw, startBytes, false);
            }
            catch (Exception ex)
            {
                log.Error($"TaskHelper: {taskType} faulted after {sw.Elapsed.TotalMilliseconds:F1}ms, as {ex}.");
                return BuildResult(taskType, sw, startBytes, true);
            }
        }

        private static TimedTaskResult BuildResult(TaskType taskType, Stopwatch sw, long startBytes, bool faulted)
        {
            var endBytes = SafeGetAllocatedBytes();
            sw.Stop();

            var bytesAllocated = endBytes - startBytes;
            if (bytesAllocated < 0)
            {
                bytesAllocated = endBytes;
            }

            var elapsedMicroseconds = (long)(sw.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
            return new TimedTaskResult(taskType, elapsedMicroseconds, bytesAllocated, faulted);
        }

        private static long SafeGetAllocatedBytes()
        {
            var current = GC.GetAllocatedBytesForCurrentThread();

            if (current < lastSeenBytes)
            {
                lastSeenBytes = current + (lastSeenBytes - current);
            }
            else
            {
                lastSeenBytes = current;
            }

            return lastSeenBytes;
        }
    }
}