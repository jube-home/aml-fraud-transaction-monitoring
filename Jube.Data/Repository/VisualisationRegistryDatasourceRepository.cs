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
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class VisualisationRegistryDatasourceRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public VisualisationRegistryDatasourceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public VisualisationRegistryDatasourceRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<VisualisationRegistryDatasource> GetByNameVisualisationRegistryIdAsync(string name,
            int visualisationRegistryId, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryDatasource
                .FirstOrDefaultAsync(f =>
                    f.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                    && f.VisualisationRegistryId == visualisationRegistryId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<VisualisationRegistryDatasource>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public async Task<IEnumerable<VisualisationRegistryDatasource>> GetByVisualisationRegistryIdOrderByIdAsync(
            int visualisationRegistryId, CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && w.VisualisationRegistryId == visualisationRegistryId &&
                            (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<VisualisationRegistryDatasource>>
            GetByVisualisationRegistryIdOrderByPriorityAsync(
                int visualisationRegistryId, CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && w.VisualisationRegistryId == visualisationRegistryId &&
                            (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Priority).ToListAsync(token);
        }

        public async Task<IEnumerable<VisualisationRegistryDatasource>> GetByVisualisationRegistryIdActiveOnlyAsync(
            int visualisationRegistryId, CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && (w.VisualisationRegistry.Deleted == 0 ||
                                w.VisualisationRegistry.Deleted == null)
                            && w.VisualisationRegistryId == visualisationRegistryId
                            && w.Active == 1
                            && (w.Deleted == 0 || w.Deleted == null)
                            && dbContext.VisualisationRegistryRole
                                .Where(r => r.VisualisationRegistryGuid == w.VisualisationRegistry.Guid
                                            && (r.Deleted == 0 || r.Deleted == null))
                                .Any(r => dbContext.RoleRegistry
                                    .Where(rr =>
                                        rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                    .Any(rr => dbContext.UserRegistry
                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                            && dbContext.VisualisationRegistryDatasourceRole
                                .Where(r => r.VisualisationRegistryDatasourceGuid == w.Guid
                                            && (r.Deleted == 0 || r.Deleted == null))
                                .Any(r => dbContext.RoleRegistry
                                    .Where(rr =>
                                        rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                    .Any(rr => dbContext.UserRegistry
                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                ).OrderBy(o => o.Priority).ToListAsync(token);
        }

        public Task<VisualisationRegistryDatasource> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryDatasource.FirstOrDefaultAsync(w
                => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                   && w.Id == id
                   && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<VisualisationRegistryDatasource> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryDatasource.FirstOrDefaultAsync(w
                => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                   && w.Guid == guid
                   && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<VisualisationRegistryDatasource> GetByIdActiveOnlyAsync(int id, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && (w.VisualisationRegistry.Deleted == 0 ||
                                w.VisualisationRegistry.Deleted == null)
                            && w.Active == 1
                            && w.Id == id
                            && (w.Deleted == 0 || w.Deleted == null)
                            && dbContext.VisualisationRegistryRole
                                .Where(r => r.VisualisationRegistryGuid == w.VisualisationRegistry.Guid
                                            && (r.Deleted == 0 || r.Deleted == null))
                                .Any(r => dbContext.RoleRegistry
                                    .Where(rr =>
                                        rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                    .Any(rr => dbContext.UserRegistry
                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                            && dbContext.VisualisationRegistryDatasourceRole
                                .Where(r => r.VisualisationRegistryDatasourceGuid == w.Guid
                                            && (r.Deleted == 0 || r.Deleted == null))
                                .Any(r => dbContext.RoleRegistry
                                    .Where(rr =>
                                        rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                    .Any(rr => dbContext.UserRegistry
                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                ).FirstOrDefaultAsync(token);
        }

        public async Task<VisualisationRegistryDatasource> InsertAsync(VisualisationRegistryDatasource model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.VisualisationRegistryDatasource
                .Where(d => d.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.Locked == 0 || d.Locked == null)
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

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryDatasource
                .Where(d => d.VisualisationRegistry.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}