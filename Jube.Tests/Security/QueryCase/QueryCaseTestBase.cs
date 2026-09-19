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
using System.Text.Json;
using System.Threading.Tasks;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using Xunit.Abstractions;

namespace Jube.Test.Security.QueryCase;

public abstract class QueryCaseTestBase(DatabaseFixture fx, ITestOutputHelper output) : PenTestBase(fx, output)
{
    protected static readonly TimeSpan BlindBound = TimeSpan.FromSeconds(6);

    protected QueryCaseSeeder Seeder { get; } = new(fx);

    protected string UserA => Fx.Seed.UserWithPermission;
    protected string UserB => Fx.Seed.UserTenantB;

    public override async Task DisposeAsync()
    {
        await Seeder.CleanupAsync();
        await base.DisposeAsync();
    }

    protected static string Q(string value) => Uri.EscapeDataString(value);

    protected static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);

    protected static List<JsonElement>? ArrayOf(PenTestResponse response)
    {
        try
        {
            using var document = JsonDocument.Parse(response.Body);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? [.. document.RootElement.EnumerateArray().Select(e => e.Clone())]
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    protected static bool TryGet(JsonElement element, string property, out JsonElement value)
    {
        foreach (var candidate in element.EnumerateObject())
        {
            if (!string.Equals(candidate.Name, property, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = candidate.Value;
            return true;
        }

        value = default;
        return false;
    }

    protected static List<int> IntsOf(PenTestResponse response, string property)
    {
        var rows = ArrayOf(response) ?? [];
        return
        [
            .. rows.Where(r => r.ValueKind == JsonValueKind.Object && TryGet(r, property, out _))
                .Select(r =>
                {
                    TryGet(r, property, out var v);
                    return v.GetInt32();
                })
        ];
    }

    protected static IReadOnlyList<string> HostileSeeds { get; } =
    [
        "' OR '1'='1", "' OR 1=1--", "\" OR \"\"=\"", "'; DROP TABLE \"Case\";--", "' UNION SELECT NULL,NULL,NULL--",
        "' UNION ALL SELECT \"Password\",NULL FROM \"UserRegistry\"--", "'; SELECT pg_sleep(8);--",
        "' AND (SELECT 1 FROM pg_sleep(8))--", "1; SELECT pg_sleep(8)", "' OR (SELECT COUNT(*) FROM \"Case\")>0--",
        "\\'; SELECT 1;--", "$$; SELECT pg_sleep(8); $$", "E'\\x27 OR 1=1--", "' || (SELECT current_user) || '",
        "' OR TRUE --", "') OR ('1'='1", "%", "_", "%_%", "%%", "%25", "a%", "%a", "_%_", ".*", "^.*$", "(?i).*",
        "[a-z]+", "(a+)+$", "\\", "\\\\", "'", "''", "\"", "`", ";", "--", "/*", "*/", "{{7*7}}", "${7*7}", "#{7*7}",
        "<%= 7*7 %>", "<script>alert(1)</script>", "<img src=x onerror=alert(1)>", "javascript:alert(1)",
        "../../etc/passwd", "..\\..\\windows\\win.ini", "$(id)", "`id`", "; id", "| id", "&& id",
        "\r\nX-Injected: 1", "%0d%0aX-Injected: 1", "*)(uid=*))(|(uid=*", "//*", "' or ''='", "{\"$ne\":null}",
        "[$ne]=1", "%s%s%s%n", "{0}{1}", "‮evil", "İ", "ǅ", "＇ OR 1=1--", "\u0000", "\u0001\u001F",
        "true", "false", "null", "undefined", "NaN", "0", "-1", "2147483648", "1e999", "0x41", "\t", " ", "  x  ",
        "😀", "K", "é", "AAAA\u0000BBBB"
    ];

    protected static IEnumerable<string> Mutate(string seed)
    {
        yield return seed;
        yield return string.Concat(seed.Select((c, i) =>
            i % 2 == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c)));
        yield return seed.Replace(" ", "/**/");
        yield return seed.Replace(" ", "\t");
        yield return $" {seed} ";
        yield return seed + "--";
        yield return "x'" + seed;
        yield return Uri.EscapeDataString(seed);
        yield return new string(seed.Select(c => c is >= '!' and <= '~' ? (char)(c + 0xFEE0) : c).ToArray());
    }

    protected static IEnumerable<string> HostileValues()
    {
        return HostileSeeds.SelectMany(Mutate).Distinct();
    }

    protected async Task AuthMatrixAsync(PenTestRun run, string method, string target, byte[]? body,
        string? contentType, Func<PenTestResponse, bool> authorisedAcceptable,
        Func<PenTestResponse, bool> foreignAcceptable, params string[] markers)
    {
        var secretMarkers = markers.Where(m => m.Length > 0).ToArray();

        async Task<PenTestResponse> SendAsync(PenTestClient client) =>
            run.Check(await client.SendAsync(method, target, body, contentType));

        var anonymous = await SendAsync(Anonymous);
        run.Expect(anonymous.IsRejection, anonymous, "ANON-NOT-REJECTED");

        foreach (var token in new[] { "garbage", "a.b.c", "Bearer", "eyJhbGciOiJub25lIn0.eyJzdWIiOiJhZG1pbiJ9.", "" })
        {
            var response = await SendAsync(Anonymous.WithBearer(token));
            run.Expect(response.IsRejection, response, "GARBAGE-BEARER-NOT-REJECTED");
        }

        foreach (var user in new[] { PenTestUser.WithoutPermission, PenTestUser.NoTenant })
        {
            var response = await SendAsync(As(user));
            run.Expect(response.IsRejection, response, "BFLA", $"{user} must be refused (403)");
            foreach (var marker in secretMarkers)
            {
                run.Expect(!response.Body.Contains(marker, StringComparison.OrdinalIgnoreCase), response, "LEAK-MARKER",
                    $"{user} body contains {marker}");
            }
        }

        var landlord = await SendAsync(As(PenTestUser.Landlord));
        run.Expect(foreignAcceptable(landlord), landlord, "LANDLORD-BOLA", "landlord obtained tenant data");
        foreach (var marker in secretMarkers)
        {
            run.Expect(!landlord.Body.Contains(marker, StringComparison.OrdinalIgnoreCase), landlord, "LANDLORD-LEAK",
                $"landlord body contains {marker}");
        }

        var foreign = await SendAsync(As(PenTestUser.TenantB));
        run.Expect(foreignAcceptable(foreign), foreign, "BOLA", "tenant B response not the not-found equivalent");
        foreach (var marker in secretMarkers)
        {
            run.Expect(!foreign.Body.Contains(marker, StringComparison.OrdinalIgnoreCase), foreign, "BOLA-LEAK",
                $"tenant B body contains {marker}");
        }

        var authorised = await SendAsync(As(PenTestUser.WithPermission));
        run.Expect(authorisedAcceptable(authorised), authorised, "AUTHORISED-UNEXPECTED");
        var bearer = await SendAsync(As(PenTestUser.WithPermission).AsBearer());
        run.Expect(authorisedAcceptable(bearer), bearer, "AUTHORISED-BEARER-UNEXPECTED");
    }

    protected static async Task VerbTamperingAsync(PenTestRun run, PenTestClient client, string target,
        params string[] forbiddenMethods)
    {
        foreach (var method in forbiddenMethods)
        {
            var response = run.Check(await client.SendAsync(method, target, method == "GET" ? null : Utf8("{}"),
                method == "GET" ? null : "application/json"));
            run.Expect(!response.IsSuccess, response, "VERB-SUCCEEDED", method);
        }
    }
}