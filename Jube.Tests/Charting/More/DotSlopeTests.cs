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
    public class DotSlopeTests
    {
        [Fact]
        public void DotPlotSortsByTheChosenSeriesAndDrawsOneDotPerValue()
        {
            var svg = MoreSamples.Dots(DotStyle.Dot).Render();
            Regex.Matches(svg, "class=\"pt sr s\\d\"").Count.Should().Be(16);
            Regex.Matches(svg, "class=\"rg\"").Count.Should().Be(8);

            var labels = Regex.Matches(svg, "<text class=\"tk end mid\"[^>]*>([^<]+)</text>")
                .Select(m => m.Groups[1].Value).ToArray();

            labels.Should().Equal("Italy", "France", "United Kingdom", "Poland", "Germany", "Sweden", "Spain",
                "Netherlands");

            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void LollipopDrawsStemsAndDumbbellDrawsConnectorsWithChange()
        {
            var lollipop = MoreSamples.Dots(DotStyle.Lollipop).Render();
            Regex.Matches(lollipop, "class=\"stem s0\"").Count.Should().Be(8);
            var dumbbell = MoreSamples.Dots(DotStyle.Dumbbell).Render();
            Regex.Matches(dumbbell, "class=\"dbc\"").Count.Should().Be(8);
            dumbbell.Should().Contain("Change: -17 (-27.9%)");
            MoreAssert.IsSafe(lollipop);
            MoreAssert.IsSafe(dumbbell);
        }

        [Fact]
        public void VerticalDotPlotsAndMissingValuesRender()
        {
            var svg = Chart.DotPlot().Vertical().Categories(["a", "b", "c"]).Series("s", [1.0, double.NaN, 3]).Render();
            MoreAssert.IsSafe(svg);
            Regex.Matches(svg, "class=\"pt sr s0\"").Count.Should().Be(2);
        }

        [Fact]
        public void DotChartsRejectBadSpecifications()
        {
            Action length = () => Chart.DotPlot().Categories(["a", "b"]).Series("s", [1.0]).Render();
            length.Should().Throw<ChartSpecificationException>();
            Action dumbbell = () => Chart.Dumbbell().Categories(["a"]).Series("s", [1.0]).Render();
            dumbbell.Should().Throw<ChartSpecificationException>();

            Action sort = () =>
                Chart.DotPlot().Categories(["a"]).Series("s", [1.0]).Sorted(DotSort.Ascending, 4).Render();
            sort.Should().Throw<ChartSpecificationException>();
            var many = Chart.DotPlot().Categories(["a"]);

            for (var i = 0; i < ChartLimits.MaxSeries + 1; i++)
            {
                many.Series("s" + i, [1.0]);
            }

            Action cap = () => many.Render();
            cap.Should().Throw<ChartLimitException>();
            Chart.DotPlot().Render().Should().Contain("No data");
        }

        [Fact]
        public void RanksUseCompetitionRankingAndSkipMissingValues()
        {
            var ranks = SlopeChart.Rank([[10.0, 5, double.NaN], [10.0, 7, 3], [4.0, 7, 9]], false);
            ranks[0].Should().Equal(1.0, 3.0, double.NaN);
            ranks[1].Should().Equal(1.0, 1.0, 2.0);
            ranks[2].Should().Equal(3.0, 1.0, 1.0);
            var lowest = SlopeChart.Rank([[1.0], [2.0], [2.0]], true);
            lowest.Select(r => r[0]).Should().Equal(1.0, 2.0, 2.0);
        }

        [Fact]
        public void LabelSpreadingEnforcesTheGapAndStaysInBounds()
        {
            var positions = new[] { 50.0, 51, 52, 53, 200 };
            SlopeChart.Spread(positions, 13, 10, 300);

            for (var i = 1; i < positions.Length; i++)
            {
                (positions[i] - positions[i - 1]).Should().BeGreaterThanOrEqualTo(12.9);
            }

            positions.All(p => p >= 10 && p <= 300).Should().BeTrue();
            positions[4].Should().BeApproximately(200, 1e-9);
        }

        [Fact]
        public void BumpChartShowsRankInsideEveryPointAndSmoothCurves()
        {
            var svg = MoreSamples.Bump().Render();
            Regex.Matches(svg, "class=\"rk ctr mid on\"").Count.Should().Be(30);
            svg.Should().Contain("C").And.Contain("Rank: 1 of 6");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void SlopeChartLabelsBothEndsAndTooltipCarriesChange()
        {
            var svg = MoreSamples.Slope().Render();

            svg.Should().Contain("Card not present  7.9").And.Contain("9.4  Card not present").And
                .Contain("Change: +1.5");

            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void MissingPointsBreakTheLineAndBadSpecsAreRejected()
        {
            var svg = Chart.Slope().Periods(["a", "b", "c", "d"]).Series("s", [1.0, 2, double.NaN, 4]).Render();
            MoreAssert.IsSafe(svg);

            Regex.Matches(Regex.Match(svg, "<path class=\"ln\" d=\"([^\"]+)\"").Groups[1].Value, "M").Count.Should()
                .Be(2);

            Action one = () => Chart.Slope().Periods(["a"]).Series("s", [1.0]).Render();
            one.Should().Throw<ChartSpecificationException>();
            Action length = () => Chart.Slope().Periods(["a", "b"]).Series("s", [1.0]).Render();
            length.Should().Throw<ChartSpecificationException>();
            Chart.Bump().Render().Should().Contain("No data");
        }
    }
}