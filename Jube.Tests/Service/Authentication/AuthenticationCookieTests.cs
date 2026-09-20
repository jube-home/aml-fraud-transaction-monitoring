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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code;
using Jube.App.Middlewares;
using Jube.App.Middlewares.Models;
using Jube.Dto.Authentication;
using Jube.Test.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
public sealed class AuthenticationCookieTests
{
    private const string Key = "an-adequately-long-unit-test-signing-key-0123456789";
    private const string Issuer = "http://localhost:5001";
    private const string Audience = "http://localhost:5001";

    private static readonly DateTimeOffset noon = new(DateTime.UtcNow.Date.AddHours(12), TimeSpan.Zero);

    private static Jube.DynamicEnvironment.DynamicEnvironment Env(params (string Key, string Value)[] overrides)
    {
        var settings = new Dictionary<string, string> { ["JWTKey"] = Key };
        foreach (var (k, v) in overrides)
        {
            settings[k] = v;
        }

        return TestDynamicEnvironment.Create(settings);
    }

    private static (AuthenticationResponseDto Dto, List<SetCookieHeaderValue> Cookies) Issue(string user,
        Jube.DynamicEnvironment.DynamicEnvironment env, TimeProvider? clock = null)
    {
        var context = new DefaultHttpContext();
        var dto = AuthenticationCookieIssuer.IssueAuthenticationCookies(context.Response, env, user, clock);
        return (dto, [.. SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie)]);
    }

    private static async Task<AuthenticateResult> ValidateAsync(string? token, string key = Key,
        string issuer = Issuer, string audience = Audience, bool viaCookie = false)
    {
        var options = new HybridAuthOptions { JwtKey = key, JwtValidIssuer = issuer, JwtValidAudience = audience };
        var handler = new HybridAuthHandler(new StaticOptionsMonitor<HybridAuthOptions>(options),
            NullLoggerFactory.Instance, UrlEncoder.Default, null,
            TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                       ?? Environment.GetEnvironmentVariable("ConnectionString")
                                       ?? "unit-test-ConnectionString"
            }), TestLog.NoOp);
        var context = new DefaultHttpContext();
        if (viaCookie)
        {
            context.Request.Headers.Cookie = $"authentication-jwt={token}";
        }
        else
        {
            context.Request.Headers.Authorization = $"Bearer {token}";
        }

        await handler.InitializeAsync(new AuthenticationScheme("Hybrid", null, typeof(HybridAuthHandler)), context);
        return await handler.AuthenticateAsync();
    }

    private static string B64(string json)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string FromB64(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }

    [Theory]
    [InlineData("True", "False")]
    [InlineData("True", "True")]
    [InlineData("False", "False")]
    [InlineData("False", "True")]
    public void TheCookieFlags_FollowSessionAndSecureSettings(string session, string secure)
    {
        var clock = new FakeClock(noon);
        var (dto, cookies) = Issue("alice", Env(("SessionCookie", session), ("SecureHttpCookie", secure)), clock);

        cookies.Select(c => c.Name.Value).Should().Equal("authentication-jwt", "authentication-expiry");
        foreach (var cookie in cookies)
        {
            cookie.Path.Value.Should().Be("/");
            cookie.HttpOnly.Should().Be(cookie.Name.Value == "authentication-jwt");
            cookie.Secure.Should().Be(secure == "True");
            cookie.SameSite.Should().Be(secure == "True"
                ? Microsoft.Net.Http.Headers.SameSiteMode.Strict
                : Microsoft.Net.Http.Headers.SameSiteMode.Lax);
            if (session == "True")
            {
                cookie.Expires.Should().BeNull();
            }
            else
            {
                cookie.Expires.Required().Should()
                    .BeCloseTo(new DateTimeOffset(dto.Expiration), TimeSpan.FromSeconds(1));
            }

            cookie.MaxAge.Should().BeNull();
            cookie.Domain.HasValue.Should().BeFalse();
        }
    }

    [Fact]
    public void TheDefaultEnvironment_IsASessionCookie_NotSecure_WithOnlyTheJwtHttpOnly()
    {
        var (_, cookies) = Issue("alice", Env());

        cookies.Should().OnlyContain(c => c.Expires == null && !c.Secure
                                                            && c.SameSite == Microsoft.Net.Http.Headers.SameSiteMode
                                                                .Lax);
        cookies.Single(c => c.Name.Value == "authentication-jwt").HttpOnly.Should().BeTrue();
        cookies.Single(c => c.Name.Value == "authentication-expiry").HttpOnly.Should().BeFalse();
    }

    [Fact]
    public void TheCookieValues_AreTheTokenAndTheIso8601Expiry_AndMatchTheResponseBody()
    {
        var clock = new FakeClock(noon);
        var (dto, cookies) = Issue("alice", Env(), clock);

        cookies[0].Value.Value.Should().Be(dto.Token);
        cookies[1].Value.Value.Should().Be(Uri.EscapeDataString(dto.Expiration.ToString("O")));
        dto.Expiration.Should().Be(noon.UtcDateTime.AddMinutes(15));
        dto.Expiration.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void TheJsonBody_HasExactlyTokenAndExpiration()
    {
        var (dto, _) = Issue("alice", Env());

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        JsonDocument.Parse(json).RootElement.EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo("token", "expiration");
    }

    [Fact]
    public async Task TheIssuedToken_IsAcceptedByTheRealHandler_AsTheUser_ViaBearerAndCookieAsync()
    {
        var (dto, _) = Issue("alice", Env());

        foreach (var viaCookie in new[] { false, true })
        {
            var result = await ValidateAsync(dto.Token, viaCookie: viaCookie);

            result.Succeeded.Should().BeTrue();
            result.Principal.Required().FindFirstValue(ClaimTypes.Name).Should().Be("alice");
        }
    }

    [Fact]
    public void TheTokenHeaderAndPayload_CarryOnlyTheNameIssuerAudienceAndExpiry()
    {
        var clock = new FakeClock(noon);
        var (dto, _) = Issue("alice", Env(), clock);
        var parts = dto.Token.Required().Split('.');

        parts.Should().HaveCount(3);
        using var header = JsonDocument.Parse(FromB64(parts[0]));
        using var payload = JsonDocument.Parse(FromB64(parts[1]));
        header.RootElement.GetProperty("alg").GetString().Should().Be("HS256");
        header.RootElement.GetProperty("typ").GetString().Should().Be("JWT");
        payload.RootElement.GetProperty("iss").GetString().Should().Be(Issuer);
        payload.RootElement.GetProperty("aud").GetString().Should().Be(Audience);
        payload.RootElement.GetProperty("exp").GetInt64().Should()
            .Be(noon.AddMinutes(15).ToUnixTimeSeconds());
        payload.RootElement.GetProperty("iat").GetInt64().Should().Be(noon.ToUnixTimeSeconds());
        payload.RootElement.GetProperty("nbf").GetInt64().Should().Be(noon.ToUnixTimeSeconds());
        payload.RootElement.EnumerateObject().Select(p => p.Name).Should().NotContain(
            ["password", "hash", "salt", "email", "role", "roles", "tenant"]);
    }

    [Fact]
    public void Weakness_TheSameSecond_YieldsTheIdenticalToken_ThereIsNoJtiOrIssuedAt()
    {
        var clock = new FakeClock(noon);

        var (a, _) = Issue("alice", Env(), clock);
        var (b, _) = Issue("alice", Env(), clock);

        a.Token.Should().Be(b.Token);
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("Ünïcödé-用户")]
    [InlineData("user with spaces")]
    [InlineData("a\"b'c<d>&e")]
    [InlineData("CORP\\alice")]
    public async Task NamesSurviveTheTokenAndTheCookieRoundTrip_ExactlyAsync(string user)
    {
        var (dto, cookies) = Issue(user, Env());

        var result = await ValidateAsync(cookies[0].Value.Value, viaCookie: true);

        result.Succeeded.Should().BeTrue();
        result.Principal.Required().FindFirstValue(ClaimTypes.Name).Should().Be(user);
        dto.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ATokenIssuedForAnEmptyName_IsRejectedByTheHandlerAsMalformedAsync()
    {
        var (dto, _) = Issue("", Env());

        var result = await ValidateAsync(dto.Token);

        result.Succeeded.Should().BeFalse();
        result.Failure.Required().Message.Should().Contain("Malformed");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("fifteen-bytes!!")]
    public void AJwtKeyBelow128Bits_CannotSign_SoLoginWouldFailAt500(string shortKey)
    {
        var act = () => Issue("alice", Env(("JWTKey", shortKey)));

        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("flip-signature")]
    [InlineData("swap-payload-name")]
    [InlineData("alg-none")]
    [InlineData("empty-signature")]
    [InlineData("truncated")]
    public async Task ATamperedToken_IsRejectedAsync(string tamper)
    {
        var (dto, _) = Issue("alice", Env());
        var parts = dto.Token.Required().Split('.');
        var tampered = tamper switch
        {
            "flip-signature" => $"{parts[0]}.{parts[1]}.{(parts[2][0] == 'A' ? 'B' : 'A')}{parts[2][1..]}",
            "swap-payload-name" => $"{parts[0]}.{B64(FromB64(parts[1]).Replace("alice", "admin"))}.{parts[2]}",
            "alg-none" => $"{B64("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{parts[1]}.",
            "empty-signature" => $"{parts[0]}.{parts[1]}.",
            _ => dto.Token.Required()[..^10]
        };

        var result = await ValidateAsync(tampered);

        result.Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData("extra-segment")]
    [InlineData("garbage")]
    [InlineData("two-segments")]
    [InlineData("payload-only")]
    [InlineData("....")]
    [InlineData("eyJ.eyJ.eyJ")]
    [InlineData("%%%.%%%.%%%")]
    [InlineData("a.b.c.d.e")]
    public async Task AStructurallyMalformedToken_IsSimplyNotAuthenticated_NeverAnExceptionAsync(string kind)
    {
        var (dto, _) = Issue("alice", Env());
        var parts = dto.Token.Required().Split('.');
        var malformed = kind switch
        {
            "extra-segment" => dto.Token + ".AAAA",
            "two-segments" => $"{parts[0]}.{parts[1]}",
            "payload-only" => parts[1],
            "garbage" => "not.a.jwt",
            _ => kind
        };

        var bearer = await ValidateAsync(malformed);
        var cookie = await ValidateAsync(malformed, viaCookie: true);

        foreach (var result in new[] { bearer, cookie })
        {
            result.Succeeded.Should().BeFalse();
            result.Principal.Should().BeNull();
            (result.Failure?.Message ?? "").Should().NotContain("IDX").And.NotContain("Exception");
        }
    }

    [Fact]
    public async Task ATokenSignedWithADifferentKey_IsRejectedAsync()
    {
        var (dto, _) = Issue("alice", Env(("JWTKey", "a-completely-different-signing-key-9876543210")));

        (await ValidateAsync(dto.Token)).Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    public async Task ATokenForAnotherIssuerOrAudience_IsRejectedAsync(string which)
    {
        var (dto, _) = Issue("alice", Env());

        var result = which == "issuer"
            ? await ValidateAsync(dto.Token, issuer: "http://elsewhere")
            : await ValidateAsync(dto.Token, audience: "http://elsewhere");

        result.Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(-16 * 60, false)]
    [InlineData(-60 * 60, false)]
    [InlineData(-15 * 60 - 45, false)]
    [InlineData(-15 * 60 - 20, true)]
    [InlineData(-14 * 60, true)]
    [InlineData(0, true)]
    public async Task ExpiryIsEnforced_With30SecondsOfClockSkewAsync(int issuedOffsetSeconds, bool valid)
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow.AddSeconds(issuedOffsetSeconds));
        var (dto, _) = Issue("alice", Env(), clock);

        var result = await ValidateAsync(dto.Token);

        result.Succeeded.Should().Be(valid);
        if (!valid)
        {
            result.Failure.Required().Message.Should().Contain("expired");
        }
    }

    [Fact]
    public async Task AnExpiredTokenInTheCookie_IsRejectedTooAsync()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow.AddHours(-2));
        var (dto, _) = Issue("alice", Env(), clock);

        (await ValidateAsync(dto.Token, viaCookie: true)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AnEmptyCookie_IsNoCredentialsAtAll_NotAFailureAsync()
    {
        var result = await ValidateAsync("", viaCookie: true);

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task AnAlteredExpiryCookie_DoesNotExtendTheTokensLifeAsync()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow.AddHours(-1));
        var (dto, cookies) = Issue("alice", Env(), clock);
        cookies[1].Value.Value.Should().NotBeNullOrEmpty();

        (await ValidateAsync(dto.Token)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public void TheJwtSecurityTokenHandler_ReadsTheSameClaimsThatWereIssued()
    {
        var (dto, _) = Issue("alice", Env());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.Token);

        jwt.Claims.Should().Contain(c => c.Value == "alice");
        jwt.Issuer.Should().Be(Issuer);
        jwt.Audiences.Should().Equal(Audience);
        jwt.SignatureAlgorithm.Should().Be("HS256");
    }
}