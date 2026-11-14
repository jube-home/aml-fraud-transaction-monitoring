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
    using Concurrency;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;

    public class GraphRegistryRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public GraphRegistryRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public GraphRegistryRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<GraphRegistry> GetByNameAsync(string name, CancellationToken token = default)
        {
            return dbContext.GraphRegistry.FirstOrDefaultAsync(f =>
                f.TenantRegistryId == tenantRegistryId
                && (f.Deleted == 0 || f.Deleted == null)
                && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<GraphRegistry>> GetOrderByIdAsync(CancellationToken token = default)
        {
            return await dbContext.GraphRegistry.Where(w =>
                    w.TenantRegistryId == tenantRegistryId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<GraphRegistry>> GetActiveOrderByIdAsync(CancellationToken token = default)
        {
            return await dbContext.GraphRegistry.Where(w =>
                    w.TenantRegistryId == tenantRegistryId
                    && w.Active == 1
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task<GraphRegistry> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.GraphRegistry.FirstOrDefaultAsync(w => w.Guid == guid
                                                                    && w.TenantRegistryId == tenantRegistryId
                                                                    && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<bool> ExistsByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.GraphRegistry.AnyAsync(w => w.Guid == guid
                                                         && w.TenantRegistryId == tenantRegistryId
                                                         && w.Active == 1
                                                         && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<GraphRegistry> GetByGuidActiveOnlyAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.GraphRegistry.FirstOrDefaultAsync(w => w.Guid == guid
                                                                    && w.TenantRegistryId == tenantRegistryId
                                                                    && w.Active == 1
                                                                    && dbContext.GraphRegistryRole
                                                                        .Where(r => r.GraphRegistryGuid == w.Guid
                                                                                    && (r.Deleted == 0 || r.Deleted == null))
                                                                        .Any(r => dbContext.RoleRegistry
                                                                            .Where(rr => rr.Guid == r.RoleRegistryGuid
                                                                                         && (rr.Deleted == 0 || rr.Deleted == null))
                                                                            .Any(rr => dbContext.UserRegistry.Any(u =>
                                                                                u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                                                                    && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<bool> IsGrantedAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.GraphRegistry.AnyAsync(w => w.Guid == guid
                                                         && (w.Deleted == 0 || w.Deleted == null)
                                                         && w.TenantRegistryId == tenantRegistryId
                                                         && dbContext.GraphRegistryRole
                                                                        .Where(r => r.GraphRegistryGuid == w.Guid
                                                                                    && (r.Deleted == 0 || r.Deleted == null))
                                                                        .Any(r => dbContext.RoleRegistry
                                                                            .Where(rr => rr.Guid == r.RoleRegistryGuid
                                                                                         && (rr.Deleted == 0 || rr.Deleted == null))
                                                                            .Any(rr => dbContext.UserRegistry.Any(u =>
                                                                                u.RoleRegistryGuid == rr.Guid && u.Name == userName))), token);
        }

        public Task<GraphRegistry> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.GraphRegistry.FirstOrDefaultAsync(w => w.Id == id
                                                                    && w.TenantRegistryId == tenantRegistryId
                                                                    && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<IEnumerable<GraphRegistry>> GetByDirectoryActiveOrderByIdAsync(
            CancellationToken token = default)
        {
            return await dbContext.GraphRegistry.Where(w => (w.ContextKindId == null || w.ContextKindId == 1)
                                                            && w.Active == 1
                                                            && dbContext.GraphRegistryRole
                                                                        .Where(r => r.GraphRegistryGuid == w.Guid
                                                                                    && (r.Deleted == 0 || r.Deleted == null))
                                                                        .Any(r => dbContext.RoleRegistry
                                                                            .Where(rr => rr.Guid == r.RoleRegistryGuid
                                                                                         && (rr.Deleted == 0 || rr.Deleted == null))
                                                                            .Any(rr => dbContext.UserRegistry.Any(u =>
                                                                                u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                                                            && w.TenantRegistryId == tenantRegistryId
                                                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<GraphRegistry> InsertAsync(GraphRegistry model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<GraphRegistry> UpdateAsync(GraphRegistry model, CancellationToken token = default)
        {
            var existing = await dbContext.GraphRegistry.FirstOrDefaultAsync(w => w.Id == model.Id
                && w.TenantRegistryId == tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null)
                && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedUser = userName;
            model.UpdatedDate = DateTime.UtcNow;
            model.TenantRegistryId = tenantRegistryId;
            OptimisticConcurrency.Verify(model.Version, existing.Version);
            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.ImportId = existing.ImportId;

            await dbContext.UpdateAsync(model, token: token);

            var audit = new GraphRegistryVersion
            {
                GraphRegistryId = existing.Id,
                TenantRegistryId = existing.TenantRegistryId,
                Name = existing.Name,
                Active = existing.Active,
                Locked = existing.Locked,
                ContextKindId = existing.ContextKindId,
                DefaultLayoutId = existing.DefaultLayoutId,
                DefaultHops = existing.DefaultHops,
                DefaultWindowDays = existing.DefaultWindowDays,
                RenderCap = existing.RenderCap,
                HubPolicyId = existing.HubPolicyId,
                CreatedUser = existing.CreatedUser,
                CreatedDate = existing.CreatedDate,
                UpdatedUser = existing.UpdatedUser,
                UpdatedDate = existing.UpdatedDate,
                Deleted = existing.Deleted,
                DeletedUser = existing.DeletedUser,
                DeletedDate = existing.DeletedDate,
                Version = existing.Version,
                InheritedId = existing.InheritedId,
                ImportId = existing.ImportId
            };

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var record = await dbContext.GraphRegistry
                .FirstOrDefaultAsync(w => w.TenantRegistryId == tenantRegistryId
                                          && w.Id == id
                                          && (w.Deleted == 0 || w.Deleted == null)
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (record == null)
            {
                throw new KeyNotFoundException();
            }

            record.DeletedUser = userName;
            record.DeletedDate = DateTime.UtcNow;
            record.Deleted = 1;
            await dbContext.UpdateAsync(record, token: token);
        }
    }
}
