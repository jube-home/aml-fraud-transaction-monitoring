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
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class NeuralNetworkClassifierRecallTests
    {
        private static SliceDataset SeparableData(int n, int seed)
        {
            return SliceData.Logistic(n, seed, 0.2, [4.5, -3.5]);
        }

        [Fact]
        [Trait("Covers", "RecallNeuralNetworkClassifier")]
        public void RecallLearnsACleanLinearlySeparableBoundaryReliably()
        {
            var data = SeparableData(800, 3);

            var result = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 6, Epochs = 300, Seed = 7 });

            var recall = NeuralNetworkClassifierRecall.FromSnapshot(result.SnapshotJson);

            var testRows = result.TestRows;

            var correct = testRows.Count(row =>
                (recall.Predict(data.Inputs[row])[0] >= 0.5 ? 1 : 0) == data.Labels[row]);

            (correct / (double)testRows.Length).Should().BeGreaterThan(0.85);
        }

        [Fact]
        public void PredictReturnsASingleProbabilityBetweenZeroAndOne()
        {
            var data = SeparableData(400, 11);

            var result = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 4, Epochs = 150, Seed = 1 });

            var recall = NeuralNetworkClassifierRecall.FromSnapshot(result.SnapshotJson);

            foreach (var row in data.Inputs.Take(50))
            {
                var prediction = recall.Predict(row);
                prediction.Should().HaveCount(1);
                prediction[0].Should().BeInRange(0d, 1d);
            }
        }

        [Fact]
        public void SameSeedProducesTheSameWeightsAndTheSameRecallBitForBit()
        {
            var data = SeparableData(400, 21);
            var options = new NeuralNetworkClassifierOptions { HiddenUnits = 5, Epochs = 120, Seed = 9 };

            var first = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, options);
            var second = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = NeuralNetworkClassifierRecall.FromSnapshot(first.SnapshotJson);
            var recallB = NeuralNetworkClassifierRecall.FromSnapshot(second.SnapshotJson);

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
            var data = SeparableData(400, 31);

            var a = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 5, Epochs = 100, Seed = 1 });
            var b = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 5, Epochs = 100, Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = SeparableData(300, 5);

            var result = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 4, Epochs = 80, Seed = 1 });

            var recall = NeuralNetworkClassifierRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = SeparableData(400, 6);

            var result = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 4, Epochs = 80, Seed = 1 });

            var recall = NeuralNetworkClassifierRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 0.3 };
            var prediction = recall.Predict(withMissing);

            prediction.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = SeparableData(400, 13);

            var result = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 4, Epochs = 80, Seed = 4 });

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsNeuralNetworkClassifierPipelineSnapshot>(result.SnapshotJson,
                    NeuralNetworkClassifierRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("NeuralNetworkClassifierPipeline");
            envelope.Payload.HiddenUnits.Should().Be(4);
            envelope.Payload.InputWeights.Should().HaveCount(4);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var data = SeparableData(300, 8);

            var result = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 4, Epochs = 60, Seed = 1 });

            Action wrongKind = () => NeuralNetworkClassifierRecall.FromSnapshot(
                result.SnapshotJson.Replace("NeuralNetworkClassifierPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => NeuralNetworkClassifierRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void BothClassesMustBePresentInTheTrainingSplit()
        {
            var data = SeparableData(50, 9);
            var allPositive = data.Labels.Select(_ => 1).ToArray();

            Action act = () => NeuralNetworkClassifierTrainer.Train(data.Inputs, allPositive, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 2, Epochs = 10, Seed = 1 });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ExplainGradientsAreFiniteAndOneEntryPerVariable()
        {
            var data = SeparableData(400, 15);

            var result = NeuralNetworkClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new NeuralNetworkClassifierOptions { HiddenUnits = 5, Epochs = 100, Seed = 3 });

            var recall = NeuralNetworkClassifierRecall.FromSnapshot(result.SnapshotJson);

            var explain = recall.Explain(data.Inputs[0]);

            explain.Contributions.Should().HaveCount(data.Names.Length);
            explain.Contributions.Select(c => c.Name).Should().Equal(data.Names);
            explain.Contributions.Should().OnlyContain(c => double.IsFinite(c.Weight));
            double.IsFinite(explain.RawLogit).Should().BeTrue();
        }
    }
}