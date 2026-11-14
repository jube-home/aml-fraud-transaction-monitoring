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
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.GraphRecall;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class GraphRecallExtensions
    {
        public static Task<Context> ExecuteGraphRecallAsync(this Context context)
        {
            var stopwatch = Stopwatch.StartNew();
            var startBytes = GC.GetAllocatedBytesForCurrentThread();

            try
            {
                var plan = context.Snapshot.GraphRecall;
                var payload = context.EntityAnalysisModelInstanceEntryPayload;

                GraphRecallSnapshot snapshot = null;

                if (RecallEnabled(context))
                {
                    var cache = context.EntityAnalysisModel.Dependencies.GraphRecall;

                    if (!cache.TryGet(context.EntityAnalysisModel.Instance.TenantRegistryId, out snapshot))
                    {
                        context.TraceLog(
                            $"has no graph recall snapshot for the tenant{(cache.LoadedForStartup ? string.Empty : " because the loader has not completed its first cycle")}, so the typed defaults stand.");
                    }
                }

                var values = new string[plan.Entities.Count];

                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = payload.Payload.TryGetValue(plan.Entities[i].Field, out var value)
                        ? value.AsString()
                        : null;
                }

                lock (payload.Graph)
                {
                    GraphRecallEvaluator.Evaluate(plan, snapshot, values, payload.ReferenceDate, payload.Graph);
                }

                context.TraceLog(
                    $"has evaluated {plan.Entities.Count} graph recall entities with {(snapshot == null ? "no snapshot" : $"a snapshot of {snapshot.NodeCount} nodes")}.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Log.Error(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has caused an error in Graph recall as {ex}.");
            }
            finally
            {
                stopwatch.Stop();

                if (context.LogSampled)
                {
                    var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                        new InvokeStagePerformance();

                    var microseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));

                    stages.Graph = new StageTiming<TaskPerformance>
                    {
                        DurationMicroseconds = microseconds,
                        Items = new Dictionary<string, TaskPerformance>
                        {
                            ["Graph"] = new(microseconds,
                                Math.Max(GC.GetAllocatedBytesForCurrentThread() - startBytes, 0))
                        }
                    };
                }
            }

            return Task.FromResult(context);
        }

        private static bool RecallEnabled(Context context)
        {
            var environment = context.Environment ?? context.EntityAnalysisModel.Services.JubeEnvironment;

            return environment != null && string.Equals(environment.AppSettings("EnableGraphRecall"), "True",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
