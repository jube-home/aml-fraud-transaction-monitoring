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

namespace Jube.Data.Query.GetCarriedPermissionsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetCarriedPermissionsQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<HashSet<int>> ForRoleAsync(Guid roleRegistryGuid, CancellationToken token = default)
        {
            var landlord = await (from rr in dbContext.RoleRegistry
                    join tr in dbContext.TenantRegistry on rr.TenantRegistryId equals tr.Id
                    where rr.Guid == roleRegistryGuid
                          && rr.TenantRegistryId == tenantRegistryId
                          && (rr.Deleted == 0 || rr.Deleted == null)
                    select tr.Landlord)
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (landlord == 1)
            {
                var all = await dbContext.PermissionSpecification.Select(s => s.Id).ToListAsync(token)
                    .ConfigureAwait(false);

                return all.ToHashSet();
            }

            var carried = await (from rrp in dbContext.RoleRegistryPermission
                    join rr in dbContext.RoleRegistry on rrp.RoleRegistryId equals rr.Id
                    where rr.Guid == roleRegistryGuid
                          && rr.TenantRegistryId == tenantRegistryId
                          && (rr.Deleted == 0 || rr.Deleted == null)
                          && rrp.Active == 1
                          && (rrp.Deleted == 0 || rrp.Deleted == null)
                          && rrp.PermissionSpecificationId != null
                    select rrp.PermissionSpecificationId.Value)
                .ToListAsync(token).ConfigureAwait(false);

            return carried.ToHashSet();
        }

        public async Task<HashSet<int>> ForRoleIdAsync(int roleRegistryId, CancellationToken token = default)
        {
            var roleRegistryGuid = await dbContext.RoleRegistry
                .Where(w => w.Id == roleRegistryId
                            && w.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .Select(w => (Guid?)w.Guid)
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (roleRegistryGuid is null)
            {
                return [];
            }

            return await ForRoleAsync(roleRegistryGuid.Value, token).ConfigureAwait(false);
        }

        public async Task<HashSet<int>> ForTenantAsync(CancellationToken token = default)
        {
            var carried = await (from rrp in dbContext.RoleRegistryPermission
                    join rr in dbContext.RoleRegistry on rrp.RoleRegistryId equals rr.Id
                    where rr.TenantRegistryId == tenantRegistryId
                          && (rr.Deleted == 0 || rr.Deleted == null)
                          && rrp.Active == 1
                          && (rrp.Deleted == 0 || rrp.Deleted == null)
                          && rrp.PermissionSpecificationId != null
                    select rrp.PermissionSpecificationId.Value)
                .ToListAsync(token).ConfigureAwait(false);

            return carried.ToHashSet();
        }

        public async Task<HashSet<int>> ForUserAsync(int userRegistryId, CancellationToken token = default)
        {
            var roleRegistryGuid = await (from ur in dbContext.UserRegistry
                    join rr in dbContext.RoleRegistry on ur.RoleRegistryGuid equals rr.Guid
                    where ur.Id == userRegistryId
                          && rr.TenantRegistryId == tenantRegistryId
                          && (rr.Deleted == 0 || rr.Deleted == null)
                          && (ur.Deleted == 0 || ur.Deleted == null)
                    select (Guid?)rr.Guid)
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (roleRegistryGuid is null)
            {
                return [];
            }

            return await ForRoleAsync(roleRegistryGuid.Value, token).ConfigureAwait(false);
        }
    }
}
