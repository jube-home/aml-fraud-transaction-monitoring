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

using Jube.Cache.Observability;
using Jube.Cache.Redis.Interfaces;
using Jube.ResilientRedisConnection;
using log4net;

namespace Jube.Cache.Redis
{
    public class CacheTtlCounterRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICacheTtlCounterRepository
    {
        public Task<double> DecrementTtlCounterCacheAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid,
            string dataName, string dataValue, double decrement)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterRepository.DecrementTtlCounterCacheAsync", async () =>
            {
                try
                {
                    var redisKey =
                        $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";
                    var redisHSetKey = $"{dataValue}";

                    var value = await resilientRedisResilientRedisDatabase
                        .HashDecrementAsync(redisKey, redisHSetKey, decrement).ConfigureAwait(false);

                    if (value > 0)
                    {
                        return value;
                    }

                    await resilientRedisResilientRedisDatabase.HashDeleteAsync(redisKey, redisHSetKey);
                    return 0;
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }

                return 0;
            });
        }

        public Task<double> GetByNameDataNameDataValueAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid, string dataName, string dataValue)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterRepository.GetByNameDataNameDataValueAsync", async () =>
            {
                try
                {
                    var redisKey =
                        $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";
                    var redisHSetKey = $"{dataValue}";
                    return (double)await resilientRedisResilientRedisDatabase.HashGetAsync(redisKey, redisHSetKey)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }

                return 0;
            });
        }

        public Task IncrementTtlCounterCacheAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string dataName,
            string dataValue,
            Guid entityAnalysisModelTtlCounterGuid, double increment, DateTime referenceDate)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterRepository.IncrementTtlCounterCacheAsync", async () =>
            {
                try
                {
                    var redisKey =
                        $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";
                    var redisHSetKey = $"{dataValue}";

                    await resilientRedisResilientRedisDatabase.HashIncrementAsync(redisKey, redisHSetKey, increment)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }
    }
}