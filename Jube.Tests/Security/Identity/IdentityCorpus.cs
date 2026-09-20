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
using Jube.Test.Security.PenTest;

namespace Jube.Test.Security.Identity;

public static class IdentityCorpus
{
    private static readonly Func<string, string>[] mutators =
    [
        CaseMix,
        seed => seed.Replace(" ", "/**/"),
        seed => seed.Replace(" ", "\t"),
        seed => seed.Replace(" ", "%20"),
        Uri.EscapeDataString,
        seed => Uri.EscapeDataString(Uri.EscapeDataString(seed)),
        WebUtility.HtmlEncode,
        seed => "a" + seed + "b",
        seed => seed + "\r\n",
        seed => seed.Replace('\'', '＇').Replace('"', '＂'),
        seed => "  " + seed + "  ",
        seed => seed + "‮",
        seed => seed.ToUpperInvariant(),
        seed => seed + " -- ",
        seed => "(" + seed + ")"
    ];

    private static IReadOnlyList<string> Seeds { get; } = BuildSeeds();

    public static IReadOnlyList<string> Injection { get; } = Expand(Seeds, 2, 420);

    public static IReadOnlyList<string> InjectionSmall { get; } = [.. Injection.Where((_, i) => i % 5 == 0)];

    public static IReadOnlyList<string> NumericProbes { get; } = BuildNumericProbes();

    public static IReadOnlyList<string> GuidProbes { get; } =
    [
        .. PenTestPayloads.TypeConfusionGuid
            .Concat(PenTestPayloads.Sql.Take(12)).Concat(PenTestPayloads.PathTraversal.Take(4))
            .Concat([
                "00000000-0000-0000-0000-000000000001", "ffffffff-ffff-ffff-ffff-ffffffffffff",
                "11111111-1111-1111-1111-11111111111g", "{00000000-0000-0000-0000-000000000000}",
                "00000000000000000000000000000000", "urn:uuid:00000000-0000-0000-0000-000000000000"
            ]).Distinct()
    ];

    private static string CaseMix(string seed)
    {
        return new string(seed.Select((c, i) => i % 2 == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c))
            .ToArray());
    }

    private static List<string> BuildSeeds()
    {
        var set = new List<string>();
        set.AddRange(PenTestPayloads.Sql);
        set.AddRange(PenTestPayloads.SqlTimeBlind);
        set.AddRange(PenTestPayloads.Xss);
        set.AddRange(PenTestPayloads.Ssti);
        set.AddRange(PenTestPayloads.CommandInjection);
        set.AddRange(PenTestPayloads.LdapXPathNoSql);
        set.AddRange(PenTestPayloads.CrlfInjection);
        set.AddRange(PenTestPayloads.PathTraversal);
        set.AddRange(PenTestPayloads.NullBytesAndControls);
        set.AddRange(PenTestPayloads.FormatString);
        set.AddRange(PenTestPayloads.CsvFormulaInjection);
        set.AddRange(PenTestPayloads.UnicodeTricks);

        string[] openers = ["'", "\"", "')", "'))", "\\", "$$", "$a$"];
        string[] bodies =
        [
            " OR 1=1", "; DROP TABLE \"UserRegistry\"", "; UPDATE \"UserRegistry\" SET \"Active\"=1",
            " UNION SELECT \"Password\" FROM \"UserRegistry\"", "; SELECT pg_sleep(2)", " AND 1=(SELECT 1)",
            "; COPY (SELECT 1) TO PROGRAM 'id'", " OR EXISTS(SELECT 1 FROM pg_shadow)",
            "; INSERT INTO \"RoleRegistryPermission\" DEFAULT VALUES", " ORDER BY 1000", "||chr(65)||"
        ];
        string[] closers = ["--", "/*", "#", ";--", "", " --"];
        for (var i = 0; i < openers.Length; i++)
        {
            for (var j = 0; j < bodies.Length; j++)
            {
                set.Add(openers[i] + bodies[j] + closers[(i + j) % closers.Length]);
            }
        }

        return [.. set.Distinct()];
    }

    private static List<string> Expand(IReadOnlyList<string> seeds, int mutationsPerSeed, int cap)
    {
        var all = new List<string>(seeds);
        for (var i = 0; i < seeds.Count; i++)
        {
            for (var m = 0; m < mutationsPerSeed; m++)
            {
                all.Add(mutators[(i * 7 + m * 3) % mutators.Length](seeds[i]));
            }
        }

        var distinct = all.Distinct().ToList();
        if (distinct.Count <= cap)
        {
            return distinct;
        }

        var stride = (double)distinct.Count / cap;
        return [.. Enumerable.Range(0, cap).Select(i => distinct[(int)(i * stride)]).Distinct()];
    }

    private static List<string> BuildNumericProbes()
    {
        var list = new List<string>(PenTestPayloads.TypeConfusionInt);
        list.AddRange(PenTestPayloads.SqlNumeric);
        list.AddRange([
            "2147483647", "-2147483648", "4294967296", "1e0", "+1", "--1", "1.0", "1_0", "0b1", "٠",
            "1;", "1'", "1%00", "1%0d%0a", "%31", "１２３", "1\t", "\t1", "0x7fffffff", "1e999", "-0", "00000000001"
        ]);
        return [.. list.Distinct()];
    }
}