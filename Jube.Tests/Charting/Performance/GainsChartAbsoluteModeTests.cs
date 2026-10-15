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

using System.Linq;
using FluentAssertions;
using Jube.Charting;
using Xunit;

namespace Jube.Test.Charting.Performance
{
    public class GainsChartAbsoluteModeTests
    {
        [Fact]
        public void AbsoluteModeStoresTheRawPositiveTotalOnTheCurve()
        {
            double[] weights = [10, 100, 5, 1];

            var chart = Chart.Gains().Absolute()
                .FromScores("Model", PerformanceData.Actual[..4], PerformanceData.Scores[..4], weights)
                .Build();

            chart.Spec.Absolute.Should().BeTrue();
            chart.Spec.Curves.Should().ContainSingle();
            var curve = chart.Spec.Curves[0];

            var expectedTotal = PerformanceData.Actual[..4]
                .Select((a, i) => a ? weights[i] : 0)
                .Sum();

            curve.TotalPositiveValue.Should().Be(expectedTotal);
        }

        [Fact]
        public void AbsoluteModeRendersWithoutThrowingAndIncludesTheRawTotalInTheSummary()
        {
            double[] weights = [10, 100, 5, 1];

            var svg = Chart.Gains().Absolute()
                .FromScores("Model", PerformanceData.Actual[..4], PerformanceData.Scores[..4], weights)
                .Render();

            svg.Should().Contain("<svg");
        }

        [Fact]
        public void NonAbsoluteModeIsUnaffectedByTheNewFlag()
        {
            var chart = Chart.Gains()
                .FromScores("Model", PerformanceData.Actual, PerformanceData.Scores)
                .Build();

            chart.Spec.Absolute.Should().BeFalse();
        }
    }
}