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
using Jube.Analytics.Models;
using Jube.Engine.Exhaustive.Calibration;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class ExhaustiveCalibrationValueTests
    {
        private static ExhaustiveCalibration Platt(string input = "logit", double slope = 2, double intercept = -0.5,
            double epsilon = 1e-6, double? prior = 0.5, double? baseRate = 0.02)
        {
            return ExhaustiveCalibration.FromColumns("platt", input, slope, intercept, epsilon, prior, baseRate,
                "PopulationCount");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("none")]
        public void NoStoredMethod_IsTheIdentityAndReturnsTheRawScoreByteForByte(string method)
        {
            var calibration = ExhaustiveCalibration.FromColumns(method, null, null, null, null, null, null, null);

            calibration.IsCalibrated.Should().BeFalse();
            calibration.IsInvalid.Should().BeFalse();
            calibration.Status.Should().Be("NotCalibrated");

            foreach (var raw in new[] { 0.123456789012345, -0.7, 1.7, 0d, 1d })
            {
                BitConverter.DoubleToInt64Bits(calibration.Apply(raw)).Should()
                    .Be(BitConverter.DoubleToInt64Bits(raw));
            }

            ExhaustiveCalibration.Identity.Apply(0.3).Should().Be(0.3);
        }

        [Fact]
        public void Apply_IsPlattThenThePriorShiftToTheBaseRate()
        {
            var calibration = Platt();

            var expected = new Calibrator(2, -0.5, CalibrationInput.LogitOfBoundedScore, 1e-6)
                .Apply(0.7, 0.5, 0.02);

            calibration.Apply(0.7).Should().Be(expected);
            calibration.Apply(0.7).Should().BeLessThan(0.1);
            calibration.IsCalibrated.Should().BeTrue();
            calibration.Slope.Should().Be(2);
            calibration.Intercept.Should().Be(-0.5);
            calibration.BaseRate.Should().Be(0.02);
            calibration.BaseRateSource.Should().Be("PopulationCount");
        }

        [Fact]
        public void Apply_WithoutABaseRate_StaysAtTheSamplePrior()
        {
            var calibration = Platt(baseRate: null);

            calibration.Apply(0.7).Should().Be(calibration.ApplyAtSamplePrior(0.7));
        }

        [Fact]
        public void Apply_IsStrictlyMonotoneInTheRawScore()
        {
            var calibration = Platt();
            var previous = -1d;

            for (var raw = 0.001; raw < 1; raw += 0.01)
            {
                var value = calibration.Apply(raw);
                value.Should().BeGreaterThan(previous);
                previous = value;
            }
        }

        [Fact]
        public void Apply_RawInputMode_UsesTheScoreDirectly()
        {
            var calibration = Platt("raw", 3, -1, 1e-6, 0.4, 0.05);

            calibration.Apply(0.9).Should().Be(new Calibrator(3, -1, CalibrationInput.RawScore, 1e-6)
                .Apply(0.9, 0.4, 0.05));
        }

        [Fact]
        public void Apply_NonFiniteRawValuesPassThroughUnchanged()
        {
            var calibration = Platt();

            double.IsNaN(calibration.Apply(double.NaN)).Should().BeTrue();
            calibration.Apply(double.PositiveInfinity).Should().Be(double.PositiveInfinity);
        }

        [Theory]
        [InlineData("unknown")]
        public void AnUnknownMethod_IsInvalid(string method)
        {
            var calibration = ExhaustiveCalibration.FromColumns(method, "raw", 1, 0, 1e-6, 0.5, 0.1, "x");

            calibration.IsInvalid.Should().BeTrue();
            calibration.Status.Should().Be("Invalid");
        }

        [Fact]
        public void CorruptColumns_AreInvalidAndNeverSilentlyRaw()
        {
            var corrupt = new[]
            {
                ExhaustiveCalibration.FromColumns("platt", "sideways", 1, 0, 1e-6, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", null, 0, 1e-6, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 1, null, 1e-6, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 1, 0, null, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", double.NaN, 0, 1e-6, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 0, 0, 1e-6, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", -2, 0, 1e-6, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 1, 0, 0.7, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 1, 0, 0, 0.5, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 1, 0, 1e-6, 0.5, 1.5, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 1, 0, 1e-6, 0.5, 0, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 1, 0, 1e-6, null, 0.1, "x"),
                ExhaustiveCalibration.FromColumns("platt", "raw", 1, 0, 1e-6, 1, 0.1, "x")
            };

            foreach (var calibration in corrupt)
            {
                calibration.IsInvalid.Should().BeTrue();
                calibration.InvalidReason.Should().NotBeNullOrEmpty();
                var act = () => calibration.Apply(0.5);
                act.Should().Throw<InvalidOperationException>().WithMessage("*invalid*");
                var atSample = () => calibration.ApplyAtSamplePrior(0.5);
                atSample.Should().Throw<InvalidOperationException>();
            }
        }
    }
}