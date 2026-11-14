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

using System.Net;
using Jube.Cache.Observability;
using Jube.Cache.Resp.CacheUserRegistryApiKey.Events;
using Jube.ResilientRespConnection.Interfaces;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache.Resp.CacheUserRegistryApiKey
{
    public class CacheUserRegistryApiKeyRepository
    {
        private readonly ConnectionMultiplexer connectionMultiplexer;
        private readonly ILog log;
        private readonly IHybridResilientRespDatabase resilientRespResilientRespDatabase;

        public CacheUserRegistryApiKeyRepository(ConnectionMultiplexer connectionMultiplexer,
            IHybridResilientRespDatabase resilientRespResilientRespDatabase,
            ILog log)
        {
            this.connectionMultiplexer =
                connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));

            this.resilientRespResilientRespDatabase = resilientRespResilientRespDatabase;
            this.log = log;

            SubscribeToRespHashEvents();
        }

        internal CacheUserRegistryApiKeyRepository(IHybridResilientRespDatabase resilientRespResilientRespDatabase,
            ILog log)
        {
            this.resilientRespResilientRespDatabase = resilientRespResilientRespDatabase;
            this.log = log;
        }

        public event EventHandler<CaseUserRegistryKeyEventArguments> OnCaseUserRegistryApiKeySetEvent;
        public event EventHandler<CaseUserRegistryKeyEventArguments> OnCaseUserRegistryApiKeyRemoveEvent;

        public Task PublishSetAsync(string apiKeyHash)
        {
            return CacheDiagnostics.RecordAsync("CacheUserRegistryApiKeyRepository.PublishSetAsync", async () =>
            {
                try
                {
                    await resilientRespResilientRespDatabase.PublishAsync(
                        RedisChannel.Pattern($"UserRegistryApiKeySet:{Dns.GetHostName()}")
                        , new RedisValue(apiKeyHash));
                }
                catch (Exception ex)
                {
                    log.Error($"Cache RESP: Has created an exception as {ex}.");
                }
            });
        }

        public Task PublishRemoveAsync(string apiKeyHash)
        {
            return CacheDiagnostics.RecordAsync("CacheUserRegistryApiKeyRepository.PublishRemoveAsync", async () =>
            {
                try
                {
                    await resilientRespResilientRespDatabase.PublishAsync(
                        RedisChannel.Pattern($"UserRegistryApiKeyRemove:{Dns.GetHostName()}")
                        , new RedisValue(apiKeyHash));
                }
                catch (Exception ex)
                {
                    log.Error($"Cache RESP: Has created an exception as {ex}.");
                }
            });
        }

        private void SubscribeToRespHashEvents()
        {
            {
                SubscribeToSet();
                SubscribeToRemove();
            }

            return;

            void SubscribeToSet()
            {
                var subscriber = connectionMultiplexer.GetSubscriber();

                subscriber.Subscribe(RedisChannel.Pattern("UserRegistryApiKeySet:*"),
                    (_, value) => HandleSetMessage(value));
            }

            void SubscribeToRemove()
            {
                var subscriber = connectionMultiplexer.GetSubscriber();

                subscriber.Subscribe(RedisChannel.Pattern("UserRegistryApiKeyRemove:*"),
                    (_, value) => HandleRemoveMessage(value));
            }
        }

        internal void HandleSetMessage(RedisValue value)
        {
            OnCaseUserRegistryApiKeySetEvent?.Invoke(this, new CaseUserRegistryKeyEventArguments
            {
                ApiKeyHash = value
            });
        }

        internal void HandleRemoveMessage(RedisValue value)
        {
            OnCaseUserRegistryApiKeyRemoveEvent?.Invoke(this, new CaseUserRegistryKeyEventArguments
            {
                ApiKeyHash = value
            });
        }
    }
}