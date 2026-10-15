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
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Density;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class GaussianMixtureDensityRecallTests
    {
        private static readonly string[] names = ["x0", "x1"];

        private static double[][] TwoClusterRows(int n, int seed, double[] centreA, double[] centreB)
        {
            var random = new Random(seed);
            var rows = new double[n][];

            for (var i = 0; i < n; i++)
            {
                var centre = i % 2 == 0 ? centreA : centreB;
                rows[i] = [centre[0] + 0.4 * SliceData.Gaussian(random), centre[1] + 0.4 * SliceData.Gaussian(random)];
            }

            return rows;
        }

        [Fact]
        [Trait("Covers", "RecallGaussianMixtureDensity")]
        public void ARowNearAFittedComponentScoresAHigherLogDensityThanARowFarFromEveryComponent()
        {
            var rows = TwoClusterRows(2000, 11, [0d, 0d], [10d, 10d]);

            var result = GaussianMixtureDensityTrainer.Train(rows, names,
                new GaussianMixtureDensityOptions { Components = 2, Seed = 3 });

            var recall = GaussianMixtureDensityRecall.FromSnapshot(result.SnapshotJson);
            recall.Variables.Should().Equal(names);

            var nearComponentA = recall.Predict([0d, 0d])[0];
            var nearComponentB = recall.Predict([10d, 10d])[0];
            var farFromBoth = recall.Predict([500d, -500d])[0];

            double.IsFinite(nearComponentA).Should().BeTrue();
            double.IsFinite(nearComponentB).Should().BeTrue();
            double.IsFinite(farFromBoth).Should().BeTrue();
            nearComponentA.Should().BeGreaterThan(farFromBoth);
            nearComponentB.Should().BeGreaterThan(farFromBoth);
        }

        [Fact]
        public void ExplainDecomposesPerDimensionAndDoesNotSumExactlyToTheLogDensity()
        {
            var rows = TwoClusterRows(2000, 23, [0d, 0d], [10d, 10d]);

            var result = GaussianMixtureDensityTrainer.Train(rows, names,
                new GaussianMixtureDensityOptions { Components = 2, Seed = 5 });

            var recall = GaussianMixtureDensityRecall.FromSnapshot(result.SnapshotJson);

            var explain = recall.Explain([0.2d, -0.1d]);

            explain.Contributions.Should().HaveCount(2);
            var reconstructed = explain.BaseValue + explain.Contributions.Sum(c => c.Weight);
            Math.Abs(reconstructed - explain.LogDensity).Should().BeGreaterThan(1e-9);
        }

        [Fact]
        public void AConstantColumnIsExcludedFromTheMixtureAndCarriesZeroWeight()
        {
            var random = new Random(29);
            var rows = new double[500][];

            for (var i = 0; i < rows.Length; i++)
            {
                rows[i] = [SliceData.Gaussian(random), 7d];
            }

            var result = GaussianMixtureDensityTrainer.Train(rows, ["x0", "constant"],
                new GaussianMixtureDensityOptions { Components = 2, Seed = 2 });

            result.Preprocessing[1].Excluded.Should().BeTrue();
            result.Warnings.Should().Contain(w => w.Code == GaussianMixtureDensityWarningCodes.ConstantColumn);

            var recall = GaussianMixtureDensityRecall.FromSnapshot(result.SnapshotJson);
            var explain = recall.Explain([0.4d, 7d]);

            explain.Contributions[1].Weight.Should().Be(0d);
        }

        [Fact]
        public void EveryVariableConstantThrowsALoudException()
        {
            var rows = Enumerable.Repeat(new[] { 1d, 2d }, 50).ToArray();

            Action act = () => GaussianMixtureDensityTrainer.Train(rows, ["a", "b"],
                new GaussianMixtureDensityOptions { Components = 1 });

            act.Should().Throw<ArgumentException>().WithMessage("*constant*");
        }

        [Fact]
        public void TooFewRowsForTheRequestedComponentCountThrowsAClearException()
        {
            var random = new Random(37);

            var rows = SliceData.Repeat(20, _ => new[] { SliceData.Gaussian(random), SliceData.Gaussian(random) })
                .ToArray();

            var split = new DataSplitIndices([.. Enumerable.Range(0, 10)], [.. Enumerable.Range(10, 5)],
                [.. Enumerable.Range(15, 5)]);

            Action act = () => GaussianMixtureDensityTrainer.TrainWithSplit(rows, names,
                new GaussianMixtureDensityOptions { Components = 10 }, split);

            act.Should().Throw<ArgumentException>().WithMessage("*component*");
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var rows = TwoClusterRows(500, 61, [0d, 0d], [8d, 8d]);

            var result = GaussianMixtureDensityTrainer.Train(rows, names,
                new GaussianMixtureDensityOptions { Components = 2 });

            var recall = GaussianMixtureDensityRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);

            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var rows = TwoClusterRows(500, 71, [0d, 0d], [8d, 8d]);

            var result = GaussianMixtureDensityTrainer.Train(rows, names,
                new GaussianMixtureDensityOptions { Components = 2 });

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsGaussianMixtureDensityPipelineSnapshot>(
                    result.SnapshotJson, GaussianMixtureDensityRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("GaussianMixtureDensityPipeline");
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var rows = TwoClusterRows(500, 81, [0d, 0d], [8d, 8d]);

            var result = GaussianMixtureDensityTrainer.Train(rows, names,
                new GaussianMixtureDensityOptions { Components = 2 });

            Action wrongKind = () => GaussianMixtureDensityRecall.FromSnapshot(
                result.SnapshotJson.Replace("GaussianMixtureDensityPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["activeInputs"]!).RemoveAt(0);
            Action shortActive = () => GaussianMixtureDensityRecall.FromSnapshot(tampered.ToString());
            shortActive.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => GaussianMixtureDensityRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void RecallIsThreadSafeAndDeterministicUnderContention()
        {
            var rows = TwoClusterRows(2000, 91, [0d, 0d], [8d, 8d]);

            var result = GaussianMixtureDensityTrainer.Train(rows, names,
                new GaussianMixtureDensityOptions { Components = 2 });

            var recall = GaussianMixtureDensityRecall.FromSnapshot(result.SnapshotJson);
            var probeRows = rows.Take(500).ToArray();
            var expected = probeRows.Select(r => SliceData.Bits(recall.Predict(r)[0])).ToArray();
            var mismatches = 0;

            Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
            {
                for (var repeat = 0; repeat < 6; repeat++)
                {
                    var outputs = recall.PredictLogDensities(probeRows);

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