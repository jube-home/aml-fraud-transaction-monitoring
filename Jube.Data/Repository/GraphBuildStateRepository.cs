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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;

    public class GraphBuildStateRepository(DbContext dbContext)
    {
        public async Task<GraphBuildState> GetOrCreateAsync(int tenantRegistryId, Guid graphRegistryEdgeGuid,
            CancellationToken token = default)
        {
            var existing = await dbContext.GraphBuildState.FirstOrDefaultAsync(s =>
                s.TenantRegistryId == tenantRegistryId && s.GraphRegistryEdgeGuid == graphRegistryEdgeGuid, token);

            if (existing != null)
            {
                return existing;
            }

            var created = new GraphBuildState
            {
                TenantRegistryId = tenantRegistryId,
                GraphRegistryEdgeGuid = graphRegistryEdgeGuid,
                StateId = 1
            };
            created.Id = await dbContext.InsertWithInt32IdentityAsync(created, token: token);

            return created;
        }

        public Task<List<GraphBuildState>> GetByTenantAsync(int tenantRegistryId, CancellationToken token = default)
        {
            return dbContext.GraphBuildState.Where(s => s.TenantRegistryId == tenantRegistryId).ToListAsync(token);
        }

        public Task UpdateAsync(GraphBuildState state, CancellationToken token = default)
        {
            return dbContext.UpdateAsync(state, token: token);
        }
    }
}
