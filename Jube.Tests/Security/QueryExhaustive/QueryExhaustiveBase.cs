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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using Xunit;
using Xunit.Abstractions;
using Jube.Test.Security.QueryExhaustive.Models;

namespace Jube.Test.Security.QueryExhaustive;

public abstract partial class QueryExhaustiveBase(DatabaseFixture fx, ITestOutputHelper output)
    : PenTestBase(fx, output)
{
    private const int NonExistentId = 2147479999;
    private const int MaxBodyBytes = 1024 * 1024;

    private QueryExhaustiveWorld? worldOrNull;

    protected QueryExhaustiveWorld World
    {
        get => worldOrNull.Required();
        private set => worldOrNull = value;
    }

    protected abstract string RouteName { get; }

    protected abstract int IdOf(Set set);

    protected virtual string Fingerprint(Set set)
    {
        return set.K.ToString("0.##", CultureInfo.InvariantCulture);
    }

    protected virtual string[] AbsentMarkers =>
    [
        QueryExhaustiveWorld.DeletedMarker.ToString(CultureInfo.InvariantCulture),
        QueryExhaustiveWorld.DeletedVariableMarker
    ];

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        World = await QueryExhaustiveWorld.CreateAsync(Fx);
    }

    public override async Task DisposeAsync()
    {
        if (worldOrNull != null)
        {
            await worldOrNull.DisposeAsync(Fx);
        }
    }

    protected string PathFor(int id) => $"/api/{RouteName}/{id.ToString(CultureInfo.InvariantCulture)}";

    protected Task<PenTestResponse> GetAsAsync(PenTestUser user, int id) => As(user).GetAsync(PathFor(id));

    private static bool IsValidJson(string body)
    {
        try
        {
            using var _ = JsonDocument.Parse(body);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool Same(PenTestResponse a, PenTestResponse b) => a.Status == b.Status && a.Body == b.Body;

    private static readonly Regex nonFiniteToken = MyNonFiniteRegex();

    [GeneratedRegex("NaN|Infinity", RegexOptions.CultureInvariant)]
    private static partial Regex MyNonFiniteRegex();

    private void CheckWellFormed(PenTestRun run, PenTestResponse response, string rule)
    {
        run.Expect(response.Status == 200, response, rule + "-STATUS", "expected 200");
        run.Expect(IsValidJson(response.Body), response, rule + "-JSON", "body is not valid JSON");
        run.Expect(!nonFiniteToken.IsMatch(response.Body), response, rule + "-NONFINITE",
            "NaN/Infinity leaked into the payload");
        run.Expect(response.BodyBytes.Length < MaxBodyBytes, response, rule + "-SIZE",
            $"{response.BodyBytes.Length} bytes");
        run.Expect((response.ContentType ?? string.Empty).Contains("json", StringComparison.OrdinalIgnoreCase),
            response, rule + "-CONTENT-TYPE", response.ContentType ?? "none");
    }

    [Fact]
    public async Task AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync()
    {
        var run = NewRun($"{RouteName} own data");
        foreach (var (user, set, other) in new[]
                 {
                     (PenTestUser.WithPermission, World.A, World.B), (PenTestUser.TenantB, World.B, World.A)
                 })
        {
            var response = run.Check(await GetAsAsync(user, IdOf(set)));
            CheckWellFormed(run, response, "OWN");
            run.Expect(response.Body.Contains(Fingerprint(set), StringComparison.Ordinal), response, "OWN-DATA",
                $"{user} did not receive its own data ({Fingerprint(set)})");
            run.Expect(!response.Body.Contains(Fingerprint(other), StringComparison.Ordinal) ||
                       Fingerprint(other) == Fingerprint(set), response, "OWN-FOREIGN-DATA",
                $"{user} received the other tenant's data ({Fingerprint(other)})");
            foreach (var marker in AbsentMarkers)
            {
                run.Expect(!response.Body.Contains(marker, StringComparison.Ordinal), response, "OWN-SOFT-DELETED",
                    $"soft-deleted/removed marker {marker} returned");
            }

            run.Expect(string.Equals(response.Header("X-Content-Type-Options"), "nosniff",
                StringComparison.OrdinalIgnoreCase), response, "NOSNIFF", "missing X-Content-Type-Options: nosniff");
        }

        run.AssertClean(Output);
    }

    [Fact]
    public async Task AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync()
    {
        var run = NewRun($"{RouteName} authn/authz");
        var id = IdOf(World.A);
        var fingerprintA = Fingerprint(World.A);

        var refused = new List<(string Who, PenTestClient Client)>
        {
            ("anonymous", Anonymous),
            ("garbage bearer", Anonymous.WithBearer("garbage")),
            ("empty bearer", Anonymous.WithBearer(string.Empty)),
            ("jwt-shaped garbage bearer", Anonymous.WithBearer("eyJhbGciOiJIUzI1NiJ9.e30.AAAA")),
            ("alg none bearer", Anonymous.WithBearer("eyJhbGciOiJub25lIn0.eyJ1bmlxdWVfbmFtZSI6ImFkbWluIn0.")),
            ("garbage cookie", Anonymous.WithCookie("authentication-jwt=garbage")),
            ("huge bearer", Anonymous.WithBearer(new string('A', 20000))),
            ("without permission", As(PenTestUser.WithoutPermission)),
            ("no tenant", As(PenTestUser.NoTenant)),
            ("unknown user", As(PenTestUser.UnknownUser))
        };

        await run.ForEachAsync(refused, async item =>
        {
            var response = run.Check(await item.Client.GetAsync(PathFor(id)));
            run.Expect(response.IsRejection || response.Status == 403, response, "AUTHZ-NOT-REFUSED",
                $"{item.Who} was not refused");
            run.Expect(!response.Body.Contains(fingerprintA, StringComparison.Ordinal), response, "AUTHZ-LEAK",
                $"{item.Who} saw tenant data");
        });

        foreach (var user in new[] { PenTestUser.Landlord, PenTestUser.BothTenants, PenTestUser.NoApproveByReview })
        {
            var response = run.Check(await GetAsAsync(user, id));
            run.Expect(!response.IsServerError, response, "AUTHZ-5XX", user.ToString());
        }

        var bearer = run.Check(await As(PenTestUser.WithPermission).AsBearer().GetAsync(PathFor(id)));
        run.Expect(bearer.Status == 200, bearer, "BEARER-AUTHORISED", "valid bearer token should be accepted");

        run.AssertClean(Output);
    }

    [Fact]
    public async Task CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync()
    {
        var run = NewRun($"{RouteName} cross tenant");
        var baselineA = run.Check(await GetAsAsync(PenTestUser.WithPermission, NonExistentId));
        var baselineB = run.Check(await GetAsAsync(PenTestUser.TenantB, NonExistentId));
        CheckWellFormed(run, baselineA, "BASELINE-A");
        CheckWellFormed(run, baselineB, "BASELINE-B");
        run.Expect(Same(baselineA, baselineB), baselineB, "BASELINE-DIFFERS", "non-existent id differs per tenant");

        var probes = new List<(PenTestUser User, string What, int Id, PenTestResponse Baseline, string Forbidden)>();
        foreach (var set in new[] { World.A, World.Edge, World.Deleted, World.Empty })
        {
            probes.Add((PenTestUser.TenantB, $"tenant B reads tenant A set {set.Tag}", IdOf(set), baselineB,
                Fingerprint(World.A)));
        }

        probes.Add((PenTestUser.WithPermission, "tenant A reads tenant B set", IdOf(World.B), baselineA,
            Fingerprint(World.B)));

        await run.ForEachAsync(probes, async probe =>
        {
            var response = run.Check(await GetAsAsync(probe.User, probe.Id));
            run.Expect(Same(response, probe.Baseline), response, "IDOR-DISTINGUISHABLE",
                $"{probe.What}: response differs from a non-existent id ({probe.Baseline.Status})");
            run.Expect(!response.Body.Contains(probe.Forbidden, StringComparison.Ordinal), response, "IDOR-LEAK",
                probe.What);
        });

        var foreign = new List<double>();
        var missing = new List<double>();
        for (var i = 0; i < 7; i++)
        {
            foreign.Add((await GetAsAsync(PenTestUser.TenantB, IdOf(World.A))).Elapsed.TotalMilliseconds);
            missing.Add((await GetAsAsync(PenTestUser.TenantB, NonExistentId - i)).Elapsed.TotalMilliseconds);
        }

        var delta = Math.Abs(foreign.Order().ElementAt(3) - missing.Order().ElementAt(3));
        run.Expect(delta < 1500, baselineB, "IDOR-TIMING",
            $"median difference {delta:F0}ms between foreign and missing");

        run.AssertClean(Output);
    }

    [Fact]
    public async Task IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync()
    {
        var run = NewRun($"{RouteName} enumeration");
        var ids = World.All.Select(IdOf).Where(i => i > 0).Distinct().ToList();
        var candidates = ids.SelectMany(i => Enumerable.Range(-6, 13).Select(d => i + d)).Where(i => i > 0)
            .Distinct().ToList();

        await run.ForEachAsync([PenTestUser.WithPermission, PenTestUser.TenantB], async user =>
        {
            var forbidden = Fingerprint(user == PenTestUser.WithPermission ? World.B : World.A);
            var own = Fingerprint(user == PenTestUser.WithPermission ? World.A : World.B);
            await run.ForEachAsync(candidates, async id =>
            {
                var response = run.Check(await GetAsAsync(user, id));
                run.Expect(response.Status == 200, response, "ENUM-STATUS-NOT-UNIFORM", $"id {id}");
                run.Expect(!response.Body.Contains(forbidden, StringComparison.Ordinal) ||
                           forbidden == own, response, "ENUM-FOREIGN-DATA",
                    $"id {id} returned the other tenant's data");
            });
        }, 2);

        run.AssertClean(Output);
    }

    [Fact]
    public async Task SoftDeleted_RowsAndInstancesAreNeverReturnedAsync()
    {
        var run = NewRun($"{RouteName} soft deleted");
        var baseline = run.Check(await GetAsAsync(PenTestUser.WithPermission, NonExistentId));
        var own = run.Check(await GetAsAsync(PenTestUser.WithPermission, IdOf(World.A)));
        foreach (var marker in AbsentMarkers)
        {
            run.Expect(!own.Body.Contains(marker, StringComparison.Ordinal), own, "SOFT-DELETED-ROW",
                $"marker {marker} returned for own instance");
        }

        var deleted = run.Check(await GetAsAsync(PenTestUser.WithPermission, IdOf(World.Deleted)));
        run.Expect(Same(deleted, baseline), deleted, "SOFT-DELETED-INSTANCE",
            "a soft-deleted search instance still serves its data");

        run.AssertClean(Output);
    }

    [Fact]
    public async Task EmptyResult_IsAWellFormedEmptyDocumentAsync()
    {
        var run = NewRun($"{RouteName} empty");
        foreach (var id in new[] { IdOf(World.Empty), NonExistentId })
        {
            var response = run.Check(await GetAsAsync(PenTestUser.WithPermission, id));
            CheckWellFormed(run, response, "EMPTY");
            run.Expect(!response.Body.Contains(Fingerprint(World.A), StringComparison.Ordinal), response,
                "EMPTY-LEAK");
        }

        run.AssertClean(Output);
    }

    [Fact]
    public async Task NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync()
    {
        var run = NewRun($"{RouteName} numeric edge cases");
        var response = run.Check(await GetAsAsync(PenTestUser.WithPermission, IdOf(World.Edge)));
        CheckWellFormed(run, response, "EDGE");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task ConcurrentReads_AreDeterministicAndBoundedAsync()
    {
        var run = NewRun($"{RouteName} concurrency");
        var id = IdOf(World.A);
        var first = run.Check(await GetAsAsync(PenTestUser.WithPermission, id));
        var responses = new System.Collections.Concurrent.ConcurrentBag<PenTestResponse>();
        await run.ForEachAsync(Enumerable.Range(0, 48),
            async _ =>
            {
                responses.Add(run.Check(await GetAsAsync(PenTestUser.WithPermission, id), TimeSpan.FromSeconds(15)));
            }, 12);

        foreach (var response in responses)
        {
            run.Expect(Same(response, first), response, "CONCURRENT-DIFFERS", "body changed under parallel reads");
        }

        run.AssertClean(Output);
    }

    [Fact]
    public async Task IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync()
    {
        var run = NewRun($"{RouteName} id boundaries");
        var before = run.Check(await GetAsAsync(PenTestUser.WithPermission, IdOf(World.A)));
        var forbiddenA = Fingerprint(World.B);
        var raw = new List<string>
        {
            "0", "-0", "+0", "1", "-1", "+1", "01", "0001", "2147483647", "2147483648", "-2147483648", "-2147483649",
            "4294967295", "4294967296", "9223372036854775807", "9223372036854775808", "99999999999999999999999999",
            "1.0", "1.5", "1e3", "1E10", "0x10", "0b1", "1_000", "1,000", "NaN", "Infinity", "-Infinity", "null",
            "undefined", "true", "false", "[]", "{}", "[1]", "{%22id%22:1}", "%00", "1%00", "%00 1", "%0a", "%0d%0a",
            "1%0d%0aSet-Cookie:%20x=y", "%20", "%20%201", "1%20", "%09", "%C0%AF", "%EF%BB%BF1", "%E2%80%AE1",
            "١٢٣", "１２３", "①", "1.", ".1", "-.1", "1/", "1//", "1/../1", "..", "%2e%2e", "%2e%2e%2f1", "1;1",
            "1/**/", "1%23", "1%3F", "1%3Fid=2", "*", "%", "%%", "%zz", "\\1", "%5c1", "~1", "$1", "${1}", "{{1}}",
            "<1>", "1'", "1\"", "1`", "1)", "1(", "1&id=2", "1#", new string('9', 5000), new string('1', 300)
        };
        raw.AddRange(GenerateInjectionPayloads());

        var targets = new HashSet<string>();
        foreach (var value in raw)
        {
            var escaped = Uri.EscapeDataString(value);
            targets.Add($"/api/{RouteName}/{escaped}");
            targets.Add($"/api/{RouteName}/{Uri.EscapeDataString(escaped)}");
            if (value.Length < 200 && !value.Contains('/') && RawSafe().IsMatch(value))
            {
                targets.Add($"/api/{RouteName}/{value}");
            }
        }

        var user = As(PenTestUser.WithPermission);
        await run.ForEachAsync(targets, async target =>
        {
            var response = run.Check(await user.GetAsync(target), TimeSpan.FromSeconds(4));
            run.Expect(response.Status is 200 or 400 or 404 or 405 or 414 or 431, response, "BOUNDARY-STATUS",
                $"unexpected status {response.Status}");
            run.Expect(!response.Body.Contains(forbiddenA, StringComparison.Ordinal), response,
                "BOUNDARY-FOREIGN-DATA");
            if (response.Status == 200)
            {
                run.Expect(IsValidJson(response.Body), response, "BOUNDARY-JSON");
                run.Expect(response.BodyBytes.Length < MaxBodyBytes, response, "BOUNDARY-SIZE");
            }
        });

        var after = run.Check(await GetAsAsync(PenTestUser.WithPermission, IdOf(World.A)));
        run.Expect(Same(before, after), after, "BOUNDARY-SIDE-EFFECT", "own data changed after hostile ids");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task HostileQueryStringAndHeaders_NeverChangeTheResultAsync()
    {
        var run = NewRun($"{RouteName} query and headers");
        var id = IdOf(World.A);
        var baseline = run.Check(await GetAsAsync(PenTestUser.WithPermission, id));
        var foreignId = IdOf(World.B);

        var queries = new List<string>
        {
            $"?id={foreignId}", $"?Id={foreignId}", $"?tenantRegistryId={foreignId}", $"?TenantRegistryId=1",
            "?deleted=1", "?Deleted=true", "?limit=2147483647", "?limit=-1", "?top=0", "?page=999999999", "?skip=-5",
            "?take=99999999", "?$top=1", "?fields=*", "?expand=all", "?format=csv", "?callback=alert(1)",
            "?id=1&id=2&id=3", "?user=admin", "?userName=admin", "?%00=%00", "?a[]=1&a[]=2", "?__proto__[x]=1",
            "?q=%27%20OR%201%3D1--", "?sort=id;select%20pg_sleep(5)", "?x=" + new string('A', 8000),
            "?" + string.Join("&", Enumerable.Range(0, 300).Select(i => $"p{i}=v{i}")),
            "?token=eyJhbGciOiJub25lIn0.e30.", "?access_token=garbage", "?jwt=garbage", "#fragment", "?"
        };
        foreach (var q in queries.ToList())
        {
            queries.Add(q.Replace("=", "%3D", StringComparison.Ordinal));
        }

        await run.ForEachAsync(queries, async q =>
        {
            var response = run.Check(await As(PenTestUser.WithPermission).GetAsync(PathFor(id) + q),
                TimeSpan.FromSeconds(4));
            run.Expect(!response.IsServerError, response, "QUERY-5XX");
            if (response.IsSuccess)
            {
                run.Expect(Same(response, baseline), response, "QUERY-CHANGES-RESULT", q.Length > 60 ? q[..60] : q);
            }
        });

        var headers = new List<(string Name, string Value)>
        {
            ("X-Tenant-Id", "1"), ("X-Tenant", foreignId.ToString(CultureInfo.InvariantCulture)),
            ("X-Forwarded-For", "127.0.0.1, 10.0.0.1"), ("X-Forwarded-Host", "evil.example"),
            ("X-Forwarded-Proto", "https"), ("X-Original-URL", PathFor(foreignId)),
            ("X-Rewrite-URL", PathFor(foreignId)),
            ("X-HTTP-Method-Override", "DELETE"), ("X-Method-Override", "PUT"), ("X-User", "admin"),
            ("X-Remote-User", World.B.Tag), ("X-Requested-With", "XMLHttpRequest"), ("Accept", "text/csv"),
            ("Accept", "application/xml"), ("Accept", "*/*;q=0.1, text/html"), ("Accept-Language", "zz-<script>"),
            ("Accept-Encoding", "gzip, deflate, br, zstd, compress"), ("Range", "bytes=0-5"), ("Range", "bytes=-1"),
            ("If-None-Match", "*"), ("If-Modified-Since", "Sat, 01 Jan 2000 00:00:00 GMT"),
            ("Origin", "https://evil.example"), ("Referer", "https://evil.example/"), ("Sec-Fetch-Site", "same-origin"),
            ("Content-Type", "application/json"), ("Content-Type", "text/xml; charset=utf-7"),
            ("User-Agent", "' OR 1=1--"), ("User-Agent", "${jndi:ldap://x/a}"), ("Host", "evil.example"),
            ("X-Custom", new string('A', 7000)), ("Cookie", "authentication-jwt=garbage")
        };
        await run.ForEachAsync(headers, async header =>
        {
            var response = run.Check(await As(PenTestUser.WithPermission).SendAsync("GET", PathFor(id), null, null,
                [header], TimeSpan.FromSeconds(10)));
            run.Expect(!response.IsServerError, response, "HEADER-5XX", header.Name);
            var text = response.Body + string.Join("|", response.Headers.Select(h => h.Value));
            run.Expect(!text.Contains("evil.example", StringComparison.Ordinal), response, "HEADER-REFLECTED",
                header.Name);
            if (response.Status == 200 && header.Name is not ("Range" or "Cookie" or "Host"))
            {
                run.Expect(Same(response, baseline), response, "HEADER-CHANGES-RESULT", header.Name);
            }
        });

        run.AssertClean(Output);
    }

    [Fact]
    public async Task VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync()
    {
        var run = NewRun($"{RouteName} verbs");
        var id = IdOf(World.A);
        var before = run.Check(await GetAsAsync(PenTestUser.WithPermission, id));
        var bodies = new (string Method, byte[]? Body, string? ContentType)[]
        {
            ("POST", null, null), ("POST", Encoding.UTF8.GetBytes("{\"id\":1,\"deleted\":1}"), "application/json"),
            ("PUT", Encoding.UTF8.GetBytes("{\"Id\":1,\"Deleted\":1,\"Score\":0}"), "application/json"),
            ("PATCH", Encoding.UTF8.GetBytes("[{\"op\":\"remove\",\"path\":\"/\"}]"), "application/json-patch+json"),
            ("DELETE", null, null), ("TRACE", null, null), ("OPTIONS", null, null), ("HEAD", null, null),
            ("PROPFIND", null, null), ("LOCK", null, null), ("GET ", null, null),
            ("SEARCH", null, null), ("PURGE", null, null), ("MOVE", null, null), ("COPY", null, null)
        };
        foreach (var user in new[] { PenTestUser.WithPermission, PenTestUser.Anonymous })
        {
            await run.ForEachAsync(bodies, async b =>
            {
                var client = user == PenTestUser.Anonymous ? Anonymous : As(user);
                var response = run.Check(await client.SendAsync(b.Method, PathFor(id), b.Body, b.ContentType));
                run.Expect(!response.IsServerError, response, "VERB-5XX", b.Method);
                run.Expect(!response.IsSuccess || b.Method is "OPTIONS" or "HEAD", response, "VERB-SERVED",
                    $"{b.Method} was served");
            });
        }

        var after = run.Check(await GetAsAsync(PenTestUser.WithPermission, id));
        run.Expect(Same(before, after), after, "VERB-SIDE-EFFECT", "data changed after verb tampering");
        run.AssertClean(Output);
    }

    protected static IEnumerable<string> GenerateInjectionPayloads()
    {
        string[] leads = ["1", "-1", "0", "1'", "1\"", "1)", "1'))", "'", "\\"];
        string[] joins = [" OR ", " or ", "/**/OR/**/", "||", " AND ", " oR/*x*/", "%0aOR%0a"];
        string[] truths = ["1=1", "'a'='a'", "2>1", "true", "1 LIKE 1", "1=1 --"];
        string[] tails = ["--", "#", ";--", "/*", "-- -", ""];
        string[] stacked =
        [
            ";select pg_sleep(5)--", ";SELECT pg_sleep(5);--", "';select pg_sleep(5)--", ") union select null--",
            " UNION ALL SELECT NULL,NULL,NULL--", "; drop table \"User\"--", ";copy (select 1) to program 'id'--",
            "' AND (SELECT 1 FROM pg_sleep(5))--", "1 AND 1=(SELECT COUNT(*) FROM pg_tables)",
            "1;select version()", "1 AND CAST(version() AS int)=1", "$$;select 1;$$", "1' waitfor delay '0:0:5'--",
            "1;exec xp_cmdshell('id')", "1 OR SLEEP(5)", "1' OR extractvalue(1,concat(0x7e,version()))--",
            "{\"$ne\":null}", "1;db.dropDatabase()", "*)(uid=*))(|(uid=*", "1' or '1'='1", "//*", "' or 1]%00",
            "{{7*7}}", "${7*7}", "<%= 7*7 %>", "#{7*7}", "{{constructor.constructor('return 1')()}}",
            "<script>alert(1)</script>", "\"><img src=x onerror=alert(1)>", "javascript:alert(1)",
            "%0d%0aX-Injected: 1", "../../../../etc/passwd", "..%2f..%2fetc%2fpasswd", "%s%s%s%s%n", "{0}{1}",
            "=cmd|' /C calc'!A0", "@SUM(1+1)", "; ls", "| id", "`id`", "$(id)", "‮1", "\u0000", "﻿1"
        ];

        var seen = new HashSet<string>();
        foreach (var lead in leads)
        {
            foreach (var join in joins)
            {
                foreach (var truth in truths)
                {
                    foreach (var tail in tails)
                    {
                        var basic = lead + join + truth + tail;
                        foreach (var variant in Mutate(basic))
                        {
                            seen.Add(variant);
                        }
                    }
                }
            }
        }

        foreach (var lead in leads)
        {
            foreach (var payload in stacked)
            {
                foreach (var variant in Mutate(lead + payload))
                {
                    seen.Add(variant);
                }
            }
        }

        return
        [
            .. seen.OrderBy(s => s, StringComparer.Ordinal).Where((_, i) => i % 40 == 0),
            .. stacked
        ];
    }

    private static IEnumerable<string> Mutate(string s)
    {
        yield return s;
        yield return new string(s.Select((c, i) => i % 2 == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c))
            .ToArray());
        yield return s.Replace(" ", "/**/", StringComparison.Ordinal);
        yield return s.Replace(" ", "%09", StringComparison.Ordinal);
        yield return s.Replace(" ", " ", StringComparison.Ordinal);
        yield return s.Replace("'", "ʼ", StringComparison.Ordinal);
        yield return s.Replace("'", "%27", StringComparison.Ordinal);
    }

    [GeneratedRegex("^[A-Za-z0-9._~%+'\"(),;=*!$@:|-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex RawSafe();
}