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
using System.Threading.Tasks;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Security.QueryCase;

public abstract class KeyValueCaseRouteTests(DatabaseFixture fx, ITestOutputHelper output)
    : QueryCaseTestBase(fx, output)
{
    private static readonly string[] forbiddenNames =
        ["tenantregistryid", "password", "hash", "salt", "secret", "token", "connection", "apikey", "deleted"];

    protected abstract string Path { get; }
    protected abstract string CaseIdProperty { get; }
    protected abstract string[] AllowedProperties { get; }

    protected abstract Task SeedRowsAsync(CaseGraph graph);

    protected virtual Task<IReadOnlyList<CaseGraph>> ExtraInvisibleVariantsAsync(string key) =>
        Task.FromResult<IReadOnlyList<CaseGraph>>([]);

    private string Target(string key, string value) => $"{Path}?key={Q(key)}&value={Q(value)}";

    private async Task<CaseGraph> GraphAsync(string owner, string key, string? value = null,
        string[]? workflowRoles = null, string[]? statusRoles = null, bool workflowDeleted = false,
        bool statusDeleted = false, bool modelDeleted = false)
    {
        var graph = await Seeder.CaseAsync(owner, key, value, workflowRoles: workflowRoles, statusRoles: statusRoles,
            workflowDeleted: workflowDeleted, statusDeleted: statusDeleted, modelDeleted: modelDeleted);
        await SeedRowsAsync(graph);
        return graph;
    }

    private static bool Empty(PenTestResponse response) =>
        response.Status == 200 && ArrayOf(response) is { Count: 0 };

    [Fact]
    public async Task AuthenticationAndAuthorisationMatrixAsync()
    {
        var run = NewRun($"{GetType().Name}.Matrix");
        var key = QueryCaseSeeder.Unique("Key");
        var graph = await GraphAsync(UserA, key);
        var target = Target(key, graph.Value);

        await AuthMatrixAsync(run, "GET", target, null, null,
            r => r.Status == 200 && IntsOf(r, CaseIdProperty).Contains(graph.CaseId),
            Empty, graph.Value);

        run.AssertClean(Output);
    }

    [Fact]
    public async Task ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync()
    {
        var run = NewRun($"{GetType().Name}.Exact");
        var key = QueryCaseSeeder.Unique("Key");
        var value = QueryCaseSeeder.Unique("Val");
        var a = await GraphAsync(UserA, key, value);
        var b = await GraphAsync(UserB, key, value);

        var asA = run.Check(await As(PenTestUser.WithPermission).GetAsync(Target(key, value)));
        run.Expect(asA.Status == 200 && IntsOf(asA, CaseIdProperty).Distinct().SequenceEqual([a.CaseId]), asA,
            "TENANT-A-VIEW", "tenant A must see exactly its own case");
        var asB = run.Check(await As(PenTestUser.TenantB).GetAsync(Target(key, value)));
        run.Expect(asB.Status == 200 && IntsOf(asB, CaseIdProperty).Distinct().SequenceEqual([b.CaseId]), asB,
            "TENANT-B-VIEW", "tenant B must see exactly its own case");

        var nearMisses = new List<(string Key, string Value)>
        {
            (key, value.ToUpperInvariant()), (key, value.ToLowerInvariant()), (key, value + " "), (key, " " + value),
            (key, value[..^1]), (key, value + "%"), (key, "%" + value), (key, value.Replace('Z', '_')),
            (key, value.Replace("Test", "%")), (key, value + "\t"), (key.ToLowerInvariant(), value),
            (key + " ", value), (key, value + "​"), (key, string.Concat(value.Select(c => c + "́"))),
            (key, value + "'"), (key, value + "' OR '1'='1"), ("%", value), ("_", value), (key, "%"), (key, "_"),
            (key, ".*"), (key, "^" + value + "$"), (key, "(?i)" + value), (key, value + "\\"),
            (key, new string('a', 4000))
        };
        await run.ForEachAsync(nearMisses, async pair =>
        {
            foreach (var user in new[] { PenTestUser.WithPermission, PenTestUser.TenantB })
            {
                var response = run.Check(await As(user).GetAsync(Target(pair.Key, pair.Value)));
                run.Expect(Empty(response) || response.Status is 400 or 414 or 431, response, "NOT-EXACT",
                    $"{user}: near miss key='{pair.Key[..Math.Min(20, pair.Key.Length)]}' matched");
            }
        });

        run.AssertClean(Output);
    }

    [Fact]
    public async Task CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync()
    {
        var run = NewRun($"{GetType().Name}.Visibility");
        var key = QueryCaseSeeder.Unique("Key");
        var visible = new List<CaseGraph>
        {
            await GraphAsync(UserA, key),
            await GraphAsync(UserA, key, workflowRoles: [UserA, UserB], statusRoles: [UserA, UserB]),
            await GraphAsync(UserA, key, workflowRoles: [UserA, Fx.Seed.UserWithoutPermission],
                statusRoles: [UserA, Fx.Seed.UserWithoutPermission])
        };
        var invisible = new List<CaseGraph>
        {
            await GraphAsync(UserA, key, statusRoles: []),
            await GraphAsync(UserA, key, workflowRoles: []),
            await GraphAsync(UserA, key, workflowRoles: [], statusRoles: []),
            await GraphAsync(UserA, key, workflowRoles: [UserB], statusRoles: [UserB]),
            await GraphAsync(UserA, key, workflowRoles: [UserA], statusRoles: [UserB]),
            await GraphAsync(UserA, key, workflowRoles: [UserB], statusRoles: [UserA]),
            await GraphAsync(UserA, key, workflowDeleted: true),
            await GraphAsync(UserA, key, statusDeleted: true),
            await GraphAsync(UserA, key, modelDeleted: true)
        };
        invisible.AddRange(await ExtraInvisibleVariantsAsync(key));

        foreach (var graph in visible)
        {
            var response = run.Check(await As(PenTestUser.WithPermission).GetAsync(Target(key, graph.Value)));
            run.Expect(IntsOf(response, CaseIdProperty).Contains(graph.CaseId), response, "VISIBLE-MISSING",
                "case with workflow and status role must be returned");
        }

        foreach (var graph in invisible)
        {
            foreach (var user in new[]
                     {
                         PenTestUser.WithPermission, PenTestUser.TenantB, PenTestUser.BothTenants, PenTestUser.Landlord
                     })
            {
                var response = run.Check(await As(user).GetAsync(Target(key, graph.Value)));
                run.Expect(!IntsOf(response, CaseIdProperty).Contains(graph.CaseId), response, "VISIBILITY-BYPASS",
                    $"{user} obtained a case it may not see (workflow/status/action role, deletion or tenant)");
            }
        }

        foreach (var graph in visible.Concat(invisible))
        {
            foreach (var user in new[] { PenTestUser.WithoutPermission, PenTestUser.NoTenant })
            {
                var response = run.Check(await As(user).GetAsync(Target(key, graph.Value)));
                run.Expect(response.IsRejection, response, "BFLA", $"{user} must be refused even with roles granted");
            }

            foreach (var stranger in new[] { PenTestUser.TenantB, PenTestUser.Landlord })
            {
                var foreignForVisible = run.Check(await As(stranger).GetAsync(Target(key, graph.Value)));
                run.Expect(!IntsOf(foreignForVisible, CaseIdProperty).Contains(graph.CaseId), foreignForVisible,
                    "BOLA", $"{stranger} obtained a tenant A case even where its role was granted the workflow");
            }
        }

        run.AssertClean(Output);
    }

    [Fact]
    public async Task InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync()
    {
        var run = NewRun($"{GetType().Name}.Injection");
        var key = QueryCaseSeeder.Unique("Key");
        var graph = await GraphAsync(UserA, key);
        var client = As(PenTestUser.WithPermission);
        var values = HostileValues().ToList();

        async Task ProbeAsync(string probeKey, string probeValue, string what)
        {
            var response = run.Check(await client.GetAsync(Target(probeKey, probeValue)), BlindBound);
            var ok = Empty(response) || response.Status is 400 or 414 or 431 or 404;
            run.Expect(ok, response, "INJECTION", $"{what}: expected [] (exact match), got {response.Status}");
        }

        await run.ForEachAsync(values, v => ProbeAsync(key, v, "value"));
        await run.ForEachAsync(HostileSeeds, v => ProbeAsync(v, graph.Value, "key"));
        await run.ForEachAsync(HostileSeeds.Take(40), v => ProbeAsync(v, v, "key+value"));
        await run.ForEachAsync(HostileSeeds.Take(40), v => ProbeAsync(key + v, graph.Value + v, "concatenated"));

        var still = run.Check(await client.GetAsync(Target(key, graph.Value)));
        run.Expect(IntsOf(still, CaseIdProperty).Contains(graph.CaseId), still, "STATE-DAMAGED",
            "the legitimate lookup must be unaffected by the injection attempts");

        run.AssertClean(Output);
    }

    [Fact]
    public async Task ParameterTypeConfusionAndBoundariesAsync()
    {
        var run = NewRun($"{GetType().Name}.Boundaries");
        var key = QueryCaseSeeder.Unique("Key");
        var nfc = "café" + Guid.NewGuid().ToString("N")[..8];
        var graph = await GraphAsync(UserA, key, nfc);
        var known = new HashSet<int> { graph.CaseId };
        var client = As(PenTestUser.WithPermission);

        var targets = new List<string>
        {
            Path, $"{Path}?key={Q(key)}", $"{Path}?value={Q(graph.Value)}", $"{Path}?key=&value=",
            $"{Path}?key={Q(key)}&key={Q("x")}&value={Q(graph.Value)}",
            $"{Path}?key={Q(key)}&value={Q(graph.Value)}&value={Q("y")}",
            $"{Path}?key[]={Q(key)}&value[]={Q(graph.Value)}", $"{Path}?key[0]={Q(key)}&value={Q(graph.Value)}",
            $"{Path}?key={Q(key)}&value=%00", $"{Path}?key=%00&value={Q(graph.Value)}",
            $"{Path}?key={Q(key)}&value=%ff%fe%fd", $"{Path}?key=%c0%af&value=%c0%ae",
            $"{Path}?key={Q(key)}&value=%EF%BB%BF{Q(graph.Value)}",
            $"{Path}?key={Q(key)}&value={Q(graph.Value.Normalize(NormalizationForm.FormD))}",
            $"{Path}?key={Q(key)}&value={Q(new string('A', 8000))}",
            $"{Path}?key={Q(new string('A', 8000))}&value={Q(graph.Value)}",
            $"{Path}?key={Q(key)}&value={Q(new string('A', 100_000))}",
            $"{Path}?key={Q(key)}&value={Q("😀😀")}",
            $"{Path}?key={Q(key)}&value=%ED%A0%80",
            $"{Path}?KEY={Q(key)}&VALUE={Q(graph.Value)}",
            $"{Path}?key={Q(key)}&value={Q(graph.Value)}&limit=999999999&offset=-1&page=-5&pageSize=0&take=-1&skip=x",
            $"{Path}?key={Q(key)}&value={Q(graph.Value)}&orderBy=id;drop table x&sort=desc%00",
            $"{Path}?key={Q(key)}&value={Q(graph.Value)}&tenantRegistryId=1&TenantRegistryId=2&userName=admin"
        };
        for (var i = 0; i < 200; i++)
        {
            targets.Add($"{Path}?key={Q(key)}&value={Q(graph.Value)}&p{i}={i}");
        }

        await run.ForEachAsync(targets, async target =>
        {
            var response = run.Check(await client.GetAsync(target), BlindBound);
            if (response.Status == 200)
            {
                var ids = IntsOf(response, CaseIdProperty);
                run.Expect(ids.All(known.Contains), response, "FOREIGN-ROWS", "returned rows outside the seeded set");
            }
            else
            {
                run.Expect(response.Status is 400 or 404 or 414 or 431, response, "STATUS", "unexpected status");
            }
        });

        var normalisation = run.Check(await client.GetAsync(
            Target(key, graph.Value.Normalize(NormalizationForm.FormD))));
        run.Expect(Empty(normalisation), normalisation, "NORMALISATION",
            "decomposed and composed forms must not be conflated");

        foreach (var headers in new[]
                 {
                     ("Accept", "text/csv"), ("Accept", "application/xml"), ("Accept", "*/*;q=0"),
                     ("Accept-Language", "zz-ZZ"), ("X-Forwarded-For", "127.0.0.1"),
                     ("X-Original-URL", "/api/admin"), ("Content-Type", "application/json")
                 })
        {
            var response = run.Check(await client.GetAsync(Target(key, graph.Value), headers));
            run.Expect(response.Status is 200 or 406, response, "HEADER-STATUS", headers.Item1);
        }

        run.AssertClean(Output);
    }

    [Fact]
    public async Task ResponseExposesOnlyTheDocumentedPropertiesAsync()
    {
        var run = NewRun($"{GetType().Name}.Shape");
        var key = QueryCaseSeeder.Unique("Key");
        var graph = await GraphAsync(UserA, key);
        var response = run.Check(await As(PenTestUser.WithPermission).GetAsync(Target(key, graph.Value)));
        var rows = ArrayOf(response);
        run.Expect(rows is { Count: > 0 }, response, "NO-ROWS", "seeded row not returned");
        var allowed = AllowedProperties.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows ?? [])
        {
            foreach (var property in row.EnumerateObject())
            {
                run.Expect(allowed.Contains(property.Name), response, "EXTRA-PROPERTY", property.Name);
                run.Expect(!forbiddenNames.Any(f => property.Name.Contains(f, StringComparison.OrdinalIgnoreCase)),
                    response, "SENSITIVE-PROPERTY", property.Name);
            }
        }

        run.Expect(response.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true, response,
            "CONTENT-TYPE", response.ContentType ?? "none");
        run.Expect(response.Header("X-Content-Type-Options") != null, response, "NOSNIFF");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync()
    {
        var run = NewRun($"{GetType().Name}.Verbs");
        var key = QueryCaseSeeder.Unique("Key");
        var graph = await GraphAsync(UserA, key);
        var target = Target(key, graph.Value);
        var client = As(PenTestUser.WithPermission);
        var before = run.Check(await client.GetAsync(target));

        await VerbTamperingAsync(run, client, target, "POST", "PUT", "PATCH", "DELETE", "TRACE");
        foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE" })
        {
            var response = run.Check(await client.SendAsync(method, target,
                Utf8($"{{\"id\":{graph.CaseId},\"caseId\":{graph.CaseId},\"deleted\":1,\"caseKeyValue\":\"x\"}}"),
                "application/json", [("X-HTTP-Method-Override", "GET")]));
            run.Expect(!response.IsSuccess, response, "OVERRIDE-SUCCEEDED", method);
        }

        var after = run.Check(await client.GetAsync(target));
        run.Expect(before.Body == after.Body && after.Status == 200, after, "ROWSET-CHANGED");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task ReplayAndConcurrentRequestsAreConsistentAsync()
    {
        var run = NewRun($"{GetType().Name}.Replay");
        var key = QueryCaseSeeder.Unique("Key");
        var graph = await GraphAsync(UserA, key);
        var foreignGraph = await GraphAsync(UserB, key, graph.Value);
        var client = As(PenTestUser.WithPermission);
        var baseline = run.Check(await client.GetAsync(Target(key, graph.Value)));

        await run.ForEachAsync(Enumerable.Range(0, 60), async i =>
        {
            var user = i % 2 == 0 ? PenTestUser.WithPermission : PenTestUser.TenantB;
            var response = run.Check(await As(user).GetAsync(Target(key, graph.Value)));
            var ids = IntsOf(response, CaseIdProperty).Distinct().ToList();
            var expected = user == PenTestUser.WithPermission ? graph.CaseId : foreignGraph.CaseId;
            run.Expect(response.Status == 200 && ids.SequenceEqual([expected]), response, "INCONSISTENT",
                $"{user} saw {string.Join(",", ids)}");
            if (user == PenTestUser.WithPermission)
            {
                run.Expect(response.Body == baseline.Body, response, "BODY-DRIFT");
            }
        }, 12);

        run.AssertClean(Output);
    }
}