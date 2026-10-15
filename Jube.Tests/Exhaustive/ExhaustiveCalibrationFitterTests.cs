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
using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using FluentAssertions;
using Jube.Engine.Exhaustive.Calibration;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class ExhaustiveCalibrationFitterTests
    {
        internal static (double[] Scores, double[] Labels) Generate(uint seed, int n)
        {
            var state = seed;

            double Next()
            {
                state = unchecked(1664525u * state + 1013904223u);

                return state / 4294967296.0;
            }

            var scores = new double[n];
            var labels = new double[n];

            for (var i = 0; i < n; i++)
            {
                var y = Next() < 0.5 ? 1 : 0;
                var a = Next();
                var b = Next();
                var z = (y == 1 ? 0.9 : -0.9) + 3.0 * (a + b - 1.0);
                scores[i] = 1.0 / (1.0 + Math.Exp(-0.6 * z));
                labels[i] = y;
            }

            return (scores, labels);
        }

        private static Outcome FitReference(IActivationFunction function,
            BaseRateResolution? baseRate = null, string requestedMethod = ExhaustiveCalibrationRequest.Platt)
        {
            var (vs, vy) = Generate(4242, 400);
            var (ts, ty) = Generate(4243, 400);

            return ExhaustiveCalibrationFitter.Fit(function, vs, vy, ts, ty, 0.5,
                baseRate ?? new BaseRateResolution(0.02, BaseRateResolution.SourceOverride), requestedMethod);
        }

        [Fact]
        public void Fit_SigmoidOutput_UsesTheLogitInputAndMatchesTheIndependentReference()
        {
            var outcome = FitReference(new SigmoidFunction(1));

            outcome.Method.Should().Be("platt");
            outcome.Input.Should().Be("logit");
            outcome.Slope.Should().BeApproximately(1.9138919318703835, 1e-7);
            outcome.Intercept.Should().BeApproximately(0.03593553619719747, 1e-7);
            outcome.CalibrationPrior.Should().BeApproximately(0.535, 1e-12);
            outcome.TrainingPrior.Should().Be(0.5);
            outcome.BaseRate.Should().Be(0.02);
            outcome.BaseRateSource.Should().Be("Override");
            outcome.SampleSize.Should().Be(400);
            outcome.Status.Should().Be("Calibrated");
            outcome.Reason.Should().BeNull();
        }

        [Fact]
        public void Fit_LinearOutput_UsesTheRawInput()
        {
            var outcome = FitReference(new LinearFunction(1));

            outcome.Input.Should().Be("raw");
            outcome.Slope.Should().BeApproximately(8.273443406186516, 1e-6);
            outcome.Intercept.Should().BeApproximately(-4.101940284158173, 1e-6);
        }

        [Theory]
        [InlineData(typeof(BipolarSigmoidFunction))]
        [InlineData(typeof(GaussianFunction))]
        [InlineData(typeof(IdentityFunction))]
        [InlineData(typeof(RectifiedLinearFunction))]
        public void Fit_BoundedAndUnboundedNonSigmoidFamilies_UseTheRawInput(Type type)
        {
            var function = (IActivationFunction)Activator.CreateInstance(type)!;

            FitReference(function).Input.Should().Be("raw");
        }

        [Fact]
        public void ApplyingTheStoredColumns_ReproducesTheReferenceProbabilitiesAtTheBaseRate()
        {
            var outcome = FitReference(new SigmoidFunction(1));

            var calibration = ExhaustiveCalibration.FromColumns(outcome.Method, outcome.Input, outcome.Slope,
                outcome.Intercept, outcome.CalibratorEpsilon, outcome.CalibrationPrior, outcome.BaseRate,
                outcome.BaseRateSource);

            calibration.IsCalibrated.Should().BeTrue();
            calibration.Apply(0.1).Should().BeApproximately(0.00027420385393226674, 1e-9);
            calibration.Apply(0.5).Should().BeApproximately(0.018054974127847623, 1e-9);
            calibration.Apply(0.9).Should().BeApproximately(0.5520943351186317, 1e-9);
            calibration.ApplyAtSamplePrior(0.1).Should().BeApproximately(0.015227391903322045, 1e-9);
            calibration.ApplyAtSamplePrior(0.9).Should().BeApproximately(0.9858136335137764, 1e-9);
        }

        [Fact]
        public void Fit_BeforeAndAfterQuality_MatchesTheIndependentReference()
        {
            var quality = FitReference(new SigmoidFunction(1)).Quality;

            quality.Validation.Ece.Raw.Should().BeApproximately(0.08069566437457171, 1e-9);
            quality.Validation.Ece.Calibrated.Should().BeApproximately(0.04836815100116504, 1e-6);
            quality.Validation.Brier.Raw.Should().BeApproximately(0.1719056198996654, 1e-9);
            quality.Validation.Brier.Calibrated.Should().BeApproximately(0.16274534138603045, 1e-7);
            quality.Validation.LogLoss.Raw.Should().BeApproximately(0.5210336325724552, 1e-9);
            quality.Validation.LogLoss.Calibrated.Should().BeApproximately(0.4830561653154531, 1e-7);
            quality.Test.Ece.Raw.Should().BeApproximately(0.07362002704498415, 1e-9);
            quality.Test.Ece.Calibrated.Should().BeApproximately(0.059210165058739134, 1e-6);
            quality.Test.Brier.Calibrated.Should().BeApproximately(0.17059511580131645, 1e-7);
            quality.Test.LogLoss.Calibrated.Should().BeApproximately(0.5021326404929101, 1e-7);
            quality.Test.MeanRaw.Should().BeApproximately(0.5040132278181723, 1e-9);
            quality.TestAtBaseRate.BaseRate.Should().Be(0.02);
            quality.TestAtBaseRate.Brier.Raw.Should().BeApproximately(0.17189735498247244, 1e-9);
            quality.TestAtBaseRate.Brier.Calibrated.Should().BeApproximately(0.017168034232122743, 1e-7);
            quality.TestAtBaseRate.LogLoss.Raw.Should().BeApproximately(0.5213018578400175, 1e-9);
            quality.TestAtBaseRate.LogLoss.Calibrated.Should().BeApproximately(0.07758368918294993, 1e-7);
        }

        [Fact]
        public void Fit_QualityRoundTripsThroughItsJson()
        {
            var quality = FitReference(new SigmoidFunction(1)).Quality;

            var back = CalibrationQuality.FromJson(quality.ToJson());

            back.Test.Brier.Calibrated.Should().Be(quality.Test.Brier.Calibrated);
            back.TestAtBaseRate.LogLoss.Raw.Should().Be(quality.TestAtBaseRate.LogLoss.Raw);
            CalibrationQuality.FromJson("not json").Should().BeNull();
            CalibrationQuality.FromJson(null).Should().BeNull();
        }

        [Fact]
        public void Fit_AnUnusableBaseRate_LeavesTheProbabilityAtTheSamplePrior()
        {
            var outcome = FitReference(new SigmoidFunction(1), BaseRateResolution.Unavailable);

            outcome.Method.Should().Be("platt");
            outcome.BaseRate.Should().BeNull();
            outcome.BaseRateSource.Should().Be("SamplePrior");
            outcome.Quality.TestAtBaseRate.Should().BeNull();
        }

        [Theory]
        [InlineData(typeof(ThresholdFunction))]
        [InlineData(typeof(BernoulliFunction))]
        public void Fit_HardOutputFamilies_AreSkippedWithARecordedReason(Type type)
        {
            var function = (IActivationFunction)Activator.CreateInstance(type)!;

            var outcome = FitReference(function);

            outcome.Method.Should().Be("none");
            outcome.Status.Should().Be("Skipped");
            outcome.Reason.Should().Contain("hard");
            outcome.Slope.Should().BeNull();
            outcome.Quality.Should().BeNull();
        }

        [Fact]
        public void Fit_SingleClassValidation_IsSkipped()
        {
            var (vs, _) = Generate(4242, 100);
            var (ts, ty) = Generate(4243, 100);

            var outcome = ExhaustiveCalibrationFitter.Fit(new SigmoidFunction(1), vs,
                [.. Enumerable.Repeat(1d, 100)], ts, ty, 0.5, BaseRateResolution.Unavailable);

            outcome.Status.Should().Be("Skipped");
            outcome.Reason.Should().Contain("single class");
        }

        [Fact]
        public void Fit_ConstantScores_AreSkipped()
        {
            var (_, vy) = Generate(4242, 200);
            var (ts, ty) = Generate(4243, 200);

            var outcome = ExhaustiveCalibrationFitter.Fit(new SigmoidFunction(1),
                [.. Enumerable.Repeat(0.4, 200)], vy, ts, ty, 0.5, BaseRateResolution.Unavailable);

            outcome.Status.Should().Be("Skipped");
            outcome.Reason.Should().Contain("constant");
        }

        [Fact]
        public void Fit_ScoresWithTwoDistinctValues_AreSkippedAsAHardOutput()
        {
            var (vs, vy) = Generate(4242, 200);
            var (ts, ty) = Generate(4243, 200);

            var outcome = ExhaustiveCalibrationFitter.Fit(new LinearFunction(1),
                [.. vs.Select(s => s > 0.5 ? 1d : 0d)], vy, ts, ty, 0.5, BaseRateResolution.Unavailable);

            outcome.Status.Should().Be("Skipped");
            outcome.Reason.Should().Contain("two distinct");
        }

        [Fact]
        public void Fit_IsotonicRequested_FitsAMonotoneStepFunctionAndImprovesLogLoss()
        {
            var platt = FitReference(new SigmoidFunction(1), requestedMethod: ExhaustiveCalibrationRequest.Platt);
            var isotonic = FitReference(new SigmoidFunction(1),
                requestedMethod: ExhaustiveCalibrationRequest.Isotonic);

            isotonic.Method.Should().Be("isotonic");
            isotonic.ParametersJson.Should().NotBeNullOrWhiteSpace();
            isotonic.Slope.Should().BeNull();

            isotonic.Quality.Validation.LogLoss.Calibrated.Should()
                .BeLessThan(isotonic.Quality.Validation.LogLoss.Raw!.Value);

            platt.Method.Should().Be("platt");
        }

        [Fact]
        public void Fit_BetaRequested_OnASigmoidFamily_FitsAndImprovesLogLoss()
        {
            var outcome = FitReference(new SigmoidFunction(1), requestedMethod: ExhaustiveCalibrationRequest.Beta);

            outcome.Method.Should().Be("beta");
            outcome.ParametersJson.Should().NotBeNullOrWhiteSpace();

            outcome.Quality.Validation.LogLoss.Calibrated.Should()
                .BeLessThan(outcome.Quality.Validation.LogLoss.Raw!.Value);
        }

        [Fact]
        public void Fit_BetaRequested_OnANonSigmoidFamily_IsSkippedWithAReason()
        {
            var outcome = FitReference(new LinearFunction(1), requestedMethod: ExhaustiveCalibrationRequest.Beta);

            outcome.Method.Should().Be("none");
            outcome.Status.Should().Be("Skipped");
            outcome.Reason.Should().Contain("bounded");
        }

        [Fact]
        public void Fit_Automatic_PicksACandidateAtLeastAsGoodAsPlattOnValidationBrier()
        {
            var platt = FitReference(new SigmoidFunction(1), requestedMethod: ExhaustiveCalibrationRequest.Platt);
            var automatic = FitReference(new SigmoidFunction(1),
                requestedMethod: ExhaustiveCalibrationRequest.Automatic);

            automatic.Method.Should().BeOneOf("platt", "isotonic", "beta");

            automatic.Quality.Validation.Brier.Calibrated.Should()
                .BeLessThanOrEqualTo(platt.Quality.Validation.Brier.Calibrated!.Value + 1e-9);
        }

        [Fact]
        public void Fit_Automatic_OnANonSigmoidFamily_NeverPicksBeta()
        {
            var automatic = FitReference(new LinearFunction(1),
                requestedMethod: ExhaustiveCalibrationRequest.Automatic);

            automatic.Method.Should().BeOneOf("platt", "isotonic");
        }

        [Fact]
        public void Fit_AnUnknownRequestedMethod_FallsBackToAutomatic()
        {
            var outcome = FitReference(new SigmoidFunction(1), requestedMethod: "not-a-real-method");

            outcome.Method.Should().BeOneOf("platt", "isotonic", "beta");
        }

        [Fact]
        public void ApplyingStoredIsotonicColumns_RoundTripsThroughFromColumns()
        {
            var outcome = FitReference(new SigmoidFunction(1), requestedMethod: ExhaustiveCalibrationRequest.Isotonic);

            var calibration = ExhaustiveCalibration.FromColumns(outcome.Method, outcome.Input, outcome.Slope,
                outcome.Intercept, outcome.CalibratorEpsilon, outcome.CalibrationPrior, outcome.BaseRate,
                outcome.BaseRateSource, outcome.ParametersJson);

            calibration.IsCalibrated.Should().BeTrue();
            calibration.Method.Should().Be("isotonic");
            calibration.Apply(0.5).Should().BeInRange(0, 1);
        }

        [Fact]
        public void ApplyingStoredBetaColumns_RoundTripsThroughFromColumns()
        {
            var outcome = FitReference(new SigmoidFunction(1), requestedMethod: ExhaustiveCalibrationRequest.Beta);

            var calibration = ExhaustiveCalibration.FromColumns(outcome.Method, outcome.Input, outcome.Slope,
                outcome.Intercept, outcome.CalibratorEpsilon, outcome.CalibrationPrior, outcome.BaseRate,
                outcome.BaseRateSource, outcome.ParametersJson);

            calibration.IsCalibrated.Should().BeTrue();
            calibration.Method.Should().Be("beta");
            calibration.Apply(0.5).Should().BeInRange(0, 1);
        }

        [Fact]
        public void FromColumns_AnUnknownMethod_IsInvalidAndThrowsOnApply()
        {
            var calibration = ExhaustiveCalibration.FromColumns("not-a-real-method", "logit", null, null, null,
                null, null, null);

            calibration.IsInvalid.Should().BeTrue();
            var act = () => calibration.Apply(0.5);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Fit_NonFiniteScores_AreSkipped()
        {
            var (vs, vy) = Generate(4242, 200);
            var (ts, ty) = Generate(4243, 200);
            vs[3] = double.NaN;

            var outcome = ExhaustiveCalibrationFitter.Fit(new LinearFunction(1), vs, vy, ts, ty, 0.5,
                BaseRateResolution.Unavailable);

            outcome.Status.Should().Be("Skipped");
            outcome.Reason.Should().Contain("finite");
        }

        [Fact]
        public void Fit_TooFewPositives_IsSkippedAndTheMinimumIsTwenty()
        {
            var (vs, vy) = Generate(4242, 400);
            var (ts, ty) = Generate(4243, 400);
            var seen = 0;

            for (var i = 0; i < vy.Length; i++)
            {
                if (vy[i] == 1 && ++seen > 19)
                {
                    vy[i] = 0;
                }
            }

            var outcome = ExhaustiveCalibrationFitter.Fit(new SigmoidFunction(1), vs, vy, ts, ty, 0.5,
                BaseRateResolution.Unavailable);

            outcome.Status.Should().Be("Skipped");
            outcome.Reason.Should().Contain("19 positives");
            ExhaustiveCalibrationFitter.MinimumPerClass.Should().Be(20);
        }

        [Fact]
        public void Fit_BetweenTwentyAndFortyNinePositives_FitsWithALowSampleWarning()
        {
            var (vs, vy) = Generate(4242, 400);
            var (ts, ty) = Generate(4243, 400);
            var seen = 0;

            for (var i = 0; i < vy.Length; i++)
            {
                if (vy[i] == 1 && ++seen > 30)
                {
                    vy[i] = 0;
                }
            }

            var outcome = ExhaustiveCalibrationFitter.Fit(new SigmoidFunction(1), vs, vy, ts, ty, 0.5,
                BaseRateResolution.Unavailable);

            outcome.Method.Should().Be("platt");
            outcome.Status.Should().Be("CalibratedLowSample");
            outcome.Reason.Should().Contain("30 positives");
        }

        [Fact]
        public void Fit_ScoresThatRankBackwards_AreSkippedRatherThanInverted()
        {
            var (vs, vy) = Generate(4242, 400);
            var (ts, ty) = Generate(4243, 400);

            var outcome = ExhaustiveCalibrationFitter.Fit(new SigmoidFunction(1), [.. vs.Select(s => 1 - s)], vy,
                ts, ty, 0.5, BaseRateResolution.Unavailable);

            outcome.Status.Should().Be("Skipped");
            outcome.Reason.Should().Contain("not positive");
        }

        [Fact]
        public void Fit_FlippingTheTestLabels_DoesNotChangeTheCalibrator_ButChangingTheValidationLabelsDoes()
        {
            var (vs, vy) = Generate(4242, 400);
            var (ts, ty) = Generate(4243, 400);

            var baseline = ExhaustiveCalibrationFitter.Fit(new SigmoidFunction(1), vs, vy, ts, ty, 0.5,
                new BaseRateResolution(0.02, "Override"));

            var flipped = ExhaustiveCalibrationFitter.Fit(new SigmoidFunction(1), vs, vy, ts,
                [.. ty.Select(l => 1 - l)], 0.5, new BaseRateResolution(0.02, "Override"));

            flipped.Slope.Should().Be(baseline.Slope);
            flipped.Intercept.Should().Be(baseline.Intercept);
            flipped.CalibrationPrior.Should().Be(baseline.CalibrationPrior);
            flipped.SampleSize.Should().Be(baseline.SampleSize);
            flipped.Quality.Test.Brier.Calibrated.Should().NotBe(baseline.Quality.Test.Brier.Calibrated);

            var changed = vy.ToArray();

            for (var i = 0; i < 60; i++)
            {
                changed[i] = 1 - changed[i];
            }

            var moved = ExhaustiveCalibrationFitter.Fit(new SigmoidFunction(1), vs, changed, ts, ty, 0.5,
                new BaseRateResolution(0.02, "Override"));

            moved.Slope.Should().NotBe(baseline.Slope);
            moved.Intercept.Should().NotBe(baseline.Intercept);
        }

        [Fact]
        public void BaseRateResolution_FromCountsOverrideAndAnomalyLabels()
        {
            BaseRateResolution.FromCounts(20, 980).Rate.Should().Be(0.02);
            BaseRateResolution.FromCounts(20, 980).Source.Should().Be("PopulationCount");
            BaseRateResolution.FromCounts(0, 100).IsUsable.Should().BeFalse();
            BaseRateResolution.FromCounts(100, 0).IsUsable.Should().BeFalse();
            BaseRateResolution.FromAnomalyLabels([0, 0, 0, 1]).Rate.Should().Be(0.25);
            BaseRateResolution.FromAnomalyLabels([0, 0, 0, 1]).Source.Should().Be("AnomalyFlaggedFraction");
            BaseRateResolution.FromAnomalyLabels([0, 0]).IsUsable.Should().BeFalse();
            BaseRateResolution.FromAnomalyLabels([]).IsUsable.Should().BeFalse();
            BaseRateResolution.WithOverride(0.3, BaseRateResolution.FromCounts(20, 980)).Rate.Should().Be(0.3);
            BaseRateResolution.WithOverride(0.3, BaseRateResolution.FromCounts(20, 980)).Source.Should().Be("Override");
            BaseRateResolution.WithOverride(null, BaseRateResolution.FromCounts(20, 980)).Rate.Should().Be(0.02);
            BaseRateResolution.WithOverride(1.5, BaseRateResolution.FromCounts(20, 980)).Rate.Should().Be(0.02);
            BaseRateResolution.WithOverride(null, null).IsUsable.Should().BeFalse();
        }
    }
}