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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.Observability;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class WaitReadTasksExtensions
    {
        public static async Task<Context> WaitReadTasksAsync(this Context context)
        {
            context.TraceLog(
                $"is waiting for {context.PendingReadTasks.Count} read tasks of which {context.PendingReadTasks.Count(c => c.IsCompleted)} are completed.");

            var invokeTaskPerformance = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance;
            invokeTaskPerformance.TaskWrapperStats ??= new TaskWrapperStats();
            invokeTaskPerformance.TaskWrapperStats.Read = new ReadTasksPerformance();

            var joinStopwatch = Stopwatch.StartNew();
            var pendingReadTasksResults = await Task.WhenAll(context.PendingReadTasks).ConfigureAwait(false);
            joinStopwatch.Stop();

            foreach (var pendingReadTasksResult in pendingReadTasksResults)
            {
                if (pendingReadTasksResult.Faulted)
                {
                    EngineDiagnostics.InvokeTaskFaultedCount.Add(1,
                        new KeyValuePair<string, object>("task.type", pendingReadTasksResult.TaskType.ToString()),
                        new KeyValuePair<string, object>("task.direction", "read"));
                    continue;
                }

                switch (pendingReadTasksResult.TaskType)
                {
                    case TaskType.SanctionsAsync:
                        invokeTaskPerformance.TaskWrapperStats.Read.SanctionsAsync =
                            new TaskPerformance(pendingReadTasksResult.ComputeTime,
                                pendingReadTasksResult.ThreadMemory);
                        break;
                    case TaskType.TtlCountersAsync:
                        invokeTaskPerformance.TaskWrapperStats.Read.TtlCountersAsync =
                            new TaskPerformance(pendingReadTasksResult.ComputeTime,
                                pendingReadTasksResult.ThreadMemory);
                        break;
                    case TaskType.AbstractionRulesWithSearchKeysAsync:
                        invokeTaskPerformance.TaskWrapperStats.Read.AbstractionRulesWithSearchKeysAsync =
                            new TaskPerformance(pendingReadTasksResult.ComputeTime,
                                pendingReadTasksResult.ThreadMemory);
                        break;
                }
            }

            if (context.LogSampled)
            {
                invokeTaskPerformance.Stages ??= new InvokeStagePerformance();
                invokeTaskPerformance.Stages.JoinReadTasks = new StageDuration
                {
                    DurationMicroseconds = (long)(joinStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency))
                };
            }

            context.TraceLog($"completed {context.PendingReadTasks.Count} read tasks.");

            return context;
        }
    }
}