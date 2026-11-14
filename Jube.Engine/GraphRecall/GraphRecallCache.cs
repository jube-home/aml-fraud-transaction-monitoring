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

namespace Jube.Engine.GraphRecall
{
    public sealed class GraphRecallCache
    {
        private readonly ConcurrentDictionary<int, GraphRecallSnapshot> snapshots = new();

        public bool LoadedForStartup { get; set; }

        public IReadOnlyCollection<int> Tenants => (IReadOnlyCollection<int>)snapshots.Keys;

        public void Publish(GraphRecallSnapshot snapshot)
        {
            snapshots[snapshot.TenantRegistryId] = snapshot;
        }

        public bool Remove(int tenantRegistryId)
        {
            return snapshots.TryRemove(tenantRegistryId, out _);
        }

        public bool TryGet(int tenantRegistryId, out GraphRecallSnapshot snapshot)
        {
            return snapshots.TryGetValue(tenantRegistryId, out snapshot);
        }
    }
}
