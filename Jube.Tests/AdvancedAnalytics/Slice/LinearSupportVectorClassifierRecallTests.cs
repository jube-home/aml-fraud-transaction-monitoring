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
    public class LinearSupportVectorClassifierRecallTests
    {
        private static (double[][] Inputs, int[] Labels) SeparatedGaussianBlobs(int perClass, int seed)
        {
            var random = new Random(seed);
            var inputs = new double[perClass * 2][];
            var labels = new int[perClass * 2];
            var index = 0;

            for (var c = 0; c < 2; c++)
            {
                var centre = c == 1 ? 8d : -8d;

                for (var i = 0; i < perClass; i++)
                {
                    inputs[index] =
                    [
                        centre + SliceData.Gaussian(random), centre * 0.5 + SliceData.Gaussian(random)
                    ];

                    labels[index] = c;
                    index++;
                }
            }

            return (inputs, labels);
        }

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallLinearSupportVectorClassifier")]
        public void ClearlyPositiveAndNegativeProbesRecallOnTheCorrectSideOfOneHalf()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(300, 11);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 5 });

            var recall = LinearSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            var positiveProbe = recall.Predict([8d, 4d]);
            var negativeProbe = recall.Predict([-8d, -4d]);

            positiveProbe.Should().HaveCount(1);
            positiveProbe[0].Should().BeGreaterThan(0.5);
            negativeProbe[0].Should().BeLessThan(0.5);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(200, 42);
            var options = new LinearSupportVectorClassifierOptions { Seed = 9 };

            var first = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2), options);
            var second = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2), options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = LinearSupportVectorClassifierRecall.FromSnapshot(first.SnapshotJson);
            var recallB = LinearSupportVectorClassifierRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in inputs.Take(50))
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
        public void ExplainContributionsPlusTheBiasSumExactlyToTheRawDecisionFunction()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(250, 21);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 3 });

            var recall = LinearSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            foreach (var row in inputs.Take(30))
            {
                var explain = recall.Explain(row);
                var reconstructed = explain.Bias + explain.Contributions.Sum(c => c.Weight);

                reconstructed.Should().BeApproximately(explain.RawDecisionFunction, 1e-9);
            }
        }

        [Fact]
        public void ExplainContributionsAreGenuinelyExactNotJustFlagged()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(150, 7);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 1 });

            var recall = LinearSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            var probe = new[] { 3d, -2d };
            var explain = recall.Explain(probe);
            var adaptation = explain.ToAdaptation(recall.Predict(probe)[0], "Probe");

            adaptation.Contribution!.Exact.Should().BeTrue();

            var sum = adaptation.Contribution.BaseValue!.Value +
                      adaptation.Contribution.Items.Sum(i => i.Weight!.Value);

            sum.Should().BeApproximately(explain.RawDecisionFunction, 1e-9);
        }

        [Fact]
        public void ConstantFeatureAcrossTheWholeTrainingSplitIsWarnedAboutButDoesNotThrow()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(150, 17);

            foreach (var row in inputs)
            {
                row[1] = 42d;
            }

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 1 });

            result.Warnings.Should().Contain(w => w.Code == LinearSupportVectorWarningCodes.ConstantColumn);
            var recall = LinearSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);
            var prediction = recall.Predict([1d, 42d]);
            prediction[0].Should().BeInRange(0d, 1d);
            double.IsFinite(prediction[0]).Should().BeTrue();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(100, 5);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 1 });

            var recall = LinearSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(150, 6);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 1 });

            var recall = LinearSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 4d };
            var prediction = recall.Predict(withMissing);

            prediction.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(150, 13);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 4 });

            var envelope =
                AdvancedAnalyticsModelJson
                    .Deserialise<AdvancedAnalyticsLinearSupportVectorClassifierPipelineSnapshot>(result.SnapshotJson,
                        LinearSupportVectorClassifierRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);

            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should()
                .Be("LinearSupportVectorClassifierPipeline");

            envelope.Payload.Weights.Should().HaveCount(2);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(150, 8);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 1 });

            Action wrongKind = () => LinearSupportVectorClassifierRecall.FromSnapshot(
                result.SnapshotJson.Replace("LinearSupportVectorClassifierPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["weights"]!)[0] = double.NaN;
            Action nonFiniteWeight = () => LinearSupportVectorClassifierRecall.FromSnapshot(tampered.ToString());
            nonFiniteWeight.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => LinearSupportVectorClassifierRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleMetricsAndCalibration()
        {
            var (inputs, labels) = SeparatedGaussianBlobs(300, 19);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 2, ValidationFraction = 0.2, TestFraction = 0.2 });

            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.9);
            result.Details.Calibration.Applied.Should().BeTrue();
            result.Details.SupportVectorCount.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1.0)]
        [InlineData(100.0)]
        public void VaryingComplexityValuesAllProduceAValidFiniteModel(double complexity)
        {
            var (inputs, labels) = SeparatedGaussianBlobs(200, 23);

            var result = LinearSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new LinearSupportVectorClassifierOptions { Seed = 1, Complexity = complexity });

            result.Details.Complexity.Should().Be(complexity);
            var recall = LinearSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);
            var prediction = recall.Predict([8d, 4d]);

            double.IsFinite(prediction[0]).Should().BeTrue();
            prediction[0].Should().BeGreaterThan(0.5);
        }
    }
}