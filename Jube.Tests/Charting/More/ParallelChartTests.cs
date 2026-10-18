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
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.More
{
    public class ParallelChartTests
    {
        [Fact]
        public void EveryRowIsAHoverableLineWithTooltipAndDimmingRules()
        {
            var svg = MoreSamples.Parallel().Render();
            Regex.Matches(svg, "<path class=\"pl\"").Count.Should().Be(90);
            Regex.Matches(svg, "class=\"mk s\\d\"").Count.Should().Be(90);
            svg.Should().Contain(":has(.mk:hover) ").And.Contain(".mk:not(:hover) .pl");
            svg.Should().Contain("Group: Corporate").And.Contain("Channel: Transfer");
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void EachColumnIsScaledIndependently()
        {
            var svg = Chart.ParallelCoordinates().Dimension("Small", [1.0, 2, 3])
                .Dimension("Large", [1000.0, 5000, 9000]).Render();

            svg.Should().Contain(">10k<").And.Contain(">3.0<");
        }

        [Fact]
        public void CategoricalColumnsAreOrderedAndNaNBreaksLines()
        {
            var svg = Chart.ParallelCoordinates().Dimension("A", [1.0, double.NaN, 3])
                .CategoricalDimension("B", ["y", "x", null]).Dimension("C", [1.0, 2, 3]).Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("B: n/a").And.Contain("A: n/a");

            var paths = Regex.Matches(svg, "<path class=\"pl\" d=\"([^\"]+)\"").Select(m => m.Groups[1].Value)
                .ToArray();
            paths.Should().HaveCount(3);
            paths.Count(p => Regex.Matches(p, "M").Count == 2).Should().BeGreaterThan(0);
        }

        [Fact]
        public void InvertedAxesFlipTheDirection()
        {
            var normal = Chart.ParallelCoordinates().Dimension("A", [0.0, 10]).Dimension("B", [0.0, 10]).Render();

            var flipped = Chart.ParallelCoordinates().Dimension("A", [0.0, 10], true).Dimension("B", [0.0, 10])
                .Render();
            normal.Should().NotBe(flipped);
        }

        [Fact]
        public void ManyRowsFallBackToPlainLinesWithoutMarks()
        {
            var values = Enumerable.Range(0, 1500).Select(i => (double)(i % 50)).ToArray();
            var svg = Chart.ParallelCoordinates().Dimension("A", values).Dimension("B", values.Reverse()).Render();
            svg.Should().NotContain("tabindex=\"0\"");
            Regex.Matches(svg, "<path class=\"pl fine\"").Count.Should().Be(1500);
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void BadSpecificationsAndCapsAreEnforced()
        {
            Action one = () => Chart.ParallelCoordinates().Dimension("A", [1.0]).Render();
            one.Should().Throw<ChartSpecificationException>();
            Action length = () => Chart.ParallelCoordinates().Dimension("A", [1.0, 2]).Dimension("B", [1.0]).Render();
            length.Should().Throw<ChartSpecificationException>();

            Action groups = () =>
                Chart.ParallelCoordinates().Dimension("A", [1.0, 2]).Dimension("B", [1.0, 2]).Groups(["x"]).Render();
            groups.Should().Throw<ChartSpecificationException>();
            var big = Enumerable.Range(0, 30000).Select(i => (double)i).ToArray();
            Action cap = () => Chart.ParallelCoordinates().Dimension("A", big).Dimension("B", big).Render();
            cap.Should().Throw<ChartLimitException>();
            var many = Enumerable.Range(0, ChartLimits.MaxSeries + 1).Select(i => "g" + i).ToArray();
            var v = many.Select((_, i) => (double)i).ToArray();
            Action series = () => Chart.ParallelCoordinates().Dimension("A", v).Dimension("B", v).Groups(many).Render();
            series.Should().Throw<ChartLimitException>();
        }
    }
}