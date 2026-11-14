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
using Jube.Service.Authentication;
using Jube.Service.Authentication.Models;
using Jube.Service.Exceptions.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using AuthEngine = Jube.Service.Authentication.Authentication;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class TenantLockoutTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private const int ProbePermission = 1;

    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    public async Task SignIn_ForADisabledTenant_IsTheSameUnauthorisedOutcomeAsBadCredentialsAsync(string state)
    {
        var user = await AddUserAsync("TenantOff");
        await DisableTenantAsync(state);

        var outcome = await LoginAsync(user.Name, user.Password);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        outcome.Should().Be(AuthenticationOutcome.Unauthorized());
        Cookies.Issued.Should().BeEmpty();
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    public async Task SignIn_ForADisabledTenant_DoesDummyHashWorkOnly_SoTimingDoesNotRevealTheStateAsync(string state)
    {
        var user = await AddUserAsync("TenantTiming");
        await DisableTenantAsync(state);

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Unauthorized);

        Hash.VerifyCalls.Should().Be(1);
        Hash.RealVerifyCalls.Should().Be(0);
        Hash.DummyVerifyCalls.Should().Be(1);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(2);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    public async Task NegotiateSignIn_ForADisabledTenant_IsRefusedAsync(string state)
    {
        var user = await AddUserAsync("TenantNegotiate");
        await DisableTenantAsync(state);
        await using var db = Fx.GetDbContext();

        var act = () => new AuthEngine(db, false, null, Hash, Clock)
            .AuthenticateByNegotiateAsync(user.Name, "10.0.0.1", Marker, false);

        await act.Should().ThrowAsync<NotActiveException>();
    }

    [Fact]
    public async Task SignIn_ReturnsOnceTheTenantIsReactivatedAsync()
    {
        var user = await AddUserAsync("TenantBack");
        await DisableTenantAsync("inactive");

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Unauthorized);

        await SetTenantAsync(t => t.Active = 1);

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task SignIn_IsUnaffectedForAUserOfAnotherTenantAsync()
    {
        var other = await AddUserAsync("TenantOther", tenantB: true);
        await DisableTenantAsync("inactive");

        (await LoginAsync(other.Name, other.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    public async Task SignIn_IsNeverRefusedForALandlordTenantAsync(string state)
    {
        var user = await AddUserAsync("TenantLandlord");
        await SetTenantAsync(t => t.Landlord = 1);
        await DisableTenantAsync(state);

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    public async Task AnExistingSession_IsBlockedTheMomentTheTenantIsDisabledAsync(string state)
    {
        var user = await AddUserAsync("TenantSession");
        var issued = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        await using var db = Fx.GetDbContext();

        (await TokenValidity.IsRevokedAsync(db, user.Name, issued, null)).Should().BeFalse();

        await DisableTenantAsync(state);

        (await TokenValidity.IsRevokedAsync(db, user.Name, issued, null)).Should().BeTrue();
        (await TokenValidity.GetSessionStateAsync(db, user.Name)).Blocked.Should().BeTrue();

        (await TokenValidity.GetSessionStatesAsync(db, [user.Name]))[user.Name].Blocked.Should().BeTrue();
    }

    [Fact]
    public async Task AnExistingSession_IsLiveAgainOnceTheTenantIsReactivatedAsync()
    {
        var user = await AddUserAsync("TenantSessionBack");
        var issued = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        await using var db = Fx.GetDbContext();

        await DisableTenantAsync("inactive");
        (await TokenValidity.IsRevokedAsync(db, user.Name, issued, null)).Should().BeTrue();

        await SetTenantAsync(t => t.Active = 1);
        (await TokenValidity.IsRevokedAsync(db, user.Name, issued, null)).Should().BeFalse();
    }

    [Fact]
    public async Task AnExistingSession_OfAnotherTenantOrALandlordIsNeverBlockedAsync()
    {
        var other = await AddUserAsync("TenantSessionOther", tenantB: true);
        var landlord = await AddUserAsync("TenantSessionLandlord");
        await SetTenantAsync(t => t.Landlord = 1);
        await DisableTenantAsync("inactive");
        await using var db = Fx.GetDbContext();

        (await TokenValidity.GetSessionStateAsync(db, other.Name)).Blocked.Should().BeFalse();
        (await TokenValidity.GetSessionStateAsync(db, landlord.Name)).Blocked.Should().BeFalse();
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    public async Task Permissions_AreEmptyAndTheUserInactive_WhileTheTenantIsDisabledAsync(string state)
    {
        var user = await AddUserAsync("TenantPermissions");
        await GrantProbePermissionAsync(user);
        await using var db = Fx.GetDbContext();

        var before = await new PermissionValidation().GetPermissionsAsync(db, user.Name, Log);
        before.ActiveUser.Should().BeTrue();
        before.Permissions.Should().Contain(ProbePermission);

        await DisableTenantAsync(state);

        var after = await new PermissionValidation().GetPermissionsAsync(db, user.Name, Log);
        after.ActiveUser.Should().BeFalse();
        after.Permissions.Should().BeEmpty();
        after.Landlord.Should().BeFalse();
    }

    [Fact]
    public async Task Permissions_AreKeptForALandlordTenantAndForAnotherTenantAsync()
    {
        var other = await AddUserAsync("TenantPermOther", tenantB: true);
        var landlord = await AddUserAsync("TenantPermLandlord");
        await GrantProbePermissionAsync(other);
        await GrantProbePermissionAsync(landlord);
        await SetTenantAsync(t => t.Landlord = 1);
        await DisableTenantAsync("inactive");
        await using var db = Fx.GetDbContext();

        var otherDto = await new PermissionValidation().GetPermissionsAsync(db, other.Name, Log);
        otherDto.ActiveUser.Should().BeTrue();
        otherDto.Permissions.Should().Contain(ProbePermission);

        var landlordDto = await new PermissionValidation().GetPermissionsAsync(db, landlord.Name, Log);
        landlordDto.ActiveUser.Should().BeTrue();
        landlordDto.Permissions.Should().Contain(ProbePermission);
    }

    private Task DisableTenantAsync(string state)
    {
        return SetTenantAsync(t =>
        {
            if (state == "inactive")
            {
                t.Active = 0;
            }
            else
            {
                t.Deleted = 1;
            }
        });
    }

    private async Task GrantProbePermissionAsync(TestUser user)
    {
        await using var db = Fx.GetDbContext();

        var guid = await db.UserRegistry.Where(w => w.Id == user.Id).Select(s => s.RoleRegistryGuid).FirstAsync();
        var roleId = await db.RoleRegistry.Where(w => w.Guid == guid).Select(s => s.Id).FirstAsync();

        await db.InsertAsync(new RoleRegistryPermission
        {
            Guid = Guid.NewGuid(), PermissionSpecificationId = ProbePermission, RoleRegistryId = roleId, Active = 1,
            Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
        });
    }

    public override async Task DisposeAsync()
    {
        await using (var db = Fx.GetDbContext())
        {
            await db.RoleRegistryPermission.Where(w => w.CreatedUser == DatabaseFixture.Prefix
                                                       && db.RoleRegistry.Any(r =>
                                                           r.Id == w.RoleRegistryId && r.Name.StartsWith(Prefix)))
                .DeleteAsync();
        }

        await base.DisposeAsync();
    }
}
