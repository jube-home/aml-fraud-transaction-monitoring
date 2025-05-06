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
using Jube.Extensions;
using log4net;
using StackExchange.Redis;

namespace Jube.Data.Cache.Jube;

public class CacheReferenceDate(IDatabaseAsync redisDatabase, ILog log) : ICacheReferenceDate
{
    public async Task UpsertReferenceDate(int tenantRegistryId, int entityAnalysisModelId, DateTime referenceDate)
    {
        try
        {
            var redisKey = $"ReferenceDate:{tenantRegistryId}";
            var redisHSetKey = $"{entityAnalysisModelId}";

            await redisDatabase.HashSetAsync(redisKey, redisHSetKey, referenceDate.ToUnixTimeMilliSeconds());
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task<DateTime?> GetReferenceDate(int tenantRegistryId, int entityAnalysisModelId)
    {
        try
        {
            var redisKey = $"ReferenceDate:{tenantRegistryId}";
            var redisHSetKey = $"{entityAnalysisModelId}";
            var referenceDateTimestamp = (long) await redisDatabase.HashGetAsync(redisKey, redisHSetKey);
            return referenceDateTimestamp.FromUnixTimeMilliSeconds();
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return null;
    }
}