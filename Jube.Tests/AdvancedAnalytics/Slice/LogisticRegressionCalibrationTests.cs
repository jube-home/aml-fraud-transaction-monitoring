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
using Jube.Engine.AdvancedAnalytics.Regression;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class LogisticRegressionCalibrationTests
    {
        private static LogisticRegressionResult Train(SliceDataset data, LogisticRegressionOptions options)
        {
            return LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);
        }

        [Fact]
        [Trait("Covers", "TrainLogisticRegression")]
        public void CalibrationRepairsADeliberatelyDistortedScoreOnTheTestSplit()
        {
            var data = SliceData.Logistic(30000, 21, -1.0, [1.2, -0.9, 0.6]);

            var distorted = new LogisticRegressionOptions { RidgeLambda = 40000d, Calibrate = false, Seed = 5 };
            var uncalibrated = Train(data, distorted);
            var calibrated = Train(data, distorted with { Calibrate = true });

            calibrated.Calibration.Applied.Should().BeTrue();
            calibrated.Calibration.Slope.Should().BeGreaterThan(1.5);

            calibrated.TestRows.Should().Equal(uncalibrated.TestRows);
            calibrated.TestMetrics!.LogLoss.Should().BeLessThan(uncalibrated.TestMetrics!.LogLoss!.Value - 0.005);
            calibrated.TestMetrics.ExpectedCalibrationError.Should().BeLessThan(0.02);

            uncalibrated.TestMetrics.ExpectedCalibrationError.Should()
                .BeGreaterThan(calibrated.TestMetrics.ExpectedCalibrationError!.Value);

            foreach (var bin in calibrated.TestMetrics.ReliabilityBins.Where(b => b.Count >= 150))
            {
                Math.Abs(bin.ObservedRate!.Value - bin.MeanPredicted!.Value).Should().BeLessThan(0.05);
            }

            calibrated.TestMetrics.CalibrationSlope!.Value.Should().BeInRange(0.85, 1.15);
            uncalibrated.TestMetrics.CalibrationSlope!.Value.Should().BeGreaterThan(1.5);
            calibrated.TestMetrics.RocAuc.Should().BeApproximately(uncalibrated.TestMetrics.RocAuc!.Value, 1e-9);
        }

        [Fact]
        public void CalibrationIsFittedOnValidationRowsOnly()
        {
            var data = SliceData.Logistic(6000, 22, -1.0, [1.0, -0.7]);
            var result = Train(data, new LogisticRegressionOptions { Seed = 8 });

            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);
            var validationScores = result.ValidationRows.Select(r => recall.Predict(data.Inputs[r])[0]).ToArray();

            validationScores.Average().Should().BeApproximately(
                result.ValidationRows.Select(r => (double)data.Labels[r]).Average(), 0.02);

            result.Calibration.RowsUsed.Should().Be(result.SplitSizes.Validation);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PriorShiftBringsBalancedFitProbabilitiesBackToTheTwoPercentBaseRate(bool calibrate)
        {
            var data = SliceData.Logistic(60000, 31, -4.7, [1.0, 0.8]);
            data.Labels.Average().Should().BeInRange(0.012, 0.035);

            var result = Train(data, new LogisticRegressionOptions
            {
                ClassWeighting = LogisticClassWeighting.Balanced, Calibrate = calibrate, Seed = 9
            });

            var observed = result.TestPredictions.Labels.Average();
            var mean = result.TestPredictions.Probabilities.Average();

            Math.Abs(mean - observed).Should().BeLessThan(0.003);
            Math.Abs(mean - data.Labels.Average()).Should().BeLessThan(0.004);
            result.Priors.EffectivePrior.Should().Be(0.5);
            result.Priors.BaseRate.Should().BeApproximately(data.Labels.Average(), 0.003);
            result.TestMetrics!.MeanPredictedProbability.Should().BeApproximately(mean, 1e-12);
            result.TestMetrics.ExpectedCalibrationError.Should().BeLessThan(0.01);
        }

        [Fact]
        public void PriorShiftOffsetMatchesTheClosedFormAndHonoursAnOverride()
        {
            var data = SliceData.Logistic(20000, 32, -1.0, [1.0]);

            var natural = Train(data, new LogisticRegressionOptions { Seed = 4, Calibrate = false });

            var shifted = Train(data, new LogisticRegressionOptions
            {
                Seed = 4, Calibrate = false, BaseRateOverride = 0.05
            });

            shifted.Priors.BaseRateSource.Should().Be("Override");

            var prior = natural.Priors.EffectivePrior;

            var naturalOffset = Math.Log(natural.Priors.BaseRate / (1d - natural.Priors.BaseRate)) -
                                Math.Log(prior / (1d - prior));

            var offset = Math.Log(0.05 / 0.95) - Math.Log(prior / (1d - prior)) - naturalOffset;

            for (var i = 0; i < 50; i++)
            {
                var p0 = natural.TestPredictions.Probabilities[i];
                var expected = 1d / (1d + Math.Exp(-(Math.Log(p0 / (1d - p0)) + offset)));
                shifted.TestPredictions.Probabilities[i].Should().BeApproximately(expected, 1e-9);
            }
        }

        [Fact]
        public void HeldOutMetricsAreComputedOnTheTestPredictionsAndMatchTheSharedDefinitions()
        {
            var data = SliceData.Logistic(8000, 41, -0.8, [1.0, -0.8, 0.5]);
            var result = Train(data, new LogisticRegressionOptions { Seed = 6 });
            var test = result.TestPredictions;
            var metrics = result.TestMetrics!;

            metrics.Rows.Should().Be(test.Rows.Length);
            metrics.Positives.Should().Be(test.Labels.Sum());
            metrics.RocAuc.Should().BeApproximately(BinaryMetrics.RocAuc(test.Probabilities, test.Labels), 1e-9);

            metrics.RocAucStandardError.Should()
                .BeApproximately(BinaryMetrics.RocAucWithStandardError(test.Probabilities, test.Labels).StandardError,
                    1e-9);

            metrics.AveragePrecision.Should()
                .BeApproximately(BinaryMetrics.AveragePrecision(test.Probabilities, test.Labels), 1e-9);
            metrics.LogLoss.Should().BeApproximately(BinaryMetrics.LogLoss(test.Probabilities, test.Labels), 1e-12);
            metrics.BrierScore.Should()
                .BeApproximately(BinaryMetrics.BrierScore(test.Probabilities, test.Labels), 1e-12);

            metrics.ReliabilityBins.Should().HaveCount(10);
            metrics.ReliabilityBins.Sum(b => b.Count).Should().Be(test.Rows.Length);

            var confusion = BinaryMetrics.Confusion(test.Probabilities, test.Labels, 0.5);
            metrics.TruePositives.Should().Be(confusion.TruePositives);
            metrics.FalsePositives.Should().Be(confusion.FalsePositives);
            metrics.F1.Should().BeApproximately(confusion.F1, 1e-12);
            metrics.MatthewsCorrelation.Should().BeApproximately(confusion.Mcc, 1e-12);
            metrics.ThresholdSource.Should().Be("Default");

            metrics.RocAuc.Should().BeInRange(0.7, 0.9);
            result.TrainMetrics!.Rows.Should().Be(result.SplitSizes.Train);
            result.ValidationMetrics!.Rows.Should().Be(result.SplitSizes.Validation);
        }

        [Fact]
        public void TheBayesMinimumRiskThresholdIsTheCostMinimiserOnCalibratedProbabilities()
        {
            var data = SliceData.Logistic(40000, 51, -2.0, [1.1, -0.9]);
            var costs = new CostMatrix(10d, 1d);

            var result = Train(data, new LogisticRegressionOptions { Seed = 2, Costs = costs });
            var metrics = result.TestMetrics!;

            metrics.Threshold.Should().BeApproximately(1d / 11d, 1e-12);
            metrics.ThresholdSource.Should().Be("BayesMinimumRisk");
            metrics.ExpectedCostAtThreshold.Should().NotBeNull();

            var test = result.TestPredictions;

            double Cost(double threshold)
            {
                return BinaryMetrics.Confusion(test.Probabilities, test.Labels, threshold).ExpectedCost(costs);
            }

            var best = Enumerable.Range(1, 99).Select(k => Cost(k / 100d)).Min();

            metrics.ExpectedCostAtThreshold!.Value.Should().BeApproximately(Cost(1d / 11d), 1e-12);
            metrics.ExpectedCostAtThreshold.Value.Should().BeLessThan(best * 1.05);
            metrics.ExpectedCostAtThreshold.Value.Should().BeLessThan(Cost(0.5));
        }

        [Fact]
        public void AnExplicitThresholdWinsOverTheCostThreshold()
        {
            var data = SliceData.Logistic(4000, 52, -1.0, [1.0]);

            var result = Train(data, new LogisticRegressionOptions
            {
                Costs = new CostMatrix(5, 1), DecisionThreshold = 0.3
            });

            result.TestMetrics!.Threshold.Should().Be(0.3);
            result.TestMetrics.ThresholdSource.Should().Be("Explicit");
        }
    }
}