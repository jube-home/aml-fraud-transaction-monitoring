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
using System.Threading.Tasks;
using Jube.Data.Cache.Interfaces;
using Jube.Extensions;
using log4net;
using JubeCache = Jube.Cache.Cache;

namespace Jube.Data.Cache.Jube;

public class CacheTtlCounterEntryRepository(JubeCache cache, ILog log) : ICacheTtlCounterEntryRepository
{
    public async Task<List<Postgres.CacheTtlCounterEntryRepository.ExpiredTtlCounterEntryDto>>
        GetExpiredTtlCounterCacheCountsAsync(int tenantRegistryId, int entityAnalysisModelId,
            int entityAnalysisModelTtlCounterId, string dataName, DateTime referenceDate)
    {
        var expired = new List<Postgres.CacheTtlCounterEntryRepository.ExpiredTtlCounterEntryDto>();
        try
        {
            var redisKeyTtlCounter =
                $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelId}:{entityAnalysisModelTtlCounterId}:{dataName}";

            foreach (var dataValue in await cache.HashKeysAsync(redisKeyTtlCounter))
            {
                var redisKeyTtlCounterEntry = $"TtlCounterEntry:{tenantRegistryId}" +
                                              $":{entityAnalysisModelId}:{entityAnalysisModelTtlCounterId}" +
                                              $":{dataName}:{dataValue}";

                foreach (var keyTtlCounterEntry in await cache.HashKeysAsync(redisKeyTtlCounterEntry))
                {
                    var referenceDateTimestamp = long.Parse(keyTtlCounterEntry).FromUnixTimeMilliSeconds();
                    if (referenceDateTimestamp >= referenceDate) continue;

                    var redisValue = await cache.HashGetIntAsync(redisKeyTtlCounterEntry, keyTtlCounterEntry);
                    if (redisValue.HasValue)
                    {
                        expired.Add(new Postgres.CacheTtlCounterEntryRepository.ExpiredTtlCounterEntryDto
                        {
                            Value = redisValue.Value,
                            DataValue = dataValue,
                            ReferenceDate = referenceDateTimestamp
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return expired;
    }

    public async Task<int> GetAsync(int tenantRegistryId,
        int entityAnalysisModelId, int entityAnalysisModelTtlCounterId,
        string dataName, string dataValue,
        DateTime referenceDateFrom, DateTime referenceDateTo)
    {
        try
        {
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        var referenceDateFromTimestamp = referenceDateFrom.ToUnixTimeMilliSeconds();
        var referenceDateToTimestamp = referenceDateTo.ToUnixTimeMilliSeconds();

        var redisKey =
            $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelId}" +
            $":{entityAnalysisModelTtlCounterId}:{dataName}:{dataValue}";
        //
        // var s = await cache.HashGetAllAsync(redisKey);
        //
        // return (from hashEntry in await redisDatabase.HashGetAllAsync(redisKey)
        //     let referenceDateTimestamp = long.Parse(hashEntry.Name)
        //     where referenceDateTimestamp >= referenceDateFromTimestamp
        //           && referenceDateTimestamp <= referenceDateToTimestamp
        //     select (int) hashEntry.Value).Sum();
        return 0;
    }

    public async Task UpsertAsync(int tenantRegistryId, int entityAnalysisModelId, string dataName, string dataValue,
        int entityAnalysisModelTtlCounterId, DateTime referenceDate, int increment)
    {
        try
        {
            var redisKey =
                $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelId}:{entityAnalysisModelTtlCounterId}:{dataName}:{dataValue}";
            var redisHSetKey = $"{referenceDate.ToUnixTimeMilliSeconds()}";

            await cache.HashIncrementAsync(redisKey, redisHSetKey, increment);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task DeleteAsync(int tenantRegistryId, int entityAnalysisModelId, int entityAnalysisModelTtlCounterId,
        string dataName,
        string dataValue, DateTime referenceDate)
    {
        try
        {
            var redisKey =
                $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelId}:{entityAnalysisModelTtlCounterId}:{dataName}:{dataValue}";
            var redisHSetKey = $"{referenceDate.ToUnixTimeMilliSeconds()}";

            await cache.HashDeleteAsync(redisKey, redisHSetKey);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }
}