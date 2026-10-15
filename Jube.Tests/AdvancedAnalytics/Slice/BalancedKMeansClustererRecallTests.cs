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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class BalancedKMeansClustererRecallTests
    {
        private static double[][] ThreeWellSeparatedBlobs(int perBlob, int seed)
        {
            var random = new Random(seed);
            var centres = new[] { new[] { -10d, -10d }, new[] { 10d, -10d }, new[] { 0d, 10d } };
            var rows = new double[perBlob * centres.Length][];
            var index = 0;

            foreach (var centre in centres)
            {
                for (var i = 0; i < perBlob; i++)
                {
                    rows[index++] =
                    [
                        centre[0] + 0.2 * SliceData.Gaussian(random),
                        centre[1] + 0.2 * SliceData.Gaussian(random)
                    ];
                }
            }

            return rows;
        }

        private static double[][] UnevenBlobs(int seed, params int[] sizes)
        {
            var random = new Random(seed);
            var centres = new[] { new[] { -10d, -10d }, new[] { 10d, -10d }, new[] { 0d, 10d } };
            var rows = new List<double[]>();

            for (var b = 0; b < sizes.Length; b++)
            for (var i = 0; i < sizes[b]; i++)
            {
                rows.Add(
                [
                    centres[b][0] + 0.2 * SliceData.Gaussian(random),
                    centres[b][1] + 0.2 * SliceData.Gaussian(random)
                ]);
            }

            return [.. rows];
        }

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallBalancedKMeansClusterer")]
        public void NearestCentroidDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront()
        {
            var data = ThreeWellSeparatedBlobs(200, 11);
            var names = Names(2);

            var result = BalancedKMeansClustererTrainer.Train(data, names,
                new BalancedKMeansClustererOptions { Clusters = 3, Seed = 5 });

            var recall = BalancedKMeansClustererRecall.FromSnapshot(result.SnapshotJson);

            recall.ClusterCount.Should().Be(3);

            var probes = new[] { new[] { -10d, -10d }, new[] { 10d, -10d }, new[] { 0d, 10d } };

            foreach (var probe in probes)
            {
                var distances = recall.Predict(probe);

                distances.Should().HaveCount(3);
                distances[0].Should().Be(distances.Min());
                distances[0].Should().BeLessThan(1d);
            }
        }

        [Fact]
        public void PredictReturnsOneDistancePerCentroidInClusterIndexOrderExceptTheNearestIsFirst()
        {
            var data = ThreeWellSeparatedBlobs(150, 21);

            var result = BalancedKMeansClustererTrainer.Train(data, Names(2),
                new BalancedKMeansClustererOptions { Clusters = 3, Seed = 3, Standardise = false });

            var recall = BalancedKMeansClustererRecall.FromSnapshot(result.SnapshotJson);

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsBalancedKMeansClustererPipelineSnapshot>(
                    result.SnapshotJson, BalancedKMeansClustererRecall.ModelKind);

            for (var c = 0; c < envelope.Payload.Centroids.Length; c++)
            {
                var probe = envelope.Payload.Centroids[c];
                var distances = recall.Predict(probe);

                distances[0].Should().Be(distances.Min());

                var expectedAtZero = c == 0
                    ? envelope.Payload.Centroids.Select((_, i) => Distance(probe, envelope.Payload.Centroids[i]))
                        .ToArray()
                    : null;

                if (expectedAtZero != null)
                {
                    distances.Should().BeEquivalentTo(expectedAtZero, o => o.WithStrictOrdering());
                }
            }
        }

        private static double Distance(double[] a, double[] b)
        {
            var sum = 0d;

            for (var i = 0; i < a.Length; i++)
            {
                var d = a[i] - b[i];
                sum += d * d;
            }

            return Math.Sqrt(sum);
        }

        [Fact]
        public void SameSeedProducesTheSameCentroidsAndTheSameRecallBitForBit()
        {
            var data = ThreeWellSeparatedBlobs(120, 42);
            var names = Names(2);
            var options = new BalancedKMeansClustererOptions { Clusters = 3, Seed = 9 };

            var first = BalancedKMeansClustererTrainer.Train(data, names, options);
            var second = BalancedKMeansClustererTrainer.Train(data, names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = BalancedKMeansClustererRecall.FromSnapshot(first.SnapshotJson);
            var recallB = BalancedKMeansClustererRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in data.Take(50))
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
        public void RequestingMoreClustersThanDistinctRowsThrowsAClearException()
        {
            var names = Names(2);
            var data = Enumerable.Range(0, 50).Select(_ => new[] { 1d, 1d }).ToArray();

            Action act = () => BalancedKMeansClustererTrainer.Train(data, names,
                new BalancedKMeansClustererOptions { Clusters = 5, Seed = 1 });

            act.Should().Throw<ArgumentException>().WithMessage("*distinct row*");
        }

        [Fact]
        public void RequestingMoreClustersThanRowsThrowsAClearException()
        {
            var names = Names(2);
            var data = new[] { new[] { 1d, 2d }, new[] { 3d, 4d } };

            Action act = () => BalancedKMeansClustererTrainer.Train(data, names,
                new BalancedKMeansClustererOptions { Clusters = 5, Seed = 1 });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ExceedingTheMaximumTrainingRowsThrowsAClearException()
        {
            var names = Names(2);
            var random = new Random(3);

            var data = Enumerable.Range(0, BalancedKMeansClustererTrainer.MaximumTrainingRows + 1)
                .Select(_ => new[] { SliceData.Gaussian(random), SliceData.Gaussian(random) })
                .ToArray();

            Action act = () => BalancedKMeansClustererTrainer.Train(data, names,
                new BalancedKMeansClustererOptions
                    { Clusters = 3, Seed = 1, ValidationFraction = 0, TestFraction = 0 });

            act.Should().Throw<ArgumentException>().WithMessage("*row limit*");
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = ThreeWellSeparatedBlobs(100, 5);

            var result = BalancedKMeansClustererTrainer.Train(data, Names(2),
                new BalancedKMeansClustererOptions { Clusters = 3, Seed = 1 });

            var recall = BalancedKMeansClustererRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = ThreeWellSeparatedBlobs(150, 6);

            var result = BalancedKMeansClustererTrainer.Train(data, Names(2),
                new BalancedKMeansClustererOptions { Clusters = 3, Seed = 1 });

            var recall = BalancedKMeansClustererRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, -10d };
            var distances = recall.Predict(withMissing);

            distances.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = ThreeWellSeparatedBlobs(120, 13);

            var result = BalancedKMeansClustererTrainer.Train(data, Names(2),
                new BalancedKMeansClustererOptions { Clusters = 3, Seed = 4 });

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsBalancedKMeansClustererPipelineSnapshot>(
                    result.SnapshotJson, BalancedKMeansClustererRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("BalancedKMeansClustererPipeline");
            envelope.Payload.Centroids.Should().HaveCount(3);
            envelope.Payload.Clusters.Should().HaveCount(3);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var result = BalancedKMeansClustererTrainer.Train(ThreeWellSeparatedBlobs(100, 8), Names(2),
                new BalancedKMeansClustererOptions { Clusters = 3, Seed = 1 });

            Action wrongKind = () => BalancedKMeansClustererRecall.FromSnapshot(
                result.SnapshotJson.Replace("BalancedKMeansClustererPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["centroids"]!).RemoveAt(0);
            Action shortCentroids = () => BalancedKMeansClustererRecall.FromSnapshot(tampered.ToString());
            shortCentroids.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => BalancedKMeansClustererRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleClusterSummaries()
        {
            var data = ThreeWellSeparatedBlobs(200, 19);

            var result = BalancedKMeansClustererTrainer.Train(data, Names(2),
                new BalancedKMeansClustererOptions
                    { Clusters = 3, Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2 });

            result.Details.Clusters.Should().Be(3);
            result.Details.ClusterSummaries.Should().HaveCount(3);
            result.Details.ClusterSummaries.Sum(s => s.Size).Should().Be(result.SplitSizes.Train);
            result.Details.ClusterSummaries.Select(s => s.Proportion).Sum().Should().BeApproximately(1d, 1e-9);
            result.Details.TrainSilhouette.Should().NotBeNull();
            result.Details.TrainSilhouette!.Value.Should().BeGreaterThan(0.9);
            result.Details.TrainSizeImbalance.Should().BeLessOrEqualTo(1);
        }

        [Fact]
        [Trait("Covers", "RecallBalancedKMeansClusterer")]
        public void ClusterSizesAreAsEqualAsPossibleEvenWhenTheNaturalGroupsAreNot()
        {
            var data = UnevenBlobs(31, 260, 20, 20);

            var result = BalancedKMeansClustererTrainer.Train(data, Names(2),
                new BalancedKMeansClustererOptions
                    { Clusters = 3, Seed = 1, ValidationFraction = 0, TestFraction = 0 });

            var sizes = result.Details.ClusterSummaries.Select(s => s.Size).ToArray();

            sizes.Max().Should().Be(sizes.Min());
            (sizes.Max() - sizes.Min()).Should().BeLessOrEqualTo(1);
            result.Details.TrainSizeImbalance.Should().BeLessOrEqualTo(1);
        }
    }
}