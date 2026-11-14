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
    using Helpers;
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;

    public class GraphRegistryRoleRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public GraphRegistryRoleRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public GraphRegistryRoleRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<bool> ExistsGraphRegistryAsync(Guid graphRegistryGuid, CancellationToken token = default)
        {
            return dbContext.GraphRegistry.AnyAsync(w =>
                w.Guid == graphRegistryGuid
                && w.TenantRegistryId == tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<bool> ExistsRoleRegistryAsync(Guid roleRegistryGuid, CancellationToken token = default)
        {
            return dbContext.RoleRegistry.AnyAsync(w =>
                w.Guid == roleRegistryGuid
                && w.TenantRegistryId == tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<List<GraphRegistryRole>> GetByGraphRegistryGuidAsync(Guid graphRegistryGuid,
            CancellationToken token = default)
        {
            return dbContext.GraphRegistryRole.Where(w =>
                w.GraphRegistry.TenantRegistryId == tenantRegistryId
                && w.GraphRegistryGuid == graphRegistryGuid
                && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public async Task<GraphRegistryRole> InsertAsync(GraphRegistryRole model, CancellationToken token = default)
        {
            var existing = await dbContext.GraphRegistryRole.FirstOrDefaultAsync(w =>
                    w.GraphRegistryGuid == model.GraphRegistryGuid && w.RoleRegistryGuid == model.RoleRegistryGuid
                    && (w.Deleted == 0 || w.Deleted == null),
                token);

            if (existing != null)
            {
                return existing;
            }

            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;

            try
            {
                model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            }
            catch (Exception ex) when (GrantInsertion.IsUniqueViolation(ex))
            {
                var raced = await dbContext.GraphRegistryRole.FirstOrDefaultAsync(w =>
                    w.GraphRegistryGuid == model.GraphRegistryGuid && w.RoleRegistryGuid == model.RoleRegistryGuid
                    && (w.Deleted == 0 || w.Deleted == null), token);

                if (raced == null)
                {
                    throw;
                }

                return raced;
            }

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.GraphRegistryRole
                .Where(d => d.GraphRegistry.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
