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
using Jube.Engine.AdvancedAnalytics;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.HttpAdaptationProtocol;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class AdvancedAnalyticsAdaptationExtensions
    {
        public static Context ExecuteAdvancedAnalyticsAdaptation(this Context context)
        {
            return ExecuteAdvancedAnalyticsAdaptation(context, ResolveBudgetTicks(context));
        }

        internal static Context ExecuteAdvancedAnalyticsAdaptation(Context context, long budgetTicks)
        {
            context.TraceLog($"will now perform Advanced Analytics and will loop through each.");

            var stopwatch = Stopwatch.StartNew();
            var items = new Dictionary<string, TaskPerformance>();

            IterateAndProcess(context, items, stopwatch, budgetTicks);

            stopwatch.Stop();

            if (!context.LogSampled)
            {
                return context;
            }

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                new InvokeStagePerformance();

            stages.AdvancedAnalyticsAdaptation = new StageTiming<TaskPerformance>
            {
                DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                Items = items
            };

            return context;
        }

        internal static long ResolveBudgetTicks(Context context)
        {
            Func<string, string> appSettings = null;

            if (context.Environment != null)
            {
                appSettings = key => context.Environment.AppSettings(key);
            }

            var milliseconds = AdvancedAnalyticsSettings.ResolveStageTimeBudgetMilliseconds(appSettings);

            return milliseconds <= 0 ? long.MaxValue : (long)(milliseconds * (Stopwatch.Frequency / 1000.0));
        }

        private static void IterateAndProcess(Context context, Dictionary<string, TaskPerformance> items,
            Stopwatch stageStopwatch, long budgetTicks)
        {
            var models = context.EntityAnalysisModel.Snapshot.AdvancedAnalyticsModels;

            foreach (var model in models)
            {
                if (stageStopwatch.ElapsedTicks > budgetTicks)
                {
                    context.TraceLog(
                        $"has exceeded the Advanced Analytics budget and will skip model Id {model.Id}.");

                    continue;
                }

                context.TraceLog($"evaluating Advanced Analytics model Id {model.Id}.");

                var itemStopwatch = Stopwatch.StartNew();
                var startBytes = GC.GetAllocatedBytesForCurrentThread();

                try
                {
                    var data = new double[model.VariablesInOrder.Count];

                    for (var i = 0; i < model.VariablesInOrder.Count; i++)
                    {
                        if (stageStopwatch.ElapsedTicks > budgetTicks)
                        {
                            throw new TimeoutException(
                                $"Advanced Analytics budget exceeded while extracting variable {i} of {model.VariablesInOrder.Count}.");
                        }

                        ExtractValueAndUpdateArray(context, model, i, data);
                    }

                    context.TraceLog($"is about to recall model with {data.Length} variables.");

                    var value = model.Recall(data)[0];

                    context.TraceLog(
                        $"has recalled a probability of {value}. Will proceed to add the value to payload collection.");

                    context.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation[model.Name] = value;

                    try
                    {
                        var detail = BuildAdaptationDetail(model, data, value);

                        if (detail != null)
                        {
                            context.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptationDetail[
                                model.Name] = detail;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.TraceLog($"could not build the Advanced Analytics explainability detail: {ex}.");

                        context.Log?.Warn(
                            $"Advanced Analytics Adaptation: GUID {model.Guid} ({model.Name}) recalled a value " +
                            $"but the explainability detail could not be built as {ex}");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.TraceLog($"has exception {ex}.");

                    context.Log?.Error(
                        $"Advanced Analytics Adaptation: GUID {model.Guid} ({model.Name}) could not be scored in the pipeline as {ex}");
                }
                finally
                {
                    itemStopwatch.Stop();
                    var allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;

                    items[model.Name] = new TaskPerformance(
                        (long)(itemStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                        Math.Max(allocated, 0));
                }

                context.TraceLog($"has concluded Advanced Analytics recall.");
            }
        }

        private static Adaptation BuildAdaptationDetail(AdvancedAnalyticsModel model, double[] data, double value)
        {
            var explanation = model.Explain?.Invoke(data);

            if (explanation == null)
            {
                return null;
            }

            var processingTypeIds = model.VariablesInOrder.Select(v => v.ProcessingTypeId).ToList();

            return explanation.ToAdaptation(value, model.Name, processingTypeIds);
        }

        private static void ExtractValueAndUpdateArray(Context context, AdvancedAnalyticsModel model, int i,
            double[] data)
        {
            var variable = model.VariablesInOrder[i];

            var sourceName = VariableTransformationApplier.SourceNameOverride(variable.TransformationParametersJson)
                             ?? variable.Name;

            var cleanName = AdaptationInputExtraction.CleanName(sourceName);

            context.TraceLog(
                $"evaluating Advanced Analytics model Id {model.Id}." +
                $" Will look up {cleanName} for processing type id {variable.ProcessingTypeId}.");

            if (VariableTransformationApplier.IsCategorical(variable.TransformationId))
            {
                if (AdaptationInputExtraction.TryGetText(context, variable.ProcessingTypeId, cleanName,
                        out var text))
                {
                    data[i] = VariableTransformationApplier.ApplyCategorical(variable.TransformationId,
                        variable.TransformationParametersJson, text);

                    context.TraceLog(
                        $"evaluating Advanced Analytics model Id {model.Id}. {cleanName} found value {text}.");
                }
                else
                {
                    data[i] = VariableTransformationApplier.ApplyCategorical(variable.TransformationId,
                        variable.TransformationParametersJson, null);

                    context.TraceLog(
                        $"evaluating Advanced Analytics model Id {model.Id}. {cleanName} is missing and is passed as unseen.");
                }

                return;
            }

            if (AdaptationInputExtraction.TryGetValue(context, variable.ProcessingTypeId, cleanName, out var value))
            {
                data[i] = VariableTransformationApplier.ApplyNumeric(variable.TransformationId,
                    variable.TransformationParametersJson, value);

                context.TraceLog(
                    $"evaluating Advanced Analytics model Id {model.Id}. {cleanName} found value {value}.");
            }
            else
            {
                data[i] = VariableTransformationApplier.ApplyNumeric(variable.TransformationId,
                    variable.TransformationParametersJson, double.NaN);

                context.TraceLog(
                    $"evaluating Advanced Analytics model Id {model.Id}. {cleanName} is missing and is passed as NaN.");
            }
        }
    }
}