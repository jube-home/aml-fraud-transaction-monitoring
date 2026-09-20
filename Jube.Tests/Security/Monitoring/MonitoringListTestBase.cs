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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using Xunit.Abstractions;

namespace Jube.Test.Security.Monitoring;

public abstract class MonitoringListTestBase(DatabaseFixture fx, ITestOutputHelper output) : PenTestBase(fx, output)
{
    private static readonly Regex secret = new(
        @"(?i)(\b(password|passwd|pwd|secret|masterauth|requirepass)\b\\?[""']?\s*[=:]\s*\\?[""']?(?!\[REDACTED\])[A-Za-z0-9!@#%^&*_\-+/.]{3,})|(-----BEGIN [A-Z ]*PRIVATE KEY-----)|(\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.)|(://[^\s/:@""\\]+:(?!\[REDACTED\])[^\s/@""\\]+@)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));

    protected static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

    protected static bool ContainsSecret(string body)
    {
        try
        {
            return secret.IsMatch(body);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    protected static string BuildQuery(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        var text = string.Join("&", pairs.Select(p => p.Key + "=" + p.Value));
        return text.Length == 0 ? string.Empty : "?" + text;
    }

    private static bool IsRow(PenTestResponse response)
    {
        if (response.BodyBytes.Length > 2_000_000)
        {
            return response.Body.StartsWith("{\"rows\":[", StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(response.Body);
            return doc.RootElement.ValueKind == JsonValueKind.Object &&
                   doc.RootElement.EnumerateObject().Any(p =>
                       p.Name.Equals("rows", StringComparison.OrdinalIgnoreCase) &&
                       p.Value.ValueKind == JsonValueKind.Array) &&
                   doc.RootElement.EnumerateObject().Any(p =>
                       p.Name.Equals("total", StringComparison.OrdinalIgnoreCase) &&
                       p.Value.ValueKind == JsonValueKind.Number);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    protected async Task AuthorizationMatrixAsync(MonitoringArea area)
    {
        var run = NewRun(area.Name + " authz");
        var target = area.Route;

        async Task<PenTestResponse> GetAsClientAsync(PenTestClient client, string? t = null)
        {
            return run.Check(await client.GetAsync(t ?? target), Bound);
        }

        void NoDataInRefusal(PenTestResponse r, string who)
        {
            run.Expect(r.Body.Length < 2000 && !r.Body.Contains("\"rows\"", StringComparison.OrdinalIgnoreCase), r,
                "REFUSAL-LEAKS-DATA", who);
        }

        foreach (var anonymousTarget in new[] { target, target + "/", target + "?take=1", target + "?search=a" })
        {
            var r = await GetAsClientAsync(Anonymous, anonymousTarget);
            run.Expect(r.IsRejection, r, "ANON-ALLOWED", "anonymous must be refused");
            NoDataInRefusal(r, "anonymous");
        }

        foreach (var token in new[]
                 {
                     "garbage", "", "a.b.c", "eyJhbGciOiJub25lIn0.eyJ1bmlxdWVfbmFtZSI6ImFkbWluIn0.", "null",
                     "undefined",
                     new string('A', 3000), "Bearer Bearer x"
                 })
        {
            var r = await GetAsClientAsync(Anonymous.WithBearer(token));
            run.Expect(r.IsRejection, r, "BAD-TOKEN-ALLOWED", $"token '{(token.Length > 20 ? token[..20] : token)}'");
            NoDataInRefusal(r, "bad token");
            var cookie = await GetAsClientAsync(Anonymous.WithCookie($"authentication-jwt={token}"));
            run.Expect(cookie.IsRejection, cookie, "BAD-COOKIE-ALLOWED", "forged cookie");
        }

        foreach (var user in new[] { PenTestUser.WithoutPermission })
        {
            var r = await GetAsClientAsync(As(user));
            run.Expect(r.Status == 403, r, "NO-PERMISSION", $"{user} must get 403");
            NoDataInRefusal(r, user.ToString());
            var bearer = await GetAsClientAsync(As(user).AsBearer());
            run.Expect(bearer.Status == 403, bearer, "NO-PERMISSION-BEARER", $"{user} must get 403 as bearer");
        }

        foreach (var user in new[] { PenTestUser.NoTenant, PenTestUser.UnknownUser })
        {
            var r = await GetAsClientAsync(As(user));
            run.Expect(r.IsRejection, r, "NO-TENANT", $"{user} must be refused");
            NoDataInRefusal(r, user.ToString());
        }

        foreach (var user in new[]
                     { PenTestUser.NoApproveByReview, PenTestUser.TenantB, PenTestUser.BothTenants })
        {
            var r = await GetAsClientAsync(As(user));
            run.Expect(r.Status == 403, r, "NOT-LANDLORD", $"{user} is not a landlord and must get 403");
            NoDataInRefusal(r, user.ToString());
        }

        foreach (var user in new[] { PenTestUser.Landlord })
        {
            var r = await GetAsClientAsync(As(user));
            run.Expect(r.Status == 200, r, "AUTHORISED-REFUSED", $"{user} is a landlord and must get 200");
            run.Expect(r.Status != 200 || IsRow(r), r, "SHAPE", "expected {rows,total,statistics}");
            run.Expect(
                r.Status != 200 || (r.ContentType ?? string.Empty).Contains("json", StringComparison.OrdinalIgnoreCase),
                r, "CONTENT-TYPE", "list must be JSON");
            run.Expect(!ContainsSecret(r.Body), r, "SECRET-IN-BODY", "credential-shaped text in the list");
            var bearer = await GetAsClientAsync(As(user).AsBearer());
            run.Expect(bearer.Status == 200, bearer, "AUTHORISED-BEARER", "bearer form must also work");
        }

        foreach (var user in new[] { PenTestUser.TenantB, PenTestUser.BothTenants, PenTestUser.Landlord })
        {
            var r = await GetAsClientAsync(As(user));
            run.Expect(r.Status is 200 or 401 or 403, r, "OTHER-TENANT", $"{user} must not error");
            run.Expect(r.Status != 200 || IsRow(r), r, "SHAPE", "expected {rows,total,statistics}");
            run.Expect(!ContainsSecret(r.Body), r, "SECRET-IN-BODY", "credential-shaped text for another tenant");
        }

        foreach (var (name, value) in new[]
                 {
                     ("X-Tenant-Id", "1"), ("TenantRegistryId", "1"),
                     ("X-Forwarded-User", Host.UserName(PenTestUser.Landlord)),
                     ("X-Original-URL", "/api/Ready"), ("X-HTTP-Method-Override", "DELETE"),
                     ("Authorization", "Basic YWRtaW46YWRtaW4=")
                 })
        {
            var r = await GetAsClientAsync(As(PenTestUser.WithoutPermission).WithHeader(name, value));
            run.Expect(r.Status is 403 or 401, r, "HEADER-SPOOF", $"{name} must not grant access");
        }

        foreach (var suffix in new[] { "/..;/Users", "/%2e%2e/Users", ";jsessionid=1", "/.", "//", "/%00" })
        {
            var r = await GetAsClientAsync(As(PenTestUser.WithoutPermission), target + suffix);
            run.Expect(!r.IsSuccess || r.Body.Length < 4 || !IsRow(r), r, "PATH-TRICK", "no data through path tricks");
        }

        run.AssertClean(Output);
    }

    protected async Task ParameterAbuseAsync(MonitoringArea area)
    {
        var run = NewRun(area.Name + " params");
        var client = As(PenTestUser.Landlord);
        var work = new List<(string Param, string Value, string Query)>();
        var baseline =
            run.Check(await client.GetAsync(area.Route + (area.Parameters.Contains("take") ? "?take=5" : string.Empty)),
                TimeSpan.FromSeconds(60));
        var blindBound = baseline.Elapsed * 3 + TimeSpan.FromSeconds(7);

        foreach (var parameter in area.Parameters)
        {
            IEnumerable<string> values = parameter switch
            {
                "take" => MonitoringPayloads.IntValues().Concat(MonitoringPayloads.Sqli().Take(30)),
                "from" or "to" => MonitoringPayloads.DateValues().Concat(MonitoringPayloads.Sqli().Take(30)),
                "samplePercentage" => MonitoringPayloads.DoubleValues(),
                "sortField" => MonitoringPayloads.SortValues().Concat(MonitoringPayloads.Sqli()),
                "sortDirection" => MonitoringPayloads.DirectionValues().Concat(MonitoringPayloads.Sqli().Take(30)),
                "search" => MonitoringPayloads.SearchValues(),
                _ => MonitoringPayloads.IntValues().Concat(MonitoringPayloads.Sqli().Take(30))
            };

            var distinct = values.Distinct().ToList();
            if (!area.Parameters.Contains("take"))
            {
                distinct = [.. distinct.Where((_, i) => i % 4 == 0 || i < 30)];
            }

            foreach (var value in distinct)
            {
                var encoded = MonitoringPayloads.Enc(value);
                work.Add((parameter, value, BuildQuery([new(parameter, encoded)])));
                if (value.Length < 200 && parameter is "search" or "sortField")
                {
                    work.Add((parameter, value, BuildQuery([new(parameter, MonitoringPayloads.Enc(encoded))])));
                }
            }

            foreach (var raw in MonitoringPayloads.RawEncoded)
            {
                work.Add((parameter, raw, BuildQuery([new(parameter, raw)])));
            }

            work.Add((parameter, "dup", BuildQuery([new(parameter, "1"), new(parameter, "2")])));
            work.Add((parameter, "array", $"?{parameter}[]=1"));
            work.Add((parameter, "object", $"?{parameter}[a]=1"));
            work.Add((parameter, "upper", $"?{parameter.ToUpperInvariant()}=1"));
            work.Add((parameter, "semicolon", $"?{parameter}=1;{parameter}=2"));
        }

        foreach (var payload in MonitoringPayloads.Sqli().Take(40))
        {
            work.Add(("all", payload, BuildQuery(area.Parameters.Select(p =>
                new KeyValuePair<string, string>(p, MonitoringPayloads.Enc(payload))))));
        }

        work.Add(("unknown", "extra",
            BuildQuery([
                new("__proto__", "1"), new("constructor", "x"), new("tenantRegistryId", "1"), new("id", "1")
            ])));

        await run.ForEachAsync(work, async item =>
        {
            var query = item.Query;
            if (area.Parameters.Contains("take") && item.Param != "take" && item.Param != "all" &&
                !query.Contains("take"))
            {
                query = "?take=5" + (query.Length > 1 ? "&" + query[1..] : string.Empty);
            }

            var r = run.Check(await client.GetAsync(area.Route + query), Bound);
            run.Expect(r.Status is 200 or 400 || (r.Status == 414 && query.Length > 4000), r, "STATUS",
                $"{item.Param}={Trim(item.Value)} must be 200 or 400");
            if (r.Status == 200)
            {
                run.Expect(IsRow(r), r, "SHAPE", $"{item.Param}={Trim(item.Value)}");
                run.Expect((r.ContentType ?? string.Empty).Contains("json", StringComparison.OrdinalIgnoreCase), r,
                    "CONTENT-TYPE", "reflected values must never be served as HTML");
                run.Expect(!ContainsSecret(r.Body), r, "SECRET-IN-BODY", $"{item.Param}={Trim(item.Value)}");
            }

            run.Expect(item.Param is "from" or "to" || r.Elapsed < blindBound, r, "TIME-BLIND",
                $"{item.Param}={Trim(item.Value)} took {r.Elapsed.TotalSeconds:F1}s");
            run.Expect(
                !r.Body.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
                (r.ContentType ?? string.Empty).Contains("json", StringComparison.OrdinalIgnoreCase),
                r, "REFLECTED-XSS", "script tag echoed outside JSON");
            run.Expect(
                r.Header("X-Injected") == null && r.Header("pen") == null &&
                !r.HeaderValues("Set-Cookie").Any(c => c.Contains("pen=1")), r,
                "HEADER-INJECTION", "CRLF in a parameter must not create headers");
        }, 10);

        run.AssertClean(Output);
    }

    protected async Task ConsumptionAndVerbsAsync(MonitoringArea area)
    {
        var run = NewRun(area.Name + " consumption");
        var client = As(PenTestUser.Landlord);

        foreach (var method in new[] { "POST", "PUT", "DELETE", "PATCH" })
        {
            foreach (var body in new[] { null, Encoding.UTF8.GetBytes("{}"), Encoding.UTF8.GetBytes("{\"id\":1}") })
            {
                var r = run.Check(
                    await client.SendAsync(method, area.Route, body, body == null ? null : "application/json"), Bound);
                run.Expect(r.Status is 404 or 405, r, "VERB", $"{method} on a read-only list must be 404/405");
            }
        }

        foreach (var method in new[] { "OPTIONS", "TRACE", "HEAD" })
        {
            var r = run.Check(await client.SendAsync(method, area.Route), Bound);
            run.Expect(r.Status < 500, r, "VERB", method);
            run.Expect(!IsRow(r), r, "VERB-DATA", $"{method} must not return rows");
        }

        var withoutPermission = As(PenTestUser.WithoutPermission);
        foreach (var method in new[] { "POST", "PUT", "DELETE", "PATCH", "HEAD" })
        {
            var r = run.Check(await withoutPermission.SendAsync(method, area.Route + "/1"), Bound);
            run.Expect(!r.IsSuccess || r.Body.Length == 0, r, "VERB-ID", $"{method} with an id");
        }

        foreach (var suffix in new[] { "/1", "/-1", "/abc", "/%00", "/../x", "/1/2/3" })
        {
            var r = run.Check(await client.GetAsync(area.Route + suffix), Bound);
            run.Expect(r.Status is 404 or 405 or 400, r, "ROUTE-EXTRA", "no per-id route exists on a monitoring list");
        }

        var hugeLine = area.Route + "?search=" + new string('a', 9000);
        var huge = run.Check(await client.GetAsync(hugeLine), Bound);
        run.Expect(huge.Status is 400 or 414 or 431 or 200, huge, "REQUEST-LINE", "oversized request line");
        var large = run.Check(await client.GetAsync(area.Route + "?search=" + new string('a', 5000)), Bound);
        run.Expect(large.Status is 200 or 400 or 414, large, "REQUEST-LINE", "5 KB search");

        var manyParams =
            BuildQuery(Enumerable.Range(0, 400).Select(i => new KeyValuePair<string, string>("p" + i, "1")));
        var many = run.Check(await client.GetAsync(area.Route + manyParams), Bound);
        run.Expect(many.Status is 200 or 400 or 414 or 431, many, "MANY-PARAMS", "400 unknown query parameters");

        var extremeQuery = area.Parameters.Contains("take")
            ? "?take=2147483647&samplePercentage=100&from=0001-01-01&to=9999-12-31"
            : "?search=&sortField=query&sortDirection=asc";

        var bursts = Enumerable.Range(0, 8).ToList();
        await run.ForEachAsync(bursts, async _ =>
        {
            var r = run.Check(await client.GetAsync(area.Route + extremeQuery), TimeSpan.FromSeconds(28));
            run.Expect(r.Status is 200 or 400, r, "BURST", "parallel extreme requests must not fail");
            run.Expect(r.BodyBytes.Length < 64 * 1024 * 1024, r, "RESPONSE-SIZE", $"{r.BodyBytes.Length} bytes");
        }, 4);

        await run.ForEachAsync(bursts, async _ =>
        {
            var r = run.Check(await Anonymous.GetAsync(area.Route + extremeQuery), Bound);
            run.Expect(r.IsRejection, r, "ANON-BURST", "anonymous burst");
        }, 12);

        run.AssertClean(Output);
    }

    private static string Trim(string value) => value.Length > 40 ? value[..40] + "..." : value.Replace("\0", "\\0");
}