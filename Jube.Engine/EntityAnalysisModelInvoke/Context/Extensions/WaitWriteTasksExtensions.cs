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
    public static class WaitWriteTasksExtensions
    {
        public static async Task<Context> WaitWriteTasksAsync(this Context context)
        {
            context.TraceLog(
                $"is waiting for {context.PendingWriteTasks.Count} write tasks of which {context.PendingWriteTasks.Count(c => c.IsCompleted)} are completed.");

            var invokeTaskPerformance = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance;
            invokeTaskPerformance.TaskWrapperStats ??= new TaskWrapperStats();
            invokeTaskPerformance.TaskWrapperStats.Write = new WriteTasksPerformance();

            var joinStopwatch = Stopwatch.StartNew();
            var pendingWriteTasksResults = await Task.WhenAll(context.PendingWriteTasks).ConfigureAwait(false);
            joinStopwatch.Stop();

            foreach (var pendingWriteTasksResult in pendingWriteTasksResults)
            {
                if (pendingWriteTasksResult.Faulted)
                {
                    EngineDiagnostics.InvokeTaskFaultedCount.Add(1,
                        new KeyValuePair<string, object>("task.type", pendingWriteTasksResult.TaskType.ToString()),
                        new KeyValuePair<string, object>("task.direction", "write"));
                    continue;
                }

                switch (pendingWriteTasksResult.TaskType)
                {
                    case TaskType.CachePayloadLatestUpsertAsync:
                        invokeTaskPerformance.TaskWrapperStats.Write.CachePayloadLatestUpsertAsync =
                            new TaskPerformance(pendingWriteTasksResult.ComputeTime,
                                pendingWriteTasksResult.ThreadMemory);
                        break;
                    case TaskType.CachePayloadUpsertAsync:
                        invokeTaskPerformance.TaskWrapperStats.Write.CachePayloadUpsertAsync =
                            new TaskPerformance(pendingWriteTasksResult.ComputeTime,
                                pendingWriteTasksResult.ThreadMemory);
                        break;
                    case TaskType.CachePayloadInsertAsync:
                        invokeTaskPerformance.TaskWrapperStats.Write.CachePayloadInsertAsync =
                            new TaskPerformance(pendingWriteTasksResult.ComputeTime,
                                pendingWriteTasksResult.ThreadMemory);
                        break;
                    case TaskType.CacheTtlCounterEntryUpsertAsync:
                        invokeTaskPerformance.TaskWrapperStats.Write.CacheTtlCounterEntryUpsertAsync =
                            new TaskPerformance(pendingWriteTasksResult.ComputeTime,
                                pendingWriteTasksResult.ThreadMemory);
                        break;
                    case TaskType.CacheTtlCounterEntryIncrementAsync:
                        invokeTaskPerformance.TaskWrapperStats.Write.CacheTtlCounterEntryIncrementAsync =
                            new TaskPerformance(pendingWriteTasksResult.ComputeTime,
                                pendingWriteTasksResult.ThreadMemory);
                        break;
                    case TaskType.CacheSanctionInsertAsync:
                        invokeTaskPerformance.TaskWrapperStats.Write.CacheSanctionInsertAsync =
                            new TaskPerformance(pendingWriteTasksResult.ComputeTime,
                                pendingWriteTasksResult.ThreadMemory);
                        break;
                    case TaskType.UpsertReferenceDateAsync:
                        invokeTaskPerformance.TaskWrapperStats.Write.UpsertReferenceDateAsync =
                            new TaskPerformance(pendingWriteTasksResult.ComputeTime,
                                pendingWriteTasksResult.ThreadMemory);
                        break;
                }
            }

            if (context.LogSampled)
            {
                invokeTaskPerformance.Stages ??= new InvokeStagePerformance();
                invokeTaskPerformance.Stages.JoinWriteTasks = new StageDuration
                {
                    DurationMicroseconds = (long)(joinStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency))
                };
            }

            context.TraceLog($"completed {context.PendingWriteTasks.Count} write tasks.");

            return context;
        }
    }
}