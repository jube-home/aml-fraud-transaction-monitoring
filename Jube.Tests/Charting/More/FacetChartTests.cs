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
using Jube.Charting.Interfaces;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.More
{
    public class FacetChartTests
    {
        private static readonly double[] xs = [1, 2, 3, 4, 5];

        [Fact]
        public void EveryPanelIsANestedChartWithItsOwnHoverAndUniqueIds()
        {
            var svg = MoreSamples.Facets().Render();
            Regex.Matches(svg, "<svg x=").Count.Should().Be(4);
            var ids = Regex.Matches(svg, "<title id=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();
            ids.Should().OnlyHaveUniqueItems();
            svg.Should().Contain("Card").And.Contain("Online").And.Contain(":hover");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void SharedScalesGiveEveryPanelTheSameAxisAndFreeScalesDoNot()
        {
            string Ticks(string svg)
            {
                return string.Join("|",
                    Regex.Matches(svg, "<text class=\"tk end mid\"[^>]*>([^<]+)</text>")
                        .Select(m => m.Groups[1].Value));
            }

            List<string> Panels(string svg)
            {
                return [.. Regex.Split(svg, "<svg x=").Skip(1).Select(Ticks)];
            }

            var shared = Chart.Facets().Columns(2).SharedScales(false).LinePanel("a", xs, [1.0, 2, 3, 4, 5])
                .LinePanel("b", xs, [100.0, 200, 300, 400, 500]).Render();

            var free = Chart.Facets().Columns(2).LinePanel("a", xs, [1.0, 2, 3, 4, 5])
                .LinePanel("b", xs, [100.0, 200, 300, 400, 500]).Render();

            Panels(shared).Distinct().Should().HaveCount(1);
            Panels(free).Distinct().Should().HaveCount(2);
        }

        [Fact]
        public void GridGrowsTheCanvasWhenPanelsWouldBeTooSmall()
        {
            var builder = Chart.Facets().Columns(2);

            for (var i = 0; i < 10; i++)
            {
                builder.LinePanel("p" + i, xs, xs);
            }

            var svg = builder.Render();
            var height = double.Parse(Regex.Match(svg, "viewBox=\"0 0 640 ([0-9.]+)\"").Groups[1].Value);
            height.Should().BeGreaterThan(400);
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void GenericPanelsHistogramAndColumnPanelsWork()
        {
            var svg = Chart.Facets().HistogramPanel("h", [1.0, 2, 2, 3, 3, 3, 4]).ColumnPanel("c", ["a", "b"], [1.0, 2])
                .ScatterPanel("s", xs, xs).Panel("custom",
                    c => Chart.Area().WithOptions(c.Options).Series("x", [1.0, 2, 3]).Build()).Render();

            Regex.Matches(svg, "<svg x=").Count.Should().Be(4);
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void ForeignPanelImplementationsAreRejected()
        {
            Action act = () => Chart.Facets().Panel("evil", _ => new Foreign()).Render();
            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void BadSpecificationsCapsAndEmptyAreHandled()
        {
            Action columns = () => Chart.Facets().Columns(99).LinePanel("a", xs, xs).Render();
            columns.Should().Throw<ChartSpecificationException>();
            Action aspect = () => Chart.Facets().PanelAspect(9).LinePanel("a", xs, xs).Render();
            aspect.Should().Throw<ChartSpecificationException>();
            var many = Chart.Facets();

            for (var i = 0; i < ChartLimits.MaxSeries + 1; i++)
            {
                many.LinePanel("p" + i, xs, xs);
            }

            Action cap = () => many.Render();
            cap.Should().Throw<ChartLimitException>();
            Chart.Facets().Render().Should().Contain("No data");
        }

        [Fact]
        public void OutputSizeCapStopsRunawayFacets()
        {
            var xs = Enumerable.Range(0, 700).Select(i => (double)i).ToArray();
            var ys = xs.Select(x => Math.Sin(x / 9)).ToArray();
            var builder = Chart.Facets().Columns(4);

            for (var i = 0; i < 24; i++)
            {
                builder.ScatterPanel("p" + i, xs, ys);
            }

            Action act = () => builder.Render();
            act.Should().Throw<ChartLimitException>().Which.Limit.Should().Contain("output");
        }

        private sealed class Foreign : ISvgChart
        {
            public string Render()
            {
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"x\"><script>alert(1)</script></svg>";
            }
        }
    }
}