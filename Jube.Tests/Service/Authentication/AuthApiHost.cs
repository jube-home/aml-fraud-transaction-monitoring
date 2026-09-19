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
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Jube.App.Endpoints;
using Jube.App.Middlewares;
using Jube.App.Middlewares.Models;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using log4net;

namespace Jube.Test.Service.Authentication;

public sealed class AuthApiHost : IAsyncDisposable
{
    private readonly HttpClient client;
    private readonly WebApplication app;

    private AuthApiHost(WebApplication app)
    {
        this.app = app;
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Required()
            .Addresses.Single();
        client = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false })
            { BaseAddress = new Uri(address) };
    }

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("JubeTestConnectionString")
        ?? Environment.GetEnvironmentVariable("ConnectionString")
        ??
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

    public static async Task<AuthApiHost> StartAsync(TestLog log, TimeProvider clock, IPasswordHashScheme hash,
        IMfaVerifier? mfa = null, bool negotiateScheme = true, params (string Key, string Value)[] env)
    {
        var settings = new Dictionary<string, string>
        {
            ["ConnectionString"] = ConnectionString,
            ["JWTKey"] = "an-adequately-long-unit-test-signing-key-0123456789",
            ["PasswordHashingKey"] = AuthenticationTestBase.HashKey
        };
        foreach (var (key, value) in env)
        {
            settings[key] = value;
        }

        var dynamicEnvironment = TestDynamicEnvironment.Create(settings);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        if (Environment.GetEnvironmentVariable("AUTH_HOST_LOG") == "1")
        {
            builder.Logging.AddConsole();
        }

        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ILog>(log);
        builder.Services.AddSingleton(dynamicEnvironment);
        builder.Services.AddSingleton(clock);
        builder.Services.AddSingleton(hash);
        if (mfa != null)
        {
            builder.Services.AddSingleton(mfa);
        }

        builder.Services.AddSingleton(
            (ApiTokensCache.ApiTokensCache)RuntimeHelpers.GetUninitializedObject(
                typeof(ApiTokensCache.ApiTokensCache)));
        builder.Services.AddAuthentication(o =>
            {
                o.DefaultScheme = "Hybrid";
                o.DefaultAuthenticateScheme = "Hybrid";
                o.DefaultChallengeScheme = "Hybrid";
            })
            .AddScheme<HybridAuthOptions, HybridAuthHandler>("Hybrid", o =>
            {
                o.JwtKey = dynamicEnvironment.AppSettings("JWTKey");
                o.JwtValidIssuer = dynamicEnvironment.AppSettings("JWTValidIssuer");
                o.JwtValidAudience = dynamicEnvironment.AppSettings("JWTValidAudience");
            })
            ;
        if (negotiateScheme)
        {
            builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestNegotiateHandler>(
                NegotiateDefaults.AuthenticationScheme, _ => { });
        }

        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAuthenticationEndpoints();
        await app.StartAsync();
        return new AuthApiHost(app);
    }

    public IReadOnlyList<RouteEndpoint> Endpoints =>
        [.. app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()];

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
    }

    public async Task<ApiResponse> SendAsync(string method, string path, string? body = null,
        string contentType = "application/json", params (string Name, string Value)[] headers)
    {
        using var message = new HttpRequestMessage(new HttpMethod(method), path);
        if (body != null)
        {
            message.Content = new StringContent(body, Encoding.UTF8);
            message.Content.Headers.Remove("Content-Type");
            message.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }

        foreach (var (name, value) in headers)
        {
            message.Headers.TryAddWithoutValidation(name, value);
        }

        using var response = await client.SendAsync(message);
        var text = await response.Content.ReadAsStringAsync();
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToList() : [];
        var all = response.Headers.Concat(response.Content.Headers)
            .ToDictionary(h => h.Key, h => String.Join("|", h.Value), StringComparer.OrdinalIgnoreCase);
        return new ApiResponse((int)response.StatusCode, text, cookies, all);
    }

    public Task<ApiResponse> PostAsync(string path, object? body, params (string, string)[] headers)
    {
        return SendAsync("POST", path, System.Text.Json.JsonSerializer.Serialize(body), "application/json", headers);
    }
}