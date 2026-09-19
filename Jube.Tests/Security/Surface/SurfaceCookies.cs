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
using System.Text.Json;
using Jube.Test.Security.PenTest;

namespace Jube.Test.Security.Surface;

public static class SurfaceCookies
{
    public static IReadOnlyList<string> SetCookies(PenTestResponse response)
    {
        return [.. response.HeaderValues("Set-Cookie")];
    }

    public static string? Jwt(PenTestResponse response)
    {
        var cookie = SetCookies(response)
            .LastOrDefault(c => c.StartsWith("authentication-jwt=", StringComparison.Ordinal));
        return cookie?.Split(';')[0]["authentication-jwt=".Length..];
    }

    public static string Header(string jwt)
    {
        return $"authentication-jwt={jwt}; authentication-expiry=x";
    }

    public static string? Subject(string jwt)
    {
        try
        {
            var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.EndsWith("/name", StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.GetString();
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}