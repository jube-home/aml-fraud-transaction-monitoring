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
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class RandomForestRegressorRecallTests
    {
        private static RandomForestRegressorOptions FastOptions(int seed = 1)
        {
            return new RandomForestRegressorOptions
            {
                TreeCount = 25,
                MaxDepth = 4,
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

        private static LinearSliceDataset MissingData()
        {
            return Continuous(1500, 91, -0.8, [3.0, -2.6, 1.4], 1, 0.08);
        }

        [Fact]
        [Trait("Covers", "RecallRandomForestRegressor")]
        public void StrongSignalDataProducesAHighTestRSquared()
        {
            var data = Continuous(1200, 31, 0d, [4d, -4d], noiseStandardDeviation: 0.2);
            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions());

            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RSquared.Should().NotBeNull();
            result.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.5);
        }

        [Fact]
        public void RecallReproducesTrainingTimePredictionsBitForBitIncludingMissingInputs()
        {
            var data = MissingData();
            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions(91));

            data.Inputs.Count(r => r.Any(double.IsNaN)).Should().BeGreaterThan(50);

            var recall = RandomForestRegressorRecall.FromSnapshot(result.SnapshotJson);
            recall.Variables.Should().Equal(data.Names);

            void AssertBitForBit(RandomForestRegressorPredictions predictions)
            {
                predictions.Rows.Length.Should().BeGreaterThan(0);

                for (var i = 0; i < predictions.Rows.Length; i++)
                {
                    var output = recall.Predict(data.Inputs[predictions.Rows[i]]);
                    output.Should().HaveCount(1);

                    SliceData.Bits(output[0]).Should().Be(SliceData.Bits(predictions.Predicted[i]),
                        "row " + predictions.Rows[i]);
                }
            }

            AssertBitForBit(result.TrainPredictions);
            AssertBitForBit(result.ValidationPredictions);
            AssertBitForBit(result.TestPredictions);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var data = Continuous(600, 12, 0.2, [1.5, -1.2]);
            var options = FastOptions(7);

            var first = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, options);
            var second = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = RandomForestRegressorRecall.FromSnapshot(first.SnapshotJson);
            var recallB = RandomForestRegressorRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in data.Inputs.Take(40))
            {
                SliceData.Bits(recallA.Predict(row)[0]).Should().Be(SliceData.Bits(recallB.Predict(row)[0]));
            }
        }

        [Fact]
        public void ADifferentSeedCanProduceADifferentSnapshot()
        {
            var data = Continuous(600, 19, 0d, [2d, -2d]);

            var a = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions());
            var b = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions(2));

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = Continuous(400, 5, 0d, [1d, -1d]);
            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions());
            var recall = RandomForestRegressorRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = Continuous(600, 6, 0d, [1d, -1d]);
            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions());
            var recall = RandomForestRegressorRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, -1d };
            var filled = new[] { result.FillValues[0], -1d };

            recall.Predict(withMissing)[0].Should().Be(recall.Predict(filled)[0]);
            double.IsFinite(recall.Predict(withMissing)[0]).Should().BeTrue();

            var infinite = new[] { double.PositiveInfinity, -1d };
            SliceData.Bits(recall.Predict(infinite)[0]).Should().Be(SliceData.Bits(recall.Predict(withMissing)[0]));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = MissingData();
            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions(91));

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsRandomForestRegressorPipelineSnapshot>(
                    result.SnapshotJson, RandomForestRegressorRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("RandomForestRegressorPipeline");
            envelope.Calibrator.Should().BeNull();
            envelope.Payload.Trees.Should().HaveCount(25);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var data = Continuous(400, 8, 0d, [1d, -1d]);
            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions());

            Action wrongKind = () => RandomForestRegressorRecall.FromSnapshot(
                result.SnapshotJson.Replace("RandomForestRegressorPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["trees"]!).Clear();
            Action noTrees = () => RandomForestRegressorRecall.FromSnapshot(tampered.ToString());
            noTrees.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var outOfRange = JObject.Parse(result.SnapshotJson);
            ((JArray)outOfRange["payload"]!["trees"]![0]!["featureIndex"]!)[0] = 999;
            Action badFeature = () => RandomForestRegressorRecall.FromSnapshot(outOfRange.ToString());
            badFeature.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => RandomForestRegressorRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleForestSummaries()
        {
            var data = Continuous(900, 21, 0.1, [3d, -2d, 1d]);

            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new RandomForestRegressorOptions
                    { TreeCount = 30, MaxDepth = 4, MinimumLeafSize = 5, Seed = 21 });

            result.Details.TreeCount.Should().Be(30);
            result.Details.FeaturesPerSplit.Should().BeGreaterThan(0).And.BeLessOrEqualTo(3);
            result.Details.FeatureImportances.Should().HaveCount(3);
            result.Details.FeatureImportances.Sum().Should().BeApproximately(1d, 1e-6);
            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RSquared.Should().NotBeNull();
            result.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.4);
            result.Details.OutOfBagRSquared.Should().NotBeNull();
        }

        [Fact]
        public void ExplainPredictedValueEqualsBaseValuePlusContributions()
        {
            var data = Continuous(500, 55, 0d, [1.3, -0.8]);
            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions(55));
            var recall = RandomForestRegressorRecall.FromSnapshot(result.SnapshotJson);

            foreach (var row in data.Inputs.Take(10))
            {
                var explain = recall.Explain(row);
                var implied = explain.BaseValue + explain.Contributions.Sum(c => c.Weight);

                implied.Should().BeApproximately(explain.PredictedValue, 1e-9);
                recall.Predict(row)[0].Should().BeApproximately(explain.PredictedValue, 1e-9);
            }
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(0.5d)]
        [InlineData(1d)]
        public void FeatureFractionResolvesToAPlausibleFeaturesPerSplit(double featureFraction)
        {
            var data = Continuous(500, 40, 0d, [1d, -1d, 0.5d], 3);

            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new RandomForestRegressorOptions
                {
                    TreeCount = 10, MaxDepth = 3, MinimumLeafSize = 5, Seed = 40, FeatureFraction = featureFraction
                });

            result.Details.FeaturesPerSplit.Should().BeGreaterThan(0).And.BeLessOrEqualTo(data.Names.Length);

            if (featureFraction == 0d)
            {
                result.Details.FeaturesPerSplit.Should().Be(Math.Max(1, data.Names.Length / 3));
            }
            else if (featureFraction == 1d)
            {
                result.Details.FeaturesPerSplit.Should().Be(data.Names.Length);
            }
        }

        [Fact]
        public void FewerRowsThanTreesStillTrainsSuccessfully()
        {
            var data = Continuous(20, 7, 0d, [1.0, 1.0]);

            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new RandomForestRegressorOptions
                {
                    TreeCount = 5, MaxDepth = 2, MinimumLeafSize = 2, ValidationFraction = 0, TestFraction = 0,
                    Seed = 7
                });

            result.SplitSizes.Train.Should().Be(20);
        }

        [Fact]
        public void RecallIsThreadSafeAndDeterministicUnderContention()
        {
            var data = MissingData();
            var result = RandomForestRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, FastOptions(91));
            var recall = RandomForestRegressorRecall.FromSnapshot(result.SnapshotJson);
            var rows = data.Inputs.Take(400).ToArray();
            var expected = rows.Select(r => SliceData.Bits(recall.Predict(r)[0])).ToArray();
            var mismatches = 0;

            Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
            {
                for (var repeat = 0; repeat < 6; repeat++)
                {
                    var outputs = recall.PredictValues(rows);

                    for (var i = 0; i < rows.Length; i++)
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