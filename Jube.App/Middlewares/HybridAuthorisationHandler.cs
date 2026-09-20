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

namespace Jube.App.Middlewares
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Net;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using ApiTokensCache;
    using Data.Context;
    using Code;
    using Data.Security;
    using log4net;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using Models;

    public class HybridAuthHandler(
        IOptionsMonitor<HybridAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApiTokensCache apiTokensCache,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log)
        : AuthenticationHandler<HybridAuthOptions>(options, logger, encoder)
    {
        private const string ApiKeyHeader = "X-API-KEY";

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyValue))
            {
                return await ValidateApiKeyAsync(apiKeyValue.ToString());
            }

            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var header = authHeader.ToString();
                if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return await ValidateJwtAsync(header["Bearer ".Length..].Trim());
                }
            }

            if (Request.Cookies.TryGetValue("authentication-jwt", out var cookieToken)
                && !String.IsNullOrEmpty(cookieToken))
            {
                return await ValidateJwtAsync(cookieToken);
            }

            return AuthenticateResult.NoResult();
        }

        private Task<AuthenticateResult> ValidateApiKeyAsync(string apiKey)
        {
            try
            {
                string userName;
                try
                {
                    userName = apiTokensCache.ValidateApiKeyAndReturnAuthenticatedUser(apiKey);
                }
                catch (KeyNotFoundException)
                {
                    return Task.FromResult(Fail("API Key Not Found."));
                }

                return Task.FromResult(Success([
                    new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.AuthenticationMethod, "ApiHmacKey")
                ]));
            }
            catch (Exception ex)
            {
                log.Error($"ValidateApiKeyAsync Authentication service unavailable given exception: {ex}");

                return Task.FromResult(Fail("ValidateApiKeyAsync Authentication service unavailable."));
            }
        }

        private async Task<AuthenticateResult> ValidateJwtAsync(string token)
        {
            ClaimsPrincipal principal;
            SecurityToken validated;
            try
            {
                var o = Options;
                var handler = new JwtSecurityTokenHandler();
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(o.JwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = o.JwtValidIssuer,
                    ValidateAudience = true,
                    ValidAudience = o.JwtValidAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name
                };

                var result = await Task.Run(() =>
                {
                    var validatedPrincipal = handler.ValidateToken(token, validationParams, out var securityToken);
                    return (validatedPrincipal, securityToken);
                });
                principal = result.validatedPrincipal;
                validated = result.securityToken;
            }
            catch (SecurityTokenExpiredException)
            {
                return Fail("Token has expired.");
            }
            catch (Exception)
            {
                return Fail("Invalid token.");
            }

            var username = principal.FindFirstValue(ClaimTypes.Name);

            if (String.IsNullOrEmpty(username))
            {
                return Fail("Malformed token claims.");
            }

            var issuedMilliseconds = principal.FindFirstValue(TokenValidity.IssuedMillisecondsClaim);
            if (AbsoluteSessionLifetime.IsExpired(dynamicEnvironment, issuedMilliseconds, DateTimeOffset.UtcNow))
            {
                return Fail("Session has expired.");
            }

            try
            {
                await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log);
                if (await TokenValidity.IsRevokedAsync(dbContext, username, issuedMilliseconds,
                        (validated as JwtSecurityToken)?.IssuedAt).ConfigureAwait(false))
                {
                    return Fail("Token has been revoked.");
                }
            }
            catch (Exception ex)
            {
                log.Error("ValidateJwtAsync could not check token revocation.", ex);
                return Fail("Token could not be verified.");
            }

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        private static AuthenticateResult Fail(string reason)
        {
            return AuthenticateResult.Fail(reason);
        }

        private AuthenticateResult Success(IEnumerable<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (Request.Path.StartsWithSegments("/api")
                || Request.Path.StartsWithSegments("/watcherHub")
                || Request.Path.StartsWithSegments("/serviceChangeHub"))
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            var loginUrl = "/Account/Login";
            var returnUrl = Request.Path + Request.QueryString;
            var redirectUrl = $"{loginUrl}?RedirectUrl={WebUtility.UrlEncode(returnUrl)}";

            Response.Redirect(redirectUrl);
            return Task.CompletedTask;
        }
    }
}