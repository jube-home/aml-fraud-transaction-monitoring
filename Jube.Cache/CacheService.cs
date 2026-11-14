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

using Jube.Data.Repository.Models;
using System.Collections.Concurrent;
using Jube.Cache.Models;
using Jube.Cache.Resp;
using Jube.Cache.Resp.CacheUserRegistryApiKey;
using Jube.Cache.Resp.Callback;
using Jube.ResilientRespConnection.Interfaces;
using Jube.TaskCancellation;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache
{
    public class CacheService : IAsyncDisposable
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

        public CacheService(string respConnectionString,
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

            var options = ConfigurationOptions.Parse(respConnectionString);

            options.ReconnectRetryPolicy = new RespReconnectRetryPolicy(
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

            ResilientRespResilientRespDatabase =
                new ResilientRespConnection.ResilientRespConnection(ConnectionMultiplexer, postgresConnectionString,
                        hsetOffload, log)
                    .GetDatabase();

            CacheAbstractionRepository = new CacheAbstractionRepository(ResilientRespResilientRespDatabase, Log);

            CachePayloadLatestRepository =
                new CachePayloadLatestRepository(postgresConnectionString, ResilientRespResilientRespDatabase, Log);

            CacheReferenceDateRepository = new CacheReferenceDateRepository(ResilientRespResilientRespDatabase, Log);
            CacheActivationWatcherLastLocationRepository =
                new CacheActivationWatcherLastLocationRepository(ResilientRespResilientRespDatabase, Log);
            CacheSanctionRepository = new CacheSanctionRepository(ResilientRespResilientRespDatabase, Log);
            CacheTtlCounterEntryRepository =
                new CacheTtlCounterEntryRepository(ResilientRespResilientRespDatabase, Log);
            CacheTtlCounterRepository = new CacheTtlCounterRepository(ResilientRespResilientRespDatabase, Log);
            CacheRateLimitRepository = new CacheRateLimitRepository(ResilientRespResilientRespDatabase, Log);
            CacheWalRepository = new CacheWalRepository(ResilientRespResilientRespDatabase, Log);
            CacheWireReplayRepository = new CacheWireReplayRepository(ResilientRespResilientRespDatabase, Log);

            CacheUserRegistryApiKeyRepository = new CacheUserRegistryApiKeyRepository(ConnectionMultiplexer,
                ResilientRespResilientRespDatabase, Log);

            CacheTtlCounterIdempotencyRepository =
                new CacheTtlCounterIdempotencyRepository(ResilientRespResilientRespDatabase, log);
            CacheActivationCaseIdempotencyRepository =
                new CacheActivationCaseIdempotencyRepository(ResilientRespResilientRespDatabase, log);
            CacheActivationNotificationIdempotencyRepository =
                new CacheActivationNotificationIdempotencyRepository(ResilientRespResilientRespDatabase, log);
        }

        public Task InstantiateRepositoriesTask { get; set; }
        public ConnectionMultiplexer ConnectionMultiplexer { get; set; }
        public ConnectionMultiplexer SentinelMultiplexer { get; private set; }
        public string SentinelServiceName { get; private set; }
        public RespConnectionDiagnosticsCounters ConnectionDiagnostics { get; } = new();
        public IHybridResilientRespDatabase ResilientRespResilientRespDatabase { get; set; }
        public CacheAbstractionRepository CacheAbstractionRepository { get; set; }
        public CachePayloadLatestRepository CachePayloadLatestRepository { get; set; }
        public CachePayloadRepository CachePayloadRepository { get; set; }
        public CacheReferenceDateRepository CacheReferenceDateRepository { get; set; }
        public CacheActivationWatcherLastLocationRepository CacheActivationWatcherLastLocationRepository { get; set; }
        public CacheSanctionRepository CacheSanctionRepository { get; set; }
        public CacheTtlCounterEntryRepository CacheTtlCounterEntryRepository { get; set; }
        public CacheTtlCounterRepository CacheTtlCounterRepository { get; set; }
        public CacheRateLimitRepository CacheRateLimitRepository { get; set; }
        public CacheWireReplayRepository CacheWireReplayRepository { get; set; }
        public CacheCallbackPublishSubscribe CacheCallbackPublishSubscribe { get; set; }
        public CacheActivationWatcherPublishSubscribe CacheActivationWatcherPublishSubscribe { get; set; }
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

        public async Task StartAsync(TaskCoordinator taskCoordinator, Action<int, string> onActivationWatcherMessage)
        {
            CachePayloadRepository = await CachePayloadRepository.CreateAsync(ConnectionMultiplexer,
                ResilientRespResilientRespDatabase,
                postgresConnectionString, Log,
                localCache, localCacheFill, localCacheBytes, messagePackCompression,
                storePayloadCountsAndBytes, publishSubscribe, maxLruAge, activationRuleIdempotency,
                taskCoordinator.CancellationToken).ConfigureAwait(false);

            CacheCallbackPublishSubscribe = new CacheCallbackPublishSubscribe(ConnectionMultiplexer,
                ResilientRespResilientRespDatabase, callbacks, callbackTimeout, Log, taskCoordinator);

            CacheActivationWatcherPublishSubscribe = new CacheActivationWatcherPublishSubscribe(ConnectionMultiplexer,
                ResilientRespResilientRespDatabase, Log, taskCoordinator, onActivationWatcherMessage);

            Ready = true;
        }

        public async ValueTask DisposeAsync()
        {
            CachePayloadRepository?.Dispose();

            await DisposeMultiplexerAsync(ConnectionMultiplexer).ConfigureAwait(false);
            await DisposeMultiplexerAsync(SentinelMultiplexer).ConfigureAwait(false);
        }

        private async Task DisposeMultiplexerAsync(ConnectionMultiplexer multiplexer)
        {
            if (multiplexer == null)
            {
                return;
            }

            try
            {
                await multiplexer.CloseAsync(false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warn($"Cache RESP: a multiplexer could not be closed cleanly as {ex}.");
            }
            finally
            {
                await multiplexer.DisposeAsync().ConfigureAwait(false);
            }
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

            var sentinel = ConnectionMultiplexer.SentinelConnect(sentinelOptions);

            try
            {
                var master = sentinel.GetSentinelMasterConnection(dataOptions);
                sentinelMultiplexer = sentinel;

                return master;
            }
            catch
            {
                sentinel.Dispose();

                throw;
            }
        }

        private static void SubscribeToSentinelEvents(ConnectionMultiplexer sentinelMultiplexer, ILog log)
        {
            sentinelMultiplexer.GetSubscriber().Subscribe(RedisChannel.Pattern("*"),
                (channel, message) => HandleSentinelEvent(log, channel, message));
        }

        internal static void HandleSentinelEvent(ILog log, RedisChannel channel, RedisValue message)
        {
            RespSentinelEventCapture.Enqueue(
                new RespSentinelEventCaptureRecord(DateTime.UtcNow, channel, message));

            log.Warn($"Cache RESP Sentinel: event on channel {channel}: {message}.");
        }

        private static void SubscribeToConnectionDiagnostics(ConnectionMultiplexer connectionMultiplexer,
            RespConnectionDiagnosticsCounters diagnostics, ILog log)
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

        internal static void HandleConnectionFailed(RespConnectionDiagnosticsCounters diagnostics, ILog log,
            ConnectionFailedEventArgs args)
        {
            diagnostics.IncrementConnectionFailed();

            RespConnectionEventCapture.Enqueue(new RespConnectionEventCaptureRecord(
                DateTime.UtcNow, RespConnectionEventType.ConnectionFailed, args.EndPoint?.ToString(),
                args.ConnectionType, args.FailureType, null, null, args.Exception?.ToString()));

            log.Error($"Cache RESP: connection failed for endpoint {args.EndPoint} " +
                      $"({args.ConnectionType}), failure type {args.FailureType}.", args.Exception);
        }

        internal static void HandleConnectionRestored(RespConnectionDiagnosticsCounters diagnostics, ILog log,
            ConnectionFailedEventArgs args)
        {
            diagnostics.IncrementConnectionRestored();

            RespConnectionEventCapture.Enqueue(new RespConnectionEventCaptureRecord(
                DateTime.UtcNow, RespConnectionEventType.ConnectionRestored, args.EndPoint?.ToString(),
                args.ConnectionType, args.FailureType, null, null, args.Exception?.ToString()));

            log.Warn($"Cache RESP: connection restored for endpoint {args.EndPoint} " +
                     $"({args.ConnectionType}) after failure type {args.FailureType}.");
        }

        internal static void HandleErrorMessage(RespConnectionDiagnosticsCounters diagnostics, ILog log,
            RedisErrorEventArgs args)
        {
            diagnostics.IncrementErrorMessage();

            RespConnectionEventCapture.Enqueue(new RespConnectionEventCaptureRecord(
                DateTime.UtcNow, RespConnectionEventType.ErrorMessage, args.EndPoint.ToString(), null, null,
                null, args.Message, null));

            log.Error($"Cache RESP: server {args.EndPoint} returned error message {args.Message}.");
        }

        internal static void HandleInternalError(RespConnectionDiagnosticsCounters diagnostics, ILog log,
            InternalErrorEventArgs args)
        {
            diagnostics.IncrementInternalError();

            RespConnectionEventCapture.Enqueue(new RespConnectionEventCaptureRecord(
                DateTime.UtcNow, RespConnectionEventType.InternalError, args.EndPoint?.ToString(),
                args.ConnectionType, null, args.Origin, null, args.Exception.ToString()));

            log.Error($"Cache RESP: internal error on endpoint {args.EndPoint} " +
                      $"during {args.Origin}.", args.Exception);
        }

        internal static void HandleConfigurationChanged(RespConnectionDiagnosticsCounters diagnostics, ILog log,
            EndPointEventArgs args)
        {
            diagnostics.IncrementConfigurationChanged();

            RespConnectionEventCapture.Enqueue(new RespConnectionEventCaptureRecord(
                DateTime.UtcNow, RespConnectionEventType.ConfigurationChanged, args.EndPoint.ToString(), null,
                null, null, null, null));

            log.Warn($"Cache RESP: configuration changed for endpoint {args.EndPoint}.");
        }

        internal static void HandleConfigurationChangedBroadcast(RespConnectionDiagnosticsCounters diagnostics,
            ILog log, EndPointEventArgs args)
        {
            diagnostics.IncrementConfigurationChangedBroadcast();

            RespConnectionEventCapture.Enqueue(new RespConnectionEventCaptureRecord(
                DateTime.UtcNow, RespConnectionEventType.ConfigurationChangedBroadcast,
                args.EndPoint.ToString(), null, null, null, null, null));

            log.Warn($"Cache RESP: configuration change broadcast received from endpoint {args.EndPoint}.");
        }
    }
}