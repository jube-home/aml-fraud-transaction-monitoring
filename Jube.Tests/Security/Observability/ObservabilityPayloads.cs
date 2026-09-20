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
using System.Text;
using Jube.Test.Security.PenTest;

namespace Jube.Test.Security.Observability;

public static class ObservabilityPayloads
{
    private const int Cap = 320;

    private static readonly string[] prefixes = ["", "'", "\"", "')", "\")", "1", "1)", "x'", "0'", "%'", "\\"];

    private static readonly string[] conditions =
    [
        "OR 1=1", "AND 1=2", "OR 'a'='a'", "OR TRUE", "AND (SELECT 1)=1", "UNION SELECT NULL", "UNION ALL SELECT 1,2,3",
        "OR 1=CAST((SELECT version()) AS int)", "AND EXISTS(SELECT 1 FROM pg_user)",
        "OR (SELECT COUNT(*) FROM \"UserRegistry\")>0",
        "; DROP TABLE \"ApplicationLogEntry\"", "; UPDATE \"UserRegistry\" SET \"Password\"='x'",
        "ORDER BY 99", "; COPY (SELECT 1) TO PROGRAM 'id'", "OR pg_read_file('/etc/passwd') IS NOT NULL",
        "OR lo_import('/etc/passwd')>0", "AND 1=(SELECT 1 FROM pg_sleep(0))"
    ];

    private static readonly string[] tails = ["", "--", "#", "/*", ";--", "--+", "\0"];

    public static IReadOnlyList<string> TimeBlind { get; } =
    [
        "1;SELECT pg_sleep(6)--", "' OR (SELECT 1 FROM pg_sleep(6))=1--", "'||pg_sleep(6)||'",
        "1 AND (SELECT pg_sleep(6)) IS NULL", "');SELECT pg_sleep(6);--", "\";SELECT pg_sleep(6);--",
        "1);SELECT pg_sleep(6)--", "'; WAITFOR DELAY '0:0:6'--", "' AND 1=(SELECT 1 FROM pg_sleep(6))--",
        "level,(SELECT 1 FROM pg_sleep(6))", "level;SELECT pg_sleep(6)",
        "message DESC,(CASE WHEN 1=1 THEN pg_sleep(6) END)"
    ];

    public static readonly TimeSpan SleepBound = TimeSpan.FromSeconds(5.5);

    public static IReadOnlyList<string> Injection { get; } = Build();

    public static IReadOnlyList<string> SortNameVariants { get; } = BuildSortVariants();

    private static IReadOnlyList<string> BuildSortVariants()
    {
        var names = new[]
        {
            "level", "loggerName", "message", "exception", "occurredDate", "createdDate", "instance", "id",
            "elapsedMicroseconds", "entityAnalysisModelName", "stageName", "durationMicroseconds"
        };
        var suffixes = new[]
        {
            "", " ", " asc", " DESC", ";", "--", "\"", "'", ",1", ",(SELECT 1)", "\0", "\t", "%", "ı", "－－",
            "; DROP TABLE x", " DESC,pg_sleep(0)"
        };
        var result = new List<string>();
        foreach (var name in names)
        {
            foreach (var suffix in suffixes)
            {
                result.Add(name + suffix);
                result.Add(suffix + name);
            }

            result.Add(name.ToUpperInvariant());
            result.Add(name.ToLowerInvariant());
            result.Add($"\"{name}\"");
            result.Add($"t.{name}");
            result.Add($"[{name}]");
        }

        return [.. result.Distinct()];
    }

    private static IReadOnlyList<string> Build()
    {
        var all = new List<string>();
        all.AddRange(PenTestPayloads.Sql);
        all.AddRange(PenTestPayloads.SqlNumeric);
        all.AddRange(PenTestPayloads.Xss);
        all.AddRange(PenTestPayloads.Ssti);
        all.AddRange(PenTestPayloads.CommandInjection);
        all.AddRange(PenTestPayloads.LdapXPathNoSql);
        all.AddRange(PenTestPayloads.CrlfInjection);
        all.AddRange(PenTestPayloads.PathTraversal);
        all.AddRange(PenTestPayloads.FormatString);
        all.AddRange(PenTestPayloads.UnicodeTricks);
        all.AddRange(PenTestPayloads.CsvFormulaInjection);

        var grammar = new List<string>();
        foreach (var prefix in prefixes)
        {
            foreach (var condition in conditions)
            {
                foreach (var tail in tails)
                {
                    grammar.Add($"{prefix} {condition} {tail}".Trim());
                }
            }
        }

        var stride = Math.Max(1, grammar.Count / 120);
        for (var i = 0; i < grammar.Count; i += stride)
        {
            all.Add(grammar[i]);
        }

        var seeds = all.Take(60).ToList();
        foreach (var seed in seeds)
        {
            all.Add(MixCase(seed));
            all.Add(seed.Replace(" ", "/**/"));
            all.Add(seed.Replace(' ', '\t'));
            all.Add(seed.Replace(' ', '\u000b'));
            all.Add(seed.Replace('\'', 'ʼ').Replace('"', '“'));
            all.Add(seed.Replace("'", "''"));
            all.Add(Uri.EscapeDataString(seed));
            all.Add(Uri.EscapeDataString(Uri.EscapeDataString(seed)));
            all.Add("('" + seed + "')||('a')");
            all.Add(HtmlEncode(seed));
        }

        var distinct = all.Distinct().ToList();
        if (distinct.Count <= Cap)
        {
            return distinct;
        }

        var step = (double)distinct.Count / Cap;
        return [.. Enumerable.Range(0, Cap).Select(i => distinct[(int)(i * step)])];
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

    private static string HtmlEncode(string value)
    {
        var builder = new StringBuilder(value.Length * 4);
        foreach (var c in value)
        {
            builder.Append("&#").Append((int)c).Append(';');
        }

        return builder.ToString();
    }
}