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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;

namespace Jube.Test.Security.Observability;

public sealed class ObservabilityUsers
{
    private static readonly SemaphoreSlim gate = new(1, 1);
    private static ObservabilityUsers? current;
    private static DatabaseFixture? owner;

    public string Only5 { get; private init; } = string.Empty;
    public string Only9 { get; private init; } = string.Empty;
    public string AllExcept27 { get; private init; } = string.Empty;
    public string AllExcept5And9 { get; private init; } = string.Empty;

    public static async Task<ObservabilityUsers> GetAsync(DatabaseFixture fx)
    {
        await gate.WaitAsync();
        try
        {
            if (current != null && ReferenceEquals(owner, fx))
            {
                return current;
            }

            owner = fx;
            current = await CreateAsync(fx);
            return current;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<ObservabilityUsers> CreateAsync(DatabaseFixture fx)
    {
        await using var db = fx.GetDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantId = await db.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
            .Select(s => s.TenantRegistryId).FirstAsync();
        var everything = Enumerable.Range(1, 43).Where(s => s != 27).ToArray();

        async Task<string> MakeAsync(string label, int[] specs)
        {
            var roleGuid = Guid.NewGuid();
            var name = $"{DatabaseFixture.Prefix}Obs{label}{suffix}";
            // ReSharper disable once AccessToDisposedClosure
            var roleId = await db.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid,
                Name = name,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            foreach (var spec in specs)
            {
                // ReSharper disable once AccessToDisposedClosure
                await db.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = spec,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            // ReSharper disable once AccessToDisposedClosure
            await db.InsertAsync(new UserRegistry
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
                CreatedUser = DatabaseFixture.Prefix
            });
            // ReSharper disable once AccessToDisposedClosure
            await db.InsertAsync(new UserInTenant { User = name, TenantRegistryId = tenantId });
            return name;
        }

        return new ObservabilityUsers
        {
            Only5 = await MakeAsync("Only5", [5]),
            Only9 = await MakeAsync("Only9", [9]),
            AllExcept27 = await MakeAsync("AllExcept27", [.. everything.Where(s => s != 27)]),
            AllExcept5And9 = await MakeAsync("AllExcept59", [.. everything.Where(s => s is not (5 or 9))])
        };
    }
}