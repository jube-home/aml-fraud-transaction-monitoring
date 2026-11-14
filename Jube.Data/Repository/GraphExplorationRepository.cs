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

    public class GraphExplorationRepository(DbContext dbContext, int tenantRegistryId, string userName)
    {
        public Task<List<GraphExploration>> GetAsync(CancellationToken token = default)
        {
            return dbContext.GraphExploration
                .Where(e => e.TenantRegistryId == tenantRegistryId && e.User == userName && e.Deleted == 0)
                .OrderByDescending(e => e.UpdatedDate ?? e.CreatedDate).ToListAsync(token);
        }

        public Task<GraphExploration> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.GraphExploration.FirstOrDefaultAsync(e => e.Id == id && e.TenantRegistryId == tenantRegistryId
                                                                       && e.User == userName && e.Deleted == 0, token);
        }

        public Task<GraphExploration> GetByNameAsync(Guid graphRegistryGuid, string name, CancellationToken token = default)
        {
            return dbContext.GraphExploration.FirstOrDefaultAsync(e => e.TenantRegistryId == tenantRegistryId
                                                                       && e.User == userName && e.Deleted == 0
                                                                       && e.GraphRegistryGuid == graphRegistryGuid
                                                                       && e.Name == name, token);
        }

        public async Task<GraphExploration> InsertAsync(GraphExploration model, CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.User = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Deleted = 0;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<GraphExploration> UpdateAsync(GraphExploration model, CancellationToken token = default)
        {
            var existing = await GetByIdAsync(model.Id, token) ?? throw new KeyNotFoundException();
            existing.Name = model.Name;
            existing.Address = model.Address;
            existing.Pinned = model.Pinned;
            existing.GraphRegistryGuid = model.GraphRegistryGuid;
            existing.UpdatedDate = DateTime.UtcNow;
            await dbContext.UpdateAsync(existing, token: token);

            return existing;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.GraphExploration
                .Where(e => e.Id == id && e.TenantRegistryId == tenantRegistryId && e.User == userName && e.Deleted == 0)
                .Set(e => e.Deleted, (byte)1)
                .Set(e => e.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
