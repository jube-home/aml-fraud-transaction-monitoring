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

using Jube.Test.Infrastructure;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationMfaTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private const string Code = "424242";

    private Task<AuthenticationOutcome> WithMfaAsync(TestUser user, string? password, string? mfa,
        NegotiateIdentity? identity = null, CancellationToken token = default)
    {
        return LoginAsync(user.Name, password, d => d.Mfa = mfa, identity, token, MfaOn);
    }

    private async Task<AuthenticationOutcome> NegotiateMfaAsync(string identityName, string? mfa)
    {
        await using var db = Fx.GetDbContext();
        return await Service(db, NegotiateOn, MfaOn).ByNegotiateMfaAsync(new AuthenticationRequestDto { Mfa = mfa },
            Context(new NegotiateIdentity(true, identityName)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task MfaRequired_ABlankCodeFailsValidation_BeforeAnyCredentialIsCheckedAsync(string? blank)
    {
        var user = await AddUserAsync("MfaBlank");

        var outcome = await WithMfaAsync(user, user.Password, blank);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.ValidationFailed);
        outcome.Validation.Required().Errors.Should().Contain(e => e.PropertyName == "Mfa");
        Mfa.Calls.Should().BeEmpty();
        Hash.VerifyCalls.Should().Be(0);
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task MfaRequired_TheCorrectCode_IssuesTheCookies_AndTheProviderSawTheUserAndCodeAsync()
    {
        var user = await AddUserAsync("MfaOk");
        Mfa.Allow(user.Name, Code);

        var outcome = await WithMfaAsync(user, user.Password, Code);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(user.Name);
        var call = Mfa.Calls.Should().ContainSingle().Subject;
        call.User.Should().Be(user.Name);
        call.Code.Should().Be(Code);
    }

    [Fact]
    public async Task MfaRequired_AWrongCode_IsUnauthorised_AndIssuesNothingAsync()
    {
        var user = await AddUserAsync("MfaWrong");
        Mfa.Allow(user.Name, Code);

        var outcome = await WithMfaAsync(user, user.Password, "000000");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task AWrongMfaCode_IsCountedAsAFailure_AndDoesNotCommitTheSuccessOrResetTheCounterAsync()
    {
        var user = await AddUserAsync("MfaHistory");

        await WithMfaAsync(user, user.Password, "000000");

        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(1);
        var history = (await HistoryAsync()).Should().ContainSingle().Subject;
        history.Failed.Should().Be(1);
        history.FailureTypeId.Should().Be(7);
        history.AuthenticationTypeId.Should().Be(1);
    }

    [Fact]
    public async Task ACorrectMfaCode_CompletesTheLogin_WritesTheSuccessRecord_AndResetsTheCounterAsync()
    {
        var user = await AddUserAsync("MfaComplete", u => u.FailedPasswordCount = 2);
        Mfa.Allow(user.Name, Code);

        (await WithMfaAsync(user, user.Password, Code)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);

        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
        (await HistoryAsync()).Should().ContainSingle().Which.Failed.Should().Be(0);
    }

    [Fact]
    public async Task
        MfaGuessing_LocksTheAccountOnTheThirdWrongCode_AndALockedAccountRejectsTheRightCodeAndPasswordAsync()
    {
        var user = await AddUserAsync("MfaBrute");
        Mfa.Allow(user.Name, Code);

        for (var i = 0; i < 6; i++)
        {
            AssertPlainOutcome(await WithMfaAsync(user, user.Password, $"{i:D6}"),
                AuthenticationOutcomeKind.Unauthorized);
        }

        var locked = await ReloadAsync(user);
        locked.PasswordLocked.Should().Be(1);
        locked.FailedPasswordCount.Should().Be(3);
        Mfa.Calls.Should().HaveCount(3, "once locked the provider is never consulted");

        AssertPlainOutcome(await WithMfaAsync(user, user.Password, Code), AuthenticationOutcomeKind.Unauthorized);
        AssertPlainOutcome(await WithMfaAsync(user, "wrong", Code), AuthenticationOutcomeKind.Unauthorized);
        Mfa.Calls.Should().HaveCount(3);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task AlternatingRightPasswordAndWrongCode_CannotDodgeTheCounterAsync()
    {
        var user = await AddUserAsync("MfaAlternate");

        for (var i = 0; i < 3; i++)
        {
            await WithMfaAsync(user, user.Password, $"{i:D6}");
        }

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }

    [Fact]
    public async Task MixedPasswordAndMfaFailures_ShareOneCounterAsync()
    {
        var user = await AddUserAsync("MfaShared");

        await WithMfaAsync(user, "wrong", "000000");
        await WithMfaAsync(user, user.Password, "000001");
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(2);
        await WithMfaAsync(user, "wrong", "000002");

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }

    [Fact]
    public async Task AProviderOutage_GivesTheReservedAttemptBack_SoItIsNotCountedAgainstTheUserAsync()
    {
        var user = await AddUserAsync("MfaOutageNoCount");
        Mfa.Throw = new HttpRequestException("down");

        var act = () => WithMfaAsync(user, user.Password, Code);
        await act.Should().ThrowAsync<HttpRequestException>();

        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task NegotiateMfa_WrongCodesAreCounted_TheAccountLocksOnTheThird_AndEverythingThenRefusesAsync()
    {
        var user = await AddUserAsync("NegMfaLock");
        Mfa.Allow(user.Name, Code);

        for (var i = 0; i < 3; i++)
        {
            AssertPlainOutcome(await NegotiateMfaAsync(user.Name, $"{i:D6}"), AuthenticationOutcomeKind.Unauthorized);
        }

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
        var providerCalls = Mfa.Calls.Count;
        AssertPlainOutcome(await NegotiateMfaAsync(user.Name, Code), AuthenticationOutcomeKind.Forbidden);
        await using (var db = Fx.GetDbContext())
        {
            AssertPlainOutcome(await Service(db, NegotiateOn, MfaOn)
                    .ByNegotiateAsync(Context(new NegotiateIdentity(true, user.Name))),
                AuthenticationOutcomeKind.Forbidden);
        }

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password, d => d.Mfa = Code, env: [MfaOn]),
            AuthenticationOutcomeKind.Unauthorized);
        Mfa.Calls.Count.Should().Be(providerCalls);
        (await HistoryAsync()).Count(h => h.FailureTypeId == 7).Should().Be(3);
    }

    [Fact]
    public async Task NegotiateMfa_ParallelWrongCodes_NeverReachTheProviderMoreThanTheBudgetAllowsAsync()
    {
        var user = await AddUserAsync("NegMfaParallel");

        await Task.WhenAll(Enumerable.Range(0, 24)
            .Select(i => Task.Run(() => NegotiateMfaAsync(user.Name, $"{i:D6}"))));

        Mfa.Calls.Count.Should().BeLessThanOrEqualTo(3);
        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
    }

    [Fact]
    public async Task NegotiateMfa_ACorrectCodeResetsTheCounterAsync()
    {
        var user = await AddUserAsync("NegMfaReset");
        Mfa.Allow(user.Name, Code);

        AssertPlainOutcome(await NegotiateMfaAsync(user.Name, "000000"), AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(1);
        (await NegotiateMfaAsync(user.Name, Code)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);

        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task NegotiateMfa_ABlankCodeIsNotCountedAsync()
    {
        var user = await AddUserAsync("NegMfaBlank");

        await NegotiateMfaAsync(user.Name, null);
        await NegotiateMfaAsync(user.Name, "");

        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task MfaRequired_AWrongPassword_NeverReachesTheProviderAsync()
    {
        var user = await AddUserAsync("MfaNoReach");
        Mfa.Allow(user.Name, Code);

        AssertPlainOutcome(await WithMfaAsync(user, "wrong", Code), AuthenticationOutcomeKind.Unauthorized);

        Mfa.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("locked")]
    [InlineData("inactive")]
    public async Task MfaRequired_AnAccountThatCannotLogIn_NeverReachesTheProviderAsync(string state)
    {
        var user = await AddUserAsync("MfaState", u =>
        {
            switch (state)
            {
                case "expired": u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-1); break;
                case "locked": u.PasswordLocked = 1; break;
                default: u.Active = 0; break;
            }
        });
        Mfa.Allow(user.Name, Code);

        var outcome = await WithMfaAsync(user, user.Password, Code);

        outcome.Kind.Should().NotBe(AuthenticationOutcomeKind.Ok);
        Mfa.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task MfaRequired_ACodeCannotBeReusedAsync()
    {
        var user = await AddUserAsync("MfaReuse");
        Mfa.Allow(user.Name, Code);

        var first = await WithMfaAsync(user, user.Password, Code);
        var second = await WithMfaAsync(user, user.Password, Code);

        first.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        AssertPlainOutcome(second, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().HaveCount(1);
    }

    [Fact]
    public async Task MfaRequired_AnExpiredCode_IsRejected_OnTheInjectedClockAsync()
    {
        var user = await AddUserAsync("MfaExpired");
        Mfa.Allow(user.Name, Code, TimeSpan.FromSeconds(30));
        Clock.Advance(TimeSpan.FromSeconds(31));

        AssertPlainOutcome(await WithMfaAsync(user, user.Password, Code), AuthenticationOutcomeKind.Unauthorized);
    }

    [Fact]
    public async Task MfaRequired_ACodeIssuedForAnotherUser_IsRejectedAsync()
    {
        var a = await AddUserAsync("MfaA");
        var b = await AddUserAsync("MfaB");
        Mfa.Allow(a.Name, Code);

        AssertPlainOutcome(await WithMfaAsync(b, b.Password, Code), AuthenticationOutcomeKind.Unauthorized);
        (await WithMfaAsync(a, a.Password, Code)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Theory]
    [InlineData("Unicode-code-ü中")]
    [InlineData("' OR '1'='1")]
    [InlineData("12345678\0")]
    public async Task MfaRequired_UnusualCodes_ArePassedToTheProviderVerbatim_AndRejectedAsync(string weird)
    {
        var user = await AddUserAsync("MfaWeird");

        AssertPlainOutcome(await WithMfaAsync(user, user.Password, weird), AuthenticationOutcomeKind.Unauthorized);

        Mfa.Calls.Single().Code.Should().Be(weird);
    }

    [Fact]
    public async Task MfaRequired_AProviderFailure_PropagatesAsAnException_NeverAsASuccessAsync()
    {
        var user = await AddUserAsync("MfaDown");
        Mfa.Throw = new HttpRequestException("RSA AM returned 503");

        var act = () => WithMfaAsync(user, user.Password, Code);

        await act.Should().ThrowAsync<HttpRequestException>();
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task MfaRequired_TheCancellationTokenReachesTheProviderAsync()
    {
        var user = await AddUserAsync("MfaToken");
        Mfa.Allow(user.Name, Code);
        using var cts = new CancellationTokenSource();

        await WithMfaAsync(user, user.Password, Code, token: cts.Token);

        Mfa.Calls.Single().Token.Should().Be(cts.Token);
    }

    [Fact]
    public async Task MfaNotRequired_ACodeIsIgnored_AndTheProviderIsNeverCalledAsync()
    {
        var user = await AddUserAsync("MfaOff");

        var outcome = await LoginAsync(user.Name, user.Password, d => d.Mfa = Code);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Mfa.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task MfaRequired_TheSubjectIsTheAuthenticatedIdentity_WhenOneIsPresentAsync()
    {
        var user = await AddUserAsync("MfaSubject");
        var identity = new NegotiateIdentity(true, $"{Prefix}Someone");
        Mfa.Allow($"{Prefix}Someone", Code);

        var outcome = await WithMfaAsync(user, user.Password, Code, identity);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal($"{Prefix}Someone");
    }

    [Fact]
    public async Task NegotiateWithMfa_TheGetSignalsMfaRequired_WithoutIssuingAnythingAsync()
    {
        var user = await AddUserAsync("NegMfa");
        await using var db = Fx.GetDbContext();

        var outcome = await Service(db, NegotiateOn, MfaOn)
            .ByNegotiateAsync(Context(new NegotiateIdentity(true, user.Name)));

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Accepted);
        Cookies.Issued.Should().BeEmpty();
        (await HistoryAsync()).Should().ContainSingle().Which.AuthenticationTypeId.Should().Be(2);
    }

    [Fact]
    public async Task NegotiateWithMfa_TheMfaPostWithoutACode_IsAccepted202_NotAnErrorAsync()
    {
        var user = await AddUserAsync("NegMfaNone");

        AssertPlainOutcome(await NegotiateMfaAsync(user.Name, null), AuthenticationOutcomeKind.Accepted);
        AssertPlainOutcome(await NegotiateMfaAsync(user.Name, ""), AuthenticationOutcomeKind.Accepted);
        Mfa.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task NegotiateWithMfa_TheCorrectCode_IssuesCookiesForTheWindowsIdentityAsync()
    {
        var user = await AddUserAsync("NegMfaOk");
        Mfa.Allow(user.Name, Code);

        var outcome = await NegotiateMfaAsync(user.Name, Code);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(user.Name);
    }

    [Fact]
    public async Task NegotiateWithMfa_AWrongCode_IsUnauthorised_AndAReusedCodeFailsAsync()
    {
        var user = await AddUserAsync("NegMfaBad");
        Mfa.Allow(user.Name, Code);

        AssertPlainOutcome(await NegotiateMfaAsync(user.Name, "000000"), AuthenticationOutcomeKind.Unauthorized);
        (await NegotiateMfaAsync(user.Name, Code)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        AssertPlainOutcome(await NegotiateMfaAsync(user.Name, Code), AuthenticationOutcomeKind.Unauthorized);
    }

    [Fact]
    public async Task NegotiateWithMfa_AnUnknownIdentity_IsForbidden_BeforeMfaIsConsultedAsync()
    {
        Mfa.Allow($"{Prefix}Ghost", Code);

        AssertPlainOutcome(await NegotiateMfaAsync($"{Prefix}Ghost", Code), AuthenticationOutcomeKind.Forbidden);

        Mfa.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task NegotiateWithMfa_AProviderFailure_PropagatesAsync()
    {
        var user = await AddUserAsync("NegMfaDown");
        Mfa.Throw = new HttpRequestException("down");

        var act = () => NegotiateMfaAsync(user.Name, Code);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task NegotiateWithoutMfa_IssuesCookiesStraightAway_AndSuppliedCodesAreIgnoredAsync()
    {
        var user = await AddUserAsync("NegNoMfa");
        await using var db = Fx.GetDbContext();

        var outcome = await Service(db, NegotiateOn)
            .ByNegotiateMfaAsync(new AuthenticationRequestDto { Mfa = "ignored" },
                Context(new NegotiateIdentity(true, user.Name)));

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Mfa.Calls.Should().BeEmpty();
    }
}