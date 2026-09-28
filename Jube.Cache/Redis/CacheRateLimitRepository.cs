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
    public class CacheRateLimitRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICacheRateLimitRepository
    {
        private const string RedisHashField = "Count";

        public Task<long> IncrementRateLimitCacheAsync(string rateLimitName, string sourceKey, TimeSpan window)
        {
            return CacheDiagnostics.RecordAsync("CacheRateLimitRepository.IncrementRateLimitCacheAsync", async () =>
            {
                try
                {
                    var redisKey = $"RateLimit:{rateLimitName}:{sourceKey}";

                    var count = await resilientRedisResilientRedisDatabase
                        .HashIncrementAsync(redisKey, RedisHashField, 1L).ConfigureAwait(false);

                    if (count == 1)
                    {
                        await resilientRedisResilientRedisDatabase.KeyExpireAsync(redisKey, window)
                            .ConfigureAwait(false);
                    }

                    return count;
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }

                return 0L;
            });
        }
    }
}