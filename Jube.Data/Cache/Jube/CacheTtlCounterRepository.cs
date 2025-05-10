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
using System.Threading.Tasks;
using Jube.Data.Cache.Interfaces;
using log4net;
using JubeCache = Jube.Cache.Cache;

namespace Jube.Data.Cache.Jube;

public class CacheTtlCounterRepository(JubeCache cache, ILog log) : ICacheTtlCounterRepository
{
    public async Task DecrementTtlCounterCacheAsync(int tenantRegistryId, int entityAnalysisModelId,
        int entityAnalysisModelTtlCounterId,
        string dataName, string dataValue, int decrement)
    {
        try
        {
            var redisKey =
                $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelId}:{entityAnalysisModelTtlCounterId}:{dataName}";
            var redisHSetKey = $"{dataValue}";

            await cache.HashDecrementAsync(redisKey, redisHSetKey, decrement);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task<int> GetByNameDataNameDataValueAsync(int tenantRegistryId, int entityAnalysisModelId,
        int entityAnalysisModelTtlCounterId, string dataName, string dataValue)
    {
        try
        {
            var redisKey =
                $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelId}:{entityAnalysisModelTtlCounterId}:{dataName}";
            var redisHSetKey = $"{dataValue}";
            return await cache.HashGetAsync(redisKey, redisHSetKey) ?? 0;
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return 0;
    }

    public async Task IncrementTtlCounterCacheAsync(int tenantRegistryId, int entityAnalysisModelId, string dataName,
        string dataValue,
        int entityAnalysisModelTtlCounterId, int increment, DateTime referenceDate)
    {
        try
        {
            var redisKey =
                $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelId}:{entityAnalysisModelTtlCounterId}:{dataName}";
            var redisHSetKey = $"{dataValue}";

            await cache.HashIncrementAsync(redisKey, redisHSetKey, increment);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }
}