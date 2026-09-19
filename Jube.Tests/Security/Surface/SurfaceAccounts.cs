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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using Jube.Test.Service.Authentication;
using LinqToDB;

namespace Jube.Test.Security.Surface;

public sealed class SurfaceAccounts(DatabaseFixture fx)
{
    public const string Password = AuthenticationTestBase.DefaultPassword;
    public const string StrongNewPassword = "Another#Horse7stapler";

    private readonly string id = Guid.NewGuid().ToString("N")[..8];
    private int counter;

    public string Prefix => $"ZzTestSurf{id}";
    public string Marker => $"ZzTestSurfUA-{id}";

    public async Task<SurfaceAccount> CreateAsync(string tag, Action<UserRegistry>? tweak = null,
        string password = Password)
    {
        var name = $"{Prefix}{tag}{Interlocked.Increment(ref counter)}";
        await using var db = fx.GetDbContext();
        var template = await db.UserRegistry.FirstAsync(f => f.Name == fx.Seed.UserWithoutPermission);
        var tenant = await db.UserInTenant.FirstAsync(f => f.User == fx.Seed.UserWithoutPermission);
        var now = DateTime.UtcNow;
        var user = new UserRegistry
        {
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = template.RoleRegistryGuid,
            Name = name,
            Email = $"{name}@example.invalid",
            Password = new Argon2PasswordHashScheme().Hash(password, AuthenticationTestBase.HashKey),
            Active = 1,
            PasswordLocked = 0,
            Deleted = 0,
            FailedPasswordCount = 0,
            PasswordCreatedDate = now,
            PasswordExpiryDate = now.AddDays(30),
            WirePasswordHash = 0,
            Version = 1,
            CreatedDate = now,
            CreatedUser = DatabaseFixture.Prefix
        };
        tweak?.Invoke(user);
        await db.InsertWithInt32IdentityAsync(user);
        await db.InsertAsync(new UserInTenant { User = name, TenantRegistryId = tenant.TenantRegistryId });
        return new SurfaceAccount(name, password);
    }

    public async Task<UserRegistry> ReloadAsync(SurfaceAccount account)
    {
        await using var db = fx.GetDbContext();
        return await db.UserRegistry.FirstAsync(f => f.Name == account.Name);
    }

    public async Task CleanupAsync()
    {
        await using var db = fx.GetDbContext();
        await db.UserLogin.Where(w =>
                w.UserAgent == Marker || (w.CreatedUser != null && w.CreatedUser.StartsWith(Prefix)))
            .DeleteAsync();
        try
        {
            await db.UserLogout.Where(w =>
                    w.UserAgent == Marker || (w.CreatedUser != null && w.CreatedUser.StartsWith(Prefix)))
                .DeleteAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
        {
        }

        await db.UserInTenant.Where(w => w.User != null && w.User.StartsWith(Prefix)).DeleteAsync();
        await db.UserRegistry.Where(w => w.Name != null && w.Name.StartsWith(Prefix)).DeleteAsync();
    }

    public string LoginJson(string userName, string password, string extra = "")
    {
        return "{\"userName\":" + JsonSerializer.Serialize(userName) + ",\"password\":" +
               JsonSerializer.Serialize(password) + extra + "}";
    }

    public Task<PenTestResponse> LoginAsync(PenTestClient client, string userName, string password,
        string extra = "", params (string Name, string Value)[] headers)
    {
        return client.PostJsonAsync("/api/Authentication/ByUserNamePassword", LoginJson(userName, password, extra),
            [.. headers, ("User-Agent", Marker)]);
    }
}