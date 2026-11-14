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

using Jube.Data.Context;
using Jube.Data.Query.GetCarriedPermissionsQuery;

namespace Jube.Service.Security
{
    public sealed class PrivilegeEscalationGuard(
        DbContext dbContext,
        int tenantRegistryId,
        PermissionValidation callerPermissions)
    {
        private readonly GetCarriedPermissionsQuery query = new(dbContext, tenantRegistryId);

        public bool MayGrant(int? permissionSpecificationId)
        {
            return callerPermissions.Landlord
                   || (permissionSpecificationId is not null && callerPermissions.Holds(permissionSpecificationId.Value));
        }

        public async Task<bool> MayAssignRoleAsync(Guid roleRegistryGuid, CancellationToken token = default)
        {
            if (callerPermissions.Landlord)
            {
                return true;
            }

            var carried = await query.ForRoleAsync(roleRegistryGuid, token).ConfigureAwait(false);

            return callerPermissions.HoldsAll(carried);
        }

        public async Task<bool> MayEditRoleAsync(int roleRegistryId, CancellationToken token = default)
        {
            if (callerPermissions.Landlord)
            {
                return true;
            }

            var carried = await query.ForRoleIdAsync(roleRegistryId, token).ConfigureAwait(false);

            return callerPermissions.HoldsAll(carried);
        }

        public async Task<bool> MayActAsUserAsync(int userRegistryId, CancellationToken token = default)
        {
            if (callerPermissions.Landlord)
            {
                return true;
            }

            var carried = await query.ForUserAsync(userRegistryId, token).ConfigureAwait(false);

            return callerPermissions.HoldsAll(carried);
        }
    }
}
