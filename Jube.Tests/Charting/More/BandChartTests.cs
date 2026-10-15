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
using Jube.Charting.More;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.More
{
    public class BandChartTests
    {
        private static readonly double[] anscombeX = [10, 8, 13, 9, 11, 14, 6, 4, 12, 7, 5];

        private static readonly double[] anscombeY =
            [8.04, 6.95, 7.58, 8.81, 8.33, 9.96, 7.24, 4.26, 10.84, 4.82, 5.68];

        [Fact]
        public void RegressionMatchesTheIndependentReference()
        {
            var fit = RegressionMath.Fit(anscombeX, anscombeY);
            var raw = fit.RawCoefficients();
            raw[0].Should().BeApproximately(3.0000909090909085, 1e-9);
            raw[1].Should().BeApproximately(0.5000909090909091, 1e-9);
            fit.RSquared.Should().BeApproximately(0.666542459508775, 1e-9);
            fit.ResidualStandardError.Should().BeApproximately(1.236603322726321, 1e-9);
            fit.DegreesOfFreedom.Should().Be(9);
            fit.Predict(10).Should().BeApproximately(8.001, 1e-9);
        }

        [Theory]
        [InlineData(10, 0.8846127268361711, 2.9339284996887347)]
        [InlineData(4, 1.5779413710971193, 3.2117434105832796)]
        [InlineData(20, 3.05275877644749, 4.1406198705003625)]
        public void RegressionIntervalsMatchTheIndependentReference(double x, double meanHalf, double predictionHalf)
        {
            var fit = RegressionMath.Fit(anscombeX, anscombeY);
            var (ml, mu) = fit.MeanInterval(x);
            var (pl, pu) = fit.PredictionInterval(x);
            ((mu - ml) / 2).Should().BeApproximately(meanHalf, 1e-9);
            ((pu - pl) / 2).Should().BeApproximately(predictionHalf, 1e-9);
        }

        [Fact]
        public void QuadraticRegressionRecoversExactCoefficients()
        {
            var xs = Enumerable.Range(0, 12).Select(i => (double)i).ToArray();
            var ys = xs.Select(x => 1 + 2 * x + 3 * x * x).ToArray();
            var fit = RegressionMath.Fit(xs, ys, 2);
            var raw = fit.RawCoefficients();
            raw[0].Should().BeApproximately(1, 1e-6);
            raw[1].Should().BeApproximately(2, 1e-6);
            raw[2].Should().BeApproximately(3, 1e-6);
            fit.RSquared.Should().BeApproximately(1, 1e-12);
        }

        [Fact]
        public void RegressionRejectsBadInput()
        {
            Action few = () => RegressionMath.Fit([1.0, 2], [1.0, 2]);
            few.Should().Throw<ChartSpecificationException>();
            Action same = () => RegressionMath.Fit([1.0, 1, 1, 1], [1.0, 2, 3, 4]);
            same.Should().Throw<ChartSpecificationException>();
            Action degree = () => RegressionMath.Fit(anscombeX, anscombeY, 5);
            degree.Should().Throw<ChartSpecificationException>();
            Action level = () => RegressionMath.Fit(anscombeX, anscombeY, 1, 1.2);
            level.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void ForecastIsDashedAndCarriesBothBandsAndMarkers()
        {
            var svg = MoreSamples.Forecast().Render();

            svg.Should().Contain("class=\"ln dash\"").And.Contain("class=\"cib o\"").And.Contain("class=\"cib\"")
                .And.Contain("class=\"evl cp\"").And.Contain("class=\"evl\"").And.Contain("Campaign launch");

            svg.Should().Contain("80% interval").And.Contain("Forecast: ");

            Chart.TimeSeries().Forecast("Projected", [1.0, 2, 3], [1.0, 2, 3], 1).Render().Should()
                .Contain("Projected (forecast): 3");

            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void NaNValuesBreakLinesAndBandsIntoGaps()
        {
            var svg = Chart.TimeSeries().Series("s", [0.0, 1, 2, 3, 4, 5], [1.0, 2, double.NaN, 4, 5, 6])
                .Interval("b", [0.0, 1, double.NaN, 3, 4, 5], [2.0, 3, double.NaN, 5, 6, 7]).Render();
            SvgAssert.IsSafe(svg);
            var line = Regex.Match(svg, "<path class=\"ln\" d=\"([^\"]+)\"").Groups[1].Value;
            Regex.Matches(line, "M").Count.Should().Be(2);
            var band = Regex.Match(svg, "<path class=\"cib\" d=\"([^\"]+)\"").Groups[1].Value;
            Regex.Matches(band, "Z").Count.Should().Be(2);
        }

        [Fact]
        public void RangeSelectorClipsTheMainPlotAndShowsTheWindow()
        {
            var svg = MoreSamples.RangeSelected().Render();

            svg.Should().Contain("class=\"rsw\"").And.Contain("class=\"rsd\"").And
                .Contain("Selected: 2026-03-02 to 2026-04-21");

            svg.Should().Contain("Mar 5");
            svg.Should().NotContain(">Jan 1<");
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void ErrorBarsAppearForPointsAndTooltipsCarryThem()
        {
            var svg = MoreSamples.ErrorBars().Render();

            svg.Should().Contain("class=\"ebs\"").And.Contain("Error: -").And.Contain("X error: -").And
                .Contain("95% band");

            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void StepSeriesRenderHorizontalThenVerticalSegments()
        {
            var svg = Chart.StepChart().Series("s", [0.0, 1, 2], [1.0, 3, 2]).Render();
            var path = Regex.Match(svg, "<path class=\"ln\" d=\"([^\"]+)\"").Groups[1].Value;
            Regex.Matches(path, "L").Count.Should().Be(4);
        }

        [Fact]
        public void BadSpecificationsAndCapsAreEnforced()
        {
            Action mismatch = () => Chart.TimeSeries().Series("s", [1.0, 2], [1.0]).Render();
            mismatch.Should().Throw<ChartSpecificationException>();
            Action band = () => Chart.TimeSeries().Series("s", [1.0, 2], [1.0, 2]).Interval("b", [1.0], [2.0]).Render();
            band.Should().Throw<ChartSpecificationException>();
            Action window = () => Chart.TimeSeries().Series("s", [1.0, 2], [1.0, 2]).RangeSelector(5, 1).Render();
            window.Should().Throw<ChartSpecificationException>();
            Action ev = () => Chart.TimeSeries().Series("s", [1.0, 2], [1.0, 2]).Event(double.NaN, "x").Render();
            ev.Should().Throw<ChartSpecificationException>();
            Action orphan = () => Chart.TimeSeries().Interval("b", [1.0], [2.0]);
            orphan.Should().Throw<ChartSpecificationException>();
            var big = Enumerable.Range(0, ChartLimits.MaxPoints + 1).Select(i => (double)i).ToArray();
            Action points = () => Chart.TimeSeries().Series("s", big, big).Render();
            points.Should().Throw<ChartLimitException>();
            var many = Chart.TimeSeries();

            for (var i = 0; i < ChartLimits.MaxSeries + 1; i++)
            {
                many.Series("s" + i, [1.0, 2], [1.0, 2]);
            }

            Action series = () => many.Render();
            series.Should().Throw<ChartLimitException>();
        }

        [Fact]
        public void EmptyChartsRenderNoData()
        {
            Chart.TimeSeries().Render().Should().Contain("No data");
            Chart.Regression().Invoking(b => b.Render()).Should().NotThrow();
        }
    }
}