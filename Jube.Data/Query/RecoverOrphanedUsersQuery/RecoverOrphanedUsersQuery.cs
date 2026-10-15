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

namespace Jube.Data.Query.RecoverOrphanedUsersQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;
    using Repository;

    public class RecoverOrphanedUsersQuery(DbContext dbContext, int? tenantRegistryId, string userName)
    {
        public async Task ExecuteAsync(CancellationToken token = default)
        {
            if (!tenantRegistryId.HasValue)
            {
                throw new KeyNotFoundException();
            }

            var namesInTenant = await dbContext.UserInTenant
                .Where(w => w.TenantRegistryId == tenantRegistryId)
                .Select(s => s.User)
                .ToListAsync(token);

            var orphanedUsers = await dbContext.UserRegistry
                .Where(w => namesInTenant.Contains(w.Name)
                            && (w.Deleted == 0 || w.Deleted == null)
                            && !dbContext.RoleRegistry.Any(r =>
                                r.Guid == w.RoleRegistryGuid && (r.Deleted == 0 || r.Deleted == null)))
                .ToListAsync(token);

            if (orphanedUsers.Count == 0)
            {
                return;
            }

            var userRegistryRepository = new UserRegistryRepository(dbContext, userName);
            var roleRegistryRepository = new RoleRegistryRepository(dbContext, userName);

            var roleRegistry = await roleRegistryRepository.InsertAsync(new RoleRegistry
            {
                Name = "Orphaned Preservation Import",
                Active = 1
            }, token);

            var permissionSpecifications = await new PermissionSpecificationRepository(dbContext).GetAsync(token);

            var roleRegistryPermissionRepository = new RoleRegistryPermissionRepository(dbContext, userName);

            foreach (var permissionSpecification in permissionSpecifications)
            {
                await roleRegistryPermissionRepository.InsertAsync(new RoleRegistryPermission
                {
                    RoleRegistryId = roleRegistry.Id,
                    PermissionSpecificationId = permissionSpecification.Id,
                    Active = 1
                }, token);
            }

            foreach (var orphanedUser in orphanedUsers)
            {
                orphanedUser.RoleRegistryGuid = roleRegistry.Guid;

                orphanedUser.Active = orphanedUser.Name == userName
                    ? (byte)1
                    : (byte)0;

                await userRegistryRepository.UpdateAsync(orphanedUser, token);
            }
        }
    }
}