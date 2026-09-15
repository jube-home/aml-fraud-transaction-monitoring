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
using Jube.Data.Repository;
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
            options.ReconnectRetryPolicy = new RedisReconnectRetryPolicy(
                new ExponentialRetry(reconnectRetryBaseDelayMilliseconds, reconnectRetryMaxDelayMilliseconds),
                log);

            if (backlogFailFast)
            {
                options.BacklogPolicy = BacklogPolicy.FailFast;
            }

            if (string.IsNullOrEmpty(options.ServiceName))
            {
                ConnectionMultiplexer = ConnectionMultiplexer.Connect(options);
            }
            else
            {
                ConnectionMultiplexer = ConnectViaSentinelWithSplitTimeouts(options,
                    sentinelConnectTimeoutMilliseconds, sentinelSyncTimeoutMilliseconds, sentinelConnectRetry,
                    out var sentinelMultiplexer);
                SentinelMultiplexer = sentinelMultiplexer;
                SentinelServiceName = options.ServiceName;
                SubscribeToSentinelEvents(sentinelMultiplexer, log);
            }

            SubscribeToConnectionDiagnostics(ConnectionMultiplexer, ConnectionDiagnostics, log);
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
        public ConnectionMultiplexer SentinelMultiplexer { get; private set; }
        public string SentinelServiceName { get; private set; }
        public RedisConnectionDiagnosticsCounters ConnectionDiagnostics { get; } = new();
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
            int sentinelConnectTimeoutMilliseconds, int sentinelSyncTimeoutMilliseconds, int sentinelConnectRetry,
            out ConnectionMultiplexer sentinelMultiplexer)
        {
            var sentinelOptions = dataOptions.Clone();
            sentinelOptions.ConnectTimeout = sentinelConnectTimeoutMilliseconds;
            sentinelOptions.SyncTimeout = sentinelSyncTimeoutMilliseconds;
            sentinelOptions.ConnectRetry = sentinelConnectRetry;
            sentinelOptions.AbortOnConnectFail = false;

            sentinelMultiplexer = ConnectionMultiplexer.SentinelConnect(sentinelOptions);
            return sentinelMultiplexer.GetSentinelMasterConnection(dataOptions);
        }

        private static void SubscribeToSentinelEvents(ConnectionMultiplexer sentinelMultiplexer, ILog log)
        {
            sentinelMultiplexer.GetSubscriber().Subscribe(RedisChannel.Pattern("*"),
                (channel, message) => HandleSentinelEvent(log, channel, message));
        }

        internal static void HandleSentinelEvent(ILog log, RedisChannel channel, RedisValue message)
        {
            RedisSentinelEventCapture.Enqueue(
                new RedisSentinelEventCaptureRecord(DateTime.UtcNow, channel, message));
            log.Warn($"Cache Redis Sentinel: event on channel {channel}: {message}.");
        }

        private static void SubscribeToConnectionDiagnostics(ConnectionMultiplexer connectionMultiplexer,
            RedisConnectionDiagnosticsCounters diagnostics, ILog log)
        {
            connectionMultiplexer.ConnectionFailed += (_, args) => HandleConnectionFailed(diagnostics, log, args);
            connectionMultiplexer.ConnectionRestored += (_, args) => HandleConnectionRestored(diagnostics, log, args);
            connectionMultiplexer.ErrorMessage += (_, args) => HandleErrorMessage(diagnostics, log, args);
            connectionMultiplexer.InternalError += (_, args) => HandleInternalError(diagnostics, log, args);
            connectionMultiplexer.ConfigurationChanged +=
                (_, args) => HandleConfigurationChanged(diagnostics, log, args);
            connectionMultiplexer.ConfigurationChangedBroadcast +=
                (_, args) => HandleConfigurationChangedBroadcast(diagnostics, log, args);
        }

        internal static void HandleConnectionFailed(RedisConnectionDiagnosticsCounters diagnostics, ILog log,
            ConnectionFailedEventArgs args)
        {
            diagnostics.IncrementConnectionFailed();
            RedisConnectionEventCapture.Enqueue(new RedisConnectionEventCaptureRecord(
                DateTime.UtcNow, RedisConnectionEventType.ConnectionFailed, args.EndPoint?.ToString(),
                args.ConnectionType, args.FailureType, null, null, args.Exception?.ToString()));
            log.Error($"Cache Redis: connection failed for endpoint {args.EndPoint} " +
                      $"({args.ConnectionType}), failure type {args.FailureType}.", args.Exception);
        }

        internal static void HandleConnectionRestored(RedisConnectionDiagnosticsCounters diagnostics, ILog log,
            ConnectionFailedEventArgs args)
        {
            diagnostics.IncrementConnectionRestored();
            RedisConnectionEventCapture.Enqueue(new RedisConnectionEventCaptureRecord(
                DateTime.UtcNow, RedisConnectionEventType.ConnectionRestored, args.EndPoint?.ToString(),
                args.ConnectionType, args.FailureType, null, null, args.Exception?.ToString()));
            log.Warn($"Cache Redis: connection restored for endpoint {args.EndPoint} " +
                     $"({args.ConnectionType}) after failure type {args.FailureType}.");
        }

        internal static void HandleErrorMessage(RedisConnectionDiagnosticsCounters diagnostics, ILog log,
            RedisErrorEventArgs args)
        {
            diagnostics.IncrementErrorMessage();
            RedisConnectionEventCapture.Enqueue(new RedisConnectionEventCaptureRecord(
                DateTime.UtcNow, RedisConnectionEventType.ErrorMessage, args.EndPoint.ToString(), null, null,
                null, args.Message, null));
            log.Error($"Cache Redis: server {args.EndPoint} returned error message {args.Message}.");
        }

        internal static void HandleInternalError(RedisConnectionDiagnosticsCounters diagnostics, ILog log,
            InternalErrorEventArgs args)
        {
            diagnostics.IncrementInternalError();
            RedisConnectionEventCapture.Enqueue(new RedisConnectionEventCaptureRecord(
                DateTime.UtcNow, RedisConnectionEventType.InternalError, args.EndPoint?.ToString(),
                args.ConnectionType, null, args.Origin, null, args.Exception.ToString()));
            log.Error($"Cache Redis: internal error on endpoint {args.EndPoint} " +
                      $"during {args.Origin}.", args.Exception);
        }

        internal static void HandleConfigurationChanged(RedisConnectionDiagnosticsCounters diagnostics, ILog log,
            EndPointEventArgs args)
        {
            diagnostics.IncrementConfigurationChanged();
            RedisConnectionEventCapture.Enqueue(new RedisConnectionEventCaptureRecord(
                DateTime.UtcNow, RedisConnectionEventType.ConfigurationChanged, args.EndPoint.ToString(), null,
                null, null, null, null));
            log.Warn($"Cache Redis: configuration changed for endpoint {args.EndPoint}.");
        }

        internal static void HandleConfigurationChangedBroadcast(RedisConnectionDiagnosticsCounters diagnostics,
            ILog log, EndPointEventArgs args)
        {
            diagnostics.IncrementConfigurationChangedBroadcast();
            RedisConnectionEventCapture.Enqueue(new RedisConnectionEventCaptureRecord(
                DateTime.UtcNow, RedisConnectionEventType.ConfigurationChangedBroadcast,
                args.EndPoint.ToString(), null, null, null, null, null));
            log.Warn($"Cache Redis: configuration change broadcast received from endpoint {args.EndPoint}.");
        }
    }
}