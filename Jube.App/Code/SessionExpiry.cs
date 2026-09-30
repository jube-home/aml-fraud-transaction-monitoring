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
    using System.Globalization;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;

    public static class SessionExpiry
    {
        public const string HeaderName = "X-Session-Expiry";

        private const string ItemsKey = "Jube.SessionExpiry";

        public static void Publish(HttpContext httpContext, DateTime expiration)
        {
            httpContext.Items[ItemsKey] = expiration;
            httpContext.Response.Headers[HeaderName] = expiration.ToString("O", CultureInfo.InvariantCulture);
        }

        public static void Revoke(HttpContext httpContext)
        {
            httpContext.Items.Remove(ItemsKey);
            httpContext.Response.Headers.Remove(HeaderName);
        }

        public static DateTime? Resolve(HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue(ItemsKey, out var published) && published is DateTime expiration)
            {
                return expiration;
            }

            var claim = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Exp);
            if (long.TryParse(claim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }

            return null;
        }
    }
}