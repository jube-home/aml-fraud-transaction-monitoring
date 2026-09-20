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

namespace Jube.App.Code
{
    using System;
    using DynamicEnvironment;
    using Jube.Dto.Authentication;
    using Microsoft.AspNetCore.Http;

    public static class AuthenticationCookieIssuer
    {
        public static void AppendCookies(HttpResponse response, DynamicEnvironment dynamicEnvironment, string token,
            DateTime expiration)
        {
            var cookieExpiration = dynamicEnvironment.AppSettings("SessionCookie")
                .Equals("True", StringComparison.OrdinalIgnoreCase)
                ? (DateTime?)null
                : expiration;

            var tokenOptions = new CookieOptions
                { Expires = cookieExpiration, HttpOnly = true, SameSite = SameSiteMode.Lax };

            var expiryOptions = new CookieOptions
                { Expires = cookieExpiration, HttpOnly = false, SameSite = SameSiteMode.Lax };

            if (dynamicEnvironment.AppSettings("SecureHttpCookie").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                tokenOptions.Secure = true;
                tokenOptions.SameSite = SameSiteMode.Strict;
                expiryOptions.Secure = true;
                expiryOptions.SameSite = SameSiteMode.Strict;
            }
            else if (response.HttpContext.Request.IsHttps)
            {
                tokenOptions.Secure = true;
                expiryOptions.Secure = true;
            }

            response.Cookies.Append("authentication-jwt", token, tokenOptions);
            response.Cookies.Append("authentication-expiry", expiration.ToString("O"), expiryOptions);
        }

        public static void DeleteCookies(HttpResponse response, DynamicEnvironment dynamicEnvironment)
        {
            var secure = dynamicEnvironment.AppSettings("SecureHttpCookie")
                .Equals("True", StringComparison.OrdinalIgnoreCase);
            var sameSite = secure ? SameSiteMode.Strict : SameSiteMode.Lax;
            var isSecure = secure || response.HttpContext.Request.IsHttps;

            response.Cookies.Delete("authentication-jwt",
                new CookieOptions { HttpOnly = true, SameSite = sameSite, Secure = isSecure });
            response.Cookies.Delete("authentication-expiry",
                new CookieOptions { HttpOnly = false, SameSite = sameSite, Secure = isSecure });
        }

        public static AuthenticationResponseDto IssueAuthenticationCookies(
            HttpResponse response,
            DynamicEnvironment dynamicEnvironment,
            string userName,
            TimeProvider timeProvider = null)
        {
            var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
            var token = Jwt.CreateToken(userName,
                dynamicEnvironment.AppSettings("JWTKey"),
                dynamicEnvironment.AppSettings("JWTValidIssuer"),
                dynamicEnvironment.AppSettings("JWTValidAudience"),
                now
            );

            var expiration = now.AddMinutes(15);

            var authenticationDto = new AuthenticationResponseDto
            {
                Token = token,
                Expiration = expiration
            };

            AppendCookies(response, dynamicEnvironment, authenticationDto.Token, expiration);

            return authenticationDto;
        }
    }
}