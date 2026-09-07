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

// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Collections.Concurrent;
using Jube.Cache.Redis;
using Jube.Cache.Redis.CacheUserRegistryApiKey;
using Jube.Cache.Redis.Callback;
using Jube.ResilientRedisConnection;
using Jube.TaskCancellation;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache
{
    public class CacheService
    {
        private readonly bool activationRuleIdempotency;
        private readonly int callbackTimeout;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks;
        private readonly bool localCache;
        private readonly long localCacheBytes;
        private readonly bool localCacheFill;
        private readonly TimeSpan maxLruAge;
        private readonly bool messagePackCompression;
        private readonly string postgresConnectionString;
        private readonly bool publishSubscribe;
        private readonly bool storePayloadCountsAndBytes;

        public CacheService(string redisConnectionString,
            string postgresConnectionString,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks, int callbackTimeout,
            bool localCache, bool localCacheFill, long localCacheBytes,
            bool messagePackCompression, bool storePayloadCountsAndBytes, bool publishSubscribe,
            bool hsetOffload,
            TimeSpan maxLruAge,
            bool activationRuleIdempotency, ILog log,
            int sentinelConnectTimeoutMilliseconds = 2000,
            int sentinelSyncTimeoutMilliseconds = 2000,
            int sentinelConnectRetry = 1,
            int reconnectRetryBaseDelayMilliseconds = 100,
            int reconnectRetryMaxDelayMilliseconds = 3000,
            bool backlogFailFast = false)
        {
            Log = log;
            this.localCache = localCache;
            this.localCacheFill = localCacheFill;
            this.localCacheBytes = localCacheBytes;
            this.messagePackCompression = messagePackCompression;
            this.storePayloadCountsAndBytes = storePayloadCountsAndBytes;
            this.publishSubscribe = publishSubscribe;
            this.callbacks = callbacks;
            this.callbackTimeout = callbackTimeout;
            this.postgresConnectionString = postgresConnectionString;
            this.maxLruAge = maxLruAge;
            this.activationRuleIdempotency = activationRuleIdempotency;

            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.ReconnectRetryPolicy = new ExponentialRetry(
                reconnectRetryBaseDelayMilliseconds,
                reconnectRetryMaxDelayMilliseconds);

            if (backlogFailFast) options.BacklogPolicy = BacklogPolicy.FailFast;

            ConnectionMultiplexer = string.IsNullOrEmpty(options.ServiceName)
                ? ConnectionMultiplexer.Connect(options)
                : ConnectViaSentinelWithSplitTimeouts(options, sentinelConnectTimeoutMilliseconds,
                    sentinelSyncTimeoutMilliseconds, sentinelConnectRetry);
            SubscribeToConnectionDiagnostics(ConnectionMultiplexer, log);
            ResilientRedisResilientRedisDatabase =
                new ResilientRedisConnection.ResilientRedisConnection(ConnectionMultiplexer, postgresConnectionString,
                        hsetOffload, log)
                    .GetDatabase();

            CacheAbstractionRepository = new CacheAbstractionRepository(ResilientRedisResilientRedisDatabase, Log);
            CachePayloadLatestRepository =
                new CachePayloadLatestRepository(postgresConnectionString, ResilientRedisResilientRedisDatabase, Log);
            CacheReferenceDateRepository = new CacheReferenceDateRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheSanctionRepository = new CacheSanctionRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheTtlCounterEntryRepository =
                new CacheTtlCounterEntryRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheTtlCounterRepository = new CacheTtlCounterRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheWalRepository = new CacheWalRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheUserRegistryApiKeyRepository = new CacheUserRegistryApiKeyRepository(ConnectionMultiplexer,
                ResilientRedisResilientRedisDatabase, Log);
            CacheTtlCounterIdempotencyRepository =
                new CacheTtlCounterIdempotencyRepository(ResilientRedisResilientRedisDatabase, log);
            CacheActivationCaseIdempotencyRepository =
                new CacheActivationCaseIdempotencyRepository(ResilientRedisResilientRedisDatabase, log);
            CacheActivationNotificationIdempotencyRepository =
                new CacheActivationNotificationIdempotencyRepository(ResilientRedisResilientRedisDatabase, log);
        }

        public Task InstantiateRepositoriesTask { get; set; }
        public ConnectionMultiplexer ConnectionMultiplexer { get; set; }
        public IHybridResilientRedisDatabase ResilientRedisResilientRedisDatabase { get; set; }
        public CacheAbstractionRepository CacheAbstractionRepository { get; set; }
        public CachePayloadLatestRepository CachePayloadLatestRepository { get; set; }
        public CachePayloadRepository CachePayloadRepository { get; set; }
        public CacheReferenceDateRepository CacheReferenceDateRepository { get; set; }
        public CacheSanctionRepository CacheSanctionRepository { get; set; }
        public CacheTtlCounterEntryRepository CacheTtlCounterEntryRepository { get; set; }
        public CacheTtlCounterRepository CacheTtlCounterRepository { get; set; }
        public CacheCallbackPublishSubscribe CacheCallbackPublishSubscribe { get; set; }
        public CacheWalRepository CacheWalRepository { get; set; }
        public CacheUserRegistryApiKeyRepository CacheUserRegistryApiKeyRepository { get; set; }
        public CacheTtlCounterIdempotencyRepository CacheTtlCounterIdempotencyRepository { get; set; }
        public CacheActivationCaseIdempotencyRepository CacheActivationCaseIdempotencyRepository { get; set; }

        public CacheActivationNotificationIdempotencyRepository CacheActivationNotificationIdempotencyRepository
        {
            get;
            set;
        }

        public bool Ready { get; private set; }

        private ILog Log { get; }

        public async Task StartAsync(TaskCoordinator taskCoordinator)
        {
            CachePayloadRepository = await CachePayloadRepository.CreateAsync(ConnectionMultiplexer,
                ResilientRedisResilientRedisDatabase,
                postgresConnectionString, Log,
                localCache, localCacheFill, localCacheBytes, messagePackCompression,
                storePayloadCountsAndBytes, publishSubscribe, maxLruAge, activationRuleIdempotency,
                taskCoordinator.CancellationToken).ConfigureAwait(false);

            CacheCallbackPublishSubscribe = new CacheCallbackPublishSubscribe(ConnectionMultiplexer,
                ResilientRedisResilientRedisDatabase, callbacks, callbackTimeout, Log, taskCoordinator);

            Ready = true;
        }

        private static ConnectionMultiplexer ConnectViaSentinelWithSplitTimeouts(ConfigurationOptions dataOptions,
            int sentinelConnectTimeoutMilliseconds, int sentinelSyncTimeoutMilliseconds, int sentinelConnectRetry)
        {
            var sentinelOptions = dataOptions.Clone();
            sentinelOptions.ConnectTimeout = sentinelConnectTimeoutMilliseconds;
            sentinelOptions.SyncTimeout = sentinelSyncTimeoutMilliseconds;
            sentinelOptions.ConnectRetry = sentinelConnectRetry;
            sentinelOptions.AbortOnConnectFail = false;

            var sentinelMultiplexer = ConnectionMultiplexer.SentinelConnect(sentinelOptions);
            return sentinelMultiplexer.GetSentinelMasterConnection(dataOptions);
        }

        private static void SubscribeToConnectionDiagnostics(ConnectionMultiplexer connectionMultiplexer, ILog log)
        {
            connectionMultiplexer.ConnectionFailed += (_, args) =>
                log.Error($"Cache Redis: connection failed for endpoint {args.EndPoint} " +
                          $"({args.ConnectionType}), failure type {args.FailureType}.", args.Exception);

            connectionMultiplexer.ConnectionRestored += (_, args) =>
                log.Warn($"Cache Redis: connection restored for endpoint {args.EndPoint} " +
                         $"({args.ConnectionType}) after failure type {args.FailureType}.");

            connectionMultiplexer.ErrorMessage += (_, args) =>
                log.Error($"Cache Redis: server {args.EndPoint} returned error message {args.Message}.");

            connectionMultiplexer.InternalError += (_, args) =>
                log.Error($"Cache Redis: internal error on endpoint {args.EndPoint} " +
                          $"during {args.Origin}.", args.Exception);

            connectionMultiplexer.ConfigurationChanged += (_, args) =>
                log.Warn($"Cache Redis: configuration changed for endpoint {args.EndPoint}.");

            connectionMultiplexer.ConfigurationChangedBroadcast += (_, args) =>
                log.Warn($"Cache Redis: configuration change broadcast received from endpoint {args.EndPoint}.");
        }
    }
}