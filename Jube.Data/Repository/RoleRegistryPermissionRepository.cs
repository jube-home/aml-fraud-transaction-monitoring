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
    using Poco;

    public class RoleRegistryPermissionRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public RoleRegistryPermissionRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public RoleRegistryPermissionRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<bool> ExistsRoleRegistryAsync(int roleRegistryId, CancellationToken token = default)
        {
            return dbContext.RoleRegistry.AnyAsync(w =>
                w.Id == roleRegistryId
                && w.TenantRegistryId == tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<bool> ExistsPermissionSpecificationAsync(int permissionSpecificationId,
            CancellationToken token = default)
        {
            return dbContext.PermissionSpecification.AnyAsync(w => w.Id == permissionSpecificationId, token);
        }

        public async Task<IEnumerable<RoleRegistryPermission>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.RoleRegistryPermission.Where(w => w.RoleRegistry.TenantRegistryId == tenantRegistryId
                                                                     && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync(token);
        }

        public Task<RoleRegistryPermission> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.RoleRegistryPermission.FirstOrDefaultAsync(w => w.Id == id
                                                                             && w.RoleRegistry.TenantRegistryId ==
                                                                             tenantRegistryId
                                                                             && (w.Deleted == 0 || w.Deleted == null),
                token);
        }

        public async Task<RoleRegistryPermission> InsertAsync(RoleRegistryPermission model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.Version = 1;
            model.CreatedDate = DateTime.UtcNow;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<RoleRegistryPermission> UpdateAsync(RoleRegistryPermission model,
            CancellationToken token = default)
        {
            var existing = await dbContext.RoleRegistryPermission
                .FirstOrDefaultAsync(u => u.RoleRegistry.TenantRegistryId == tenantRegistryId
                                          && u.Id == model.Id
                                          && (u.Deleted == 0 || u.Deleted == null)
                                          && (u.Locked == 0 || u.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var version = (existing.Version ?? 0) + 1;
            var updatedDate = DateTime.UtcNow;

            await dbContext.RoleRegistryPermission
                .Where(w => w.Id == model.Id)
                .Set(s => s.RoleRegistryId, model.RoleRegistryId)
                .Set(s => s.PermissionSpecificationId, model.PermissionSpecificationId)
                .Set(s => s.Active, model.Active)
                .Set(s => s.Locked, model.Locked)
                .Set(s => s.UpdatedDate, updatedDate)
                .Set(s => s.UpdatedUser, userName)
                .Set(s => s.Version, version)
                .UpdateAsync(token);

            var audit = new RoleRegistryPermissionVersion
            {
                RoleRegistryPermissionId = existing.Id,
                RoleRegistryId = existing.RoleRegistryId,
                PermissionSpecificationId = existing.PermissionSpecificationId,
                Active = existing.Active,
                Locked = existing.Locked,
                CreatedDate = existing.CreatedDate,
                CreatedUser = existing.CreatedUser,
                UpdatedDate = existing.UpdatedDate,
                UpdatedUser = existing.UpdatedUser,
                Version = existing.Version,
            };

            await dbContext.InsertAsync(audit, token: token);

            model.Guid = existing.Guid;
            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedUser = userName;
            model.UpdatedDate = updatedDate;
            model.Version = version;

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.RoleRegistryPermission
                .Where(d =>
                    d.RoleRegistry.TenantRegistryId == tenantRegistryId
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

        public async Task<IEnumerable<RoleRegistryPermission>> GetByRoleRegistryIdOrderByIdAsync(
            int roleRegistryId, CancellationToken token = default)
        {
            return await dbContext.RoleRegistryPermission
                .Where(w =>
                    w.RoleRegistry.TenantRegistryId == tenantRegistryId
                    && w.RoleRegistry.Id == roleRegistryId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.RoleRegistryPermission
                .Where(d => d.RoleRegistry.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}