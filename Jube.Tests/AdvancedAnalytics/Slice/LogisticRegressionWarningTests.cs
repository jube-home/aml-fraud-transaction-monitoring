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
using Jube.Engine.AdvancedAnalytics.Regression;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class LogisticRegressionWarningTests
    {
        private static readonly LogisticRegressionOptions plain = new()
        {
            ValidationFraction = 0, TestFraction = 0, Calibrate = false
        };

        private static LogisticRegressionResult Train(SliceDataset data, LogisticRegressionOptions? options = null)
        {
            return LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);
        }

        private static string[] Codes(LogisticRegressionResult result)
        {
            return [.. result.Warnings.Select(w => w.Code)];
        }

        [Fact]
        [Trait("Covers", "TrainLogisticRegression")]
        public void AWellPosedProblemRaisesNoModelQualityWarnings()
        {
            var data = SliceData.Logistic(6000, 61, -0.5, [1.0, -0.8]);
            var codes = Codes(Train(data));

            codes.Should().NotContain([
                LogisticWarningCodes.NonConvergence, LogisticWarningCodes.PerfectSeparation,
                LogisticWarningCodes.HugeCoefficients, LogisticWarningCodes.LowEventsPerVariable,
                LogisticWarningCodes.ConstantColumn, LogisticWarningCodes.NearConstantColumn,
                LogisticWarningCodes.HighVariance, LogisticWarningCodes.TinyTestSet,
                LogisticWarningCodes.SevereImbalance, LogisticWarningCodes.CalibrationSlopeFar,
                LogisticWarningCodes.NoSignificantPredictors
            ]);
        }

        [Fact]
        public void PerfectSeparationIsDetected()
        {
            var random = new Random(3);
            var n = 400;
            var labels = Enumerable.Range(0, n).Select(i => i % 2).ToArray();

            var inputs = labels.Select(l => new[]
                    { (l == 1 ? 4d : -4d) + 0.2 * SliceData.Gaussian(random), SliceData.Gaussian(random) })
                .ToArray();

            var result = LogisticRegressionTrainer.Train(inputs, labels, ["signal", "noise"]);

            Codes(result).Should().Contain(LogisticWarningCodes.PerfectSeparation);

            result.Warnings.Single(w => w.Code == LogisticWarningCodes.PerfectSeparation).Message.Should()
                .Contain("separable");

            var ridge = LogisticRegressionTrainer.Train(inputs, labels, ["signal", "noise"],
                new LogisticRegressionOptions { RidgeLambda = 1d });

            Codes(ridge).Should().Contain(LogisticWarningCodes.PerfectSeparation);
            ridge.Coefficients.All(c => double.IsFinite(c.Estimate)).Should().BeTrue();
        }

        [Fact]
        public void QuasiSeparationFromAZeroCellIsFlaggedByHugeCoefficientsOrNonConvergence()
        {
            var n = 3000;
            var random = new Random(4);
            var labels = new int[n];
            var inputs = new double[n][];

            for (var i = 0; i < n; i++)
            {
                var x = SliceData.Gaussian(random);
                var flag = i % 10 == 0 ? 1d : 0d;
                var eta = -0.5 + 0.8 * x;
                labels[i] = flag == 1d ? 1 : random.NextDouble() < SliceData.Sigmoid(eta) ? 1 : 0;
                inputs[i] = [x, flag];
            }

            var result = LogisticRegressionTrainer.Train(inputs, labels, ["x", "alwaysPositiveFlag"], plain);

            Codes(result).Should().Contain([LogisticWarningCodes.HugeCoefficients]);
            result.Coefficients.Single(c => c.Name == "alwaysPositiveFlag").Estimate.Should().BeGreaterThan(25d);
            result.Coefficients.Single(c => c.Name == "x").Estimate.Should().BeInRange(0.4, 1.2);
        }

        [Fact]
        public void ConstantColumnsAreExcludedAndReported()
        {
            var data = SliceData.Logistic(2000, 62, -0.5, [1.0]);
            var inputs = data.Inputs.Select(r => new[] { r[0], 3d }).ToArray();

            var result = LogisticRegressionTrainer.Train(inputs, data.Labels, ["signal", "flat"]);

            Codes(result).Should().Contain(LogisticWarningCodes.ConstantColumn);
            var flat = result.Preprocessing.Single(p => p.Name == "flat");
            flat.Excluded.Should().BeTrue();
            flat.ExclusionReason.Should().Be("Constant");
            result.Coefficients.Single(c => c.Name == "flat").StandardError.Should().BeNull();
            result.Coefficients.Single(c => c.Name == "signal").PValue.Should().BeLessThan(0.001);

            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);
            recall.Predict([0.5, 3d])[0].Should().Be(recall.Predict([0.5, 99d])[0]);
        }

        [Fact]
        public void NearConstantColumnsAreReported()
        {
            var data = SliceData.Logistic(4000, 63, -0.5, [1.0]);
            var inputs = data.Inputs.Select((r, i) => new[] { r[0], i % 400 == 0 ? 1d : 0d }).ToArray();

            var result = LogisticRegressionTrainer.Train(inputs, data.Labels, ["signal", "rare"]);

            Codes(result).Should().Contain(LogisticWarningCodes.NearConstantColumn);
            Codes(result).Should().NotContain(LogisticWarningCodes.ConstantColumn);
        }

        [Fact]
        public void LowEventsPerVariableIsReportedAndAdequateEventsAreNot()
        {
            var thin = SliceData.Logistic(150, 64, -0.5, [1.0, -0.8], 10);
            var thinResult = Train(thin, plain);

            thinResult.Fit.EventsPerVariable.Should().BeLessThan(10);
            Codes(thinResult).Should().Contain(LogisticWarningCodes.LowEventsPerVariable);

            var rich = SliceData.Logistic(3000, 65, -0.5, [1.0, -0.8], 3);
            var richResult = Train(rich, plain);
            richResult.Fit.EventsPerVariable.Should().BeGreaterThan(50);
            Codes(richResult).Should().NotContain(LogisticWarningCodes.LowEventsPerVariable);
        }

        [Fact]
        public void HighVarianceInflationIsMeasuredAndReported()
        {
            var random = new Random(66);
            var data = SliceData.Logistic(3000, 66, -0.5, [1.0, 0.0]);
            var collinear = data.Inputs.Select(r => new[] { r[0], r[0] + 0.05 * SliceData.Gaussian(random) }).ToArray();

            var result = LogisticRegressionTrainer.Train(collinear, data.Labels, ["a", "b"], plain);

            result.Preprocessing.Select(p => p.VarianceInflationFactor!.Value).Should().OnlyContain(v => v > 100d);
            Codes(result).Should().Contain(LogisticWarningCodes.HighVariance);

            var independent = Train(data, plain);

            independent.Preprocessing.Select(p => p.VarianceInflationFactor!.Value).Should()
                .OnlyContain(v => v > 0.99 && v < 1.05);

            Codes(independent).Should().NotContain(LogisticWarningCodes.HighVariance);
        }

        [Fact]
        public void ExactlyDuplicatedPredictorsAreReportedAsInfiniteVariance()
        {
            var data = SliceData.Logistic(1000, 67, -0.5, [1.0]);
            var duplicated = data.Inputs.Select(r => new[] { r[0], r[0] }).ToArray();

            var result = LogisticRegressionTrainer.Train(duplicated, data.Labels, ["a", "b"], plain);

            result.Preprocessing.Should().OnlyContain(p => p.VarianceInflationFactor == null);
            Codes(result).Should().Contain(LogisticWarningCodes.HighVariance);
        }

        [Fact]
        public void TinyAndMissingTestSplitsAreFlagged()
        {
            var small = SliceData.Logistic(150, 68, -0.5, [1.0]);
            var smallResult = Train(small);
            smallResult.SplitSizes.Test.Should().BeLessThan(100);
            Codes(smallResult).Should().Contain(LogisticWarningCodes.TinyTestSet);

            var large = SliceData.Logistic(4000, 69, -0.5, [1.0]);
            Codes(Train(large)).Should().NotContain(LogisticWarningCodes.TinyTestSet);

            var none = Train(large, new LogisticRegressionOptions { TestFraction = 0 });
            none.TestMetrics.Should().BeNull();
            Codes(none).Should().Contain(LogisticWarningCodes.NoTestSet);
        }

        [Fact]
        public void ImbalanceBeyondOneToOneHundredWithoutWeightsIsFlaggedAndWeightsSilenceIt()
        {
            var data = SliceData.Logistic(40000, 70, -6.5, [1.0, 0.7]);
            (data.Labels.Average() < 0.01).Should().BeTrue();

            Codes(Train(data)).Should().Contain(LogisticWarningCodes.SevereImbalance);

            Codes(Train(data, new LogisticRegressionOptions { ClassWeighting = LogisticClassWeighting.Balanced }))
                .Should().NotContain(LogisticWarningCodes.SevereImbalance);
        }

        [Fact]
        public void ACalibrationSlopeFarFromOneIsFlagged()
        {
            var data = SliceData.Logistic(20000, 71, -0.5, [1.5, -1.0]);
            var result = Train(data, new LogisticRegressionOptions { RidgeLambda = 60000d });

            Codes(result).Should().Contain(LogisticWarningCodes.CalibrationSlopeFar);
            Codes(result).Should().Contain(LogisticWarningCodes.ApproximateStandardErrors);
        }

        [Fact]
        public void NonConvergenceIsReportedWhenTheIterationBudgetIsTooSmall()
        {
            var data = SliceData.Logistic(3000, 72, -0.5, [1.0, -0.8]);

            var starved = Train(data, plain with { MaximumIterations = 1 });
            starved.Fit.Converged.Should().BeFalse();
            Codes(starved).Should().Contain(LogisticWarningCodes.NonConvergence);

            var normal = Train(data, plain);
            normal.Fit.Converged.Should().BeTrue();
            normal.Fit.Iterations.Should().BeLessThan(20);

            var penalisedStarved = Train(data, plain with { MaximumIterations = 1, RidgeLambda = 1d });
            Codes(penalisedStarved).Should().Contain(LogisticWarningCodes.NonConvergence);
        }

        [Fact]
        public void TheNoSignificantPredictorsWarningAppearsExactlyWhenEveryPValueExceedsFivePercent()
        {
            var flagged = 0;

            for (var seed = 0; seed < 40; seed++)
            {
                var data = SliceData.Logistic(300, 7000 + seed, -0.5, [], 3);
                var result = Train(data, plain);
                var allAbove = result.Coefficients.Where(c => c.Kind == "Predictor").All(c => c.PValue > 0.05);

                Codes(result).Contains(LogisticWarningCodes.NoSignificantPredictors).Should().Be(allAbove);

                if (allAbove)
                {
                    flagged++;
                }
            }

            flagged.Should().BeInRange(20, 39);
        }

        [Fact]
        public void BalancedWeightsCarryTheApproximateStandardErrorWarning()
        {
            var data = SliceData.Logistic(4000, 73, -2.0, [1.0]);

            var result = Train(data,
                new LogisticRegressionOptions { ClassWeighting = LogisticClassWeighting.Balanced });

            Codes(result).Should().Contain(LogisticWarningCodes.ApproximateStandardErrors);
            result.Fit.StandardErrorBasis.Should().Contain("frequency weights");
        }
    }
}