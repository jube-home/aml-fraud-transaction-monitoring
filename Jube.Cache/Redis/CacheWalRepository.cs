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
using StackExchange.Redis;

namespace Jube.Cache.Redis
{
    public class CacheWalRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICacheWalRepository
    {
        public Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelInstanceEntryGuid, string node, byte[] bytes)
        {
            return CacheDiagnostics.RecordAsync("CacheWalRepository.InsertAsync", async () =>
            {
                try
                {
                    var redisKey = $"Wal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{node}";
                    var redisHSetKey = $"{entityAnalysisModelInstanceEntryGuid:N}";
                    await resilientRedisResilientRedisDatabase.HashSetAsync(redisKey, redisHSetKey, bytes)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }

        public Task FlushWalAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string node,
            Guid[] entityAnalysisModelInstanceEntryGuids)
        {
            return CacheDiagnostics.RecordAsync("CacheWalRepository.FlushWalAsync", async () =>
            {
                try
                {
                    var redisKey = $"Wal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{node}";
                    await resilientRedisResilientRedisDatabase.HashDeleteAsync(redisKey,
                        entityAnalysisModelInstanceEntryGuids.Select(x => (RedisValue)x.ToString("N"))
                            .ToArray()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }

        public Task<long> GetWalSizeAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string node)
        {
            return CacheDiagnostics.RecordAsync("CacheWalRepository.GetWalSizeAsync", () =>
            {
                var redisKey = $"Wal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{node}";
                return resilientRedisResilientRedisDatabase.HashLengthAsync(redisKey);
            });
        }
    }
}