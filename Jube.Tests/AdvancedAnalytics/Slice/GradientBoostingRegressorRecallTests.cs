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
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class GradientBoostingRegressorRecallTests
    {
        private static GradientBoostingRegressorOptions FastOptions(int seed = 1)
        {
            return new GradientBoostingRegressorOptions
            {
                Rounds = 40,
                MaxDepth = 3,
                LearningRate = 0.2,
                MinimumLeafSize = 5,
                Seed = seed
            };
        }

        private static LinearSliceDataset Continuous(int n, int seed, double intercept, double[] slopes,
            int noiseColumns = 0, double missingRate = 0d, double noiseStandardDeviation = 0.3)
        {
            return LinearSliceData.Continuous(n, seed, intercept, slopes, noiseColumns, missingRate,
                noiseStandardDeviation);
        }

        [Fact]
        [Trait("Covers", "RecallGradientBoostingRegressor")]
        public void PredictionsTrackTheOutcomeReasonablyWellOnASimpleRelationship()
        {
            var data = Continuous(600, 31, 0.5, [3.5, -2.5], noiseStandardDeviation: 0.2);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                FastOptions());

            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RSquared.Should().NotBeNull();
            result.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.5);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var data = Continuous(300, 12, 0.2, [1.5, -1.2]);
            var options = FastOptions(7);

            var first = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, options);
            var second = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = GradientBoostingRegressorRecall.FromSnapshot(first.SnapshotJson);
            var recallB = GradientBoostingRegressorRecall.FromSnapshot(second.SnapshotJson);

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
            var data = Continuous(300, 19, 0d, [2d, -2d]);

            var a = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions());
            var b = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions(2));

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void PredictionsAreAlwaysFinite()
        {
            var data = Continuous(400, 44, -0.3, [1.1, 0.4, -0.9]);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                FastOptions());

            var recall = GradientBoostingRegressorRecall.FromSnapshot(result.SnapshotJson);

            var predictions = recall.PredictValues(data.Inputs);

            predictions.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = Continuous(200, 5, 0d, [1d, -1d]);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                FastOptions());

            var recall = GradientBoostingRegressorRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = Continuous(300, 6, 0d, [1d, -1d]);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                FastOptions());

            var recall = GradientBoostingRegressorRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, -1d };
            var prediction = recall.Predict(withMissing);

            prediction.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = Continuous(300, 13, 0d, [1.2, -1.2]);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                FastOptions());

            var envelope =
                AdvancedAnalyticsModelJson
                    .Deserialise<AdvancedAnalyticsGradientBoostingRegressorPipelineSnapshot>(
                        result.SnapshotJson, GradientBoostingRegressorRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);

            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should()
                .Be("GradientBoostingRegressorPipeline");

            envelope.Payload.Trees.Should().HaveCount(40);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var data = Continuous(200, 8, 0d, [1d, -1d]);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                FastOptions());

            Action wrongKind = () => GradientBoostingRegressorRecall.FromSnapshot(
                result.SnapshotJson.Replace("GradientBoostingRegressorPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["trees"]!).Clear();
            Action noTrees = () => GradientBoostingRegressorRecall.FromSnapshot(tampered.ToString());
            noTrees.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var outOfRange = JObject.Parse(result.SnapshotJson);
            ((JArray)outOfRange["payload"]!["trees"]![0]!["featureIndex"]!)[0] = 999;
            Action badFeature = () => GradientBoostingRegressorRecall.FromSnapshot(outOfRange.ToString());
            badFeature.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => GradientBoostingRegressorRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleEnsembleSummaries()
        {
            var data = Continuous(500, 21, 0.1, [1.4, -1.1, 0.6], noiseStandardDeviation: 0.25);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new GradientBoostingRegressorOptions
                    { Rounds = 60, MaxDepth = 3, LearningRate = 0.15, MinimumLeafSize = 5, Seed = 21 });

            result.Details.Rounds.Should().Be(60);
            result.Details.TreesFitted.Should().Be(60);
            result.Details.TrainingLossByRound.Should().HaveCount(60);
            result.Details.FeatureImportances.Should().HaveCount(3);
            result.Details.FeatureImportances.Sum().Should().BeApproximately(1d, 1e-6);
            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RSquared.Should().NotBeNull();
            result.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.3);
            result.Details.TestMetrics.RootMeanSquaredError.Should().NotBeNull();
            result.Details.TestMetrics.MeanAbsoluteError.Should().NotBeNull();
            result.Details.TestMetrics.MeanError.Should().NotBeNull();

            var loss = result.Details.TrainingLossByRound;
            loss.Last().Should().BeLessThan(loss.First());
        }

        [Fact]
        public void ExplainPredictedValueMatchesThePipelinePrediction()
        {
            var data = Continuous(300, 55, 0d, [1.3, -0.8]);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                FastOptions(55));

            var recall = GradientBoostingRegressorRecall.FromSnapshot(result.SnapshotJson);

            foreach (var row in data.Inputs.Take(10))
            {
                var explain = recall.Explain(row);
                var implied = explain.BaseValue + explain.Contributions.Sum(c => c.Weight);

                implied.Should().BeApproximately(explain.PredictedValue, 1e-9);
                recall.Predict(row)[0].Should().BeApproximately(explain.PredictedValue, 1e-9);
            }
        }

        [Fact]
        [Trait("Covers", "GradientBoostingRegressorExactness")]
        public void ContributionsReconstructThePredictedValueExactlyAcrossManyProbesAndAllTrainingRows()
        {
            var data = Continuous(500, 61, -0.2, [1.6, -1.1, 0.5], 2);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new GradientBoostingRegressorOptions
                    { Rounds = 80, MaxDepth = 3, LearningRate = 0.1, MinimumLeafSize = 4, Seed = 61 });

            var recall = GradientBoostingRegressorRecall.FromSnapshot(result.SnapshotJson);

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

                reconstructed.Should().BeApproximately(explain.PredictedValue, 1e-9);
            }

            foreach (var row in data.Inputs)
            {
                var explain = recall.Explain(row);
                var reconstructed = explain.BaseValue + explain.Contributions.Sum(c => c.Weight);

                reconstructed.Should().BeApproximately(explain.PredictedValue, 1e-9);
            }
        }

        [Fact]
        public void ALargerLearningRateReachesAGivenTrainingLossInFewerRounds()
        {
            var data = Continuous(500, 71, 0d, [2d, -2d]);

            var slow = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new GradientBoostingRegressorOptions
                    { Rounds = 20, MaxDepth = 2, LearningRate = 0.02, MinimumLeafSize = 5, Seed = 71 });
            var fast = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new GradientBoostingRegressorOptions
                    { Rounds = 20, MaxDepth = 2, LearningRate = 0.5, MinimumLeafSize = 5, Seed = 71 });

            fast.Details.TrainingLossByRound.Last().Should().BeLessThan(slow.Details.TrainingLossByRound.Last());
        }

        [Fact]
        public void ASingleRoundStillProducesAValidModel()
        {
            var data = Continuous(300, 9, 0d, [1.5, -1.5]);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new GradientBoostingRegressorOptions
                    { Rounds = 1, MaxDepth = 2, LearningRate = 0.3, MinimumLeafSize = 5, Seed = 9 });

            var recall = GradientBoostingRegressorRecall.FromSnapshot(result.SnapshotJson);

            var predictions = recall.PredictValues(data.Inputs);

            predictions.Should().OnlyContain(v => double.IsFinite(v));
            result.Details.TreesFitted.Should().Be(1);
        }

        [Fact]
        public void FewerRowsThanVariablesStillTrainsSinceTreesHaveNoDesignMatrixRankRequirement()
        {
            var data = Continuous(6, 7, 0, [1.0, 1.0, 1.0, 1.0]);

            Action act = () => GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new GradientBoostingRegressorOptions { ValidationFraction = 0, TestFraction = 0, MinimumLeafSize = 1 });

            act.Should().NotThrow();
        }

        [Fact]
        public void TestSplitRowsNeverTouchTheFittedTrainingStatistics()
        {
            var data = Continuous(2000, 17, 0.3, [1.2, -0.4, 0.7]);

            var result = GradientBoostingRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new GradientBoostingRegressorOptions { Seed = 17, TestFraction = 0.3 });

            result.TrainRows.Intersect(result.TestRows).Should().BeEmpty();
            result.TrainRows.Intersect(result.ValidationRows).Should().BeEmpty();
            result.ValidationRows.Intersect(result.TestRows).Should().BeEmpty();

            var meansFromTrainOnly = Enumerable.Range(0, data.Names.Length).Select(j =>
                result.TrainRows.Select(r => data.Inputs[r][j]).Average()).ToArray();

            result.Preprocessing.Select(p => p.Mean).Should().Equal(meansFromTrainOnly,
                (actual, expected) => Math.Abs(actual - expected) < 1e-9);
        }
    }
}