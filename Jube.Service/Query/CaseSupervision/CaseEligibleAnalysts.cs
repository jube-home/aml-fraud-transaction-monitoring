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
using LinqToDB.Async;

namespace Jube.Service.Query.CaseSupervision
{
    public static class CaseEligibleAnalysts
    {
        public static Task<List<string>> ListAsync(DbContext dbContext, int tenantRegistryId, Guid caseWorkflowGuid,
            Guid? statusGuid, CancellationToken token = default)
        {
            return (from u in dbContext.UserRegistry
                    join t in dbContext.UserInTenant on u.Name equals t.User
                    join rr in dbContext.RoleRegistry on u.RoleRegistryGuid equals rr.Guid
                    where t.TenantRegistryId == tenantRegistryId
                          && u.Active == 1 && rr.Active == 1
                          && (u.PasswordLocked == 0 || u.PasswordLocked == null)
                          && (u.Deleted == 0 || u.Deleted == null)
                          && (rr.Deleted == 0 || rr.Deleted == null)
                          && dbContext.RoleRegistryPermission.Any(p => p.RoleRegistryId == rr.Id
                                                                       && p.PermissionSpecificationId == 1
                                                                       && p.Active == 1
                                                                       && (p.Deleted == 0 || p.Deleted == null))
                          && dbContext.CaseWorkflowRole.Any(r => r.CaseWorkflowGuid == caseWorkflowGuid
                                                                 && r.RoleRegistryGuid == u.RoleRegistryGuid
                                                                 && (r.Deleted == 0 || r.Deleted == null))
                          && (statusGuid == null
                              || dbContext.CaseWorkflowStatusRole.Any(r => r.CaseWorkflowStatusGuid == statusGuid
                                                                           && r.RoleRegistryGuid == u.RoleRegistryGuid
                                                                           && (r.Deleted == 0 || r.Deleted == null)))
                    select u.Name!).Distinct().ToListAsync(token);
        }
    }
}
