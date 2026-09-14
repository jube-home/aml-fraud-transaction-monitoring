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
using System.Linq;
using System.Threading.Tasks;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class InlineScriptsExtensions
    {
        public static async Task<Context> ExecuteInlineScriptsAsync(this Context context)
        {
            context.TraceLog($"is going to execute inline scripts.");

            var stopwatch = Stopwatch.StartNew();
            var items = new Dictionary<string, TaskPerformance>();

            await IterateAndProcessAsync(context, items);

            stopwatch.Stop();

            if (context.LogSampled)
            {
                var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                    new InvokeStagePerformance();

                stages.InlineScripts = new StageTiming<TaskPerformance>
                {
                    DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                    Items = items
                };
            }

            context.EntityAnalysisModelInstanceEntryPayload.JObject = null;

            context.TraceLog(
                $"Payload JObject has been set to null in context to free up for GC. Payload JObject is no longer available as it may only be used in [ResponsePayload] attribute decoration.");

            return context;
        }

        private static async Task IterateAndProcessAsync(Context context, Dictionary<string, TaskPerformance> items)
        {
            foreach (var inlineScript in
                     context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Where(s => s
                         .EntityAnalysisModelInlineScriptEvents
                         .Any(e => e.EntityAnalysisModelInlineScriptEventType ==
                                   EntityAnalysisModelInlineScriptEventTypeEnum.Payload)))
            {
                try
                {
                    if (context.Log.IsDebugEnabled)
                    {
                        context.Log.Debug(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is going to invoke {inlineScript.InlineScriptCode}.");
                    }

                    context.TraceLog($"is going to invoke inline script {inlineScript.InlineScriptCode}.");

                    var timed = await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.InlineScript,
                            async () => await ReflectInlineScriptHelper.ExecuteAsync(inlineScript, context))
                        .ConfigureAwait(false);
                    items[inlineScript.InlineScriptCode] = new TaskPerformance(timed.ComputeTime, timed.ThreadMemory);

                    context.TraceLog($"has invoked inline script {inlineScript.InlineScriptCode}.");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has tried to invoke inline script {inlineScript.InlineScriptCode} but it has produced an error as {ex}.");
                }
            }
        }
    }
}