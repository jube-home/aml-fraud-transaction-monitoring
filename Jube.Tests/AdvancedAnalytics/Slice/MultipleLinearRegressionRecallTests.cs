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
using System.Text.Json;
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
    public class MultipleLinearRegressionRecallTests
    {
        private static LinearSliceDataset Continuous(int n, int seed, double intercept, double[] slopes,
            int noiseColumns = 0, double missingRate = 0d, double noiseStandardDeviation = 0.3)
        {
            return LinearSliceData.Continuous(n, seed, intercept, slopes, noiseColumns, missingRate,
                noiseStandardDeviation);
        }

        private static LinearSliceDataset MissingData()
        {
            return Continuous(4000, 91, -0.8, [1.0, -0.6, 0.4], 1, 0.08);
        }

        private static LinearRegressionResult Train(LinearSliceDataset data,
            LinearRegressionOptions? options = null)
        {
            return LinearRegressionTrainer.Train(data.Inputs, data.Outputs, data.Names,
                options ?? new LinearRegressionOptions { Seed = 3 });
        }

        private static void AssertBitForBit(MultipleLinearRegressionRecall recall, LinearSliceDataset data,
            LinearPredictions predictions)
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

        [Fact]
        public void RecallReproducesTrainingTimePredictionsBitForBitIncludingMissingInputs()
        {
            var data = MissingData();
            var result = Train(data);

            result.Preprocessing.Count(p => p.HasMissingIndicator).Should().BeGreaterThan(0);
            data.Inputs.Count(r => r.Any(double.IsNaN)).Should().BeGreaterThan(500);

            var recall = MultipleLinearRegressionRecall.FromSnapshot(result.SnapshotJson);

            recall.Variables.Should().Equal(data.Names);
            AssertBitForBit(recall, data, result.TrainPredictions);
            AssertBitForBit(recall, data, result.ValidationPredictions);
            AssertBitForBit(recall, data, result.TestPredictions);
        }

        [Fact]
        public void RecallReproducesTheUnstandardisedPipelineToo()
        {
            var data = MissingData();

            foreach (var options in new[]
                     {
                         new LinearRegressionOptions { Standardise = false }
                     })
            {
                var result = Train(data, options);
                var recall = MultipleLinearRegressionRecall.FromSnapshot(result.SnapshotJson);
                AssertBitForBit(recall, data, result.TestPredictions);
            }
        }

        [Fact]
        public void APredictionIsExactlyTheDotProductOfWeightsAndStandardisedInputsPlusTheIntercept()
        {
            var data = Continuous(1000, 44, 2.5, [1.5, -0.5]);
            var result = Train(data, new LinearRegressionOptions { Seed = 5 });
            var recall = MultipleLinearRegressionRecall.FromSnapshot(result.SnapshotJson);

            var probe = data.Inputs[0];
            var explain = recall.Explain(probe);

            explain.Contributions.Sum(c => c.Weight).Should().BeApproximately(
                explain.PredictedValue - explain.Intercept, 1e-9);

            recall.Predict(probe)[0].Should().BeApproximately(explain.PredictedValue, 1e-9);
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedianAndFlagged()
        {
            var data = MissingData();
            var result = Train(data);
            var recall = MultipleLinearRegressionRecall.FromSnapshot(result.SnapshotJson);
            var row = new[] { 0.3, -0.2, 0.9, 0.1 };

            var withMissing = (double[])row.Clone();
            withMissing[1] = double.NaN;
            var filled = (double[])row.Clone();
            filled[1] = result.Preprocessing[1].FillValue;

            recall.Predict(withMissing)[0].Should().NotBe(recall.Predict(filled)[0]);

            var infinite = (double[])row.Clone();
            infinite[1] = double.PositiveInfinity;
            SliceData.Bits(recall.Predict(infinite)[0]).Should().Be(SliceData.Bits(recall.Predict(withMissing)[0]));

            var allMissing = Enumerable.Repeat(double.NaN, 4).ToArray();
            var predicted = recall.Predict(allMissing)[0];
            double.IsFinite(predicted).Should().BeTrue();
        }

        [Fact]
        public void RecallRejectsMissingValuesWhenTheModelWasTrainedToReject()
        {
            var data = Continuous(1000, 92, -0.5, [1.0, -0.5]);

            var result = Train(data,
                new LinearRegressionOptions { MissingValuePolicy = LinearMissingValuePolicy.Reject });

            var recall = MultipleLinearRegressionRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([double.NaN, 1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*rejects missing*");
            double.IsFinite(recall.Predict([0.2, 1d])[0]).Should().BeTrue();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var result = Train(MissingData());
            var recall = MultipleLinearRegressionRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d, 2d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 4*");
        }

        [Fact]
        public void FewerRowsThanVariablesThrowsAClearException()
        {
            var data = Continuous(3, 7, 0, [1.0, 1.0, 1.0, 1.0]);

            Action act = () => LinearRegressionTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new LinearRegressionOptions { ValidationFraction = 0, TestFraction = 0 });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var result = Train(MissingData());

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLinearRegressionPipelineSnapshot>(
                result.SnapshotJson, MultipleLinearRegressionRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("MultipleLinearRegressionPipeline");
            envelope.Calibrator.Should().BeNull();
            envelope.Payload.Weights.Should().Equal(result.Weights);
            envelope.Payload.Means.Should().Equal(result.Preprocessing.Select(p => p.Mean));
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var result = Train(MissingData());

            Action wrongKind = () => MultipleLinearRegressionRecall.FromSnapshot(
                result.SnapshotJson.Replace("MultipleLinearRegressionPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["weights"]!).RemoveAt(0);
            Action shortWeights = () => MultipleLinearRegressionRecall.FromSnapshot(tampered.ToString());
            shortWeights.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => MultipleLinearRegressionRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void RecallIsThreadSafeAndDeterministicUnderContention()
        {
            var data = MissingData();
            var result = Train(data);
            var recall = MultipleLinearRegressionRecall.FromSnapshot(result.SnapshotJson);
            var rows = data.Inputs.Take(1500).ToArray();
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

        [Fact]
        public void TestSplitRowsNeverTouchTheFittedTrainingStatistics()
        {
            var data = Continuous(2000, 17, 0.3, [1.2, -0.4, 0.7]);
            var result = Train(data, new LinearRegressionOptions { Seed = 17, TestFraction = 0.3 });

            result.TrainRows.Intersect(result.TestRows).Should().BeEmpty();
            result.TrainRows.Intersect(result.ValidationRows).Should().BeEmpty();
            result.ValidationRows.Intersect(result.TestRows).Should().BeEmpty();

            var meansFromTrainOnly = Enumerable.Range(0, data.Names.Length).Select(j =>
                result.TrainRows.Select(r => data.Inputs[r][j]).Average()).ToArray();

            result.Preprocessing.Select(p => p.Mean).Should().Equal(meansFromTrainOnly,
                (actual, expected) => Math.Abs(actual - expected) < 1e-9);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndDifferentSeedsDoNot()
        {
            var data = Continuous(1200, 21, -0.2, [0.9, 0.4]);

            var first = Train(data, new LinearRegressionOptions { Seed = 21 });
            var second = Train(data, new LinearRegressionOptions { Seed = 21 });
            var third = Train(data, new LinearRegressionOptions { Seed = 22 });

            first.SnapshotJson.Should().Be(second.SnapshotJson);
            first.SnapshotJson.Should().NotBe(third.SnapshotJson);
        }

        [Fact]
        public void TheResultSerialisesForAnLlmWithoutBulkPredictionArrays()
        {
            var data = Continuous(1500, 93, -0.5, [1.0]);
            var result = Train(data, new LinearRegressionOptions { ValidationFraction = 0.3, TestFraction = 0.3 });

            var json = JsonSerializer.Serialize(result);

            json.Should().Contain("Coefficients").And.Contain("Warnings").And.Contain("SnapshotJson");
            json.Should().NotContain("TrainPredictions").And.NotContain("TestRows");
        }
    }
}