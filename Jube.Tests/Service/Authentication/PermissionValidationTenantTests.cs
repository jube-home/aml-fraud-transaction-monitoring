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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Security;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class PermissionValidationTenantTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    [Fact]
    public async Task PermissionsCarryTheTenantTheUserIsInAsync()
    {
        var inA = await AddUserAsync("TenantA");
        var inB = await AddUserAsync("TenantB", tenantB: true);

        await using var db = Fx.GetDbContext();

        (await new PermissionValidation().GetPermissionsAsync(db, inA.Name, Log)).TenantRegistryId
            .Should().Be(TenantAId);

        (await new PermissionValidation().GetPermissionsAsync(db, inB.Name, Log)).TenantRegistryId
            .Should().Be(TenantBId);
    }

    [Fact]
    public async Task AUserWithNoTenantRowReportsTenantZeroAsync()
    {
        var user = await AddUserAsync("NoTenant", inTenant: false);

        await using var db = Fx.GetDbContext();

        (await new PermissionValidation().GetPermissionsAsync(db, user.Name, Log)).TenantRegistryId
            .Should().Be(0);
    }

    private async Task SetRoleActiveAsync(byte active)
    {
        await using var db = Fx.GetDbContext();

        await db.RoleRegistry.Where(w => w.Name == $"{Prefix}RoleA").Set(w => w.Active, active).UpdateAsync();
    }

    [Fact]
    public async Task ALandlordWithAnInactiveRoleHasNoPermissionsAndIsNotALandlordAsync()
    {
        var user = await AddUserAsync("LandlordInactiveRole");
        await SetTenantAsync(t => t.Landlord = 1);
        await SetRoleActiveAsync(0);

        await using var db = Fx.GetDbContext();
        var resolved = await new PermissionValidation().GetPermissionsAsync(db, user.Name, Log);

        resolved.Landlord.Should().BeFalse();
        resolved.Permissions.Should().BeEmpty();
        resolved.ActiveUser.Should().BeFalse();
    }

    [Fact]
    public async Task ALandlordWithAnActiveRoleStillHasEveryPermissionAsync()
    {
        var user = await AddUserAsync("LandlordActiveRole");
        await SetTenantAsync(t => t.Landlord = 1);

        await using var db = Fx.GetDbContext();
        var resolved = await new PermissionValidation().GetPermissionsAsync(db, user.Name, Log);
        var all = await db.PermissionSpecification.Select(s => s.Id).ToListAsync();

        resolved.Landlord.Should().BeTrue();
        resolved.Permissions.Should().BeEquivalentTo(all);
    }

    [Fact]
    public async Task ANonLandlordWithAnInactiveRoleStillHasNoPermissionsAsync()
    {
        var user = await AddUserAsync("NonLandlordInactiveRole");
        await SetRoleActiveAsync(0);

        await using var db = Fx.GetDbContext();
        var resolved = await new PermissionValidation().GetPermissionsAsync(db, user.Name, Log);

        resolved.Landlord.Should().BeFalse();
        resolved.Permissions.Should().BeEmpty();
    }
}
