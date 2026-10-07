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
using System.Threading;
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers;
using Jube.Engine.EntityAnalysisModelInvoke.Models.CaseManagement;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using RabbitMQ.Client;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    using EntityAnalysisModel = EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
    using EntityAnalysisModelActivationRule =
        EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelActivationRule;

    public static class IterateActivationRulesExtensions
    {
        public static async
            Task<(int activationRuleCount, CreateCase createCase, int? prevailingActivationRuleId,
                Dictionary<string, ActivationRuleTiming> items)> IterateAndProcessAsync(this Context context,
                CacheService cacheService, IReadOnlyDictionary<int, EntityAnalysisModel> availableModels,
                IModel rabbitMqChannel)
        {
            var prevailingActivationRuleName = string.Empty;
            var responseElevationHighWaterMark = 0d;
            var overrideBinding = context.ActivationRuleGetOverrides();
            var activationRuleCount = 0;
            CreateCase createCase = null;
            int? prevailingActivationRuleId = null;
            var items = new Dictionary<string, ActivationRuleTiming>();

            foreach (var evaluateActivationRule in context.Snapshot.ModelActivationRules)
            {
                var itemStopwatch = Stopwatch.StartNew();
                var itemStartBytes = GC.GetAllocatedBytesForCurrentThread();
                var timing = new ActivationRuleTiming();

                try
                {
                    var overrideKind = evaluateActivationRule.EnableOverride
                        ? overrideBinding.KindFor(evaluateActivationRule.Name)
                        : null;

                    var forced = overrideKind == EntityAnalysisModelOverrideKind.Force;

                    if (!evaluateActivationRule.EnableOverride)
                    {
                        context.TraceLog(
                            $"activation rule {evaluateActivationRule.Id} is not enabled for override, so any override naming it is ignored.");
                    }

                    var suppressed = false;
                    if (overrideKind == EntityAnalysisModelOverrideKind.Suppress)
                    {
                        suppressed = true;

                        context.TraceLog(
                            $"activation rule {evaluateActivationRule.Id} is suppressed by an override.");
                    }

                    if (context.CheckSuppressedResponseElevation())
                    {
                        suppressed = true;

                        context.TraceLog(
                            $"activation rule {evaluateActivationRule.Id} is suppressed because it has exceeded response elevation counter at {context.EntityAnalysisModel.ConcurrentQueues.ResponseElevationEntries.Count}.");
                    }

                    if (!evaluateActivationRule.EnableReprocessing && context
                            .EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId
                            .HasValue)
                    {
                        suppressed = true;

                        context.TraceLog(
                            $"activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level because of reprocessing.");
                    }

                    if (!forced)
                    {
                        var activationSample = evaluateActivationRule.ActivationSample >= context.Random.NextDouble();
                        if (!activationSample)
                        {
                            context.TraceLog(
                                $"has failed in sampling so certain activations will not take place even if there is a match on the activation rule.");

                            continue;
                        }
                    }

                    UpdateEvaluationCount(evaluateActivationRule);

                    context.TraceLog($"has passed sampling and is eligible for activation.");


                    bool matched;
                    if (forced)
                    {
                        matched = true;

                        context.TraceLog(
                            $"activation rule {evaluateActivationRule.Id} is treated as matched by a force override without evaluating the rule.");
                    }
                    else
                    {
                        matched = ReflectRuleHelper.Execute(
                            evaluateActivationRule,
                            context.Snapshot,
                            context.EntityAnalysisModelInstanceEntryPayload,
                            context.EntityAnalysisModelInstanceEntryPayload.Dictionary,
                            context.Log);
                    }

                    var matchedForLog = matched;

                    context.TraceLog(
                        $"has finished testing the activation rule and it has a matched status of {matchedForLog}.");

                    context.TraceLog(
                        $"is checking for Activation Rule Evaluation InlineScripts looking for context Guid:{evaluateActivationRule.Guid} and or Name:{evaluateActivationRule.Name}.");

                    var overrideScriptTimings = new Dictionary<string, TaskPerformance>();
                    foreach (var inlineScript in context.Snapshot
                                 .EntityAnalysisModelInlineScripts.Where(s => s.EntityAnalysisModelInlineScriptEvents
                                     .Any(e => e.EntityAnalysisModelInlineScriptEventType ==
                                               EntityAnalysisModelInlineScriptEventTypeEnum.AbstractionRuleOverride
                                               && (e.Guid == evaluateActivationRule.Guid || e.Guid == Guid.Empty)
                                               && (e.Name == evaluateActivationRule.Name ||
                                                   string.IsNullOrEmpty(e.Name))
                                     )))
                    {
                        context.TraceLog($"is about to execute Inline Script Id: {inlineScript.Id}.");

                        var (scriptMatched, scriptTiming) =
                            await TimeAsync(() => ReflectInlineScriptHelper.ExecuteAsync(inlineScript, context))
                                .ConfigureAwait(false);
                        overrideScriptTimings[inlineScript.InlineScriptCode] = scriptTiming;

                        if (scriptMatched)
                        {
                            context.TraceLog($"matched Inline Script Id: {inlineScript.Id}.");

                            matched = true;
                        }
                        else
                        {
                            context.TraceLog($"did not match Inline Script Id: {inlineScript.Id}.");
                        }
                    }

                    timing.AbstractionRuleOverrideInlineScripts = overrideScriptTimings;

                    if (!matched)
                    {
                        continue;
                    }

                    UpdateActivationCounter(evaluateActivationRule);

                    if (forced)
                    {
                        UpdateForcedActivationCounter(evaluateActivationRule);
                    }

                    if (context.EntityAnalysisModelInstanceEntryPayload.Activation.ContainsKey(evaluateActivationRule
                            .Name))
                    {
                        context.TraceLog(
                            $"and has already added the activation rule {evaluateActivationRule.Id} on {evaluateActivationRule.Name} for processing.");

                        continue;
                    }

                    context.EntityAnalysisModelInstanceEntryPayload.Activation.Add(
                        evaluateActivationRule.Name,
                        new EntityModelActivationRulePayload
                        {
                            Visible = evaluateActivationRule.Visible,
                            Forced = forced
                        });

                    context.TraceLog(
                        $"and has added the activation rule {evaluateActivationRule.Id} flag on {evaluateActivationRule.Name} to the activation buffer for processing.");

                    if (evaluateActivationRule.ReportTable)
                    {
                        context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                        {
                            ProcessingTypeId = 11,
                            Key = evaluateActivationRule.Name,
                            KeyValueBoolean = 1,
                            EntityAnalysisModelInstanceEntryGuid =
                                context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                        });

                        context.TraceLog(
                            $"and has added the activation rule {evaluateActivationRule.Id} flag to the response payload also.");
                    }

                    timing.ResponseElevation = Time(() =>
                        context.ProcessResponseElevation(evaluateActivationRule, ref responseElevationHighWaterMark,
                            suppressed));

                    timing.Notification = await TimeAsync(() =>
                        context.ActivationRuleNotificationAsync(evaluateActivationRule, suppressed, rabbitMqChannel,
                            cacheService)).ConfigureAwait(false);

                    context.ActivationRuleCountsAndArchiveHighWatermark(evaluateActivationRule, suppressed,
                        ref activationRuleCount,
                        ref prevailingActivationRuleId, ref prevailingActivationRuleName);

                    timing.ActivationWatcher = await TimeAsync(() =>
                            context.ActivationRuleActivationWatcherAsync(evaluateActivationRule, suppressed,
                                cacheService))
                        .ConfigureAwait(false);

                    if (createCase == null)
                    {
                        var (caseResult, caseTiming) = await TimeAsync(() =>
                            context.ActivationRuleCreateCaseObjectAsync(evaluateActivationRule, suppressed,
                                cacheService)).ConfigureAwait(false);
                        createCase = caseResult;
                        timing.CaseCreation = caseTiming;
                    }

                    await context.ActivationRuleTtlCounterAsync(evaluateActivationRule, availableModels, cacheService);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} error in TTL Counter processing as {ex} .");
                }
                finally
                {
                    itemStopwatch.Stop();
                    var allocated = GC.GetAllocatedBytesForCurrentThread() - itemStartBytes;
                    timing.Rule = new TaskPerformance(
                        (long)(itemStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                        Math.Max(allocated, 0));
                    items[evaluateActivationRule.Name] = timing;
                }
            }

            return (activationRuleCount, createCase, prevailingActivationRuleId, items);
        }

        private static TaskPerformance Time(Action action)
        {
            var sw = Stopwatch.StartNew();
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            action();
            sw.Stop();
            return new TaskPerformance((long)(sw.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                Math.Max(GC.GetAllocatedBytesForCurrentThread() - startBytes, 0));
        }

        private static async Task<TaskPerformance> TimeAsync(Func<Task> action)
        {
            var sw = Stopwatch.StartNew();
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            await action().ConfigureAwait(false);
            sw.Stop();
            return new TaskPerformance((long)(sw.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                Math.Max(GC.GetAllocatedBytesForCurrentThread() - startBytes, 0));
        }

        private static async Task<(T Result, TaskPerformance Timing)> TimeAsync<T>(Func<Task<T>> action)
        {
            var sw = Stopwatch.StartNew();
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            var result = await action().ConfigureAwait(false);
            sw.Stop();
            return (result, new TaskPerformance((long)(sw.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                Math.Max(GC.GetAllocatedBytesForCurrentThread() - startBytes, 0)));
        }

        private static void UpdateForcedActivationCounter(EntityAnalysisModelActivationRule evaluateActivationRule)
        {
            Interlocked.Increment(ref evaluateActivationRule.Counters.ForcedActivationCounter);
        }

        private static void UpdateActivationCounter(EntityAnalysisModelActivationRule evaluateActivationRule)
        {
            Interlocked.Increment(ref evaluateActivationRule.Counters.ActivationCounter);
            evaluateActivationRule.ActivationCounterDate = DateTime.UtcNow;
        }

        private static void UpdateEvaluationCount(EntityAnalysisModelActivationRule evaluateActivationRule)
        {
            Interlocked.Increment(ref evaluateActivationRule.Counters.EvaluationCounter);
        }
    }
}