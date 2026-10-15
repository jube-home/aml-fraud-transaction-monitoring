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
using Jube.Charting.Charts;
using Jube.Charting.Interfaces;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting
{
    public class ChartFamilyTests
    {
        [Fact]
        public void SplineLinesUseCubicSegmentsAndStraightLinesDoNot()
        {
            var smooth = Chart.Line().Smooth().Series("s", [1.0, 5, 2, 8, 3]).Render();
            var straight = Chart.Line().Series("s", [1.0, 5, 2, 8, 3]).Render();
            Regex.Match(smooth, "<path class=\"ln\" d=\"([^\"]+)\"").Groups[1].Value.Should().Contain("C");
            Regex.Match(straight, "<path class=\"ln\" d=\"([^\"]+)\"").Groups[1].Value.Should().NotContain("C");
            SvgAssert.IsSafe(smooth);
        }

        [Fact]
        public void DataLabelsAppearOnLinesAndStackedTotalsOnColumns()
        {
            var line = Chart.Line().DataLabels().Series("s", [1.0, 2.5, 2]).Render();
            line.Should().Contain("class=\"lbl ctr\"");
            line.Should().Contain(">2.5<");

            var stacked = Chart.Column().Categories(["a"]).Stacked().ShowValues().Series("x", [3.0]).Series("y", [4.0])
                .Render();

            Regex.Matches(stacked, "class=\"lbl ctr\"[^>]*>7<").Count.Should().Be(1);
        }

        [Fact]
        public void SplineDoesNotOvershootMonotoneData()
        {
            var svg = Chart.Line().Smooth().Series("s", [0.0, 0, 10, 10, 10]).YDomain(0, 10).Render();
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void SplineBreaksAtGaps()
        {
            var svg = Chart.Line().Smooth().Series("s", [1.0, 3, 2, double.NaN, 4, 6, 5]).Render();
            var path = Regex.Match(svg, "<path class=\"ln\" d=\"([^\"]+)\"").Groups[1].Value;
            Regex.Matches(path, "M").Count.Should().Be(2);
        }

        [Fact]
        public void SecondaryAxisRendersItsOwnLabelsAndTitle()
        {
            var svg = ChartSamples.LineSecondary().Render();
            svg.Should().Contain("Confirmation rate");
            svg.Should().Contain("rotate(90)");
            svg.Should().Contain("class=\"tk mid\"");
        }

        [Fact]
        public void BandsLinesAndAnnotationsAreDrawn()
        {
            var svg = ChartSamples.LineSecondary().Render();
            svg.Should().Contain("class=\"band\"");
            svg.Should().Contain("Capacity warning");
            svg.Should().Contain("class=\"an\"");
            svg.Should().Contain("Campaign start");
            svg.Should().Contain("class=\"ref\"");
        }

        [Fact]
        public void VerticalBandsAndLinesWorkOnNumericAxes()
        {
            var svg = Chart.Line().Series("s", [1.0, 2, 3, 4]).VerticalBand(1, 2, "Window").VerticalLine(2.5, "Cut")
                .Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("Window");
        }

        [Fact]
        public void PercentStackedAreaHasAHundredPercentAxis()
        {
            var svg = ChartSamples.AreaPercent().Render();
            svg.Should().Contain(">100%<");
            svg.Should().Contain("100% stacked area chart");
        }

        [Fact]
        public void PercentStackedColumnsShowSharesAndAHundredPercentAxis()
        {
            var svg = ChartSamples.ColumnPercent().Render();
            svg.Should().Contain(">100%<");
            svg.Should().Contain("Share: 12%");
            svg.Should().Contain("100% stacked bar chart");
        }

        [Fact]
        public void RangeBarsSpanFromTo()
        {
            var svg = ChartSamples.BarRange().Render();
            svg.Should().Contain("Duration: 1 to 6");
            svg.Should().Contain("Service level breach");
        }

        [Fact]
        public void PerPointColoursAreAppliedToBars()
        {
            var svg = Chart.Column().Categories(["a", "b"]).Series("s", [1.0, 2]).PointColors(["#112233", null])
                .Render();
            svg.Should().Contain("style=\"--sc:#112233\"");
            Action bad = () => Chart.Column().Categories(["a"]).Series("s", [1.0]).PointColors(["red"]).Render();
            bad.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void ComboCombinesColumnsLinesAndASecondaryAxis()
        {
            var svg = ChartSamples.Combo().Render();
            svg.Should().Contain("class=\"bar m\"");
            svg.Should().Contain("class=\"ln\"");
            svg.Should().Contain("18%");
            svg.Should().Contain("Combination chart");
        }

        [Fact]
        public void ComboSeriesMustMatchCategories()
        {
            Action act = () => Chart.Combo().Categories(["a", "b"]).Columns("c", [1.0]).Render();
            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void TrendLinesCarryTheRegressionAndSeriesColour()
        {
            var svg = Chart.Scatter().Series("s", [1.0, 2, 3, 4], [2.0, 4, 6, 8]).TrendLines().Render();
            svg.Should().Contain("s trend: y = 2 x + 0 (R squared 1)");
            svg.Should().Contain("class=\"ref s0\"");
        }

        [Fact]
        public void BoxPlotQuartilesMatchKnownAnswers()
        {
            var sorted = Enumerable.Range(1, 9).Select(i => (double)i).ToArray();
            BoxPlotChartBuilder.Quantile(sorted, 0.25).Should().Be(3);
            BoxPlotChartBuilder.Quantile(sorted, 0.5).Should().Be(5);
            BoxPlotChartBuilder.Quantile(sorted, 0.75).Should().Be(7);
            BoxPlotChartBuilder.Quantile([1.0, 2, 3, 4], 0.5).Should().Be(2.5);
        }

        [Fact]
        public void BoxPlotWhiskersStopAtTheFencesAndOutliersAreCounted()
        {
            var chart = Chart.BoxPlot().FromValues("g", [1.0, 2, 3, 4, 5, 6, 7, 8, 9, 100]).Build();
            var box = chart.Spec.Boxes[0];
            box.Max.Should().Be(9);
            box.Outliers.Should().Equal(100);
            var svg = chart.Render();
            svg.Should().Contain("Outliers: 1");
            svg.Should().Contain("Observations: 10");
        }

        [Fact]
        public void BoxPlotWithNoValuesRenders()
        {
            SvgAssert.IsSafe(Chart.BoxPlot().FromValues("empty", []).Render());
        }

        [Fact]
        public void CandlesSplitRisingAndFalling()
        {
            var svg = Chart.Candlestick().Candle("a", 10, 12, 9, 11).Candle("b", 11, 12, 8, 9).Render();
            svg.Should().Contain("class=\"cd hollow m\"");
            svg.Should().Contain("class=\"cd m\"");
            svg.Should().Contain("Change: 10%");
            svg.Should().Contain("Change: -18.18%");
        }

        [Fact]
        public void OhlcDrawsTicksInsteadOfBodies()
        {
            var svg = Chart.Candlestick().Ohlc().Candle("a", 10, 12, 9, 11).Render();
            svg.Should().Contain("class=\"wk m\"");
            svg.Should().NotContain("class=\"cd");
        }

        [Fact]
        public void WaterfallRunningTotalsAreCorrect()
        {
            var svg = ChartSamples.Waterfall().Render();
            svg.Should().Contain("Running total: 580");
            svg.Should().Contain("Running total: 400");
            svg.Should().Contain("Running total: 525");
            svg.Should().Contain("+90");
        }

        [Fact]
        public void FunnelShowsConversionAgainstFirstAndPreviousStage()
        {
            var svg = ChartSamples.Funnel().Render();
            svg.Should().Contain("Of first stage: 61.7%");
            svg.Should().Contain("Of previous stage: 28.4%");
        }

        [Fact]
        public void RadarNeedsThreeAxesAndRejectsMismatchedSeries()
        {
            Chart.Radar().Axes(["a", "b"]).Series("s", [1.0, 2]).Render().Should().Contain("at least three axes");
            Action act = () => Chart.Radar().Axes(["a", "b", "c"]).Series("s", [1.0]).Render();
            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void RadarDrawsRingsSpokesAndFilledPolygons()
        {
            var svg = ChartSamples.Radar().Render();
            svg.Should().Contain("Radar chart with 2 series");
            svg.Should().Contain("class=\"ar\"");
            Regex.Matches(svg, "class=\"gl\"").Count.Should().BeGreaterThan(6);
        }

        [Fact]
        public void PolarConvertsAnglesClockwiseFromTop()
        {
            var svg = Chart.Polar().Series("s", [90.0], [10.0]).Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("Angle: 90");
        }

        [Fact]
        public void GaugeShowsValueTargetAndClampsOutOfRangeValues()
        {
            var svg = Chart.Gauge().Range(0, 100).Value(250, "Load").Target(80).Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("Target: 80");
            svg.Should().Contain(">250<");
        }

        [Fact]
        public void GaugeWithNonFiniteValueFallsBackToTheMinimum()
        {
            SvgAssert.IsSafe(Chart.Gauge().Value(double.NaN).Render());
        }

        [Fact]
        public void BulletDrawsRangesBarsAndTargets()
        {
            var svg = ChartSamples.Bullet().Render();
            svg.Should().Contain("class=\"rg0\"");
            svg.Should().Contain("class=\"tgt\"");
            svg.Should().Contain("Target: 100");
            svg.Should().Contain("Bullet chart with 3 measures");
        }

        [Fact]
        public void LinearGaugeIsAThickBullet()
        {
            ChartSamples.LinearGauge().Render().Should().Contain("Linear gauge");
        }

        [Fact]
        public void SparklineGapsBreakTheLine()
        {
            var svg = Chart.Sparkline().Values([1.0, 2, double.NaN, 3, 4]).Render();
            var path = Regex.Match(svg, "<path class=\"ln\" d=\"([^\"]+)\"").Groups[1].Value;
            Regex.Matches(path, "M").Count.Should().Be(2);
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void SparklineIsCompactAndTransparentByDefault()
        {
            var svg = Chart.Sparkline().Values([1.0, 2, 3]).Render();
            svg.Should().Contain("viewBox=\"0 0 160 36\"");
            svg.Should().NotContain("class=\"bg\"");
        }

        [Fact]
        public void SquarifyConservesAreaAndProportions()
        {
            var values = new[] { 6.0, 6, 4, 3, 2, 2, 1 };
            var rects = TreemapChart.Squarify(values, 0, 0, 600, 400);
            rects.Sum(r => r.Width * r.Height).Should().BeApproximately(600 * 400, 1e-6);

            for (var i = 0; i < values.Length; i++)
            {
                (rects[i].Width * rects[i].Height).Should().BeApproximately(values[i] / values.Sum() * 600 * 400, 1e-6);
            }

            foreach (var r in rects)
            {
                r.X.Should().BeGreaterOrEqualTo(-1e-9);
                r.Y.Should().BeGreaterOrEqualTo(-1e-9);
                (r.X + r.Width).Should().BeLessOrEqualTo(600 + 1e-9);
                (r.Y + r.Height).Should().BeLessOrEqualTo(400 + 1e-9);
            }
        }

        [Fact]
        public void TreemapTooltipsShowShares()
        {
            var svg = ChartSamples.TreemapGrouped().Render();
            svg.Should().Contain("Of group: ");
            svg.Should().Contain("Of total: ");
            ChartSamples.TreemapFlat().Render().Should().Contain("Of total: 27.3%");
        }

        [Fact]
        public void GanttMilestonesAndProgressRender()
        {
            var svg = ChartSamples.Gantt().Render();
            svg.Should().Contain("Progress: 60%");
            svg.Should().Contain("Duration: 0 days");
            svg.Should().Contain("Today");
            svg.Should().Contain("class=\"bar m part\"");
        }

        [Fact]
        public void EveryFamilyRendersWithEmptyInput()
        {
            foreach (var chart in new ISvgChart[]
                     {
                         Chart.Combo(), Chart.BoxPlot(), Chart.Candlestick(), Chart.Waterfall(), Chart.Funnel(),
                         Chart.Radar(), Chart.Polar(), Chart.Gauge(), Chart.Bullet(), Chart.LinearGauge(),
                         Chart.Sparkline(), Chart.Treemap(), Chart.Gantt(), Chart.ConfusionMatrix()
                     })
            {
                SvgAssert.IsSafe(chart.Render());
            }
        }
    }
}