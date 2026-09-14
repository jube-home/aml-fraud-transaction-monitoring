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
using System.Linq;
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using EntityAnalysisModelTtlCounter =
        EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelTtlCounter;

    public static class TtlCounterExtensions
    {
        internal static DateTime ApplyTtlCounterInterval(DateTime referenceDate, string interval, int intervalValue)
        {
            return interval switch
            {
                "d" => referenceDate.AddDays(intervalValue * -1),
                "h" => referenceDate.AddHours(intervalValue * -1),
                "n" => referenceDate.AddMinutes(intervalValue * -1),
                "s" => referenceDate.AddSeconds(intervalValue * -1),
                "m" => referenceDate.AddMonths(intervalValue * -1),
                "y" => referenceDate.AddYears(intervalValue * -1),
                _ => referenceDate
            };
        }

        public static async Task<Context> ExecuteTtlCountersAsync(this Context context)
        {
            var stopwatch = Stopwatch.StartNew();
            var items = new ConcurrentDictionary<string, TaskPerformance>();

            try
            {
                await StartAndWaitOnTasksIfTtlCounterEnabledAtModelLevelAsync(context,
                    context.EntityAnalysisModel.Services.CacheService, items).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Log.Error(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has caused an error in TTL Counters as {ex}.");
            }
            finally
            {
                stopwatch.Stop();

                if (context.LogSampled)
                {
                    var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                        new InvokeStagePerformance();

                    stages.TtlCounters = new StageTiming<TaskPerformance>
                    {
                        DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                        Items = new Dictionary<string, TaskPerformance>(items)
                    };
                }
            }

            return context;
        }

        private static async Task StartAndWaitOnTasksIfTtlCounterEnabledAtModelLevelAsync(Context context,
            CacheService cacheService, ConcurrentDictionary<string, TaskPerformance> items)
        {
            if (context.EntityAnalysisModel.Flags.EnableTtlCounter)
            {
                var tasks = new List<Task>
                {
                    OnlineAggregationOfTtlCountersAsync(context, cacheService, items),
                    OutOfProcessAggregationOfTtlCountersAsync(context, cacheService, items)
                };

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                context.TraceLog(
                    $"TTL Counter cache storage is not enabled so it cannot fetch TTL Counter Aggregation.");
            }
        }

        private static Task OnlineAggregationOfTtlCountersAsync(Context context, CacheService cacheService,
            ConcurrentDictionary<string, TaskPerformance> items)
        {
            context.TraceLog(
                $"TTL Counter cache storage is enabled so it will now proceed to return the TTL Counters with online aggregation.");

            return IterateAndProcessAsync(context, cacheService, items);
        }

        private static async Task IterateAndProcessAsync(Context context, CacheService cacheService,
            ConcurrentDictionary<string, TaskPerformance> items)
        {
            var onlineTtlCounters = context.EntityAnalysisModel.Collections.ModelTtlCounters
                .Where(x => x.OnlineAggregation)
                .ToList();

            if (onlineTtlCounters.Count > 0)
            {
                var tasks = onlineTtlCounters.Select(async ttlCounter =>
                {
                    var itemStopwatch = Stopwatch.StartNew();
                    var startBytes = GC.GetAllocatedBytesForCurrentThread();

                    try
                    {
                        context.TraceLog(
                            $"creating prediction for TTL Counter {ttlCounter.Id} is online aggregation.");

                        if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(ttlCounter
                                .TtlCounterDataName))
                        {
                            AddToResponse(context, ttlCounter,
                                await PerformOnlineAggregationFromCacheAsync(context, cacheService, ttlCounter)
                                    .ConfigureAwait(false));

                            context.TraceLog(
                                $"creating prediction for TTL Counter {ttlCounter.Id} to {context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate}, the TTL Counter Name is {ttlCounter.Name}, the TTL Counter Data Name is {ttlCounter.TtlCounterDataName} and the TTL Counter Data Name Value is {context.EntityAnalysisModelInstanceEntryPayload.Payload[ttlCounter.TtlCounterDataName]}.");
                        }
                        else
                        {
                            context.TraceLog(
                                $"was unable to find a value for TTL Counter Data Name {ttlCounter.TtlCounterDataName} and TTL Counter Name {ttlCounter.Name}.");
                        }
                    }
                    finally
                    {
                        itemStopwatch.Stop();

                        if (context.LogSampled)
                        {
                            var allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;

                            items[ttlCounter.Name] = new TaskPerformance(
                                (long)(itemStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                                Math.Max(allocated, 0));
                        }
                    }
                }).ToList();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                context.TraceLog($"Does not have any online TTL Counters.");
            }
        }

        private static async Task<double> PerformOnlineAggregationFromCacheAsync(Context context,
            CacheService cacheService,
            EntityAnalysisModelTtlCounter ttlCounter)
        {
            context.TraceLog(
                $"creating prediction for TTL Counter {ttlCounter.Id} which has an interval type of {ttlCounter.TtlCounterInterval} and interval value of {ttlCounter.TtlCounterValue}.");

            var adjustedTtlCounterDate = ApplyTtlCounterInterval(
                context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate, ttlCounter.TtlCounterInterval,
                ttlCounter.TtlCounterValue);

            var count = await cacheService.CacheTtlCounterEntryRepository.GetAggregationPreferReplicaAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid,
                ttlCounter.Guid,
                ttlCounter.TtlCounterDataName,
                context.EntityAnalysisModelInstanceEntryPayload.Payload[ttlCounter.TtlCounterDataName].AsString(),
                adjustedTtlCounterDate,
                context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate
            ).ConfigureAwait(false);

            context.TraceLog(
                $"has performed online aggregation for adjusted reference date {adjustedTtlCounterDate} and returned {count}.");

            return count;
        }

        private static void AddToResponse(Context context, EntityAnalysisModelTtlCounter ttlCounter, double count)
        {
            lock (context.EntityAnalysisModelInstanceEntryPayload.TtlCounter)
            {
                if (context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.TryAdd(ttlCounter.Name, count))
                {
                    context.TraceLog(
                        $"TTL Counter {ttlCounter.Id} is missing, so will add this as name {ttlCounter.Name} with value of zero.");

                    if (!ttlCounter.ReportTable || context.EntityAnalysisModelInstanceEntryPayload
                            .EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                    {
                        return;
                    }

                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 5,
                        Key = ttlCounter.Name,
                        KeyValueLong = (int)count,
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload
                                .EntityAnalysisModelInstanceEntryGuid
                    });

                    context.TraceLog(
                        $"TTL Counter {ttlCounter.Id} is missing, added this as name {ttlCounter.Name} with value of zero to the report payload also.");
                }
                else
                {
                    context.TraceLog($"TTL Counter {ttlCounter.Id} exists already, so nothing more added.");
                }
            }
        }

        private static Task OutOfProcessAggregationOfTtlCountersAsync(Context context, CacheService cacheService,
            ConcurrentDictionary<string, TaskPerformance> items)
        {
            context.TraceLog($"will now look for TTL Counters from the cache.");

            var ttlCounters =
                context.EntityAnalysisModel.Collections.ModelTtlCounters.FindAll(x => !x.OnlineAggregation);
            var tasks = ttlCounters.Select(async ttlCounter =>
            {
                var timed = await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                    TaskType.ExecuteTimeToLiveCounterIterationAsync, async () =>
                    {
                        try
                        {
                            AddToResponse(context, ttlCounter,
                                await GetCachedTtlCounterValueAsync(context, cacheService, ttlCounter)
                                    .ConfigureAwait(false));
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.TraceLog($"TTL Counter {ttlCounter.Id} has thrown an error as {ex}.");
                        }
                    }).ConfigureAwait(false);

                if (context.LogSampled)
                {
                    items[ttlCounter.Name] = new TaskPerformance(timed.ComputeTime, timed.ThreadMemory);
                }
            }).ToList();

            return Task.WhenAll(tasks);
        }

        private static async Task<double> GetCachedTtlCounterValueAsync(Context context, CacheService cacheService,
            EntityAnalysisModelTtlCounter ttlCounter)
        {
            var ttlCounterValue = await cacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid,
                    ttlCounter.Guid,
                    ttlCounter.TtlCounterDataName,
                    context.EntityAnalysisModelInstanceEntryPayload.Payload[ttlCounter.TtlCounterDataName].AsString())
                .ConfigureAwait(false);

            return ttlCounterValue;
        }
    }
}