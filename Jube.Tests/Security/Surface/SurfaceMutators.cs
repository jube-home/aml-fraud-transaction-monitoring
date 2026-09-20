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
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Jube.Test.Security.Surface;

public static class SurfaceMutators
{
    public static readonly string[] InjectionSeeds =
    [
        "' OR '1'='1", "admin'--", "'; SELECT pg_sleep(10);--", "\" OR \"\"=\"", "1; DROP TABLE \"UserRegistry\";--",
        "' UNION SELECT NULL,NULL,NULL--", "' AND (SELECT 1 FROM pg_sleep(10))--", "${7*7}", "{{7*7}}",
        "<script>alert(1)</script>", "../../etc/passwd", "%s%s%s%n", "a\r\nSet-Cookie: injected=1",
        "*)(uid=*))(|(uid=*", "' or 1=1 or ''='", "{\"$ne\":null}", "\\u0000", "‮admin", "ａｄｍｉｎ",
        "admin\u0000", "$(id)", "`id`", "|| id", "'; COPY (SELECT '') TO PROGRAM 'id';--"
    ];

    public static IEnumerable<string> Variants(string seed)
    {
        yield return seed;
        yield return seed.ToUpperInvariant();
        yield return CaseMix(seed);
        yield return seed.Replace(" ", "/**/");
        yield return seed.Replace(" ", "\t");
        yield return seed.Replace(" ", "%20");
        yield return Uri.EscapeDataString(seed);
        yield return Uri.EscapeDataString(Uri.EscapeDataString(seed));
        yield return string.Concat(seed.Select(c => $"\\u{(int)c:x4}"));
        yield return WebUtility.HtmlEncode(seed);
        yield return " " + seed + " ";
        yield return seed + new string(' ', 300);
        yield return "a" + seed + "z";
    }

    public static IEnumerable<string> AllVariants()
    {
        return InjectionSeeds.SelectMany(Variants).Distinct();
    }

    private static string CaseMix(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            builder.Append(i % 2 == 0 ? char.ToUpperInvariant(value[i]) : char.ToLowerInvariant(value[i]));
        }

        return builder.ToString();
    }

    public static string Marker(int seed)
    {
        return "zzmark" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("surface" + seed)))[..10]
            .ToLowerInvariant();
    }
}