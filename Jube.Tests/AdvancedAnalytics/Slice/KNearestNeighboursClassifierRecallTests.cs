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
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class KNearestNeighboursClassifierRecallTests
    {
        private static (double[][] Inputs, int[] Labels) TwoWellSeparatedBlobs(int perBlob, int seed)
        {
            var random = new Random(seed);
            var negativeCentre = new[] { -10d, -10d };
            var positiveCentre = new[] { 10d, 10d };
            var inputs = new double[perBlob * 2][];
            var labels = new int[perBlob * 2];
            var index = 0;

            foreach (var (centre, label) in new[] { (negativeCentre, 0), (positiveCentre, 1) })
            {
                for (var i = 0; i < perBlob; i++)
                {
                    inputs[index] =
                    [
                        centre[0] + 0.2 * SliceData.Gaussian(random),
                        centre[1] + 0.2 * SliceData.Gaussian(random)
                    ];

                    labels[index] = label;
                    index++;
                }
            }

            return (inputs, labels);
        }

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallKNearestNeighboursClassifier")]
        public void APointNearAKnownClassClusterPredictsThatClassWithHighProbability()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(200, 11);

            var result = KNearestNeighboursClassifierTrainer.Train(inputs, labels, Names(2),
                new KNearestNeighboursClassifierOptions { NeighbourCount = 9, Seed = 5 });

            var recall = KNearestNeighboursClassifierRecall.FromSnapshot(result.SnapshotJson);

            var nearNegative = recall.Predict([-10d, -10d])[0];
            var nearPositive = recall.Predict([10d, 10d])[0];

            nearNegative.Should().BeLessThan(0.2);
            nearPositive.Should().BeGreaterThan(0.8);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(150, 21);
            var names = Names(2);
            var options = new KNearestNeighboursClassifierOptions { NeighbourCount = 7, Seed = 9 };

            var first = KNearestNeighboursClassifierTrainer.Train(inputs, labels, names, options);
            var second = KNearestNeighboursClassifierTrainer.Train(inputs, labels, names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = KNearestNeighboursClassifierRecall.FromSnapshot(first.SnapshotJson);
            var recallB = KNearestNeighboursClassifierRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in inputs.Take(50))
            {
                var a = recallA.Predict(row);
                var b = recallB.Predict(row);

                for (var i = 0; i < a.Length; i++)
                {
                    SliceData.Bits(a[i]).Should().Be(SliceData.Bits(b[i]));
                }
            }
        }

        [Fact]
        public void ADifferentSeedCanProduceADifferentSnapshot()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(150, 33);
            var names = Names(2);

            var a = KNearestNeighboursClassifierTrainer.Train(inputs, labels, names,
                new KNearestNeighboursClassifierOptions { NeighbourCount = 7, Seed = 1 });
            var b = KNearestNeighboursClassifierTrainer.Train(inputs, labels, names,
                new KNearestNeighboursClassifierOptions { NeighbourCount = 7, Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RequestingMoreNeighboursThanAvailableTrainingRowsThrowsAClearException()
        {
            var names = Names(2);
            var inputs = Enumerable.Range(0, 10).Select(i => new[] { (double)i, i }).ToArray();
            var labels = Enumerable.Range(0, 10).Select(i => i % 2).ToArray();

            Action act = () => KNearestNeighboursClassifierTrainer.Train(inputs, labels, names,
                new KNearestNeighboursClassifierOptions
                {
                    NeighbourCount = 1000, Seed = 1, ValidationFraction = 0,
                    TestFraction = 0
                });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RequestingMoreNeighboursThanStoredRowsAtRecallTimeThrowsAClearException()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(20, 41);

            Action act = () => new KNearestNeighboursClassifierPipeline(true, [0d, 0d], [0d, 0d], [1d, 1d],
                [.. inputs.Take(5)], [.. labels.Take(5)], 6, null);

            act.Should().Throw<ArgumentException>().WithMessage("*neighbour count*");
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(100, 5);

            var result = KNearestNeighboursClassifierTrainer.Train(inputs, labels, Names(2),
                new KNearestNeighboursClassifierOptions { NeighbourCount = 5, Seed = 1 });

            var recall = KNearestNeighboursClassifierRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(150, 6);

            var result = KNearestNeighboursClassifierTrainer.Train(inputs, labels, Names(2),
                new KNearestNeighboursClassifierOptions { NeighbourCount = 5, Seed = 1 });

            var recall = KNearestNeighboursClassifierRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 10d };
            var prediction = recall.Predict(withMissing);

            prediction.Should().OnlyContain(v => double.IsFinite(v) && v >= 0d && v <= 1d);
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(120, 13);

            var result = KNearestNeighboursClassifierTrainer.Train(inputs, labels, Names(2),
                new KNearestNeighboursClassifierOptions { NeighbourCount = 5, Seed = 4 });

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsKNearestNeighboursClassifierPipelineSnapshot>(
                    result.SnapshotJson, KNearestNeighboursClassifierRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);

            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should()
                .Be("KNearestNeighboursClassifierPipeline");

            envelope.Payload.NeighbourCount.Should().Be(5);
            envelope.Payload.Rows.Should().HaveCount(result.Details.StoredTrainingRows);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(100, 8);

            var result = KNearestNeighboursClassifierTrainer.Train(inputs, labels, Names(2),
                new KNearestNeighboursClassifierOptions { NeighbourCount = 5, Seed = 1 });

            Action wrongKind = () => KNearestNeighboursClassifierRecall.FromSnapshot(
                result.SnapshotJson.Replace("KNearestNeighboursClassifierPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["rows"]!).RemoveAt(0);
            Action shortRows = () => KNearestNeighboursClassifierRecall.FromSnapshot(tampered.ToString());
            shortRows.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => KNearestNeighboursClassifierRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsCalibratedTestMetricsOnWellSeparatedClasses()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(300, 19);

            var result = KNearestNeighboursClassifierTrainer.Train(inputs, labels, Names(2),
                new KNearestNeighboursClassifierOptions
                {
                    NeighbourCount = 9, Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2
                });

            result.Details.NeighbourCount.Should().Be(9);
            result.Details.StoredTrainingRows.Should().Be(result.SplitSizes.Train);
            result.Details.Calibration.Applied.Should().BeTrue();
            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.95);
        }

        [Fact]
        public void TheExplainSharesAreNonNegativeAndSumToOne()
        {
            var (inputs, labels) = TwoWellSeparatedBlobs(150, 23);

            var result = KNearestNeighboursClassifierTrainer.Train(inputs, labels, Names(2),
                new KNearestNeighboursClassifierOptions { NeighbourCount = 5, Seed = 3 });

            var recall = KNearestNeighboursClassifierRecall.FromSnapshot(result.SnapshotJson);

            var explain = recall.Explain([9.5d, 9.5d]);

            explain.NeighbourLabels.Should().HaveCount(5);
            explain.Contributions.Should().HaveCount(2);
            explain.Contributions.Should().OnlyContain(c => c.Weight >= 0d);
            explain.Contributions.Sum(c => c.Weight).Should().BeApproximately(1d, 1e-9);
        }
    }
}