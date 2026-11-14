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
using Jube.Data.Repository;
using Jube.Dto.Authentication;
using Jube.Mfa.Totp;
using Jube.Service.Authentication;
using Jube.Service.Authentication.Models;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationTotpTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private const string EncryptionKey = "a long random test key for totp";

    private static readonly (string Key, string Value)[] totpOn =
    [
        MfaOn, ("MultifactorAuthenticationProvider", "Totp"), ("MfaSecretEncryptionKey", EncryptionKey),
        ("MfaTotpIssuer", "JubeTest")
    ];

    private async Task<AuthenticationOutcome> EnrolAsync(TestUser user, string? password,
        params (string Key, string Value)[]? settings)
    {
        await using var db = Fx.GetDbContext();

        return await Service(db, settings is { Length: > 0 } ? settings : totpOn).BeginTotpEnrolmentAsync(
            new AuthenticationRequestDto { UserName = user.Name, Password = password }, Context());
    }

    private async Task<UserRegistryTotpState?> StateAsync(TestUser user)
    {
        await using var db = Fx.GetDbContext();

        return await new UserRegistryTotpRepository(db).GetAsync(user.Name);
    }

    [Fact]
    public async Task ACorrectPasswordReturnsAnOtpauthLinkAndStoresAnEncryptedPendingSecretAsync()
    {
        var user = await AddUserAsync("TotpFirst");

        var outcome = await EnrolAsync(user, user.Password);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Enrolment);
        var enrolment = outcome.Enrolment.Required();
        enrolment.ProvisioningUri.Should().StartWith("otpauth://totp/JubeTest:");
        enrolment.Secret.Should().NotBeNullOrEmpty();
        Cookies.Issued.Should().BeEmpty("enrolment signs nobody in");

        var state = (await StateAsync(user)).Required();
        state.EnrolledDate.Should().BeNull("set up is pending the first valid code");
        state.ProtectedSecret.Should().NotContain(enrolment.Secret);
        TotpAuthenticator.ToBase32(TotpSecretProtector.Unprotect(state.ProtectedSecret, EncryptionKey))
            .Should().Be(enrolment.Secret);
    }

    [Fact]
    public async Task AWrongPasswordRevealsNothingAndStoresNothingAsync()
    {
        var user = await AddUserAsync("TotpWrong");

        var outcome = await EnrolAsync(user, "not the password");

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.Unauthorized);
        (await StateAsync(user)).Required().ProtectedSecret.Should().BeNull();
    }

    [Fact]
    public async Task EnrolmentCountsWrongPasswordsTowardsTheAccountLockoutAsync()
    {
        var user = await AddUserAsync("TotpLock");

        for (var i = 0; i < 4; i++)
        {
            await EnrolAsync(user, "wrong");
        }

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
        AssertPlainOutcome(await EnrolAsync(user, user.Password), AuthenticationOutcomeKind.Unauthorized);
    }

    [Fact]
    public async Task ASecondEnrolmentBeforeTheFirstCodeReplacesThePendingSecretAsync()
    {
        var user = await AddUserAsync("TotpRetry");

        var first = (await EnrolAsync(user, user.Password)).Enrolment.Required();
        var second = (await EnrolAsync(user, user.Password)).Enrolment.Required();

        second.Secret.Should().NotBe(first.Secret);
    }

    [Fact]
    public async Task AUserWhoHasProvedTheirAuthenticatorCannotEnrolAgainWithJustAPasswordAsync()
    {
        var user = await AddUserAsync("TotpDone");
        await EnrolAsync(user, user.Password);
        await using (var db = Fx.GetDbContext())
        {
            await new UserRegistryTotpRepository(db).AcceptStepAsync(user.Name, 100);
        }

        var outcome = await EnrolAsync(user, user.Password);

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.BadRequestMessage);
        (await StateAsync(user)).Required().EnrolledDate.Should().NotBeNull();
    }

    [Theory]
    [InlineData("MultifactorAuthenticationProvider", "Rsa")]
    [InlineData("EnableMultifactorAuthentication", "False")]
    public async Task EnrolmentIsNotFoundUnlessMfaWithTotpIsOnAsync(string key, string value)
    {
        var user = await AddUserAsync("TotpOff");
        var settings = totpOn.Where(s => s.Key != key).Append((key, value)).ToArray();

        var outcome = await EnrolAsync(user, user.Password, settings);

        AssertPlainOutcome(outcome, AuthenticationOutcomeKind.NotFound);
        (await StateAsync(user)).Required().ProtectedSecret.Should().BeNull();
    }

    [Fact]
    public async Task EnrolmentWithNoEncryptionKeyConfiguredFailsAndStoresNothingAsync()
    {
        var user = await AddUserAsync("TotpNoKey");
        var settings = totpOn.Where(s => s.Key != "MfaSecretEncryptionKey").ToArray();

        var act = () => EnrolAsync(user, user.Password, settings);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await StateAsync(user)).Required().ProtectedSecret.Should().BeNull();
    }

    [Fact]
    public async Task TheRepositoryAcceptsAStepOnceAndMarksTheUserEnrolledAsync()
    {
        var user = await AddUserAsync("TotpRepo");
        await using var db = Fx.GetDbContext();
        var repository = new UserRegistryTotpRepository(db);

        (await repository.AcceptStepAsync(user.Name, 5)).Should().Be(0, "there is no secret yet");
        await repository.SetPendingAsync(user.Name, "protected");

        (await repository.AcceptStepAsync(user.Name, 5)).Should().Be(1);
        (await repository.AcceptStepAsync(user.Name, 5)).Should().Be(0, "the same step is never accepted twice");
        (await repository.AcceptStepAsync(user.Name, 4)).Should().Be(0, "an earlier step is never accepted");
        (await repository.AcceptStepAsync(user.Name, 6)).Should().Be(1);

        var state = (await repository.GetAsync(user.Name)).Required();
        state.EnrolledDate.Should().NotBeNull();
        state.LastAcceptedStep.Should().Be(6);
        (await repository.SetPendingAsync(user.Name, "other")).Should().Be(0, "an enrolled user is never overwritten");
    }

    [Fact]
    public async Task ResettingClearsTheSecretSoTheUserCanEnrolAgainAsync()
    {
        var user = await AddUserAsync("TotpReset");
        await using var db = Fx.GetDbContext();
        var repository = new UserRegistryTotpRepository(db);
        await repository.SetPendingAsync(user.Name, "protected");
        await repository.AcceptStepAsync(user.Name, 9);

        (await repository.ResetByIdAsync(user.Id)).Should().Be(1);

        var state = (await repository.GetAsync(user.Name)).Required();
        state.ProtectedSecret.Should().BeNull();
        state.EnrolledDate.Should().BeNull();
        state.LastAcceptedStep.Should().BeNull();
        (await repository.SetPendingAsync(user.Name, "fresh")).Should().Be(1);
    }
}
