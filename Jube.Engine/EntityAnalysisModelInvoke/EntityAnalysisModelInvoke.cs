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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Exceptions;
using Jube.Engine.EntityAnalysisModelInvoke.Extraction;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.AsyncInvocationCallbackToken;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ResponseTimePipelineCounters;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.StagePerformanceCounters;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.TaskPerformanceCounters;
using Jube.Engine.Helpers;
using Jube.Engine.Observability;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelInvoke
{
    using EntityAnalysisModel = EntityAnalysisModel;

    public static class EntityAnalysisModelInvoke
    {
        private const int DefaultImplicitAsyncTimeoutMilliseconds = 5000;

        public static async Task<Context.Context> InvokeAsync(EntityAnalysisModel entityAnalysisModel,
            MemoryStream inputStream, int? maxBytes = null,
            bool async = false)
        {
            if (maxBytes.HasValue)
            {
                CheckLengthGuardsForExceptions(inputStream, maxBytes.Value);
            }

            var extractor = new EntityAnalysisModelJsonExtractor(entityAnalysisModel,
                entityAnalysisModel.Dependencies.ActiveEntityAnalysisModels,
                entityAnalysisModel.Services.JubeEnvironment, entityAnalysisModel.Services.Log);

            var context = extractor.CreateContext(inputStream);
            DetermineSampled(context);

            if (context.LogSampled)
            {
                var parseStages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                    new InvokeStagePerformance();
                parseStages.Parse = new StageDuration
                {
                    DurationMicroseconds =
                        (long)(context.Stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency))
                };
            }

            context.RecordResponseTime("Parse");
            await CheckAsyncAndInvokeContextAsync(async, context).ConfigureAwait(false);

            return context;
        }

        public static Task InvokeAsync(EntityAnalysisModel entityAnalysisModel,
            DictionaryNoBoxing<string> dictionaryNoBoxing, int entityAnalysisModelReprocessingRuleInstanceId)
        {
            var extractor = new EntityAnalysisModelDictionaryNoBoxingExtractor(entityAnalysisModel,
                entityAnalysisModel.Dependencies.ActiveEntityAnalysisModels,
                entityAnalysisModel.Services.JubeEnvironment, entityAnalysisModel.Services.Log);
            var context = extractor.CreateContext(dictionaryNoBoxing, entityAnalysisModelReprocessingRuleInstanceId);
            DetermineSampled(context);

            if (context.LogSampled)
            {
                var parseStages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                    new InvokeStagePerformance();
                parseStages.Parse = new StageDuration
                {
                    DurationMicroseconds =
                        (long)(context.Stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency))
                };
            }

            context.RecordResponseTime("Parse");
            return InvokeAsync(context);
        }

        private static async Task CheckAsyncAndInvokeContextAsync(bool async, Context.Context context)
        {
            if (async && context.EntityAnalysisModel.ConcurrentQueues.PendingEntityInvoke != null)
            {
                context.TraceLog($"will start asynchronous invocation.");

                EnqueueForAsyncInvocationOfContext(context,
                    context.EntityAnalysisModel.ConcurrentQueues.PendingEntityInvoke);
            }
            else if (context.EntityAnalysisModel.Flags.EnableImplicitAsync)
            {
                context.TraceLog($"will start implicit asynchronous invocation.");

                await InvokeWithImplicitAsyncTimeoutAsync(context).ConfigureAwait(false);
            }
            else
            {
                context.TraceLog($"will start synchronous invocation.");

                await InvokeAsync(context).ConfigureAwait(false);
            }

            context.TraceLog($"has finished model invocation.");
        }

        private static async Task InvokeWithImplicitAsyncTimeoutAsync(Context.Context context)
        {
            Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ModelImplicitAsyncInvokeCounter);

            var timeoutMilliseconds = context.EntityAnalysisModel.Flags.ImplicitAsyncTimeoutMilliseconds ??
                                      DefaultImplicitAsyncTimeoutMilliseconds;

            var stableSnapshot = new EntityAnalysisModelInstanceEntryPayload
            {
                EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload
                    .EntityAnalysisModelInstanceEntryGuid,
                EntityInstanceEntryId = context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId,
                CreatedDate = context.EntityAnalysisModelInstanceEntryPayload.CreatedDate,
                ReferenceDate = context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                EntityAnalysisModelGuid = context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelGuid,
                EntityAnalysisModelName = context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelName,
                EntityAnalysisModelInstanceGuid =
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceGuid,
                EntityAnalysisModelInstanceName =
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceName,
                EntityAnalysisModelReprocessingRuleInstanceId = context.EntityAnalysisModelInstanceEntryPayload
                    .EntityAnalysisModelReprocessingRuleInstanceId
            };

            var invocation = InvokeAsync(context);

            try
            {
                await invocation.WaitAsync(TimeSpan.FromMilliseconds(timeoutMilliseconds)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ModelImplicitAsyncTimeoutCounter);

                context.Async = true;
                context.ImplicitAsyncTimedOut = true;
                stableSnapshot.ImplicitAsyncTimedOut = true;
                context.EntityAnalysisModelInstanceEntryPayload.ImplicitAsyncTimedOut = true;

                var timeoutResponseJson = BuildJsonResponses.BuildFullJson(
                    stableSnapshot, context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);

                context.ImplicitAsyncTimeoutResponseJson = timeoutResponseJson;
                context.EntityAnalysisModelInstanceEntryPayload.ResponseJson = timeoutResponseJson;

                context.EntityAnalysisModel.Services.ImplicitAsyncInvocationTracker.TrackOverdueInvocation(
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                    context.EntityAnalysisModel, invocation);
            }
        }

        private static void CheckLengthGuardsForExceptions(MemoryStream inputStream, int maxBytes)
        {
            if (inputStream.Length == 0)
            {
                throw new ZeroBytesException();
            }

            if (inputStream.Length > maxBytes)
            {
                throw new ExceededBytesException();
            }
        }

        private static void EnqueueForAsyncInvocationOfContext(Context.Context context,
            ConcurrentQueue<Context.Context> pendingEntityInvoke)
        {
            if (pendingEntityInvoke.Count >=
                int.Parse(context.EntityAnalysisModel.Services.JubeEnvironment.AppSettings(
                    "MaximumModelInvokeAsyncQueue")))
            {
                throw new ExceededQueueLengthException();
            }

            context.Async = true;
            pendingEntityInvoke.Enqueue(context);

            var asyncInvocationCallbackToken = new AsyncInvocationCallbackToken
            {
                EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload
                    .EntityAnalysisModelInstanceEntryGuid
            };

            context.EntityAnalysisModelInstanceEntryPayload.ResponseJson = BuildJsonResponses.BuildFullJson(
                asyncInvocationCallbackToken,
                context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);
        }

        public static async Task InvokeAsync(Context.Context context)
        {
            try
            {
                context.TraceLog(
                    $"has started the invocation timer and will now update the reference date and check that it is not older than the latest stored, else exception.");

                IncrementModelInvokeCounter(context);

                await context.CheckIntegrityAndUpsertAsync(context.EntityAnalysisModel.Services.CacheService)
                    .ConfigureAwait(false);
                context.RecordResponseTime("CheckIntegrityAndUpsert");

                context.TraceLog(
                    $"has configured startup options. The model invocation counter is {context.EntityAnalysisModel.Counters.ModelInvokeCounter} and " +
                    $"reprocessing is set to {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue}. Will now proceed" +
                    $" to create the Invoke Context and execute Inline Functions. " +
                    $"Has updated reference date to {context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate}.");

                context.ExecuteInlineFunctions();
                context.RecordResponseTime("InlineFunctions");

                await context.ExecuteInlineScriptsAsync();
                context.RecordResponseTime("InlineScripts");

                context.ExecuteGatewayRules();
                context.RecordResponseTime("Gateway");

                if (context.EntityAnalysisModelInstanceEntryPayload.MatchedGatewayRule)
                {
                    context.ExecuteCacheDbStorage(context.EntityAnalysisModel.Services.CacheService,
                        context.EntityAnalysisModel.Collections.DistinctSearchKeys);

                    context.RecordResponseTime("CacheDbStorage");

                    context.PendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                        TaskType.SanctionsAsync,
                        async () => await context.ExecuteSanctionsAsync().ConfigureAwait(false),
                        context.Log, true));

                    context.PendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                        TaskType.TtlCountersAsync,
                        async () => await context.ExecuteTtlCountersAsync().ConfigureAwait(false),
                        context.Log));

                    context.PendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                        TaskType.AbstractionRulesWithSearchKeysAsync,
                        async () => await context.ExecuteAbstractionRulesWithSearchKeysAsync().ConfigureAwait(false),
                        context.Log));

                    await context.WaitReadTasksAsync().ConfigureAwait(false);
                    context.RecordResponseTime("JoinReadTasks");

                    context.ExecuteAbstractionRulesWithoutSearchKeys();
                    context.RecordResponseTime("AbstractionRulesWithoutSearchKeys");

                    context.ExecuteAbstractionCalculations();
                    context.RecordResponseTime("AbstractionCalculations");

                    context.ExecuteExhaustiveAdaptation();
                    context.RecordResponseTime("ExhaustiveAdaptation");

                    await context.ExecuteHttpAdaptationsAsync().ConfigureAwait(false);
                    context.RecordResponseTime("HttpAdaptation");

                    await context.ExecuteActivationsAsync().ConfigureAwait(false);
                    context.RecordResponseTime("Activation");
                }

                await context.WaitWriteTasksAsync().ConfigureAwait(false);
                context.RecordResponseTime("JoinWriteTasks");

                await context.WriteResponseJsonAndQueueAsynchronousResponseMessageAsync().ConfigureAwait(false);
                context.RecordResponseTime("WriteResponse");

                await context.ActivationRuleBuildArchivePayloadAsync().ConfigureAwait(false);
                context.RecordResponseTime("BuildArchivePayload");

                Interlocked.Add(ref context.EntityAnalysisModel.Counters.ModelTotalResponseTime,
                    (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency));

                var totalDurationMicroseconds =
                    (long)(context.Stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));

                context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TotalDurationMicroseconds =
                    totalDurationMicroseconds;

                InterlockedMinMax.AccumulateMin(ref context.EntityAnalysisModel.Counters.MinModelResponseTime,
                    totalDurationMicroseconds);

                InterlockedMinMax.AccumulateMax(ref context.EntityAnalysisModel.Counters.MaxModelResponseTime,
                    totalDurationMicroseconds);

                RecordStagePerformance(context);
                RecordResponseTimePipelinePerformance(context);
                RecordTaskPerformance(context);

                context.TraceLog($"all model invocation processing has completed.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                                       && ex is not ReferenceDateInFutureException
                                       && ex is not ExceededQueueLengthException
                                       && ex is not ZeroBytesException
                                       && ex is not ExceededBytesException)
            {
                context.Log.Error(
                    $"Entity Invoke: {context.EntityAnalysisModel.Instance.Id} has created a general error as {ex}.");
            }
        }

        private static void IncrementModelInvokeCounter(Context.Context context)
        {
            Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ModelInvokeCounter);
        }

        private static void DetermineSampled(Context.Context context)
        {
            var flags = context.EntityAnalysisModel.Flags;
            context.LogSampled = !flags.EnableLogsInfo
                                 || !flags.EnableSampling
                                 || context.Random.NextDouble() * 100.0 < flags.SamplePercentage.GetValueOrDefault();

            context.EntityAnalysisModelInstanceEntryPayload.IsSampled = context.LogSampled;
        }

        private static void RecordStagePerformance(Context.Context context)
        {
            if (!context.LogSampled)
            {
                return;
            }

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages;
            if (stages == null)
            {
                return;
            }

            foreach (var (name, stage) in stages.EnumerateStages())
            {
                EngineDiagnostics.StageDuration.Record(stage.DurationMicroseconds / 1000.0,
                    new KeyValuePair<string, object>("stage", name),
                    new KeyValuePair<string, object>("model", context.EntityAnalysisModel.Instance.Name));

                var accumulator =
                    context.EntityAnalysisModel.StagePerformanceCounters.Stages.GetOrAdd(name,
                        _ => new StagePerformanceAccumulator());
                Interlocked.Add(ref accumulator.TotalMicroseconds, stage.DurationMicroseconds);
                InterlockedMinMax.AccumulateMin(ref accumulator.MinMicroseconds, stage.DurationMicroseconds);
                InterlockedMinMax.AccumulateMax(ref accumulator.MaxMicroseconds, stage.DurationMicroseconds);
                Interlocked.Increment(ref accumulator.InvokeCount);
            }
        }

        private static void RecordResponseTimePipelinePerformance(Context.Context context)
        {
            var pipeline = context.EntityAnalysisModelInstanceEntryPayload.ResponseTimePipeline;
            if (pipeline == null)
            {
                return;
            }

            foreach (var entry in pipeline.Entries)
            {
                EngineDiagnostics.ResponseTimePipelineDuration.Record(entry.DurationMicroseconds / 1000.0,
                    new KeyValuePair<string, object>("stage", entry.Stage));

                var accumulator =
                    context.EntityAnalysisModel.ResponseTimePipelineCounters.Stages.GetOrAdd(entry.Stage,
                        _ => new ResponseTimePipelineAccumulator());
                Interlocked.Add(ref accumulator.TotalMicroseconds, entry.DurationMicroseconds);
                InterlockedMinMax.AccumulateMin(ref accumulator.MinMicroseconds, entry.DurationMicroseconds);
                InterlockedMinMax.AccumulateMax(ref accumulator.MaxMicroseconds, entry.DurationMicroseconds);
                Interlocked.Add(ref accumulator.TotalAllocatedBytes, entry.AllocatedBytes);
                InterlockedMinMax.AccumulateMin(ref accumulator.MinAllocatedBytes, entry.AllocatedBytes);
                InterlockedMinMax.AccumulateMax(ref accumulator.MaxAllocatedBytes, entry.AllocatedBytes);
                Interlocked.Increment(ref accumulator.InvokeCount);
            }
        }

        private static void RecordTaskPerformance(Context.Context context)
        {
            if (!context.LogSampled)
            {
                return;
            }

            var taskWrapperStats =
                context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats;
            if (taskWrapperStats == null)
            {
                return;
            }

            foreach (var (direction, name, task) in taskWrapperStats.EnumerateTasks())
            {
                EngineDiagnostics.TaskDuration.Record(task.ComputeTimeMicroseconds / 1000.0,
                    new KeyValuePair<string, object>("direction", direction),
                    new KeyValuePair<string, object>("task", name));

                var key = $"{direction}.{name}";
                var accumulator =
                    context.EntityAnalysisModel.TaskPerformanceCounters.Tasks.GetOrAdd(key,
                        _ => new TaskPerformanceAccumulator());
                Interlocked.Add(ref accumulator.TotalMicroseconds, task.ComputeTimeMicroseconds);
                InterlockedMinMax.AccumulateMin(ref accumulator.MinMicroseconds, task.ComputeTimeMicroseconds);
                InterlockedMinMax.AccumulateMax(ref accumulator.MaxMicroseconds, task.ComputeTimeMicroseconds);
                Interlocked.Add(ref accumulator.TotalAllocatedBytes, task.Memory);
                InterlockedMinMax.AccumulateMin(ref accumulator.MinAllocatedBytes, task.Memory);
                InterlockedMinMax.AccumulateMax(ref accumulator.MaxAllocatedBytes, task.Memory);
                Interlocked.Increment(ref accumulator.InvokeCount);
            }
        }
    }
}