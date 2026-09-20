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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Service.Query.RegisterSignalrConnection;
using Jube.Service.Reactivity;
using Microsoft.AspNetCore.SignalR;

namespace Jube.App.Code.signalr
{
    public sealed class SignalrGroupRegistrar(IGroupManager groups, WatcherConnectionRegistry registry)
        : ISignalrGroupRegistrar
    {
        public bool IsOwnedBy(string connectionId, string userName)
        {
            return registry.IsOwnedBy(connectionId, userName);
        }

        public IReadOnlyCollection<string> TenantGroupsOf(string connectionId)
        {
            return registry.GroupsOf(connectionId).Where(TenantGroup.IsTenantGroup).ToList();
        }

        public async Task AddToGroupAsync(string connectionId, string groupName, CancellationToken token = default)
        {
            await groups.AddToGroupAsync(connectionId, groupName, token).ConfigureAwait(false);
            registry.Joined(connectionId, groupName);
        }

        public async Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken token = default)
        {
            await groups.RemoveFromGroupAsync(connectionId, groupName, token).ConfigureAwait(false);
            registry.Left(connectionId, groupName);
        }
    }
}