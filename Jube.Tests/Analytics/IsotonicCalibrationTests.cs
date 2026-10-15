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
using FluentAssertions;
using Jube.Analytics;
using Jube.Analytics.Calibration;
using Jube.Analytics.Calibration.Models;
using Xunit;

namespace Jube.Test.Analytics
{
    public class IsotonicCalibrationTests
    {
        [Fact]
        public void Apply_IsNonDecreasingAcrossTheFittedRange()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            var calibrator = IsotonicCalibrator.Fit(scores, labels);

            var sorted = scores.Distinct().OrderBy(s => s).ToArray();
            var calibrated = sorted.Select(calibrator.Apply).ToArray();

            for (var i = 1; i < calibrated.Length; i++)
            {
                calibrated[i].Should().BeGreaterThanOrEqualTo(calibrated[i - 1]);
            }
        }

        [Fact]
        public void Apply_ImprovesLogLossAndBrierScore()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            var calibrator = IsotonicCalibrator.Fit(scores, labels);
            var calibrated = calibrator.Apply(scores);

            BinaryMetrics.LogLoss(calibrated, labels).Should().BeLessThan(BinaryMetrics.LogLoss(scores, labels));
            BinaryMetrics.BrierScore(calibrated, labels).Should().BeLessThan(BinaryMetrics.BrierScore(scores, labels));
        }

        [Fact]
        public void Apply_ClampsToTheBoundaryOutsideTheFittedRange()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            var calibrator = IsotonicCalibrator.Fit(scores, labels);

            var min = calibrator.Points[0];
            var max = calibrator.Points[^1];
            calibrator.Apply(min.X - 10).Should().Be(min.Y);
            calibrator.Apply(max.X + 10).Should().Be(max.Y);
        }

        [Fact]
        public void Fit_ThrowsWhenAllScoresAreEqual()
        {
            var scores = Enumerable.Repeat(0.5, 10).ToArray();
            var labels = new[] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };

            var act = () => IsotonicCalibrator.Fit(scores, labels);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Fit_WithOnlyOneClassProducesANearConstantCalibrator()
        {
            var scores = new[] { 0.1, 0.2, 0.3, 0.4 };
            var labels = new[] { 1, 1, 1, 1 };

            var calibrator = IsotonicCalibrator.Fit(scores, labels);

            calibrator.Points.Should().OnlyContain(p => p.Y > 0.99);
        }

        [Fact]
        public void Fit_ThrowsOnNonFiniteScores()
        {
            var scores = new[] { 0.1, double.PositiveInfinity, 0.3, 0.4 };
            var labels = new[] { 0, 1, 0, 1 };

            var act = () => IsotonicCalibrator.Fit(scores, labels);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToJson_FromJson_RoundTripsTheFittedFunction()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            var calibrator = IsotonicCalibrator.Fit(scores, labels);

            var restored = IsotonicCalibrator.FromJson(calibrator.ToJson());

            var probe = new[] { -5d, 0, 0.25, 0.5, 0.75, 1, 5 };

            foreach (var p in probe)
            {
                restored.Apply(p).Should().Be(calibrator.Apply(p));
            }
        }

        [Fact]
        public void FromState_RejectsNonIncreasingX()
        {
            var state = new IsotonicCalibratorState { X = [0.5, 0.5], Y = [0.1, 0.2] };

            var act = () => IsotonicCalibrator.FromState(state);

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void FromState_RejectsAnOutOfRangeY()
        {
            var state = new IsotonicCalibratorState { X = [0.1, 0.2], Y = [0.1, 1.5] };

            var act = () => IsotonicCalibrator.FromState(state);

            act.Should().Throw<FormatException>();
        }
    }
}