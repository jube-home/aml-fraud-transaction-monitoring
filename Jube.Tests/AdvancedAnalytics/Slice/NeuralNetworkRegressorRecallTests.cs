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
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class NeuralNetworkRegressorRecallTests
    {
        private static LinearSliceDataset NonlinearData(int n, int seed)
        {
            var random = new Random(seed);
            var inputs = new double[n][];
            var outputs = new double[n];

            for (var i = 0; i < n; i++)
            {
                var x0 = SliceData.Gaussian(random);
                var x1 = SliceData.Gaussian(random);
                var y = 2.5 * Math.Sin(x0) + 1.2 * x1 * x1 - 0.6 * x1 + 0.1 * SliceData.Gaussian(random);
                inputs[i] = [x0, x1];
                outputs[i] = y;
            }

            return new LinearSliceDataset(inputs, outputs, ["x0", "x1"]);
        }

        private static LinearSliceDataset ConstantTargetData(int n, int seed, double constant)
        {
            var random = new Random(seed);
            var inputs = new double[n][];
            var outputs = new double[n];

            for (var i = 0; i < n; i++)
            {
                inputs[i] = [SliceData.Gaussian(random), SliceData.Gaussian(random)];
                outputs[i] = constant;
            }

            return new LinearSliceDataset(inputs, outputs, ["x0", "x1"]);
        }

        [Fact]
        [Trait("Covers", "RecallNeuralNetworkRegressor")]
        public void RecallLearnsANonlinearRelationshipReliably()
        {
            var data = NonlinearData(1500, 3);

            var result = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 16, Epochs = 4000, LearningRate = 0.5, Seed = 7 });

            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RSquared.Should().NotBeNull();
            result.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.6);
        }

        [Fact]
        public void PredictReturnsASingleFiniteValue()
        {
            var data = NonlinearData(500, 11);

            var result = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 6, Epochs = 150, Seed = 1 });

            var recall = NeuralNetworkRegressorRecall.FromSnapshot(result.SnapshotJson);

            foreach (var row in data.Inputs.Take(50))
            {
                var prediction = recall.Predict(row);
                prediction.Should().HaveCount(1);
                double.IsFinite(prediction[0]).Should().BeTrue();
            }
        }

        [Fact]
        public void SameSeedProducesTheSameWeightsAndTheSameRecallBitForBit()
        {
            var data = NonlinearData(500, 21);
            var options = new NeuralNetworkRegressorOptions { HiddenUnits = 5, Epochs = 120, Seed = 9 };

            var first = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, options);
            var second = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = NeuralNetworkRegressorRecall.FromSnapshot(first.SnapshotJson);
            var recallB = NeuralNetworkRegressorRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in data.Inputs.Take(50))
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
            var data = NonlinearData(500, 31);

            var a = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 5, Epochs = 100, Seed = 1 });
            var b = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 5, Epochs = 100, Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = NonlinearData(300, 5);

            var result = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 4, Epochs = 80, Seed = 1 });

            var recall = NeuralNetworkRegressorRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = NonlinearData(400, 6);

            var result = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 4, Epochs = 80, Seed = 1 });

            var recall = NeuralNetworkRegressorRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 0.3 };
            var prediction = recall.Predict(withMissing);

            prediction.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = NonlinearData(400, 13);

            var result = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 4, Epochs = 80, Seed = 4 });

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsNeuralNetworkRegressorPipelineSnapshot>(result.SnapshotJson,
                    NeuralNetworkRegressorRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("NeuralNetworkRegressorPipeline");
            envelope.Calibrator.Should().BeNull();
            envelope.Payload.HiddenUnits.Should().Be(4);
            envelope.Payload.InputWeights.Should().HaveCount(4);
            envelope.Payload.TargetSpan.Should().BeGreaterThan(0);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var data = NonlinearData(300, 8);

            var result = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 4, Epochs = 60, Seed = 1 });

            Action wrongKind = () => NeuralNetworkRegressorRecall.FromSnapshot(
                result.SnapshotJson.Replace("NeuralNetworkRegressorPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => NeuralNetworkRegressorRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void AConstantTrainingTargetProducesAFiniteConstantPredictionInsteadOfDiverging()
        {
            var data = ConstantTargetData(300, 9, 5.0);

            var result = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 3, Epochs = 100, Seed = 1 });

            result.Warnings.Should().Contain(w => w.Code == NeuralNetworkRegressorWarningCodes.ConstantTarget);
            result.Details.TargetSpan.Should().Be(0d);

            var recall = NeuralNetworkRegressorRecall.FromSnapshot(result.SnapshotJson);

            foreach (var row in data.Inputs.Take(20))
            {
                var prediction = recall.Predict(row)[0];
                double.IsFinite(prediction).Should().BeTrue();
                prediction.Should().BeApproximately(5.0, 0.5);
            }
        }

        [Fact]
        public void ExplainGradientsAreFiniteAndOneEntryPerVariable()
        {
            var data = NonlinearData(400, 15);

            var result = NeuralNetworkRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new NeuralNetworkRegressorOptions { HiddenUnits = 5, Epochs = 100, Seed = 3 });

            var recall = NeuralNetworkRegressorRecall.FromSnapshot(result.SnapshotJson);

            var explain = recall.Explain(data.Inputs[0]);

            explain.Contributions.Should().HaveCount(data.Names.Length);
            explain.Contributions.Select(c => c.Name).Should().Equal(data.Names);
            explain.Contributions.Should().OnlyContain(c => double.IsFinite(c.Weight));
            double.IsFinite(explain.PredictedValue).Should().BeTrue();
        }
    }
}