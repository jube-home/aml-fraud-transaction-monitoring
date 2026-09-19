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
    using System.Globalization;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Code;
    using Data.Security;
    using DynamicEnvironment;
    using Microsoft.AspNetCore.Http;

    public class TokenRefreshMiddleware(RequestDelegate next, DynamicEnvironment dynamicEnvironment)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            await TryRefreshAccessTokenAsync(context);
            await next(context);
        }

        private Task TryRefreshAccessTokenAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/Account/Logout", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.StartsWithSegments("/api/Authentication/Logout",
                    StringComparison.OrdinalIgnoreCase)
                || context.User.Identity?.IsAuthenticated != true
                || !context.Request.Cookies.TryGetValue("authentication-jwt", out var accessToken)
                || String.IsNullOrEmpty(accessToken))
            {
                return Task.CompletedTask;
            }

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return Task.CompletedTask;
            }

            var username = context.User.FindFirstValue(ClaimTypes.Name);
            if (String.IsNullOrEmpty(username))
            {
                return Task.CompletedTask;
            }

            var now = DateTime.UtcNow;
            var sessionStart = now;
            if (long.TryParse(context.User.FindFirstValue(TokenValidity.IssuedMillisecondsClaim),
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out var sessionStartMilliseconds))
            {
                sessionStart = DateTimeOffset.FromUnixTimeMilliseconds(sessionStartMilliseconds).UtcDateTime;
            }

            var expiration = now.AddMinutes(15);
            var absoluteLifetime = AbsoluteSessionLifetime.From(dynamicEnvironment);
            if (absoluteLifetime.HasValue && sessionStart + absoluteLifetime.Value < expiration)
            {
                expiration = sessionStart + absoluteLifetime.Value;
            }

            if (expiration <= now)
            {
                return Task.CompletedTask;
            }

            var token = Jwt.CreateToken(username,
                dynamicEnvironment.AppSettings("JWTKey"),
                dynamicEnvironment.AppSettings("JWTValidIssuer"),
                dynamicEnvironment.AppSettings("JWTValidAudience"),
                now, sessionStart, expiration
            );

            AuthenticationCookieIssuer.AppendCookies(context.Response, dynamicEnvironment, token, expiration);

            return Task.CompletedTask;
        }
    }
}