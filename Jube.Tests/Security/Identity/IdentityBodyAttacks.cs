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
using Jube.Test.Security.PenTest;

namespace Jube.Test.Security.Identity;

public static class IdentityBodyAttacks
{
    private static readonly TimeSpan slow = TimeSpan.FromSeconds(20);

    public static IReadOnlyList<(string Name, string Json)> StructuralBodies { get; } =
    [
        ("array-root", "[]"), ("string-root", "\"x\""), ("null-root", "null"), ("number-root", "1"),
        ("true-root", "true"), ("nested-deep", string.Concat(Enumerable.Repeat("{\"a\":", 300)) + "1" +
                                               new string('}', 300)),
        ("truncated", "{\"name\":\"n1\",\"email\""), ("trailing-comma", "{\"name\":\"n1\",}"),
        ("comments", "{/*c*/\"name\":\"n1\"}"), ("single-quotes", "{'name':'n1'}"),
        ("unquoted-key", "{name:1}"), ("nan", "{\"id\":NaN}"), ("infinity", "{\"id\":Infinity}"),
        ("hex", "{\"id\":0x10}"), ("leading-zero", "{\"id\":007}"), ("plus", "{\"id\":+1}"),
        ("lone-surrogate", "{\"name\":\"\\ud800\"}"), ("bad-escape", "{\"name\":\"\\q\"}"),
        ("control-char", "{\"name\":\"a\u0001b\"}"), ("empty-key", "{\"\":1}"),
        ("id-string", "{\"id\":\"abc\"}"), ("id-huge", "{\"id\":99999999999999999999}"),
        ("id-exp", "{\"id\":1e999}"), ("id-fraction", "{\"id\":1.5}"), ("id-array", "{\"id\":[1]}"),
        ("id-object", "{\"id\":{\"$gt\":0}}"), ("id-bool", "{\"id\":true}"), ("id-null-string", "{\"id\":\"null\"}")
    ];

    public static async Task RunAsync(PenTestRun run, PenTestClient client,
        IReadOnlyList<(string Method, string Target)> targets, IEnumerable<(string Name, string Json)> extraBodies,
        object validBody, bool emptyObjectIsValid = false)
    {
        var bodies = StructuralBodies.Concat(extraBodies).ToList();
        await run.ForEachAsync([.. bodies.SelectMany(b => targets.Select(t => (b, t)))], async item =>
        {
            var response = await client.SendAsync(item.t.Method, item.t.Target, Encoding.UTF8.GetBytes(item.b.Json),
                "application/json");
            run.Check(IdentityPenTestBase.Scrub(response, ""), slow);
            run.Expect(response.Status is >= 400 and < 500 || (emptyObjectIsValid && !response.IsServerError), response,
                "REJECTED", $"{item.b.Name} on {item.t.Method} {item.t.Target}");
        });

        foreach (var attack in PenTestPayloads.JsonAttacks)
        {
            foreach (var (method, target) in targets)
            {
                var response = run.Check(
                    IdentityPenTestBase.Scrub(await client.SendAsync(method, target, attack.Bytes, attack.ContentType),
                        ""),
                    slow);
                run.Expect(response.Status is >= 400 and < 500 || (emptyObjectIsValid && !response.IsServerError),
                    response, "REJECTED", $"{attack.Name} on {method} {target}");
            }
        }

        var valid = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(validBody,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
        foreach (var contentType in new[]
                 {
                     "text/plain", "application/xml", "application/x-www-form-urlencoded",
                     "multipart/form-data; boundary=x", "application/jsonx", "text/json", null
                 })
        {
            foreach (var (method, target) in targets)
            {
                var response =
                    run.Check(IdentityPenTestBase.Scrub(await client.SendAsync(method, target, valid, contentType), ""),
                        slow);
                run.Expect(response.Status is >= 400 and < 500, response, "CONTENT-TYPE",
                    $"{contentType ?? "none"} accepted on {method} {target}");
            }
        }

        var encodings = new (string Name, byte[] Bytes)[]
        {
            ("bom", [.. Encoding.UTF8.GetPreamble(), .. valid]),
            ("utf16", Encoding.Unicode.GetBytes(Encoding.UTF8.GetString(valid))),
            ("invalid-utf8", [.. valid, 0xFF, 0xFE]),
            ("gzip-lie", valid)
        };
        foreach (var (name, bytes) in encodings)
        {
            foreach (var (method, target) in targets)
            {
                var headers = name == "gzip-lie" ? new[] { ("Content-Encoding", "gzip") } : [];
                var response =
                    run.Check(
                        IdentityPenTestBase.Scrub(
                            await client.SendAsync(method, target, bytes, "application/json", headers), ""), slow);
                run.Expect(!response.IsServerError, response, "STATUS", $"{name} on {method} {target}");
            }
        }
    }
}