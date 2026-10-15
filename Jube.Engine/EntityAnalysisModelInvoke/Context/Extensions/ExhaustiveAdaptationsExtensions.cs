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
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.Exhaustive.Models;
using Jube.Engine.Exhaustive.Utilities;
using Jube.HttpAdaptationProtocol;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using ExhaustiveSearchInstance = ExhaustiveSearchInstance;

    public static class ExhaustiveAdaptationsExtensions
    {
        internal const int DefaultBudgetMilliseconds = 1000;

        public static Context ExecuteExhaustiveAdaptation(this Context context)
        {
            return ExecuteExhaustiveAdaptation(context, ResolveBudgetTicks(context));
        }

        internal static Context ExecuteExhaustiveAdaptation(Context context, long budgetTicks)
        {
            context.TraceLog($"will now perform Exhaustive and will loop through each.");

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

            stages.ExhaustiveAdaptation = new StageTiming<TaskPerformance>
            {
                DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                Items = items
            };

            return context;
        }

        internal static long ResolveBudgetTicks(Context context)
        {
            var milliseconds = DefaultBudgetMilliseconds;
            var configured = context.Environment?.AppSettings("ExhaustiveRecallTimeout");

            if (configured != null && int.TryParse(configured, out var parsed))
            {
                milliseconds = parsed;
            }

            return milliseconds <= 0 ? long.MaxValue : (long)(milliseconds * (Stopwatch.Frequency / 1000.0));
        }

        private static void IterateAndProcess(Context context, Dictionary<string, TaskPerformance> items,
            Stopwatch stageStopwatch, long budgetTicks)
        {
            foreach (var exhaustive in context.Snapshot.ExhaustiveModels)
            {
                if (stageStopwatch.ElapsedTicks > budgetTicks)
                {
                    context.TraceLog(
                        $"has exceeded the Exhaustive recall budget and will skip Exhaustive Search Instance Id {exhaustive.Id}.");

                    continue;
                }

                if ((exhaustive.TopologyNetwork == null && exhaustive.ChallengerRecall == null)
                    || exhaustive.NetworkVariablesInOrder.Count == 0
                    || exhaustive.Calibration.IsInvalid
                    || (exhaustive.TopologyNetwork != null
                        && exhaustive.TopologyNetwork.InputsCount != exhaustive.NetworkVariablesInOrder.Count))
                {
                    context.TraceLog(
                        $"is incomplete or invalid and will not be scored for Exhaustive Search Instance Id {exhaustive.Id}.");

                    items[exhaustive.Name] = new TaskPerformance(0, 0);
                    continue;
                }

                context.TraceLog($"evaluating Exhaustive Search Instance Id {exhaustive.Id}.");

                var itemStopwatch = Stopwatch.StartNew();
                var startBytes = GC.GetAllocatedBytesForCurrentThread();

                try
                {
                    var data = new double[exhaustive.NetworkVariablesInOrder.Count];

                    for (var i = 0; i < exhaustive.NetworkVariablesInOrder.Count; i++)
                    {
                        if (stageStopwatch.ElapsedTicks > budgetTicks)
                        {
                            throw new TimeoutException(
                                $"Exhaustive recall budget exceeded while extracting variable {i} of {exhaustive.NetworkVariablesInOrder.Count}.");
                        }

                        if (exhaustive.TopologyNetwork != null)
                        {
                            ExtractValueGivenProcessingTypeAndUpdateArray(context, exhaustive, i, data);
                        }
                        else
                        {
                            ExtractRawValueAndUpdateArray(context, exhaustive, i, data);
                        }
                    }

                    context.TraceLog($"is about to recall model with {data.Length} variables.");

                    double value;
                    double rawScore;

                    if (exhaustive.TopologyNetwork != null)
                    {
                        rawScore = exhaustive.TopologyNetwork.Compute(data)[0];
                        value = exhaustive.Calibration.Apply(rawScore);
                    }
                    else
                    {
                        rawScore = exhaustive.ChallengerRecall.Predict(data)[0];
                        value = rawScore;
                    }

                    context.TraceLog(
                        $"has recalled a score of {value}. Will proceed to add the value to payload collection.");

                    context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.Add(exhaustive.Name, value);

                    try
                    {
                        var detail = exhaustive.TopologyNetwork != null
                            ? BuildAdaptationDetail(exhaustive, data, value)
                            : BuildChallengerAdaptationDetail(exhaustive, data, value);

                        if (detail != null)
                        {
                            context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptationDetail.Add(
                                exhaustive.Name, detail);
                        }
                    }
                    catch (Exception ex)
                    {
                        context.TraceLog($"could not build the Exhaustive explainability detail: {ex}.");

                        context.Log?.Warn(
                            $"Exhaustive Adaptation: GUID {exhaustive.Guid} ({exhaustive.Name}) recalled a score " +
                            $"but the explainability detail could not be built as {ex}");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.TraceLog($"has exception {ex}.");

                    context.Log?.Error(
                        $"Exhaustive Adaptation: GUID {exhaustive.Guid} ({exhaustive.Name}) could not be scored in the pipeline as {ex}");
                }
                finally
                {
                    itemStopwatch.Stop();
                    var allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;

                    items[exhaustive.Name] = new TaskPerformance(
                        (long)(itemStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                        Math.Max(allocated, 0));
                }

                context.TraceLog($"has concluded exhaustive recall.");
            }
        }

        private static Adaptation BuildAdaptationDetail(ExhaustiveSearchInstance exhaustive, double[] data,
            double value)
        {
            var names = exhaustive.NetworkVariablesInOrder.Select(v => v.Name).ToArray();
            var relevance = RecallExplainability.Explain(exhaustive.TopologyNetwork, data, names);
            var processingTypeIds = exhaustive.NetworkVariablesInOrder.Select(v => v.ProcessingTypeId).ToList();

            return relevance.ToAdaptation(value, exhaustive.Name, exhaustive.TopologyNetwork.Layers.Length - 1,
                exhaustive.TopologyNetwork.Layers.Sum(l => l.Neurons.Length), exhaustive.Calibration,
                processingTypeIds);
        }

        private static Adaptation BuildChallengerAdaptationDetail(ExhaustiveSearchInstance exhaustive, double[] data,
            double value)
        {
            var explanation = exhaustive.ChallengerRecall.Explain?.Invoke(data);

            if (explanation == null)
            {
                return null;
            }

            var processingTypeIds = exhaustive.NetworkVariablesInOrder.Select(v => v.ProcessingTypeId).ToList();

            return explanation.ToAdaptation(value, exhaustive.Name, processingTypeIds);
        }

        private static void ExtractRawValueAndUpdateArray(Context context, ExhaustiveSearchInstance exhaustive,
            int i, double[] data)
        {
            var variable = exhaustive.NetworkVariablesInOrder[i];

            if (variable.IsCategorical)
            {
                var sourceName = AdaptationInputExtraction.CleanName(variable.CategoricalSourceName);
                string raw = null;

                if (context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(sourceName,
                        out var payloadValue))
                {
                    raw = CategoricalValue.FromInternalValue(payloadValue);
                }

                data[i] = variable.CategoricalEncoder.Indicator(raw, variable.CategoricalIndicatorIndex);

                context.TraceLog(
                    $"evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" categorical {sourceName} indicator {variable.Name} value {data[i]}.");

                return;
            }

            var cleanName = AdaptationInputExtraction.CleanName(variable.Name);

            context.TraceLog(
                $"evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                $" Will look up {cleanName} for processing type id {variable.ProcessingTypeId}.");

            if (AdaptationInputExtraction.TryGetValue(context, variable.ProcessingTypeId, cleanName, out var value))
            {
                data[i] = value;

                context.TraceLog(
                    $"evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" {cleanName} found value {data[i]}.");
            }
            else
            {
                data[i] = variable.Mean;

                context.TraceLog(
                    $"evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" {cleanName} fall back value {data[i]}.");
            }
        }

        private static void ExtractValueGivenProcessingTypeAndUpdateArray(Context context,
            ExhaustiveSearchInstance exhaustive, int i, double[] data)
        {
            var variable = exhaustive.NetworkVariablesInOrder[i];

            if (variable.IsCategorical)
            {
                var sourceName = AdaptationInputExtraction.CleanName(variable.CategoricalSourceName);
                string raw = null;

                if (context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(sourceName,
                        out var payloadValue))
                {
                    raw = CategoricalValue.FromInternalValue(payloadValue);
                }

                data[i] = variable.ZScore(
                    variable.CategoricalEncoder.Indicator(raw, variable.CategoricalIndicatorIndex));

                context.TraceLog(
                    $"evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" categorical {sourceName} indicator {variable.Name} value {data[i]}.");

                return;
            }

            var cleanName = AdaptationInputExtraction.CleanName(variable.Name);

            context.TraceLog(
                $"evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                $"  Will look up {cleanName} for processing type id {variable.ProcessingTypeId}.");

            if (AdaptationInputExtraction.TryGetValue(context, variable.ProcessingTypeId, cleanName, out var value))
            {
                data[i] = variable.ZScore(value);

                context.TraceLog(
                    $"evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" {cleanName} found value {data[i]}.");
            }
            else
            {
                data[i] = variable.ZScore(variable.Mean);

                context.TraceLog(
                    $"evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" {cleanName} fall back value {data[i]}.");
            }
        }
    }
}