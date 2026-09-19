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
using Jube.Service.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationLockoutTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task TheAccountLocksExactlyOnTheNthWrongPassword_WhereNIsPasswordAttemptsAsync(int threshold)
    {
        var user = await AddUserAsync("Lock");

        for (var attempt = 1; attempt <= threshold; attempt++)
        {
            AssertPlainOutcome(await LoginAsync(user.Name, "wrong", env: [Attempts(threshold)]),
                AuthenticationOutcomeKind.Unauthorized);

            var reloaded = await ReloadAsync(user);
            reloaded.FailedPasswordCount.Should().Be(attempt);
            reloaded.PasswordLocked.Should().Be(attempt == threshold ? (byte)1 : (byte)0,
                $"attempt {attempt} of threshold {threshold}");
        }

        (await ReloadAsync(user)).PasswordLockedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        AssertPlainOutcome(await LoginAsync(user.Name, user.Password, env: [Attempts(threshold)]),
            AuthenticationOutcomeKind.Unauthorized);
    }

    [Fact]
    public async Task WithTheDefaultOfThree_TheThirdWrongPasswordLocks_NotTheFifthAsync()
    {
        var user = await AddUserAsync("DefaultThree");

        await LoginAsync(user.Name, "w1");
        await LoginAsync(user.Name, "w2");
        (await ReloadAsync(user)).PasswordLocked.Should().Be(0);
        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);

        await LoginAsync(user.Name, "w1");
        await LoginAsync(user.Name, "w2");
        await LoginAsync(user.Name, "w3");

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task
        ANonPositiveThreshold_LocksOnTheVeryFirstFailure_AndNeverVerifiesAgainstTheRealHashTwiceAsync(int t)
    {
        var user = await AddUserAsync("NegThreshold");

        await LoginAsync(user.Name, "wrong", env: [Attempts(t)]);

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }

    [Fact]
    public async Task OnceLocked_EvenTheCorrectPasswordIsRefused_WithNoRealHashWork_AndTheCounterStopsMovingAsync()
    {
        var user = await AddUserAsync("LockedOut");
        for (var i = 0; i < 3; i++)
        {
            await LoginAsync(user.Name, "wrong");
        }

        var realBefore = Hash.RealVerifyCalls;
        var outcome = await LoginAsync(user.Name, user.Password);
        await LoginAsync(user.Name, "wrong-again");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Hash.RealVerifyCalls.Should().Be(realBefore);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(3);
        (await HistoryAsync()).TakeLast(2).Should().OnlyContain(h => h.FailureTypeId == 3);
        Cookies.Issued.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(400)]
    public async Task Weakness_ALockoutNeverExpires_NoMatterHowMuchTimePassesAsync(int days)
    {
        var user = await AddUserAsync("NoExpiryLock", u => u.PasswordLocked = 1);
        await using (var db = Fx.GetDbContext())
        {
            await db.UserRegistry.Where(w => w.Id == user.Id)
                .Set(s => s.PasswordLockedDate, DateTime.UtcNow.AddDays(-days))
                .UpdateAsync();
        }

        Clock.Advance(TimeSpan.FromDays(days));

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Unauthorized);
    }

    [Fact]
    public async Task ASuccessfulLogin_ResetsTheFailureCounterToZeroAsync()
    {
        var user = await AddUserAsync("Reset");
        await LoginAsync(user.Name, "wrong");
        await LoginAsync(user.Name, "wrong");
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(2);

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);

        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task AfterAReset_TheCountRestartsFromZero_SoFailuresDoNotAccumulateAcrossSuccessesAsync()
    {
        var user = await AddUserAsync("Restart");
        for (var round = 0; round < 3; round++)
        {
            await LoginAsync(user.Name, "wrong");
            await LoginAsync(user.Name, "wrong");
            await LoginAsync(user.Name, user.Password);
        }

        var reloaded = await ReloadAsync(user);
        reloaded.FailedPasswordCount.Should().Be(0);
        reloaded.PasswordLocked.Should().Be(0);
    }

    [Fact]
    public async Task TheCorrectPasswordOnTheLastUnlockedAttempt_StillSucceedsAsync()
    {
        var user = await AddUserAsync("LastChance");
        for (var i = 0; i < 2; i++)
        {
            await LoginAsync(user.Name, "wrong");
        }

        (await ReloadAsync(user)).PasswordLocked.Should().Be(0);
        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task ExpiredPasswordWithTheCorrectPassword_ClearsTheCounter_SoARightPasswordNeverBuildsUpToALockAsync()
    {
        var user = await AddUserAsync("ExpiredReset", u =>
        {
            u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-1);
            u.FailedPasswordCount = 2;
        });

        for (var i = 0; i < 6; i++)
        {
            AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Forbidden);
        }

        var reloaded = await ReloadAsync(user);
        reloaded.FailedPasswordCount.Should().Be(0);
        reloaded.PasswordLocked.Should().Be(0);
    }

    [Fact]
    public async Task AUserWithoutAPasswordExpiry_IsCountedAndLockedLikeAnyOther_ButStillGetsTheMustChangeOutcomeAsync()
    {
        var user = await AddUserAsync("NoExpiryLocks", u => u.PasswordExpiryDate = null);

        for (var i = 1; i <= 3; i++)
        {
            AssertPlainOutcome(await LoginAsync(user.Name, $"guess-{i}"), AuthenticationOutcomeKind.Forbidden);
            var state = await ReloadAsync(user);
            state.FailedPasswordCount.Should().Be(i);
            state.PasswordLocked.Should().Be(i == 3 ? (byte)1 : (byte)0);
        }

        var realBefore = Hash.RealVerifyCalls;
        AssertPlainOutcome(await LoginAsync(user.Name, "guess-4"), AuthenticationOutcomeKind.Unauthorized);
        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Unauthorized);
        Hash.RealVerifyCalls.Should().Be(realBefore);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(3);
        (await HistoryAsync()).Take(3).Should().OnlyContain(h => h.FailureTypeId == 4);
    }

    [Fact]
    public async Task ACorrectPasswordForAUserWithoutExpiry_StillGetsMustChange_AndIsNotCountedAsync()
    {
        var user = await AddUserAsync("NoExpiryCorrect", u => u.PasswordExpiryDate = null);

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Forbidden);

        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task TheNoExpiryLockThresholdMatchesTheExpiringUsersThresholdAsync(int threshold)
    {
        var noExpiry = await AddUserAsync("ParityNoExpiry", u => u.PasswordExpiryDate = null);
        var expiring = await AddUserAsync("ParityExpiring");

        for (var i = 0; i < Math.Max(threshold, 1); i++)
        {
            await LoginAsync(noExpiry.Name, "wrong", env: [Attempts(threshold)]);
            await LoginAsync(expiring.Name, "wrong", env: [Attempts(threshold)]);
        }

        (await ReloadAsync(noExpiry)).PasswordLocked.Should().Be(1);
        (await ReloadAsync(expiring)).PasswordLocked.Should().Be(1);
        (await ReloadAsync(noExpiry)).FailedPasswordCount.Should()
            .Be((await ReloadAsync(expiring)).FailedPasswordCount);
    }

    [Fact]
    public async Task ANullCreatedDateUserIsLockedTooAsync()
    {
        var user = await AddUserAsync("NoCreatedLocks", u => u.PasswordCreatedDate = null);

        for (var i = 0; i < 3; i++)
        {
            await LoginAsync(user.Name, "wrong");
        }

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }

    [Fact]
    public async Task WhenANewPasswordIsSupplied_TheNoExpiryUserDoesLockOutAsync()
    {
        var user = await AddUserAsync("NewPwLocks", u => u.PasswordExpiryDate = null);

        for (var i = 0; i < 5; i++)
        {
            AssertPlainOutcome(await LoginAsync(user.Name, "wrong", d => d.NewPassword = "Brand#NewPassword12"),
                AuthenticationOutcomeKind.Unauthorized);
        }

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }

    [Fact]
    public async Task CountersAreIndependentPerUserAsync()
    {
        var a = await AddUserAsync("CounterA");
        var b = await AddUserAsync("CounterB");

        for (var i = 0; i < 6; i++)
        {
            await LoginAsync(a.Name, "wrong");
        }

        (await ReloadAsync(a)).PasswordLocked.Should().Be(1);
        (await ReloadAsync(b)).FailedPasswordCount.Should().Be(0);
        (await LoginAsync(b.Name, b.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(24)]
    [InlineData(64)]
    public async Task ParallelWrongPasswords_LockAtTheBudget_AndOnlyTheAllowedNumberEverReachTheRealHashAsync(
        int parallel)
    {
        var user = await AddUserAsync("Parallel");

        var outcomes = await Task.WhenAll(Enumerable.Range(0, parallel)
            .Select(i => Task.Run(() => LoginAsync(user.Name, $"wrong-{i}"))));

        outcomes.Should().OnlyContain(o => o.Kind == AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
        var reloaded = await ReloadAsync(user);
        reloaded.FailedPasswordCount.Should().BeInRange(3, parallel);
        reloaded.PasswordLocked.Should().Be(1);
        Hash.RealVerifyCalls.Should().BeLessThanOrEqualTo(3);
        Hash.VerifyCalls.Should().Be(parallel, "every attempt does exactly one hash verification, real or dummy");
        (await HistoryAsync()).Should().HaveCount(parallel).And.OnlyContain(h => h.Failed == 1);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(48)]
    public async Task ParallelFailuresBelowTheThreshold_ProduceExactlyNCountedFailures_NoLostUpdatesAsync(int parallel)
    {
        var user = await AddUserAsync("ParallelExact");

        await Task.WhenAll(Enumerable.Range(0, parallel)
            .Select(i => Task.Run(() => LoginAsync(user.Name, $"w{i}", env: [Attempts(1000)]))));

        var reloaded = await ReloadAsync(user);
        reloaded.FailedPasswordCount.Should().Be(parallel);
        reloaded.PasswordLocked.Should().Be(0);
        (await HistoryAsync()).Should().HaveCount(parallel);
    }

    [Fact]
    public async Task ParallelGuesses_IncludingTheRightPassword_CannotAllBeTried_TheBudgetIsEnforcedAsync()
    {
        var user = await AddUserAsync("ParallelGuessBudget");

        for (var i = 0; i < 3; i++)
        {
            await LoginAsync(user.Name, "wrong");
        }

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(i => Task.Run(() => LoginAsync(user.Name, i == 13 ? user.Password : $"guess-{i}"))));

        outcomes.Should().OnlyContain(o => o.Kind == AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task ParallelWrongPasswords_NeverExceedTheThresholdOfRealVerifications_EvenWithAHigherThresholdAsync()
    {
        var user = await AddUserAsync("ParallelFive");

        await Task.WhenAll(Enumerable.Range(0, 30)
            .Select(i => Task.Run(() => LoginAsync(user.Name, $"w{i}", env: [Attempts(5)]))));

        Hash.RealVerifyCalls.Should().BeLessThanOrEqualTo(5);
        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }

    [Fact]
    public async Task ParallelCorrectPasswordsOnALockedAccount_NeverSucceedAsync()
    {
        var user = await AddUserAsync("ParallelLocked", u => u.PasswordLocked = 1);

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => Task.Run(() => LoginAsync(user.Name, user.Password))));

        outcomes.Should().OnlyContain(o => o.Kind == AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
        Hash.RealVerifyCalls.Should().Be(0);
    }

    [Fact]
    public async Task ParallelMixOfRightAndWrong_OnlyTheRightOnesCanSucceed_AndNoneAreForgedAsync()
    {
        var user = await AddUserAsync("ParallelMix");

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(i => Task.Run(() => LoginAsync(user.Name, i % 2 == 0 ? user.Password : "wrong"))));

        outcomes.Count(o => o.Kind == AuthenticationOutcomeKind.Ok).Should().BeLessThanOrEqualTo(8);
        outcomes.Should().OnlyContain(o => o.Kind == AuthenticationOutcomeKind.Ok
                                           || o.Kind == AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().OnlyContain(n => n == user.Name);
        Hash.RealVerifyCalls.Should().BeLessThanOrEqualTo(16);
    }

    [Fact]
    public async Task ParallelFailuresAgainstDifferentUsers_AreCountedExactlyAndDoNotBleedAsync()
    {
        var users = await Task.WhenAll(Enumerable.Range(0, 5).Select(i => AddUserAsync($"Bleed{i}")));

        await Task.WhenAll(users.SelectMany((u, i) =>
            Enumerable.Range(0, i + 1).Select(_ => Task.Run(() => LoginAsync(u.Name, "wrong", env: [Attempts(100)])))));

        for (var i = 0; i < users.Length; i++)
        {
            (await ReloadAsync(users[i])).FailedPasswordCount.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task SequentialLoginsAfterAParallelBurst_BehaveAsLockedAsync()
    {
        var user = await AddUserAsync("BurstThenSequential");
        await Task.WhenAll(Enumerable.Range(0, 12).Select(i => Task.Run(() => LoginAsync(user.Name, $"w{i}"))));

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password), AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }
}