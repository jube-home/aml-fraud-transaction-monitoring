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
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Jube.Test.Charting.Diagrams;
using Jube.Test.Charting.More;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Charting.External
{
    public class ExternalStyleTests
    {
        private static readonly HashSet<string> allowedElements =
        [
            "svg", "title", "desc", "rect", "text", "line", "path", "circle", "g"
        ];

        private static readonly HashSet<string> allowedAttributes =
        [
            "xmlns", "class", "viewBox", "preserveAspectRatio", "width", "height", "data-theme", "data-node", "data-from",
            "data-to", "role",
            "aria-labelledby", "aria-label", "id", "x", "y", "x1", "y1", "x2", "y2", "d", "transform", "rx", "r", "cx",
            "cy", "style", "tabindex", "stroke-width"
        ];

        private readonly ITestOutputHelper output;

        public ExternalStyleTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        public static IEnumerable<object[]> Names()
        {
            return ExternalSamples.Names().Select(n => new object[] { n });
        }

        internal static void AssertExternalSafe(string svg, bool strict = true)
        {
            var document = XDocument.Parse(svg);
            svg.Should().NotContain("<style");
            svg.Should().NotContain("<script");
            svg.Should().NotContain("<foreignObject");
            svg.Should().NotContain("<image");
            svg.Should().NotContain(" href=\"");
            svg.Should().NotContain("xlink:");
            svg.Should().NotContain("@import");

            if (strict)
            {
                svg.Should().NotContain("javascript:");
                svg.Should().NotContain("NaN");
                svg.Should().NotContain("Infinity");
                svg.Should().NotContain("url(");
                svg.Replace("http://www.w3.org/2000/svg", string.Empty).Should().NotContain("http");
            }

            document.Root!.Attribute("class")!.Value.Split(' ').Should().Contain("jc");

            foreach (var element in document.Descendants())
            {
                allowedElements.Should().Contain(element.Name.LocalName);

                foreach (var attribute in element.Attributes())
                {
                    allowedAttributes.Should().Contain(attribute.Name.LocalName);
                    attribute.Name.LocalName.Should().NotStartWith("on");

                    if (attribute.Name.LocalName == "style")
                    {
                        attribute.Value.Should().MatchRegex(
                            "^--[a-z0-9-]+:#[0-9a-f]{3,6}(;--[a-z0-9-]+:#[0-9a-f]{3,6})*$",
                            "an External style attribute may only set custom properties to validated hex colours");
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EverySampleRendersWellFormedWithoutAStyleElement(string name)
        {
            AssertExternalSafe(ExternalSamples.RenderExternal(name));
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void OutputIsDeterministic(string name)
        {
            ExternalSamples.RenderExternal(name).Should().Be(ExternalSamples.RenderExternal(name));
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void ClassesAreStableAndNeverHashed(string name)
        {
            var document = XDocument.Parse(ExternalSamples.RenderExternal(name));

            foreach (var token in ClassTokens(document))
            {
                token.Should().NotMatchRegex("^jc-[0-9a-f]{8}$");
            }

            var root = document.Root!.Attribute("class")!.Value.Split(' ');
            root[0].Should().Be("jc");
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void OutputIsSmallerThanInline(string name)
        {
            var inline = ExternalSamples.RenderInline(name);
            var external = ExternalSamples.RenderExternal(name);
            output.WriteLine(name + " inline " + inline.Length + " external " + external.Length);

            (inline.Length - external.Length).Should().BeGreaterThan(8_000,
                "the per chart stylesheet is at least 8 KB of the inline output");
        }

        [Fact]
        public void SizesAcrossAllSamplesAreMateriallySmaller()
        {
            long inline = 0;
            long external = 0;
            long smallInline = 0;
            long smallExternal = 0;

            foreach (var name in ExternalSamples.Names())
            {
                var i = ExternalSamples.RenderInline(name).Length;
                var e = ExternalSamples.RenderExternal(name).Length;
                inline += i;
                external += e;

                if (i < 30_000)
                {
                    smallInline += i;
                    smallExternal += e;
                }
            }

            output.WriteLine(
                "total inline " + inline + " external " + external + " ratio " + (double)external / inline);
            output.WriteLine("small inline " + smallInline + " external " + smallExternal + " ratio " +
                             (double)smallExternal / smallInline);

            ((double)external / inline).Should().BeLessThan(0.8);
            ((double)smallExternal / smallInline).Should().BeLessThan(0.5);
        }

        [Fact]
        public void InlineIsTheDefaultAndKeepsItsStyleElement()
        {
            new ChartOptions().StyleMode.Should().Be(ChartStyleMode.Inline);
            var byDefault = Chart.Line().Series("a", [1.0, 2]).Render();
            var explicitInline = Chart.Line().Series("a", [1.0, 2]).StyleMode(ChartStyleMode.Inline).Render();
            explicitInline.Should().Be(byDefault);
            byDefault.Should().Contain("<style>");
            byDefault.Should().MatchRegex("class=\"jc-[0-9a-f]{8}\"");
        }

        [Fact]
        public void ExternalStylesShortcutMatchesTheOption()
        {
            var a = Chart.Line().Series("a", [1.0, 2]).ExternalStyles().Render();
            var b = Chart.Line().Series("a", [1.0, 2]).StyleMode(ChartStyleMode.External).Render();
            a.Should().Be(b);
            a.Should().NotContain("<style");
        }

        [Fact]
        public void EveryBuilderAndDiagramAcceptsTheStyleMode()
        {
            var samples = ChartSamples.All().Values
                .Concat(DiagramSamples.All().Values)
                .Concat(MoreSamples.All().Values);

            foreach (var sample in samples)
            {
                sample.Should().BeAssignableTo<IStyleModeChart>();
            }
        }

        [Fact]
        public void DefaultPaletteChartsCarryNoStyleAttribute()
        {
            var svg = ExternalSamples.RenderExternal("line");
            svg.Should().NotContain("style=");
        }

        [Fact]
        public void CustomSeriesColoursAreCustomPropertiesOnTheRootOnly()
        {
            var svg = ExternalSamples.RenderExternal("x-custom-series-colours");
            var document = XDocument.Parse(svg);
            var root = document.Root!;
            root.Attribute("style")!.Value.Should().StartWith("--jc-s0:#c0392b;--jc-o0:#");
            root.Attribute("style")!.Value.Should().Contain("--jc-s1:#1e8449");
            root.Attribute("style")!.Value.Should().NotContain("--jc-s2");
            document.Descendants().Skip(1).Count(e => e.Attribute("style") is not null).Should().Be(0);
        }

        [Fact]
        public void PerPointColoursUseTheSeriesCustomPropertyOnTheMark()
        {
            var svg = ExternalSamples.RenderExternal("x-point-colours");
            var document = XDocument.Parse(svg);

            document.Descendants().Where(e => e.Attribute("style") is not null)
                .Select(e => e.Attribute("style")!.Value).Should().BeEquivalentTo("--sc:#c0392b", "--sc:#1e8449");
        }

        [Fact]
        public void ExplicitIdsKeepTheTitleAndDescriptionIdsUniquePerChart()
        {
            var a = XDocument.Parse(Chart.Line().Id("chart-a").Series("a", [1.0, 2]).ExternalStyles().Render());
            var b = XDocument.Parse(Chart.Line().Id("chart-b").Series("a", [1.0, 2]).ExternalStyles().Render());
            var idsA = a.Descendants().Select(e => e.Attribute("id")?.Value).Where(v => v is not null).ToList();
            var idsB = b.Descendants().Select(e => e.Attribute("id")?.Value).Where(v => v is not null).ToList();
            idsA.Should().BeEquivalentTo("chart-a-t", "chart-a-d");
            idsB.Should().BeEquivalentTo("chart-b-t", "chart-b-d");
            a.Root!.Attribute("aria-labelledby")!.Value.Should().Be("chart-a-t chart-a-d");
        }

        [Fact]
        public void ExplicitThemeStaysAnAttributeOnTheRoot()
        {
            var svg = ExternalSamples.RenderExternal("x-dark-fixed");
            var root = XDocument.Parse(svg).Root!;
            root.Attribute("data-theme")!.Value.Should().Be("dark");
            root.Attribute("width")!.Value.Should().Be("480");
        }

        [Fact]
        public void FacetPanelsAreNestedExternalCharts()
        {
            var document = XDocument.Parse(ExternalSamples.RenderExternal("facets"));
            var roots = document.Descendants().Where(e => e.Name.LocalName == "svg").ToList();
            roots.Count.Should().BeGreaterThan(1);
            roots.Should().OnlyContain(e => e.Attribute("class")!.Value.Split(' ').Contains("jc"));
        }

        [Fact]
        public void EveryFamilyClassIsDocumentedByTheStylesheet()
        {
            var families = ExternalSamples.Names()
                .Select(n =>
                    XDocument.Parse(ExternalSamples.RenderExternal(n)).Root!.Attribute("class")!.Value.Split(' '))
                .SelectMany(c => c.Where(t => t != "jc"))
                .Distinct().ToList();

            var css = ChartStylesheet.Css(true);

            foreach (var family in families)
            {
                css.Should().Contain("." + family + ")");
            }
        }

        [Fact]
        public void SeriesClassesBeyondEightStillResolveAgainstTheStylesheet()
        {
            var svg = ExternalSamples.RenderExternal("x-many-series");
            var css = ChartStylesheet.Css(true);

            foreach (Match match in Regex.Matches(svg, "\\bs(\\d+)\\b"))
            {
                css.Should().Contain(".s" + match.Groups[1].Value + "{");
            }
        }

        public static IEnumerable<string> ClassTokens(XDocument document)
        {
            return document.Descendants().Select(e => e.Attribute("class")?.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .SelectMany(v => v!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Distinct();
        }
    }
}