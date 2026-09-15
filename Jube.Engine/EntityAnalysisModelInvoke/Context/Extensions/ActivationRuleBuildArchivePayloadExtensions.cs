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
using System.Diagnostics;
using System.Threading.Tasks;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Archiver;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class ActivationRuleBuildArchivePayloadExtensions
    {
        public static async Task<Context> ActivationRuleBuildArchivePayloadAsync(this Context context)
        {
            var stopwatch = Stopwatch.StartNew();
            context.EntityAnalysisModelInstanceEntryPayload.ArchiveEnqueueDate = DateTime.UtcNow;

            context.TraceLog(
                $"has been selected for sampling or case creation has been specified. " +
                $"Is building the XML payload from the payload created. ArchiveEnqueueDate set to {context.EntityAnalysisModelInstanceEntryPayload.ArchiveEnqueueDate}.");

            CalculateMemoryUsedInThreadForPayload(context);

            context.TraceLog($"a payload has been created for archive.");

            if (context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
            {
                await ArchiverProcessing.CaseCreationAndArchiveStorageAsync(
                        context.EntityAnalysisModelInstanceEntryPayload,
                        context.EntityAnalysisModel,
                        context.EntityAnalysisModel.JsonSerializationHelper,
                        null, null, context.Environment, context.EntityAnalysisModel.Services.CacheService, context.Log)
                    .ConfigureAwait(false);

                context.TraceLog(
                    $"a payload has been added for archive synchronously as it is set for reprocessing.");
            }
            else
            {
                context.EntityAnalysisModel.ConcurrentQueues.PersistToDatabaseAsync.Enqueue(
                    context.EntityAnalysisModelInstanceEntryPayload);

                context.TraceLog($"a payload has been added for archive asynchronously.");
            }

            stopwatch.Stop();

            if (!context.LogSampled)
            {
                return context;
            }

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                new InvokeStagePerformance();
            stages.BuildArchivePayload = new StageDuration
            {
                DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency))
            };

            return context;
        }

        private static void CalculateMemoryUsedInThreadForPayload(Context context)
        {
            if (context.StartBytesUsed.HasValue)
            {
                var currentBytes = GC.GetAllocatedBytesForCurrentThread();
                context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Memory =
                    currentBytes - context.StartBytesUsed.Value;

                context.TraceLog(
                    $"has start bytes of {context.StartBytesUsed} and currentBytes {currentBytes}. " +
                    $"The used bytes is {context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Memory}.");
            }
            else
            {
                context.TraceLog(
                    $"does not have start bytes recorded in the context so can't calculate memory usage.");
            }
        }
    }
}