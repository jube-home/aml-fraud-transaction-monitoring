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
    using System.Linq;
    using Microsoft.AspNetCore.Http;

    public static class OAuthSignInInterstitial
    {
        public static bool IsSafeTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target) || target.Any(char.IsControl))
            {
                return false;
            }

            if (target.StartsWith("/", StringComparison.Ordinal))
            {
                return !target.StartsWith("//", StringComparison.Ordinal)
                       && !target.StartsWith("/\\", StringComparison.Ordinal);
            }

            return Uri.TryCreate(target, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }

        public static void Write(HttpResponse response, string target)
        {
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "text/html";
            response.ContentLength = 0;
            response.Headers.CacheControl = "no-store";
            response.Headers["Refresh"] = $"0; url={target}";
        }
    }
}