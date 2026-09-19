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
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code;
using Jube.Test.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
public sealed class AuthenticationRsaMfaTests : IAsyncLifetime
{
    private const string GoodClientKey = "SomethingSecretForTheClientKeyHeader";
    private readonly ConcurrentQueue<string> bodies = new();
    private WebApplication? app;
    private string endpoint = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var host = builder.Build();
        app = host;
        host.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            using (var reader = new System.IO.StreamReader(context.Request.Body, leaveOpen: true))
            {
                bodies.Enqueue(await reader.ReadToEndAsync());
            }

            context.Request.Body.Position = 0;
            await next(context);
        });
        Jube.App.Endpoints.Mocks.MockRsaMfaEndpoints.MapMockRsaMfaEndpoints(host);
        await host.StartAsync();
        endpoint = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Required()
            .Addresses
            .Single() + "/api/mfa";
    }

    public async Task DisposeAsync()
    {
        if (app == null)
        {
            return;
        }

        await app.StopAsync();
        await app.DisposeAsync();
    }

    private RsaMfaVerifier Verifier(TestLog log, params (string Key, string Value)[] overrides)
    {
        var settings = new System.Collections.Generic.Dictionary<string, string>
            { ["MultifactorAuthenticationEndpoint"] = endpoint };
        foreach (var (k, v) in overrides)
        {
            settings[k] = v;
        }

        return new RsaMfaVerifier(TestDynamicEnvironment.Create(settings), log);
    }

    [Fact]
    public async Task TheCorrectCode_VerifiesAsync()
    {
        (await Verifier(new TestLog()).VerifyAsync("alice", "12345678")).Should().BeTrue();
    }

    [Theory]
    [InlineData("00000000")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData(" 12345678")]
    [InlineData("12345678 ")]
    [InlineData("")]
    [InlineData("' OR '1'='1")]
    public async Task AnyOtherCode_IsRejectedAsync(string code)
    {
        (await Verifier(new TestLog()).VerifyAsync("alice", code)).Should().BeFalse();
    }

    [Fact]
    public async Task TheRequest_CarriesTheSubjectClientIdAndTheSecuridFactorAsync()
    {
        await Verifier(new TestLog()).VerifyAsync("alice", "12345678");

        using var document = JsonDocument.Parse(bodies.Single());
        var root = document.RootElement;
        root.GetProperty("subjectName").GetString().Should().Be("alice");
        root.GetProperty("clientId").GetString().Should().Be("MyApplicationGuidOrSomeSuch");
        var credential = root.GetProperty("subjectCredentials")[0];
        credential.GetProperty("methodId").GetString().Should().Be("SECURID");
        credential.GetProperty("collectedInputs")[0].GetProperty("value").GetString().Should().Be("12345678");
    }

    [Fact]
    public Task AWrongClientKey_IsAProviderFailure_NotAFalseAsync()
    {
        var act = () => Verifier(new TestLog(), ("MultifactorAuthenticationClientKey", "wrong-key"))
            .VerifyAsync("alice", "12345678");

        return act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public Task AnUnreachableProvider_ThrowsRatherThanAcceptingTheCodeAsync()
    {
        var act = () => Verifier(new TestLog(), ("MultifactorAuthenticationEndpoint", "http://127.0.0.1:1/api/mfa"))
            .VerifyAsync("alice", "12345678");

        return act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ACancelledToken_AbortsTheCallAsync()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var token = cts.Token;
        var act = () => Verifier(new TestLog()).VerifyAsync("alice", "12345678", token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("00000000")]
    public async Task TheRsaProviderNeverLogsTheMfaCode_OnSuccessOrRejectionAsync(string code)
    {
        var log = new TestLog();

        await Verifier(log).VerifyAsync("alice", code);

        log.Entries.Should().NotBeEmpty();
        log.Entries.Should().NotContain(e => (e.Message + e.Exception).Contains(code));
        log.Entries.Should().NotContain(e => e.Message.Contains("collectedInputs")
                                             || e.Message.Contains("subjectCredentials"));
    }

    [Fact]
    public async Task TheProviderLog_NeverContainsTheClientKeyAsync()
    {
        var log = new TestLog();

        await Verifier(log).VerifyAsync("alice", "12345678");

        log.Entries.Should().NotContain(e => e.Message.Contains(GoodClientKey));
    }

    [Fact]
    public async Task ProviderFailures_AreLoggedAtError_WithoutTheCodeAsync()
    {
        var log = new TestLog();

        var act = () => Verifier(log, ("MultifactorAuthenticationEndpoint", "http://127.0.0.1:1/api/mfa"))
            .VerifyAsync("alice", "87654321");
        await act.Should().ThrowAsync<HttpRequestException>();

        log.Entries.Should().Contain(e => e.Level == "ERROR");
        log.Entries.Where(e => e.Level == "ERROR").Should().NotContain(e => e.Message.Contains("87654321"));
    }

    [Fact]
    public async Task ARejectedProviderResponse_LeavesNoCodeOrResponseBodyInAnyLogAsync()
    {
        var log = new TestLog();

        var act = () => Verifier(log, ("MultifactorAuthenticationClientKey", "wrong-key"))
            .VerifyAsync("alice", "87654321");
        await act.Should().ThrowAsync<HttpRequestException>();

        log.Entries.Should().NotBeEmpty();
        log.Entries.Should().NotContain(e => (e.Message + e.Exception).Contains("87654321")
                                             || (e.Message + e.Exception).Contains("Invalid client-key")
                                             || (e.Message + e.Exception).Contains("wrong-key"));
    }

    [Fact]
    public async Task Weakness_EachVerification_BuildsANewProviderAndHttpClientAsync()
    {
        var verifier = Verifier(new TestLog());

        for (var i = 0; i < 3; i++)
        {
            (await verifier.VerifyAsync("alice", "12345678")).Should().BeTrue();
        }

        bodies.Should().HaveCount(3);
    }
}