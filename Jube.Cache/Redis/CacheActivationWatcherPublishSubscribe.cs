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
using Jube.ResilientRedisConnection;
using Jube.TaskCancellation;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache.Redis
{
    public class CacheActivationWatcherPublishSubscribe
    {
        public readonly Task SubscriptionTask;
        public bool Ready { get; private set; }
        private readonly ConnectionMultiplexer connectionMultiplexer;
        private readonly ILog log;
        private readonly Action<int, string> onMessage;
        private readonly IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase;

        public CacheActivationWatcherPublishSubscribe(
            ConnectionMultiplexer connectionMultiplexer,
            IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
            ILog log,
            TaskCoordinator taskCoordinator,
            Action<int, string> onMessage)
        {
            this.connectionMultiplexer = connectionMultiplexer;
            this.resilientRedisResilientRedisDatabase = resilientRedisResilientRedisDatabase;
            this.log = log;
            this.onMessage = onMessage;

            SubscriptionTask = taskCoordinator.RunAsync("ActivationWatcherListenAsync",
                _ => StartSubscriptionAsync(taskCoordinator.CancellationToken));
        }

        internal CacheActivationWatcherPublishSubscribe(
            IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
            ILog log)
        {
            this.resilientRedisResilientRedisDatabase = resilientRedisResilientRedisDatabase;
            this.log = log;
            SubscriptionTask = Task.CompletedTask;
            Ready = true;
        }

        private async Task StartSubscriptionAsync(CancellationToken token)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            var channel = RedisChannel.Pattern("ActivationWatcher:*");

            try
            {
                await subscriber.SubscribeAsync(channel, (ch, msg) => HandleMessage(ch, msg));
                log.Info("Subscribed to Redis activation watcher events.");
                Ready = true;

                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    log.Info("Graceful cancellation of Redis activation watcher subscription.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Redis activation watcher subscription error: {ex}");
            }
            finally
            {
                Ready = false;
                await subscriber.UnsubscribeAsync(channel);
                log.Info("Unsubscribed from Redis activation watcher events.");
            }
        }

        private void HandleMessage(string channel, string payload)
        {
            try
            {
                var tenantRegistryId = int.Parse(channel.Split(':')[1]);
                onMessage(tenantRegistryId, payload);
            }
            catch (Exception ex)
            {
                log.Info($"Failed to parse activation watcher channel {channel} with error {ex}.");
            }
        }

        public Task PublishAsync(int tenantRegistryId, string payload, CancellationToken token = default)
        {
            return CacheDiagnostics.RecordAsync("CacheActivationWatcherPublishSubscribe.PublishAsync", async () =>
            {
                try
                {
                    await resilientRedisResilientRedisDatabase.PublishAsync(
                        RedisChannel.Pattern($"ActivationWatcher:{tenantRegistryId}"), payload,
                        CommandFlags.FireAndForget);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }
    }
}