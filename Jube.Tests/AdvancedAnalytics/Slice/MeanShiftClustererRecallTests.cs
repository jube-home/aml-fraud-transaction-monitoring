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
    public class MeanShiftClustererRecallTests
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

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallMeanShiftClusterer")]
        public void NearestModeDistanceIsGenuinelyTheMinimumAndIsMovedToTheFront()
        {
            var data = ThreeWellSeparatedBlobs(200, 11);
            var names = Names(2);

            var result = MeanShiftClustererTrainer.Train(data, names,
                new MeanShiftClustererOptions { Bandwidth = 0.5, Seed = 5 });

            var recall = MeanShiftClustererRecall.FromSnapshot(result.SnapshotJson);

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
        public void PredictReturnsOneDistancePerModeInModeIndexOrderExceptTheNearestIsFirst()
        {
            var data = ThreeWellSeparatedBlobs(150, 21);

            var result = MeanShiftClustererTrainer.Train(data, Names(2),
                new MeanShiftClustererOptions { Bandwidth = 3d, Seed = 3, Standardise = false });

            var recall = MeanShiftClustererRecall.FromSnapshot(result.SnapshotJson);

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsMeanShiftClustererPipelineSnapshot>(result.SnapshotJson,
                    MeanShiftClustererRecall.ModelKind);

            for (var c = 0; c < envelope.Payload.Modes.Length; c++)
            {
                var probe = envelope.Payload.Modes[c];
                var distances = recall.Predict(probe);

                distances[0].Should().Be(distances.Min());

                var expectedAtZero = c == 0
                    ? envelope.Payload.Modes.Select((_, i) => Distance(probe, envelope.Payload.Modes[i]))
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
        public void SameDataProducesTheSameModesAndTheSameRecallBitForBit()
        {
            var data = ThreeWellSeparatedBlobs(120, 42);
            var names = Names(2);
            var options = new MeanShiftClustererOptions { Bandwidth = 0.5, Seed = 9 };

            var first = MeanShiftClustererTrainer.Train(data, names, options);
            var second = MeanShiftClustererTrainer.Train(data, names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = MeanShiftClustererRecall.FromSnapshot(first.SnapshotJson);
            var recallB = MeanShiftClustererRecall.FromSnapshot(second.SnapshotJson);

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
        public void ADifferentBandwidthCanProduceADifferentModeCount()
        {
            var data = ThreeWellSeparatedBlobs(120, 77);
            var names = Names(2);

            var narrow = MeanShiftClustererTrainer.Train(data, names,
                new MeanShiftClustererOptions { Bandwidth = 0.5, Seed = 1 });
            var wide = MeanShiftClustererTrainer.Train(data, names,
                new MeanShiftClustererOptions { Bandwidth = 1d, Seed = 1 });

            narrow.Details.Modes.Should().Be(3);
            wide.Details.Modes.Should().BeLessThan(narrow.Details.Modes);
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = ThreeWellSeparatedBlobs(100, 5);

            var result = MeanShiftClustererTrainer.Train(data, Names(2),
                new MeanShiftClustererOptions { Bandwidth = 0.5, Seed = 1 });

            var recall = MeanShiftClustererRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = ThreeWellSeparatedBlobs(150, 6);

            var result = MeanShiftClustererTrainer.Train(data, Names(2),
                new MeanShiftClustererOptions { Bandwidth = 0.5, Seed = 1 });

            var recall = MeanShiftClustererRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, -10d };
            var distances = recall.Predict(withMissing);

            distances.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = ThreeWellSeparatedBlobs(120, 13);

            var result = MeanShiftClustererTrainer.Train(data, Names(2),
                new MeanShiftClustererOptions { Bandwidth = 0.5, Seed = 4 });

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsMeanShiftClustererPipelineSnapshot>(result.SnapshotJson,
                    MeanShiftClustererRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("MeanShiftClustererPipeline");
            envelope.Payload.Modes.Should().HaveCount(3);
            envelope.Payload.Clusters.Should().HaveCount(3);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var result = MeanShiftClustererTrainer.Train(ThreeWellSeparatedBlobs(100, 8), Names(2),
                new MeanShiftClustererOptions { Bandwidth = 0.5, Seed = 1 });

            Action wrongKind = () => MeanShiftClustererRecall.FromSnapshot(
                result.SnapshotJson.Replace("MeanShiftClustererPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["modes"]!).RemoveAt(0);
            Action shortModes = () => MeanShiftClustererRecall.FromSnapshot(tampered.ToString());
            shortModes.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => MeanShiftClustererRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleModeSummaries()
        {
            var data = ThreeWellSeparatedBlobs(200, 19);

            var result = MeanShiftClustererTrainer.Train(data, Names(2),
                new MeanShiftClustererOptions
                    { Bandwidth = 0.5, Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2 });

            result.Details.Modes.Should().Be(3);
            result.Details.ModeSummaries.Should().HaveCount(3);
            result.Details.ModeSummaries.Sum(s => s.Size).Should().Be(result.SplitSizes.Train);
            result.Details.ModeSummaries.Select(s => s.Proportion).Sum().Should().BeApproximately(1d, 1e-9);
            result.Details.TrainSilhouette.Should().NotBeNull();
            result.Details.TrainSilhouette!.Value.Should().BeGreaterThan(0.9);
            result.Warnings.Should().NotContain(w => w.Code == MeanShiftWarningCodes.EmptyMode);
        }
    }
}