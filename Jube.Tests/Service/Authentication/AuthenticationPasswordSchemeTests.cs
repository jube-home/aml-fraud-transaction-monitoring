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
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Security;
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;
using AuthEngine = Jube.Service.Authentication.Authentication;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationPasswordSchemeTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private (string, string)[] Encrypted(bool enabled = true) =>
    [
        ("PasswordAsymmetricEncryption", enabled ? "True" : "False"),
        ("PasswordAsymmetricEncryptionPrivateKey", TestRsa.PrivateKeyEnv()),
        ("PasswordAsymmetricEncryptionPublicKey", "unused-by-the-server-path")
    ];

    private Task<AuthenticationOutcome> EncryptedLoginAsync(string user, string password,
        Action<AuthenticationRequestDto>? tweak = null, bool enabled = true)
    {
        return LoginAsync(user, password, tweak, env: Encrypted(enabled));
    }

    [Fact]
    public void Argon2_HashesVerify_AreSalted_AndCarryTheArgon2Prefix()
    {
        var scheme = new Argon2PasswordHashScheme();

        var first = scheme.Hash(DefaultPassword, HashKey);
        var second = scheme.Hash(DefaultPassword, HashKey);

        first.Should().StartWith("$argon2");
        first.Should().NotBe(second);
        scheme.Verify(first, DefaultPassword, HashKey).Should().BeTrue();
        scheme.Verify(second, DefaultPassword, HashKey).Should().BeTrue();
        first.Should().NotContain(DefaultPassword);
    }

    [Theory]
    [InlineData("wrong", "same-key", false)]
    [InlineData("Correct#Horse9battery", "other-key", false)]
    [InlineData("Correct#Horse9battery ", "same-key", false)]
    [InlineData("correct#horse9battery", "same-key", false)]
    [InlineData("", "same-key", false)]
    [InlineData("Correct#Horse9battery", "same-key", true)]
    public void Argon2_Verify_IsExactOnPasswordAndKey(string attempt, string key, bool expected)
    {
        var scheme = new Argon2PasswordHashScheme();
        var hash = scheme.Hash(DefaultPassword, "same-key");

        scheme.Verify(hash, attempt, key).Should().Be(expected);
    }

    [Fact]
    public void Argon2_NullKey_NeverVerifies_EvenTheRightPassword()
    {
        var scheme = new Argon2PasswordHashScheme();
        var hash = scheme.Hash(DefaultPassword, "k");

        scheme.Verify(hash, DefaultPassword, null).Should().BeFalse();
    }

    [Fact]
    public void Argon2_EmptyKey_UsesTheUnkeyedForm_AndDoesNotVerifyAgainstAKeyedHash()
    {
        var scheme = new Argon2PasswordHashScheme();
        var unkeyed = scheme.Hash(DefaultPassword, "");
        var keyed = scheme.Hash(DefaultPassword, "k");

        scheme.Verify(unkeyed, DefaultPassword, "").Should().BeTrue();
        scheme.Verify(keyed, DefaultPassword, "").Should().BeFalse();
        scheme.Verify(unkeyed, DefaultPassword, "k").Should().BeFalse();
    }

    [Theory]
    [InlineData("Ünïcödé-密码-🔐")]
    [InlineData("pass\0word")]
    [InlineData("  spaced  ")]
    public void Argon2_HandlesUnusualPasswords_ExactlyAndFailsClosedOnNeighbours(string password)
    {
        var scheme = new Argon2PasswordHashScheme();
        var hash = scheme.Hash(password, HashKey);

        scheme.Verify(hash, password, HashKey).Should().BeTrue();
        scheme.Verify(hash, password + "x", HashKey).Should().BeFalse();
        scheme.Verify(hash, password.Trim(), HashKey).Should().Be(password.Trim() == password);
    }

    [Fact]
    public void Argon2_OneMegabytePassword_HashesAndVerifies()
    {
        var scheme = new Argon2PasswordHashScheme();
        var huge = new string('h', 1024 * 1024);
        var hash = scheme.Hash(huge, HashKey);

        scheme.Verify(hash, huge, HashKey).Should().BeTrue();
        scheme.Verify(hash, huge[..^1], HashKey).Should().BeFalse();
    }

    [Fact]
    public void Argon2_ATamperedHash_FailsClosed_NeverVerifies()
    {
        var scheme = new Argon2PasswordHashScheme();
        var hash = scheme.Hash(DefaultPassword, HashKey);
        var flipped = hash[..^2] + (hash[^2] == 'A' ? 'B' : 'A') + hash[^1];

        Action verify = () => scheme.Verify(flipped, DefaultPassword, HashKey).Should().BeFalse();

        verify.Should().NotThrow();
    }

    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    public void Sha256_ProducesLowerCaseHexOfTheUtf8Bytes(string input, string expected)
    {
        HashPassword.Sha256(input).Should().Be(expected);
    }

    [Fact]
    public void Sha256_IsDeterministic_AndUnicodeSafe()
    {
        HashPassword.Sha256("密码").Should().Be(HashPassword.Sha256("密码")).And.HaveLength(64);
        HashPassword.Sha256("a").Should().NotBe(HashPassword.Sha256("A"));
    }

    [Fact]
    public async Task RealArgon2_EndToEndLogin_AcceptsTheRightPasswordAndRejectsTheWrongOneAsync()
    {
        var hash = HashPassword.Argon2(DefaultPassword, HashKey);
        var user = await AddUserAsync("RealArgon", storedHash: hash);
        await using var db = Fx.GetDbContext();
        var service = Service(db, new Argon2PasswordHashScheme());

        var wrong = await service.ByUserNamePasswordAsync(
            new AuthenticationRequestDto { UserName = user.Name, Password = "nope" }, Context());
        var right = await service.ByUserNamePasswordAsync(
            new AuthenticationRequestDto { UserName = user.Name, Password = DefaultPassword }, Context());

        wrong.Kind.Should().Be(AuthenticationOutcomeKind.Unauthorized);
        right.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task RealArgon2_NewPasswordIsStoredAsAnArgon2Hash_AndVerifiesAfterwardsAsync()
    {
        var hash = HashPassword.Argon2(DefaultPassword, HashKey);
        var user = await AddUserAsync("RealChange", storedHash: hash);
        await using var db = Fx.GetDbContext();
        var service = Service(db, new Argon2PasswordHashScheme());

        var changed = await service.ByUserNamePasswordAsync(new AuthenticationRequestDto
            { UserName = user.Name, Password = DefaultPassword, NewPassword = "Brand#NewPassword12" }, Context());

        changed.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        var stored = (await ReloadAsync(user)).Password;
        stored.Should().StartWith("$argon2").And.NotContain("Brand#NewPassword12");
        HashPassword.Verify(stored, "Brand#NewPassword12", HashKey).Should().BeTrue();
        HashPassword.Verify(stored, DefaultPassword, HashKey).Should().BeFalse();
    }

    [Fact]
    public async Task AWrongPasswordCostsTheSameHashWorkAsARightOne_ForAKnownUserAsync()
    {
        var user = await AddUserAsync("SameWork");

        await LoginAsync(user.Name, "wrong");
        var afterWrong = Hash.VerifyCalls;
        await LoginAsync(user.Name, user.Password);

        afterWrong.Should().Be(1);
        Hash.VerifyCalls.Should().Be(2);
    }

    [Theory]
    [InlineData("unknown", true)]
    [InlineData("wire1", true)]
    [InlineData("wire0", false)]
    [InlineData("wirenull", false)]
    [InlineData("deleted", true)]
    public async Task WirePasswordHash_ReflectsTheUsersFlag_AndUnknownAndDeletedUsersLookLikeWireUsersAsync(string kind,
        bool expected)
    {
        var user = kind switch
        {
            "wire1" => await AddUserAsync("Wire", u => u.WirePasswordHash = 1),
            "wire0" => await AddUserAsync("Wire", u => u.WirePasswordHash = 0),
            "wirenull" => await AddUserAsync("Wire", u => u.WirePasswordHash = null),
            "deleted" => await AddUserAsync("Wire", u =>
            {
                u.WirePasswordHash = 0;
                u.Deleted = 1;
            }),
            _ => new TestUser($"{Prefix}Nobody", "", 0, "")
        };
        await using var db = Fx.GetDbContext();

        var outcome = await Service(db).WirePasswordHashAsync(new AuthenticationSchemaRequestDto(user.Name),
            NegotiateIdentity.Anonymous);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        outcome.WirePasswordHash.Should().Be(expected);
        outcome.Response.Should().BeNull();
        (await HistoryAsync()).Should().BeEmpty("the probe is not a login attempt");
    }

    [Fact]
    public async Task WirePasswordHash_NullUserName_IsFalse_WithoutTouchingTheDatabaseAsync()
    {
        await using var db = Fx.GetDbContext();

        var outcome = await Service(db).WirePasswordHashAsync(new AuthenticationSchemaRequestDto(null),
            NegotiateIdentity.Anonymous);

        outcome.WirePasswordHash.Should().BeFalse();
    }

    [Fact]
    public async Task Weakness_WirePasswordHash_ProbeDistinguishesWireZeroUsersFromUnknownOnesAsync()
    {
        var wire0 = await AddUserAsync("Probe0", u => u.WirePasswordHash = 0);
        await using var db = Fx.GetDbContext();
        var service = Service(db);

        var known = await service.WirePasswordHashAsync(new AuthenticationSchemaRequestDto(wire0.Name),
            NegotiateIdentity.Anonymous);
        var unknown = await service.WirePasswordHashAsync(new AuthenticationSchemaRequestDto($"{Prefix}Ghost"),
            NegotiateIdentity.Anonymous);

        known.WirePasswordHash.Should().BeFalse();
        unknown.WirePasswordHash.Should().BeTrue();
    }

    [Theory]
    [InlineData("oauth", false)]
    [InlineData("oauth", true)]
    [InlineData("negotiate", false)]
    [InlineData("negotiate", true)]
    public async Task WirePasswordHash_IsAlways404_WhenAnotherSchemeIsEnabled_EvenForAnAuthenticatedCallerAsync(
        string scheme,
        bool authenticated)
    {
        await using var db = Fx.GetDbContext();
        var identity = new NegotiateIdentity(authenticated, authenticated ? "someone" : null);

        var outcome = await Service(db, scheme == "oauth" ? OAuthOn : NegotiateOn)
            .WirePasswordHashAsync(new AuthenticationSchemaRequestDto("x"), identity);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.NotFound);
    }

    [Fact]
    public async Task Encryption_On_ACorrectlyEncryptedPassword_AuthenticatesAsync()
    {
        var user = await AddUserAsync("EncOk");

        var outcome = await EncryptedLoginAsync(user.Name, TestRsa.Encrypt(user.Password));

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        Cookies.Issued.Should().Equal(user.Name);
    }

    [Fact]
    public async Task Encryption_On_AnEncryptedWrongPassword_IsABadCredential_AndCountsAsync()
    {
        var user = await AddUserAsync("EncWrong");

        var outcome = await EncryptedLoginAsync(user.Name, TestRsa.Encrypt("wrong-password"));

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(1);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(5);
    }

    [Fact]
    public async Task Encryption_On_UnicodePassword_RoundTripsAsync()
    {
        var user = await AddUserAsync("EncUni", password: "pässwörd-密码-🔐");

        (await EncryptedLoginAsync(user.Name, TestRsa.Encrypt(user.Password))).Kind
            .Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task Encryption_On_ThePlaintextPassword_IsNotAcceptable_AndLeavesNoTraceAsync()
    {
        var user = await AddUserAsync("EncPlain");

        var outcome = await EncryptedLoginAsync(user.Name, user.Password);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task Encryption_On_ACiphertextForTheWrongPrivateKey_IsRejectedAsync()
    {
        var user = await AddUserAsync("EncWrongKey");

        var outcome = await EncryptedLoginAsync(user.Name, TestRsa.Encrypt(user.Password, true));

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
    }

    [Theory]
    [InlineData("flip-first")]
    [InlineData("flip-middle")]
    [InlineData("flip-last")]
    [InlineData("truncate")]
    [InlineData("extend")]
    [InlineData("garbage-base64")]
    [InlineData("not-base64")]
    [InlineData("padding-only")]
    public async Task Encryption_On_TamperedOrGarbledCiphertext_NeverAuthenticatesAsync(string tamper)
    {
        var user = await AddUserAsync("EncTamper");
        var bytes = Convert.FromBase64String(TestRsa.Encrypt(user.Password));
        var text = tamper switch
        {
            "flip-first" => Convert.ToBase64String(bytes.Select((b, i) => i == 0 ? (byte)(b ^ 1) : b).ToArray()),
            "flip-middle" => Convert.ToBase64String(bytes.Select((b, i) => i == 100 ? (byte)(b ^ 1) : b).ToArray()),
            "flip-last" => Convert.ToBase64String(bytes.Select((b, i) => i == bytes.Length - 1 ? (byte)(b ^ 1) : b)
                .ToArray()),
            "truncate" => Convert.ToBase64String(bytes[..^1]),
            "extend" => Convert.ToBase64String([.. bytes, 0]),
            "garbage-base64" => Convert.ToBase64String(RandomNumberGenerator.GetBytes(256)),
            "not-base64" => "%%%not*base64%%%",
            _ => "===="
        };

        var outcome = await EncryptedLoginAsync(user.Name, text);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task Encryption_On_WhitespaceInsideTheBase64_IsIgnoredByTheDecoder_SoStillAuthenticatesAsync()
    {
        var user = await AddUserAsync("EncWhitespace");
        var cipher = TestRsa.Encrypt(user.Password);

        var outcome = await EncryptedLoginAsync(user.Name, cipher[..10] + " \r\n" + cipher[10..]);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task UndecryptableCiphertext_IsACountedFailure_LocksTheAccount_AndLeavesAHistoryRowAsync()
    {
        var user = await AddUserAsync("EncCounted");

        for (var i = 0; i < 3; i++)
        {
            AssertPlainOutcome(await EncryptedLoginAsync(user.Name, "%%%garbage%%%"),
                AuthenticationOutcomeKind.Unauthorized);
        }

        var reloaded = await ReloadAsync(user);
        reloaded.FailedPasswordCount.Should().Be(3);
        reloaded.PasswordLocked.Should().Be(1);
        (await HistoryAsync()).Should().HaveCount(3).And.OnlyContain(h => h.FailureTypeId == 5);
    }

    [Fact]
    public async Task Encryption_Off_ACiphertextIsJustAWrongPasswordAsync()
    {
        var user = await AddUserAsync("EncOff");

        var outcome = await EncryptedLoginAsync(user.Name, TestRsa.Encrypt(user.Password), enabled: false);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(1);
    }

    [Fact]
    public async Task Encryption_Off_ThePlaintextAuthenticatesAsync()
    {
        var user = await AddUserAsync("EncOffPlain");

        (await EncryptedLoginAsync(user.Name, user.Password, enabled: false)).Kind
            .Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Fact]
    public async Task Weakness_EncryptionOnWithoutAPrivateKey_SilentlyAcceptsPlaintextPasswordsAsync()
    {
        var user = await AddUserAsync("EncDowngrade");
        await using var db = Fx.GetDbContext();
        var service = new AuthEngine(db, true, null, Hash, Clock);

        var act = () => service.AuthenticateByUserNamePasswordAsync(
            new AuthenticationRequestDto { UserName = user.Name, Password = user.Password }, HashKey);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Encryption_On_TheNewPasswordIsDecryptedBeforeItIsHashedAndStoredAsync()
    {
        var user = await AddUserAsync("EncNew");

        var outcome = await EncryptedLoginAsync(user.Name, TestRsa.Encrypt(user.Password),
            d => d.NewPassword = TestRsa.Encrypt("Brand#NewPassword12"));

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        (await ReloadAsync(user)).Password.Should().Be(FastHashScheme.Stored("Brand#NewPassword12", HashKey));
    }

    [Fact]
    public async Task Encryption_On_AnUndecryptableNewPassword_ThrowsAfterTheOldOneVerified_AndChangesNothingAsync()
    {
        var user = await AddUserAsync("EncBadNew");

        var outcome = await EncryptedLoginAsync(user.Name, TestRsa.Encrypt(user.Password),
            d => d.NewPassword = "%%%garbage%%%");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Fact]
    public async Task CorrectPasswordWithANewOne_ChangesThePassword_ResetsCounters_AndExtendsExpiryBy90DaysAsync()
    {
        var user = await AddUserAsync("Change", u => u.FailedPasswordCount = 2);

        var outcome = await LoginAsync(user.Name, user.Password, d => d.NewPassword = "Brand#NewPassword12");

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        var reloaded = await ReloadAsync(user);
        reloaded.Password.Should().Be(FastHashScheme.Stored("Brand#NewPassword12", HashKey));
        reloaded.FailedPasswordCount.Should().Be(0);
        reloaded.PasswordLocked.Should().Be(0);
        reloaded.PasswordExpiryDate.Should().BeCloseTo(Clock.GetUtcNow().UtcDateTime.AddDays(90),
            TimeSpan.FromSeconds(1));
        Hash.HashCalls.Should().Be(1);
        (await LoginAsync(user.Name, "Brand#NewPassword12")).Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        (await LoginAsync(user.Name, user.Password)).Kind.Should().Be(AuthenticationOutcomeKind.Unauthorized);
    }

    [Fact]
    public async Task AnExpiredPassword_CanBeReplaced_ByPresentingTheOldOneWithANewOneAsync()
    {
        var user = await AddUserAsync("ExpiredChange", u => u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-5));

        var outcome = await LoginAsync(user.Name, user.Password, d => d.NewPassword = "Brand#NewPassword12");

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
    }

    [Theory]
    [InlineData("Sh0rt!a", "At least 12 characters")]
    [InlineData("alllowercase#1234", "At least one uppercase letter")]
    [InlineData("ALLUPPERCASE#1234", "At least one lowercase letter")]
    [InlineData("NoDigitsHere#abcd", "At least one number")]
    [InlineData("NoSpecialChar1234", "At least one special character")]
    [InlineData("Repeatedddd#1234", "No character repeated more than twice consecutively")]
    [InlineData("Password#12345678", "Cannot start with common patterns")]
    [InlineData("qwertyUIOP#12345", "Cannot start with common patterns")]
    [InlineData("123456Abcdef#xyz", "Cannot start with common patterns")]
    public async Task AWeakNewPassword_IsRejectedWithTheSpecificRule_AndNothingChangesAsync(string weak, string rule)
    {
        var user = await AddUserAsync("Weak");

        var outcome = await LoginAsync(user.Name, user.Password, d => d.NewPassword = weak);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.PasswordStrength);
        outcome.Errors.Should().Contain(rule);
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
        Cookies.Issued.Should().BeEmpty();
    }

    [Fact]
    public async Task ANewPasswordLongerThan128Characters_IsRejectedAsync()
    {
        var user = await AddUserAsync("TooLong");

        var outcome = await LoginAsync(user.Name, user.Password, d => d.NewPassword = "Aa1#" + new string('x', 130));

        outcome.Errors.Should().Contain("No more than 128 characters");
    }

    [Fact]
    public async Task ManyWeaknesses_AreAllReportedTogetherAsync()
    {
        var user = await AddUserAsync("ManyWeak");

        var outcome = await LoginAsync(user.Name, user.Password, d => d.NewPassword = "aaa");

        outcome.Errors.Should().HaveCountGreaterThan(3);
    }

    [Fact]
    public async Task Weakness_WirePasswordHashUsers_HaveNoStrengthRulesApplied_BecauseTheServerOnlySeesAHashAsync()
    {
        var user = await AddUserAsync("WireWeak", u => u.WirePasswordHash = 1);

        var outcome = await LoginAsync(user.Name, user.Password, d => d.NewPassword = "a");

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        var reloaded = await ReloadAsync(user);
        reloaded.Password.Should().Be(FastHashScheme.Stored("a", HashKey));
        reloaded.WirePasswordHash.Should().Be(1);
    }

    [Fact]
    public async Task ANewPasswordWithAWrongCurrentOne_ChangesNothingAsync()
    {
        var user = await AddUserAsync("ChangeWrong");

        var outcome = await LoginAsync(user.Name, "wrong", d => d.NewPassword = "Brand#NewPassword12");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Fact]
    public async Task ALockedUser_CannotChangeThePasswordThroughLoginAsync()
    {
        var user = await AddUserAsync("ChangeLocked", u => u.PasswordLocked = 1);

        var outcome = await LoginAsync(user.Name, user.Password, d => d.NewPassword = "Brand#NewPassword12");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Fact]
    public async Task PasswordChangeState_WithABlankPassword_IsAPasswordEmptyFailure_Type6Async()
    {
        var user = await AddUserAsync("ChangeState");

        var outcome = await LoginAsync(user.Name, "", d =>
        {
            d.PasswordChangeState = true;
            d.NewPassword = "Brand#NewPassword12";
        });

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(6);
    }

    [Fact]
    public async Task PasswordChangeState_WithoutANewPassword_FailsValidationAsync()
    {
        var user = await AddUserAsync("ChangeStateNoNew");

        var outcome = await LoginAsync(user.Name, user.Password, d => d.PasswordChangeState = true);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.ValidationFailed);
        outcome.Validation.Required().Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    private async Task<AuthenticationOutcome> ChangeAsync(string? identityName, string? current, string? next,
        params (string, string)[] env)
    {
        await using var db = Fx.GetDbContext();
        return await Service(db, env).ChangePasswordAsync(new ChangePasswordRequestDto(current, next),
            new NegotiateIdentity(identityName != null, identityName));
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrentAndStrongNew_ReplacesTheHash_WithAnEmptySuccessAsync()
    {
        var user = await AddUserAsync("Cp");

        var outcome = await ChangeAsync(user.Name, user.Password, "Brand#NewPassword12");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Ok);
        var reloaded = await ReloadAsync(user);
        reloaded.Password.Should().Be(FastHashScheme.Stored("Brand#NewPassword12", HashKey));
        reloaded.PasswordExpiryDate.Should().BeCloseTo(Clock.GetUtcNow().UtcDateTime.AddDays(90),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_IsUnauthorised_ChangesNothing_AndIsNotCountedAsync()
    {
        var user = await AddUserAsync("CpWrong");

        for (var i = 0; i < 6; i++)
        {
            AssertPlainOutcome(await ChangeAsync(user.Name, $"guess{i}", "Brand#NewPassword12"),
                AuthenticationOutcomeKind.Unauthorized);
        }

        var reloaded = await ReloadAsync(user);
        reloaded.Password.Should().Be(user.StoredHash);
        reloaded.FailedPasswordCount.Should().Be(0);
        reloaded.PasswordLocked.Should().Be(0);
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ChangePassword_MissingNewPassword_IsAStrengthFailureAsync(string? next)
    {
        var user = await AddUserAsync("CpMissing");

        var outcome = await ChangeAsync(user.Name, user.Password, next);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.PasswordStrength);
        outcome.Errors.Should().Equal("Missing New Password.");
    }

    [Fact]
    public async Task ChangePassword_WeakNew_ListsTheBrokenRulesAsync()
    {
        var user = await AddUserAsync("CpWeak");

        var outcome = await ChangeAsync(user.Name, user.Password, "short");

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.PasswordStrength);
        outcome.Errors.Should().Contain("At least 12 characters");
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Theory]
    [InlineData("oauth")]
    [InlineData("negotiate")]
    public async Task ChangePassword_IsForbidden_WhenAnotherSchemeOwnsIdentityAsync(string scheme)
    {
        var user = await AddUserAsync("CpScheme");

        var outcome = await ChangeAsync(user.Name, user.Password, "Brand#NewPassword12",
            scheme == "oauth" ? OAuthOn : NegotiateOn);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Forbidden);
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Fact]
    public async Task ChangePassword_WithoutAnIdentityName_IsABadRequestWithTheLegacyMessageAsync()
    {
        var outcome = await ChangeAsync(null, "a", "b");

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.BadRequestMessage);
        outcome.Message.Should().Be("Authorized but the username is null.");
    }

    [Fact]
    public async Task ChangePassword_EmptyCurrentPassword_IsABadRequest_NotAnExceptionAsync()
    {
        var user = await AddUserAsync("CpEmpty");

        var outcome = await ChangeAsync(user.Name, "", "Brand#NewPassword12");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.BadRequest);
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Fact]
    public async Task ChangePassword_ForAnIdentityWithNoUserRecord_IsUnauthorised_WithEquivalentHashWorkAsync()
    {
        var outcome = await ChangeAsync($"{Prefix}Ghost", "a", "Brand#NewPassword12");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        Hash.DummyVerifyCalls.Should().Be(1);
        Hash.RealVerifyCalls.Should().Be(0);
    }

    [Fact]
    public async Task Weakness_ChangePassword_UnlocksALockedAccount_BecauseSetPasswordClearsTheLockAsync()
    {
        var user = await AddUserAsync("CpUnlock", u =>
        {
            u.PasswordLocked = 1;
            u.FailedPasswordCount = 9;
        });

        await ChangeAsync(user.Name, user.Password, "Brand#NewPassword12");

        var reloaded = await ReloadAsync(user);
        reloaded.PasswordLocked.Should().Be(0);
        reloaded.FailedPasswordCount.Should().Be(0);
    }

    [Fact]
    public async Task ChangePassword_EncryptedCurrentAndNew_AreDecryptedAsync()
    {
        var user = await AddUserAsync("CpEnc");

        var outcome = await ChangeAsync(user.Name, TestRsa.Encrypt(user.Password),
            TestRsa.Encrypt("Brand#NewPassword12"), Encrypted());

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Ok);
        (await ReloadAsync(user)).Password.Should().Be(FastHashScheme.Stored("Brand#NewPassword12", HashKey));
    }
}