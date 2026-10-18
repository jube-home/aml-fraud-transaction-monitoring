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
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class LogisticRegressionRecallTests
    {
        private static SliceDataset MissingData()
        {
            return SliceData.Logistic(4000, 91, -0.8, [1.0, -0.6, 0.4], 1, 0.08);
        }

        private static LogisticRegressionResult Train(SliceDataset data, LogisticRegressionOptions? options = null)
        {
            return LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names,
                options ?? new LogisticRegressionOptions { Seed = 3 });
        }

        private static void AssertBitForBit(LogisticRegressionRecall recall, SliceDataset data,
            LogisticPredictions predictions)
        {
            predictions.Rows.Length.Should().BeGreaterThan(0);

            for (var i = 0; i < predictions.Rows.Length; i++)
            {
                var output = recall.Predict(data.Inputs[predictions.Rows[i]]);
                output.Should().HaveCount(1);

                SliceData.Bits(output[0]).Should().Be(SliceData.Bits(predictions.Probabilities[i]),
                    "row " + predictions.Rows[i]);
            }
        }

        [Fact]
        [Trait("Covers", "RecallLogisticRegression")]
        public void RecallReproducesTrainingTimePredictionsBitForBitIncludingMissingInputs()
        {
            var data = MissingData();
            var result = Train(data);

            result.Preprocessing.Count(p => p.HasMissingIndicator).Should().BeGreaterThan(0);
            data.Inputs.Count(r => r.Any(double.IsNaN)).Should().BeGreaterThan(500);

            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);

            recall.Variables.Should().Equal(data.Names);
            AssertBitForBit(recall, data, result.TrainPredictions);
            AssertBitForBit(recall, data, result.ValidationPredictions);
            AssertBitForBit(recall, data, result.TestPredictions);
        }

        [Fact]
        public void RecallReproducesTheUncalibratedAndUnstandardisedPipelinesToo()
        {
            var data = MissingData();

            foreach (var options in new[]
                     {
                         new LogisticRegressionOptions { Calibrate = false },
                         new LogisticRegressionOptions { Standardise = false },
                         new LogisticRegressionOptions
                             { RidgeLambda = 5d, ClassWeighting = LogisticClassWeighting.Balanced }
                     })
            {
                var result = Train(data, options);
                var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);
                AssertBitForBit(recall, data, result.TestPredictions);
                result.Calibration.Applied.Should().Be(options.Calibrate);
            }
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedianAndFlagged()
        {
            var data = MissingData();
            var result = Train(data);
            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);
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
            var probability = recall.Predict(allMissing)[0];
            probability.Should().BeInRange(0d, 1d);
            double.IsFinite(probability).Should().BeTrue();
        }

        [Fact]
        public void RecallRejectsMissingValuesWhenTheModelWasTrainedToReject()
        {
            var data = SliceData.Logistic(1000, 92, -0.5, [1.0, -0.5]);

            var result = Train(data,
                new LogisticRegressionOptions { MissingValuePolicy = LogisticMissingValuePolicy.Reject });

            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([double.NaN, 1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*rejects missing*");
            recall.Predict([0.2, 1d])[0].Should().BeInRange(0d, 1d);
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var result = Train(MissingData());
            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d, 2d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 4*");
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var result = Train(MissingData());

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionPipelineSnapshot>(
                result.SnapshotJson, LogisticRegressionRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("LogisticRegressionPipeline");
            envelope.Calibrator!.Method.Should().Be("Platt");
            envelope.Payload.TrainingPrior.Should().Be(result.Priors.EffectivePrior);
            envelope.Payload.BaseRate.Should().Be(result.Priors.BaseRate);
            envelope.Payload.Weights.Should().Equal(result.Weights);
            envelope.Payload.Means.Should().Equal(result.Preprocessing.Select(p => p.Mean));
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void TheSnapshotCarriesSignificancesMatchingTheCoefficientTableAndExplainSurfacesThem()
        {
            var data = MissingData();
            var result = Train(data);

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionPipelineSnapshot>(
                result.SnapshotJson, LogisticRegressionRecall.ModelKind);

            var expected = result.Coefficients.Skip(1).Select(c => c.PValue).ToArray();

            envelope.Payload.Significances.Should().Equal(expected);
            expected.Should().Contain(p => p.HasValue);

            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);
            var explain = recall.Explain(data.Inputs[0]);

            explain.Contributions.Select(c => c.Significance).Should().Equal(expected);
        }

        [Fact]
        public void RecallFallsBackToNullSignificancesWhenTheSnapshotPredatesTheField()
        {
            var data = MissingData();
            var result = Train(data);

            var old = JObject.Parse(result.SnapshotJson);
            ((JObject)old["payload"]!).Remove("significances");

            var recall = LogisticRegressionRecall.FromSnapshot(old.ToString());
            var explain = recall.Explain(data.Inputs[0]);

            explain.Contributions.Should().OnlyContain(c => c.Significance == null);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var result = Train(MissingData());

            Action wrongKind = () => LogisticRegressionRecall.FromSnapshot(
                result.SnapshotJson.Replace("LogisticRegressionPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["weights"]!).RemoveAt(0);
            Action shortWeights = () => LogisticRegressionRecall.FromSnapshot(tampered.ToString());
            shortWeights.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var badPrior = JObject.Parse(result.SnapshotJson);
            badPrior["payload"]!["baseRate"] = 1.5;
            Action prior = () => LogisticRegressionRecall.FromSnapshot(badPrior.ToString());
            prior.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var badMethod = JObject.Parse(result.SnapshotJson);
            badMethod["calibrator"]!["method"] = "Isotonic";
            Action method = () => LogisticRegressionRecall.FromSnapshot(badMethod.ToString());
            method.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => LogisticRegressionRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void RecallIsThreadSafeAndDeterministicUnderContention()
        {
            var data = MissingData();
            var result = Train(data);
            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);
            var rows = data.Inputs.Take(1500).ToArray();
            var expected = rows.Select(r => SliceData.Bits(recall.Predict(r)[0])).ToArray();
            var mismatches = 0;

            Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
            {
                for (var repeat = 0; repeat < 6; repeat++)
                {
                    var outputs = recall.PredictProbabilities(rows);

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
        public void TheToolsScoreExactlyLikeTheTrainerAndRecall()
        {
            var data = MissingData();
            var tools = new LogisticRegressionTools();

            var result =
                tools.TrainLogisticRegression(data.Inputs, data.Labels, data.Names, seed: 3, testFraction: 0.25);

            var direct = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names,
                new LogisticRegressionOptions { Seed = 3, TestFraction = 0.25 });

            result.SnapshotJson.Should().Be(direct.SnapshotJson);

            var scores = tools.RecallLogisticRegression(result.SnapshotJson, [.. data.Inputs.Take(50)]);
            scores.Should().HaveCount(50);

            var recall = LogisticRegressionRecall.FromSnapshot(result.SnapshotJson);

            for (var i = 0; i < 50; i++)
            {
                SliceData.Bits(scores[i]).Should().Be(SliceData.Bits(recall.Predict(data.Inputs[i])[0]));
            }

            var costed = tools.TrainLogisticRegression(data.Inputs, data.Labels, data.Names,
                costFalseNegative: 9, costFalsePositive: 1);

            costed.TestMetrics!.Threshold.Should().BeApproximately(0.1, 1e-12);

            Action oneCost = () =>
                tools.TrainLogisticRegression(data.Inputs, data.Labels, data.Names, costFalseNegative: 9);
            oneCost.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TheResultSerialisesForAnLlmWithoutNaNAndWithoutBulkPredictionArrays()
        {
            var data = SliceData.Logistic(1500, 93, -0.5, [1.0]);
            var result = Train(data, new LogisticRegressionOptions { ValidationFraction = 0.3, TestFraction = 0.3 });

            var json = JsonSerializer.Serialize(result);

            json.Should().Contain("Coefficients").And.Contain("Warnings").And.Contain("SnapshotJson");
            json.Should().NotContain("TrainPredictions").And.NotContain("TestRows");
        }
    }
}