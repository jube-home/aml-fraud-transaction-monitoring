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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Service.Query.RegisterSignalrConnection;

namespace Jube.Test.Service.Query.RegisterSignalrConnection.Models
{
    internal sealed class CapturingRegistrar : ISignalrGroupRegistrar
    {
        public readonly List<(string ConnectionId, string Group)> Calls = [];
        public readonly List<(string ConnectionId, string Group)> Removals = [];
        public readonly Dictionary<string, string> Owners = [];
        public readonly Dictionary<string, List<string>> Joined = [];
        public bool OwnEverything { get; init; } = true;
        public bool Throw { get; init; }

        public bool IsOwnedBy(string connectionId, string userName)
        {
            return OwnEverything || (Owners.TryGetValue(connectionId, out var o) && o == userName);
        }

        public IReadOnlyCollection<string> TenantGroupsOf(string connectionId)
        {
            return Joined.TryGetValue(connectionId, out var g) ? g.ToList() : [];
        }

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken token = default)
        {
            if (Throw)
            {
                throw new InvalidOperationException("hub down");
            }

            Calls.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken token = default)
        {
            Removals.Add((connectionId, groupName));
            return Task.CompletedTask;
        }
    }
}