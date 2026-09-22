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
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using EntityAnalysisModelAbstractionCalculation =
        EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionCalculation;

    public static class AbstractionCalculationsExtensions
    {
        public static Context ExecuteAbstractionCalculations(this Context context)
        {
            context.TraceLog(
                $"will now perform entity analysis abstractions calculations and will loop through each.");

            var stopwatch = Stopwatch.StartNew();
            var items = new Dictionary<string, TaskPerformance>();

            IterateAndProcess(context, items);

            stopwatch.Stop();

            if (context.LogSampled)
            {
                var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                    new InvokeStagePerformance();

                stages.AbstractionCalculations = new StageTiming<TaskPerformance>
                {
                    DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                    Items = items
                };
            }

            context.TraceLog($"Abstraction Calculations have concluded.");

            return context;
        }

        private static void IterateAndProcess(Context context, Dictionary<string, TaskPerformance> items)
        {
            foreach (var entityAnalysisModelAbstractionCalculation in context.EntityAnalysisModel.Collections
                         .EntityAnalysisModelAbstractionCalculations)
            {
                context.TraceLog(
                    $"evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id}.");

                var itemStopwatch = Stopwatch.StartNew();
                var startBytes = GC.GetAllocatedBytesForCurrentThread();
                try
                {
                    var calculationDouble = CalculationFromRule(context, entityAnalysisModelAbstractionCalculation);

                    context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation.Add(
                        entityAnalysisModelAbstractionCalculation.Name, calculationDouble);

                    var calculationDoubleForLog = calculationDouble;

                    context.TraceLog(
                        $"evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has added the name {entityAnalysisModelAbstractionCalculation.Name} with the value {calculationDoubleForLog} to abstractions for processing.");

                    if (entityAnalysisModelAbstractionCalculation.ReportTable)
                    {
                        context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                        {
                            ProcessingTypeId = 6,
                            Key = entityAnalysisModelAbstractionCalculation.Name,
                            KeyValueFloat = calculationDouble,
                            EntityAnalysisModelInstanceEntryGuid =
                                context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                        });

                        context.TraceLog(
                            $"evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has added the name {entityAnalysisModelAbstractionCalculation.Name} with the value {calculationDoubleForLog} to report payload also with a column name of {entityAnalysisModelAbstractionCalculation.Name}.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} has produced an error as {ex}.");
                }
                finally
                {
                    itemStopwatch.Stop();
                    var allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
                    items[entityAnalysisModelAbstractionCalculation.Name] = new TaskPerformance(
                        (long)(itemStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                        Math.Max(allocated, 0));
                }
            }
        }

        private static double CalculationFromRule(Context context,
            EntityAnalysisModelAbstractionCalculation entityAnalysisModelAbstractionCalculation)
        {
            var calculationDouble = ReflectRuleHelper.Execute(entityAnalysisModelAbstractionCalculation,
                context.EntityAnalysisModel,
                context.EntityAnalysisModelInstanceEntryPayload,
                context.EntityAnalysisModelInstanceEntryPayload.Dictionary, context.Log);

            return calculationDouble;
        }
    }
}
