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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using Jube.Test.Security.QueryExhaustive.Models;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Security.QueryExhaustive
{
    public abstract partial class ChartExhaustiveBase(DatabaseFixture fx, ITestOutputHelper output)
        : PenTestBase(fx, output)
    {
        private const int NonExistentId = 2147479999;
        private const int MaxBodyBytes = 1024 * 1024;
        private const string SvgNamespace = "http://www.w3.org/2000/svg";
        private const string EmptyMessage = "No data available";

        private static readonly string[] forbiddenElements =
        [
            "script", "foreignobject", "style", "iframe", "object", "embed", "image", "audio", "video", "link", "meta",
            "base", "animate", "set", "animatetransform", "animatemotion", "handler", "listener"
        ];

        private static readonly Regex chartIdPattern = MyChartIdRegex();
        private ChartHostileSeed? hostileOrNull;
        private QueryExhaustiveWorld? worldOrNull;

        protected QueryExhaustiveWorld World
        {
            get => worldOrNull.Required();
            private set => worldOrNull = value;
        }

        protected ChartHostileSeed Hostile
        {
            get => hostileOrNull.Required();
            private set => hostileOrNull = value;
        }

        protected abstract string RouteName { get; }
        protected abstract int IdOf(Set set);

        protected virtual int? SecondIdOf(Set set)
        {
            return null;
        }

        protected virtual bool AllowsPost => false;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            World = await QueryExhaustiveWorld.CreateAsync(Fx);
            Hostile = await ChartHostileSeed.CreateAsync(Fx, World.A);
        }

        public override async Task DisposeAsync()
        {
            if (hostileOrNull != null)
            {
                await hostileOrNull.DisposeAsync(Fx);
            }

            if (worldOrNull != null)
            {
                await worldOrNull.DisposeAsync(Fx);
            }
        }

        protected string PathFor(int id, int? second = null)
        {
            var path = $"/api/Chart/Exhaustive/{RouteName}/{id.ToString(CultureInfo.InvariantCulture)}";

            return second == null ? path : path + "/" + second.Value.ToString(CultureInfo.InvariantCulture);
        }

        protected string PathOf(Set set)
        {
            return PathFor(IdOf(set), SecondIdOf(set));
        }

        protected string UnknownPath()
        {
            return PathFor(NonExistentId, SecondIdOf(World.A) == null ? null : NonExistentId - 1);
        }

        protected string SameWidthUnknownPath(Set set)
        {
            var id = IdOf(set);
            var second = SecondIdOf(set);
            var idText = id.ToString(CultureInfo.InvariantCulture);

            if (idText.Length < 5 || second is < 10000)
            {
                return UnknownPath();
            }

            var unknownId = int.Parse(new string('9', idText.Length), CultureInfo.InvariantCulture) - 13;

            return PathFor(unknownId,
                second == null
                    ? null
                    : int.Parse(new string('9', second.Value.ToString(CultureInfo.InvariantCulture).Length),
                        CultureInfo.InvariantCulture) - 13);
        }

        protected Task<PenTestResponse> GetAsync(PenTestUser user, string path)
        {
            return As(user).GetAsync(path);
        }

        [GeneratedRegex("ex-[a-z]+-\\d+(?:-\\d+)?", RegexOptions.CultureInvariant)]
        private static partial Regex MyChartIdRegex();

        protected static string Normalised(string body)
        {
            return chartIdPattern.Replace(body, "ex-x");
        }

        protected static bool Same(PenTestResponse a, PenTestResponse b)
        {
            return a.Status == b.Status && Normalised(a.Body) == Normalised(b.Body);
        }

        protected static bool IsEmptyState(PenTestResponse response)
        {
            return response.Body.Contains(EmptyMessage, StringComparison.Ordinal);
        }

        protected static IEnumerable<string> SvgViolations(string body)
        {
            XDocument document;

            try
            {
                using var reader = XmlReader.Create(new StringReader(body),
                    new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });

                document = XDocument.Load(reader);
            }
            catch (XmlException e)
            {
                return [$"body is not well-formed XML: {e.Message}"];
            }

            var violations = new List<string>();

            if (document.Root is not { } root || root.Name.LocalName != "svg" ||
                root.Name.NamespaceName != SvgNamespace)
            {
                violations.Add("root element is not svg in the SVG namespace");

                return violations;
            }

            foreach (var element in document.Descendants().Prepend(root))
            {
                var local = element.Name.LocalName.ToLowerInvariant();

                if (forbiddenElements.Contains(local))
                {
                    violations.Add($"forbidden element <{element.Name.LocalName}>");
                }

                foreach (var attribute in element.Attributes())
                {
                    var name = attribute.Name.LocalName;
                    var value = attribute.Value;
                    var isNamespaceDeclaration = attribute.IsNamespaceDeclaration;

                    if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) && !isNamespaceDeclaration)
                    {
                        violations.Add($"event handler attribute {name}");
                    }

                    if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add("inline style attribute");
                    }

                    if (name.Equals("href", StringComparison.OrdinalIgnoreCase) && !value.StartsWith('#'))
                    {
                        violations.Add($"non fragment href {value}");
                    }

                    if (isNamespaceDeclaration)
                    {
                        continue;
                    }

                    if (Regex.IsMatch(value, @"(?i)javascript:|data:|vbscript:|https?:|//|@import|expression\(") ||
                        Regex.IsMatch(value, @"(?i)url\(\s*['""]?(?!#)"))
                    {
                        violations.Add($"external reference or script scheme in {name}={value}");
                    }
                }
            }

            var lower = body.ToLowerInvariant();

            foreach (var raw in new[] { "<script", "foreignobject", "<!doctype", "<!entity", "<img", "<svg onload" })
            {
                if (lower.Contains(raw, StringComparison.Ordinal))
                {
                    violations.Add($"body contains {raw}");
                }
            }

            if (body.Contains("NaN", StringComparison.Ordinal) || body.Contains("Infinity", StringComparison.Ordinal))
            {
                violations.Add("NaN or Infinity leaked into the SVG");
            }

            return violations;
        }

        protected void CheckSvg(PenTestRun run, PenTestResponse response, string rule)
        {
            run.Expect(response.Status == 200, response, rule + "-STATUS", $"expected 200 got {response.Status}");

            run.Expect(
                (response.ContentType ?? string.Empty).StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase),
                response, rule + "-CONTENT-TYPE", response.ContentType ?? "none");

            run.Expect(string.Equals(response.Header("X-Content-Type-Options"), "nosniff",
                StringComparison.OrdinalIgnoreCase), response, rule + "-NOSNIFF", "missing nosniff");

            var policies = response.HeaderValues("Content-Security-Policy").ToList();

            run.Expect(policies.Count == 1 && policies[0] == "default-src 'none'", response, rule + "-CSP",
                string.Join(" | ", policies));

            run.Expect(string.Equals(response.Header("Cache-Control"), "no-store", StringComparison.OrdinalIgnoreCase),
                response, rule + "-CACHE", response.Header("Cache-Control") ?? "none");

            run.Expect(response.HeaderValues("Set-Cookie").All(c =>
                    c.StartsWith("authentication-jwt=", StringComparison.Ordinal) ||
                    c.StartsWith("authentication-expiry=", StringComparison.Ordinal)),
                response, rule + "-COOKIE", "chart sets a cookie other than the session refresh");

            run.Expect(response.BodyBytes.Length < MaxBodyBytes, response, rule + "-SIZE",
                $"{response.BodyBytes.Length} bytes");

            foreach (var violation in SvgViolations(response.Body))
            {
                run.Fail(response, rule + "-SVG", violation);
            }
        }

        [Fact]
        public async Task AuthorisedUser_ReadsOwnDataAsSafeSvgAsync()
        {
            var run = NewRun($"{RouteName} own data");

            foreach (var (user, set, other) in new[]
                     {
                         (PenTestUser.WithPermission, World.A, World.B), (PenTestUser.TenantB, World.B, World.A)
                     })
            {
                var response = run.Check(await GetAsync(user, PathOf(set)));
                CheckSvg(run, response, "OWN");

                run.Expect(!IsEmptyState(response), response, "OWN-DATA",
                    $"{user} received an empty chart for its own data");

                run.Expect(!response.Body.Contains($"Qx{other.Tag}", StringComparison.Ordinal), response,
                    "OWN-FOREIGN-DATA", $"{user} received the other tenant's variable names");

                foreach (var marker in new[]
                         {
                             QueryExhaustiveWorld.DeletedVariableMarker, QueryExhaustiveWorld.RemovedVariableMarker
                         })
                {
                    run.Expect(!response.Body.Contains(marker, StringComparison.Ordinal), response, "OWN-SOFT-DELETED",
                        $"soft-deleted or removed marker {marker} returned");
                }
            }

            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync()
        {
            var run = NewRun($"{RouteName} authn/authz");
            var path = PathOf(World.A);

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
                var response = run.Check(await item.Client.GetAsync(path));

                run.Expect(response.IsRejection || response.Status == 403, response, "AUTHZ-NOT-REFUSED",
                    $"{item.Who} was not refused");

                run.Expect(!response.Body.Contains("<svg", StringComparison.Ordinal), response, "AUTHZ-LEAK",
                    $"{item.Who} received a chart");

                run.Expect(response.Header("X-Content-Type-Options") != null || response.IsRedirect ||
                           response.Status is 401 or 403, response, "AUTHZ-SHAPE");
            });

            var anonymous = run.Check(await Anonymous.GetAsync(path));
            var withoutPermission = run.Check(await As(PenTestUser.WithoutPermission).GetAsync(path));

            run.Expect(anonymous.Status is 401 or 403 or 302, anonymous, "AUTHZ-ANON-STATUS",
                anonymous.Status.ToString());

            run.Expect(withoutPermission.Status == 403, withoutPermission, "AUTHZ-NOPERM-STATUS",
                withoutPermission.Status.ToString());

            run.Expect(withoutPermission.BodyBytes.Length == 0 || !withoutPermission.Body.Contains("Exception"),
                withoutPermission, "AUTHZ-NOPERM-BODY");

            foreach (var user in new[] { PenTestUser.Landlord, PenTestUser.BothTenants, PenTestUser.NoApproval })
            {
                var response = run.Check(await GetAsync(user, path));
                run.Expect(!response.IsServerError, response, "AUTHZ-5XX", user.ToString());
            }

            var bearer = run.Check(await As(PenTestUser.WithPermission).AsBearer().GetAsync(path));
            run.Expect(bearer.Status == 200, bearer, "BEARER-AUTHORISED", "valid bearer token should be accepted");

            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync()
        {
            var run = NewRun($"{RouteName} cross tenant");
            var probes = new List<(PenTestUser User, string What, Set Set)>();

            foreach (var set in new[] { World.A, World.Edge, World.Deleted, World.Empty })
            {
                probes.Add((PenTestUser.TenantB, $"tenant B reads tenant A set {set.Tag}", set));
            }

            probes.Add((PenTestUser.WithPermission, "tenant A reads tenant B set", World.B));
            probes.Add((PenTestUser.WithPermission, "tenant A reads its own soft deleted set", World.Deleted));
            probes.Add((PenTestUser.WithPermission, "tenant A reads its own set without a promoted trial",
                World.Empty));

            await run.ForEachAsync(probes, async probe =>
            {
                var baseline = run.Check(await GetAsync(probe.User, UnknownPath()));
                CheckSvg(run, baseline, "BASELINE");
                run.Expect(IsEmptyState(baseline), baseline, "BASELINE-EMPTY", "unknown id is not the empty state");

                var response = run.Check(await GetAsync(probe.User, PathOf(probe.Set)));

                run.Expect(Same(response, baseline), response, "IDOR-DISTINGUISHABLE",
                    $"{probe.What}: differs from a non-existent id");

                var forbiddenTag = probe.User == PenTestUser.TenantB ? World.A.Tag : World.B.Tag;

                run.Expect(!response.Body.Contains($"Qx{forbiddenTag}", StringComparison.Ordinal), response,
                    "IDOR-LEAK",
                    probe.What);

                var wide = SameWidthUnknownPath(probe.Set);

                if (wide != UnknownPath())
                {
                    var wideBaseline = run.Check(await GetAsync(probe.User, wide));

                    run.Expect(wideBaseline.BodyBytes.Length == response.BodyBytes.Length, response, "IDOR-SIZE",
                        $"{probe.What}: {response.BodyBytes.Length} bytes against {wideBaseline.BodyBytes.Length}");
                }
            });

            var foreign = new List<double>();
            var missing = new List<double>();

            for (var i = 0; i < 7; i++)
            {
                foreign.Add((await GetAsync(PenTestUser.TenantB, PathOf(World.A))).Elapsed.TotalMilliseconds);
                missing.Add((await GetAsync(PenTestUser.TenantB, UnknownPath())).Elapsed.TotalMilliseconds);
            }

            var delta = Math.Abs(foreign.Order().ElementAt(3) - missing.Order().ElementAt(3));

            run.Expect(delta < 1500, await GetAsync(PenTestUser.TenantB, PathOf(World.A)), "IDOR-TIMING",
                $"median difference {delta:F0}ms between foreign and missing");

            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync()
        {
            var run = NewRun($"{RouteName} enumeration");
            var second = SecondIdOf(World.A);
            var ids = World.All.Select(IdOf).Where(i => i > 0).Distinct().ToList();

            var candidates = ids.SelectMany(i => Enumerable.Range(-6, 13).Select(d => i + d)).Where(i => i > 0)
                .Distinct().ToList();

            await run.ForEachAsync([PenTestUser.WithPermission, PenTestUser.TenantB], async user =>
            {
                var own = user == PenTestUser.WithPermission ? World.A : World.B;
                var forbidden = user == PenTestUser.WithPermission ? World.B : World.A;
                var baseline = run.Check(await GetAsync(user, UnknownPath()));
                var allowed = new HashSet<string> { Normalised(baseline.Body) };

                foreach (var set in new[] { own, World.Edge, World.Deleted, World.Empty })
                {
                    if (user == PenTestUser.WithPermission || set == own)
                    {
                        allowed.Add(Normalised((await GetAsync(user, PathOf(set))).Body));
                    }
                }

                var forbiddenBody = Normalised((await As(user == PenTestUser.WithPermission
                    ? PenTestUser.TenantB
                    : PenTestUser.WithPermission).GetAsync(PathOf(forbidden))).Body);

                await run.ForEachAsync(candidates, async id =>
                {
                    var response = run.Check(await GetAsync(user, PathFor(id, second == null ? null : id)));
                    run.Expect(response.Status == 200, response, "ENUM-STATUS-NOT-UNIFORM", $"id {id}");
                    var normalised = Normalised(response.Body);

                    run.Expect(allowed.Contains(normalised) || normalised != forbiddenBody,
                        response, "ENUM-FOREIGN-DATA", $"id {id} returned the other tenant's chart");

                    run.Expect(!response.Body.Contains($"Qx{forbidden.Tag}", StringComparison.Ordinal), response,
                        "ENUM-FOREIGN-NAMES", $"id {id}");

                    CheckSvg(run, response, "ENUM");
                });
            }, 2);

            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync()
        {
            var run = NewRun($"{RouteName} empty");
            var unknown = run.Check(await GetAsync(PenTestUser.WithPermission, UnknownPath()));
            var missingPromoted = run.Check(await GetAsync(PenTestUser.WithPermission, PathOf(World.Empty)));
            var deleted = run.Check(await GetAsync(PenTestUser.WithPermission, PathOf(World.Deleted)));

            var otherSecond = SecondIdOf(World.A) == null
                ? unknown
                : run.Check(await GetAsync(PenTestUser.WithPermission,
                    PathFor(IdOf(World.A), NonExistentId)));

            foreach (var response in new[] { unknown, missingPromoted, deleted, otherSecond })
            {
                CheckSvg(run, response, "EMPTY");
                run.Expect(IsEmptyState(response), response, "EMPTY-MESSAGE", "the empty state text is missing");
            }

            run.Expect(Same(unknown, missingPromoted), missingPromoted, "EMPTY-PROMOTED-DIFFERS",
                "an instance with no promoted trial differs from an unknown id");
            run.Expect(Same(unknown, deleted), deleted, "EMPTY-DELETED-DIFFERS",
                "a soft deleted instance differs from an unknown id");
            run.Expect(Same(unknown, otherSecond), otherSecond, "EMPTY-SECOND-DIFFERS",
                "an unknown second id differs from an unknown id");

            run.Expect(unknown.BodyBytes.Length < 8192, unknown, "EMPTY-SIZE", $"{unknown.BodyBytes.Length} bytes");

            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync()
        {
            var run = NewRun($"{RouteName} numeric edge cases");
            var response = run.Check(await GetAsync(PenTestUser.WithPermission, PathOf(World.Edge)));
            CheckSvg(run, response, "EDGE");
            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task ConcurrentReads_AreDeterministicAndBoundedAsync()
        {
            var run = NewRun($"{RouteName} concurrency");
            var path = PathOf(World.A);
            var first = run.Check(await GetAsync(PenTestUser.WithPermission, path));
            var responses = new ConcurrentBag<PenTestResponse>();

            await run.ForEachAsync(Enumerable.Range(0, 48),
                async _ =>
                {
                    responses.Add(run.Check(await GetAsync(PenTestUser.WithPermission, path),
                        TimeSpan.FromSeconds(15)));
                },
                12);

            foreach (var response in responses)
            {
                run.Expect(response.Status == first.Status && response.Body == first.Body, response,
                    "CONCURRENT-DIFFERS",
                    "body changed under parallel reads");
            }

            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync()
        {
            var run = NewRun($"{RouteName} id boundaries");
            var before = run.Check(await GetAsync(PenTestUser.WithPermission, PathOf(World.A)));
            var forbidden = $"Qx{World.B.Tag}";

            var raw = new List<string>
            {
                "0", "-0", "+0", "1", "-1", "+1", "01", "0001", "2147483647", "2147483648", "-2147483648",
                "-2147483649",
                "4294967295", "4294967296", "9223372036854775807", "9223372036854775808", "99999999999999999999999999",
                "1.0", "1.5", "1e3", "1E10", "0x10", "0b1", "1_000", "1,000", "NaN", "Infinity", "-Infinity", "null",
                "undefined", "true", "false", "[]", "{}", "[1]", "{%22id%22:1}", "%00", "1%00", "%00 1", "%0a",
                "%0d%0a",
                "1%0d%0aSet-Cookie:%20x=y", "%20", "%20%201", "1%20", "%09", "%C0%AF", "%EF%BB%BF1", "%E2%80%AE1",
                "١٢٣", "１２３", "①", "1.", ".1", "-.1", "1/", "1//", "1/../1", "..", "%2e%2e", "%2e%2e%2f1", "1;1",
                "1/**/", "1%23", "1%3F", "1%3Fid=2", "*", "%", "%%", "%zz", "\\1", "%5c1", "~1", "$1", "${1}", "{{1}}",
                "<1>", "1'", "1\"", "1`", "1)", "1(", "1&id=2", "1#", new('9', 5000), new('1', 300)
            };
            raw.AddRange(QueryExhaustiveBase.GenerateInjectionPayloads());

            var hasSecond = SecondIdOf(World.A) != null;
            var targets = new HashSet<string>();

            foreach (var value in raw)
            {
                var escaped = Uri.EscapeDataString(value);

                foreach (var form in new[] { escaped, Uri.EscapeDataString(escaped) })
                {
                    targets.Add($"/api/Chart/Exhaustive/{RouteName}/{form}" + (hasSecond ? "/1" : string.Empty));

                    if (hasSecond)
                    {
                        targets.Add($"/api/Chart/Exhaustive/{RouteName}/{IdOf(World.A)}/{form}");
                    }
                }
            }

            var user = As(PenTestUser.WithPermission);

            await run.ForEachAsync(targets, async target =>
            {
                var response = run.Check(await user.GetAsync(target), TimeSpan.FromSeconds(4));

                run.Expect(response.Status is 200 or 400 or 404 or 405 or 414 or 431, response, "BOUNDARY-STATUS",
                    $"unexpected status {response.Status}");

                run.Expect(!response.Body.Contains(forbidden, StringComparison.Ordinal), response,
                    "BOUNDARY-FOREIGN-DATA");

                if (response.Status == 200)
                {
                    CheckSvg(run, response, "BOUNDARY");

                    run.Expect(IsEmptyState(response) || response.Body.Contains("<svg", StringComparison.Ordinal),
                        response,
                        "BOUNDARY-BODY");
                }
                else
                {
                    run.Expect(!response.Body.Contains("<svg", StringComparison.Ordinal), response,
                        "BOUNDARY-ERROR-BODY");
                }
            });

            var after = run.Check(await GetAsync(PenTestUser.WithPermission, PathOf(World.A)));
            run.Expect(Same(before, after), after, "BOUNDARY-SIDE-EFFECT", "own chart changed after hostile ids");
            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task HostileQueryStringAndHeaders_NeverChangeTheChartAsync()
        {
            var run = NewRun($"{RouteName} query and headers");
            var path = PathOf(World.A);
            var baseline = run.Check(await GetAsync(PenTestUser.WithPermission, path));
            var foreignId = IdOf(World.B);

            var queries = new List<string>
            {
                $"?id={foreignId}", $"?Id={foreignId}", $"?tenantRegistryId={foreignId}", "?TenantRegistryId=1",
                "?deleted=1", "?Deleted=true", "?limit=2147483647", "?top=0", "?page=999999999", "?format=png",
                "?callback=alert(1)", "?id=1&id=2&id=3", "?user=admin", "?userName=admin", "?style=inline",
                "?styleMode=Inline", "?width=99999&height=99999", "?theme=dark", "?title=%3Cscript%3E", "?%00=%00",
                "?q=%27%20OR%201%3D1--", "?x=" + new string('A', 8000),
                "?" + string.Join("&", Enumerable.Range(0, 300).Select(i => $"p{i}=v{i}")),
                "?access_token=garbage", "#fragment", "?"
            };

            await run.ForEachAsync(queries, async q =>
            {
                var response = run.Check(await As(PenTestUser.WithPermission).GetAsync(path + q),
                    TimeSpan.FromSeconds(4));

                run.Expect(!response.IsServerError, response, "QUERY-5XX");

                if (response.IsSuccess)
                {
                    run.Expect(Same(response, baseline), response, "QUERY-CHANGES-RESULT", q.Length > 60 ? q[..60] : q);
                    CheckSvg(run, response, "QUERY");
                }
            });

            var headers = new List<(string Name, string Value)>
            {
                ("X-Tenant-Id", "1"), ("X-Tenant", foreignId.ToString(CultureInfo.InvariantCulture)),
                ("X-Forwarded-For", "127.0.0.1, 10.0.0.1"), ("X-Forwarded-Host", "evil.example"),
                ("X-Forwarded-Proto", "https"), ("X-Original-URL", PathOf(World.B)), ("X-Rewrite-URL", PathOf(World.B)),
                ("X-HTTP-Method-Override", "DELETE"), ("X-Method-Override", "PUT"), ("X-User", "admin"),
                ("X-Remote-User", World.B.Tag), ("X-Requested-With", "XMLHttpRequest"), ("Accept", "text/csv"),
                ("Accept", "application/json"), ("Accept", "text/html"), ("Accept", "*/*;q=0.1, text/html"),
                ("Accept-Language", "zz-<script>"), ("Accept-Encoding", "gzip, deflate, br, zstd, compress"),
                ("Range", "bytes=0-5"), ("Range", "bytes=-1"), ("If-None-Match", "*"),
                ("If-Modified-Since", "Sat, 01 Jan 2000 00:00:00 GMT"), ("Origin", "https://evil.example"),
                ("Referer", "https://evil.example/"), ("Sec-Fetch-Site", "same-origin"), ("Sec-Fetch-Dest", "document"),
                ("Content-Type", "application/json"), ("User-Agent", "' OR 1=1--"),
                ("User-Agent", "${jndi:ldap://x/a}"),
                ("Host", "evil.example"), ("X-Custom", new string('A', 7000)), ("Cookie", "authentication-jwt=garbage")
            };

            await run.ForEachAsync(headers, async header =>
            {
                var response = run.Check(await As(PenTestUser.WithPermission).SendAsync("GET", path, null, null,
                    [header], TimeSpan.FromSeconds(10)));

                run.Expect(!response.IsServerError, response, "HEADER-5XX", header.Name);
                var text = response.Body + string.Join("|", response.Headers.Select(h => h.Value));

                run.Expect(!text.Contains("evil.example", StringComparison.Ordinal), response, "HEADER-REFLECTED",
                    header.Name);

                if (response.Status == 200 && header.Name is not ("Range" or "Cookie" or "Host"))
                {
                    run.Expect(Same(response, baseline), response, "HEADER-CHANGES-RESULT", header.Name);
                    CheckSvg(run, response, "HEADER");
                }
            });

            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task VerbTampering_OnlyGetIsServedAsync()
        {
            var run = NewRun($"{RouteName} verbs");
            var path = PathOf(World.A);
            var before = run.Check(await GetAsync(PenTestUser.WithPermission, path));

            var bodies = new (string Method, byte[]? Body, string? ContentType)[]
            {
                ("POST", null, null), ("POST", Encoding.UTF8.GetBytes("{\"id\":1,\"deleted\":1}"), "application/json"),
                ("PUT", Encoding.UTF8.GetBytes("{\"Id\":1,\"Deleted\":1,\"Score\":0}"), "application/json"),
                ("PATCH", Encoding.UTF8.GetBytes("[{\"op\":\"remove\",\"path\":\"/\"}]"),
                    "application/json-patch+json"),
                ("DELETE", null, null), ("TRACE", null, null), ("OPTIONS", null, null), ("HEAD", null, null),
                ("PROPFIND", null, null), ("LOCK", null, null), ("GET ", null, null), ("SEARCH", null, null),
                ("PURGE", null, null), ("MOVE", null, null), ("COPY", null, null), ("CONNECT", null, null)
            };

            foreach (var user in new[] { PenTestUser.WithPermission, PenTestUser.Anonymous })
            {
                await run.ForEachAsync(bodies, async b =>
                {
                    var client = user == PenTestUser.Anonymous ? Anonymous : As(user);
                    var response = run.Check(await client.SendAsync(b.Method, path, b.Body, b.ContentType));
                    run.Expect(!response.IsServerError, response, "VERB-5XX", b.Method);

                    var postIsAllowed = AllowsPost && user != PenTestUser.Anonymous
                                                   && b.Method == "POST";

                    run.Expect(
                        !response.IsSuccess || b.Method is "OPTIONS" or "HEAD" || postIsAllowed, response,
                        "VERB-SERVED", $"{b.Method} was served");

                    run.Expect(
                        !response.Body.Contains("<svg", StringComparison.Ordinal) || postIsAllowed,
                        response, "VERB-BODY", $"{b.Method} returned a chart");
                });
            }

            var after = run.Check(await GetAsync(PenTestUser.WithPermission, path));
            run.Expect(Same(before, after), after, "VERB-SIDE-EFFECT", "chart changed after verb tampering");
            await run.AssertCleanAsync(Output);
        }

        [Fact]
        public async Task HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync()
        {
            var run = NewRun($"{RouteName} hostile names");
            var paths = HostilePaths().ToList();

            foreach (var path in paths)
            {
                var response = run.Check(await GetAsync(PenTestUser.WithPermission, path));
                CheckSvg(run, response, "HOSTILE");
                run.Expect(!IsEmptyState(response), response, "HOSTILE-DATA", "hostile variable was not charted");

                run.Expect(!response.Body.Contains("</title><svg", StringComparison.Ordinal), response,
                    "HOSTILE-BREAKOUT");

                if (!SvgViolations(response.Body).Any())
                {
                    var text = XDocument.Parse(response.Body).Root!.DescendantNodes().OfType<XText>()
                        .Select(t => t.Value).ToList();

                    run.Expect(text.Any(t => t.Contains("alert(1)", StringComparison.Ordinal)) ||
                               text.Any(t => t.Contains("Ctl", StringComparison.Ordinal)), response, "HOSTILE-TEXT",
                        "the hostile name is not carried as inert text");
                }
            }

            await run.AssertCleanAsync(Output);
        }

        protected virtual IEnumerable<string> HostilePaths()
        {
            return [];
        }
    }
}