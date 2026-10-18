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
using System.Threading;
using FluentAssertions;
using Jube.Analytics;
using Jube.Analytics.Models;
using Xunit;

namespace Jube.Test.Analytics
{
    public class PlattCalibrationTests
    {
        [Fact]
        public void Fit_RawScore_MatchesIndependentNewton()
        {
            var calibrator = PlattCalibration.Fit(FoundationFixtures.D1Scores, FoundationFixtures.D1Labels);
            calibrator.Slope.Should().BeApproximately(2.5055244056244117, 1e-7);
            calibrator.Intercept.Should().BeApproximately(-1.148554840625531, 1e-7);
            calibrator.Mode.Should().Be(CalibrationInput.RawScore);
        }

        [Fact]
        public void Fit_LogitMode_MatchesIndependentNewton()
        {
            var calibrator = PlattCalibration.Fit(FoundationFixtures.D1Scores, FoundationFixtures.D1Labels,
                mode: CalibrationInput.LogitOfBoundedScore);
            calibrator.Slope.Should().BeApproximately(0.5274154612508071, 1e-7);
            calibrator.Intercept.Should().BeApproximately(0.10430862055589737, 1e-7);
        }

        [Fact]
        public void Fit_MiscalibratedSet_MatchesIndependentNewtonInBothModes()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            labels.Sum().Should().Be(190);

            var raw = PlattCalibration.Fit(scores, labels);
            raw.Slope.Should().BeApproximately(28.406493935694318, 1e-5);
            raw.Intercept.Should().BeApproximately(-21.263609085476382, 1e-5);

            var logit = PlattCalibration.Fit(scores, labels, mode: CalibrationInput.LogitOfBoundedScore);
            logit.Slope.Should().BeApproximately(5.337220009148282, 1e-7);
            logit.Intercept.Should().BeApproximately(-5.938376835563489, 1e-7);
        }

        [Fact]
        public void Apply_MiscalibratedSet_ImprovesLogLossBrierAndCalibrationError()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();
            var calibrator = PlattCalibration.Fit(scores, labels, mode: CalibrationInput.LogitOfBoundedScore);
            var calibrated = calibrator.Apply(scores);

            BinaryMetrics.LogLoss(scores, labels).Should().BeApproximately(0.6999541579252937, 1e-9);
            BinaryMetrics.LogLoss(calibrated, labels).Should().BeApproximately(0.3494890755613571, 1e-7);
            BinaryMetrics.BrierScore(scores, labels).Should().BeApproximately(0.25771915255176703, 1e-9);
            BinaryMetrics.BrierScore(calibrated, labels).Should().BeApproximately(0.1111787639005835, 1e-7);
            BinaryMetrics.ExpectedCalibrationError(scores, labels).Should().BeApproximately(0.31713861944473054, 1e-9);
            BinaryMetrics.ExpectedCalibrationError(calibrated, labels)
                .Should().BeApproximately(0.03433654440361132, 1e-6);

            BinaryMetrics.LogLoss(calibrated, labels).Should().BeLessThan(BinaryMetrics.LogLoss(scores, labels));
        }

        [Fact]
        public void Apply_IsStrictlyMonotoneAndPreservesAuc()
        {
            var (scores, labels) = FoundationFixtures.Miscalibrated();

            foreach (var mode in new[] { CalibrationInput.RawScore, CalibrationInput.LogitOfBoundedScore })
            {
                var calibrator = PlattCalibration.Fit(scores, labels, mode: mode);
                calibrator.IsIncreasing.Should().BeTrue();
                var sorted = scores.Distinct().OrderBy(s => s).ToArray();
                var mapped = calibrator.Apply(sorted);

                for (var i = 1; i < mapped.Length; i++)
                {
                    mapped[i].Should().BeGreaterThan(mapped[i - 1]);
                }

                BinaryMetrics.RocAuc(calibrator.Apply(scores), labels)
                    .Should().BeApproximately(BinaryMetrics.RocAuc(scores, labels), 1e-12);
            }
        }

        [Fact]
        public void Fit_InvertedScores_GivesNegativeSlopeThatIsStillMonotone()
        {
            var scores = FoundationFixtures.D1Scores.Select(s => 1 - s).ToArray();
            var calibrator = PlattCalibration.Fit(scores, FoundationFixtures.D1Labels);
            calibrator.Slope.Should().BeLessThan(0);
            calibrator.IsIncreasing.Should().BeFalse();
        }

        [Fact]
        public void Fit_DuplicatedRowEqualsWeightTwo()
        {
            var duplicated = PlattCalibration.Fit([.. FoundationFixtures.D1Scores, 0.2],
                [.. FoundationFixtures.D1Labels, 0]);

            var weighted = PlattCalibration.Fit(FoundationFixtures.D1Scores, FoundationFixtures.D1Labels,
                [1, 2, 1, 1, 1, 1, 1, 1, 1, 1]);

            duplicated.Slope.Should().BeApproximately(weighted.Slope, 1e-9);
            duplicated.Intercept.Should().BeApproximately(weighted.Intercept, 1e-9);
            weighted.Slope.Should().BeApproximately(2.932055509547933, 1e-7);
            weighted.Intercept.Should().BeApproximately(-1.459246434075449, 1e-7);
        }

        [Fact]
        public void Fit_ZeroWeightRowIsIgnored()
        {
            var baseline = PlattCalibration.Fit(FoundationFixtures.D1Scores, FoundationFixtures.D1Labels);

            var withZero = PlattCalibration.Fit([.. FoundationFixtures.D1Scores, 0.95],
                [.. FoundationFixtures.D1Labels, 0], [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0]);

            withZero.Slope.Should().BeApproximately(baseline.Slope, 1e-9);
            withZero.Intercept.Should().BeApproximately(baseline.Intercept, 1e-9);
        }

        [Fact]
        public void Fit_IsDeterministicAndIgnoresNaNScores()
        {
            var a = PlattCalibration.Fit(FoundationFixtures.D1Scores, FoundationFixtures.D1Labels);
            var b = PlattCalibration.Fit(FoundationFixtures.D1Scores, FoundationFixtures.D1Labels);

            var withNaN = PlattCalibration.Fit([.. FoundationFixtures.D1Scores, double.NaN],
                [.. FoundationFixtures.D1Labels, 1]);

            a.Should().Be(b);
            withNaN.Slope.Should().BeApproximately(a.Slope, 1e-12);
        }

        [Fact]
        public void Fit_ExtremeScoresStayFinite()
        {
            double[] scores = [-1e6, -5e5, 0, 1e6, 2e6, 3e6];
            int[] labels = [0, 0, 1, 0, 1, 1];
            var calibrator = PlattCalibration.Fit(scores, labels);
            calibrator.Apply(scores).Should().OnlyContain(p => p > 0 && p < 1 && !double.IsNaN(p));
            calibrator.Apply(1e12).Should().BeInRange(0, 1);
            calibrator.Apply(-1e12).Should().BeInRange(0, 1);
        }

        [Fact]
        public void Fit_SeparableDataStillConvergesBecauseOfSmoothedTargets()
        {
            double[] scores = [0.1, 0.2, 0.3, 0.7, 0.8, 0.9];
            int[] labels = [0, 0, 0, 1, 1, 1];
            var calibrator = PlattCalibration.Fit(scores, labels);
            calibrator.Slope.Should().BeGreaterThan(0);
            double.IsFinite(calibrator.Slope).Should().BeTrue();
            calibrator.Apply(0.9).Should().BeLessThan(1);
        }

        [Fact]
        public void Fit_SingleClass_Throws()
        {
            Action act = () => PlattCalibration.Fit([0.1, 0.5, 0.9], [1, 1, 1]);
            act.Should().Throw<InvalidOperationException>();
            act = () => PlattCalibration.Fit([0.1, 0.5, 0.9], [0, 0, 0]);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Fit_ConstantScores_Throws()
        {
            Action act = () => PlattCalibration.Fit([0.5, 0.5, 0.5, 0.5], [0, 1, 0, 1]);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Fit_InvalidArguments_ThrowStandardExceptions()
        {
            Action act = () => PlattCalibration.Fit(null!, [1]);
            act.Should().Throw<ArgumentNullException>();
            act = () => PlattCalibration.Fit([0.1, 0.2], [1]);
            act.Should().Throw<ArgumentException>();
            act = () => PlattCalibration.Fit([], []);
            act.Should().Throw<ArgumentException>();
            act = () => PlattCalibration.Fit([double.NaN, double.NaN], [0, 1]);
            act.Should().Throw<ArgumentException>();
            act = () => PlattCalibration.Fit([0.1, 0.2], [0, 2]);
            act.Should().Throw<ArgumentException>();
            act = () => PlattCalibration.Fit([0.1, 0.2], [0, 1], [1d]);
            act.Should().Throw<ArgumentException>();
            act = () => PlattCalibration.Fit([0.1, 0.2], [0, 1], [1d, -1d]);
            act.Should().Throw<ArgumentException>();
            act = () => PlattCalibration.Fit([0.1, 1.2], [0, 1], mode: CalibrationInput.LogitOfBoundedScore);
            act.Should().Throw<ArgumentException>();
            act = () => PlattCalibration.Fit([0.1, double.PositiveInfinity], [0, 1]);
            act.Should().Throw<ArgumentException>();
            act = () => PlattCalibration.Fit([0.1, 0.2], [0, 1], epsilon: 0.7);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Fit_HonoursCancellation()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            var token = source.Token;

            Action act = () => PlattCalibration.Fit(FoundationFixtures.D1Scores, FoundationFixtures.D1Labels,
                cancellationToken: token);
            act.Should().Throw<OperationCanceledException>();
        }

        [Fact]
        public void Calibrator_SnapshotsToPrimitivesAndBack()
        {
            var calibrator = PlattCalibration.Fit(FoundationFixtures.D1Scores, FoundationFixtures.D1Labels,
                mode: CalibrationInput.LogitOfBoundedScore, epsilon: 1e-4);

            var restored = Calibrator.FromPrimitives(calibrator.ToPrimitives());
            restored.Should().Be(calibrator);
            restored.Apply(0.3).Should().Be(calibrator.Apply(0.3));
        }

        [Fact]
        public void Calibrator_FromPrimitives_RejectsBadSnapshots()
        {
            Action act = () => Calibrator.FromPrimitives([1, 2, 3]);
            act.Should().Throw<ArgumentException>();
            act = () => Calibrator.FromPrimitives([1, 2, 7, 1e-6]);
            act.Should().Throw<ArgumentException>();
            act = () => Calibrator.FromPrimitives([1, double.NaN, 0, 1e-6]);
            act.Should().Throw<ArgumentException>();
            act = () => Calibrator.FromPrimitives([1, 2, 0, 0.9]);
            act.Should().Throw<ArgumentException>();
            act = () => Calibrator.FromPrimitives(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Calibrator_NaNInGivesNaNOut_AndLogitModeClampsOutOfRange()
        {
            var calibrator = new Calibrator(1d, 0d, CalibrationInput.LogitOfBoundedScore, 1e-6);
            double.IsNaN(calibrator.Apply(double.NaN)).Should().BeTrue();
            calibrator.Apply(0.0).Should().BeApproximately(1e-6, 1e-12);
            calibrator.Apply(-3.0).Should().Be(calibrator.Apply(0.0));
            calibrator.Apply(0.5).Should().BeApproximately(0.5, 1e-12);
        }
    }
}