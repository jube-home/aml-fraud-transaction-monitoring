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
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Test.Service.Query.CaseChecklist
{
    public sealed class CaseChecklistTestUsers
    {
        private readonly List<Guid> roleGuids = [];
        private readonly List<int> roleIds = [];
        private readonly List<string> userNames = [];

        public string Holder { get; private set; } = string.Empty;
        public string Other { get; private set; } = string.Empty;

        public static async Task<CaseChecklistTestUsers> CreateAsync(DbContext dbContext, string workflowName,
            int tenantRegistryId)
        {
            var users = new CaseChecklistTestUsers();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid,
                Name = $"zzChkRole{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "zzChk"
            });

            users.roleIds.Add(roleId);
            users.roleGuids.Add(roleGuid);

            await dbContext.InsertAsync(new RoleRegistryPermission
            {
                Guid = Guid.NewGuid(),
                PermissionSpecificationId = 1,
                RoleRegistryId = roleId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "zzChk"
            });

            users.Holder = await AddUserAsync(dbContext, users, $"zzChkHolder{suffix}", roleGuid, tenantRegistryId);
            users.Other = await AddUserAsync(dbContext, users, $"zzChkOther{suffix}", roleGuid, tenantRegistryId);

            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Name == workflowName).Select(s => s.Guid)
                .FirstAsync();

            await dbContext.InsertAsync(new CaseWorkflowRole
            {
                Guid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                RoleRegistryGuid = roleGuid,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "zzChk",
                Deleted = 0,
                Version = 1
            });

            var statusGuids = await (from s in dbContext.CaseWorkflowStatus
                join w in dbContext.CaseWorkflow on s.CaseWorkflowId equals w.Id
                where w.Guid == workflowGuid
                select s.Guid).ToListAsync();

            foreach (var statusGuid in statusGuids)
            {
                await dbContext.InsertAsync(new CaseWorkflowStatusRole
                {
                    Guid = Guid.NewGuid(),
                    CaseWorkflowStatusGuid = statusGuid,
                    RoleRegistryGuid = roleGuid,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = "zzChk",
                    Deleted = 0,
                    Version = 1
                });
            }

            return users;
        }

        public async Task DeleteAsync(DbContext dbContext)
        {
            await dbContext.CaseWorkflowStatusRole.Where(w => roleGuids.Contains(w.RoleRegistryGuid)).DeleteAsync();
            await dbContext.CaseWorkflowRole.Where(w => roleGuids.Contains(w.RoleRegistryGuid)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => userNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => userNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission.Where(w => w.RoleRegistryId != null && roleIds.Contains(w.RoleRegistryId.Value)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => roleIds.Contains(w.Id)).DeleteAsync();
        }

        private static async Task<string> AddUserAsync(DbContext dbContext, CaseChecklistTestUsers users, string name,
            Guid roleGuid, int tenantRegistryId)
        {
            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = name,
                Email = $"{name}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "zzChk"
            });

            await dbContext.InsertAsync(new UserInTenant { User = name, TenantRegistryId = tenantRegistryId });
            users.userNames.Add(name);

            return name;
        }
    }
}
