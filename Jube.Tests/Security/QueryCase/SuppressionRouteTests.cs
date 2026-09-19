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
using System.Text.Json;
using System.Threading.Tasks;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using Xunit;
using Xunit.Abstractions;
using Jube.Test.Security.QueryCase.Models;

namespace Jube.Test.Security.QueryCase;

public abstract class SuppressionRouteTests(DatabaseFixture fx, ITestOutputHelper output)
    : QueryCaseTestBase(fx, output)
{
    protected abstract string Path { get; }
    protected abstract string[] AllowedProperties { get; }

    protected abstract string Target(string key, string value, Guid? modelGuid);

    protected abstract Task<Scenario> ScenarioAsync(string owner, string key, string value, bool suppressed,
        DateTime? expiry = null, bool suppressionDeleted = false, bool xpathSuppression = true,
        bool xpathDeleted = false, bool modelDeleted = false);

    protected static Guid? GuidOf(JsonElement row) =>
        TryGet(row, "entityAnalysisModelGuid", out var v) && v.TryGetGuid(out var g) ? g : null;

    protected static bool? FlagFor(PenTestResponse response, Guid modelGuid, string? ruleName = null)
    {
        foreach (var row in ArrayOf(response) ?? [])
        {
            if (GuidOf(row) != modelGuid)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(ruleName) && !(TryGet(row, "name", out var n) && n.GetString() == ruleName))
            {
                continue;
            }

            return TryGet(row, "suppression", out var flag) && flag.GetBoolean();
        }

        return null;
    }

    private static bool Empty(PenTestResponse response) =>
        response.Status == 200 && ArrayOf(response) is { Count: 0 };

    private string Key() => QueryCaseSeeder.Unique("Sup");

    [Fact]
    public async Task AuthenticationAndAuthorisationMatrixAsync()
    {
        var run = NewRun($"{GetType().Name}.Matrix");
        var key = Key();
        var scenario = await ScenarioAsync(UserA, key, "v1", true);

        await AuthMatrixAsync(run, "GET", Target(key, "v1", scenario.ModelGuid), null, null,
            r => r.Status == 200 &&
                 FlagFor(r, scenario.ModelGuid, scenario.RuleName == "" ? null : scenario.RuleName) == true,
            Empty, scenario.ModelGuid.ToString(), scenario.RuleName);
        run.AssertClean(Output);
    }

    [Fact]
    public async Task TenantIsolationForIdenticalKeysAndForeignModelGuidsAsync()
    {
        var run = NewRun($"{GetType().Name}.Isolation");
        var key = Key();
        var a = await ScenarioAsync(UserA, key, "v1", true);
        var b = await ScenarioAsync(UserB, key, "v1", true);

        var asA = run.Check(await As(PenTestUser.WithPermission).GetAsync(Target(key, "v1", a.ModelGuid)));
        run.Expect(FlagFor(asA, a.ModelGuid, NameOf(a)) == true && FlagFor(asA, b.ModelGuid) == null &&
                   !asA.Body.Contains(b.ModelGuid.ToString(), StringComparison.OrdinalIgnoreCase), asA,
            "A-VIEW", "tenant A must only see its own model");
        var asB = run.Check(await As(PenTestUser.TenantB).GetAsync(Target(key, "v1", b.ModelGuid)));
        run.Expect(FlagFor(asB, b.ModelGuid, NameOf(b)) == true && FlagFor(asB, a.ModelGuid) == null &&
                   !asB.Body.Contains(a.ModelGuid.ToString(), StringComparison.OrdinalIgnoreCase), asB,
            "B-VIEW", "tenant B must only see its own model");

        foreach (var (user, foreignGuid) in new[]
                 {
                     (PenTestUser.TenantB, a.ModelGuid), (PenTestUser.WithPermission, b.ModelGuid),
                     (PenTestUser.WithPermission, Guid.Empty), (PenTestUser.WithPermission, Guid.NewGuid()),
                     (PenTestUser.TenantB, Guid.Empty)
                 })
        {
            var response = run.Check(await As(user).GetAsync(Target(key, "v1", foreignGuid)));
            var foreignModel = user == PenTestUser.TenantB ? a.ModelGuid : b.ModelGuid;
            run.Expect(response.Status == 200 && (user == PenTestUser.TenantB
                    ? FlagFor(response, a.ModelGuid) == null
                    : FlagFor(response, b.ModelGuid) == null) && !response.Body.Contains(
                    foreignModel.ToString(), StringComparison.OrdinalIgnoreCase) || Empty(response),
                response, "FOREIGN-GUID", $"{user} with guid {foreignGuid}");
        }

        run.AssertClean(Output);
    }

    protected abstract string NameOf(Scenario scenario);

    [Fact]
    public async Task SuppressionStateRulesAreExactAndLiveOnlyAsync()
    {
        var run = NewRun($"{GetType().Name}.State");
        var key = Key();
        var active = await ScenarioAsync(UserA, key, "act", true);
        var future = await ScenarioAsync(UserA, key, "fut", true, DateTime.UtcNow.AddDays(1));
        var expired = await ScenarioAsync(UserA, key, "exp", true, DateTime.UtcNow.AddDays(-1));
        var deleted = await ScenarioAsync(UserA, key, "del", true, suppressionDeleted: true);
        var none = await ScenarioAsync(UserA, key, "none", false);
        var noXPathSuppression = await ScenarioAsync(UserA, QueryCaseSeeder.Unique("Sup"), "x", true,
            xpathSuppression: false);
        var xpathDeleted = await ScenarioAsync(UserA, QueryCaseSeeder.Unique("Sup"), "x", true, xpathDeleted: true);
        var modelDeleted = await ScenarioAsync(UserA, QueryCaseSeeder.Unique("Sup"), "x", true, modelDeleted: true);
        var client = As(PenTestUser.WithPermission);

        async Task<bool?> QueryAsync(Scenario s, string value)
        {
            var response = run.Check(await client.GetAsync(Target(s.Key, value, s.ModelGuid)));
            return FlagFor(response, s.ModelGuid, NameOf(s));
        }

        run.Expect(await QueryAsync(active, "act") == true, R(), "ACTIVE");
        run.Expect(await QueryAsync(future, "fut") == true, R(), "FUTURE-EXPIRY");
        run.Expect(await QueryAsync(expired, "exp") == false, R(), "EXPIRED-SUPPRESSION-STILL-ACTIVE");
        run.Expect(await QueryAsync(deleted, "del") == false, R(), "DELETED-SUPPRESSION-STILL-ACTIVE");
        run.Expect(await QueryAsync(none, "none") == false, R(), "NO-SUPPRESSION-ROW");
        run.Expect(await QueryAsync(active, "ACT") == false, R(), "CASE-INSENSITIVE-VALUE");
        run.Expect(await QueryAsync(active, "act ") == false, R(), "TRAILING-SPACE");
        run.Expect(await QueryAsync(active, "a%") == false, R(), "LIKE-WILDCARD");
        run.Expect(await QueryAsync(noXPathSuppression, "x") == null, R(), "XPATH-WITHOUT-SUPPRESSION-LISTED");
        run.Expect(await QueryAsync(xpathDeleted, "x") == null, R(), "DELETED-XPATH-LISTED");
        run.Expect(await QueryAsync(modelDeleted, "x") == null, R(), "DELETED-MODEL-LISTED");
        run.AssertClean(Output);
    }

    private static PenTestResponse R() => new() { Request = "state" };

    [Fact]
    public async Task InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync()
    {
        var run = NewRun($"{GetType().Name}.Injection");
        var key = Key();
        var scenario = await ScenarioAsync(UserA, key, "v1", true);
        var foreign = await ScenarioAsync(UserB, key, "v1", true);
        var client = As(PenTestUser.WithPermission);

        async Task ProbeAsync(string probeKey, string probeValue, bool keyIsHostile)
        {
            var response = run.Check(await client.GetAsync(Target(probeKey, probeValue, scenario.ModelGuid)),
                BlindBound);
            if (response.Status != 200)
            {
                run.Expect(response.Status is 400 or 404 or 414 or 431, response, "STATUS");
                return;
            }

            run.Expect(FlagFor(response, foreign.ModelGuid) == null, response, "FOREIGN-MODEL");
            if (keyIsHostile)
            {
                run.Expect(Empty(response), response, "HOSTILE-KEY-MATCHED", "an unknown key lists nothing");
            }
            else
            {
                run.Expect(FlagFor(response, scenario.ModelGuid, NameOf(scenario)) != true, response,
                    "HOSTILE-VALUE-SUPPRESSED", "a hostile value must never report an active suppression");
            }
        }

        await run.ForEachAsync(HostileValues(), v => ProbeAsync(key, v, false));
        await run.ForEachAsync(HostileSeeds, v => ProbeAsync(v, "v1", true));
        await run.ForEachAsync(HostileSeeds.Take(40), v => ProbeAsync(v, v, true));

        var still = run.Check(await client.GetAsync(Target(key, "v1", scenario.ModelGuid)));
        run.Expect(FlagFor(still, scenario.ModelGuid, NameOf(scenario)) == true, still, "STATE-DAMAGED");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task ParameterBoundariesAndTypeConfusionAsync()
    {
        var run = NewRun($"{GetType().Name}.Boundaries");
        var key = Key();
        var scenario = await ScenarioAsync(UserA, key, "v1", true);
        var foreign = await ScenarioAsync(UserB, key, "v1", true);
        var client = As(PenTestUser.WithPermission);
        var modelQ = scenario.ModelGuid.ToString();
        var targets = new List<string>
        {
            Path, $"{Path}?suppressionKey={Q(key)}", $"{Path}?suppressionKeyValue=v1",
            $"{Path}?suppressionKey=&suppressionKeyValue=", $"{Path}?entityAnalysisModelGuid={modelQ}",
            $"{Path}?entityAnalysisModelGuid=abc&suppressionKey={Q(key)}&suppressionKeyValue=v1",
            $"{Path}?entityAnalysisModelGuid=&suppressionKey={Q(key)}&suppressionKeyValue=v1",
            $"{Path}?entityAnalysisModelGuid=null&suppressionKey={Q(key)}&suppressionKeyValue=v1",
            $"{Path}?entityAnalysisModelGuid={modelQ}&entityAnalysisModelGuid={foreign.ModelGuid}&suppressionKey={Q(key)}&suppressionKeyValue=v1",
            $"{Path}?entityAnalysisModelGuid={modelQ.ToUpperInvariant()}&suppressionKey={Q(key)}&suppressionKeyValue=v1",
            $"{Path}?entityAnalysisModelGuid={{{modelQ}}}&suppressionKey={Q(key)}&suppressionKeyValue=v1",
            $"{Path}?entityAnalysisModelGuid={modelQ.Replace("-", "")}&suppressionKey={Q(key)}&suppressionKeyValue=v1",
            $"{Path}?entityAnalysisModelGuid={Q(modelQ + "' OR '1'='1")}&suppressionKey={Q(key)}&suppressionKeyValue=v1",
            $"{Path}?suppressionKey[]={Q(key)}&suppressionKeyValue[]=v1",
            $"{Path}?suppressionKey={Q(key)}&suppressionKey=x&suppressionKeyValue=v1&suppressionKeyValue=y",
            $"{Path}?suppressionKey=%00&suppressionKeyValue=%00",
            $"{Path}?suppressionKey=%ff%fe&suppressionKeyValue=%c0%af",
            $"{Path}?suppressionKey={Q(new string('A', 8000))}&suppressionKeyValue=v1",
            $"{Path}?suppressionKey={Q(key)}&suppressionKeyValue={Q(new string('A', 100_000))}",
            $"{Path}?suppressionKey={Q(key)}&suppressionKeyValue=v1&limit=999999999&offset=-1&tenantRegistryId=1&userName=admin"
        };
        for (var i = 0; i < 100; i++)
        {
            targets.Add(
                $"{Path}?suppressionKey={Q(key)}&suppressionKeyValue=v1&entityAnalysisModelGuid={modelQ}&p{i}={i}");
        }

        await run.ForEachAsync(targets, async target =>
        {
            var response = run.Check(await client.GetAsync(target), BlindBound);
            if (response.Status == 200)
            {
                run.Expect(FlagFor(response, foreign.ModelGuid) == null &&
                           !response.Body.Contains(foreign.ModelGuid.ToString(), StringComparison.OrdinalIgnoreCase),
                    response, "FOREIGN-DATA");
            }
            else
            {
                run.Expect(response.Status is 400 or 404 or 414 or 431, response, "STATUS");
            }
        });
        run.AssertClean(Output);
    }

    [Fact]
    public async Task ResponseShapeVerbsAndReplayAsync()
    {
        var run = NewRun($"{GetType().Name}.ShapeVerbsReplay");
        var key = Key();
        var scenario = await ScenarioAsync(UserA, key, "v1", true, DateTime.UtcNow.AddDays(1));
        var foreign = await ScenarioAsync(UserB, key, "v1", true);
        var client = As(PenTestUser.WithPermission);
        var target = Target(key, "v1", scenario.ModelGuid);

        var response = run.Check(await client.GetAsync(target));
        var allowed = AllowedProperties.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in ArrayOf(response) ?? [])
        {
            foreach (var property in row.EnumerateObject())
            {
                run.Expect(allowed.Contains(property.Name), response, "EXTRA-PROPERTY", property.Name);
            }
        }

        run.Expect(response.Header("X-Content-Type-Options") != null, response, "NOSNIFF");
        await VerbTamperingAsync(run, client, target, "POST", "PUT", "PATCH", "DELETE", "TRACE");

        await run.ForEachAsync(Enumerable.Range(0, 40), async i =>
        {
            var user = i % 2 == 0 ? PenTestUser.WithPermission : PenTestUser.TenantB;
            var guid = user == PenTestUser.WithPermission ? scenario.ModelGuid : foreign.ModelGuid;
            var r = run.Check(await As(user).GetAsync(Target(key, "v1", guid)));
            var own = user == PenTestUser.WithPermission ? scenario : foreign;
            var other = user == PenTestUser.WithPermission ? foreign : scenario;
            run.Expect(r.Status == 200 && FlagFor(r, own.ModelGuid, NameOf(own)) == true &&
                       FlagFor(r, other.ModelGuid) == null, r, "PARALLEL");
        }, 10);
        run.AssertClean(Output);
    }
}