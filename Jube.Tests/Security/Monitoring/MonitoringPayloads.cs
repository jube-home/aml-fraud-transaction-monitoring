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

namespace Jube.Test.Security.Monitoring;

public static class MonitoringPayloads
{
    private static readonly string[] sqlSeeds =
    [
        "' OR '1'='1", "' OR 1=1--", "1; DROP TABLE \"RedisSlowOperation\";--",
        "' UNION SELECT NULL,NULL,version()--", "'; SELECT pg_sleep(8);--", "1 AND (SELECT 1 FROM pg_sleep(8))",
        "\" OR \"\"=\"", "') OR ('1'='1", "$$;select pg_sleep(8);$$", "'||(SELECT pg_sleep(8))||'", "admin'--",
        "1' ORDER BY 99--", "'; COPY (SELECT '') TO PROGRAM 'id';--", "' AND 1=CAST((SELECT current_user) AS int)--",
        "\\'; select 1;--", "%' OR '%'='", "1); SELECT pg_sleep(8);--", "'; WAITFOR DELAY '0:0:8';--"
    ];

    public static readonly string[] Xss =
    [
        "<script>alert(1)</script>", "\"><img src=x onerror=alert(1)>", "<svg/onload=alert(1)>",
        "javascript:alert(1)", "'-alert(1)-'", "</script><script>alert(2)</script>",
        "<iframe srcdoc=\"<script>1</script>\">"
    ];

    public static readonly string[] Templates =
    [
        "{{7*7}}", "${7*7}", "#{7*7}", "<%= 7*7 %>", "${jndi:ldap://127.0.0.1/a}",
        "{{constructor.constructor('return 1')()}}",
        "%s%s%s%n", "%x%x%x%x", "{0}{1}{2}", "$(id)", "`id`", "; id", "| id", "&& id", "\r\nSet-Cookie: pen=1",
        "%0d%0aX-Injected: 1", "../../../../etc/passwd", "..\\..\\..\\windows\\win.ini", "/etc/passwd%00",
        "*)(uid=*))(|(uid=*", "' or ''='", "//*", "{\"$ne\":null}", "{$where: '1'}", "=cmd|' /C calc'!A0", "+1+1",
        "@SUM(1)",
        "\t=1+1", "\u202Etxt.exe", "\uFF07 OR 1=1--", "\u0000", "\u0001\u0002\u001f", "\uFEFF", "\u00e9\u0301", "%",
        "_", "\\",
        "%%%", "\\%", "[[:alpha:]]", "(?i)a", ".*", "'\"`;"
    ];

    public static readonly string[] RawEncoded =
    [
        "%FF", "%FE%FF", "%C0%AF", "%ED%A0%80", "%E2%82", "%u0041", "%2", "%", "%G0", "%00", "%00%00",
        "%252e%252e%252f", "%c0%ae%c0%ae/", "%EF%BB%BF", "%25%32%37", "%2527", "%0a", "%0d%0a%0d%0a"
    ];

    public static IEnumerable<string> Sqli()
    {
        var all = new HashSet<string>(StringComparer.Ordinal);
        foreach (var seed in sqlSeeds)
        {
            all.Add(seed);
            all.Add(CaseMix(seed));
            all.Add(seed.Replace(" ", "/**/"));
            all.Add(seed.Replace(" ", "\t"));
            all.Add(seed.Replace(" ", "\n"));
            all.Add(seed.Replace(" ", "%20"));
            all.Add(seed.Replace(" ", "+"));
            all.Add(seed.Replace("'", "\uFF07").Replace("-", "\u2010"));
            all.Add(seed.Replace("SELECT", "SEL/**/ECT").Replace("select", "sel/**/ect"));
            all.Add(seed + "\u0000");
            all.Add("\u0000" + seed);
            all.Add(Uri.EscapeDataString(seed));
            all.Add("(" + seed + ")");
        }

        return all;
    }

    public static string CaseMix(string value)
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

    public static IEnumerable<string> IntValues() =>
    [
        "-1", "0", "1", "100000", "100001", "2147483647", "2147483648", "-2147483648", "-2147483649",
        "9999999999999999999999", "1e3", "1E10", "1.5", "0x10", "abc", "", " ", "%20", "NaN", "Infinity", "+5", "--5",
        "1,2", "[1]", "{}", "null", "true", "\u0663", "\uFF11", "1 OR 1=1", "1;--", "0000000000000000000001", "-0",
        "1%00", "%EF%BC%91", "1\n2"
    ];

    public static IEnumerable<string> DateValues() =>
    [
        "not-a-date", "0001-01-01", "0001-01-01T00:00:00Z", "9999-12-31T23:59:59Z", "9999-12-31T23:59:59.9999999Z",
        "10000-01-01", "2020-13-45", "2020-02-30", "-1", "1e9", "0", "null", "2020-01-01T00:00:00+99:99",
        "2020-01-01T25:61:61", "2020-01-01' OR '1'='1", "'; select 1;--", "2020-01-01T00:00:00Z", "2099-01-01",
        "1900-01-01", "Tue, 01 Jan 2030 00:00:00 GMT", "2020-01-01T00:00:00.000000000000001Z",
        "\u0662\u0660\u0662\u0660-01-01",
        "yesterday", "now()", "'2020-01-01'::date"
    ];

    public static IEnumerable<string> DoubleValues() =>
    [
        "-1", "0", "50", "100", "101", "1e308", "1e309", "-1e309", "NaN", "Infinity", "-Infinity", "abc", "0.0000001",
        "1,5", "'", "0x1F", "\u0663", "1e-400", "100.00000000000001", "-0", "99999999999999999999999999999999999999"
    ];

    public static IEnumerable<string> SortValues() =>
    [
        "occurredDate", "OCCURREDDATE", "durationMicroseconds", "id", "Id", "instance", "message", "query", "pid",
        "unknown", "", " ", "occurredDate desc", "occurredDate;drop table x", "1", "-1", "(select 1)", "id,pg_sleep(8)",
        "occurredDate)--", "__proto__", "constructor", "toString", new string('a', 4000), "\u0000",
        "occurredDate\u0000",
        "'||pg_sleep(8)||'"
    ];

    public static IEnumerable<string> DirectionValues() =>
    [
        "asc", "ASC", "desc", "DESC", "Asc", "ascending", "", " ", "asc;drop table x", "asc,id", "0", "1", "-1",
        "asc\u0000", "'", "asc--", "ASC NULLS FIRST", "sideways", new string('d', 4000)
    ];

    public static IEnumerable<string> SearchValues()
    {
        foreach (var s in Sqli())
        {
            yield return s;
        }

        foreach (var s in Xss)
        {
            yield return s;
        }

        foreach (var s in Templates)
        {
            yield return s;
        }

        foreach (var s in new[]
                 {
                     "", " ", "a", "hunter2", "AUTH", "password", "SET", "GET", new string('x', 4000),
                     new string('%', 1500),
                     new string('_', 1500), string.Concat(Enumerable.Repeat("\u0301", 1500)), "\U0001F600\U0001F4A5"
                 })
        {
            yield return s;
        }
    }

    public static string Enc(string value)
    {
        return Uri.EscapeDataString(value);
    }
}