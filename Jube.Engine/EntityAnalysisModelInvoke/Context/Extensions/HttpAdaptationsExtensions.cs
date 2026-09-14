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
using System.Threading.Tasks;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.HttpAdaptations;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class HttpAdaptationsExtensions
    {
        public static async Task<Context> ExecuteHttpAdaptationsAsync(this Context context)
        {
            context.TraceLog($"will begin processing adaptations.");

            var stopwatch = Stopwatch.StartNew();
            var items = new Dictionary<string, TaskPerformance>();

            await IterateAndProcessAsync(context, items).ConfigureAwait(false);

            stopwatch.Stop();

            if (context.LogSampled)
            {
                var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                    new InvokeStagePerformance();

                stages.HttpAdaptation = new StageTiming<TaskPerformance>
                {
                    DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                    Items = items
                };
            }

            context.TraceLog($"adaptations have concluded.");

            return context;
        }

        private static async Task IterateAndProcessAsync(Context context, Dictionary<string, TaskPerformance> items)
        {
            foreach (var modelAdaptation in context.EntityAnalysisModel.Collections.EntityAnalysisModelAdaptations)
            {
                try
                {
                    context.TraceLog(
                        $"is evaluating {modelAdaptation.Id} and is about to serialise the Entity Analysis Model Instance Entry Payload for the HTTP Adaptation POST.");

                    var timed = await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.HttpAdaptation,
                        async () =>
                        {
                            var adaptation = await context.RecallHttpEndpointAsync(modelAdaptation,
                                context.EntityAnalysisModel.JsonSerializationHelper
                                    .DefaultJsonSerializerSettingsSettings).ConfigureAwait(false);

                            context.EntityAnalysisModelInstanceEntryPayload.HttpAdaptation[modelAdaptation.Name] =
                                adaptation;

                            if (adaptation.IsSuppressed)
                            {
                                context.TraceLog(
                                    $"is evaluating {modelAdaptation.Id} and the HTTP Adaptation response for {modelAdaptation.Name} is suppressed (Error: {adaptation.Error ?? "none"}); value will read as null to any rule evaluating it.");
                            }

                            context.ArchiveHttpAdaptation(modelAdaptation);
                        }).ConfigureAwait(false);
                    items[modelAdaptation.Name] = new TaskPerformance(timed.ComputeTime, timed.ThreadMemory);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.TraceLog($"is evaluating {modelAdaptation.Id} and produced an error {ex}.");
                }
            }
        }
    }
}