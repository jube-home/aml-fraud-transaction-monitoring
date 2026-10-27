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
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Xunit;

namespace Jube.Test.CaseRisk.Journal
{
    [Trait("Category", "Unit")]
    public sealed class RobustStatisticsTests
    {
        private static readonly double[] oneToNine = [1, 2, 3, 4, 5, 6, 7, 8, 9];

        [Fact]
        public void TheFitIsTheMedianAndTheScaledMedianAbsoluteDeviation()
        {
            var fit = RobustStatistics.Fit(oneToNine);

            fit.Count.Should().Be(9);
            fit.Median.Should().Be(5);
            fit.Scale.Should().BeApproximately(2.9652, 1e-4);
            fit.Q1.Should().Be(3);
            fit.Q3.Should().Be(7);
        }

        [Fact]
        public void AZScoreIsTheDistanceInScalesFromTheMedianAndIsClamped()
        {
            var fit = RobustStatistics.Fit(oneToNine);

            RobustStatistics.ZScore(fit, 5).Should().Be(0);
            RobustStatistics.ZScore(fit, 11).Should().BeApproximately(2.0235, 1e-3);
            RobustStatistics.ZScore(fit, -1000).Should().Be(-RobustStatistics.MaxZ);
            RobustStatistics.ZScore(fit, 1000).Should().Be(RobustStatistics.MaxZ);
        }

        [Fact]
        public void FewerThanFiveValuesCannotBeJudgedAndNonFiniteValuesAreIgnored()
        {
            RobustStatistics.ZScore(RobustStatistics.Fit([1, 2, 3, 4]), 100).Should().BeNull();
            RobustStatistics.ZScore(RobustStatistics.Fit(oneToNine), double.NaN).Should().BeNull();
            RobustStatistics.Fit([1, double.NaN, double.PositiveInfinity, 3]).Count.Should().Be(2);
        }

        [Fact]
        public void AConstantHistoryJudgesAnyDeviationAsTheLargestPossible()
        {
            var fit = RobustStatistics.Fit([7, 7, 7, 7, 7, 7]);

            fit.Scale.Should().Be(0);
            RobustStatistics.ZScore(fit, 7).Should().Be(0);
            RobustStatistics.ZScore(fit, 8).Should().Be(RobustStatistics.MaxZ);
            RobustStatistics.ZScore(fit, 6).Should().Be(-RobustStatistics.MaxZ);
        }

        [Fact]
        public void WhenTheMedianDeviationIsZeroTheInterquartileRangeProvidesTheScale()
        {
            var fit = RobustStatistics.Fit([1, 1, 1, 1, 1, 1, 1, 5, 9, 9]);

            fit.Scale.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AnEmptyFitHasNoCountAndNoMedian()
        {
            var fit = RobustStatistics.Fit([]);

            fit.Count.Should().Be(0);
            double.IsNaN(fit.Median).Should().BeTrue();
            fit.Confident.Should().BeFalse();
        }

        [Fact]
        public void QuantilesInterpolateAndHandleOneValue()
        {
            RobustStatistics.Quantile([10], 0.9).Should().Be(10);
            RobustStatistics.Quantile([0, 10], 0.25).Should().Be(2.5);
            double.IsNaN(RobustStatistics.Quantile([], 0.5)).Should().BeTrue();
        }

        [Fact]
        public void ConfidenceStartsAtTwentyValues()
        {
            RobustStatistics.Fit(new double[19]).Confident.Should().BeFalse();
            RobustStatistics.Fit(new double[20]).Confident.Should().BeTrue();
        }
    }
}
