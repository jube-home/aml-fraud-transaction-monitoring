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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using Jube.App.Code.Models;
using log4net;

namespace Jube.App.Code.Sse
{
    public class SseConnectionRegistry(ILog log) : IRevocableConnectionRegistry
    {
        private readonly ConcurrentDictionary<string, SseConnectionEntry> connections = new();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, SseConnectionEntry>>
            connectionsByTenant = new();

        public Channel<string> Track(string connectionId, string userName, string issuedMilliseconds,
            int tenantRegistryId, CancellationTokenSource cancellationTokenSource)
        {
            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

            var entry = new SseConnectionEntry(userName, issuedMilliseconds, tenantRegistryId, channel,
                cancellationTokenSource);

            connections[connectionId] = entry;
            connectionsByTenant.GetOrAdd(tenantRegistryId, _ => new ConcurrentDictionary<string, SseConnectionEntry>())
                [connectionId] = entry;

            return channel;
        }

        public void Remove(string connectionId)
        {
            if (!connections.TryRemove(connectionId, out var entry))
            {
                return;
            }

            if (connectionsByTenant.TryGetValue(entry.TenantRegistryId, out var tenantConnections))
            {
                tenantConnections.TryRemove(connectionId, out _);
            }
        }

        public IReadOnlyList<TrackedConnection> Tracked()
        {
            return connections
                .Select(c => new TrackedConnection(c.Key, c.Value.UserName, c.Value.IssuedMilliseconds))
                .ToList();
        }

        public void Abort(string connectionId)
        {
            if (connections.TryGetValue(connectionId, out var entry))
            {
                entry.Abort();
            }
        }

        public void AbortUser(string userName)
        {
            foreach (var connection in connections.Where(c =>
                         string.Equals(c.Value.UserName, userName, System.StringComparison.Ordinal)))
            {
                connection.Value.Abort();
            }
        }

        public bool IsRevoked(string connectionId)
        {
            return connections.TryGetValue(connectionId, out var entry) && entry.Revoked;
        }

        public void Dispatch(int tenantRegistryId, string payload)
        {
            if (!connectionsByTenant.TryGetValue(tenantRegistryId, out var tenantConnections))
            {
                return;
            }

            foreach (var (connectionId, entry) in tenantConnections)
            {
                try
                {
                    entry.Channel.Writer.TryWrite(payload);
                }
                catch (System.Exception ex)
                {
                    log.Info($"SseConnectionRegistry: could not dispatch to connection {connectionId}: {ex}.");
                }
            }
        }
    }
}