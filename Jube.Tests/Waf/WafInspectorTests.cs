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
using System.Diagnostics;
using FluentAssertions;
using Jube.App.Code.Waf;
using Jube.App.Code.Waf.Models;
using Jube.Data.Poco;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Waf
{
    [Trait("Category", "Unit")]
    public sealed class WafInspectorTests
    {
        private static WafInspectionRequest Request(string route, params WafFieldValue[] fields)
        {
            return new WafInspectionRequest("Http", route, "POST", "127.0.0.1", "tester", "trace", fields);
        }

        private static WafFieldValue Body(string value)
        {
            return new WafFieldValue("$.field", value, WafTargetScope.Body);
        }

        public static IEnumerable<object[]> AttackPayloads()
        {
            yield return ["OWASP-SQLI-UNION", "1 UNION SELECT password FROM users"];
            yield return ["OWASP-SQLI-TAUTOLOGY", "x' OR '1'='1"];
            yield return ["OWASP-SQLI-STACKED", "1; DROP TABLE users"];
            yield return ["OWASP-SQLI-TIMING", "1 AND sleep(5)"];
            yield return ["OWASP-XSS-SCRIPT", "<script>alert(1)</script>"];
            yield return ["OWASP-XSS-JS-URI", "javascript:alert(document.cookie)"];
            yield return ["OWASP-XSS-EVENT", "<img src=x onerror=alert(1)>"];
            yield return ["OWASP-TRAVERSAL-DOTDOT", "../../../../etc/passwd"];
            yield return ["OWASP-CMDI-SHELL", "value; cat /etc/passwd"];
            yield return ["OWASP-SSTI-BRACES", "${7*7}"];
            yield return ["OWASP-XXE-DOCTYPE", "<!DOCTYPE foo [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]>"];
            yield return ["OWASP-NULLBYTE", "invoice.pdf\u0000.exe"];
        }

        public static IEnumerable<object[]> BenignPayloads()
        {
            yield return ["united nations humanitarian report"];
            yield return ["orders and deliveries for the week"];
            yield return ["hello; world of finance"];
            yield return ["a sleepy little town"];
            yield return ["typescript is a nice language"];
            yield return ["i enjoy java and coffee"];
            yield return ["please turn the lights on before you leave"];
            yield return ["the path a/b/c is fine"];
            yield return ["cats and dogs are lovely"];
            yield return ["seven times seven is forty nine"];
            yield return ["the doctor will see you now"];
            yield return ["a perfectly ordinary transaction narrative"];
            yield return ["Jean-Pierre O'Brien paid 100.00 to ACME Ltd"];
        }

        [Theory]
        [MemberData(nameof(AttackPayloads))]
        public void AnAttackPayloadIsMatchedAndDropped(string expectedSignature, string payload)
        {
            var inspector = new WafInspector(WafTestRuleSets.SeededRegistry(), TestDynamicEnvironment.Create());

            var result = inspector.Inspect(Request("/api/Test", Body(payload)));

            result.Blocked.Should().BeTrue(payload);
            result.Matches.Should().Contain(match => match.SignatureName == expectedSignature);
        }

        [Theory]
        [MemberData(nameof(BenignPayloads))]
        public void ABenignPayloadIsNotMatched(string payload)
        {
            var inspector = new WafInspector(WafTestRuleSets.SeededRegistry(), TestDynamicEnvironment.Create());

            var result = inspector.Inspect(Request("/api/Test", Body(payload)));

            result.Blocked.Should().BeFalse($"'{payload}' should not trip any signature");
            result.Matches.Should().BeEmpty();
        }

        [Fact]
        public void AnEmptyRuleSetBlocksNothing()
        {
            var inspector = new WafInspector(new WafRegistry(), TestDynamicEnvironment.Create());

            inspector.Inspect(Request("/api/Test", Body("<script>alert(1)</script>"))).Blocked.Should().BeFalse();
        }

        [Fact]
        public void ADetectOnlySignatureRecordsButDoesNotBlock()
        {
            var registry = WafTestRuleSets.RegistryFor(
            [
                new WafSignature
                {
                    Id = 1, Name = "DETECT-XSS", Category = "A7-XSS", Pattern = @"(?i)<\s*script\b",
                    TargetScope = 7, MatchTimeoutMilliseconds = 50, Drop = 0, Active = 1
                }
            ], []);
            var inspector = new WafInspector(registry, TestDynamicEnvironment.Create());

            var result = inspector.Inspect(Request("/api/Test", Body("<script>alert(1)</script>")));

            result.Blocked.Should().BeFalse();
            result.Matches.Should().ContainSingle().Which.Drop.Should().BeFalse();
        }

        [Fact]
        public void AnExceptionForTheRouteSuppressesTheMatch()
        {
            var registry = WafTestRuleSets.RegistryFor(
                [
                    new WafSignature
                    {
                        Id = 1, Name = "XSS", Category = "A7-XSS", Pattern = @"(?i)<\s*script\b",
                        TargetScope = 7, MatchTimeoutMilliseconds = 50, Drop = 1, Active = 1
                    }
                ],
                [
                    new WafException { Id = 1, Name = "trusted", RouteRegex = "^/api/Trusted", Active = 1 }
                ]);
            var inspector = new WafInspector(registry, TestDynamicEnvironment.Create());
            var attack = Body("<script>alert(1)</script>");

            inspector.Inspect(Request("/api/Trusted/Import", attack)).Blocked.Should().BeFalse();
            inspector.Inspect(Request("/api/Other", attack)).Blocked.Should().BeTrue();
        }

        [Fact]
        public void AnExceptionForTheFieldSuppressesOnlyThatField()
        {
            var registry = WafTestRuleSets.RegistryFor(
                [
                    new WafSignature
                    {
                        Id = 1, Name = "XSS", Category = "A7-XSS", Pattern = @"(?i)<\s*script\b",
                        TargetScope = 7, MatchTimeoutMilliseconds = 50, Drop = 1, Active = 1
                    }
                ],
                [
                    new WafException
                    {
                        Id = 1, Name = "narrative-free-text", RouteRegex = "^/api/Case",
                        FieldRegex = @"\.narrative$", Active = 1
                    }
                ]);
            var inspector = new WafInspector(registry, TestDynamicEnvironment.Create());
            var attack = "<script>alert(1)</script>";

            inspector.Inspect(Request("/api/Case",
                new WafFieldValue("$.narrative", attack, WafTargetScope.Body))).Blocked.Should().BeFalse();
            inspector.Inspect(Request("/api/Case",
                new WafFieldValue("$.accountNumber", attack, WafTargetScope.Body))).Blocked.Should().BeTrue();
        }

        [Fact]
        public void AnExceptionScopedToAnotherSignatureDoesNotSuppress()
        {
            var registry = WafTestRuleSets.RegistryFor(
                [
                    new WafSignature
                    {
                        Id = 5, Name = "XSS", Category = "A7-XSS", Pattern = @"(?i)<\s*script\b",
                        TargetScope = 7, MatchTimeoutMilliseconds = 50, Drop = 1, Active = 1
                    }
                ],
                [
                    new WafException
                    {
                        Id = 1, Name = "wrong-signature", RouteRegex = "^/api/Test", WafSignatureId = 999, Active = 1
                    }
                ]);
            var inspector = new WafInspector(registry, TestDynamicEnvironment.Create());

            inspector.Inspect(Request("/api/Test", Body("<script>alert(1)</script>"))).Blocked.Should().BeTrue();
        }

        [Fact]
        public void AScopeMismatchMeansTheSignatureDoesNotEvaluateThatField()
        {
            var registry = WafTestRuleSets.RegistryFor(
            [
                new WafSignature
                {
                    Id = 1, Name = "BODY-ONLY", Category = "A7-XSS", Pattern = @"(?i)<\s*script\b",
                    TargetScope = (int)WafTargetScope.Body, MatchTimeoutMilliseconds = 50, Drop = 1, Active = 1
                }
            ], []);
            var inspector = new WafInspector(registry, TestDynamicEnvironment.Create());
            const string attack = "<script>alert(1)</script>";

            inspector.Inspect(Request("/api/Test",
                new WafFieldValue("$query.q", attack, WafTargetScope.Query))).Blocked.Should().BeFalse();
            inspector.Inspect(Request("/api/Test",
                new WafFieldValue("$.field", attack, WafTargetScope.Body))).Blocked.Should().BeTrue();
        }

        [Fact]
        public void ManySlowSignaturesTogetherStayWithinTheOverallInspectionBudget()
        {
            var signatures = new List<WafSignature>();
            for (var id = 1; id <= 40; id++)
            {
                signatures.Add(new WafSignature
                {
                    Id = id, Name = $"REDOS-{id}", Category = "test", Pattern = "^(a+)+$", TargetScope = 7,
                    MatchTimeoutMilliseconds = 50, Drop = 0, Active = 1
                });
            }

            var registry = WafTestRuleSets.RegistryFor(signatures, []);
            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["WafMaxInspectionMilliseconds"] = "200"
            });
            var inspector = new WafInspector(registry, environment);
            var evilInput = new string('a', 40) + "!";

            var stopwatch = Stopwatch.StartNew();
            inspector.Inspect(Request("/api/Test", Body(evilInput)));
            stopwatch.Stop();

            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500),
                "40 signatures each nearing their own 50ms timeout would otherwise add up to ~2s without an overall budget");
        }
    }
}