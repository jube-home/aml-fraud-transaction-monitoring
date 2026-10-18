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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class MahalanobisAnomalyRecallTests
    {
        private static readonly double[] mean = [10d, -5d];

        private static readonly double[,] sigmaOneHalfHalfTwoCholeskyFactor =
            { { 1d, 0d }, { 0.5d, 1.3228756555322954d } };

        private static readonly double[,] sigmaInverse =
        {
            { 2d / 1.75d, -0.5d / 1.75d },
            { -0.5d / 1.75d, 1d / 1.75d }
        };

        private static string[] Names => ["x0", "x1"];

        private static double[][] CorrelatedRows(int n, int seed, double[] center, double[,] cholesky)
        {
            var random = new Random(seed);
            var d = center.Length;
            var rows = new double[n][];

            for (var i = 0; i < n; i++)
            {
                var z = new double[d];

                for (var j = 0; j < d; j++)
                {
                    z[j] = SliceData.Gaussian(random);
                }

                var row = new double[d];

                for (var a = 0; a < d; a++)
                {
                    var value = center[a];

                    for (var b = 0; b <= a; b++)
                    {
                        value += cholesky[a, b] * z[b];
                    }

                    row[a] = value;
                }

                rows[i] = row;
            }

            return rows;
        }

        [Fact]
        [Trait("Covers", "RecallMahalanobisAnomaly")]
        public void RecallReproducesTrainingTimeDistancesBitForBit()
        {
            var rows = CorrelatedRows(3000, 11, mean, sigmaOneHalfHalfTwoCholeskyFactor);

            var result = MahalanobisAnomalyTrainer.Train(rows, null, Names,
                new MahalanobisAnomalyOptions { Standardise = false, Calibrate = false, Seed = 3 });

            var recall = MahalanobisAnomalyRecall.FromSnapshot(result.SnapshotJson);

            recall.Variables.Should().Equal(Names);

            foreach (var row in result.TestRows.Take(200))
            {
                var predicted = recall.Predict(rows[row])[0];
                var expected = result.TestDistances[Array.IndexOf(result.TestRows, row)];
                SliceData.Bits(predicted).Should().Be(SliceData.Bits(expected));
            }
        }

        [Fact]
        public void TheFittedDistanceMatchesTheTheoreticalMahalanobisDistanceOnAKnownTwoDimensionalGaussian()
        {
            var rows = CorrelatedRows(20000, 17, mean, sigmaOneHalfHalfTwoCholeskyFactor);

            var result = MahalanobisAnomalyTrainer.Train(rows, null, Names,
                new MahalanobisAnomalyOptions { Standardise = false, Calibrate = false, Seed = 1 });

            var recall = MahalanobisAnomalyRecall.FromSnapshot(result.SnapshotJson);

            var probe = new[] { 12d, -2d };
            var diff = new[] { probe[0] - mean[0], probe[1] - mean[1] };

            var theoreticalSquared = diff[0] * (sigmaInverse[0, 0] * diff[0] + sigmaInverse[0, 1] * diff[1])
                                     + diff[1] * (sigmaInverse[1, 0] * diff[0] + sigmaInverse[1, 1] * diff[1]);

            var theoretical = Math.Sqrt(theoreticalSquared);

            var predicted = recall.Predict(probe)[0];

            predicted.Should().BeApproximately(theoretical, 0.15);
        }

        [Fact]
        public void ExplainDecomposesPerDimensionAndDoesNotSumExactlyToTheDistance()
        {
            var rows = CorrelatedRows(4000, 23, mean, sigmaOneHalfHalfTwoCholeskyFactor);

            var result = MahalanobisAnomalyTrainer.Train(rows, null, Names,
                new MahalanobisAnomalyOptions { Standardise = false, Calibrate = false, Seed = 5 });

            var recall = MahalanobisAnomalyRecall.FromSnapshot(result.SnapshotJson);

            var probe = new[] { 12.5d, -6.5d };
            var explain = recall.Explain(probe);

            explain.Contributions.Should().HaveCount(2);
            explain.Contributions.Should().OnlyContain(c => c.Weight >= 0);
            var sumOfTerms = explain.Contributions.Sum(c => c.Weight);
            var squaredDistance = explain.Distance * explain.Distance;
            Math.Abs(sumOfTerms - squaredDistance).Should().BeGreaterThan(1e-9);
        }

        [Fact]
        public void AConstantColumnIsExcludedFromTheCovarianceAndCarriesZeroWeight()
        {
            var random = new Random(29);
            var rows = new double[500][];

            for (var i = 0; i < rows.Length; i++)
            {
                rows[i] = [SliceData.Gaussian(random), 7d];
            }

            var result = MahalanobisAnomalyTrainer.Train(rows, null, ["x0", "constant"],
                new MahalanobisAnomalyOptions { Calibrate = false, Seed = 2 });

            result.Preprocessing[1].Excluded.Should().BeTrue();
            result.Warnings.Should().Contain(w => w.Code == MahalanobisWarningCodes.ConstantColumn);

            var recall = MahalanobisAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var explain = recall.Explain([0.4d, 7d]);

            explain.Contributions[1].Weight.Should().Be(0d);
        }

        [Fact]
        public void ANearSingularCovarianceIsRegularisedRatherThanThrowing()
        {
            var random = new Random(31);
            var rows = new double[500][];

            for (var i = 0; i < rows.Length; i++)
            {
                var x0 = SliceData.Gaussian(random);
                rows[i] = [x0, x0 + 1e-9 * SliceData.Gaussian(random)];
            }

            var result = MahalanobisAnomalyTrainer.Train(rows, null, ["x0", "x1"],
                new MahalanobisAnomalyOptions { Calibrate = false, Seed = 4 });

            result.Details.CovarianceRegularised.Should().BeTrue();
            result.Warnings.Should().Contain(w => w.Code == MahalanobisWarningCodes.CovarianceRegularised);

            var recall = MahalanobisAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var distance = recall.Predict([0.1d, 0.1d])[0];
            double.IsFinite(distance).Should().BeTrue();
        }

        [Fact]
        public void EveryVariableConstantThrowsALoudException()
        {
            var rows = Enumerable.Repeat(new[] { 1d, 2d }, 50).ToArray();

            Action act = () => MahalanobisAnomalyTrainer.Train(rows, null, ["a", "b"]);

            act.Should().Throw<ArgumentException>().WithMessage("*constant*");
        }

        [Fact]
        public void LabelledTrainingCalibratesWhenBothClassesFallInTheValidationSplitAndUnlabelledDoesNot()
        {
            var random = new Random(41);
            var n = 4000;
            var rows = new double[n][];
            var labels = new int[n];

            for (var i = 0; i < n; i++)
            {
                var anomaly = i % 50 == 0;
                var scale = anomaly ? 6d : 1d;
                rows[i] = [SliceData.Gaussian(random) * scale, SliceData.Gaussian(random) * scale];
                labels[i] = anomaly ? 1 : 0;
            }

            var labelled = MahalanobisAnomalyTrainer.Train(rows, labels, ["x0", "x1"],
                new MahalanobisAnomalyOptions { Seed = 6 });

            labelled.Details.Calibration.Applied.Should().BeTrue();
            var labelledRecall = MahalanobisAnomalyRecall.FromSnapshot(labelled.SnapshotJson);
            var labelledProbability = labelledRecall.Predict([0.2d, 0.2d])[0];
            labelledProbability.Should().BeInRange(0d, 1d);

            var unlabelled = MahalanobisAnomalyTrainer.Train(rows, null, ["x0", "x1"],
                new MahalanobisAnomalyOptions { Seed = 6 });

            unlabelled.Details.Calibration.Applied.Should().BeFalse();
            unlabelled.Warnings.Should().Contain(w => w.Code == MahalanobisWarningCodes.CalibrationSkipped);
            var unlabelledRecall = MahalanobisAnomalyRecall.FromSnapshot(unlabelled.SnapshotJson);
            var rawDistance = unlabelledRecall.Predict([0.2d, 0.2d])[0];
            rawDistance.Should().BeGreaterThan(0d);
        }

        [Fact]
        public void ALargerDistanceCalibratesToAHigherOrEqualProbability()
        {
            var random = new Random(53);
            var n = 4000;
            var rows = new double[n][];
            var labels = new int[n];

            for (var i = 0; i < n; i++)
            {
                var anomaly = i % 40 == 0;
                var scale = anomaly ? 5d : 1d;
                rows[i] = [SliceData.Gaussian(random) * scale, SliceData.Gaussian(random) * scale];
                labels[i] = anomaly ? 1 : 0;
            }

            var result = MahalanobisAnomalyTrainer.Train(rows, labels, ["x0", "x1"],
                new MahalanobisAnomalyOptions { Seed = 9 });

            var recall = MahalanobisAnomalyRecall.FromSnapshot(result.SnapshotJson);

            var near = recall.Predict([0.1d, 0.1d])[0];
            var far = recall.Predict([9d, 9d])[0];

            far.Should().BeGreaterThan(near);
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var rows = CorrelatedRows(500, 61, mean, sigmaOneHalfHalfTwoCholeskyFactor);

            var result = MahalanobisAnomalyTrainer.Train(rows, null, Names,
                new MahalanobisAnomalyOptions { Calibrate = false });

            var recall = MahalanobisAnomalyRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);

            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var rows = CorrelatedRows(500, 71, mean, sigmaOneHalfHalfTwoCholeskyFactor);

            var result = MahalanobisAnomalyTrainer.Train(rows, null, Names,
                new MahalanobisAnomalyOptions { Calibrate = false });

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsMahalanobisAnomalyPipelineSnapshot>(
                    result.SnapshotJson, MahalanobisAnomalyRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("MahalanobisAnomalyPipeline");
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var rows = CorrelatedRows(500, 81, mean, sigmaOneHalfHalfTwoCholeskyFactor);

            var result = MahalanobisAnomalyTrainer.Train(rows, null, Names,
                new MahalanobisAnomalyOptions { Calibrate = false });

            Action wrongKind = () => MahalanobisAnomalyRecall.FromSnapshot(
                result.SnapshotJson.Replace("MahalanobisAnomalyPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["activeInputs"]!).RemoveAt(0);
            Action shortActive = () => MahalanobisAnomalyRecall.FromSnapshot(tampered.ToString());
            shortActive.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => MahalanobisAnomalyRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void RecallIsThreadSafeAndDeterministicUnderContention()
        {
            var rows = CorrelatedRows(2000, 91, mean, sigmaOneHalfHalfTwoCholeskyFactor);

            var result = MahalanobisAnomalyTrainer.Train(rows, null, Names,
                new MahalanobisAnomalyOptions { Calibrate = false });

            var recall = MahalanobisAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var probeRows = rows.Take(500).ToArray();
            var expected = probeRows.Select(r => SliceData.Bits(recall.Predict(r)[0])).ToArray();
            var mismatches = 0;

            Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
            {
                for (var repeat = 0; repeat < 6; repeat++)
                {
                    var outputs = recall.PredictDistances(probeRows);

                    for (var i = 0; i < probeRows.Length; i++)
                    {
                        if (SliceData.Bits(outputs[i]) != expected[i])
                        {
                            Interlocked.Increment(ref mismatches);
                        }
                    }
                }
            });

            mismatches.Should().Be(0);
        }
    }
}