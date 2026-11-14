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
using FluentAssertions;
using Jube.Service.CaseDispatch;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.CaseDispatch
{
    [Trait("Category", "Unit")]
    public sealed class DispatchGuardTests
    {
        private static readonly Dictionary<string, object?> Roots = new(StringComparer.OrdinalIgnoreCase)
        {
            ["rows"] = new List<object?>
            {
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = "A <b>&</b> B", ["amount"] = 1234.5m, ["id"] = "x y/z"
                }
            },
            ["rowCount"] = 1,
            ["truncated"] = false
        };

        [Fact]
        public void AcceptsOneOrMoreWellFormedAddresses()
        {
            EmailTarget.Recipients("a@b.io").Should().Equal("a@b.io");
            EmailTarget.Recipients("a@b.io; c@d.io , e@f.io").Should().Equal("a@b.io", "c@d.io", "e@f.io");
        }

        [Fact]
        public void RefusesAnythingThatIsNotJustAddresses()
        {
            EmailTarget.Recipients(null).Should().BeNull();
            EmailTarget.Recipients("").Should().BeNull();
            EmailTarget.Recipients("not an address").Should().BeNull();
            EmailTarget.Recipients("Name <a@b.io>").Should().BeNull();
            EmailTarget.Recipients("a@b.io\r\nBcc: x@y.io").Should().BeNull();
            EmailTarget.Recipients(string.Join(",", Enumerable.Range(0, 11).Select(i => $"u{i}@b.io"))).Should().BeNull();
        }

        [Fact]
        public void StripsLineBreaksFromASubjectSoItCannotAddHeaders()
        {
            EmailTarget.Subject("Case 1\r\nBcc: x@y.io").Should().Be("Case 1Bcc: x@y.io");
            EmailTarget.Subject(null).Should().BeEmpty();
            EmailTarget.Subject(new string('s', 2000)).Length.Should().Be(998);
        }

        [Fact]
        public void AcceptsATelephoneNumberAndRefusesOtherText()
        {
            EmailTarget.Number("+44 (0) 20 7946 0000").Should().Be("+44 (0) 20 7946 0000");
            EmailTarget.Number("call me").Should().BeNull();
            EmailTarget.Number("12").Should().BeNull();
        }

        [Fact]
        public void FillsOnlyThePathAndQueryOfAFixedHost()
        {
            var (url, refusal) = HttpTarget.Resolve("https://hooks.example.com/case/[@ rows[0].id @]?n=[@ rows[0].name @]", Roots);

            refusal.Should().BeNull();
            url.Should().Be("https://hooks.example.com/case/x%20y%2Fz?n=A%20%3Cb%3E%26%3C%2Fb%3E%20B");
        }

        [Fact]
        public void RefusesATokenInTheHostOrSchemeOfTheEndpoint()
        {
            HttpTarget.Resolve("https://[@ rows[0].id @].example.com/x", Roots).Refusal.Should().Be(HttpTarget.AuthorityNotLiteral);
            HttpTarget.Resolve("[@ rows[0].id @]://example.com/x", Roots).Refusal.Should().Be(HttpTarget.AuthorityNotLiteral);
            HttpTarget.Resolve("https://example.com[@ rows[0].id @]", Roots).Refusal.Should().Be(HttpTarget.AuthorityNotLiteral);
        }

        [Fact]
        public void RefusesAValueThatTriesToLeaveTheHost()
        {
            var hostile = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["rows"] = new List<object?> { new Dictionary<string, object?> { ["p"] = "@evil.example/" } }
            };

            var (url, refusal) = HttpTarget.Resolve("https://hooks.example.com/[@ rows[0].p @]", hostile);

            refusal.Should().BeNull();
            new Uri(url!).Host.Should().Be("hooks.example.com");
        }

        [Fact]
        public void RefusesEndpointsThatAreEmptyRelativeOrNotHttp()
        {
            HttpTarget.Resolve(null, Roots).Refusal.Should().Be(HttpTarget.Empty);
            HttpTarget.Resolve("/relative/path", Roots).Refusal.Should().Be(HttpTarget.NotAbsolute);
            HttpTarget.Resolve("ftp://example.com/x", Roots).Refusal.Should().Be(HttpTarget.SchemeNotAllowed);
            HttpTarget.Resolve("https://user:pw@example.com/x", Roots).Refusal.Should().Be(HttpTarget.HostChanged);
        }

        [Fact]
        public void AcceptsAFixedEndpointWithNoTokens()
        {
            HttpTarget.Resolve("https://example.com:8443/hook", Roots).Url.Should().Be("https://example.com:8443/hook");
        }

        [Fact]
        public void BuildsAJsonBodyFromTheRowsAndTheCase()
        {
            var rows = new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { ["name"] = "A", ["amount"] = 5 }
            };

            var json = JObject.Parse(DispatchBody.Build("macro", "Email the branch", 9, "AccountId", "K1", "alex", rows,
                new Dictionary<string, string?> { ["reason"] = "fraud" }));

            json["source"]!.Value<string>().Should().Be("macro");
            json["caseId"]!.Value<int>().Should().Be(9);
            json["rows"]![0]!["amount"]!.Value<int>().Should().Be(5);
            json["values"]!["reason"]!.Value<string>().Should().Be("fraud");
        }
    }
}
