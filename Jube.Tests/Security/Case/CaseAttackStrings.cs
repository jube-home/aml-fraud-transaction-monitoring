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

namespace Jube.Test.Security.Case;

public static class CaseAttackStrings
{
    private static readonly string[] sqlSeeds =
    [
        "'", "''", "\"", "' OR '1'='1", "' OR 1=1--", "\" OR \"\"=\"", "') OR ('1'='1", "1; DROP TABLE \"Case\"--",
        "' UNION SELECT NULL,NULL,NULL--", "' UNION SELECT \"Name\",NULL FROM \"UserRegistry\"--",
        "'; SELECT pg_sleep(5)--", "1' AND (SELECT 1 FROM pg_sleep(5))--", "$$;SELECT pg_sleep(5);$$",
        "' || pg_sleep(5) || '", "1) OR (1=1", "'/*", "\\'; --", "1 AND 1=CAST((SELECT version()) AS int)",
        "'; UPDATE \"Case\" SET \"Locked\"=1--", "x'; DELETE FROM \"CaseNote\"--", "1 OR 1=1", "-1 UNION SELECT 1",
        "0x27", "%27", "' AND '1'='1' -- -", "';WAITFOR DELAY '0:0:5'--"
    ];

    private static readonly string[] markupSeeds =
    [
        "<script>alert(1)</script>", "\"><img src=x onerror=alert(1)>", "<svg/onload=alert(1)>", "javascript:alert(1)",
        "'-alert(1)-'", "<iframe srcdoc='<script>alert(1)</script>'>", "{{7*7}}", "${7*7}", "#{7*7}", "<%= 7*7 %>",
        "@(7*7)", "{{constructor.constructor('return 1')()}}", "<style>@import'//x'</style>",
        "</textarea><script>1</script>",
        "<a href=\"javascript:alert(1)\">x</a>", "<!--#exec cmd=\"id\"-->"
    ];

    private static readonly string[] otherSeeds =
    [
        "../../etc/passwd", "..\\..\\windows\\win.ini", "%2e%2e%2f%2e%2e%2fetc%2fpasswd", "a\0b", "\r\nX-Injected: 1",
        "%0d%0aSet-Cookie: x=1", "=cmd|' /C calc'!A0", "+1+1", "-1-1", "@SUM(1+1)", "\t=1+1", "%s%s%s%n", "{0}{1}",
        "ｕｎｉｏｎ ｓｅｌｅｃｔ", "‮evil‬", "$(id)", "`id`",
        "; id", "| id", "*)(uid=*))(|(uid=*", "{\"$ne\":null}", "//*", "' or ''='",
        "<!DOCTYPE x [<!ENTITY e SYSTEM \"file:///etc/passwd\">]><x>&e;</x>", "__proto__", "constructor",
        "\u0001\u0002\u001f",
        "é́", "😀", "NaN", "Infinity", "1e999", "9223372036854775808", "-9223372036854775809"
    ];

    public static IReadOnlyList<string> Seeds { get; } = [.. sqlSeeds, .. markupSeeds, .. otherSeeds];

    public static IReadOnlyList<string> SqlOnly { get; } = sqlSeeds;

    public static IReadOnlyList<string> Mutated { get; } = [.. Seeds.SelectMany(Variants).Distinct()];

    public static IReadOnlyList<string> MutatedSql { get; } = [.. sqlSeeds.SelectMany(Variants).Distinct()];

    public static IEnumerable<string> Variants(string seed)
    {
        yield return seed;
        yield return AlternateCase(seed);
        yield return seed.Replace(" ", "/**/");
        yield return seed.Replace(" ", "\t");
        yield return seed.Replace(" ", "%20");
        yield return seed.Replace(" ", "+");
        yield return Uri.EscapeDataString(seed);
        yield return Uri.EscapeDataString(Uri.EscapeDataString(seed));
        yield return string.Concat(seed.Select(c => c is '\'' or '"' or '<' or '>' ? $"\\u{(int)c:x4}" : c.ToString()));
        yield return string.Concat(seed.Select(c => c is '\'' or '"' or '<' or '>' ? $"&#{(int)c};" : c.ToString()));
        yield return "x" + seed;
        yield return seed + "x";
        yield return "1" + seed;
        yield return seed + seed;
        yield return $" {seed} ";
        yield return seed + "\u0000";
    }

    private static string AlternateCase(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = i % 2 == 0 ? char.ToUpperInvariant(chars[i]) : char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }
}