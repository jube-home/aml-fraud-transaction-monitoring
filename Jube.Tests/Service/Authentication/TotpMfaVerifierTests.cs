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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code;
using Jube.Data.Repository;
using Jube.Mfa.Totp;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class TotpMfaVerifierTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private const string EncryptionKey = "a long random test key for the verifier";

    private static TotpMfaVerifier Verifier(string key = EncryptionKey)
    {
        var connection = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                         ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=x;";

        return new TotpMfaVerifier(TestDynamicEnvironment.Create(new Dictionary<string, string>
        {
            ["ConnectionString"] = connection, ["MfaSecretEncryptionKey"] = key
        }), TestLog.NoOp);
    }

    private async Task<byte[]> PendingAsync(TestUser user)
    {
        var secret = TotpAuthenticator.GenerateSecret();
        await using var db = Fx.GetDbContext();
        await new UserRegistryTotpRepository(db).SetPendingAsync(user.Name,
            TotpSecretProtector.Protect(secret, EncryptionKey));

        return secret;
    }

    private static string Current(byte[] secret) => new OtpNet.Totp(secret).ComputeTotp(DateTime.UtcNow);

    private async Task<UserRegistryTotpState> StateAsync(TestUser user)
    {
        await using var db = Fx.GetDbContext();

        return (await new UserRegistryTotpRepository(db).GetAsync(user.Name)).Required();
    }

    [Fact]
    public async Task AUserWithNoAuthenticatorCannotPassAsync()
    {
        var user = await AddUserAsync("TvNone");

        (await Verifier().VerifyAsync(user.Name, "123456")).Should().BeFalse();
    }

    [Fact]
    public async Task TheFirstValidCodeCompletesSetUpAndTheSameCodeCannotBeUsedAgainAsync()
    {
        var user = await AddUserAsync("TvFirst");
        var secret = await PendingAsync(user);
        var code = Current(secret);

        (await Verifier().VerifyAsync(user.Name, code)).Should().BeTrue();

        var state = await StateAsync(user);
        state.EnrolledDate.Should().NotBeNull();
        state.LastAcceptedStep.Should().NotBeNull();

        (await Verifier().VerifyAsync(user.Name, code)).Should().BeFalse("a code is single use");
    }

    [Fact]
    public async Task AWrongCodeIsRefusedAndDoesNotCompleteSetUpAsync()
    {
        var user = await AddUserAsync("TvWrong");
        var secret = await PendingAsync(user);
        var wrong = Current(secret) == "000000" ? "111111" : "000000";

        (await Verifier().VerifyAsync(user.Name, wrong)).Should().BeFalse();

        (await StateAsync(user)).EnrolledDate.Should().BeNull();
    }

    [Fact]
    public async Task ACodeForAnotherUsersSecretIsRefusedAsync()
    {
        var alice = await AddUserAsync("TvAlice");
        var bob = await AddUserAsync("TvBob");
        await PendingAsync(alice);
        var bobsSecret = await PendingAsync(bob);

        (await Verifier().VerifyAsync(alice.Name, Current(bobsSecret))).Should().BeFalse();
    }

    [Fact]
    public async Task AWrongEncryptionKeyFailsClosedWithoutThrowingAsync()
    {
        var user = await AddUserAsync("TvKey");
        var secret = await PendingAsync(user);

        (await Verifier("not the key").VerifyAsync(user.Name, Current(secret))).Should().BeFalse();
        (await Verifier("").VerifyAsync(user.Name, Current(secret))).Should().BeFalse();
    }
}
