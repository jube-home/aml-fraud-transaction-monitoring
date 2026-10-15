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
using Jube.Charting.Exceptions;
using Jube.Charting.More;
using Jube.Charting.More.Models;
using Xunit;

namespace Jube.Test.Charting.More
{
    public class ScatterMatrixTests
    {
        [Fact]
        public void PearsonMatchesKnownAnswers()
        {
            ChartStats.Pearson([1.0, 2, 3, 4], [2.0, 4, 6, 8], out var n).Should().BeApproximately(1, 1e-12);
            n.Should().Be(4);
            ChartStats.Pearson([1.0, 2, 3, 4], [8.0, 6, 4, 2], out _).Should().BeApproximately(-1, 1e-12);
            var x = new[] { 10.0, 8, 13, 9, 11, 14, 6, 4, 12, 7, 5 };
            var y = new[] { 8.04, 6.95, 7.58, 8.81, 8.33, 9.96, 7.24, 4.26, 10.84, 4.82, 5.68 };
            ChartStats.Pearson(x, y, out _).Should().BeApproximately(0.81642051634484, 1e-9);
            double.IsNaN(ChartStats.Pearson([1.0, 1, 1], [1.0, 2, 3], out _)).Should().BeTrue();

            ChartStats.Pearson([1.0, double.NaN, 3, 4], [2.0, 5, 6, 8], out var pairs).Should()
                .BeApproximately(1, 1e-9);

            pairs.Should().Be(3);
        }

        [Fact]
        public void EveryOffDiagonalAndDiagonalCellIsDrawn()
        {
            var svg = MoreSamples.ScatterMatrix().Render();
            Regex.Matches(svg, "class=\"cf\"").Count.Should().Be(16);
            Regex.Matches(svg, "class=\"cell m d\\d+\"").Count.Should().Be(6);
            Regex.Matches(svg, "class=\"pt dot\"").Count.Should().Be(6 * 90);
            svg.Should().Contain("Correlation: 0.");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void HoveringARowLinksItAcrossEveryScatterCell()
        {
            var svg = MoreSamples.ScatterMatrix().Render();
            svg.Should().Contain(":has(.mk.r0:hover) .mk.r0 .pt").And.Contain(":has(.mk.r89:hover) .mk.r89 .pt");
            Regex.Matches(svg, "class=\"mk s\\d r7\"").Count.Should().Be(6);
        }

        [Fact]
        public void UpperTriangleModesAndNaNAreHandled()
        {
            var values = new[] { 1.0, 2, double.NaN, 4, 5 };

            var scatter = Chart.ScatterMatrix().Variable("a", values).Variable("b", [5.0, 4, 3, 2, 1])
                .Upper(MatrixUpper.Scatter).Render();

            Regex.Matches(scatter, "class=\"pt dot\"").Count.Should().Be(2 * 4);

            var blank = Chart.ScatterMatrix().Variable("a", values).Variable("b", [5.0, 4, 3, 2, 1])
                .Upper(MatrixUpper.Blank).Render();

            Regex.Matches(blank, "class=\"pt dot\"").Count.Should().Be(4);
            blank.Should().NotContain("Correlation:");
            MoreAssert.IsSafe(scatter);
        }

        [Fact]
        public void DiagonalHistogramCountsEveryFiniteValue()
        {
            var svg = Chart.ScatterMatrix().Variable("a", [1.0, 2, 2, 3, 9]).Variable("b", [1.0, 2, 3, 4, 5]).Bins(4)
                .Render();

            var counts = Regex.Matches(svg, ">Count: (\\d+)<").Select(m => int.Parse(m.Groups[1].Value)).ToArray();
            counts.Sum().Should().Be(10);
        }

        [Fact]
        public void BadSpecificationsAndCapsAreEnforced()
        {
            Action one = () => Chart.ScatterMatrix().Variable("a", [1.0, 2]).Render();
            one.Should().Throw<ChartSpecificationException>();
            Action length = () => Chart.ScatterMatrix().Variable("a", [1.0, 2]).Variable("b", [1.0]).Render();
            length.Should().Throw<ChartSpecificationException>();
            Action bins = () => Chart.ScatterMatrix().Variable("a", [1.0, 2]).Variable("b", [1.0, 2]).Bins(1).Render();
            bins.Should().Throw<ChartSpecificationException>();

            Action groups = () =>
                Chart.ScatterMatrix().Variable("a", [1.0, 2]).Variable("b", [1.0, 2]).Groups(["x"]).Render();
            groups.Should().Throw<ChartSpecificationException>();
            var builder = Chart.ScatterMatrix();

            for (var i = 0; i < ScatterMatrixChart.MaxVariables + 1; i++)
            {
                builder.Variable("v" + i, [1.0, 2]);
            }

            Action variables = () => builder.Render();
            variables.Should().Throw<ChartLimitException>();
            var big = Enumerable.Range(0, 20000).Select(i => (double)i).ToArray();

            Action points = () =>
                Chart.ScatterMatrix().Variable("a", big).Variable("b", big).Variable("c", big).Render();
            points.Should().Throw<ChartLimitException>();
            Chart.ScatterMatrix().Render().Should().Contain("No data");
        }

        [Fact]
        public void ManyRowsRenderPlainPointsWithoutMarks()
        {
            var v = Enumerable.Range(0, 800).Select(i => (double)(i % 37)).ToArray();

            var svg = Chart.ScatterMatrix().Variable("a", v).Variable("b", v.Reverse()).Variable("c", v)
                .Upper(MatrixUpper.Scatter).Render();
            svg.Should().NotContain("tabindex=\"0\"");
            MoreAssert.IsSafe(svg);
        }
    }
}