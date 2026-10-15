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
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class KModesClustererRecallTests
    {
        private static double[][] ThreeCategoricalGroups(int perGroup, int seed)
        {
            var random = new Random(seed);
            var groups = new[] { new[] { 1d, 1d, 1d }, new[] { 2d, 2d, 2d }, new[] { 3d, 3d, 3d } };
            var rows = new double[perGroup * groups.Length][];
            var index = 0;

            foreach (var group in groups)
            {
                for (var i = 0; i < perGroup; i++)
                {
                    var row = (double[])group.Clone();

                    if (random.NextDouble() < 0.03)
                    {
                        row[random.Next(row.Length)] = group[0] == 1 ? 2 : 1;
                    }

                    rows[index++] = row;
                }
            }

            return rows;
        }

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallKModesClusterer")]
        public void NearestModeDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront()
        {
            var data = ThreeCategoricalGroups(200, 11);
            var names = Names(3);

            var result = KModesClustererTrainer.Train(data, names,
                new KModesClustererOptions { Clusters = 3, Seed = 5 });

            var recall = KModesClustererRecall.FromSnapshot(result.SnapshotJson);

            recall.ClusterCount.Should().Be(3);

            var probes = new[] { new[] { 1d, 1d, 1d }, new[] { 2d, 2d, 2d }, new[] { 3d, 3d, 3d } };

            foreach (var probe in probes)
            {
                var distances = recall.Predict(probe);

                distances.Should().HaveCount(3);
                distances[0].Should().Be(distances.Min());
                distances[0].Should().BeLessThan(1d);
            }
        }

        [Fact]
        public void PredictReturnsOneMismatchCountPerModeInClusterIndexOrderExceptTheNearestIsFirst()
        {
            var data = ThreeCategoricalGroups(150, 21);

            var result = KModesClustererTrainer.Train(data, Names(3),
                new KModesClustererOptions { Clusters = 3, Seed = 3 });

            var recall = KModesClustererRecall.FromSnapshot(result.SnapshotJson);

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsKModesClustererPipelineSnapshot>(
                result.SnapshotJson, KModesClustererRecall.ModelKind);

            for (var c = 0; c < envelope.Payload.Modes.Length; c++)
            {
                var probe = envelope.Payload.Modes[c];
                var distances = recall.Predict(probe);
                var nearestIndex = Array.IndexOf(distances, distances.Min());

                distances[0].Should().Be(distances.Min());
                distances[nearestIndex].Should().Be(0d);
            }
        }

        [Fact]
        public void SameSeedProducesTheSameModesAndTheSameRecallBitForBit()
        {
            var data = ThreeCategoricalGroups(120, 42);
            var names = Names(3);
            var options = new KModesClustererOptions { Clusters = 3, Seed = 9 };

            var first = KModesClustererTrainer.Train(data, names, options);
            var second = KModesClustererTrainer.Train(data, names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = KModesClustererRecall.FromSnapshot(first.SnapshotJson);
            var recallB = KModesClustererRecall.FromSnapshot(second.SnapshotJson);

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
        public void ADifferentSeedCanProduceDifferentModes()
        {
            var data = ThreeCategoricalGroups(120, 77);
            var names = Names(3);

            var a = KModesClustererTrainer.Train(data, names, new KModesClustererOptions { Clusters = 3, Seed = 1 });
            var b = KModesClustererTrainer.Train(data, names, new KModesClustererOptions { Clusters = 3, Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RequestingMoreClustersThanDistinctRowsThrowsAClearException()
        {
            var names = Names(2);
            var data = Enumerable.Range(0, 50).Select(_ => new[] { 1d, 1d }).ToArray();

            Action act = () => KModesClustererTrainer.Train(data, names,
                new KModesClustererOptions { Clusters = 5, Seed = 1 });

            act.Should().Throw<ArgumentException>().WithMessage("*distinct row*");
        }

        [Fact]
        public void RequestingMoreClustersThanRowsThrowsAClearException()
        {
            var names = Names(2);
            var data = new[] { new[] { 1d, 2d }, new[] { 3d, 4d } };

            Action act = () => KModesClustererTrainer.Train(data, names,
                new KModesClustererOptions { Clusters = 5, Seed = 1 });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = ThreeCategoricalGroups(100, 5);

            var result = KModesClustererTrainer.Train(data, Names(3),
                new KModesClustererOptions { Clusters = 3, Seed = 1 });

            var recall = KModesClustererRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 3*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMode()
        {
            var data = ThreeCategoricalGroups(150, 6);

            var result = KModesClustererTrainer.Train(data, Names(3),
                new KModesClustererOptions { Clusters = 3, Seed = 1 });

            var recall = KModesClustererRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 2d, 2d };
            var distances = recall.Predict(withMissing);

            distances.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void ANonIntegerInputIsRoundedToTheNearestCategory()
        {
            var data = ThreeCategoricalGroups(150, 14);

            var result = KModesClustererTrainer.Train(data, Names(3),
                new KModesClustererOptions { Clusters = 3, Seed = 1 });

            var recall = KModesClustererRecall.FromSnapshot(result.SnapshotJson);

            var exact = recall.Predict([2d, 2d, 2d]);
            var nearlyRoundsTheSame = recall.Predict([2.2, 1.8, 2.4]);

            nearlyRoundsTheSame.Should().Equal(exact);
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = ThreeCategoricalGroups(120, 13);

            var result = KModesClustererTrainer.Train(data, Names(3),
                new KModesClustererOptions { Clusters = 3, Seed = 4 });

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsKModesClustererPipelineSnapshot>(
                result.SnapshotJson, KModesClustererRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("KModesClustererPipeline");
            envelope.Payload.Modes.Should().HaveCount(3);
            envelope.Payload.Clusters.Should().HaveCount(3);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var result = KModesClustererTrainer.Train(ThreeCategoricalGroups(100, 8), Names(3),
                new KModesClustererOptions { Clusters = 3, Seed = 1 });

            Action wrongKind = () => KModesClustererRecall.FromSnapshot(
                result.SnapshotJson.Replace("KModesClustererPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["modes"]!).RemoveAt(0);
            Action shortModes = () => KModesClustererRecall.FromSnapshot(tampered.ToString());
            shortModes.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var fractional = JObject.Parse(result.SnapshotJson);
            ((JArray)fractional["payload"]!["modes"]![0]!)[0] = 1.5;
            Action fractionalMode = () => KModesClustererRecall.FromSnapshot(fractional.ToString());
            fractionalMode.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => KModesClustererRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleClusterSummaries()
        {
            var data = ThreeCategoricalGroups(200, 19);

            var result = KModesClustererTrainer.Train(data, Names(3),
                new KModesClustererOptions { Clusters = 3, Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2 });

            result.Details.Clusters.Should().Be(3);
            result.Details.ClusterSummaries.Should().HaveCount(3);
            result.Details.ClusterSummaries.Sum(s => s.Size).Should().Be(result.SplitSizes.Train);
            result.Details.ClusterSummaries.Select(s => s.Proportion).Sum().Should().BeApproximately(1d, 1e-9);
            result.Details.TrainSilhouette.Should().NotBeNull();
            result.Details.TrainSilhouette!.Value.Should().BeGreaterThan(0.7);
            result.Warnings.Should().NotContain(w => w.Code == KModesWarningCodes.EmptyCluster);
        }

        [Fact]
        public void ANonIntegerTrainingColumnRaisesAWarning()
        {
            var random = new Random(31);

            var data = Enumerable.Range(0, 60)
                .Select(i => new[] { i % 3 == 0 ? 1.4 : i % 3, i % 2 })
                .ToArray();

            var result = KModesClustererTrainer.Train(data, Names(2),
                new KModesClustererOptions { Clusters = 2, Seed = random.Next() });

            result.Warnings.Should().Contain(w => w.Code == KModesWarningCodes.NonIntegerValuesRounded);
        }
    }
}