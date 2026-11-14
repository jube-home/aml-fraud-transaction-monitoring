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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Security;
using Jube.Data.Security.Models;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class RoleAllocationEffectTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private const int Users = 35;
    private const int Roles = 34;

    public override async Task DisposeAsync()
    {
        await using (var db = Fx.GetDbContext())
        {
            var roleIds = await db.RoleRegistry.Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .Select(s => s.Id).ToListAsync();

            await db.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && roleIds.Contains(w.RoleRegistryId.Value)).DeleteAsync();
        }

        await base.DisposeAsync();
    }

    private async Task<TestUser> UserWithGrantsAsync(string tag, Action<RoleRegistry>? roleTweak = null,
        Action<RoleRegistryPermission>? grantTweak = null, params int[] specs)
    {
        var role = new RoleRegistry
        {
            Guid = Guid.NewGuid(), Name = $"{Prefix}Effect{tag}", Active = 1, Locked = 0, Deleted = 0,
            TenantRegistryId = TenantAId, Version = 1, CreatedDate = DateTime.UtcNow,
            CreatedUser = DatabaseFixture.Prefix
        };

        roleTweak?.Invoke(role);

        await using (var db = Fx.GetDbContext())
        {
            var roleId = await db.InsertWithInt32IdentityAsync(role);

            foreach (var spec in specs.Length == 0 ? new[] { Users } : specs)
            {
                var grant = new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), RoleRegistryId = roleId, PermissionSpecificationId = spec, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                };

                grantTweak?.Invoke(grant);
                await db.InsertAsync(grant);
            }
        }

        return await AddUserAsync(tag, u => u.RoleRegistryGuid = role.Guid);
    }

    private async Task<PermissionValidationDto> ResolveAsync(TestUser user)
    {
        await using var db = Fx.GetDbContext();

        return await new PermissionValidation().GetPermissionsAsync(db, user.Name, Log);
    }

    private async Task SetRoleAsync(string tag, Action<RoleRegistry> change)
    {
        await using var db = Fx.GetDbContext();
        var role = await db.RoleRegistry.FirstAsync(f => f.Name == $"{Prefix}Effect{tag}");
        change(role);
        await db.UpdateAsync(role);
    }

    [Fact]
    public async Task AnActiveRoleWithAnActiveGrantYieldsExactlyTheGrantedPermissionAsync()
    {
        var user = await UserWithGrantsAsync("Baseline");
        var resolved = await ResolveAsync(user);

        resolved.Permissions.Should().BeEquivalentTo(new[] { Users });
        resolved.ActiveUser.Should().BeTrue();
        resolved.Landlord.Should().BeFalse();
    }

    [Theory]
    [InlineData("role inactive")]
    [InlineData("role deleted")]
    [InlineData("grant inactive")]
    [InlineData("grant deleted")]
    public async Task ADeactivatedOrDeletedRoleOrGrantRemovesThePermissionAsync(string state)
    {
        var tag = "State" + state.Replace(" ", string.Empty);

        var user = await UserWithGrantsAsync(tag,
            role =>
            {
                role.Active = (byte)(state == "role inactive" ? 0 : 1);
                role.Deleted = (byte)(state == "role deleted" ? 1 : 0);
            },
            grant =>
            {
                grant.Active = (byte)(state == "grant inactive" ? 0 : 1);
                grant.Deleted = (byte)(state == "grant deleted" ? 1 : 0);
            });

        var resolved = await ResolveAsync(user);

        resolved.Permissions.Should().BeEmpty();
        resolved.ActiveUser.Should().Be(!state.StartsWith("role", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeactivatingTheRoleRevokesImmediatelyAndReactivatingRestoresAsync()
    {
        var user = await UserWithGrantsAsync("Toggle");
        (await ResolveAsync(user)).Permissions.Should().BeEquivalentTo(new[] { Users });

        await SetRoleAsync("Toggle", role => role.Active = 0);
        var revoked = await ResolveAsync(user);
        revoked.Permissions.Should().BeEmpty();
        revoked.ActiveUser.Should().BeFalse();

        await SetRoleAsync("Toggle", role => role.Active = 1);
        (await ResolveAsync(user)).Permissions.Should().BeEquivalentTo(new[] { Users });
    }

    [Fact]
    public async Task LockingARoleOrAGrantFreezesEditingButDoesNotRevokeAsync()
    {
        var user = await UserWithGrantsAsync("Locked", role => role.Locked = 1, grant => grant.Locked = 1);

        (await ResolveAsync(user)).Permissions.Should().BeEquivalentTo(new[] { Users });
    }

    [Fact]
    public async Task AUserReceivesOnlyTheGrantsOfItsOwnRoleAsync()
    {
        var first = await UserWithGrantsAsync("First", null, null, Users);
        var second = await UserWithGrantsAsync("Second", null, null, Roles);

        (await ResolveAsync(first)).Permissions.Should().BeEquivalentTo(new[] { Users });
        (await ResolveAsync(second)).Permissions.Should().BeEquivalentTo(new[] { Roles });
    }
}
