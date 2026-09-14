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
using Jube.Cache.Redis.Models;
using Jube.ResilientRedisConnection;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache.Redis
{
    public class CacheAbstractionRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICacheAbstractionRepository
    {
        public Task DeleteAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string searchKey,
            string searchValue,
            string name)
        {
            return CacheDiagnostics.RecordAsync("CacheAbstractionRepository.DeleteAsync", async () =>
            {
                try
                {
                    var redisKey =
                        $"Abstraction:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{searchKey}:{searchValue}";
                    var redisHSetKey = $"{name}";

                    await resilientRedisResilientRedisDatabase.HashDeleteAsync(redisKey, redisHSetKey)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }

        public Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string searchKey,
            string searchValue,
            string name,
            double value)
        {
            return CacheDiagnostics.RecordAsync("CacheAbstractionRepository.UpsertAsync", async () =>
            {
                try
                {
                    var redisKey =
                        $"Abstraction:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{searchKey}:{searchValue}";
                    var redisHSetKey = $"{name}";

                    await resilientRedisResilientRedisDatabase.HashSetAsync(redisKey, redisHSetKey, value)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }

        public Task<double?> GetPreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            string name, string searchKey,
            string searchValue)
        {
            return CacheDiagnostics.RecordAsync("CacheAbstractionRepository.GetPreferReplicaAsync", async () =>
            {
                try
                {
                    var redisKey =
                        $"Abstraction:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{searchKey}:{searchValue}";
                    var redisHSetKey = $"{name}";
                    var redisValue = await resilientRedisResilientRedisDatabase
                        .HashGetAsync(redisKey, redisHSetKey, CommandFlags.PreferReplica).ConfigureAwait(false);

                    if (!redisValue.HasValue)
                    {
                        return null;
                    }

                    return (double?)redisValue;
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }

                return null;
            });
        }

        public Task<Dictionary<string, double>>
            GetAsync(int tenantRegistryId,
                Guid entityAnalysisModelGuid,
                List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue>
                    entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests)
        {
            return CacheDiagnostics.RecordAsync("CacheAbstractionRepository.GetAsync", async () =>
            {
                var value = new Dictionary<string, double>();

                var groupFetchTasks = entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests
                    .GroupBy(request => (request.SearchKey, request.SearchValue))
                    .Select(async group =>
                    {
                        var abstractionRuleNames = group.Select(request => request.AbstractionRuleName).ToArray();
                        try
                        {
                            var redisKey =
                                $"Abstraction:{tenantRegistryId}:{entityAnalysisModelGuid:N}:" +
                                $"{group.Key.SearchKey}:{group.Key.SearchValue}";
                            var redisHSetKeys = Array.ConvertAll(abstractionRuleNames, name => (RedisValue)name);

                            var redisValues = await resilientRedisResilientRedisDatabase
                                .HashGetAsync(redisKey, redisHSetKeys).ConfigureAwait(false);

                            return (abstractionRuleNames, redisValues, exception: (Exception)null);
                        }
                        catch (Exception ex)
                        {
                            return (abstractionRuleNames, redisValues: null, exception: ex);
                        }
                    });

                foreach (var (abstractionRuleNames, redisValues, exception) in
                         await Task.WhenAll(groupFetchTasks).ConfigureAwait(false))
                {
                    if (exception != null)
                    {
                        log.Error($"Cache Redis: Has created an exception as {exception}.");
                        continue;
                    }

                    for (var i = 0; i < abstractionRuleNames.Length; i++)
                    {
                        value.TryAdd(abstractionRuleNames[i],
                            redisValues[i].HasValue ? (double)redisValues[i] : 0);
                    }
                }

                return value;
            });
        }
    }
}