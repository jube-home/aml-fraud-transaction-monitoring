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
using Jube.Extensions;
using Jube.ResilientRedisConnection;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache.Redis
{
    public class CacheReferenceDateRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICacheReferenceDate
    {
        public Task UpsertReferenceDateAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, DateTime referenceDate)
        {
            return CacheDiagnostics.RecordAsync("CacheReferenceDateRepository.UpsertReferenceDateAsync", async () =>
            {
                try
                {
                    var redisKey = $"ReferenceDate:{tenantRegistryId}";
                    var redisHSetKey = $"{entityAnalysisModelGuid:N}";

                    await resilientRedisResilientRedisDatabase.HashSetAsync(redisKey, redisHSetKey,
                        referenceDate.ToUnixTimeMilliSeconds()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }

        public Task<DateTime?> GetReferenceDateAsync(int tenantRegistryId, Guid entityAnalysisModelGuid)
        {
            return CacheDiagnostics.RecordAsync("CacheReferenceDateRepository.GetReferenceDateAsync", async () =>
            {
                try
                {
                    var redisKey = $"ReferenceDate:{tenantRegistryId}";
                    var redisHSetKey = $"{entityAnalysisModelGuid:N}";
                    var redisValue = await resilientRedisResilientRedisDatabase
                        .HashGetAsync(redisKey, redisHSetKey).ConfigureAwait(false);

                    if (!redisValue.HasValue)
                    {
                        return null;
                    }

                    return ((long)redisValue).FromUnixTimeMilliSeconds();
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }

                return (DateTime?)null;
            });
        }

        public Task<DateTime?> GetReferenceDatePreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid)
        {
            return CacheDiagnostics.RecordAsync("CacheReferenceDateRepository.GetReferenceDatePreferReplicaAsync",
                async () =>
                {
                    try
                    {
                        var redisKey = $"ReferenceDate:{tenantRegistryId}";
                        var redisHSetKey = $"{entityAnalysisModelGuid:N}";
                        var redisValue = await resilientRedisResilientRedisDatabase
                            .HashGetAsync(redisKey, redisHSetKey, CommandFlags.PreferReplica).ConfigureAwait(false);

                        if (!redisValue.HasValue)
                        {
                            return null;
                        }

                        return ((long)redisValue).FromUnixTimeMilliSeconds();
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Cache Redis: Has created an exception as {ex}.");
                    }

                    return (DateTime?)null;
                });
        }
    }
}