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
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.More
{
    public class MoreInjectionTests
    {
        public static IEnumerable<object[]> Hostile()
        {
            var values = new[]
            {
                "<script>alert(1)</script>",
                "\"><script>alert(1)</script>",
                "' onload='alert(1)",
                "a & b &amp; c &lt;",
                "]]><script>x</script><![CDATA[",
                "<!-- comment -->",
                "</text></svg><script>alert(1)</script>",
                "</style><script>alert(1)</script>",
                "<foreignObject><body onload=alert(1)></foreignObject>",
                "<a href=\"javascript:alert(1)\">x</a>",
                "\u0000\u0001\u0008\u000B\u000C\u001F\u007F\u0085",
                "\uD800 lone surrogate \uDC00",
                "‮evil‬ ⁦x⁩",
                "مرحبا بالعالم שלום",
                "😀😀😀 emoji 👨‍👩‍👧‍👦",
                "line1\nline2\r\nline3\ttab",
                new string('x', 100_000),
                new string('<', 5_000),
                string.Empty,
                "   "
            };

            return values.Select(v => new object[] { v });
        }

        public static IEnumerable<ISvgChart> Charts(string t)
        {
            var d = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            yield return Chart.Survival().Title(t).Subtitle(t).XAxisTitle(t).YAxisTitle(t).Description(t)
                .Group(t, [1.0, 2, 3, 4], [true, false, true, true]).Group(t + "2", [2.0, 3, 5], [true, true, false])
                .Build();

            yield return Chart.TimeSeries().Title(t).Subtitle(t).XAxisTitle(t).YAxisTitle(t)
                .Series(t, [d, d.AddDays(1), d.AddDays(2)], [1.0, 2, 3]).Interval(t, [0.0, 1, 2], [2.0, 3, 4])
                .Event(d.AddDays(1), t).ChangePoint(d.AddDays(2), t).Region(d, d.AddDays(1), t).HorizontalLine(2, t)
                .Build();

            yield return Chart.ErrorBars().Title(t).Points(t, [1.0, 2, 3], [1.0, 2, 3]).YErrors([0.2, 0.3, 0.1])
                .XErrors([0.1, 0.1, 0.1]).Build();

            yield return Chart.StepChart().Title(t).Series(t, [1.0, 2, 3], [1.0, 3, 2]).RangeSelector(1, 2).Build();

            yield return Chart.Regression().Title(t).XAxisTitle(t).YAxisTitle(t)
                .Data(t, [1.0, 2, 3, 4, 5], [1.1, 2.3, 2.9, 4.2, 4.8]).Build();

            yield return Chart.ParallelCoordinates().Title(t).Dimension(t, [1.0, 2, 3])
                .CategoricalDimension(t + "c", [t, "b", t])
                .Groups([t, "g", t]).RowLabels([t, t, t]).Build();

            yield return Chart.Facets().Title(t).XAxisTitle(t).YAxisTitle(t).LinePanel(t, [1.0, 2, 3], [1.0, 2, 3])
                .ScatterPanel(t, [1.0, 2, 3], [3.0, 2, 1]).Build();

            yield return Chart.ScatterMatrix().Title(t).Variable(t, [1.0, 2, 3, 4]).Variable(t + "b", [4.0, 3, 1, 2])
                .Groups([t, "g", t, "g"]).Build();

            yield return Chart.Density().Title(t).Values(t, [1.0, 2, 3, 4, 5]).Build();
            yield return Chart.Ecdf().Title(t).Values(t, [1.0, 2, 3, 4, 5]).ConfidenceBand().Build();

            yield return Chart.Violin().Title(t).Values(t, [1.0, 2, 3, 4, 5]).Values(t + "b", [2.0, 3, 4, 8]).Points()
                .Build();

            yield return Chart.Violin().Title(t).Horizontal().Values(t, [1.0, 2, 3, 4, 5]).Build();

            yield return Chart.Ridgeline().Title(t).Values(t, [1.0, 2, 3, 4, 5]).Values(t + "b", [2.0, 3, 4, 8])
                .Build();

            yield return Chart.DotPlot().Title(t).Categories([t, "b"]).Series(t, [1.0, 2]).Build();
            yield return Chart.Lollipop().Title(t).Categories([t, "b"]).Series(t, [1.0, 2]).ShowValues().Build();

            yield return Chart.Dumbbell().Title(t).Categories([t, "b"]).Series(t, [1.0, 2]).Series(t + "2", [2.0, 1])
                .Build();

            yield return Chart.Slope().Title(t).Periods([t, "b"]).Series(t, [1.0, 2]).Series("x", [2.0, 1]).Build();

            yield return Chart.Bump().Title(t).Periods([t, "b", t]).Series(t, [1.0, 2, 3]).Series("x", [2.0, 1, 3])
                .Build();

            yield return Chart.CalendarHeatmap().Title(t).ValueName(t).Day(new DateTime(2026, 2, 3), 4).Build();

            yield return Chart.CorrelationMatrix().Title(t).Variable(t, [1.0, 2, 3]).Variable(t + "b", [3.0, 1, 2])
                .Variable("c", [1.0, 3, 2]).Build();

            yield return Chart.Adjacency().Title(t).Nodes([t, "b"]).Edge(t, "b", 2).Build();
            yield return Chart.Sankey().Title(t).Link(t, "b", 2).Link("b", t + "c", 1).Node("d").Build();
            yield return Chart.Stream().Title(t).X([0.0, 1, 2]).Series(t, [1.0, 2, 3]).Series("b", [2.0, 1, 1]).Build();
            yield return Chart.PercentStream().Title(t).X([0.0, 1, 2]).Series(t, [1.0, 2, 3]).Build();
        }

        [Theory]
        [MemberData(nameof(Hostile))]
        public void HostileTextNeverProducesMarkup(string text)
        {
            foreach (var chart in Charts(text))
            {
                var svg = chart.Render();
                MoreAssert.IsSafe(svg, false);
                svg.Length.Should().BeLessThan(600_000);
            }
        }

        [Fact]
        public void ScriptTextAppearsOnlyEscapedInEveryTextSlotOfANewChart()
        {
            var svg = Chart.Sankey().Title("<script>alert(1)</script>").Link("<b>x</b>", "\"quoted\" & 'single'", 3)
                .Render();

            svg.Should().Contain("&lt;script&gt;").And.Contain("&lt;b&gt;").And
                .Contain("&quot;quoted&quot; &amp; &apos;single&apos;");

            svg.Should().NotContain("<script").And.NotContain("<b>");
            var facet = Chart.Facets().LinePanel("</style><script>x</script>", [1.0, 2], [1.0, 2]).Render();
            facet.Should().NotContain("<script");
        }

        [Theory]
        [InlineData("red")]
        [InlineData("url(#x)")]
        [InlineData("#fff;} </style><script>")]
        public void InvalidColoursAreRejectedByNewCharts(string colour)
        {
            var actions = new Action[]
            {
                () => Chart.Survival().Group("a", [1.0], [true], colour).Render(),
                () => Chart.TimeSeries().Series("a", [1.0, 2], [1.0, 2], colour).Render(),
                () => Chart.Density().Values("a", [1.0, 2, 3], colour).Render(),
                () => Chart.Violin().Values("a", [1.0, 2, 3], colour).Render(),
                () => Chart.Stream().X([0.0, 1]).Series("a", [1.0, 2], colour).Render(),
                () => Chart.DotPlot().Categories(["a"]).Series("a", [1.0], colour).Render(),
                () => Chart.Sankey().Node("a", colour).Link("a", "b", 1).Render()
            };

            foreach (var action in actions)
            {
                action.Should().Throw<ChartSpecificationException>();
            }
        }

        [Fact]
        public void ChartIdsAreValidatedAndForcedThemesAreApplied()
        {
            Action bad = () => Chart.Density().Values("a", [1.0, 2, 3]).Id("bad id\"><script>").Render();
            bad.Should().Throw<ChartSpecificationException>();

            var dark = Chart.Survival().Group("a", [1.0, 2], [true, true]).Theme(ThemeMode.Dark).Id("my-chart")
                .Render();

            dark.Should().Contain("data-theme=\"dark\"").And.Contain("class=\"my-chart\"");
            var light = Chart.Sankey().Link("a", "b", 1).Theme(ThemeMode.Light).Render();
            light.Should().Contain("data-theme=\"light\"");
            var fixedSize = Chart.Stream().X([0.0, 1]).Series("a", [1.0, 2]).FixedSize(500, 300).Render();
            fixedSize.Should().Contain("width=\"500\"").And.Contain("height=\"300\"");
            Action size = () => Chart.Density().Values("a", [1.0, 2, 3]).Size(10, 10).Render();
            size.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void IdenticalChartsShareAClassAndDifferentChartsDoNot()
        {
            string ClassOf(string svg)
            {
                return Regex
                    .Match(svg, "<svg[^>]* class=\"([^\"]+)\"").Groups[1].Value;
            }

            var a = ClassOf(MoreSamples.Density().Render());
            ClassOf(MoreSamples.Density().Render()).Should().Be(a);
            ClassOf(MoreSamples.Ecdf().Render()).Should().NotBe(a);
        }
    }
}