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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Cache.Interfaces;
using Jube.Data.Cache.Redis.MessagePack;
using Jube.Extensions;
using log4net;
using MessagePack;
using StackExchange.Redis;
using Exception = System.Exception;

namespace Jube.Data.Cache.Jube;

public class CachePayloadLatestRepository(IDatabaseAsync redisDatabase, ILog log) : ICachePayloadLatestRepository
{
    public async Task UpsertAsync(int tenantRegistryId, int entityAnalysisModelId,
        DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid, string entryKey, string entryKeyValue)
    {
        try
        {
            var cachePayloadLatest = new CachePayloadLatest
            {
                Key = $"Payload:{tenantRegistryId}:{entityAnalysisModelId}",
                Field = entityAnalysisModelInstanceEntryGuid.ToString(),
                ReferenceDate = referenceDate,
                ReclassificationCount = 0,
                ReclassificationDate = null,
                UpdatedDate = DateTime.Now
            };

            await UpsertMessagePack(tenantRegistryId, entityAnalysisModelId, entryKey, entryKeyValue,
                cachePayloadLatest, referenceDate);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task UpsertAsync(int tenantRegistryId, int entityAnalysisModelId, Dictionary<string, object> payload,
        DateTime referenceDate,
        Guid entityAnalysisModelInstanceEntryGuid, string entryKey, string entryKeyValue)
    {
        try
        {
            var cachePayloadLatest = new CachePayloadLatest
            {
                Payload = payload,
                Key = $"Payload:{tenantRegistryId}:{entityAnalysisModelId}:{entityAnalysisModelInstanceEntryGuid}",
                ReferenceDate = referenceDate,
                ReclassificationCount = 0,
                ReclassificationDate = null,
                UpdatedDate = DateTime.Now
            };

            await UpsertMessagePack(tenantRegistryId, entityAnalysisModelId, entryKey, entryKeyValue,
                cachePayloadLatest, referenceDate);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    private async Task UpsertMessagePack(int tenantRegistryId, int entityAnalysisModelId, string entryKey,
        string entryKeyValue, CachePayloadLatest cachePayloadLatest, DateTime referenceDate)
    {
        try
        {
            var ms = new MemoryStream();
            await MessagePackSerializer.SerializeAsync(ms, cachePayloadLatest,
                MessagePackSerializerOptionsHelper.ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true));

            var redisKeyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelId}:{entryKey}";
            var redisKeyPayloadCount = $"PayloadCount:{tenantRegistryId}";
            var redisKeyPayloadFirst = $"ReferenceDateFirst:{tenantRegistryId}:{entityAnalysisModelId}:{entryKey}";
            var redisKeyPayloadLatest = $"ReferenceDateLatest:{tenantRegistryId}:{entityAnalysisModelId}:{entryKey}";
            var redisKeyPayloadLatestCount = $"LatestCount:{tenantRegistryId}:{entityAnalysisModelId}";
            var redisHSetKey = $"{entryKeyValue}";
            var referenceDateTimestamp = referenceDate.ToUnixTimeMilliSeconds();
            var bytes = ms.ToArray();

            var tasks = new List<Task>
            {
                redisDatabase.SortedSetUpdateAsync(redisKeyPayloadLatest, redisHSetKey, referenceDateTimestamp),
                redisDatabase.HashIncrementAsync(redisKeyPayloadCount, entityAnalysisModelId),
                redisDatabase.HashExistsAsync(redisKeyPayload, redisHSetKey).ContinueWith(w =>
                {
                    if (!w.Result)
                    {
                        redisDatabase.HashIncrementAsync(redisKeyPayloadLatestCount, entryKey);
                    }

                    redisDatabase.HashSetAsync(redisKeyPayload, redisHSetKey, bytes);
                }),
                redisDatabase.SortedSetScoreAsync(redisKeyPayloadFirst, entryKeyValue).ContinueWith(w =>
                {
                    if (w.Result == null)
                    {
                        redisDatabase.SortedSetAddAsync(redisKeyPayloadFirst, entryKeyValue, referenceDateTimestamp);
                    }

                    redisDatabase.HashSetAsync(redisKeyPayload, redisHSetKey, bytes);
                })
            };

            Task.WaitAll(tasks.ToArray());
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task<List<string>> GetDistinctKeysAsync(int tenantRegistryId, int entityAnalysisModelId,
        string key, DateTime dateFrom, DateTime dateTo)
    {
        var value = new List<string>();
        try
        {
            var redisKey = $"Payload:{tenantRegistryId}:{entityAnalysisModelId}:{key}";
            var hashEntries = await redisDatabase.HashGetAllAsync(redisKey);

            value = (from hashEntry in hashEntries
                    let cachePayloadLatest = MessagePackSerializer
                        .Deserialize<CachePayloadLatest>(hashEntry.Value,
                            MessagePackSerializerOptionsHelper
                                .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true))
                    where cachePayloadLatest.UpdatedDate >= dateFrom
                          && cachePayloadLatest.UpdatedDate <= dateTo
                    select hashEntry.Name)
                .Select(s => (string) s).ToList();
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return value;
    }

    public async Task<List<string>> GetDistinctKeysAsync(int tenantRegistryId, int entityAnalysisModelId, string key,
        DateTime dateBefore)
    {
        var value = new List<string>();
        try
        {
            var redisKey = $"Payload:{tenantRegistryId}:{entityAnalysisModelId}:{key}";
            var hashEntries = await redisDatabase.HashGetAllAsync(redisKey);

            value = (from hashEntry in hashEntries
                    let cachePayloadLatest = MessagePackSerializer
                        .Deserialize<CachePayloadLatest>(hashEntry.Value,
                            MessagePackSerializerOptionsHelper
                                .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true))
                    where cachePayloadLatest.UpdatedDate <= dateBefore
                    select hashEntry.Name)
                .Select(s => (string) s).ToList();
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return value;
    }

    public async Task<List<string>> GetDistinctKeysAsync(int tenantRegistryId, int entityAnalysisModelId, string key)
    {
        var value = new List<string>();
        try
        {
            var redisKey = $"Payload:{tenantRegistryId}:{entityAnalysisModelId}:{key}";
            var hashEntries = await redisDatabase.HashGetAllAsync(redisKey);
            return hashEntries.Select(hashEntry => hashEntry.Name).Select(s => (string) s).ToList();
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return value;
    }

    public async Task DeleteByReferenceDate(int tenantRegistryId, int entityAnalysisModelId,
        DateTime referenceDate, DateTime thresholdReferenceDate, int limit,
        List<(string name, string interval, int intervalValue)> searchKeys)
    {
        var tasks = new List<Task>();

        await DeleteLatestWhereExpiredAndReduceLatestCount(tenantRegistryId, entityAnalysisModelId, thresholdReferenceDate,
            limit, tasks);

        await DeleteSortedSetForSearchKeyAndSetFirst(tenantRegistryId, entityAnalysisModelId, referenceDate, limit,
            searchKeys);

        Task.WaitAll(tasks.ToArray());
    }

    private async Task DeleteSortedSetForSearchKeyAndSetFirst(int tenantRegistryId, int entityAnalysisModelId,
        DateTime referenceDate, int limit, List<(string name, string interval, int intervalValue)> searchKeys)
    {
        foreach (var searchKey in searchKeys)
        {
            var thresholdReferenceDateTimestamp = (searchKey.interval switch
            {
                "d" => referenceDate.AddDays(searchKey.intervalValue * -1),
                "h" => referenceDate.AddHours(searchKey.intervalValue * -1),
                "n" => referenceDate.AddMinutes(searchKey.intervalValue * -1),
                "s" => referenceDate.AddSeconds(searchKey.intervalValue * -1),
                "m" => referenceDate.AddMonths(searchKey.intervalValue * -1),
                "y" => referenceDate.AddYears(searchKey.intervalValue * -1),
                _ => referenceDate.AddDays(searchKey.intervalValue * -1)
            }).ToUnixTimeMilliSeconds();

            const double epsilon = 2.2250738585072014E-308d;

            var redisKeyReferenceDateFirst =
                $"ReferenceDateFirst:{tenantRegistryId}:{entityAnalysisModelId}:{searchKey.name}";

            var breakOutOfWhileLoopAsUpToDate = false;
            while (!breakOutOfWhileLoopAsUpToDate)
            {
                var sortedSetFirstEntries =
                    await redisDatabase.SortedSetRangeByRankWithScoresAsync(redisKeyReferenceDateFirst, 0, limit);

                if (sortedSetFirstEntries.Length == 0)
                {
                    breakOutOfWhileLoopAsUpToDate = true;
                }
                else
                {
                    if (sortedSetFirstEntries[0].Score >= thresholdReferenceDateTimestamp)
                    {
                        breakOutOfWhileLoopAsUpToDate = true;
                    }
                    else
                    {
                        foreach (var sortedSetFirstEntry in sortedSetFirstEntries)
                        {
                            var redisSortedSetKeyValue =
                                $"Payload:{tenantRegistryId}:{entityAnalysisModelId}:{searchKey.name}:{sortedSetFirstEntry.Element}";

                            var breakOutOfWhileLoopAsNoMoreRecordsForKey = false;
                            while (!breakOutOfWhileLoopAsNoMoreRecordsForKey)
                            {
                                var sortedSetPayloadEntries =
                                    await redisDatabase.SortedSetRangeByRankWithScoresAsync(redisSortedSetKeyValue, 0,
                                        limit);

                                if (sortedSetPayloadEntries.Length == 0)
                                {
                                    await redisDatabase.SortedSetRemoveAsync(redisKeyReferenceDateFirst,
                                        sortedSetFirstEntry.Element);

                                    breakOutOfWhileLoopAsNoMoreRecordsForKey = true;
                                }
                                else
                                {
                                    var keysToBeDeleted = new List<RedisValue>();
                                    foreach (var sortedSetPayloadEntry in sortedSetPayloadEntries)
                                    {
                                        if (sortedSetPayloadEntry.Score >= thresholdReferenceDateTimestamp)
                                        {
                                            if (Math.Abs(sortedSetPayloadEntry.Score - sortedSetFirstEntry.Score) >
                                                epsilon)
                                            {
                                                await redisDatabase.SortedSetUpdateAsync(redisKeyReferenceDateFirst,
                                                    sortedSetFirstEntry.Element, sortedSetPayloadEntry.Score);
                                            }

                                            breakOutOfWhileLoopAsNoMoreRecordsForKey = true;
                                            break;
                                        }

                                        keysToBeDeleted.Add(sortedSetPayloadEntry.Element);
                                    }

                                    if (keysToBeDeleted.Count > 0)
                                    {
                                        await redisDatabase.SortedSetRemoveAsync(redisSortedSetKeyValue,
                                            keysToBeDeleted.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private async Task DeleteLatestWhereExpiredAndReduceLatestCount(int tenantRegistryId, int entityAnalysisModelId,
        DateTime referenceDate, int limit, List<Task> tasks)
    {
        var referenceDateTimestampThreshold =
            referenceDate.ToUnixTimeMilliSeconds();

        var redisKeyCount = $"LatestCount:{tenantRegistryId}:{entityAnalysisModelId}";
        var latestCounts = await redisDatabase.HashGetAllAsync(redisKeyCount);

        foreach (var latestCount in latestCounts)
        {
            var redisKey = $"ReferenceDateLatest:{tenantRegistryId}:{entityAnalysisModelId}:{latestCount.Name}";

            var breakWhile = false;
            while (!breakWhile)
            {
                var sortedSetEntries = await redisDatabase.SortedSetRangeByRankWithScoresAsync(redisKey, 0, limit);
                if (sortedSetEntries.Length == 0)
                {
                    breakWhile = true;
                    continue;
                }

                var redisValuesToDelete = new List<RedisValue>();
                foreach (var sortedSetEntry in sortedSetEntries)
                {
                    if (sortedSetEntry.Score <= referenceDateTimestampThreshold)
                    {
                        redisValuesToDelete.Add(new RedisValue(sortedSetEntry.Element));
                    }
                    else
                    {
                        breakWhile = true;
                    }
                }

                if (redisValuesToDelete.Count <= 0) continue;

                tasks.Add(redisDatabase.HashDeleteAsync($"Payload:{tenantRegistryId}:{entityAnalysisModelId}",
                    redisValuesToDelete.ToArray()));

                tasks.Add(redisDatabase.SortedSetRemoveAsync(redisKey, redisValuesToDelete.ToArray()));

                tasks.Add(redisDatabase.HashDecrementAsync(redisKeyCount, latestCount.Name, redisValuesToDelete.Count));
            }
        }
    }
}