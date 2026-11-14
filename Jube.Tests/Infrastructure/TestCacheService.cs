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
using Jube.Cache.Resp;
using log4net;
using StackExchange.Redis;

namespace Jube.Test.Infrastructure
{
    public static class TestCacheService
    {
        private static readonly ConnectionMultiplexer inertMultiplexer =
            ConnectionMultiplexer.Connect("127.0.0.1:59999,abortConnect=false,connectTimeout=100,connectRetry=0");

        public static CacheService Create(out FakeHybridResilientRespDatabase resp, ILog? log = null,
            string postgresConnectionString = "unused-postgres-connection-string")
        {
            resp = new FakeHybridResilientRespDatabase();
            log ??= TestLog.NoOp;

            var cacheService = (CacheService)RuntimeHelpers.GetUninitializedObject(typeof(CacheService));

            cacheService.ResilientRespResilientRespDatabase = resp;

            cacheService.CacheActivationCaseIdempotencyRepository =
                new CacheActivationCaseIdempotencyRepository(resp, log);
            cacheService.CacheActivationNotificationIdempotencyRepository =
                new CacheActivationNotificationIdempotencyRepository(resp, log);
            cacheService.CacheTtlCounterIdempotencyRepository = new CacheTtlCounterIdempotencyRepository(resp, log);
            cacheService.CacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(resp, log);
            cacheService.CacheTtlCounterRepository = new CacheTtlCounterRepository(resp, log);
            cacheService.CacheRateLimitRepository = new CacheRateLimitRepository(resp, log);
            cacheService.CacheActivationWatcherPublishSubscribe =
                new CacheActivationWatcherPublishSubscribe(resp, log);
            cacheService.CacheSanctionRepository = new CacheSanctionRepository(resp, log);
            cacheService.CacheReferenceDateRepository = new CacheReferenceDateRepository(resp, log);
            cacheService.CacheActivationWatcherLastLocationRepository =
                new CacheActivationWatcherLastLocationRepository(resp, log);

            cacheService.CachePayloadLatestRepository =
                new CachePayloadLatestRepository(postgresConnectionString, resp, log);

            cacheService.CacheWalRepository = new CacheWalRepository(resp, log);

            cacheService.CachePayloadRepository = new CachePayloadRepository(inertMultiplexer, resp,
                postgresConnectionString, log, false, true, 10_000_000,
                false, false, false,
                TimeSpan.FromHours(1), false);

            return cacheService;
        }
    }
}