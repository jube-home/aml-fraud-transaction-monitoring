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

namespace Jube.Test.Security.Visualisation;

internal static class VisPayloads
{
    private static readonly string[] openers = ["", "'", "\"", "')", "\")", "1", "-1", "0", "1'", "x'"];

    private static readonly string[] bodies =
    [
        " OR 1=1", " OR '1'='1", " AND 1=2", "; SELECT pg_sleep(6)", " UNION SELECT NULL,NULL,NULL",
        " UNION ALL SELECT \"Name\",\"Guid\" FROM \"UserRegistry\"", "; SELECT 1",
        " AND (SELECT pg_sleep(6)) IS NULL", " OR 1=(SELECT COUNT(*) FROM \"UserRegistry\")",
        "||(SELECT pg_sleep(6))||", " OR EXISTS(SELECT 1 FROM pg_sleep(6))",
        "; INSERT INTO \"ZzPenProbe\" VALUES (1)", "%' OR '%'='", "\\'; SELECT 1;"
    ];

    private static readonly string[] tails = ["--", "-- -", "#", "/*", ";", "", ";--"];

    private static readonly string[] others =
    [
        "<script>alert(1)</script>", "\"><img src=x onerror=alert(1)>", "<svg/onload=alert(1)>",
        "javascript:alert(1)", "{{7*7}}", "${7*7}", "<%= 7*7 %>", "#{7*7}",
        "{{constructor.constructor('return 1')()}}", "$(id)", "`id`", "; ls", "| id", "&& id",
        "*)(uid=*))(|(uid=*", "' or ''='", "{\"$ne\":null}", "{\"$gt\":\"\"}", "//*", "' or 1=1 or ''='",
        "=1+1", "+1+1", "-1+1", "@SUM(1+1)", "\t=1+1", "=cmd|' /C calc'!A0", "%s%s%s%n", "{0}{1}", "%x%x%x",
        "\u202Etxt.exe", "\uFF1Cscript\uFF1E", "\uFB01", "\u0130", "\uFF21", "\u0085", "\u2028",
        "\r\nX-Injected: 1", "\r\nSet-Cookie: a=b", "%00", "\u0000", "a\u0000b", "\u0007\u001b[31m"
    ];

    private static IEnumerable<string> Mutate(string seed)
    {
        yield return seed;
        yield return CaseMix(seed);
        yield return seed.Replace(" ", "/**/");
        yield return seed.Replace(" ", "\t");
        yield return seed.Replace(" ", "%20");
        yield return Uri.EscapeDataString(seed);
        yield return Uri.EscapeDataString(Uri.EscapeDataString(seed));
        yield return seed.Replace("'", "\u02BC").Replace("\"", "\uFF02");
        yield return "\u202E" + seed;
        yield return System.Net.WebUtility.HtmlEncode(seed);
        yield return seed + " ";
        yield return " " + seed;
    }

    private static string CaseMix(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i += 2)
        {
            chars[i] = char.ToUpperInvariant(chars[i]);
        }

        for (var i = 1; i < chars.Length; i += 2)
        {
            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }

    public static IReadOnlyList<string> Sql(int take = 240)
    {
        var all = new List<string>();
        foreach (var opener in openers)
        {
            foreach (var body in bodies)
            {
                foreach (var tail in tails)
                {
                    all.Add(opener + body + tail);
                }
            }
        }

        var stride = Math.Max(1, all.Count / Math.Max(1, take / 4));
        var picked = new List<string>();
        for (var i = 0; i < all.Count; i += stride)
        {
            picked.AddRange(Mutate(all[i]).Where((_, index) => index % 3 == picked.Count % 3 || index == 0));
        }

        return [.. picked.Distinct().Take(take)];
    }

    public static IReadOnlyList<string> OtherVariants()
    {
        return [.. others.SelectMany(seed => Mutate(seed).Take(4)).Distinct()];
    }

    public static IReadOnlyList<string> RouteSegments()
    {
        var list = new List<string>
        {
            "abc", "-1", "0", "1.5", "1e3", "0x10", "2147483648", "99999999999999999999", "-2147483649", "NaN",
            "true", "null", "undefined", "%00", "%0d%0a", "..", "%2e%2e", "..%2f..%2f", "%252e%252e%252f", "a b",
            "1%20OR%201=1", "1;select%201", "%27", "%22", "%3Cscript%3E", "{id}", "*", "%", "%zz", "\u00E9",
            "%E2%80%AE", "%EF%BB%BF", new string('9', 400), new string('a', 4000)
        };
        list.AddRange(Sql(60).Select(Uri.EscapeDataString));
        return list;
    }

    public static IReadOnlyList<string> GuidSegments()
    {
        return
        [
            "00000000-0000-0000-0000-000000000000", "ffffffff-ffff-ffff-ffff-ffffffffffff", "not-a-guid",
            "{00000000-0000-0000-0000-000000000000}", "00000000000000000000000000000000",
            "0000000000000000-0000-000000000000", "1", "-1", "null", "%00", "'", "1 OR 1=1", "%27%20OR%201=1",
            "..%2f", new string('f', 300), "GGGGGGGG-GGGG-GGGG-GGGG-GGGGGGGGGGGG",
            "0000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-0000000000000"
        ];
    }
}