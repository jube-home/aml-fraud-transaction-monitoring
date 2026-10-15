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
using Jube.Analytics;
using Jube.Analytics.Calibration;
using Xunit;

namespace Jube.Test.Analytics
{
    public class BetaCalibrationTests
    {
        [Fact]
        public void Apply_ImprovesLogLossAndBrierScore()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            var calibrator = BetaCalibration.Fit(scores, labels);
            var calibrated = calibrator.Apply(scores);

            BinaryMetrics.LogLoss(calibrated, labels).Should().BeLessThan(BinaryMetrics.LogLoss(scores, labels));
            BinaryMetrics.BrierScore(calibrated, labels).Should().BeLessThan(BinaryMetrics.BrierScore(scores, labels));
        }

        [Fact]
        public void Apply_AlwaysReturnsAProbability()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            var calibrator = BetaCalibration.Fit(scores, labels);

            foreach (var score in scores)
            {
                var p = calibrator.Apply(score);
                p.Should().BeInRange(0, 1);
            }
        }

        [Fact]
        public void Fit_ThrowsWhenScoresAreOutsideTheUnitInterval()
        {
            var scores = new[] { 0.1, 0.2, 1.5, 0.4 };
            var labels = new[] { 0, 1, 0, 1 };

            var act = () => BetaCalibration.Fit(scores, labels);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Fit_ThrowsWithOnlyOneClass()
        {
            var scores = new[] { 0.1, 0.2, 0.3, 0.4 };
            var labels = new[] { 1, 1, 1, 1 };

            var act = () => BetaCalibration.Fit(scores, labels);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Fit_SilentlyDropsNaNScoresLikePlatt()
        {
            var scores = new[] { 0.1, double.NaN, 0.3, 0.4 };
            var labels = new[] { 0, 1, 0, 1 };

            var act = () => BetaCalibration.Fit(scores, labels);

            act.Should().NotThrow();
        }

        [Fact]
        public void Fit_ThrowsOnInfiniteScores()
        {
            var scores = new[] { 0.1, double.PositiveInfinity, 0.3, 0.4 };
            var labels = new[] { 0, 1, 0, 1 };

            var act = () => BetaCalibration.Fit(scores, labels);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToPrimitives_FromPrimitives_RoundTrips()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            var calibrator = BetaCalibration.Fit(scores, labels);

            var restored = BetaCalibrator.FromPrimitives(calibrator.ToPrimitives());

            restored.Should().Be(calibrator);
        }

        [Fact]
        public void FromPrimitives_RejectsAnOutOfRangeEpsilon()
        {
            var act = () => BetaCalibrator.FromPrimitives([1, 1, 0, 0.9]);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FromPrimitives_RejectsTheWrongLength()
        {
            var act = () => BetaCalibrator.FromPrimitives([1, 1, 0]);

            act.Should().Throw<ArgumentException>();
        }
    }
}