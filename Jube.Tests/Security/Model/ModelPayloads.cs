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
using System.Text;

namespace Jube.Test.Security.Model;

public static class ModelPayloads
{
    private const int SleepSeconds = 6;

    private static readonly string[] sqlSeeds =
    [
        "'", "''", "\"", "' OR '1'='1", "' OR 1=1--", "\" OR \"\"=\"", "'; DROP TABLE \"EntityAnalysisModel\";--",
        $"1; SELECT pg_sleep({SleepSeconds})--", "' UNION SELECT NULL,NULL,NULL--",
        "' UNION SELECT \"Password\" FROM \"UserRegistry\"--", $"'||(SELECT pg_sleep({SleepSeconds}))||'",
        $"1' AND (SELECT 1 FROM pg_sleep({SleepSeconds}))--", $"'; SELECT pg_sleep({SleepSeconds}); --",
        $"$$;select pg_sleep({SleepSeconds});$$", "\\'; select 1;--", "1) OR (1=1",
        "' AND 1=CAST((SELECT version()) AS INT)--", "'; COPY (SELECT '') TO PROGRAM 'id';--", "%'; --", "admin'--",
        "') OR ('1'='1", "'; INSERT INTO \"TenantRegistry\"(\"Name\") VALUES ('x');--",
        $"' OR EXISTS(SELECT 1 FROM pg_sleep({SleepSeconds}))--", "\u02BC OR 1=1--", "\uFF07 OR 1=1--",
        "'; UPDATE \"EntityAnalysisModel\" SET \"Active\"=0;--", "1 OR 1=1", "0x27", "\\", "\\\\", "%",
        "' OR pg_read_file('/etc/passwd') IS NOT NULL--", "'; SELECT lo_import('/etc/passwd');--",
        "' AND (SELECT COUNT(*) FROM pg_shadow)>0--"
    ];

    private static readonly string[] xssSeeds =
    [
        "<script>alert(1)</script>", "\"><img src=x onerror=alert(1)>", "<svg/onload=alert(1)>", "javascript:alert(1)",
        "'-alert(1)-'", "<iframe srcdoc='<script>alert(1)</script>'>", "</textarea><script>alert(1)</script>",
        "<a href=\"data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==\">x</a>", "<math><mtext><script>",
        "<body onpageshow=alert(1)>", "\"onmouseover=\"alert(1)", "<img src=1 href=1 onerror=\"javascript:alert(1)\">"
    ];

    private static readonly string[] templateSeeds =
    [
        "{{7*7}}", "${7*7}", "#{7*7}", "<%= 7*7 %>", "{{config}}", "${{7*7}}", "@(7*7)", "{% debug %}",
        "{{constructor.constructor('alert(1)')()}}", "[[${7*7}]]", "*{7*7}"
    ];

    private static readonly string[] commandSeeds =
    [
        "; id", "| id", "`id`", "$(id)", "& whoami", "\n id", "%0a id", "|| ping -c 1 127.0.0.1", "&& cat /etc/passwd"
    ];

    private static readonly string[] directorySeeds =
    [
        "*)(uid=*))(|(uid=*", "' or '1'='1' or ''='", "//*", "\") or (\"\"=\"", "{\"$ne\": null}",
        "{\"$where\": \"sleep(4000)\"}", "'] | //user/*[contains(*,'", "../../../../etc/passwd",
        "..\\..\\windows\\win.ini", "%2e%2e%2f%2e%2e%2fetc%2fpasswd", "....//....//etc/passwd",
        "/etc/passwd%00.png", "file:///etc/passwd", "\\\\127.0.0.1\\c$\\x"
    ];

    private static readonly string[] headerAndControlSeeds =
    [
        "a\r\nX-Injected: 1", "a%0d%0aSet-Cookie: x=1", "a\u2028b", "\r\n\r\n<html>", "\u0001\u0002\u001f",
        "\u202Egnp.exe",
        "\uFF53\uFF45\uFF4C\uFF45\uFF43\uFF54", "\uD83D\uDE00", "A\u030A", "\u200B", "\uFEFF", "%s%s%s%n", "{0}{1}",
        "%x%x%x%x", "%(name)s", "\t", " ", "\u00A0", "\u180E"
    ];

    private static readonly string[] formulaSeeds =
    [
        "=1+1", "+cmd|' /C calc'!A0", "-2+3", "@SUM(1+1)", "\t=1+1", "=HYPERLINK(\"http://x\",\"y\")",
        "=cmd|'/C calc'!A1", "\r=1+1"
    ];

    private static readonly string[] ruleSeeds =
    [
        "System.Diagnostics.Process.Start(\"id\")", "Shell(\"id\")", "Imports System.IO",
        "System.IO.File.ReadAllText(\"/etc/passwd\")", "AppDomain.CurrentDomain", "Environment.Exit(0)",
        "While True : End While", "Return New Threading.Thread(Sub() Do : Loop)", "Assembly.Load(New Byte(){})",
        "System.Reflection.Assembly.GetEntryAssembly()", "Dim x = Nothing : x.ToString()", "#If True Then",
        "Declare Function Kill Lib \"libc\" (ByVal p As Integer) As Integer", "End Function : Function X()",
        "GetType(String).Assembly", "Marshal.AllocHGlobal(2147483647)", "<DllImport(\"libc\")>",
        "Return Payload.Dictionary(\"../../etc/passwd\")",
        "Return New System.Net.WebClient().DownloadString(\"http://127.0.0.1:1/\")",
        "Return System.Environment.GetEnvironmentVariable(\"ConnectionString\")", "Return Nothing.ToString()",
        "Return 1/0", "Return \"" + new string('x', 300) + "\"", "Option Strict Off\nReturn CObj(Me)"
    ];

    public static IReadOnlyList<string> Seeds { get; } =
    [
        .. sqlSeeds, .. xssSeeds, .. templateSeeds, .. commandSeeds, .. directorySeeds,
        .. headerAndControlSeeds, .. formulaSeeds
    ];

    public static IReadOnlyList<string> RuleText { get; } = ruleSeeds;

    public static IReadOnlyList<string> Structured { get; } =
    [
        new string('(', 5000) + "1" + new string(')', 5000),
        new string('{', 5000) + new string('}', 5000),
        new string('[', 5000) + new string(']', 5000),
        string.Concat(Enumerable.Repeat("Not ", 5000)) + "True",
        string.Concat(Enumerable.Repeat("If True Then ", 2000)),
        new string('\n', 20000),
        string.Concat(Enumerable.Repeat("e\u0301", 10000)),
        new string('\u202e', 1000),
        string.Concat(Enumerable.Repeat("a=b;", 10000)),
        "\"" + new string('\\', 4000) + "\"",
        string.Concat(Enumerable.Repeat("%00", 3000))
    ];

    public static IReadOnlyList<string> Expanded { get; } = Expand(Seeds);

    public static IReadOnlyList<string> ExpandedRuleText { get; } = Expand(ruleSeeds);

    public static IReadOnlyList<string> Sample(IReadOnlyList<string> source, int every)
    {
        return [.. source.Where((_, i) => i % every == 0)];
    }

    public static bool IsTimeBlind(string payload)
    {
        return payload.Contains("pg_sleep", StringComparison.OrdinalIgnoreCase);
    }

    public static TimeSpan TimeBlindBound => TimeSpan.FromSeconds(SleepSeconds - 1);

    private static List<string> Expand(IEnumerable<string> seeds)
    {
        var result = new List<string>();
        foreach (var seed in seeds)
        {
            result.Add(seed);
            result.Add(seed.ToUpperInvariant());
            result.Add(MixCase(seed));
            result.Add(seed.Replace(" ", "/**/"));
            result.Add(seed.Replace(" ", "\t"));
            result.Add(seed.Replace(" ", "\n"));
            result.Add(WebUtility.UrlEncode(seed));
            result.Add(WebUtility.UrlEncode(WebUtility.UrlEncode(seed)));
            result.Add(HtmlEntities(seed));
            result.Add(UnicodeEscapedText(seed));
            result.Add("  " + seed + "  ");
            result.Add(seed + "-- ");
            result.Add("\"" + seed + "\"");
            result.Add(seed + seed);
            result.Add(seed.Length > 3 ? seed.Insert(seed.Length / 2, "/**/") : seed + "/**/");
        }

        return [.. result.Distinct(StringComparer.Ordinal)];
    }

    private static string MixCase(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            builder.Append(i % 2 == 0 ? char.ToUpperInvariant(value[i]) : char.ToLowerInvariant(value[i]));
        }

        return builder.ToString();
    }

    private static string HtmlEntities(string value)
    {
        var builder = new StringBuilder(value.Length * 4);
        foreach (var c in value)
        {
            builder.Append("&#x").Append(((int)c).ToString("x")).Append(';');
        }

        return builder.ToString();
    }

    private static string UnicodeEscapedText(string value)
    {
        var builder = new StringBuilder(value.Length * 6);
        foreach (var c in value)
        {
            builder.Append("\\u").Append(((int)c).ToString("x4"));
        }

        return builder.ToString();
    }
}