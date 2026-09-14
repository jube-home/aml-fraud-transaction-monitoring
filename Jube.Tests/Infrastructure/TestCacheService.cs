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
using System.Runtime.CompilerServices;
using Jube.Cache;
using Jube.Cache.Redis;
using log4net;
using StackExchange.Redis;

namespace Jube.Test.Infrastructure
{
    public static class TestCacheService
    {
        private static readonly ConnectionMultiplexer inertMultiplexer =
            ConnectionMultiplexer.Connect("127.0.0.1:59999,abortConnect=false,connectTimeout=100,connectRetry=0");

        public static CacheService Create(out FakeHybridResilientRedisDatabase redis, ILog? log = null,
            string postgresConnectionString = "unused-postgres-connection-string")
        {
            redis = new FakeHybridResilientRedisDatabase();
            log ??= TestLog.NoOp;

            var cacheService = (CacheService)RuntimeHelpers.GetUninitializedObject(typeof(CacheService));

            cacheService.ResilientRedisResilientRedisDatabase = redis;
            cacheService.CacheActivationCaseIdempotencyRepository =
                new CacheActivationCaseIdempotencyRepository(redis, log);
            cacheService.CacheActivationNotificationIdempotencyRepository =
                new CacheActivationNotificationIdempotencyRepository(redis, log);
            cacheService.CacheTtlCounterIdempotencyRepository = new CacheTtlCounterIdempotencyRepository(redis, log);
            cacheService.CacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(redis, log);
            cacheService.CacheTtlCounterRepository = new CacheTtlCounterRepository(redis, log);
            cacheService.CacheSanctionRepository = new CacheSanctionRepository(redis, log);
            cacheService.CacheReferenceDateRepository = new CacheReferenceDateRepository(redis, log);
            cacheService.CachePayloadLatestRepository =
                new CachePayloadLatestRepository(postgresConnectionString, redis, log);
            cacheService.CacheWalRepository = new CacheWalRepository(redis, log);
            cacheService.CachePayloadRepository = new CachePayloadRepository(inertMultiplexer, redis,
                postgresConnectionString, log, false, true, 10_000_000,
                false, false, false,
                TimeSpan.FromHours(1), false);

            return cacheService;
        }
    }
}