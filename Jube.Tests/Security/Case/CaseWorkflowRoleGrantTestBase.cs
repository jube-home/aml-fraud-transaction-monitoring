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
using LinqToDB.Data;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Security.Case;

public abstract class CaseWorkflowRoleGrantTestBase(DatabaseFixture fx, ITestOutputHelper output)
    : CasePenTestBase(fx, output)
{
    protected override bool RoleGatingApplies => false;

    protected new abstract string Route { get; }

    protected abstract string ParentField { get; }

    protected abstract string ParentColumn { get; }

    protected abstract string Table { get; }

    protected abstract string ParentTable { get; }

    protected abstract string ListTemplate { get; }

    protected abstract Guid ParentGuid(CaseGraph graph);

    protected abstract int OwnGrantId(CaseGraph graph);

    private string Scope =>
        $"\"{ParentColumn}\" IN (SELECT \"Guid\" FROM \"{ParentTable}\" WHERE \"CaseWorkflowId\" IN (SELECT \"Id\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m)))";

    private DataParameter ModelParameter => new("m", World.ModelIds.ToArray());

    protected Task<long> GrantCountAsync(string extra = "TRUE") =>
        World.CountAsync(Table, $"{Scope} AND {extra}", ModelParameter);

    private Task<string> StateAsync() => World.ScalarAsync<string>(
        $"SELECT COALESCE(string_agg(\"Id\"::text || ':' || COALESCE(\"Deleted\"::text,'') || ':' || \"{ParentColumn}\"::text || ':' || \"RoleRegistryGuid\"::text, ',' ORDER BY \"Id\"), '') FROM \"{Table}\" WHERE {Scope}",
        ModelParameter);

    private object Grant(Guid parent, Guid role) => new Dictionary<string, object?>
        { [ParentField] = parent, ["roleRegistryGuid"] = role };

    private static bool IsSuccess(int status) => status == 200;

    private static int IdOf(PenTestResponse response)
    {
        var element = Parse(response);
        return element is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty("id", out var id)
            ? id.GetInt32()
            : 0;
    }

    [Fact]
    public async Task Matrix_EveryPrincipal_OnEveryRouteAsync()
    {
        var run = NewRun("Role grant matrix");
        var list = await MatrixAsync(run, "GET",
            $"{Route}/{ListTemplate.Replace("{guid}", ParentGuid(World.A).ToString())}", () => null, s => s == 200,
            s => s == 200);
        run.Expect(list.Body.Contains(World.RoleA.ToString()), list, "OWN-DATA", "own grant missing");
        var foreign = run.Check(await As(PenTestUser.TenantB)
            .GetAsync($"{Route}/{ListTemplate.Replace("{guid}", ParentGuid(World.A).ToString())}"));
        run.Expect(!foreign.Body.Contains(World.RoleA.ToString()), foreign, "BOLA",
            "tenant B listed tenant A's grants");

        var created = await MatrixAsync(run, "POST", Route + "/",
            () => Grant(ParentGuid(World.A), World.RoleNoPermission), s => s == 400, s => s == 200);
        var id = IdOf(created);
        run.Expect(created.Status == 200 && id > 0, created, "AUTHORISED", "owner could not grant");
        var deleted = await MatrixAsync(run, "DELETE", $"{Route}/{id}", () => null, s => !IsSuccess(s), IsSuccess);
        run.Expect(IsSuccess(deleted.Status), deleted, "AUTHORISED", "owner could not revoke");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task Create_WithForeignReferences_IsRefused_AndChangesNothingAsync()
    {
        var run = NewRun("Role grant BOLA create");
        var before = await StateAsync();
        var count = await GrantCountAsync();
        var otherTenantRoleCount =
            await World.CountAsync(Table, "\"RoleRegistryGuid\" = @r", new DataParameter("r", World.RoleB));
        foreach (var (label, parent, role) in new[]
                 {
                     ("foreign parent, own role", ParentGuid(World.B), World.RoleA),
                     ("own parent, foreign role (privilege escalation)", ParentGuid(World.A), World.RoleB),
                     ("foreign parent and foreign role", ParentGuid(World.B), World.RoleB),
                     ("random parent", Guid.NewGuid(), World.RoleA),
                     ("random role", ParentGuid(World.A), Guid.NewGuid()), ("empty parent", Guid.Empty, World.RoleA),
                     ("empty role", ParentGuid(World.A), Guid.Empty)
                 })
        {
            var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
                Grant(parent, role)));
            run.Expect(r.Status == 400, r, "BOLA", $"{label}: grant was not refused");
        }

        var reverse = run.Check(await SendJsonAsync(PenTestUser.TenantB, "POST", Route + "/",
            Grant(ParentGuid(World.A), World.RoleB)));
        run.Expect(reverse.Status == 400, reverse, "BOLA", "tenant B granted a role on tenant A's row");
        run.Expect(await StateAsync() == before && await GrantCountAsync() == count, reverse, "SIDE-EFFECT",
            "a refused grant changed rows");
        run.Expect(
            await World.CountAsync(Table, "\"RoleRegistryGuid\" = @r", new DataParameter("r", World.RoleB)) ==
            otherTenantRoleCount, reverse,
            "SIDE-EFFECT", "tenant B's role was attached to something");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task List_ForForeignAndMissingParents_IsIndistinguishableAsync()
    {
        var run = NewRun("Role grant BOLA list");
        var missing = run.Check(await As(PenTestUser.WithPermission)
            .GetAsync($"{Route}/{ListTemplate.Replace("{guid}", Guid.NewGuid().ToString())}"));
        var foreign = run.Check(await As(PenTestUser.WithPermission)
            .GetAsync($"{Route}/{ListTemplate.Replace("{guid}", ParentGuid(World.B).ToString())}"));
        run.Expect(foreign.Status == missing.Status && foreign.Body == missing.Body, foreign, "BOLA",
            "a foreign parent differs from a missing one");
        var reverse = run.Check(await As(PenTestUser.TenantB)
            .GetAsync($"{Route}/{ListTemplate.Replace("{guid}", ParentGuid(World.A).ToString())}"));
        run.Expect(reverse.Status == missing.Status && reverse.Body == missing.Body, reverse, "BOLA",
            "tenant B can tell tenant A's row from a missing one");
        ExpectNoForeign(run, PenTestUser.WithPermission, foreign);
        run.AssertClean(Output);
    }

    [Fact]
    public async Task Delete_ForeignIds_AreRefused_ReplayIsRefused_AndRowsSurviveAsync()
    {
        var run = NewRun("Role grant delete");
        var before = await StateAsync();
        var missing = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/2147480001"));
        foreach (var (label, id) in new[] { ("tenant B grant", OwnGrantId(World.B)), ("missing", 2147480001) })
        {
            var r = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{id}"));
            run.Expect(r.Status == missing.Status && !IsSuccess(r.Status), r, "BOLA",
                $"{label}: differs from a missing id");
        }

        var reverse = run.Check(await As(PenTestUser.TenantB).SendAsync("DELETE", $"{Route}/{OwnGrantId(World.A)}"));
        run.Expect(!IsSuccess(reverse.Status), reverse, "BOLA", "tenant B revoked tenant A's grant");
        run.Expect(await StateAsync() == before, reverse, "SIDE-EFFECT", "a refused delete changed rows");

        var created = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
            Grant(ParentGuid(World.A), World.RoleNoPermission)));
        var id2 = IdOf(created);
        var first = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{id2}"));
        var second = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{id2}"));
        run.Expect(IsSuccess(first.Status) && !IsSuccess(second.Status), second, "REPLAY",
            "double revoke should succeed once, then be refused");
        var listed = run.Check(await As(PenTestUser.WithPermission)
            .GetAsync($"{Route}/{ListTemplate.Replace("{guid}", ParentGuid(World.A).ToString())}"));
        run.Expect(!listed.Body.Contains(World.RoleNoPermission.ToString()), listed, "REPLAY",
            "a revoked grant is still listed");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task DuplicateAndConcurrentGrants_DoNotMultiplyAsync()
    {
        var run = NewRun("Role grant duplicates");
        var pair =
            $"\"RoleRegistryGuid\" = '{World.RoleNoPermission}' AND \"{ParentColumn}\" = '{ParentGuid(World.A)}' AND COALESCE(\"Deleted\",0) = 0";
        for (var i = 0; i < 3; i++)
        {
            var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
                Grant(ParentGuid(World.A), World.RoleNoPermission)));
            run.Expect(r.Status is 200 or 400 or 409, r, "REPLAY", "unexpected status");
        }

        run.Expect(await GrantCountAsync(pair) <= 1, new PenTestResponse { Request = "duplicates" }, "DUPLICATE",
            "the same role was granted more than once on the same row");
        await run.ForEachAsync(Enumerable.Range(0, 8), async _ =>
        {
            var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
                Grant(ParentGuid(World.A), World.RoleNoPermission)));
            run.Expect(r.Status is 200 or 400 or 409, r, "CONCURRENCY", "unexpected status");
        });
        run.AssertClean(Output);
    }

    [Fact]
    public async Task MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync()
    {
        var run = NewRun("Role grant mass assignment");
        var forged = Guid.NewGuid();
        var body = new Dictionary<string, object?>
        {
            [ParentField] = ParentGuid(World.A), ["roleRegistryGuid"] = World.RoleNoPermission, ["id"] = 1,
            ["guid"] = forged,
            ["createdUser"] = "forged-admin", ["createdDate"] = "1999-01-01T00:00:00Z", ["deleted"] = 1,
            ["deletedUser"] = "x", ["version"] = 9,
            ["importId"] = 4, ["tenantRegistryId"] = World.TenantBId, ["$type"] = "x", ["__proto__"] = new { x = 1 }
        };
        var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/", body));
        run.Expect(r.Status is 200 or 400, r, "STATUS", "unexpected status");
        if (r.Status == 200)
        {
            var id = IdOf(r);
            run.Expect(id > 1, r, "MASS", "client id honoured");
            var row = (await World.ScalarAsync<string>(
                $"SELECT \"Guid\"::text || '|' || COALESCE(\"CreatedUser\",'') || '|' || COALESCE(\"Deleted\"::text,'0') || '|' || COALESCE(\"Version\"::text,'') || '|' || COALESCE(\"ImportId\"::text,'') || '|' || \"CreatedDate\"::text FROM \"{Table}\" WHERE \"Id\" = @i",
                new DataParameter("i", id))).Split('|');
            run.Expect(row[0] != forged.ToString(), r, "MASS", "guid honoured");
            run.Expect(row[1] == Fx.Seed.UserWithPermission, r, "MASS", "created user forged");
            run.Expect(row[2] is "0" or "", r, "MASS", "created as deleted");
            run.Expect(row[3] == "1" && row[4] == "", r, "MASS", "version or import id forged");
            run.Expect(!row[5].StartsWith("1999", StringComparison.Ordinal), r, "MASS", "created date forged");
        }

        run.AssertClean(Output);
    }

    [Fact]
    public async Task Boundaries_TypeConfusion_AndInjection_FailSafelyAsync()
    {
        var run = NewRun("Role grant boundaries");
        var before = await StateAsync();
        var values = CaseAttackStrings.Mutated.Take(240).Select(v => JsonSerializer.Serialize(v)).Concat([
            "null", "1", "-1", "1.5", "true", "[]", "{}", "[\"a\"]", "\"\"", "\" \"",
            "\"00000000-0000-0000-0000-000000000000\"", "\"{11111111-2222-3333-4444-555555555555}\"",
            "\"" + new string('a', 5000) + "\""
        ]).ToList();
        await run.ForEachAsync(values, async value =>
        {
            foreach (var body in new[]
                     {
                         $"{{\"{ParentField}\":{value},\"roleRegistryGuid\":\"{World.RoleNoPermission}\"}}",
                         $"{{\"{ParentField}\":\"{ParentGuid(World.A)}\",\"roleRegistryGuid\":{value}}}"
                     })
            {
                var r = run.Check(await SendRawAsync(PenTestUser.WithPermission, "POST", Route + "/", body),
                    TimeSpan.FromSeconds(4));
                run.Expect(r.Status is 400 or 415 or 422, r, "BOUNDARY", "a malformed reference was not refused");
            }
        }, 6);

        foreach (var body in new[]
                 {
                     "", "{", "[]", "null", "\"x\"", "1", "{}", "﻿{}", string.Concat(Enumerable.Repeat("[", 4000)),
                     $"{{\"{ParentField}\":\"{ParentGuid(World.B)}\",\"{ParentField}\":\"{ParentGuid(World.A)}\",\"roleRegistryGuid\":\"{World.RoleB}\",\"roleRegistryGuid\":\"{World.RoleNoPermission}\"}}"
                 })
        {
            var r = run.Check(await SendRawAsync(PenTestUser.WithPermission, "POST", Route + "/", body));
            run.Expect(r.Status is 400 or 415 or 200, r, "BOUNDARY", "malformed body");
        }

        foreach (var contentType in new[]
                 {
                     "text/plain", "application/xml", "application/x-www-form-urlencoded",
                     "multipart/form-data; boundary=x", ""
                 })
        {
            var r = run.Check(await SendRawAsync(PenTestUser.WithPermission, "POST", Route + "/",
                JsonSerializer.Serialize(Grant(ParentGuid(World.A), World.RoleNoPermission)), contentType));
            run.Expect(r.Status is 400 or 415, r, "CONTENT-TYPE", contentType);
        }

        var state = await StateAsync();
        run.Expect(
            await World.CountAsync(Table, $"\"{ParentColumn}\" = @p AND \"RoleRegistryGuid\" = @r",
                new DataParameter("p", ParentGuid(World.A)), new DataParameter("r", World.RoleB)) == 0,
            new PenTestResponse { Request = "state" }, "BOLA", "another tenant's role was attached");
        run.Expect(state.StartsWith(before.Split(',')[0], StringComparison.Ordinal),
            new PenTestResponse { Request = "state" }, "INTEGRITY", "seeded grant changed");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync()
    {
        var run = NewRun("Role grant route parameters");
        var before = await StateAsync();
        var values = CaseAttackStrings.Mutated.Take(240).Concat([
            "0", "-1", "2147483647", "2147483648", "1e3", "0x10", "1.5", "NaN", new string('9', 3000),
            "00000000-0000-0000-0000-000000000000"
        ]).ToList();
        await run.ForEachAsync(values, async value =>
        {
            var get = run.Check(
                await As(PenTestUser.WithPermission).GetAsync($"{Route}/{ListTemplate.Replace("{guid}", Enc(value))}"),
                TimeSpan.FromSeconds(4));
            run.Expect(get.Status is 200 or 400 or 404 or 405 or 414 or 431, get, "STATUS", "unexpected status");
            ExpectNoForeign(run, PenTestUser.WithPermission, get);
            var delete = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{Enc(value)}"),
                TimeSpan.FromSeconds(4));
            run.Expect(delete.Status is 200 or 204 or 400 or 404 or 405 or 414 or 431, delete, "STATUS",
                "unexpected status");
        }, 6);
        run.Expect(await StateAsync() == before, new PenTestResponse { Request = "state" }, "SIDE-EFFECT",
            "route probing changed rows");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task Verbs_AndOverrides_AreNotAcceptedAsync()
    {
        var run = NewRun("Role grant verbs");
        var before = await StateAsync();
        foreach (var (method, target) in new[]
                 {
                     ("PUT", Route + "/"), ("PATCH", Route + "/"), ("DELETE", Route + "/"), ("GET", Route + "/"),
                     ("POST", $"{Route}/{OwnGrantId(World.A)}"),
                     ("PUT", $"{Route}/{OwnGrantId(World.A)}"), ("GET", $"{Route}/{OwnGrantId(World.A)}"),
                     ("POST", $"{Route}/{ListTemplate.Replace("{guid}", ParentGuid(World.A).ToString())}")
                 })
        {
            var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, method, target,
                Grant(ParentGuid(World.A), World.RoleNoPermission)));
            run.Expect(r.Status is 404 or 405, r, "VERB", $"{method} {target} accepted");
        }

        foreach (var header in new[] { "X-HTTP-Method-Override", "X-HTTP-Method", "X-Method-Override" })
        {
            var r = run.Check(await As(PenTestUser.WithPermission)
                .SendAsync("POST", $"{Route}/{OwnGrantId(World.A)}", null, null, (header, "DELETE")));
            run.Expect(r.Status is 404 or 405, r, "OVERRIDE", "method override honoured");
        }

        run.Expect(await StateAsync() == before, new PenTestResponse { Request = "state" }, "VERB",
            "a wrong verb changed rows");
        run.AssertClean(Output);
    }
}