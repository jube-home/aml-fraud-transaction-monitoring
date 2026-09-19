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

public abstract class CaseWorkflowChildTestBase(DatabaseFixture fx, ITestOutputHelper output)
    : CasePenTestBase(fx, output)
{
    protected override bool RoleGatingApplies => false;

    protected new abstract string Route { get; }

    protected abstract string Kind { get; }

    protected abstract string Table { get; }

    protected abstract Dictionary<string, object?> Valid(CaseGraph graph, string name);

    protected abstract int OwnId(CaseGraph graph);

    protected abstract Guid OwnGuid(CaseGraph graph);

    protected abstract IReadOnlyDictionary<string, int> TextFields { get; }

    protected abstract IReadOnlyList<(string Template, bool ActiveOnly)> ListRoutes { get; }

    protected string LabelOf(CaseGraph graph) =>
        graph == World.A ? "A" : graph == World.Gated ? "G" : graph == World.HiddenStatus ? "H" : "B";

    protected string OwnName(CaseGraph graph) => $"ZzTestPen{Kind}{LabelOf(graph)}{World.Tag}";

    private string ChildWhere =>
        "\"CaseWorkflowId\" IN (SELECT \"Id\" FROM \"CaseWorkflow\" WHERE \"EntityAnalysisModelId\" = ANY(@m))";

    protected Task<long> RowCountAsync(string extraWhere = "TRUE") =>
        World.CountAsync(Table, $"{ChildWhere} AND {extraWhere}", new DataParameter("m", World.ModelIds.ToArray()));

    private Task<string> StateAsync() => World.ScalarAsync<string>(
        $"SELECT COALESCE(string_agg(\"Id\"::text || ':' || COALESCE(\"Name\",'') || ':' || COALESCE(\"Deleted\"::text,'') || ':' || COALESCE(\"Version\"::text,'') || ':' || \"CaseWorkflowId\"::text || ':' || COALESCE(\"Locked\"::text,'') || ':' || COALESCE(\"Active\"::text,''), ',' ORDER BY \"Id\"), '') FROM \"{Table}\" WHERE {ChildWhere}",
        new DataParameter("m", World.ModelIds.ToArray()));

    private string Target(string template, CaseGraph graph) =>
        template.Replace("{id}", graph.WorkflowId.ToString()).Replace("{guid}", graph.WorkflowGuid.ToString());

    private Dictionary<string, object?> WithId(CaseGraph graph, int id,
        Action<Dictionary<string, object?>>? mutate = null)
    {
        var body = Valid(graph, OwnName(graph));
        body["id"] = id;
        mutate?.Invoke(body);
        return body;
    }

    private async Task<int> CreateOwnAsync(PenTestRun run, string suffix,
        Action<Dictionary<string, object?>>? mutate = null)
    {
        var body = Valid(World.A, $"ZzTestPen{Kind}New{suffix}{World.Tag}");
        mutate?.Invoke(body);
        var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/", body));
        run.Expect(r.Status == 200, r, "SETUP", "could not create a row");
        var element = Parse(r);
        return element is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty("id", out var id)
            ? id.GetInt32()
            : 0;
    }

    [Fact]
    public async Task Matrix_ReadRoutes_EveryPrincipalAsync()
    {
        var run = NewRun($"{Kind} read matrix");
        var list = await MatrixAsync(run, "GET", Route + "/", () => null, s => s == 200, s => s == 200);
        run.Expect(list.Body.Contains(OwnName(World.A)), list, "OWN-DATA", "own row missing from the list");
        run.Expect(!list.Body.Contains(OwnName(World.B)), list, "BOLA", "tenant B's row is listed for tenant A");
        var byId = await MatrixAsync(run, "GET", $"{Route}/{OwnId(World.A)}", () => null, s => s == 200 && true,
            s => s == 200);
        run.Expect(byId.Body.Contains(OwnName(World.A)), byId, "OWN-DATA", "own row not returned by id");
        var foreignView = run.Check(await As(PenTestUser.TenantB).GetAsync($"{Route}/{OwnId(World.A)}"));
        run.Expect(!foreignView.Body.Contains(OwnName(World.A)), foreignView, "BOLA",
            "tenant B read tenant A's row by id");
        foreach (var (template, _) in ListRoutes)
        {
            var target = Route + "/" + Target(template, World.A);
            var r = await MatrixAsync(run, "GET", target, () => null, s => s == 200, s => s == 200);
            run.Expect(r.Body.Contains(OwnName(World.A)), r, "OWN-DATA", $"{template}: own row missing");
            var foreign = run.Check(await As(PenTestUser.TenantB).GetAsync(target));
            run.Expect(!foreign.Body.Contains(OwnName(World.A)), foreign, "BOLA",
                $"{template}: tenant B saw tenant A's row");
        }

        run.AssertClean(Output);
    }

    [Fact]
    public async Task Matrix_WriteRoutes_EveryPrincipalAsync()
    {
        var run = NewRun($"{Kind} write matrix");
        var created = await MatrixAsync(run, "POST", Route + "/",
            () => Valid(World.A, $"ZzTestPen{Kind}Matrix{Guid.NewGuid():N}"[..40] + World.Tag),
            s => s == 400, s => s == 200);
        run.Expect(created.Status == 200, created, "AUTHORISED", "owner could not create");
        var id = CreateId(created);
        var updated = await MatrixAsync(run, "PUT", Route + "/",
            () => WithId(World.A, id, b => b["name"] = OwnName(World.A) + "u"),
            s => s is 204 or 400, s => s == 200);
        run.Expect(updated.Status == 200, updated, "AUTHORISED", "owner could not update");
        var deleted = await MatrixAsync(run, "DELETE", $"{Route}/{id}", () => null, s => !IsSuccess(s), IsSuccess);
        run.Expect(IsSuccess(deleted.Status), deleted, "AUTHORISED", "owner could not delete");
        run.AssertClean(Output);
    }

    private static bool IsSuccess(int status) => status == 200;

    private static int CreateId(PenTestResponse response)
    {
        var element = Parse(response);
        return element is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty("id", out var id)
            ? id.GetInt32()
            : 0;
    }

    [Fact]
    public async Task ForeignAndGatedIds_BehaveLikeMissing_OnEveryReadRouteAsync()
    {
        var run = NewRun($"{Kind} BOLA reads");
        var missingId = await ReadAsync(run, PenTestUser.WithPermission, $"{Route}/2147480001");
        var foreignId = await ReadAsync(run, PenTestUser.WithPermission, $"{Route}/{OwnId(World.B)}");
        run.Expect(foreignId.Status == missingId.Status && foreignId.Body == missingId.Body, foreignId, "BOLA",
            "a foreign id differs from a missing id");
        ExpectNoForeign(run, PenTestUser.WithPermission, foreignId);
        var reverse = await ReadAsync(run, PenTestUser.TenantB, $"{Route}/{OwnId(World.A)}");
        run.Expect(reverse.Status == missingId.Status && reverse.Body == missingId.Body, reverse, "BOLA",
            "tenant B can tell tenant A's id from a missing one");

        foreach (var (template, activeOnly) in ListRoutes)
        {
            var missingWorkflow = template.Contains("{guid}")
                ? await ReadAsync(run, PenTestUser.WithPermission,
                    $"{Route}/{template.Replace("{guid}", Guid.NewGuid().ToString())}")
                : await ReadAsync(run, PenTestUser.WithPermission, $"{Route}/{template.Replace("{id}", "2147480001")}");
            foreach (var graph in new[] { World.B })
            {
                var r = await ReadAsync(run, PenTestUser.WithPermission, $"{Route}/{Target(template, graph)}");
                run.Expect(r.Status == missingWorkflow.Status && r.Body == missingWorkflow.Body, r, "BOLA",
                    $"{template}: foreign workflow differs from a missing one");
                ExpectNoForeign(run, PenTestUser.WithPermission, r);
            }

            var back = await ReadAsync(run, PenTestUser.TenantB, $"{Route}/{Target(template, World.A)}");
            run.Expect(back.Status == missingWorkflow.Status && back.Body == missingWorkflow.Body, back, "BOLA",
                $"{template}: tenant B sees tenant A's workflow");
            if (!activeOnly)
            {
                continue;
            }

            var gated = await ReadAsync(run, PenTestUser.WithPermission,
                $"{Route}/{Target(template, World.Gated)}");
            run.Expect(gated.Status == 200 && !gated.Body.Contains(OwnName(World.Gated)), gated, "GATING",
                $"{template}: role-gated workflow content returned");
        }

        await run.ForEachAsync(Enumerable.Range(-4, 9).Select(o => OwnId(World.A) + o), async id =>
        {
            var r = await ReadAsync(run, PenTestUser.WithPermission, $"{Route}/{id}");
            ExpectNoForeign(run, PenTestUser.WithPermission, r);
        });
        run.AssertClean(Output);
    }

    private async Task<PenTestResponse> ReadAsync(PenTestRun run, PenTestUser user, string target)
    {
        return run.Check(await As(user).GetAsync(target));
    }

    [Fact]
    public async Task Writes_ToForeignRowsAndParents_AreRefused_AndChangeNothingAsync()
    {
        var run = NewRun($"{Kind} BOLA writes");
        var before = await StateAsync();
        var missing = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "PUT", Route + "/",
            WithId(World.A, 2147480001, b => b["name"] = "ZzTestNobody")));
        var foreignPut = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "PUT", Route + "/",
            WithId(World.B, OwnId(World.B), b => b["name"] = "ZzTestTakenOver")));
        run.Expect(foreignPut.Status == missing.Status || foreignPut.Status == 400, foreignPut, "BOLA",
            "update of a foreign row differs from a missing row");
        var reversePut = run.Check(await SendJsonAsync(PenTestUser.TenantB, "PUT", Route + "/", WithId(World.A,
            OwnId(World.A), b =>
            {
                b["name"] = "ZzTestTakenOver";
                b["caseWorkflowId"] = World.B.WorkflowId;
            })));
        run.Expect(reversePut.Status is 204 or 400, reversePut, "BOLA", "tenant B updated tenant A's row");

        var reparent = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "PUT", Route + "/",
            WithId(World.A, OwnId(World.A), b => b["caseWorkflowId"] = World.B.WorkflowId)));
        run.Expect(reparent.Status == 400, reparent, "REPARENT", "a row was moved under another tenant's workflow");
        var insertForeign = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
            Valid(World.B, $"ZzTestPen{Kind}Planted{World.Tag}")));
        run.Expect(insertForeign.Status == 400, insertForeign, "REPARENT",
            "a row was created under another tenant's workflow");
        foreach (var workflowId in new[] { 0, -1, int.MaxValue, 2147480001 })
        {
            var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
                Set(Valid(World.A, "x"), "caseWorkflowId", workflowId)));
            run.Expect(r.Status == 400, r, "REPARENT", $"workflow id {workflowId} accepted");
        }

        var deleteForeign =
            run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{OwnId(World.B)}"));
        var deleteMissing = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/2147480001"));
        run.Expect(deleteForeign.Status == deleteMissing.Status && !IsSuccess(deleteForeign.Status), deleteForeign,
            "BOLA", "delete of a foreign row differs from a missing row");
        var reverseDelete = run.Check(await As(PenTestUser.TenantB).SendAsync("DELETE", $"{Route}/{OwnId(World.A)}"));
        run.Expect(!IsSuccess(reverseDelete.Status), reverseDelete, "BOLA", "tenant B deleted tenant A's row");
        run.Expect(await StateAsync() == before, foreignPut, "SIDE-EFFECT", "a refused write changed rows");
        run.AssertClean(Output);
    }

    private static Dictionary<string, object?> Set(Dictionary<string, object?> body, string key, object? value)
    {
        body[key] = value;
        return body;
    }

    [Fact]
    public async Task MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync()
    {
        var run = NewRun($"{Kind} mass assignment");
        var forgedGuid = Guid.NewGuid();
        var id = await CreateOwnAsync(run, "Mass", b =>
        {
            b["id"] = 1;
            b["guid"] = forgedGuid;
            b["createdUser"] = "forged-admin";
            b["createdDate"] = "1999-01-01T00:00:00Z";
            b["updatedUser"] = "forged-admin";
            b["deleted"] = 1;
            b["deletedUser"] = "forged-admin";
            b["version"] = 77;
            b["tenantRegistryId"] = World.TenantBId;
            b["importId"] = 5;
            b["$type"] = "x";
            b["__proto__"] = new { x = 1 };
        });
        run.Expect(id > 1, new PenTestResponse { Request = "create" }, "MASS", "client supplied id honoured");
        var row = await World.ScalarAsync<string>(
            $"SELECT \"Guid\"::text || '|' || COALESCE(\"CreatedUser\",'') || '|' || COALESCE(\"Deleted\"::text,'0') || '|' || COALESCE(\"Version\"::text,'') || '|' || COALESCE(\"ImportId\"::text,'') || '|' || COALESCE(\"CreatedDate\"::text,'') FROM \"{Table}\" WHERE \"Id\" = @i",
            new DataParameter("i", id));
        var parts = row.Split('|');
        run.Expect(parts[0] != forgedGuid.ToString(), new PenTestResponse { Request = "create" }, "MASS",
            "client supplied guid honoured");
        run.Expect(parts[1] == Fx.Seed.UserWithPermission, new PenTestResponse { Request = "create" }, "MASS",
            $"created user forged: {parts[1]}");
        run.Expect(parts[2] is "0" or "", new PenTestResponse { Request = "create" }, "MASS", "created as deleted");
        run.Expect(parts[3] == "1", new PenTestResponse { Request = "create" }, "MASS", $"version forged: {parts[3]}");
        run.Expect(parts[4] == "", new PenTestResponse { Request = "create" }, "MASS", "import id forged");
        run.Expect(!parts[5].StartsWith("1999", StringComparison.Ordinal), new PenTestResponse { Request = "create" },
            "MASS", "created date forged");

        var guidBefore = parts[0];
        var update = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "PUT", Route + "/", WithId(World.A, id,
            b =>
            {
                b["name"] = $"ZzTestPen{Kind}NewMass{World.Tag}";
                b["guid"] = Guid.NewGuid();
                b["createdUser"] = "forged-admin";
                b["createdDate"] = "1999-01-01T00:00:00Z";
                b["deleted"] = 1;
                b["version"] = 500;
                b["deletedUser"] = "forged-admin";
            })));
        run.Expect(update.Status == 200, update, "STATUS", "update failed");
        var after = (await World.ScalarAsync<string>(
            $"SELECT \"Guid\"::text || '|' || COALESCE(\"CreatedUser\",'') || '|' || COALESCE(\"Deleted\"::text,'0') || '|' || COALESCE(\"Version\"::text,'') || '|' || COALESCE(\"CreatedDate\"::text,'') FROM \"{Table}\" WHERE \"Id\" = @i",
            new DataParameter("i", id))).Split('|');
        run.Expect(after[0] == guidBefore, update, "MASS", "guid changed by update");
        run.Expect(after[1] == Fx.Seed.UserWithPermission, update, "MASS", "created user changed by update");
        run.Expect(after[2] is "0" or "", update, "MASS", "update soft-deleted the row");
        run.Expect(after[3] == "2", update, "MASS", $"version not server managed: {after[3]}");
        run.Expect(after[4] == parts[5], update, "MASS", "created date changed by update");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task TextFields_Injection_IsStoredAsDataOrRefusedAsync()
    {
        var run = NewRun($"{Kind} injection");
        var before = await StateAsync();
        var foreignBefore =
            await World.CountAsync(Table, "\"CaseWorkflowId\" = @w", new DataParameter("w", World.B.WorkflowId));
        var values = CaseAttackStrings.Mutated.Where(v => !v.Contains('\0') && v.Length < 400).Take(220).ToList();
        var fields = new[] { "name" }.Concat(TextFields.Keys).ToList();
        var counter = 0;
        foreach (var field in fields)
        {
            await run.ForEachAsync(values, async value =>
            {
                var n = System.Threading.Interlocked.Increment(ref counter);
                var body = Valid(World.A, $"ZzTestPen{Kind}Inj{n}{World.Tag}");
                body[field] = field == "name" ? $"ZzTestPen{Kind}Inj{n}{World.Tag}{value}" : Encode(field, value);
                if (field.StartsWith("enable", StringComparison.Ordinal) is false)
                {
                    EnableFor(body, field);
                }

                var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/", body),
                    TimeSpan.FromSeconds(4));
                run.Expect(r.Status is 200 or 400, r, "INJECTION", $"{field}: unexpected status");
                if (r.Status != 200)
                {
                    return;
                }

                var element = Parse(r);
                if (element is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty(field, out var stored) &&
                    stored.ValueKind == JsonValueKind.String &&
                    field != "name")
                {
                    run.Expect(stored.GetString() == Encode(field, value), r, "ROUNDTRIP",
                        $"{field}: value not stored faithfully");
                }
            }, 4);
        }

        run.Expect(
            await World.CountAsync(Table, "\"CaseWorkflowId\" = @w", new DataParameter("w", World.B.WorkflowId)) ==
            foreignBefore,
            new PenTestResponse { Request = "state" }, "INJECTION", "injection reached another tenant's rows");
        var seeded = run.Check(await As(PenTestUser.WithPermission).GetAsync($"{Route}/{OwnId(World.A)}"));
        run.Expect(seeded.Body.Contains(OwnName(World.A)), seeded, "INTEGRITY", "seeded row damaged");
        run.Expect((await StateAsync()).StartsWith(before.Split(',')[0], StringComparison.Ordinal), seeded, "INTEGRITY",
            "first seeded row changed");
        run.AssertClean(Output);
    }

    protected virtual void EnableFor(Dictionary<string, object?> body, string field)
    {
        if (field == "httpEndpoint")
        {
            body["enableHttpEndpoint"] = true;
            body["httpEndpointTypeId"] = 1;
        }

        if (field.StartsWith("notification", StringComparison.Ordinal))
        {
            body["enableNotification"] = true;
            body["notificationTypeId"] = 1;
            body["notificationDestination"] = body.TryGetValue("notificationDestination", out var d) && d is not null
                ? d
                : "a@b.example";
        }
    }

    [Fact]
    public async Task TextFields_Limits_AndOversizeBodies_AreEnforcedWithoutErrorsAsync()
    {
        var run = NewRun($"{Kind} limits");
        var name256 = "N" + new string('n', 255 - World.Tag.Length) + World.Tag;
        var ok = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
            Valid(World.A, name256[..256])));
        run.Expect(ok.Status == 200, ok, "LIMIT", "a name of 256 characters was refused");
        var tooLong = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
            Valid(World.A, name256 + "xx")));
        run.Expect(tooLong.Status == 400, tooLong, "LIMIT", "a name of 258 characters was accepted");
        var description = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
            Set(Valid(World.A, $"ZzTestPen{Kind}Desc{World.Tag}"), "description", new string('d', 1025))));
        run.Expect(description.Status == 400, description, "LIMIT", "a description of 1025 characters was accepted");
        foreach (var (field, limit) in TextFields)
        {
            var atLimit = Valid(World.A, $"ZzTestPen{Kind}L{field}{World.Tag}");
            atLimit[field] = Blob(field, limit);
            EnableFor(atLimit, field);
            var a = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/", atLimit),
                TimeSpan.FromSeconds(15));
            run.Expect(a.Status == 200, a, "LIMIT", $"{field}: a value at the limit ({limit}) was refused");
            var over = Valid(World.A, $"ZzTestPen{Kind}O{field}{World.Tag}");
            over[field] = Blob(field, limit + 1);
            EnableFor(over, field);
            var b = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/", over),
                TimeSpan.FromSeconds(15));
            run.Expect(b.Status == 400, b, "LIMIT", $"{field}: a value over the limit was accepted");
            var huge = Valid(World.A, $"ZzTestPen{Kind}H{field}{World.Tag}");
            huge[field] = Blob(field, 4_000_000);
            EnableFor(huge, field);
            var c = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/", huge),
                TimeSpan.FromSeconds(20));
            run.Expect(c.Status is 400 or 413, c, "API4", $"{field}: a 4 MB value was accepted");
        }

        run.AssertClean(Output);
    }

    [Fact]
    public async Task Boundaries_TypeConfusion_AndMalformedBodies_FailSafelyAsync()
    {
        var run = NewRun($"{Kind} boundaries");
        var before = await StateAsync();
        var workflowId = World.A.WorkflowId;

        string Raw(string field, string value) =>
            $"{{\"caseWorkflowId\":{workflowId},\"name\":\"ZzTestPen{Kind}Bnd{World.Tag}\",\"active\":true,{Fixed()}\"{field}\":{value}}}";

        var cases = new List<(string, string)>();
        foreach (var field in new[]
                     { "caseWorkflowId", "id", "httpEndpointTypeId", "notificationTypeId", "notificationType" })
        {
            foreach (var value in new[]
                     {
                         "0", "-1", "2147483647", "2147483648", "-2147483649", "1.5", "1e3", "\"1\"", "\"abc\"", "null",
                         "true", "[]", "{}", "NaN",
                         "999999999999999999999", "[1]", "{\"a\":1}"
                     })
            {
                cases.Add((field, value));
            }
        }

        foreach (var field in new[] { "active", "locked", "enableHttpEndpoint", "enableNotification" })
        {
            foreach (var value in new[] { "\"yes\"", "1", "0", "null", "[]", "{}", "\"true\"", "2" })
            {
                cases.Add((field, value));
            }
        }

        foreach (var field in new[] { "name", "description" }.Concat(TextFields.Keys))
        {
            foreach (var value in new[]
                     {
                         "null", "\"\"", "\" \"", "123", "[]", "{}", "true", "[\"a\"]", "\"\\u0000\"", "\"\\ud800\""
                     })
            {
                cases.Add((field, value));
            }
        }

        await run.ForEachAsync(cases, async c =>
        {
            var r = run.Check(
                await SendRawAsync(PenTestUser.WithPermission, "POST", Route + "/", Raw(c.Item1, c.Item2)),
                TimeSpan.FromSeconds(4));
            run.Expect(r.Status is 200 or 400 or 415 or 422, r, "BOUNDARY", $"{c.Item1}={c.Item2}");
        }, 4);

        foreach (var method in new[] { "POST", "PUT" })
        {
            foreach (var body in new[]
                     {
                         "", "{", "[]", "null", "\"x\"", "1", "{}", "\uFEFF{}",
                         string.Concat(Enumerable.Repeat("[", 4000)),
                         "{\"id\":\"1\"}"
                     })
            {
                var r = run.Check(await SendRawAsync(PenTestUser.WithPermission, method, Route + "/", body));
                run.Expect(r.Status is 400 or 415 or 204, r, "BOUNDARY", $"{method} malformed body");
            }

            foreach (var contentType in new[]
                     {
                         "text/plain", "application/xml", "application/x-www-form-urlencoded",
                         "multipart/form-data; boundary=x", ""
                     })
            {
                var r = run.Check(await SendRawAsync(PenTestUser.WithPermission, method, Route + "/",
                    JsonSerializer.Serialize(WithId(World.A, OwnId(World.A))), contentType));
                run.Expect(r.Status is 400 or 415, r, "CONTENT-TYPE", $"{method} {contentType}");
            }
        }

        var state = await StateAsync();
        run.Expect(state.StartsWith(before.Split(',')[0], StringComparison.Ordinal),
            new PenTestResponse { Request = "state" }, "INTEGRITY", "seeded rows changed");
        run.AssertClean(Output);
    }

    protected virtual string Fixed() => string.Empty;

    protected virtual string Encode(string field, string value) => value;

    protected virtual string Blob(string field, int length) => new('a', length);

    [Fact]
    public async Task RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync()
    {
        var run = NewRun($"{Kind} route parameters");
        var values = CaseAttackStrings.Mutated.Take(260).Concat([
            "0", "-1", "2147483647", "2147483648", "-2147483649", "1e3", "0x10", "1.5", "NaN", "١٢٣",
            new string('9', 3000),
            "00000000-0000-0000-0000-000000000000", Guid.NewGuid().ToString(), "{" + Guid.NewGuid() + "}"
        ]).ToList();
        var before = await StateAsync();
        await run.ForEachAsync(values, async value =>
        {
            foreach (var target in new[] { $"{Route}/{Enc(value)}" }.Concat(ListRoutes.Select(l =>
                         $"{Route}/{l.Template.Replace("{id}", Enc(value)).Replace("{guid}", Enc(value))}")))
            {
                var r = run.Check(await As(PenTestUser.WithPermission).GetAsync(target), TimeSpan.FromSeconds(4));
                run.Expect(r.Status is 200 or 400 or 404 or 405 or 414 or 431, r, "STATUS",
                    "route value produced an unexpected status");
                ExpectNoForeign(run, PenTestUser.WithPermission, r);
            }

            var d = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{Enc(value)}"),
                TimeSpan.FromSeconds(4));
            run.Expect(d.Status is 200 or 204 or 400 or 404 or 405 or 414 or 431, d, "STATUS",
                "delete route value produced an unexpected status");
        }, 6);
        run.Expect(await StateAsync() == before, new PenTestResponse { Request = "state" }, "SIDE-EFFECT",
            "route probing changed rows");
        run.AssertClean(Output);
    }

    [Fact]
    public async Task SoftDelete_Replay_LockedRows_AndConcurrentDuplicatesAsync()
    {
        var run = NewRun($"{Kind} lifecycle");
        var id = await CreateOwnAsync(run, "Life");
        var duplicate = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
            Valid(World.A, $"ZzTestPen{Kind}NewLife{World.Tag}")));
        run.Expect(duplicate.Status == 400, duplicate, "REPLAY", "a duplicate name was accepted");
        var lower = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/",
            Valid(World.A, $"zztestpen{Kind}newlife{World.Tag}".ToLowerInvariant())));
        run.Expect(lower.Status == 400, lower, "REPLAY", "a case-insensitive duplicate name was accepted");

        var first = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{id}"));
        var second = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{id}"));
        run.Expect(IsSuccess(first.Status) && !IsSuccess(second.Status), second, "REPLAY",
            "double delete should succeed once, then be refused");
        var updateDeleted = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "PUT", Route + "/",
            WithId(World.A, id, b => b["name"] = $"ZzTestPen{Kind}Undead{World.Tag}")));
        run.Expect(updateDeleted.Status is 204 or 400, updateDeleted, "DELETED", "a soft-deleted row could be updated");
        run.Expect(await World.CountAsync(Table, "\"Id\" = @i AND \"Deleted\" = 1", new DataParameter("i", id)) == 1,
            updateDeleted, "DELETED", "the row was resurrected");
        var listed = run.Check(await As(PenTestUser.WithPermission).GetAsync(Route + "/"));
        run.Expect(!listed.Body.Contains($"ZzTestPen{Kind}NewLife{World.Tag}\"") || !IsListedActive(listed, id), listed,
            "DELETED", "a deleted row is still listed as active");

        var lockedId = await CreateOwnAsync(run, "Lck", b => b["locked"] = true);
        var lockedUpdate = run.Check(await SendJsonAsync(PenTestUser.WithPermission, "PUT", Route + "/", WithId(World.A,
            lockedId, b =>
            {
                b["name"] = $"ZzTestPen{Kind}NewLck{World.Tag}";
                b["locked"] = false;
                b["description"] = "unlocked";
            })));
        var lockedDelete = run.Check(await As(PenTestUser.WithPermission).SendAsync("DELETE", $"{Route}/{lockedId}"));
        run.Expect(lockedUpdate.Status is 204 or 400 or 403 or 409, lockedUpdate, "LOCK",
            "a locked row could be updated (and unlocked)");
        run.Expect(!IsSuccess(lockedDelete.Status), lockedDelete, "LOCK", "a locked row could be deleted");
        run.Expect(
            await World.CountAsync(Table, "\"Id\" = @i AND \"Locked\" = 1 AND COALESCE(\"Deleted\",0) = 0",
                new DataParameter("i", lockedId)) == 1,
            lockedDelete, "LOCK", "the locked row changed");

        run.AssertClean(Output);
    }

    [Fact]
    public async Task ConcurrentCreates_OfTheSameName_ProduceOneRowAsync()
    {
        var run = NewRun($"{Kind} race");
        var name = $"ZzTestPen{Kind}Race{World.Tag}";
        var results = new List<int>();
        await run.ForEachAsync(Enumerable.Range(0, 8), async _ =>
        {
            var r = run.Check(
                await SendJsonAsync(PenTestUser.WithPermission, "POST", Route + "/", Valid(World.A, name)));
            lock (results)
            {
                results.Add(r.Status);
            }
        });
        run.Expect(results.All(s => s is 200 or 400 or 409), new PenTestResponse { Request = "race" }, "CONCURRENCY",
            "unexpected status under concurrency");
        run.Expect(await RowCountAsync($"\"Name\" = '{name}' AND COALESCE(\"Deleted\",0) = 0") <= 1,
            new PenTestResponse { Request = "race" }, "CONCURRENCY",
            "concurrent creates produced duplicate names");
        run.AssertClean(Output);
    }

    private static bool IsListedActive(PenTestResponse list, int id)
    {
        var element = Parse(list);
        return element is { ValueKind: JsonValueKind.Array } a &&
               a.EnumerateArray().Any(e => e.TryGetProperty("id", out var v) && v.GetInt32() == id);
    }

    [Fact]
    public async Task Verbs_AndOverrides_AreNotAcceptedAsync()
    {
        var run = NewRun($"{Kind} verbs");
        var before = await StateAsync();
        var id = OwnId(World.A);
        foreach (var (method, target) in new[]
                 {
                     ("POST", $"{Route}/{id}"), ("PUT", $"{Route}/{id}"), ("PATCH", $"{Route}/{id}"),
                     ("PATCH", Route + "/"), ("DELETE", Route + "/"),
                     ("GET", Route + "/x"), ("POST", $"{Route}/{id}/"), ("TRACE", Route + "/")
                 })
        {
            var r = run.Check(await SendJsonAsync(PenTestUser.WithPermission, method, target, WithId(World.A, id)));
            run.Expect(r.Status is 404 or 405 or 400, r, "VERB", $"{method} {target} accepted");
        }

        foreach (var header in new[] { "X-HTTP-Method-Override", "X-HTTP-Method", "X-Method-Override" })
        {
            var r = run.Check(await As(PenTestUser.WithPermission)
                .SendAsync("POST", $"{Route}/{id}", null, null, (header, "DELETE")));
            run.Expect(r.Status is 404 or 405, r, "OVERRIDE", "method override honoured");
        }

        run.Expect(await StateAsync() == before, new PenTestResponse { Request = "state" }, "VERB",
            "a wrong verb changed rows");
        run.AssertClean(Output);
    }
}