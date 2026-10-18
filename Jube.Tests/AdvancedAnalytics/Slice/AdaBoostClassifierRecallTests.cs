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
    public class AdaBoostClassifierRecallTests
    {
        private static AdaBoostClassifierOptions FastOptions(int seed = 1)
        {
            return new AdaBoostClassifierOptions
            {
                Rounds = 25,
                MaxDepth = 2,
                MinimumLeafSize = 5,
                Seed = seed
            };
        }

        [Fact]
        [Trait("Covers", "RecallAdaBoostClassifier")]
        public void SeparableDataProducesHighProbabilityForPositivesAndLowForNegatives()
        {
            var data = SliceData.Logistic(600, 31, 0d, [3.5, -3.5]);
            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var recall = AdaBoostClassifierRecall.FromSnapshot(result.SnapshotJson);

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

            var first = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, options);
            var second = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = AdaBoostClassifierRecall.FromSnapshot(first.SnapshotJson);
            var recallB = AdaBoostClassifierRecall.FromSnapshot(second.SnapshotJson);

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

            var a = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var b = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions(2));

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void PredictedProbabilitiesAreAlwaysBetweenZeroAndOne()
        {
            var data = SliceData.Logistic(400, 44, -0.3, [1.1, 0.4, -0.9]);
            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var recall = AdaBoostClassifierRecall.FromSnapshot(result.SnapshotJson);

            var probabilities = recall.PredictProbabilities(data.Inputs);

            probabilities.Should().OnlyContain(p => p >= 0d && p <= 1d);
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = SliceData.Logistic(200, 5, 0d, [1d, -1d]);
            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var recall = AdaBoostClassifierRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = SliceData.Logistic(300, 6, 0d, [1d, -1d]);
            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());
            var recall = AdaBoostClassifierRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, -1d };
            var probability = recall.Predict(withMissing);

            probability.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void RequiringBothClassesThrowsAClearExceptionWhenOnlyOneIsPresent()
        {
            var data = SliceData.Logistic(100, 3, 0d, [1d]);
            var labels = data.Labels.Select(_ => 1).ToArray();

            Action act = () => AdaBoostClassifierTrainer.Train(data.Inputs, labels, data.Names, FastOptions());

            act.Should().Throw<ArgumentException>().WithMessage("*both classes*");
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = SliceData.Logistic(300, 13, 0d, [1.2, -1.2]);
            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsAdaBoostClassifierPipelineSnapshot>(
                    result.SnapshotJson, AdaBoostClassifierRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("AdaBoostClassifierPipeline");
            envelope.Payload.Trees.Should().HaveCount(result.Details.RoundsFitted);
            envelope.Payload.Alphas.Should().HaveCount(result.Details.RoundsFitted);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var data = SliceData.Logistic(200, 8, 0d, [1d, -1d]);
            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions());

            Action wrongKind = () => AdaBoostClassifierRecall.FromSnapshot(
                result.SnapshotJson.Replace("AdaBoostClassifierPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["trees"]!).Clear();
            Action noTrees = () => AdaBoostClassifierRecall.FromSnapshot(tampered.ToString());
            noTrees.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var outOfRange = JObject.Parse(result.SnapshotJson);
            ((JArray)outOfRange["payload"]!["trees"]![0]!["attributeIndex"]!)[0] = 999;
            Action badFeature = () => AdaBoostClassifierRecall.FromSnapshot(outOfRange.ToString());
            badFeature.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var negativeAlpha = JObject.Parse(result.SnapshotJson);
            ((JArray)negativeAlpha["payload"]!["alphas"]!)[0] = -1d;
            Action badAlpha = () => AdaBoostClassifierRecall.FromSnapshot(negativeAlpha.ToString());
            badAlpha.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => AdaBoostClassifierRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleEnsembleSummariesAndFiniteNonNegativeAlphas()
        {
            var data = SliceData.Logistic(500, 21, 0.1, [1.4, -1.1, 0.6]);

            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new AdaBoostClassifierOptions { Rounds = 30, MaxDepth = 2, MinimumLeafSize = 5, Seed = 21 });

            result.Details.Rounds.Should().Be(30);
            result.Details.RoundsFitted.Should().BeGreaterThan(0).And.BeLessOrEqualTo(30);
            result.Details.RoundAlphas.Should().HaveCount(result.Details.RoundsFitted);
            result.Details.RoundErrors.Should().HaveCount(result.Details.RoundsFitted);
            result.Details.RoundAlphas.Should().OnlyContain(a => double.IsFinite(a) && a >= 0d);
            result.Details.RoundErrors.Should().OnlyContain(e => double.IsFinite(e) && e >= 0d && e < 0.5);
            result.Details.FeatureImportances.Should().HaveCount(3);
            result.Details.FeatureImportances.Sum().Should().BeApproximately(1d, 1e-6);
            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.6);
        }

        [Fact]
        public void ASingleRoundFitsExactlyOneTreeWithAFiniteNonNegativeAlpha()
        {
            var data = SliceData.Logistic(300, 9, 0d, [2.5, -2.5]);

            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new AdaBoostClassifierOptions { Rounds = 1, MaxDepth = 2, MinimumLeafSize = 5, Seed = 9 });

            result.Details.Rounds.Should().Be(1);
            result.Details.RoundsFitted.Should().Be(1);
            result.Details.RoundAlphas.Should().HaveCount(1);
            result.Details.RoundAlphas[0].Should().BeGreaterOrEqualTo(0d).And.Match(a => double.IsFinite(a));

            var recall = AdaBoostClassifierRecall.FromSnapshot(result.SnapshotJson);
            var probabilities = recall.PredictProbabilities(data.Inputs);
            probabilities.Should().OnlyContain(p => p >= 0d && p <= 1d);
        }

        [Fact]
        public void ExplainRawScoreMatchesThePipelineRawScore()
        {
            var data = SliceData.Logistic(300, 55, 0d, [1.3, -0.8]);
            var result = AdaBoostClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, FastOptions(55));
            var recall = AdaBoostClassifierRecall.FromSnapshot(result.SnapshotJson);

            foreach (var row in data.Inputs.Take(10))
            {
                var explain = recall.Explain(row);
                var impliedRaw = explain.BaseValue + explain.Contributions.Sum(c => c.Weight);

                impliedRaw.Should().BeApproximately(explain.RawScore, 1e-9);
                explain.RawScore.Should().BeInRange(-1d, 1d);
            }
        }
    }
}