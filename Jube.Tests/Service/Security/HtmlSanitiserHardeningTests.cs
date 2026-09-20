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
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using HtmlSanitiser = global::Jube.Service.Security.HtmlSanitiser;

namespace Jube.Test.Service.Security
{
    [Trait("Category", "Service")]
    public sealed class HtmlSanitiserHardeningTests(ITestOutputHelper output)
    {
        [Theory]
        [InlineData("/Account/Logout")]
        [InlineData("/api/Case")]
        [InlineData("/api/RegisterSignalrConnection/abc")]
        [InlineData("/Administration/Frame/UserRegistry")]
        [InlineData("//evil.example/x.png")]
        [InlineData("/\\evil.example/x.png")]
        [InlineData("/icons/../Account/Logout")]
        [InlineData("/icons/x.png?next=/Account/Logout")]
        [InlineData("javascript:alert(1)")]
        [InlineData("JaVaScRiPt:alert(1)")]
        [InlineData("vbscript:msgbox(1)")]
        [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
        [InlineData("data:image/svg+xml;base64,PHN2ZyBvbmxvYWQ9YWxlcnQoMSk+")]
        [InlineData("http://images.example.test/a.png")]
        [InlineData("ftp://images.example.test/a.png")]
        [InlineData("file:///etc/passwd")]
        public void ImageSourcesThatAreSameOriginOrExecutableAreDropped(string source)
        {
            var result = HtmlSanitiser.Sanitise($"<p>note</p><img src=\"{source}\" alt=\"x\">");

            result.Should().NotContain("src=");
            result.Should().Contain("<img");
            result.Should().NotContainEquivalentOf("javascript");
            result.Should().NotContainEquivalentOf("vbscript");
        }

        [Theory]
        [InlineData(
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg==")]
        [InlineData("data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////")]
        [InlineData("https://images.example.test/a.png")]
        [InlineData("https://images.example.test/a.png?size=large&v=2")]
        [InlineData("/icons/flag.png")]
        [InlineData("/images/logo/mark.svg")]
        public void SafeImageSourcesAreKept(string source)
        {
            var result = HtmlSanitiser.Sanitise($"<img src=\"{source}\" alt=\"x\">");

            result.Should().Contain("src=");
            result.Should().Contain(System.Net.WebUtility.HtmlEncode(source).Replace("&#39;", "'"));
        }

        [Fact]
        public void LinksAndOrdinaryMarkupAreUnchangedAndScriptIsStillDropped()
        {
            var result = HtmlSanitiser.Sanitise(
                "<p>Hello <b>bold</b> <a href=\"/Case/Case?id=1\">case</a> <a href=\"https://example.test/x\">x</a>" +
                "<script>alert(1)</script><img src=x onerror=alert(1)><a href=\"javascript:alert(1)\">j</a></p>");

            result.Should().Contain("<b>bold</b>");
            result.Should().Contain("href=\"/Case/Case?id=1\"");
            result.Should().Contain("href=\"https://example.test/x\"");
            result.Should().NotContain("onerror");
            result.Should().NotContain("javascript:");
            result.Should().NotContain("<script");
        }

        [Theory]
        [InlineData("width:expression(alert(1))")]
        [InlineData("width:EXPRESSION(alert(1))")]
        [InlineData("width:expr&#101;ssion(alert(1))")]
        [InlineData("width:expression (alert(1))")]
        [InlineData("background-color:url(javascript:alert(1))")]
        [InlineData("background-color:URL(https://evil.example/x)")]
        [InlineData("background-color:ur&#108;(https://evil.example/x)")]
        [InlineData("color:red;width:expression(alert(1))")]
        [InlineData("width:exp/**/ression(alert(1))")]
        [InlineData("width:\\65xpression(alert(1))")]
        [InlineData("color:@import url(x)")]
        public void StyleValuesCannotCarryAUrlOrAnExpressionWithParentheses(string style)
        {
            var result = HtmlSanitiser.Sanitise($"<span style=\"{style}\">t</span>");

            result.Should().NotContainEquivalentOf("expression");
            result.Should().NotContainEquivalentOf("url(");
            result.Should().NotContain("javascript");
            result.Should().NotContain("@import");
        }

        [Fact]
        public void HarmlessParenthesisedStyleValuesAreKept()
        {
            var result =
                HtmlSanitiser.Sanitise("<span style=\"color:rgb(10, 20, 30);width:calc(100% - 2px)\">t</span>");

            result.Should().Contain("color:rgb(10, 20, 30)");
            result.Should().Contain("width:calc(100% - 2px)");
        }

        [Fact]
        public void HostileLongInputCostsLinearTime()
        {
            var inputs = new (string Name, string Text)[]
            {
                ("100k '<' then one '>'", new string('<', 100_000) + ">"),
                ("100k '<' and no '>'", new string('<', 100_000)),
                ("33k '<a ' then one '>'", string.Concat(Enumerable.Repeat("<a ", 33_000)) + ">"),
                ("unterminated attributes", string.Concat(Enumerable.Repeat("<a b='", 16_000)) + ">"),
                ("many attributes", "<a " + string.Concat(Enumerable.Repeat("b=c ", 24_000)) + ">"),
                ("nested angle noise", string.Concat(Enumerable.Repeat("<<>", 33_000)))
            };

            foreach (var (name, text) in inputs)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = HtmlSanitiser.Sanitise(text);
                stopwatch.Stop();

                output.WriteLine(
                    $"{name}: {text.Length} chars in {stopwatch.ElapsedMilliseconds} ms -> {result.Length} chars");
                stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1.5), name);
                result.Should().NotContain("<script");
            }
        }
    }
}