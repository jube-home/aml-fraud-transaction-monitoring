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
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Service.Exceptions.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using AuthEngine = Jube.Service.Authentication.Authentication;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationAccountStateTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    [Fact]
    public async Task DeletedUser_IsIndistinguishableFromUnknown_AndDoesNoHashWorkAsync()
    {
        var user = await AddUserAsync("Deleted", u => u.Deleted = 1);

        var outcome = await LoginAsync(user.Name, user.Password);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(1);
        Hash.RealVerifyCalls.Should().Be(0);
        Hash.VerifyCalls.Should().Be(1, "equivalent dummy work is done");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(255)]
    public async Task NotActiveUser_IsUnauthorised_FailureType2_WithoutHashWorkAsync(int active)
    {
        var user = await AddUserAsync("Inactive", u => u.Active = (byte)active);

        var outcome = await LoginAsync(user.Name, user.Password);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(2);
        Hash.RealVerifyCalls.Should().Be(0);
        Hash.VerifyCalls.Should().Be(1, "equivalent dummy work is done");
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task NullActiveFlag_IsTreatedAsInactiveAsync()
    {
        var user = await AddUserAsync("NullActive", u => u.Active = null);

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(2);
    }

    [Fact]
    public async Task LockedUser_CannotLogIn_EvenWithTheCorrectPassword_FailureType3Async()
    {
        var user = await AddUserAsync("Locked", u => u.PasswordLocked = 1);

        var outcome = await LoginAsync(user.Name, user.Password);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(3);
        Hash.RealVerifyCalls.Should().Be(0);
        Hash.VerifyCalls.Should().Be(1, "equivalent dummy work is done");
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task NullPasswordLockedFlag_IsTreatedAsUnlockedAsync()
    {
        var user = await AddUserAsync("NullLock", u => u.PasswordLocked = null);

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task ExpiredPassword_WithTheCorrectPassword_IsForbidden_AndNotRecordedInTheLoginHistoryAsync()
    {
        var user = await AddUserAsync("Expired", u => u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-1));

        var outcome = await LoginAsync(user.Name, user.Password);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Forbidden);
        Cookies.Issued.Should().BeEmpty();
        (await HistoryAsync()).Should().BeEmpty();
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task NullPasswordExpiry_WithTheCorrectPassword_IsForbiddenAsync()
    {
        var user = await AddUserAsync("NoExpiry", u => u.PasswordExpiryDate = null);

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Forbidden);
    }

    [Fact]
    public async Task PasswordExpiryBoundary_IsInclusive_OnTheInjectedClockAsync()
    {
        var expiry = Clock.GetUtcNow().UtcDateTime.AddHours(1);
        var user = await AddUserAsync("Boundary", u => u.PasswordExpiryDate = expiry);

        Clock.Set(new DateTimeOffset(expiry, TimeSpan.Zero));
        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);

        Clock.Advance(TimeSpan.FromSeconds(1));
        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Forbidden);
    }

    [Fact]
    public async Task ExpiredPassword_WithAWrongPassword_IsJustBadCredentials_NotForbiddenAsync()
    {
        var user = await AddUserAsync("ExpiredWrong", u => u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-1));

        AssertPlainOutcome(await LoginAsync(user.Name, "wrong"), AuthenticationOutcomeKind.Unauthorized);
    }

    [Fact]
    public async Task ASuccessfulLogin_DoesNotChangeAnyOtherUserAsync()
    {
        var a = await AddUserAsync("IsoA");
        var b = await AddUserAsync("IsoB");

        await LoginAsync(a.Name, "wrong");
        await LoginAsync(a.Name, a.Password);

        (await ReloadAsync(b)).FailedPasswordCount.Should().Be(0);
        (await HistoryAsync()).Should().OnlyContain(h => h.CreatedUser == a.Name);
    }

    [Fact]
    public async Task UserWithNoTenantMembership_CanStillAuthenticateAsync()
    {
        var user = await AddUserAsync("NoTenant", inTenant: false);

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task UserInBothTenants_AuthenticatesOnceAsync()
    {
        var user = await AddUserAsync("Both");
        await using (var db = Fx.GetDbContext())
        {
            await db.InsertAsync(new UserInTenant { User = user.Name, TenantRegistryId = TenantBId });
        }

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(user.Name);
    }

    [Fact]
    public async Task UserOfTenantB_AuthenticatesWithoutTenantContextAsync()
    {
        var user = await AddUserAsync("TenantB", tenantB: true);

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("locked")]
    [InlineData("deleted")]
    public async Task UserOfADisabledTenant_CanStillAuthenticateAsync(string state)
    {
        var user = await AddUserAsync("DisabledTenant");
        await SetTenantAsync(t =>
        {
            switch (state)
            {
                case "inactive": t.Active = 0; break;
                case "locked": t.Locked = 1; break;
                default: t.Deleted = 1; break;
            }
        });

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task UserWhoseRoleIsDeleted_CanStillAuthenticateAsync()
    {
        var user = await AddUserAsync("DeletedRole");
        await using (var db = Fx.GetDbContext())
        {
            await db.RoleRegistry.Where(w => w.TenantRegistryId == TenantAId).Set(s => s.Deleted, (byte)1)
                .UpdateAsync();
        }

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("$argon2id$v=19$m=1,t=1,p=1$xxxx$yyyy")]
    public async Task ACorruptOrMissingStoredHash_NeverAuthenticates_UnderTheRealArgon2SchemeAsync(string? stored)
    {
        var user = await AddUserAsync("Corrupt", password: null, storedHash: stored);
        await using var db = Fx.GetDbContext();

        var outcome = await Service(db, new Argon2PasswordHashScheme()).ByUserNamePasswordAsync(
            new AuthenticationRequestDto { UserName = user.Name, Password = "anything" },
            Context());

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task TwoUsersWithDifferentPasswords_CannotUseEachOthersPasswordAsync()
    {
        var a = await AddUserAsync("CrossA", password: "Alpha#Password1234");
        var b = await AddUserAsync("CrossB", password: "Bravo#Password5678");

        AssertPlainOutcome(await LoginAsync(a.Name, b.Password), AuthenticationOutcomeKind.Unauthorized);
        AssertPlainOutcome(await LoginAsync(b.Name, a.Password), AuthenticationOutcomeKind.Unauthorized);
        (await LoginAsync(a.Name, a.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(a.Name);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("wrong")]
    [InlineData("inactive")]
    [InlineData("locked")]
    [InlineData("deleted")]
    public async Task EveryNonSuccessReason_ProducesTheSameOutcomeShape_ForAUserWithAnExpiryAsync(string reason)
    {
        var user = reason switch
        {
            "inactive" => await AddUserAsync("Enum", u => u.Active = 0),
            "locked" => await AddUserAsync("Enum", u => u.PasswordLocked = 1),
            "deleted" => await AddUserAsync("Enum", u => u.Deleted = 1),
            _ => await AddUserAsync("Enum")
        };
        var name = reason == "unknown" ? $"{Prefix}Nobody" : user.Name;
        var password = reason == "wrong" ? "wrong-password" : user.Password;

        var outcome = await LoginAsync(name, password);

        outcome.Should().Be(AuthenticationOutcome.Unauthorized());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("deleted")]
    [InlineData("inactive")]
    [InlineData("locked")]
    [InlineData("wrong")]
    public async Task EveryBranchDoesExactlyOneHashVerification_SoTimingDoesNotEnumerateUsersAsync(string branch)
    {
        var user = branch switch
        {
            "deleted" => await AddUserAsync("Timing", u => u.Deleted = 1),
            "inactive" => await AddUserAsync("Timing", u => u.Active = 0),
            "locked" => await AddUserAsync("Timing", u => u.PasswordLocked = 1),
            _ => await AddUserAsync("Timing")
        };
        var name = branch == "unknown" ? $"{Prefix}TimingUnknown" : user.Name;

        var outcome = await LoginAsync(name, "wrong-password");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Hash.VerifyCalls.Should().Be(1);
        Hash.RealVerifyCalls.Should().Be(branch == "wrong" ? 1 : 0);
        Hash.DummyVerifyCalls.Should().Be(branch == "wrong" ? 0 : 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task AnEmptyPasswordForARealAccount_StillDoesOneDummyHashVerification_CR09Async(string? password)
    {
        var user = await AddUserAsync("EmptyPw");

        await using var db = Fx.GetDbContext();
        var engine = new AuthEngine(db, false, null, Hash, Clock);

        var act = () => engine.AuthenticateByUserNamePasswordAsync(
            new AuthenticationRequestDto { UserName = user.Name, Password = password }, HashKey);

        await act.Should().ThrowAsync<PasswordEmptyException>();
        Hash.VerifyCalls.Should().Be(1);
        Hash.RealVerifyCalls.Should().Be(0);
        Hash.DummyVerifyCalls.Should().Be(1);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("deleted")]
    [InlineData("inactive")]
    [InlineData("locked")]
    public async Task TheDummyVerification_CanNeverAuthenticateEvenWithTheDummyPasswordItselfAsync(string branch)
    {
        var user = branch switch
        {
            "deleted" => await AddUserAsync("DummyAuth", u => u.Deleted = 1),
            "inactive" => await AddUserAsync("DummyAuth", u => u.Active = 0),
            "locked" => await AddUserAsync("DummyAuth", u => u.PasswordLocked = 1),
            _ => await AddUserAsync("DummyAuth")
        };
        var name = branch == "unknown" ? $"{Prefix}DummyUnknown" : user.Name;

        var outcome = await LoginAsync(name, AuthEngine.DummyPassword);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task TheWireResponsesAndHistoryShapeStayIdentical_AcrossTheBranchesAsync()
    {
        var known = await AddUserAsync("SameShape");
        var locked = await AddUserAsync("SameShapeLocked", u => u.PasswordLocked = 1);

        var results = new[]
        {
            await LoginAsync($"{Prefix}Nobody", "x"), await LoginAsync(known.Name, "x"),
            await LoginAsync(locked.Name, "x")
        };

        results.Should().OnlyContain(r => r == AuthenticationOutcome.Unauthorized());
    }

    [Fact]
    public async Task Weakness_AWrongPasswordForAUserWithoutExpiry_IsForbiddenNotUnauthorised_AnEnumerationOracleAsync()
    {
        var user = await AddUserAsync("OracleNoExpiry", u => u.PasswordExpiryDate = null);

        var outcome = await LoginAsync(user.Name, "wrong-password");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Forbidden);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(4);
    }

    [Fact]
    public async Task Weakness_AUserWithoutPasswordCreatedDate_IsAlsoAnOracleAsync()
    {
        var user = await AddUserAsync("OracleNoCreated", u => u.PasswordCreatedDate = null);

        AssertPlainOutcome(await LoginAsync(user.Name, "wrong-password"), AuthenticationOutcomeKind.Forbidden);
    }

    [Fact]
    public async Task Weakness_TheCorrectPasswordForAnExpiredUser_IsConfirmedByA403Async()
    {
        var user = await AddUserAsync("OracleExpired", u => u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-2));

        var right = await LoginAsync(user.Name, user.Password);
        var wrong = await LoginAsync(user.Name, "wrong");

        right.Kind.Should().Be(AuthenticationOutcomeKind.Forbidden);
        wrong.Kind.Should().Be(AuthenticationOutcomeKind.Unauthorized);
    }

    [Fact]
    public async Task UnknownUser_AndWrongPassword_BothWriteOneFailedHistoryRow_WithDifferentInternalFailureTypesAsync()
    {
        var known = await AddUserAsync("HistKnown");

        await LoginAsync($"{Prefix}HistUnknown", "x");
        await LoginAsync(known.Name, "x");

        (await HistoryAsync()).Select(h => h.FailureTypeId).Should().Equal(1, 5);
    }
}