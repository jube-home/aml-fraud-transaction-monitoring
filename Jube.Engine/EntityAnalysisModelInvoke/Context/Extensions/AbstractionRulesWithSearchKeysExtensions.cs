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
using Jube.Cache.Redis.Models;
using Jube.Data.Poco;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.TaskCancellation.TaskHelper;
using StackExchange.Redis;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using EntityAnalysisModelAbstractionRule =
        EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionRule;

    public static class AbstractionRulesWithSearchKeysExtensions
    {
        public static async Task<Context> ExecuteAbstractionRulesWithSearchKeysAsync(this Context context)
        {
            var stopwatch = Stopwatch.StartNew();
            var items = new Dictionary<string, TaskPerformance>();

            if (context.EntityAnalysisModel.Flags.EnableCache)
            {
                context.TraceLog(
                    $"Entity cache storage is enabled so will now proceed to loop through the distinct grouping keys for this model.");

                var abstractionRuleMatches = new ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>>();
                var sortedSetKeysByGroupingKey = new ConcurrentDictionary<string, List<RedisValue>>();

                var payloadMap = await FetchSortedSetKeysByGroupingKeyAsync(context, sortedSetKeysByGroupingKey)
                    .ConfigureAwait(false);
                var parsedPayloadMap = BuildParsedPayloadMap(context, payloadMap);

                await ExecuteAbstractionRulesForGroupingKeysAsync(context, sortedSetKeysByGroupingKey,
                    abstractionRuleMatches, parsedPayloadMap).ConfigureAwait(false);
                await CalculateAbstractionRuleValuesOrLookupFromTheCacheAsync(context,
                        context.EntityAnalysisModel.Services.CacheService, abstractionRuleMatches, items)
                    .ConfigureAwait(false);

                context.TraceLog($"all abstraction aggregation has finished, basic rules will now be processed.");
            }
            else
            {
                context.TraceLog(
                    $"Entity cache storage is not enabled so it cannot fetch anything relating to Abstraction Rules.");
            }

            stopwatch.Stop();

            if (!context.LogSampled)
            {
                return context;
            }

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                new InvokeStagePerformance();
            stages.AbstractionRulesWithSearchKeys = new StageTiming<TaskPerformance>
            {
                DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                Items = items
            };

            return context;
        }

        private static Task ExecuteAbstractionRulesForGroupingKeysAsync(Context context,
            ConcurrentDictionary<string, List<RedisValue>> sortedSetKeysByGroupingKey,
            ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatches,
            Dictionary<string, DictionaryNoBoxing<string>> parsedPayloadMap)
        {
            var pendingExecutionThreads = new List<Task>();
            foreach (var (key, value) in context.EntityAnalysisModel.Collections.DistinctSearchKeys)
            {
                if (value.SearchKeyCache)
                {
                    continue;
                }

                if (!context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(key))
                {
                    continue;
                }

                if (!sortedSetKeysByGroupingKey.TryGetValue(key, out var sortedSetKeys))
                {
                    context.TraceLog(
                        $"grouping key {key} had no sorted set keys available — rules for this grouping key will not be evaluated.");

                    continue;
                }

                var execute = new Execute
                {
                    EntityInstanceEntryDictionaryKvPs = context.EntityAnalysisModelInstanceEntryPayload.Dictionary,
                    DistinctSearchKey = value,
                    CachePayloadDocument = context.EntityAnalysisModelInstanceEntryPayload.Payload,
                    EntityAnalysisModelInstanceEntryPayload = context.EntityAnalysisModelInstanceEntryPayload,
                    AbstractionRuleMatches = abstractionRuleMatches,
                    EntityAnalysisModel = context.EntityAnalysisModel,
                    Log = context.Log,
                    SortedSetKeys = sortedSetKeys,
                    PayloadMap = parsedPayloadMap,
                    Context = context
                };

                context.TraceLog(
                    $"has created an execute object for grouping key {key} with {sortedSetKeys.Count} sorted set keys.");

                pendingExecutionThreads.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                    TaskType.ExecuteAbstractionRulesWithSearchKeyAsync,
                    async () => await execute.StartAsync().ConfigureAwait(false)));
            }

            context.TraceLog(
                $"will now loop around all of the Abstraction rules for the purposes of performing the aggregations.");

            return Task.WhenAll(pendingExecutionThreads);
        }

        private static Dictionary<string, DictionaryNoBoxing<string>> BuildParsedPayloadMap(Context context,
            Dictionary<string, DictionaryNoBoxing<int>> payloadMap)
        {
            var parsedPayloadMap = new Dictionary<string, DictionaryNoBoxing<string>>(payloadMap.Count);
            var parseIndexCache = context.EntityAnalysisModel.Collections.ParseIndexCache;

            foreach (var (key, raw) in payloadMap)
            {
                var document = new DictionaryNoBoxing<string>(raw.Count);
                foreach (var (i, value) in raw)
                {
                    switch (i)
                    {
                        case -1:
                            document.AddUnchecked(context.EntityAnalysisModel.References.ReferenceDateName, value);
                            continue;
                        case < 0:
                            continue;
                    }

                    if (parseIndexCache is not null && parseIndexCache.TryGetValue(i, out var name))
                    {
                        document.AddUnchecked(name, value);
                    }
                }

                parsedPayloadMap[key] = document;
            }

            return parsedPayloadMap;
        }

        private static async Task<Dictionary<string, DictionaryNoBoxing<int>>> FetchSortedSetKeysByGroupingKeyAsync(
            Context context, ConcurrentDictionary<string, List<RedisValue>> sortedSetKeysByGroupingKey)
        {
            var sortedSetFetchTasks = new List<Task>();
            foreach (var (key, value) in context.EntityAnalysisModel.Collections.DistinctSearchKeys)
            {
                context.TraceLog($"is evaluating grouping key {key}.");

                try
                {
                    if (value.SearchKeyCache)
                    {
                        context.TraceLog(
                            $"grouping key {key} is a search key, so the values will be fetched from the cache later on.");
                    }
                    else
                    {
                        context.TraceLog($"checking if grouping key {key} exists in the current payload data.");

                        if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(key))
                        {
                            var limit = context.EntityAnalysisModel.Cache.CacheTtlLimit < value.SearchKeyFetchLimit
                                ? context.EntityAnalysisModel.Cache.CacheTtlLimit
                                : value.SearchKeyFetchLimit;

                            context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                                TaskType.CachePayloadInsertAsync, async () => await context.EntityAnalysisModel.Services
                                    .CacheService.CachePayloadRepository
                                    .InsertPayloadJournalAndLedgerAsync(
                                        context.EntityAnalysisModel.Instance.TenantRegistryId,
                                        context.EntityAnalysisModel.Instance.Guid,
                                        key,
                                        context.EntityAnalysisModelInstanceEntryPayload.Payload[key].AsString(),
                                        context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                                        context.EntityAnalysisModelInstanceEntryPayload
                                            .EntityAnalysisModelInstanceEntryGuid).ConfigureAwait(false), context.Log));

                            sortedSetFetchTasks.Add(FetchSortedSetKeysAsync(key, limit));

                            async Task FetchSortedSetKeysAsync(string groupingKey, int fetchLimit)
                            {
                                try
                                {
                                    var keys = await context.EntityAnalysisModel.Services.CacheService
                                        .CachePayloadRepository
                                        .GetSortedSetKeysAsync(
                                            context.EntityAnalysisModel.Instance.TenantRegistryId,
                                            context.EntityAnalysisModel.Instance.Guid,
                                            groupingKey,
                                            context.EntityAnalysisModelInstanceEntryPayload.Payload[groupingKey]
                                                .AsString(),
                                            fetchLimit,
                                            context.EntityAnalysisModelInstanceEntryPayload
                                                .EntityAnalysisModelInstanceEntryGuid)
                                        .ConfigureAwait(false);

                                    sortedSetKeysByGroupingKey[groupingKey] = keys;

                                    context.TraceLog(
                                        $"grouping key {groupingKey} returned {keys.Count} sorted set keys.");
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    context.Log.Error(
                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} grouping key {groupingKey} failed to fetch sorted set keys and will be excluded from this evaluation as {ex}.");
                                }
                            }
                        }
                        else
                        {
                            context.TraceLog(
                                $"grouping key {key} does not exist in the current transaction data being processed.");
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} checking if grouping key {key} has created an error as {ex}.");
                }
            }

            await Task.WhenAll(sortedSetFetchTasks).ConfigureAwait(false);

            var distinctRedisValues = sortedSetKeysByGroupingKey.Values
                .SelectMany(x => x)
                .Select(x => x.ToString())
                .Distinct()
                .Select(s => (RedisValue)s)
                .ToList();

            context.TraceLog(
                $"has {distinctRedisValues.Count} distinct payload keys across all grouping keys. Fetching batch.");

            var payloadMap = await context.EntityAnalysisModel.Services.CacheService.CachePayloadRepository
                .GetPayloadBatchAsync(
                    context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid,
                    distinctRedisValues,
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid)
                .ConfigureAwait(false);

            context.TraceLog($"batch payload fetch returned {payloadMap.Count} records.");
            return payloadMap;
        }

        private static async Task CalculateAbstractionRuleValuesOrLookupFromTheCacheAsync(Context context,
            CacheService cacheService,
            ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatches,
            Dictionary<string, TaskPerformance> items)
        {
            var cachedAbstractionValues =
                await FetchCacheBackedAbstractionRuleValuesAsync(context, cacheService, items)
                    .ConfigureAwait(false);

            foreach (var abstractionRule in context.EntityAnalysisModel.Collections.ModelAbstractionRules)
            {
                if (!abstractionRule.Search)
                {
                    continue;
                }

                var itemStopwatch = Stopwatch.StartNew();
                var startBytes = GC.GetAllocatedBytesForCurrentThread();
                try
                {
                    context.TraceLog($"is evaluating abstraction rule {abstractionRule.Id}.");

                    var isCacheBacked = context.EntityAnalysisModel.Collections.DistinctSearchKeys.FirstOrDefault(x =>
                        x.Key == abstractionRule.SearchKey && x.Value.SearchKeyCache).Value != null;

                    if (isCacheBacked)
                    {
                        context.TraceLog($"abstraction rule {abstractionRule.Id} has its values in the cache.");

                        if (cachedAbstractionValues.TryGetValue(abstractionRule.Name, out var cachedValue))
                        {
                            AddComputedValuesToAbstractionRulePayload(context, abstractionRule, cachedValue);
                        }
                    }
                    else
                    {
                        context.TraceLog(
                            $"is aggregating abstraction rule {abstractionRule.Id} using documents in the entities collection of the cache.");

                        var aggregatedValue = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                            context.EntityAnalysisModelInstanceEntryPayload, abstractionRuleMatches, abstractionRule,
                            context.Log);

                        AddComputedValuesToAbstractionRulePayload(context, abstractionRule, aggregatedValue);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is aggregating abstraction rule {abstractionRule.Id} but has created an error as {ex}.");
                }
                finally
                {
                    itemStopwatch.Stop();
                    var allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
                    items[abstractionRule.Name] = new TaskPerformance(
                        (long)(itemStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                        Math.Max(allocated, 0));
                }
            }
        }

        private static async Task<Dictionary<string, double>> FetchCacheBackedAbstractionRuleValuesAsync(
            Context context, CacheService cacheService, Dictionary<string, TaskPerformance> items)
        {
            var itemStopwatch = Stopwatch.StartNew();
            var startBytes = GC.GetAllocatedBytesForCurrentThread();
            var requests = new List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue>();

            try
            {
                foreach (var abstractionRule in context.EntityAnalysisModel.Collections.ModelAbstractionRules)
                {
                    if (!abstractionRule.Search)
                    {
                        continue;
                    }

                    try
                    {
                        var isCacheBacked = context.EntityAnalysisModel.Collections.DistinctSearchKeys
                            .FirstOrDefault(x => x.Key == abstractionRule.SearchKey && x.Value.SearchKeyCache)
                            .Value != null;

                        if (!isCacheBacked)
                        {
                            continue;
                        }

                        requests.Add(new EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue
                        {
                            AbstractionRuleName = abstractionRule.Name,
                            SearchKey = abstractionRule.SearchKey,
                            SearchValue = context.EntityAnalysisModelInstanceEntryPayload
                                .Payload[abstractionRule.SearchKey].AsString()
                        });
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Log.Error(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} could not build a cache lookup request for abstraction rule {abstractionRule.Id} as {ex}.");
                    }
                }

                if (requests.Count == 0)
                {
                    return new Dictionary<string, double>();
                }

                return await cacheService.CacheAbstractionRepository
                    .GetAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                        context.EntityAnalysisModel.Instance.Guid, requests)
                    .ConfigureAwait(false);
            }
            finally
            {
                itemStopwatch.Stop();
                var allocated = GC.GetAllocatedBytesForCurrentThread() - startBytes;
                items["AbstractionRuleCacheBatchFetch"] = new TaskPerformance(
                    (long)(itemStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                    Math.Max(allocated, 0));
            }
        }

        private static void AddComputedValuesToAbstractionRulePayload(Context context,
            EntityAnalysisModelAbstractionRule abstractionRule, double value)
        {
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction
                .Add(abstractionRule.Name, value);

            if (abstractionRule.ReportTable)
            {
                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                {
                    ProcessingTypeId = 5,
                    Key = abstractionRule.Name,
                    KeyValueFloat = value,
                    EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload
                        .EntityAnalysisModelInstanceEntryGuid
                });

                context.TraceLog(
                    $"is aggregating abstraction rule {abstractionRule.Id} added value {value} to report payload with a column name of {abstractionRule.Name}.");
            }

            context.TraceLog($"finished aggregating abstraction rule {abstractionRule.Id}.");
        }
    }
}