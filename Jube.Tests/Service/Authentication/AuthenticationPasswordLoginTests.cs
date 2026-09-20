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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationPasswordLoginTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    public static TheoryData<string?> BlankValues =>
        new() { null, "", " ", "   ", "\t", "\r\n", " \t \n " };

    public static TheoryData<string> HostileNames =>
        new()
        {
            "' OR '1'='1", "admin'--", "'; DROP TABLE \"UserRegistry\"; --", "\" OR \"\"=\"", "1; SELECT pg_sleep(5)",
            "admin' UNION SELECT NULL,NULL,NULL--", "%", "%%", "_", "ZzTest%", "*", "*)(uid=*))(|(uid=*",
            "cn=admin,dc=example,dc=com", "admin)(&)", "\\00", "admin\\2a", "../../etc/passwd",
            "<script>alert(1)</script>", "admin\r\nX-Injected: 1", "‮admin", "ａｄｍｉｎ", "Ünïcödé-用户-🔐",
            "null", "NULL", "undefined", "{{7*7}}", "${jndi:ldap://x/a}", new string('a', 4096)
        };

    public static TheoryData<string> HostilePasswords =>
        new()
        {
            "' OR '1'='1", "\" OR \"\"=\"", "'; DROP TABLE \"UserRegistry\"; --", "*", "*)(userPassword=*", "%", "\\00",
            "pass\0word", "\0", "Ünïcödé-密码-🔐", " " + DefaultPassword, DefaultPassword + " ",
            DefaultPassword.ToUpperInvariant(), DefaultPassword.ToLowerInvariant(), DefaultPassword[..^1],
            DefaultPassword + "x", "null", new string('x', 100_000)
        };

    [Fact]
    public async Task CorrectPassword_IssuesCookiesForTheUser_AndRecordsASuccessfulLoginAsync()
    {
        var user = await AddUserAsync("Ok");

        var outcome = await LoginAsync(user.Name, user.Password);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        outcome.Response.Required().Token.Should().Be($"token-for:{user.Name}");
        Cookies.Issued.Should().Equal(user.Name);
        var history = (await HistoryAsync()).Should().ContainSingle().Subject;
        history.CreatedUser.Should().Be(user.Name);
        history.Failed.Should().Be(0);
        history.AuthenticationTypeId.Should().Be(1);
        history.FailureTypeId.Should().Be(0);
        history.UserAgent.Should().Be(Marker);
        history.LocalIp.Should().Be("10.0.0.1");
        history.RemoteIp.Should().Be("192.0.2.10");
        history.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        Hash.VerifyCalls.Should().Be(1);
    }

    [Fact]
    public async Task WrongPassword_IsUnauthorised_IssuesNothing_IncrementsTheCounter_AndRecordsFailureType5Async()
    {
        var user = await AddUserAsync("Wrong");

        var outcome = await LoginAsync(user.Name, "not-the-password");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(1);
        var history = (await HistoryAsync()).Should().ContainSingle().Subject;
        history.Failed.Should().Be(1);
        history.FailureTypeId.Should().Be(5);
        history.CreatedUser.Should().Be(user.Name);
        history.AuthenticationTypeId.Should().Be(1);
    }

    [Fact]
    public async Task UnknownUser_IsUnauthorised_AndRecordedAsFailureType1UnderTheAttemptedNameAsync()
    {
        var attempted = $"{Prefix}NoSuchUser";

        var outcome = await LoginAsync(attempted, DefaultPassword);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
        var history = (await HistoryAsync()).Should().ContainSingle().Subject;
        history.CreatedUser.Should().Be(attempted);
        history.FailureTypeId.Should().Be(1);
        history.Failed.Should().Be(1);
        history.AuthenticationTypeId.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(BlankValues))]
    public async Task BlankUserName_FailsValidation_TouchesNothingAsync(string? blank)
    {
        var outcome = await LoginAsync(blank, DefaultPassword);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.ValidationFailed);
        outcome.Validation.Required().Errors.Should().Contain(e => e.PropertyName == "UserName");
        (await HistoryAsync()).Should().BeEmpty();
        Hash.VerifyCalls.Should().Be(0);
        Cookies.Issued.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(BlankValues))]
    public async Task BlankPassword_FailsValidation_TouchesNothing_AndDoesNotCountAsAFailureAsync(string? blank)
    {
        var user = await AddUserAsync("BlankPw");

        var outcome = await LoginAsync(user.Name, blank);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.ValidationFailed);
        outcome.Validation.Required().Errors.Should().Contain(e => e.PropertyName == "Password");
        (await HistoryAsync()).Should().BeEmpty();
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task NullUserAndNullPassword_ReportBothValidationErrorsAsync()
    {
        var outcome = await LoginAsync(null, null);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.ValidationFailed);
        outcome.Validation.Required().Errors.Select(e => e.PropertyName).Should().Contain(["UserName", "Password"]);
    }

    [Theory]
    [MemberData(nameof(HostileNames))]
    public async Task HostileUserName_IsJustAnUnknownUser_AndTheRealUserIsUntouchedAsync(string hostile)
    {
        var real = await AddUserAsync("Real");

        var outcome = await LoginAsync(hostile, DefaultPassword);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
        var reloaded = await ReloadAsync(real);
        reloaded.FailedPasswordCount.Should().Be(0);
        reloaded.PasswordLocked.Should().Be(0);
        var history = (await HistoryAsync()).Should().ContainSingle().Subject;
        history.FailureTypeId.Should().Be(1);
        history.CreatedUser.Should().Be(hostile);
        await using var db = Fx.GetDbContext();
        (await db.UserRegistry.AnyAsync(w => w.Id == real.Id)).Should().BeTrue("the table must survive");
    }

    [Theory]
    [MemberData(nameof(HostilePasswords))]
    public async Task HostilePassword_IsJustAWrongPasswordAsync(string hostile)
    {
        var user = await AddUserAsync("HostilePw");

        var outcome = await LoginAsync(user.Name, hostile);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(1);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(5);
    }

    [Fact]
    public async Task UserNameWithANulByte_IsRejected_AndNothingIsRecordedAsync()
    {
        var user = await AddUserAsync("Nul");

        var outcome = await LoginAsync(user.Name + "\0", user.Password);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().BeEmpty();
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task
        OneMegabyteUserName_IsUnauthorised_ButLeavesNoLoginHistory_BecauseTheIndexRowLimitRejectsTheInsertAsync()
    {
        var huge = new string('u', 1024 * 1024);

        var outcome = await LoginAsync(huge, DefaultPassword);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Weakness_AHundredKilobyteUserName_IsPersistedInFullInTheLoginHistoryAsync()
    {
        var big = new string('u', 100_000);

        var outcome = await LoginAsync(big, DefaultPassword);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().ContainSingle().Which.CreatedUser.Required().Length.Should().Be(big.Length);
    }

    [Fact]
    public async Task OneMegabytePassword_IsJustAWrongPasswordAsync()
    {
        var user = await AddUserAsync("BigPw");

        var outcome = await LoginAsync(user.Name, new string('p', 1024 * 1024));

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(1);
    }

    [Theory]
    [InlineData("Ünïcödé-用户-🔐", "pässwörd-密码-🔐")]
    [InlineData("user with spaces", "pass word")]
    [InlineData("ÄÖÜ", "ß")]
    public async Task UnicodeUserNameAndPassword_AuthenticateExactlyAsync(string name, string password)
    {
        var user = await AddUserAsync("Uni", password: password, exactName: $"{Prefix}{name}");

        var outcome = await LoginAsync(user.Name, password);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(user.Name);
    }

    [Fact]
    public async Task UnicodeNormalisationDifferences_DoNotMatchAsync()
    {
        var composed = $"{Prefix}café";
        await AddUserAsync("Nfc", exactName: composed);

        var outcome = await LoginAsync($"{Prefix}café", DefaultPassword);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
    }

    [Theory]
    [InlineData("upper")]
    [InlineData("lower")]
    [InlineData("swap")]
    [InlineData("leading")]
    [InlineData("trailing")]
    [InlineData("tab")]
    public async Task UserNameMatchingIsExact_CaseAndWhitespaceSensitiveAsync(string variant)
    {
        var user = await AddUserAsync("CaseUser");
        var attempted = variant switch
        {
            "upper" => user.Name.ToUpperInvariant(),
            "lower" => user.Name.ToLowerInvariant(),
            "swap" => new string(user.Name.Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c)).ToArray()),
            "leading" => " " + user.Name,
            "trailing" => user.Name + " ",
            _ => user.Name + "\t"
        };

        var outcome = await LoginAsync(attempted, user.Password);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task PasswordMatchingIsExact_CaseSensitiveAsync()
    {
        var user = await AddUserAsync("CasePw");

        (await LoginAsync(user.Name, user.Password.ToUpperInvariant())).Kind
            .Should().Be(AuthenticationOutcomeKind.Unauthorized);
        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task AuthenticatedIdentityOnTheRequest_WinsOverTheSubmittedUserNameAsync()
    {
        var submitted = await AddUserAsync("Submitted");
        var caller = new NegotiateIdentity(true, $"{Prefix}Other");

        var outcome = await LoginAsync(submitted.Name, submitted.Password, identity: caller);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal($"{Prefix}Other");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("3.5")]
    [InlineData("99999999999")]
    public async Task UnparsablePasswordAttemptsSetting_MakesEveryLoginFail_EvenTheCorrectOneAsync(string setting)
    {
        var user = await AddUserAsync("BadConfig");

        var outcome = await LoginAsync(user.Name, user.Password, env: [("PasswordAttempts", setting)]);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task ThePasswordHashingKeyFromTheEnvironmentIsWhatVerifyReceivesAsync()
    {
        var user = await AddUserAsync("KeyFlow", storedHash: FastHashScheme.Stored(DefaultPassword, "other-key"));

        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Unauthorized);
        (await LoginAsync(user.Name, user.Password, env: [("PasswordHashingKey", "other-key")])).Kind
            .Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task ASpoofedBodyRemoteIp_NeverReachesTheLoginHistory_TheConnectionAddressDoesAsync()
    {
        var user = await AddUserAsync("Ips");

        await LoginAsync(user.Name, user.Password, d =>
        {
            d.RemoteIp = "203.0.113.9";
            d.LocalIp = "spoofed-local";
            d.UserAgent = "spoofed-agent";
        });

        var history = (await HistoryAsync()).Should().ContainSingle().Subject;
        history.RemoteIp.Should().Be("192.0.2.10");
        history.LocalIp.Should().Be("10.0.0.1");
        history.UserAgent.Should().Be(Marker);
        AllLogText().Should().NotContain("203.0.113.9");
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("wrong")]
    [InlineData("locked")]
    [InlineData("inactive")]
    public async Task ASpoofedBodyRemoteIp_IsIgnoredOnFailuresToo_AndWhenTheConnectionHasNoneItIsNullAsync(string kind)
    {
        var user = kind switch
        {
            "locked" => await AddUserAsync("IpsFail", u => u.PasswordLocked = 1),
            "inactive" => await AddUserAsync("IpsFail", u => u.Active = 0),
            _ => await AddUserAsync("IpsFail")
        };
        var name = kind == "unknown" ? $"{Prefix}IpsGhost" : user.Name;
        await using var db = Fx.GetDbContext();

        await Service(db).ByUserNamePasswordAsync(
            new AuthenticationRequestDto { UserName = name, Password = "wrong", RemoteIp = "203.0.113.9" },
            Context(remoteIp: null));

        (await HistoryAsync()).Should().ContainSingle().Which.RemoteIp.Should().BeNull();
    }

    [Fact]
    public async Task ACancelledToken_AbortsBeforeAnyCredentialWorkAsync()
    {
        var user = await AddUserAsync("Cancelled");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var token = cts.Token;
        var act = () => LoginAsync(user.Name, user.Password, token: token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        Cookies.Issued.Should().BeEmpty();
        Hash.VerifyCalls.Should().Be(0);
    }

    [Fact]
    public async Task EachLoginWritesExactlyOneAuditLine_WithAnonymousActor_AndNoCredentialsAsync()
    {
        var user = await AddUserAsync("Audit");

        await LoginAsync(user.Name, user.Password);
        await LoginAsync(user.Name, "wrong-password-marker");

        var lines = Audit.Entries.Select(e => e.Message).ToList();
        lines.Should().HaveCount(2);
        lines[0].Should().Contain("area=Authentication op=ByUserNamePassword actor=(anonymous)").And
            .Contain("outcome=ok");
        lines[1].Should().Contain("outcome=unauthorized");
        lines.Should().NotContain(l => l.Contains(user.Password) || l.Contains("wrong-password-marker")
                                                                 || l.Contains(user.Name));
    }

    [Fact]
    public async Task OAuthOrNegotiateEnabled_HidesTheUserNamePasswordRouteAsync()
    {
        var user = await AddUserAsync("Hidden");

        AssertPlainOutcome(await LoginAsync(user.Name, user.Password, env: [OAuthOn]),
            AuthenticationOutcomeKind.NotFound);
        AssertPlainOutcome(await LoginAsync(user.Name, user.Password, env: [NegotiateOn]),
            AuthenticationOutcomeKind.NotFound);
        (await HistoryAsync()).Should().BeEmpty();
    }
}