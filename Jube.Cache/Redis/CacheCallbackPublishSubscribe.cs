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

using System.Collections.Concurrent;
using System.Net;
using Jube.Cache.Observability;
using Jube.ResilientRedisConnection;
using Jube.TaskCancellation;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache.Redis
{
    public class CacheCallbackPublishSubscribe
    {
        public readonly Task CallbackRemoveSubscriptionTask;

        public readonly Task CallbackSetSubscriptionTask;
        public readonly Task CallbackTimeoutTask;
        public readonly ConcurrentDictionary<Guid, TaskCompletionSource<Callback.Callback>> Callbacks;
        private readonly int callbackTimeout;
        private readonly ConnectionMultiplexer connectionMultiplexer;
        private readonly string localCacheInstanceGuidString;
        private readonly ILog log;
        private readonly ConcurrentDictionary<Guid, DateTime> pendingSince = new();
        private readonly IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase;

        public CacheCallbackPublishSubscribe(ConnectionMultiplexer connectionMultiplexer,
            IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback.Callback>> callbacks,
            int callbackTimeout,
            ILog log,
            TaskCoordinator taskCoordinator)
        {
            this.connectionMultiplexer = connectionMultiplexer;
            this.resilientRedisResilientRedisDatabase = resilientRedisResilientRedisDatabase;
            Callbacks = callbacks;
            this.log = log;
            this.callbackTimeout = callbackTimeout;
            localCacheInstanceGuidString = Guid.NewGuid().ToString("N");
            CallbackSetSubscriptionTask = taskCoordinator.RunAsync("CallbackListenAddAsync",
                _ => StartCallbackSubscriptionAddAsync(taskCoordinator.CancellationToken));
            CallbackRemoveSubscriptionTask = taskCoordinator.RunAsync("CallbackListenRemoveAsync",
                _ => StartCallbackSubscriptionRemoveAsync(taskCoordinator.CancellationToken));
            CallbackTimeoutTask = taskCoordinator.RunAsync("CallbackListenRemoveAsync",
                _ => StartCallbackTimeoutAsync(taskCoordinator.CancellationToken));
        }

        internal CacheCallbackPublishSubscribe(
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback.Callback>> callbacks,
            ILog log,
            string localCacheInstanceGuidString,
            IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase = null)
        {
            Callbacks = callbacks;
            this.log = log;
            this.localCacheInstanceGuidString = localCacheInstanceGuidString;
            this.resilientRedisResilientRedisDatabase = resilientRedisResilientRedisDatabase;
        }

        private async Task<Task> StartCallbackTimeoutAsync(CancellationToken token)
        {
            const int pollDelay = 1000;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Callback Timeout Management: Starting to inspect pending callbacks.");
                    }

                    var threshold = DateTime.UtcNow.AddMilliseconds(-callbackTimeout);

                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Callback Timeout Management: Threshold for timeout is {threshold} and it has been offset from now by {callbackTimeout} ms.");
                    }

                    SweepExpiredCallbacks(threshold);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error(
                        $"Callback Timeout Management: Has created an error trying to timeout callbacks {ex}.");
                }

                await Task.Delay(pollDelay, token);
            }

            return Task.FromResult(Task.CompletedTask);
        }

        internal void SweepExpiredCallbacks(DateTime threshold)
        {
            var stillPending = new HashSet<Guid>();

            foreach (var pendingCallback in Callbacks)
            {
                var guid = pendingCallback.Key;
                var tcs = pendingCallback.Value;

                if (tcs.Task.IsCompletedSuccessfully)
                {
                    pendingSince.TryRemove(guid, out _);

#pragma warning disable VSTHRD002
                    var callback = tcs.Task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

                    if (callback.CreatedDate > threshold)
                    {
                        continue;
                    }

                    if (log.IsDebugEnabled)
                    {
                        log.Debug($"Callback Timeout Management: Expired callback {guid} found.");
                    }

                    Callbacks.TryRemove(pendingCallback);

                    if (log.IsDebugEnabled)
                    {
                        log.Debug($"Callback Timeout Management: Expired callback {guid} removed..");
                    }

                    continue;
                }

                stillPending.Add(guid);
                var firstSeenPending = pendingSince.GetOrAdd(guid, static _ => DateTime.UtcNow);

                if (firstSeenPending > threshold)
                {
                    continue;
                }

                if (log.IsDebugEnabled)
                {
                    log.Debug($"Callback Timeout Management: Pending callback {guid} has never arrived and is " +
                              "past its timeout; faulting it and removing it.");
                }

                tcs.TrySetException(
                    new TimeoutException($"Callback {guid} was never received within the configured timeout."));
                Callbacks.TryRemove(pendingCallback);
                pendingSince.TryRemove(guid, out _);
            }

            foreach (var trackedGuid in pendingSince.Keys)
            {
                if (!stillPending.Contains(trackedGuid))
                {
                    pendingSince.TryRemove(trackedGuid, out _);
                }
            }
        }

        internal void AddToDictionaryFromSubscription(string channel, byte[] value)
        {
            try
            {
                var splits = channel.Split(":");
                if (splits[1] == Dns.GetHostName() && splits[2] == localCacheInstanceGuidString)
                {
                    return;
                }

                var guid = Guid.Parse(splits[3]);
                var tenantRegistryId = int.Parse(splits[4]);
                AddToDictionary(value, guid, tenantRegistryId);
            }
            catch (Exception ex)
            {
                log.Info($"Failed to parse message from channel {channel} with error {ex}.");
            }
        }

        internal void AddToDictionary(byte[] value, Guid guid, int tenantRegistryId)
        {
            var callback = new Callback.Callback
            {
                CreatedDate = DateTime.UtcNow,
                Payload = value,
                TenantRegistryId = tenantRegistryId
            };

            var tcs = Callbacks.GetOrAdd(guid,
                _ => new TaskCompletionSource<Callback.Callback>(TaskCreationOptions.RunContinuationsAsynchronously));
            tcs.TrySetResult(callback);
        }

        internal void RemoveFromDictionaryFromSubscription(string channel, RedisValue value)
        {
            try
            {
                var splits = channel.Split(":");

                if (splits[1] == Dns.GetHostName() && splits[2] == localCacheInstanceGuidString)
                {
                    return;
                }

                var guid = Guid.Parse(value.ToString());
                Callbacks.TryRemove(guid, out _);
            }
            catch (Exception ex)
            {
                log.Info($"Failed to parse redis value to guid {value} with error {ex}.");
            }
        }

        private async Task StartCallbackSubscriptionAddAsync(CancellationToken token = default)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            var channel = RedisChannel.Pattern("CallbackSet:*");

            try
            {
                await subscriber.SubscribeAsync(channel, (ch, msg) => AddToDictionaryFromSubscription(ch, msg));
                log.Info("Subscribed to Redis callbacks.");

                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    log.Info("Graceful cancellation of Redis callback subscription.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Redis callback subscription error: {ex}");
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channel);
                log.Info("Unsubscribed from Redis callbacks.");
            }
        }

        private async Task StartCallbackSubscriptionRemoveAsync(CancellationToken token = default)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            var channel = RedisChannel.Pattern("CallbackRemove:*");

            try
            {
                await subscriber.SubscribeAsync(channel, (ch, msg) => RemoveFromDictionaryFromSubscription(ch, msg));
                log.Info("Subscribed to Redis callbacks.");

                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    log.Info("Graceful cancellation of Redis callback subscription.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Redis callback subscription error: {ex}");
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channel);
                log.Info("Unsubscribed from Redis callbacks.");
            }
        }

        public Task PublishAsync(byte[] json, Guid entityAnalysisModelInstanceEntryGuid,
            int tenantRegistryId, CancellationToken token = default)
        {
            return CacheDiagnostics.RecordAsync("CacheCallbackPublishSubscribe.PublishAsync", async () =>
            {
                try
                {
                    AddToDictionary(json, entityAnalysisModelInstanceEntryGuid, tenantRegistryId);

                    await resilientRedisResilientRedisDatabase.PublishAsync(
                        RedisChannel.Pattern(
                            $"CallbackSet:{Dns.GetHostName()}:{localCacheInstanceGuidString}:{entityAnalysisModelInstanceEntryGuid:N}:{tenantRegistryId}"),
                        json);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache SQL: Has created an exception as {ex}.");
                }
            });
        }

        public Task DeleteAsync(Guid entityAnalysisModelInstanceEntryGuid, CancellationToken token = default)
        {
            return CacheDiagnostics.RecordAsync("CacheCallbackPublishSubscribe.DeleteAsync", async () =>
            {
                try
                {
                    Callbacks.TryRemove(entityAnalysisModelInstanceEntryGuid, out _);

                    await resilientRedisResilientRedisDatabase.PublishAsync(
                        RedisChannel.Pattern($"CallbackRemove:{Dns.GetHostName()}:{localCacheInstanceGuidString}")
                        , new RedisValue(entityAnalysisModelInstanceEntryGuid.ToString("N")));
                }
                catch (Exception ex)
                {
                    log.Error($"Cache SQL: Has created an exception as {ex}.");
                }
            });
        }
    }
}