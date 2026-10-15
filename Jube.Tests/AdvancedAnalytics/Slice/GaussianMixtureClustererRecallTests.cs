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
    public class GaussianMixtureClustererRecallTests
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
        [Trait("Covers", "RecallGaussianMixtureClusterer")]
        public void HighestPosteriorIsGenuinelyTheMaximumAndIsMovedToTheFront()
        {
            var data = ThreeWellSeparatedBlobs(200, 11);
            var names = Names(2);

            var result = GaussianMixtureClustererTrainer.Train(data, names,
                new GaussianMixtureClustererOptions { Components = 3, Seed = 5 });

            var recall = GaussianMixtureClustererRecall.FromSnapshot(result.SnapshotJson);

            recall.ComponentCount.Should().Be(3);

            var probes = new[] { new[] { -10d, -10d }, new[] { 10d, -10d }, new[] { 0d, 10d } };

            foreach (var probe in probes)
            {
                var posteriors = recall.Predict(probe);

                posteriors.Should().HaveCount(3);
                posteriors[0].Should().Be(posteriors.Max());
                posteriors[0].Should().BeGreaterThan(0.9);
                posteriors.Sum().Should().BeApproximately(1d, 1e-6);
            }
        }

        [Fact]
        public void PredictReturnsOnePosteriorPerComponentInComponentIndexOrderExceptTheHighestIsFirst()
        {
            var data = ThreeWellSeparatedBlobs(150, 21);

            var result = GaussianMixtureClustererTrainer.Train(data, Names(2),
                new GaussianMixtureClustererOptions { Components = 3, Seed = 3, Standardise = false });

            var recall = GaussianMixtureClustererRecall.FromSnapshot(result.SnapshotJson);

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot>(result.SnapshotJson,
                    GaussianMixtureClustererRecall.ModelKind);

            foreach (var component in envelope.Payload.Components)
            {
                var probe = component.Mean;
                var posteriors = recall.Predict(probe);
                var highestIndex = Array.IndexOf(posteriors, posteriors.Max());

                posteriors[0].Should().Be(posteriors.Max());
                highestIndex.Should().Be(0);
            }
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var data = ThreeWellSeparatedBlobs(120, 42);
            var names = Names(2);
            var options = new GaussianMixtureClustererOptions { Components = 3, Seed = 9 };

            var first = GaussianMixtureClustererTrainer.Train(data, names, options);
            var second = GaussianMixtureClustererTrainer.Train(data, names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = GaussianMixtureClustererRecall.FromSnapshot(first.SnapshotJson);
            var recallB = GaussianMixtureClustererRecall.FromSnapshot(second.SnapshotJson);

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
        public void ADifferentSeedCanProduceADifferentSnapshot()
        {
            var data = ThreeWellSeparatedBlobs(120, 77);
            var names = Names(2);

            var a = GaussianMixtureClustererTrainer.Train(data, names,
                new GaussianMixtureClustererOptions { Components = 3, Seed = 1 });
            var b = GaussianMixtureClustererTrainer.Train(data, names,
                new GaussianMixtureClustererOptions { Components = 3, Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RequestingMoreComponentsThanDistinctRowsThrowsAClearException()
        {
            var names = Names(2);
            var data = Enumerable.Range(0, 50).Select(_ => new[] { 1d, 1d }).ToArray();

            Action act = () => GaussianMixtureClustererTrainer.Train(data, names,
                new GaussianMixtureClustererOptions { Components = 5, Seed = 1 });

            act.Should().Throw<ArgumentException>().WithMessage("*distinct row*");
        }

        [Fact]
        public void RequestingMoreComponentsThanRowsThrowsAClearException()
        {
            var names = Names(2);
            var data = new[] { new[] { 1d, 2d }, new[] { 3d, 4d } };

            Action act = () => GaussianMixtureClustererTrainer.Train(data, names,
                new GaussianMixtureClustererOptions { Components = 5, Seed = 1 });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = ThreeWellSeparatedBlobs(100, 5);

            var result = GaussianMixtureClustererTrainer.Train(data, Names(2),
                new GaussianMixtureClustererOptions { Components = 3, Seed = 1 });

            var recall = GaussianMixtureClustererRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = ThreeWellSeparatedBlobs(150, 6);

            var result = GaussianMixtureClustererTrainer.Train(data, Names(2),
                new GaussianMixtureClustererOptions { Components = 3, Seed = 1 });

            var recall = GaussianMixtureClustererRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, -10d };
            var posteriors = recall.Predict(withMissing);

            posteriors.Should().OnlyContain(v => double.IsFinite(v));
            posteriors.Sum().Should().BeApproximately(1d, 1e-6);
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = ThreeWellSeparatedBlobs(120, 13);

            var result = GaussianMixtureClustererTrainer.Train(data, Names(2),
                new GaussianMixtureClustererOptions { Components = 3, Seed = 4 });

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot>(result.SnapshotJson,
                    GaussianMixtureClustererRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("GaussianMixtureClustererPipeline");
            envelope.Payload.Components.Should().HaveCount(3);
            envelope.Payload.Components.Sum(c => c.MixtureWeight).Should().BeApproximately(1d, 1e-6);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var result = GaussianMixtureClustererTrainer.Train(ThreeWellSeparatedBlobs(100, 8), Names(2),
                new GaussianMixtureClustererOptions { Components = 3, Seed = 1 });

            Action wrongKind = () => GaussianMixtureClustererRecall.FromSnapshot(
                result.SnapshotJson.Replace("GaussianMixtureClustererPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["components"]!).RemoveAt(0);
            Action shortComponents = () => GaussianMixtureClustererRecall.FromSnapshot(tampered.ToString());
            shortComponents.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => GaussianMixtureClustererRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleClusterSummaries()
        {
            var data = ThreeWellSeparatedBlobs(200, 19);

            var result = GaussianMixtureClustererTrainer.Train(data, Names(2),
                new GaussianMixtureClustererOptions
                    { Components = 3, Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2 });

            result.Details.Components.Should().Be(3);
            result.Details.ClusterSummaries.Should().HaveCount(3);
            result.Details.ClusterSummaries.Sum(s => s.Size).Should().Be(result.SplitSizes.Train);
            result.Details.ClusterSummaries.Select(s => s.MixtureWeight).Sum().Should().BeApproximately(1d, 1e-6);
            result.Details.TrainMeanMaxPosterior.Should().BeGreaterThan(0.9);
            result.Warnings.Should().NotContain(w => w.Code == GaussianMixtureWarningCodes.EmptyComponent);
        }
    }
}