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
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Test.Service.ReprocessingPermissions;

public sealed class ReprocessingPermissionUsers(DatabaseFixture fx) : IAsyncDisposable
{
    private readonly List<string> userNames = [];
    private readonly List<Guid> roleGuids = [];

    public Task<string> CreateAsync(string tag, params int[] specs)
    {
        return CreateInTenantOfAsync(fx.Seed.UserWithPermission, tag, specs);
    }

    public async Task<string> CreateInTenantOfAsync(string tenantUser, string tag, params int[] specs)
    {
        await using var db = fx.GetDbContext();
        var tenantId = await db.UserInTenant.Where(w => w.User == tenantUser)
            .Select(s => s.TenantRegistryId).FirstAsync();

        var name = $"{DatabaseFixture.Prefix}Rp{tag}{Guid.NewGuid():N}"[..40];
        var roleGuid = Guid.NewGuid();

        var roleId = await db.InsertWithInt32IdentityAsync(new RoleRegistry
        {
            Guid = roleGuid, Name = name, Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenantId,
            Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
        });

        foreach (var spec in specs)
        {
            await db.InsertAsync(new RoleRegistryPermission
            {
                Guid = Guid.NewGuid(), PermissionSpecificationId = spec, RoleRegistryId = roleId, Active = 1,
                Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
        }

        await db.InsertAsync(new UserRegistry
        {
            Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = name, Email = $"{name}@example.invalid",
            Password = "not-used-by-permission-checks", Active = 1, PasswordLocked = 0, Deleted = 0, Version = 1,
            CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
        });

        await db.InsertAsync(new UserInTenant { User = name, TenantRegistryId = tenantId });

        userNames.Add(name);
        roleGuids.Add(roleGuid);

        return name;
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = fx.GetDbContext();

        var roleIds = await db.RoleRegistry.Where(w => roleGuids.Contains(w.Guid)).Select(s => s.Id)
            .ToListAsync();

        await db.UserInTenant.Where(w => userNames.Contains(w.User)).DeleteAsync();
        await db.UserRegistry.Where(w => userNames.Contains(w.Name)).DeleteAsync();
        await db.RoleRegistryPermission
            .Where(w => w.RoleRegistryId != null && roleIds.Contains(w.RoleRegistryId.Value)).DeleteAsync();
        await db.RoleRegistry.Where(w => roleIds.Contains(w.Id)).DeleteAsync();
    }
}
