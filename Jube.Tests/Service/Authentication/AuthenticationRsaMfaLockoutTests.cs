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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using AuthService = Jube.Service.Authentication.AuthenticationLoginService;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationRsaMfaLockoutTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private WebApplication? mock;

    public override async Task DisposeAsync()
    {
        if (mock != null)
        {
            await mock.StopAsync();
            await mock.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    private async Task<AuthService> ServiceAsync(DbContext db)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        mock = builder.Build();
        Jube.App.Endpoints.Mocks.MockRsaMfaEndpoints.MapMockRsaMfaEndpoints(mock);
        await mock.StartAsync();
        var endpoint = mock.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Required()
            .Addresses
            .Single() + "/api/mfa";
        var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
        {
            ["MultifactorAuthenticationEndpoint"] = endpoint,
            ["EnableMultifactorAuthentication"] = "True"
        });
        return new AuthService(db, environment, Log, Cookies, new RsaMfaVerifier(environment, Log), Hash, Clock, Audit);
    }

    [Fact]
    public async Task WrongRsaCodes_AreCounted_LockTheAccount_AndTheRightCodeIsThenRefusedAsync()
    {
        var user = await AddUserAsync("RsaLock");
        await using var db = Fx.GetDbContext();
        var service = await ServiceAsync(db);

        AuthenticationRequestDto Request(string code) =>
            new() { UserName = user.Name, Password = user.Password, Mfa = code };

        for (var i = 0; i < 5; i++)
        {
            (await service.ByUserNamePasswordAsync(Request("00000000"), Context())).Kind
                .Should().Be(AuthenticationOutcomeKind.Unauthorized);
        }

        (await ReloadAsync(user)).PasswordLocked.Should().Be(1);
        (await service.ByUserNamePasswordAsync(Request("12345678"), Context())).Kind
            .Should().Be(AuthenticationOutcomeKind.Unauthorized);
        Cookies.Issued.Should().BeEmpty();
        AllLogText().Should().NotContain("12345678").And.NotContain("00000000");
    }

    [Fact]
    public async Task TheRightRsaCode_CompletesTheLoginAndResetsTheCounterAsync()
    {
        var user = await AddUserAsync("RsaOk", u => u.FailedPasswordCount = 2);
        await using var db = Fx.GetDbContext();
        var service = await ServiceAsync(db);

        var outcome = await service.ByUserNamePasswordAsync(
            new AuthenticationRequestDto { UserName = user.Name, Password = user.Password, Mfa = "12345678" },
            Context());

        outcome.Kind.Should().Be(AuthenticationOutcomeKind.Ok);
        (await ReloadAsync(user)).FailedPasswordCount.Should().Be(0);
        AllLogText().Should().NotContain("12345678");
    }
}