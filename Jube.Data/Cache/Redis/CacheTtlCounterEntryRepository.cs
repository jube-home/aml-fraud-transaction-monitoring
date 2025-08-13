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
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Cache.Dto;
using Jube.Data.Cache.Interfaces;
using Jube.Extensions;
using log4net;
using StackExchange.Redis;

namespace Jube.Data.Cache.Redis;

public class CacheTtlCounterEntryRepository(
    IDatabaseAsync redisDatabase,
    ILog log,
    CommandFlags commandFlag = CommandFlags.FireAndForget) : ICacheTtlCounterEntryRepository
{
    public async Task<List<ExpiredTtlCounterEntryDto>>
        GetExpiredTtlCounterCacheCountsAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid, string dataName, DateTime referenceDate)
    {
        var expired = new List<ExpiredTtlCounterEntryDto>();
        try
        {
            var redisKeyTtlCounter =
                $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";

            foreach (var dataValue in await redisDatabase.HashKeysAsync(redisKeyTtlCounter))
            {
                var redisKeyTtlCounterEntry = $"TtlCounterEntry:{tenantRegistryId}" +
                                              $":{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}" +
                                              $":{dataName}:{dataValue}";

                foreach (var keyTtlCounterEntry in await redisDatabase.HashKeysAsync(redisKeyTtlCounterEntry))
                {
                    var referenceDateTimestamp = long.Parse(keyTtlCounterEntry).FromUnixTimeMilliSeconds();
                    if (referenceDateTimestamp >= referenceDate) continue;

                    var redisValue = await redisDatabase.HashGetAsync(redisKeyTtlCounterEntry, keyTtlCounterEntry);
                    if (redisValue.HasValue)
                        expired.Add(new ExpiredTtlCounterEntryDto
                        {
                            Value = (int)redisValue,
                            DataValue = dataValue,
                            ReferenceDate = referenceDateTimestamp
                        });
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
        Guid entityAnalysisModelGuid, Guid entityAnalysisModelTtlCounterGuid,
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
            $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}" +
            $":{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";

        return (from hashEntry in await redisDatabase.HashGetAllAsync(redisKey)
            let referenceDateTimestamp = long.Parse(hashEntry.Name)
            where referenceDateTimestamp >= referenceDateFromTimestamp
                  && referenceDateTimestamp <= referenceDateToTimestamp
            select (int)hashEntry.Value).Sum();
    }

    public async Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string dataName, string dataValue,
        Guid entityAnalysisModelTtlCounterGuid, DateTime referenceDate, int increment)
    {
        try
        {
            var redisKey =
                $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";
            var redisHSetKey = $"{referenceDate.ToUnixTimeMilliSeconds()}";

            await redisDatabase.HashIncrementAsync(redisKey, redisHSetKey, increment,
                commandFlag);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task DeleteAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
        Guid entityAnalysisModelTtlCounterGuid,
        string dataName,
        string dataValue, DateTime referenceDate)
    {
        try
        {
            var redisKey =
                $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";
            var redisHSetKey = $"{referenceDate.ToUnixTimeMilliSeconds()}";

            await redisDatabase.HashDeleteAsync(redisKey, redisHSetKey,
                commandFlag);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }
}