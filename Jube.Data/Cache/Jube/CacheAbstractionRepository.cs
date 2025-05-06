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
using Jube.Data.Cache.Postgres;
using log4net;
using JubeCache = Jube.Cache;

namespace Jube.Data.Cache.Jube;

public class CacheAbstractionRepository(JubeCache.Cache cache, ILog log) : ICacheAbstractionRepository
{
    public async Task DeleteAsync(int tenantRegistryId, int entityAnalysisModelId, string searchKey, string searchValue,
        string name)
    {
        try
        {
            var redisKey = $"Abstraction:{tenantRegistryId}:{entityAnalysisModelId}:{searchKey}:{searchValue}";
            var redisHSetKey = $"{name}";
            
            await JubeCache.HashDeleteAsync(redisKey, redisHSetKey);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task InsertAsync(int tenantRegistryId, int entityAnalysisModelId, string searchKey, string searchValue,
        string name,
        double value)
    {
        try
        {
            var redisKey = $"Abstraction:{tenantRegistryId}:{entityAnalysisModelId}:{searchKey}:{searchValue}";
            var redisHSetKey = $"{name}";

            await redisDatabase.HashSetAsync(redisKey, redisHSetKey, searchValue);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task UpdateAsync(int tenantRegistryId, int entityAnalysisModelId, string searchKey, string searchValue,
        string name,
        double value)
    {
        try
        {
            var redisKey = $"Abstraction:{tenantRegistryId}:{entityAnalysisModelId}:{searchKey}:{searchValue}";
            var redisHSetKey = $"{name}";

            await redisDatabase.HashSetAsync(redisKey, redisHSetKey, searchValue);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task<double?> GetByNameSearchNameSearchValueAsync(int tenantRegistryId, int entityAnalysisModelId,
        string name, string searchKey,
        string searchValue)
    {
        try
        {
            var redisKey = $"Abstraction:{tenantRegistryId}:{entityAnalysisModelId}:{searchKey}:{searchValue}";
            var redisHSetKey = $"{name}";
            var redisValue = await redisDatabase.HashGetAsync(redisKey, redisHSetKey);

            if (!redisValue.HasValue) return null;
            return (double) redisValue;
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return null;
    }

    public async Task<Dictionary<string, double>>
        GetByNameSearchNameSearchValueReturnValueOnlyTreatingMissingAsNullByReturnZeroRecordAsync(int tenantRegistryId,
            int entityAnalysisModelId,
            List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueDto>
                entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests)
    {
        var value = new Dictionary<string, double>();
        try
        {
            foreach (var entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest
                     in entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests)
            {
                var redisKey =
                    $"Abstraction:{tenantRegistryId}:{entityAnalysisModelId}:" +
                    $"{entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.SearchKey}:" +
                    $"{entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.SearchValue}";
                var redisHSetKey =
                    $"{entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.AbstractionRuleName}";

                var redisValue = await redisDatabase.HashGetAsync(redisKey, redisHSetKey);

                value.Add(entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.AbstractionRuleName,
                    redisValue.HasValue ? (double) redisValue : 0);
            }
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return value;
    }
}