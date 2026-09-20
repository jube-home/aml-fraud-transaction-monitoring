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
using Jube.App.Code.signalr.Models;

namespace Jube.App.Code.signalr
{
    public sealed class WatcherConnectionRegistry
    {
        public static readonly WatcherConnectionRegistry Instance = new();

        private readonly ConcurrentDictionary<string, Entry> connections = new();

        public void Track(string connectionId, string userName, System.Action abort = null,
            string issuedMilliseconds = null)
        {
            connections[connectionId] = new Entry(userName, abort, issuedMilliseconds);
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

        public void Remove(string connectionId)
        {
            connections.TryRemove(connectionId, out _);
        }

        public bool IsOwnedBy(string connectionId, string userName)
        {
            return connections.TryGetValue(connectionId, out var entry) && entry.UserName is { } owner &&
                   string.Equals(owner, userName, System.StringComparison.Ordinal);
        }

        public IReadOnlyCollection<string> GroupsOf(string connectionId)
        {
            return connections.TryGetValue(connectionId, out var entry) ? entry.Snapshot() : [];
        }

        public void Joined(string connectionId, string groupName)
        {
            if (connections.TryGetValue(connectionId, out var entry))
            {
                entry.Add(groupName);
            }
        }

        public void Left(string connectionId, string groupName)
        {
            if (connections.TryGetValue(connectionId, out var entry))
            {
                entry.Remove(groupName);
            }
        }
    }
}