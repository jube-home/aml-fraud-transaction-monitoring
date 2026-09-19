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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Service.Exceptions.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationNegotiateTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private async Task<AuthenticationOutcome> GetAsync(NegotiateIdentity identity, CancellationToken token = default,
        params (string Key, string Value)[] env)
    {
        await using var db = Fx.GetDbContext();
        return await Service(db, env.Length == 0 ? [NegotiateOn] : env).ByNegotiateAsync(Context(identity), token);
    }

    private async Task<AuthenticationOutcome> PostAsync(NegotiateIdentity identity,
        params (string Key, string Value)[] env)
    {
        await using var db = Fx.GetDbContext();
        return await Service(db, env.Length == 0 ? [NegotiateOn] : env)
            .ByNegotiateMfaAsync(new AuthenticationRequestDto { UserAgent = "spoofed" }, Context(identity));
    }

    private static NegotiateIdentity IdentityFor(string? name)
    {
        return new NegotiateIdentity(true, name);
    }

    [Fact]
    public async Task GetByNegotiate_ForAKnownActiveUser_IssuesCookies_AndRecordsAType2SuccessAsync()
    {
        var user = await AddUserAsync("NegOk");

        var outcome = await GetAsync(IdentityFor(user.Name));

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(user.Name);
        var history = (await HistoryAsync()).Should().ContainSingle().Subject;
        history.CreatedUser.Should().Be(user.Name);
        history.AuthenticationTypeId.Should().Be(2);
        history.Failed.Should().Be(0);
        history.LocalIp.Should().Be("10.0.0.1");
        history.UserAgent.Should().Be(Marker);
        history.RemoteIp.Should().Be("192.0.2.10");
    }

    [Fact]
    public async Task PostByNegotiateMfa_ForAKnownActiveUser_IssuesCookies_WithoutMfaConfiguredAsync()
    {
        var user = await AddUserAsync("NegPostOk");

        var outcome = await PostAsync(IdentityFor(user.Name));

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(user.Name);
        (await HistoryAsync()).Should().ContainSingle().Which.UserAgent.Should().Be(Marker);
    }

    [Fact]
    public async Task UnknownWindowsIdentity_IsForbidden_AndRecordedAsFailureType1UnderThatIdentityAsync()
    {
        var name = $"{Prefix}CORP\\nobody";

        var outcome = await GetAsync(IdentityFor(name));

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Forbidden);
        Cookies.Issued.Should().BeEmpty();
        var history = (await HistoryAsync()).Should().ContainSingle().Subject;
        history.CreatedUser.Should().Be(name);
        history.FailureTypeId.Should().Be(1);
        history.Failed.Should().Be(1);
        history.AuthenticationTypeId.Should().Be(2);
    }

    [Theory]
    [InlineData("domain-prefix")]
    [InlineData("upn-suffix")]
    [InlineData("upper")]
    [InlineData("lower")]
    [InlineData("trailing-space")]
    public async Task IdentityMatchingIsExact_DomainQualifiersAreNotStripped_AndCaseMattersAsync(string variant)
    {
        var user = await AddUserAsync("NegExact");
        var presented = variant switch
        {
            "domain-prefix" => $"CORP\\{user.Name}",
            "upn-suffix" => $"{user.Name}@corp.example",
            "upper" => user.Name.ToUpperInvariant(),
            "lower" => user.Name.ToLowerInvariant(),
            _ => user.Name + " "
        };

        var outcome = await GetAsync(IdentityFor(presented));

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Forbidden);
        Cookies.Issued.Should().BeEmpty();
    }

    [Theory]
    [InlineData("CORP\\alice")]
    [InlineData("alice@corp.example")]
    [InlineData("CORP.EXAMPLE\\Ünïcödé")]
    public async Task ADomainQualifiedRegistryName_MatchesTheSameQualifiedIdentityAsync(string qualified)
    {
        var name = $"{Prefix}{qualified}";
        await AddUserAsync("NegQualified", exactName: name);

        (await GetAsync(IdentityFor(name))).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(name);
    }

    [Theory]
    [InlineData("deleted", 1)]
    [InlineData("inactive", 2)]
    [InlineData("locked", 3)]
    public async Task DeletedInactiveOrLockedUsers_AreForbidden_WithTheMatchingFailureTypeAsync(string state,
        int failureType)
    {
        var user = await AddUserAsync("NegState", u =>
        {
            switch (state)
            {
                case "deleted": u.Deleted = 1; break;
                case "inactive": u.Active = 0; break;
                default: u.PasswordLocked = 1; break;
            }
        });

        AssertPlainOutcome(await GetAsync(IdentityFor(user.Name)), AuthenticationOutcomeKind.Forbidden);
        AssertPlainOutcome(await PostAsync(IdentityFor(user.Name)), AuthenticationOutcomeKind.Forbidden);

        (await HistoryAsync()).Should().HaveCount(2).And.OnlyContain(h => h.FailureTypeId == failureType);
    }

    [Fact]
    public async Task Negotiate_DoesNotCheckPasswordsExpiryOrFailureCountersAsync()
    {
        var user = await AddUserAsync("NegIgnores", u =>
        {
            u.PasswordExpiryDate = null;
            u.FailedPasswordCount = 2;
            u.Password = "no-usable-hash";
        });

        var outcome = await GetAsync(IdentityFor(user.Name));

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Hash.VerifyCalls.Should().Be(0);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByNegotiate_AnUnauthenticatedIdentity_IsUnauthorised_ButThePostIsForbiddenAsync()
    {
        var user = await AddUserAsync("NegAnon");

        AssertPlainOutcome(await GetAsync(new NegotiateIdentity(false, user.Name)),
            AuthenticationOutcomeKind.Unauthorized);
        AssertPlainOutcome(await PostAsync(new NegotiateIdentity(false, user.Name)),
            AuthenticationOutcomeKind.Forbidden);
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AnAuthenticatedIdentityWithoutAName_IsUnauthorised_OnBothRoutesAsync()
    {
        AssertPlainOutcome(await GetAsync(IdentityFor(null)), AuthenticationOutcomeKind.Unauthorized);
        AssertPlainOutcome(await PostAsync(IdentityFor(null)), AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("oauth")]
    [InlineData("negotiate-off")]
    public async Task BothRoutes_Are404_WhenNegotiateIsNotTheActiveSchemeAsync(string mode)
    {
        var user = await AddUserAsync("NegHidden");
        (string, string)[] env = mode == "oauth" ? [OAuthOn, NegotiateOn] : [("NegotiateAuthentication", "False")];

        AssertPlainOutcome(await GetAsync(IdentityFor(user.Name), default, env), AuthenticationOutcomeKind.NotFound);
        AssertPlainOutcome(await PostAsync(IdentityFor(user.Name), env), AuthenticationOutcomeKind.NotFound);
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("*)(uid=*))(|(uid=*")]
    [InlineData("%")]
    [InlineData("CORP\\*")]
    [InlineData("name‮")]
    public async Task HostileIdentities_MatchNothingAsync(string hostile)
    {
        var real = await AddUserAsync("NegHostile");

        AssertPlainOutcome(await GetAsync(IdentityFor(hostile)), AuthenticationOutcomeKind.Forbidden);

        (await ReloadAsync(real)).PasswordLocked.Should().Be(0);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task ACancelledToken_IsSwallowedByTheGenericCatch_AndBecomesForbiddenAsync()
    {
        var user = await AddUserAsync("NegCancel");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var outcome = await GetAsync(IdentityFor(user.Name), cts.Token);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Forbidden);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task ControlCharactersInAnIdentity_NeverReachTheLogOrAuditLinesRawAsync()
    {
        var evil = $"{Prefix}bob{'\r'}{'\n'}FAKE-LOG-LINE level=ERROR{'\a'}";

        await GetAsync(IdentityFor(evil));

        var lines = Log.Entries.Concat(Audit.Entries).Select(e => e.Message).ToList();
        lines.Should().NotBeEmpty();
        lines.Should().OnlyContain(l => !l.Contains('\r') && !l.Contains('\n') && !l.Contains('\a'));
    }

    [Fact]
    public async Task TheAuditLine_NamesTheEstablishedIdentityAsActor_AndTheOutcomeAsync()
    {
        var user = await AddUserAsync("NegAudit");

        await GetAsync(IdentityFor(user.Name));
        await GetAsync(IdentityFor($"{Prefix}Ghost"));

        var lines = Audit.Entries.Select(e => e.Message).ToList();
        lines.Should().HaveCount(2);
        lines[0].Should().Contain("op=ByNegotiate").And.Contain($"actor={user.Name}").And.Contain("outcome=ok");
        lines[1].Should().Contain("outcome=forbidden");
    }

    [Fact]
    public async Task NegotiateOutcomes_AreLogged_SuccessAtInfo_AndFailureAtErrorWithTheCauseAsync()
    {
        var user = await AddUserAsync("NegLog");

        await GetAsync(IdentityFor(user.Name));
        await GetAsync(IdentityFor($"{Prefix}Ghost"));

        Log.Entries.Should().Contain(e => e.Level == "INFO" && e.Message.Contains("resolved identity"));
        Log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Exception is NoUserException);
    }

    [Fact]
    public async Task ManyParallelNegotiateLogins_AllSucceedAndAllAreRecordedAsync()
    {
        var user = await AddUserAsync("NegParallel");

        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Task.Run(() => GetAsync(IdentityFor(user.Name)))));

        outcomes.Should().OnlyContain(o => o.Kind == AuthenticationOutcomeKind.Ok);
        (await HistoryAsync()).Should().HaveCount(10);
    }
}