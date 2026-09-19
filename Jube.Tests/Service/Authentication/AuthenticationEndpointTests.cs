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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Authentication;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Validations.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationEndpointTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private static readonly JsonSerializerSettings legacyNewtonsoft = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
    };

    private readonly List<AuthApiHost> hosts = [];

    public override async Task DisposeAsync()
    {
        foreach (var host in hosts)
        {
            await host.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    private async Task<AuthApiHost> HostAsync(TimeProvider? clock = null, IMfaVerifier? mfa = null,
        bool negotiateScheme = true, params (string Key, string Value)[] env)
    {
        var host = await AuthApiHost.StartAsync(Log, clock ?? TimeProvider.System, Hash, mfa, negotiateScheme, env);
        hosts.Add(host);
        return host;
    }

    private Task<ApiResponse> LoginAsync(AuthApiHost host, string user, string? password, string? mfa = null,
        string? newPassword = null, params (string, string)[] headers)
    {
        return host.PostAsync("/api/Authentication/ByUserNamePassword",
            new { userName = user, password, mfa, newPassword }, [.. headers, ("User-Agent", Marker)]);
    }

    private static string Token(ApiResponse response)
    {
        return JsonDocument.Parse(response.Body).RootElement.GetProperty("token").GetString().Required();
    }

    private static IEnumerable<string> HeaderKeys(ApiResponse response)
    {
        return response.Headers.Keys.Where(k => !k.Equals("Date", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExactlyTheFiveLegacyRoutesAndLogout_AreMapped_WithTheirVerbsAsync()
    {
        var host = await HostAsync();

        var table = host.Endpoints.Select(e => (e.RoutePattern.RawText,
            Verbs: String.Join(",", e.Metadata.GetMetadata<HttpMethodMetadata>().Required().HttpMethods))).ToList();

        table.Should().BeEquivalentTo([
            ("/api/Authentication/WirePasswordHash", "POST"), ("/api/Authentication/ByNegotiate", "GET"),
            ("/api/Authentication/ByNegotiateMfa", "POST"), ("/api/Authentication/ByUserNamePassword", "POST"),
            ("/api/Authentication/ChangePassword", "POST"), ("/api/Authentication/Logout", "POST")
        ]);
    }

    [Theory]
    [InlineData("/api/Authentication/WirePasswordHash", true)]
    [InlineData("/api/Authentication/ByUserNamePassword", true)]
    [InlineData("/api/Authentication/Logout", true)]
    [InlineData("/api/Authentication/ByNegotiate", false)]
    [InlineData("/api/Authentication/ByNegotiateMfa", false)]
    [InlineData("/api/Authentication/ChangePassword", false)]
    public async Task AnonymousEndpointsStayAnonymous_AndTheOthersStayProtectedAsync(string route, bool anonymous)
    {
        var host = await HostAsync();

        var endpoint = host.Endpoints.Single(e => e.RoutePattern.RawText == route);

        (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null).Should().Be(anonymous);
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any().Should().Be(!anonymous);
    }

    [Theory]
    [InlineData("/api/Authentication/ByNegotiate")]
    [InlineData("/api/Authentication/ByNegotiateMfa")]
    public async Task TheNegotiateRoutes_AreBoundToTheNegotiateSchemeOnlyAsync(string route)
    {
        var host = await HostAsync();

        var endpoint = host.Endpoints.Single(e => e.RoutePattern.RawText == route);

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should()
            .OnlyContain(a => a.AuthenticationSchemes == "Negotiate");
    }

    [Fact]
    public async Task ChangePassword_UsesTheDefaultSchemeAuthorisationAsync()
    {
        var host = await HostAsync();

        host.Endpoints.Single(e => e.RoutePattern.RawText == "/api/Authentication/ChangePassword")
            .Metadata.GetOrderedMetadata<IAuthorizeData>().Should().OnlyContain(a => a.AuthenticationSchemes == null);
    }

    [Fact]
    public void NothingInTheAuthenticationAreaIsExposedToTheAgentToolCatalogue()
    {
        ServiceToolCatalogue.All.Select(t => t.Name).Should().NotContain(n => n.Contains("Authentication")
                                                                              || n.Contains("Login") &&
                                                                              !n.StartsWith("UserLogin") ||
                                                                              n.Contains("Password") ||
                                                                              n.Contains("Mfa"));
        typeof(ServiceToolCatalogue).GetMethod("AddAuthentication", BindingFlags.NonPublic | BindingFlags.Static)
            .Should().BeNull();
        typeof(AuthenticationLoginService).GetMethods().Should()
            .OnlyContain(m => m.GetCustomAttribute<ServiceOperationAttribute>() == null);
    }

    [Theory]
    [InlineData("GET", "/api/Authentication/ByUserNamePassword")]
    [InlineData("PUT", "/api/Authentication/ByUserNamePassword")]
    [InlineData("DELETE", "/api/Authentication/ByUserNamePassword")]
    [InlineData("GET", "/api/Authentication/WirePasswordHash")]
    [InlineData("GET", "/api/Authentication/ByNegotiateMfa")]
    [InlineData("POST", "/api/Authentication/ByNegotiate")]
    [InlineData("GET", "/api/Authentication/ChangePassword")]
    [InlineData("PATCH", "/api/Authentication/ChangePassword")]
    public async Task WrongVerbs_Are405_AndReachNoLogicAsync(string method, string route)
    {
        var host = await HostAsync();

        var response = await host.SendAsync(method, route, method == "GET" ? null : "{}");

        response.Status.Should().Be(405);
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("/api/Authentication")]
    [InlineData("/api/Authentication/Nope")]
    [InlineData("/api/Authentication/ByUserNamePassword/extra")]
    [InlineData("/api/authentication/../Authentication/x")]
    public async Task UnknownRoutes_Are404Async(string route)
    {
        var host = await HostAsync();

        (await host.SendAsync("POST", route, "{}")).Status.Should().Be(404);
    }

    [Fact]
    public async Task RoutingIsCaseInsensitive_AsTheLegacyControllerRoutesWereAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("Case");

        var response = await host.PostAsync("/API/authentication/BYUSERNAMEPASSWORD",
            new { userName = user.Name, password = user.Password }, ("User-Agent", Marker));

        response.Status.Should().Be(200);
    }

    [Fact]
    public async Task ACorrectLogin_Is200_WithATokenBodyAndExactlyTheTwoCookiesAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("E2eOk");

        var response = await LoginAsync(host, user.Name, user.Password);

        response.Status.Should().Be(200);
        using var body = JsonDocument.Parse(response.Body);
        body.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("token", "expiration");
        response.Headers["Content-Type"].Should().StartWith("application/json");
        response.SetCookies.Select(c => c.Split('=')[0]).Should().Equal("authentication-jwt", "authentication-expiry");
        response.SetCookies.Should().OnlyContain(c => c.Contains("path=/", StringComparison.OrdinalIgnoreCase)
                                                      && c.Contains("httponly", StringComparison.OrdinalIgnoreCase) ==
                                                      c.StartsWith("authentication-jwt="));
        response.Body.Should().NotContain("password").And.NotContain(user.StoredHash);
    }

    [Fact]
    public async Task TheIssuedToken_AuthenticatesLaterRequests_ViaBearerAndViaCookieAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("E2eToken");
        var token = Token(await LoginAsync(host, user.Name, user.Password));

        var body = new { password = user.Password, newPassword = "weak" };
        var viaBearer = await host.PostAsync("/api/Authentication/ChangePassword", body,
            ("Authorization", $"Bearer {token}"));
        var viaCookie = await host.PostAsync("/api/Authentication/ChangePassword", body,
            ("Cookie", $"authentication-jwt={token}"));

        viaBearer.Status.Should().Be(400);
        viaCookie.Status.Should().Be(400);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("tampered")]
    [InlineData("otherkey")]
    [InlineData("expired")]
    [InlineData("emptyname")]
    public async Task ARejectedToken_CannotReachChangePasswordAsync(string kind)
    {
        var host = await HostAsync();
        var user = await AddUserAsync("E2eBadToken");
        var env = TestDynamicEnvironmentFor("an-adequately-long-unit-test-signing-key-0123456789");
        var good = Token(await LoginAsync(host, user.Name, user.Password));
        var parts = good.Split('.');
        var token = kind switch
        {
            "none" => "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0." + parts[1] + ".",
            "tampered" => $"{parts[0]}.{parts[1]}.{(parts[2][0] == 'A' ? 'B' : 'A')}{parts[2][1..]}",
            "otherkey" => IssueFor(user.Name,
                TestDynamicEnvironmentFor("a-completely-different-signing-key-9876543210")),
            "expired" => IssueFor(user.Name, env, new FakeClock(DateTimeOffset.UtcNow.AddHours(-1))),
            _ => IssueFor("", env)
        };

        var response = await host.PostAsync("/api/Authentication/ChangePassword",
            new { password = user.Password, newPassword = "weak" }, ("Authorization", $"Bearer {token}"));

        response.Status.Should().Be(401);
        response.Body.Should().BeEmpty();
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("a.b")]
    [InlineData("a.b.c.d")]
    [InlineData("eyJ.eyJ.eyJ")]
    [InlineData("....")]
    [InlineData("%%%.%%%.%%%")]
    [InlineData("Bearer")]
    public async Task AMalformedBearerToken_Is401OnProtectedRoutes_AndAnonymousRoutesProceedAsAnonymousAsync(
        string malformed)
    {
        var host = await HostAsync();
        var user = await AddUserAsync("MalformedBearer");
        var bearer = ("Authorization", $"Bearer {malformed}");

        var change = await host.PostAsync("/api/Authentication/ChangePassword", new { password = "a" }, bearer);
        var wire = await host.PostAsync("/api/Authentication/WirePasswordHash", new { userName = user.Name }, bearer);
        var login = await LoginAsync(host, user.Name, user.Password, headers: bearer);

        change.Status.Should().Be(401);
        change.Body.Should().BeEmpty();
        wire.Status.Should().Be(200);
        login.Status.Should().Be(200);
        var claims = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(Token(login)).Claims;
        claims.Should().Contain(c => c.Value == user.Name, "the bogus token gave no identity");
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("a.b")]
    [InlineData("....")]
    public async Task AMalformedCookieToken_IsTreatedTheSameAsync(string malformed)
    {
        var host = await HostAsync();
        var cookie = ("Cookie", $"authentication-jwt={malformed}");

        (await host.PostAsync("/api/Authentication/ChangePassword", new { password = "a" }, cookie)).Status
            .Should().Be(401);
        (await host.PostAsync("/api/Authentication/WirePasswordHash", new { userName = "x" }, cookie)).Status
            .Should().Be(200);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("tampered")]
    [InlineData("truncated")]
    [InlineData("expired")]
    [InlineData("otherkey")]
    public async Task AnUnacceptableToken_NeverBreaksAnAnonymousRoute_AndLeaksNoExceptionDetailAsync(string kind)
    {
        var host = await HostAsync();
        var user = await AddUserAsync("AnonProceeds");
        var good = Token(await LoginAsync(host, user.Name, user.Password));
        var parts = good.Split('.');
        var token = kind switch
        {
            "none" => "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0." + parts[1] + ".",
            "tampered" => $"{parts[0]}.{parts[1]}.{(parts[2][0] == 'A' ? 'B' : 'A')}{parts[2][1..]}",
            "truncated" => good[..^12],
            "expired" => IssueFor(user.Name,
                TestDynamicEnvironmentFor("an-adequately-long-unit-test-signing-key-0123456789"),
                new FakeClock(DateTimeOffset.UtcNow.AddHours(-1))),
            _ => IssueFor(user.Name, TestDynamicEnvironmentFor("a-completely-different-signing-key-9876543210"))
        };

        var anonymous = await host.PostAsync("/api/Authentication/WirePasswordHash", new { userName = user.Name },
            ("Authorization", $"Bearer {token}"));
        var protectedRoute = await host.PostAsync("/api/Authentication/ChangePassword", new { password = "a" },
            ("Authorization", $"Bearer {token}"));

        anonymous.Status.Should().Be(200);
        protectedRoute.Status.Should().Be(401);
        protectedRoute.Body.Should().BeEmpty();
        anonymous.Body.Should().NotContain("Exception").And.NotContain("IDX");
    }

    private static Jube.DynamicEnvironment.DynamicEnvironment TestDynamicEnvironmentFor(string key)
    {
        return TestDynamicEnvironment.Create(new Dictionary<string, string> { ["JWTKey"] = key });
    }

    private static string IssueFor(string user, Jube.DynamicEnvironment.DynamicEnvironment env,
        TimeProvider? clock = null)
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return Jube.App.Code.AuthenticationCookieIssuer.IssueAuthenticationCookies(context.Response, env, user, clock)
            .Token.Required();
    }

    [Fact]
    public async Task NoCredentialsAtAll_On_ChangePassword_Is401WithAnEmptyBodyAsync()
    {
        var host = await HostAsync();

        var response = await host.PostAsync("/api/Authentication/ChangePassword",
            new { password = "a", newPassword = "b" });

        response.Status.Should().Be(401);
        response.Body.Should().BeEmpty();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("wrong")]
    [InlineData("inactive")]
    [InlineData("locked")]
    [InlineData("deleted")]
    public async Task EveryFailedLoginReason_LooksIdenticalOnTheWireAsync(string reason)
    {
        var host = await HostAsync();
        var reference = await AddUserAsync("WireRef");
        var referenceResponse = await LoginAsync(host, $"{Prefix}Ghost", reference.Password);
        var user = reason switch
        {
            "inactive" => await AddUserAsync("WireVariant", u => u.Active = 0),
            "locked" => await AddUserAsync("WireVariant", u => u.PasswordLocked = 1),
            "deleted" => await AddUserAsync("WireVariant", u => u.Deleted = 1),
            _ => await AddUserAsync("WireVariant")
        };

        var response = reason == "unknown"
            ? await LoginAsync(host, $"{Prefix}Other", reference.Password)
            : await LoginAsync(host, user.Name, reason == "wrong" ? "wrong-password" : user.Password);

        response.Status.Should().Be(401);
        response.Body.Should().Be(referenceResponse.Body).And.BeEmpty();
        response.SetCookies.Should().BeEmpty();
        HeaderKeys(response).Should().Equal(HeaderKeys(referenceResponse));
    }

    [Fact]
    public async Task Weakness_TheWireDiffersForAUserWithoutAPasswordExpiry_403VersusTheUsual401Async()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("WireOracle", u => u.PasswordExpiryDate = null);

        var response = await LoginAsync(host, user.Name, "wrong-password");

        response.Status.Should().Be(403);
        response.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task AnExpiredPassword_IsA403_WithoutCookiesAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("WireExpired", u => u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-3));

        var response = await LoginAsync(host, user.Name, user.Password);

        response.Status.Should().Be(403);
        response.SetCookies.Should().BeEmpty();
    }

    [Fact]
    public async Task ANewStrongPassword_WithTheOldOne_Is200_AndTheNewOneWorksAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("WireChange", u => u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-3));

        var change = await LoginAsync(host, user.Name, user.Password, newPassword: "Brand#NewPassword12");
        var relogin = await LoginAsync(host, user.Name, "Brand#NewPassword12");

        change.Status.Should().Be(200);
        relogin.Status.Should().Be(200);
    }

    [Fact]
    public async Task AWeakNewPassword_Is400_WithTheErrorsArrayTheUiReadsAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("WireWeak");

        var response = await LoginAsync(host, user.Name, user.Password, newPassword: "weak");

        response.Status.Should().Be(400);
        var errors = JObject.Parse(response.Body)["errors"].Required().ToObject<List<Dictionary<string, string>>>()
            .Required();
        errors.Should().OnlyContain(e => e.Keys.Single() == "errorMessage");
        errors.Select(e => e["errorMessage"]).Should().Contain("At least 12 characters");
        response.SetCookies.Should().BeEmpty();
    }

    [Theory]
    [InlineData("blank-user")]
    [InlineData("blank-password")]
    [InlineData("missing-both")]
    public async Task ValidationFailures_Are400_WithABodyMatchingTheLegacyNewtonsoftShapeAsync(string kind)
    {
        var host = await HostAsync();
        var dto = kind switch
        {
            "blank-user" => new AuthenticationRequestDto { UserName = " ", Password = "x" },
            "blank-password" => new AuthenticationRequestDto { UserName = "x", Password = "" },
            _ => new AuthenticationRequestDto()
        };

        var response = await host.SendAsync("POST", "/api/Authentication/ByUserNamePassword",
            System.Text.Json.JsonSerializer.Serialize(new { userName = dto.UserName, password = dto.Password }));

        var legacy = JObject.Parse(JsonConvert.SerializeObject(
            await new AuthenticationRequestDtoValidator(
                TestDynamicEnvironment.Create()).ValidateAsync(dto), legacyNewtonsoft));
        response.Status.Should().Be(400);
        var actual = JObject.Parse(response.Body);
        actual["isValid"].Required().Value<bool>().Should().BeFalse();
        actual["errors"].Required().Select(e => e["errorMessage"].Required().ToString()).Should()
            .BeEquivalentTo(legacy["errors"].Required().Select(e => e["errorMessage"].Required().ToString()));
        actual["errors"].Required().Select(e => e["propertyName"].Required().ToString()).Should()
            .BeEquivalentTo(legacy["errors"].Required().Select(e => e["propertyName"].Required().ToString()));
        actual.Properties().Select(p => p.Name).Should().BeEquivalentTo(legacy.Properties().Select(p => p.Name));
    }

    [Fact]
    public async Task TheSuccessBody_MatchesTheLegacyNewtonsoftShapeAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("Shape");

        var response = await LoginAsync(host, user.Name, user.Password);

        var actual = JObject.Parse(response.Body);
        var legacy = JObject.Parse(JsonConvert.SerializeObject(
            new AuthenticationResponseDto { Token = "t", Expiration = DateTime.UtcNow }, legacyNewtonsoft));
        actual.Properties().Select(p => p.Name).Should().BeEquivalentTo(legacy.Properties().Select(p => p.Name));
        actual["expiration"].Required().Value<DateTime>().ToUniversalTime()
            .Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData("oauth")]
    [InlineData("negotiate")]
    public async Task WhenAnotherSchemeIsActive_LoginAndTheWireProbe_Are404Async(string scheme)
    {
        var host = await HostAsync(env: [scheme == "oauth" ? OAuthOn : NegotiateOn]);
        var user = await AddUserAsync("E2eHidden");

        (await LoginAsync(host, user.Name, user.Password)).Status.Should().Be(404);
        (await host.PostAsync("/api/Authentication/WirePasswordHash", new { userName = user.Name })).Status
            .Should().Be(404);
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AnAlreadyAuthenticatedCaller_GetsATokenForTheCredentialsSubmitted_NotForTheCallerAsync()
    {
        var host = await HostAsync();
        var alice = await AddUserAsync("Alice");
        var bob = await AddUserAsync("Bob");
        var aliceToken = Token(await LoginAsync(host, alice.Name, alice.Password));

        var response = await LoginAsync(host, bob.Name, bob.Password,
            headers: ("Authorization", $"Bearer {aliceToken}"));

        response.Status.Should().Be(200);
        var claims = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(Token(response)).Claims;
        claims.Should().Contain(c => c.Value == bob.Name).And.NotContain(c => c.Value == alice.Name);
    }

    [Fact]
    public async Task ParallelLoginsOverHttp_AllGetTheirAnswers_AndWrongOnesNeverSucceedAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("HttpParallel");

        var responses = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(i => Task.Run(() => LoginAsync(host, user.Name, i % 2 == 0 ? user.Password : "wrong"))));

        responses.Count(r => r.Status == 200).Should().BeInRange(0, 6);
        responses.Where((_, i) => i % 2 == 1).Should().OnlyContain(r => r.Status == 401);
        responses.Count(r => r.Status is 200 or 401).Should().Be(12);
        responses.Where(r => r.Status == 401).Should().OnlyContain(r => r.SetCookies.Count == 0);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task WirePasswordHash_Is200_WithTheFlagInAJsonBodyAsync(int flag, bool expected)
    {
        var host = await HostAsync();
        var user = await AddUserAsync("WireProbe", u => u.WirePasswordHash = (byte)flag);

        var response = await host.PostAsync("/api/Authentication/WirePasswordHash", new { userName = user.Name });

        response.Status.Should().Be(200);
        JObject.Parse(response.Body).Properties().Select(p => p.Name).Should().Equal("wirePasswordHash");
        JObject.Parse(response.Body)["wirePasswordHash"].Required().Value<bool>().Should().Be(expected);
    }

    [Theory]
    [InlineData("{}", false)]
    [InlineData("{\"userName\":null}", false)]
    [InlineData("{\"userName\":\"no-such-user\"}", true)]
    public async Task WirePasswordHash_UnknownAndMissingNamesAsync(string body, bool expected)
    {
        var host = await HostAsync();

        var response = await host.SendAsync("POST", "/api/Authentication/WirePasswordHash", body);

        response.Status.Should().Be(200);
        JObject.Parse(response.Body)["wirePasswordHash"].Required().Value<bool>().Should().Be(expected);
    }

    [Fact]
    public async Task ChangePassword_WithAValidToken_Is200WithAnEmptyBody_AndChangesTheHashAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("CpE2e");
        var token = Token(await LoginAsync(host, user.Name, user.Password));

        var response = await host.PostAsync("/api/Authentication/ChangePassword",
            new { password = user.Password, newPassword = "Brand#NewPassword12" },
            ("Authorization", $"Bearer {token}"));

        response.Status.Should().Be(200);
        response.Body.Should().BeEmpty();
        (await ReloadAsync(user)).Password.Should().Be(FastHashScheme.Stored("Brand#NewPassword12", HashKey));
    }

    [Fact]
    public async Task ChangePassword_WithTheWrongCurrentPassword_Is401Async()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("CpWrongE2e");
        var token = Token(await LoginAsync(host, user.Name, user.Password));

        var response = await host.PostAsync("/api/Authentication/ChangePassword",
            new { password = "wrong", newPassword = "Brand#NewPassword12" }, ("Authorization", $"Bearer {token}"));

        response.Status.Should().Be(401);
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Fact]
    public async Task ChangePassword_WeakNew_Is400_WithTheErrorsArrayAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("CpWeakE2e");
        var token = Token(await LoginAsync(host, user.Name, user.Password));

        var response = await host.PostAsync("/api/Authentication/ChangePassword",
            new { password = user.Password, newPassword = "short" }, ("Authorization", $"Bearer {token}"));

        response.Status.Should().Be(400);
        JObject.Parse(response.Body)["errors"].Required()[0].Required()["errorMessage"].Required().Value<string>().Should()
            .NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePassword_IsForbidden_WhenNegotiateOwnsIdentityAsync()
    {
        var host = await HostAsync(env: NegotiateOn);
        var user = await AddUserAsync("CpNeg");
        var token = IssueFor(user.Name,
            TestDynamicEnvironmentFor("an-adequately-long-unit-test-signing-key-0123456789"));

        var response = await host.PostAsync("/api/Authentication/ChangePassword",
            new { password = user.Password, newPassword = "Brand#NewPassword12" },
            ("Authorization", $"Bearer {token}"));

        response.Status.Should().Be(403);
    }

    [Fact]
    public async Task ChangePassword_ForAValidTokenWhoseUserNoLongerExists_Is401WithNoDetailAsync()
    {
        var host = await HostAsync();
        var token = IssueFor($"{Prefix}Vanished",
            TestDynamicEnvironmentFor("an-adequately-long-unit-test-signing-key-0123456789"));

        var response = await host.PostAsync("/api/Authentication/ChangePassword",
            new { password = "a", newPassword = "Brand#NewPassword12" }, ("Authorization", $"Bearer {token}"));

        response.Status.Should().Be(401);
        response.Body.Should().BeEmpty();
        response.SetCookies.Should().BeEmpty();
    }

    [Fact]
    public async Task TheLoginHistoryRemoteIp_IsTheConnectionAddress_NeverTheBodyOrAForwardedHeaderAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("HttpIp");

        await host.SendAsync("POST", "/api/Authentication/ByUserNamePassword",
            System.Text.Json.JsonSerializer.Serialize(new
                { userName = user.Name, password = user.Password, remoteIp = "203.0.113.9" }),
            "application/json", ("User-Agent", Marker), ("X-Forwarded-For", "203.0.113.77"),
            ("X-Real-IP", "203.0.113.78"));
        await host.SendAsync("POST", "/api/Authentication/ByUserNamePassword",
            System.Text.Json.JsonSerializer.Serialize(new
                { userName = user.Name, password = "wrong", remoteIp = "203.0.113.9" }),
            "application/json", ("User-Agent", Marker), ("X-Forwarded-For", "203.0.113.77"));

        var history = await HistoryAsync();
        history.Should().HaveCount(2);
        history.Should().OnlyContain(h => h.RemoteIp == "127.0.0.1" || h.RemoteIp == "::1");
        AllLogText().Should().NotContain("203.0.113");
    }

    private static readonly string[] bodyRoutes =
    [
        "/api/Authentication/ByUserNamePassword", "/api/Authentication/WirePasswordHash",
        "/api/Authentication/ChangePassword", "/api/Authentication/ByNegotiateMfa"
    ];

    public static TheoryData<string, string> FrameworkRejectedBodies()
    {
        var generic = new[]
        {
            "{", "not json", "[]", "\"text\"", "12", "true", "{\"userName\":\"a\",}", "\u0000", "  ",
            "{\"userName\":\"unterminated"
        };
        var data = new TheoryData<string, string>();
        foreach (var route in bodyRoutes)
        {
            foreach (var body in generic)
            {
                data.Add(route, body);
            }

            var typed = route.EndsWith("ChangePassword")
                ? ["{\"password\":5}", "{\"newPassword\":[1]}", "{\"password\":{\"a\":1}}"]
                : route.EndsWith("WirePasswordHash")
                    ? ["{\"userName\":5}", "{\"userName\":[\"x\"]}", "{\"userName\":{\"a\":1}}"]
                    : new[] { "{\"userName\":5}", "{\"userName\":[\"x\"]}", "{\"password\":{\"a\":1}}" };
            foreach (var body in typed)
            {
                data.Add(route, body);
            }
        }

        return data;
    }

    public static TheoryData<string, string> NullBodies()
    {
        var data = new TheoryData<string, string>();
        foreach (var route in bodyRoutes)
        {
            foreach (var body in new[] { "", "null" })
            {
                data.Add(route, body);
            }
        }

        return data;
    }

    private async Task<(AuthApiHost Host, TestUser User, string Token)> BindingSetupAsync(string route)
    {
        var host = await HostAsync(env: route.EndsWith("ByNegotiateMfa") ? [NegotiateOn] : []);
        var user = await AddUserAsync("Binding");
        var token = IssueFor(user.Name,
            TestDynamicEnvironmentFor("an-adequately-long-unit-test-signing-key-0123456789"));
        return (host, user, token);
    }

    [Theory]
    [MemberData(nameof(FrameworkRejectedBodies))]
    public async Task MalformedJsonOrWrongShapes_Are400_AndTouchNothingAsync(string route, string body)
    {
        var (host, user, token) = await BindingSetupAsync(route);

        var response = await host.SendAsync("POST", route, body, "application/json",
            ("Authorization", $"Bearer {token}"), (TestNegotiateHandler.Header, user.Name), ("User-Agent", Marker));

        response.Status.Should().Be(400);
        response.SetCookies.Should().BeEmpty();
        (await HistoryAsync()).Should().BeEmpty();
        var reloaded = await ReloadAsync(user);
        reloaded.Password.Should().Be(user.StoredHash);
        reloaded.FailedPasswordCount.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(NullBodies))]
    public async Task AnEmptyOrNullBody_Is400_WithNoDetail_AndTouchesNothingAsync(string route, string body)
    {
        var (host, user, token) = await BindingSetupAsync(route);

        var response = await host.SendAsync("POST", route, body, "application/json",
            ("Authorization", $"Bearer {token}"), (TestNegotiateHandler.Header, user.Name), ("User-Agent", Marker));

        response.Status.Should().Be(400);
        response.SetCookies.Should().BeEmpty();
        response.Body.Should().BeEmpty();
        (await HistoryAsync()).Should().BeEmpty();
        (await ReloadAsync(user)).Password.Should().Be(user.StoredHash);
    }

    [Fact]
    public async Task ChangePassword_WithNoRecognisedProperties_Is400_NotA500Async()
    {
        var (host, _, token) = await BindingSetupAsync("/api/Authentication/ChangePassword");

        var response = await host.SendAsync("POST", "/api/Authentication/ChangePassword", "{\"unrelated\":1}",
            "application/json", ("Authorization", $"Bearer {token}"));

        response.Status.Should().Be(400);
        response.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task WirePasswordHash_IgnoresUnrecognisedProperties_AndAnswersFalseForNoUserNameAsync()
    {
        var host = await HostAsync();

        var response = await host.SendAsync("POST", "/api/Authentication/WirePasswordHash",
            "{\"password\":{\"a\":1}}");

        response.Status.Should().Be(200);
        JObject.Parse(response.Body)["wirePasswordHash"].Required().Value<bool>().Should().BeFalse();
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("application/x-www-form-urlencoded")]
    [InlineData("multipart/form-data")]
    public async Task ANonJsonContentType_Is415_AndTouchesNothingAsync(string contentType)
    {
        var host = await HostAsync();
        var user = await AddUserAsync("ContentType");

        var response = await host.SendAsync("POST", "/api/Authentication/ByUserNamePassword",
            $"{{\"userName\":\"{user.Name}\",\"password\":\"{user.Password}\"}}", contentType);

        response.Status.Should().Be(415);
        response.SetCookies.Should().BeEmpty();
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task APostWithNoBodyAtAll_Is400_WithoutTouchingTheDatabaseAsync()
    {
        var host = await HostAsync();

        var response = await host.SendAsync("POST", "/api/Authentication/ByUserNamePassword");

        response.Status.Should().Be(400);
        response.Body.Should().BeEmpty();
        (await HistoryAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task UnknownAndExtraJsonProperties_AreIgnored_AndCaseInsensitiveNamesBindAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("Extras");

        var response = await host.SendAsync("POST", "/api/Authentication/ByUserNamePassword",
            System.Text.Json.JsonSerializer.Serialize(new
            {
                USERNAME = user.Name, PassWord = user.Password, isAdmin = true, role = "root"
            }), "application/json", ("User-Agent", Marker));

        response.Status.Should().Be(200);
    }

    [Fact]
    public async Task AHugeBody_OfTwoMegabytes_IsRejectedOrFailsClosed_NeverAuthenticatingAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("HugeBody");

        var response = await LoginAsync(host, user.Name, new string('p', 2 * 1024 * 1024));

        response.Status.Should().BeOneOf(400, 401, 413);
        response.SetCookies.Should().BeEmpty();
    }

    [Fact]
    public async Task ByNegotiate_WithAnEstablishedIdentity_Is200_WithCookiesAsync()
    {
        var host = await HostAsync(env: NegotiateOn);
        var user = await AddUserAsync("HttpNeg");

        var response = await host.SendAsync("GET", "/api/Authentication/ByNegotiate", null, "application/json",
            (TestNegotiateHandler.Header, user.Name), ("User-Agent", Marker));

        response.Status.Should().Be(200);
        response.SetCookies.Should().HaveCount(2);
        JObject.Parse(response.Body).Properties().Select(p => p.Name).Should().BeEquivalentTo("token", "expiration");
    }

    [Theory]
    [InlineData("GET", "/api/Authentication/ByNegotiate")]
    [InlineData("POST", "/api/Authentication/ByNegotiateMfa")]
    public async Task ByNegotiate_WithoutAnIdentity_ChallengesWith401AndWwwAuthenticateNegotiateAsync(string method,
        string route)
    {
        var host = await HostAsync(env: NegotiateOn);

        var response = await host.SendAsync(method, route, method == "POST" ? "{}" : null);

        response.Status.Should().Be(401);
        response.Headers["WWW-Authenticate"].Should().Be("Negotiate");
        response.SetCookies.Should().BeEmpty();
    }

    [Fact]
    public async Task ByNegotiate_AnUnknownIdentity_Is403Async()
    {
        var host = await HostAsync(env: NegotiateOn);

        var response = await host.SendAsync("GET", "/api/Authentication/ByNegotiate", null, "application/json",
            (TestNegotiateHandler.Header, $"{Prefix}CORP\\ghost"), ("User-Agent", Marker));

        response.Status.Should().Be(403);
        (await HistoryAsync()).Should().ContainSingle().Which.FailureTypeId.Should().Be(1);
    }

    [Fact]
    public async Task ByNegotiate_WhenNegotiateIsNotEnabled_Is404_EvenWithAnIdentityHeaderAsync()
    {
        var host = await HostAsync();
        var user = await AddUserAsync("NegOff");

        var response = await host.SendAsync("GET", "/api/Authentication/ByNegotiate", null, "application/json",
            (TestNegotiateHandler.Header, user.Name));

        response.Status.Should().Be(404);
    }

    [Fact]
    public async Task ByNegotiate_TheHybridJwtIsNotAcceptedInPlaceOfANegotiateIdentityAsync()
    {
        var host = await HostAsync(env: NegotiateOn);
        var user = await AddUserAsync("NegJwt");
        var token = IssueFor(user.Name,
            TestDynamicEnvironmentFor("an-adequately-long-unit-test-signing-key-0123456789"));

        var response = await host.SendAsync("GET", "/api/Authentication/ByNegotiate", null, "application/json",
            ("Authorization", $"Bearer {token}"));

        response.Status.Should().Be(401);
    }

    [Fact]
    public async Task ByNegotiate_WithMfaEnabled_Is202WithAnEmptyBody_AndThePostFlowCompletesAsync()
    {
        var user = await AddUserAsync("HttpNegMfa");
        Mfa.Allow(user.Name, "424242");
        var host = await HostAsync(mfa: Mfa, env: [NegotiateOn, MfaOn]);
        var identity = (TestNegotiateHandler.Header, user.Name);

        var first = await host.SendAsync("GET", "/api/Authentication/ByNegotiate", null, "application/json", identity);
        var none = await host.PostAsync("/api/Authentication/ByNegotiateMfa", new { }, identity);
        var wrong = await host.PostAsync("/api/Authentication/ByNegotiateMfa", new { mfa = "000000" }, identity);
        var right = await host.PostAsync("/api/Authentication/ByNegotiateMfa", new { mfa = "424242" }, identity);

        first.Status.Should().Be(202);
        first.Body.Should().BeEmpty();
        none.Status.Should().Be(202);
        wrong.Status.Should().Be(401);
        right.Status.Should().Be(200);
        right.SetCookies.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenTheNegotiateSchemeIsNotRegistered_TheRoutesFailWith500NotAnAccidentalOpenAsync()
    {
        var host = await HostAsync(negotiateScheme: false, env: NegotiateOn);

        var response = await host.SendAsync("GET", "/api/Authentication/ByNegotiate", null, "application/json",
            (TestNegotiateHandler.Header, "someone"));

        response.Status.Should().Be(500);
        response.SetCookies.Should().BeEmpty();
    }

    [Fact]
    public async Task UserNamePassword_WithMfa_RightCode200_WrongCode401_MissingCode400Async()
    {
        var user = await AddUserAsync("HttpMfa");
        Mfa.Allow(user.Name, "424242");
        var host = await HostAsync(mfa: Mfa, env: MfaOn);

        var wrong = await LoginAsync(host, user.Name, user.Password, "000000");
        var missing = await LoginAsync(host, user.Name, user.Password);
        var right = await LoginAsync(host, user.Name, user.Password, "424242");

        wrong.Status.Should().Be(401);
        wrong.SetCookies.Should().BeEmpty();
        missing.Status.Should().Be(400);
        JObject.Parse(missing.Body)["errors"].Required().Select(e => e["propertyName"].Required().ToString()).Should()
            .Contain("Mfa");
        right.Status.Should().Be(200);
    }

    [Fact]
    public async Task AProviderOutage_DuringMfa_Is500_AndNeverAnAuthenticationAsync()
    {
        var user = await AddUserAsync("HttpMfaDown");
        Mfa.Throw = new System.Net.Http.HttpRequestException("provider down");
        var host = await HostAsync(mfa: Mfa, env: MfaOn);

        var response = await LoginAsync(host, user.Name, user.Password, "424242");

        response.Status.Should().Be(500);
        response.SetCookies.Should().BeEmpty();
    }
}