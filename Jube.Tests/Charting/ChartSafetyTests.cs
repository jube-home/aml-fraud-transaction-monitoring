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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Charts.Models;
using Jube.Charting.Core.Models;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting
{
    public class ChartSafetyTests
    {
        public static IEnumerable<object[]> Names()
        {
            return ChartSamples.All().Keys.Select(k => new object[] { k });
        }

        public static IEnumerable<object[]> InteractiveNames()
        {
            return ChartSamples.All().Keys.Where(k => !k.StartsWith("sparkline", StringComparison.Ordinal))
                .Select(k => new object[] { k });
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void OutputIsWellFormedAndSafe(string name)
        {
            SvgAssert.IsSafe(ChartSamples.All()[name].Render());
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void IdenticalSpecificationsRenderIdenticalBytes(string name)
        {
            var first = ChartSamples.All()[name].Render();
            var second = ChartSamples.All()[name].Render();
            second.Should().Be(first);
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EveryChartCarriesBothThemesAndResponsiveMetadata(string name)
        {
            var svg = ChartSamples.All()[name].Render();
            svg.Should().Contain("prefers-color-scheme:dark");
            svg.Should().Contain("[data-theme=\"dark\"]");
            svg.Should().Contain("[data-theme=\"light\"]");
            svg.Should().Contain("--jc-surface:#fcfcfb");
            svg.Should().Contain("--jc-surface:#1a1a19");
            svg.Should().Contain("viewBox=\"0 0 ");
            svg.Should().Contain("preserveAspectRatio=\"xMidYMid meet\"");
            svg.Should().Contain("role=\"img\"");
            svg.Should().Contain("<title id=");
            svg.Should().Contain("<desc id=");
            svg.Should().Contain("prefers-reduced-motion:reduce");
        }

        [Theory]
        [MemberData(nameof(InteractiveNames))]
        public void EveryChartHasHoverCssTooltipsAndKeyboardFocus(string name)
        {
            var svg = ChartSamples.All()[name].Render();
            svg.Should().Contain(":has(.mk:hover)");
            svg.Should().Contain(":focus-visible");
            svg.Should().Contain("class=\"tt\"");
            svg.Should().Contain("tabindex=\"0\"");
            svg.Should().Contain("role=\"graphics-symbol\"");
            svg.Should().Contain("aria-label=");
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void StylesAreScopedToTheChartClass(string name)
        {
            var svg = ChartSamples.All()[name].Render();
            var start = svg.IndexOf("<style>", StringComparison.Ordinal) + 7;
            var end = svg.IndexOf("</style>", StringComparison.Ordinal);
            var css = svg[start..end];
            var cls = Regex.Match(svg, "class=\"(jc-[0-9a-f]+)\"").Groups[1].Value;
            cls.Should().NotBeEmpty();
            var rules = Regex.Matches(css, "(?<=^|\\})([^{}@]+)\\{");

            foreach (Match rule in rules)
            {
                foreach (var selector in rule.Groups[1].Value.Split(','))
                {
                    selector.Should().Contain("." + cls);
                }
            }
        }

        [Fact]
        public void SparklinesAreFocusableWithNativeTitles()
        {
            var svg = ChartSamples.Spark(SparklineKind.Line).Render();
            svg.Should().Contain("tabindex=\"0\"");
            svg.Should().Contain("<title>");
            svg.Should().NotContain("class=\"tt\"");
        }

        [Fact]
        public void LegendAndMarkSharedSeriesClassesAreLinked()
        {
            var svg = ChartSamples.Line().Render();
            svg.Should().Contain("class=\"lg s0\"");
            svg.Should().Contain("class=\"sr s0\"");
            svg.Should().Contain(":has(.lg.s0:hover) ");
            svg.Should().Contain(":has(.sr.s0:hover) ");
        }

        [Fact]
        public void DifferentChartsOnOnePageGetDifferentClasses()
        {
            var a = Chart.Line().Series("a", [1.0, 2]).Render();
            var b = Chart.Line().Series("b", [3.0, 1]).Render();
            var ca = Regex.Match(a, "class=\"(jc-[0-9a-f]+)\"").Groups[1].Value;
            var cb = Regex.Match(b, "class=\"(jc-[0-9a-f]+)\"").Groups[1].Value;
            ca.Should().NotBe(cb);
        }

        [Fact]
        public void ExplicitIdIsUsedAsClassAndPrefix()
        {
            var svg = Chart.Line().Id("roc-1").Series("a", [1.0, 2]).Render();
            svg.Should().Contain("class=\"roc-1\"");
            svg.Should().Contain("id=\"roc-1-t\"");
        }

        [Fact]
        public void InvalidIdIsRejected()
        {
            var act = () => Chart.Line().Id("bad id\"><script>").Series("a", [1.0, 2]).Render();
            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void ResponsiveOmitsSizeAndFixedSetsIt()
        {
            var responsive = Chart.Line().Series("a", [1.0, 2]).Render();
            var fixedSize = Chart.Line().FixedSize(500, 300).Series("a", [1.0, 2]).Render();
            var rootOf = (Func<string, string>)(svg => svg[..(svg.IndexOf('>') + 1)]);
            rootOf(responsive).Should().NotContain(" width=\"");
            responsive.Should().Contain("width:100%");
            rootOf(fixedSize).Should().Contain(" width=\"500\" height=\"300\"");
            fixedSize.Should().NotContain("width:100%");
        }

        [Fact]
        public void ExplicitThemeSetsTheDataThemeAttribute()
        {
            Chart.Line().Theme(ThemeMode.Dark).Series("a", [1.0, 2]).Render().Should().Contain("data-theme=\"dark\"");
            Chart.Line().Theme(ThemeMode.Light).Series("a", [1.0, 2]).Render().Should().Contain("data-theme=\"light\"");
            Chart.Line().Series("a", [1.0, 2]).Render().Should().NotContain("data-theme=\"light\" ");
        }

        [Fact]
        public void NonInteractiveChartsCarryNoTooltipsOrFocus()
        {
            var svg = Chart.Column().Categories(["a", "b"]).Series("s", [1.0, 2]).Interactive(false).Render();
            svg.Should().NotContain("class=\"tt\"");
            svg.Should().NotContain("tabindex");
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void TooltipsStayInsideTheChart()
        {
            var svg = Chart.Column().Categories(["Very long category name that wants a wide tooltip indeed", "b"])
                .Series("A very long series name indeed for width", [100.0, 2]).FixedSize(300, 200).Render();

            var doc = SvgAssert.WellFormed(svg);
            var ns = doc.Root!.Name.Namespace;

            foreach (var group in doc.Descendants(ns + "g").Where(g => (string?)g.Attribute("class") == "tt"))
            {
                var rect = group.Element(ns + "rect")!;
                var x = double.Parse((string)rect.Attribute("x")!, CultureInfo.InvariantCulture);
                var y = double.Parse((string)rect.Attribute("y")!, CultureInfo.InvariantCulture);
                var w = double.Parse((string)rect.Attribute("width")!,
                    CultureInfo.InvariantCulture);
                var h = double.Parse((string)rect.Attribute("height")!,
                    CultureInfo.InvariantCulture);

                x.Should().BeGreaterOrEqualTo(0);
                y.Should().BeGreaterOrEqualTo(0);
                (x + w).Should().BeLessOrEqualTo(300);
                (y + h).Should().BeLessOrEqualTo(200);
            }
        }
    }
}