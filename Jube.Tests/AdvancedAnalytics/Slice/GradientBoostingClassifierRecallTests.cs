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
    public class GradientBoostingClassifierRecallTests
    {
        private static GradientBoostingClassifierOptions FastOptions(int seed = 1)
        {
            return new GradientBoostingClassifierOptions
            {
                Rounds = 40,
                MaxDepth = 3,
                LearningRate = 0.2,
                MinimumLeafSize = 5,
                Seed = seed
            };
        }

        [Fact]
        [Trait("Covers", "RecallGradientBoostingClassifier")]
        public void SeparableDataProducesHighProbabilityForPositivesAndLowForNegatives()
        {
            var data = SliceData.Logistic(600, 31, 0d, [3.5, -3.5]);
            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var recall = GradientBoostingClassifierRecall.FromSnapshot(result.SnapshotJson);

            var positiveProbe = recall.Predict([5d, -5d]);
            var negativeProbe = recall.Predict([-5d, 5d]);

            positiveProbe.Should().HaveCount(1);
            positiveProbe[0].Should().BeGreaterThan(0.7);
            negativeProbe[0].Should().BeLessThan(0.3);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var data = SliceData.Logistic(300, 12, 0.2, [1.5, -1.2]);
            var options = FastOptions(7);

            var first = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, options);
            var second = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = GradientBoostingClassifierRecall.FromSnapshot(first.SnapshotJson);
            var recallB = GradientBoostingClassifierRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in data.Inputs.Take(40))
            {
                var a = recallA.Predict(row);
                var b = recallB.Predict(row);
                SliceData.Bits(a[0]).Should().Be(SliceData.Bits(b[0]));
            }
        }

        [Fact]
        public void ADifferentSeedCanProduceADifferentSnapshot()
        {
            var data = SliceData.Logistic(300, 19, 0d, [2d, -2d]);

            var a = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var b = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions(2));

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void PredictedProbabilitiesAreAlwaysBetweenZeroAndOne()
        {
            var data = SliceData.Logistic(400, 44, -0.3, [1.1, 0.4, -0.9]);
            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var recall = GradientBoostingClassifierRecall.FromSnapshot(result.SnapshotJson);

            var probabilities = recall.PredictProbabilities(data.Inputs);

            probabilities.Should().OnlyContain(p => p >= 0d && p <= 1d);
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = SliceData.Logistic(200, 5, 0d, [1d, -1d]);
            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var recall = GradientBoostingClassifierRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = SliceData.Logistic(300, 6, 0d, [1d, -1d]);
            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var recall = GradientBoostingClassifierRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, -1d };
            var probability = recall.Predict(withMissing);

            probability.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void RequiringBothClassesThrowsAClearExceptionWhenOnlyOneIsPresent()
        {
            var data = SliceData.Logistic(100, 3, 0d, [1d]);
            var labels = data.Labels.Select(_ => 1).ToArray();

            Action act = () =>
                GradientBoostingClassifierTrainer.Train(data.Inputs, labels, data.Names, FastOptions());

            act.Should().Throw<ArgumentException>().WithMessage("*both classes*");
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = SliceData.Logistic(300, 13, 0d, [1.2, -1.2]);
            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());

            var envelope =
                AdvancedAnalyticsModelJson
                    .Deserialise<AdvancedAnalyticsGradientBoostingClassifierPipelineSnapshot>(
                        result.SnapshotJson, GradientBoostingClassifierRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);

            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should()
                .Be("GradientBoostingClassifierPipeline");

            envelope.Payload.Trees.Should().HaveCount(40);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var data = SliceData.Logistic(200, 8, 0d, [1d, -1d]);
            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());

            Action wrongKind = () => GradientBoostingClassifierRecall.FromSnapshot(
                result.SnapshotJson.Replace("GradientBoostingClassifierPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["trees"]!).Clear();
            Action noTrees = () => GradientBoostingClassifierRecall.FromSnapshot(tampered.ToString());
            noTrees.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var outOfRange = JObject.Parse(result.SnapshotJson);
            ((JArray)outOfRange["payload"]!["trees"]![0]!["featureIndex"]!)[0] = 999;
            Action badFeature = () => GradientBoostingClassifierRecall.FromSnapshot(outOfRange.ToString());
            badFeature.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => GradientBoostingClassifierRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleEnsembleSummaries()
        {
            var data = SliceData.Logistic(500, 21, 0.1, [1.4, -1.1, 0.6]);

            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new GradientBoostingClassifierOptions
                    { Rounds = 60, MaxDepth = 3, LearningRate = 0.15, MinimumLeafSize = 5, Seed = 21 });

            result.Details.Rounds.Should().Be(60);
            result.Details.TreesFitted.Should().Be(60);
            result.Details.TrainingLossByRound.Should().HaveCount(60);
            result.Details.FeatureImportances.Should().HaveCount(3);
            result.Details.FeatureImportances.Sum().Should().BeApproximately(1d, 1e-6);
            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.6);

            var loss = result.Details.TrainingLossByRound;
            loss.Last().Should().BeLessThan(loss.First());
        }

        [Fact]
        public void ExplainRawScoreMatchesThePipelineRawScore()
        {
            var data = SliceData.Logistic(300, 55, 0d, [1.3, -0.8]);

            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                FastOptions(55));

            var recall = GradientBoostingClassifierRecall.FromSnapshot(result.SnapshotJson);

            foreach (var row in data.Inputs.Take(10))
            {
                var explain = recall.Explain(row);
                var impliedRaw = explain.BaseValue + explain.Contributions.Sum(c => c.Weight);

                impliedRaw.Should().BeApproximately(explain.RawScore, 1e-9);
            }
        }

        [Fact]
        [Trait("Covers", "GradientBoostingClassifierExactness")]
        public void ContributionsReconstructTheRawLogOddsMarginExactlyAcrossManyProbes()
        {
            var data = SliceData.Logistic(500, 61, -0.2, [1.6, -1.1, 0.5], 2);

            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new GradientBoostingClassifierOptions
                    { Rounds = 80, MaxDepth = 3, LearningRate = 0.1, MinimumLeafSize = 4, Seed = 61 });

            var recall = GradientBoostingClassifierRecall.FromSnapshot(result.SnapshotJson);

            var random = new Random(2026);

            for (var i = 0; i < 200; i++)
            {
                var row = new double[data.Names.Length];

                for (var j = 0; j < row.Length; j++)
                {
                    row[j] = (random.NextDouble() - 0.5) * 10d;
                }

                var explain = recall.Explain(row);
                var reconstructed = explain.BaseValue + explain.Contributions.Sum(c => c.Weight);

                reconstructed.Should().BeApproximately(explain.RawScore, 1e-9);
            }

            foreach (var row in data.Inputs)
            {
                var explain = recall.Explain(row);
                var reconstructed = explain.BaseValue + explain.Contributions.Sum(c => c.Weight);

                reconstructed.Should().BeApproximately(explain.RawScore, 1e-9);
            }
        }

        [Fact]
        public void ALargerLearningRateReachesAGivenTrainingLossInFewerRounds()
        {
            var data = SliceData.Logistic(500, 71, 0d, [2d, -2d]);

            var slow = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new GradientBoostingClassifierOptions
                    { Rounds = 20, MaxDepth = 2, LearningRate = 0.02, MinimumLeafSize = 5, Seed = 71 });
            var fast = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new GradientBoostingClassifierOptions
                    { Rounds = 20, MaxDepth = 2, LearningRate = 0.5, MinimumLeafSize = 5, Seed = 71 });

            fast.Details.TrainingLossByRound.Last().Should().BeLessThan(slow.Details.TrainingLossByRound.Last());
        }

        [Fact]
        public void ASingleRoundStillProducesAValidCalibratedModel()
        {
            var data = SliceData.Logistic(300, 9, 0d, [1.5, -1.5]);

            var result = GradientBoostingClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new GradientBoostingClassifierOptions
                    { Rounds = 1, MaxDepth = 2, LearningRate = 0.3, MinimumLeafSize = 5, Seed = 9 });

            var recall = GradientBoostingClassifierRecall.FromSnapshot(result.SnapshotJson);

            var probabilities = recall.PredictProbabilities(data.Inputs);

            probabilities.Should().OnlyContain(p => p >= 0d && p <= 1d);
            result.Details.TreesFitted.Should().Be(1);
        }
    }
}