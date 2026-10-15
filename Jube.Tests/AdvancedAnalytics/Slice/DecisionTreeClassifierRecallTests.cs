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
    public class DecisionTreeClassifierRecallTests
    {
        private static SliceDataset ClearlySeparableData(int n, int seed)
        {
            return SliceData.Logistic(n, seed, 0d, [4.0, 0.05], 1);
        }

        [Fact]
        [Trait("Covers", "RecallDecisionTreeClassifier")]
        public void PredictReturnsAHighProbabilityForAClearlyPositiveRowAndLowForAClearlyNegativeRow()
        {
            var data = ClearlySeparableData(900, 11);

            var result = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 5 });

            var recall = DecisionTreeClassifierRecall.FromSnapshot(result.SnapshotJson);

            var positiveProbe = new[] { 8d, 0d, 0d };
            var negativeProbe = new[] { -8d, 0d, 0d };

            recall.Predict(positiveProbe).Should().HaveCount(1);
            recall.Predict(positiveProbe)[0].Should().BeGreaterThan(0.8);
            recall.Predict(negativeProbe)[0].Should().BeLessThan(0.2);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var data = ClearlySeparableData(500, 42);
            var options = new DecisionTreeClassifierOptions { Seed = 9 };

            var first = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, options);
            var second = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = DecisionTreeClassifierRecall.FromSnapshot(first.SnapshotJson);
            var recallB = DecisionTreeClassifierRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in data.Inputs.Take(50))
            {
                var a = recallA.Predict(row);
                var b = recallB.Predict(row);
                SliceData.Bits(a[0]).Should().Be(SliceData.Bits(b[0]));
            }
        }

        [Fact]
        public void ADifferentSeedCanProduceADifferentSnapshot()
        {
            var data = ClearlySeparableData(500, 77);

            var a = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 1 });
            var b = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = ClearlySeparableData(400, 5);

            var result = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 1 });

            var recall = DecisionTreeClassifierRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 3*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedianAndStillProducesAFiniteProbability()
        {
            var data = ClearlySeparableData(400, 6);

            var result = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 1 });

            var recall = DecisionTreeClassifierRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 0d, 0d };
            var probability = recall.Predict(withMissing)[0];

            double.IsFinite(probability).Should().BeTrue();
            probability.Should().BeInRange(0d, 1d);
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = ClearlySeparableData(400, 13);

            var result = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 4 });

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsDecisionTreeClassifierPipelineSnapshot>(
                    result.SnapshotJson, DecisionTreeClassifierRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("DecisionTreeClassifierPipeline");
            envelope.Payload.IsLeaf.Length.Should().BeGreaterThan(0);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var data = ClearlySeparableData(400, 8);

            var result = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 1 });

            Action wrongKind = () => DecisionTreeClassifierRecall.FromSnapshot(
                result.SnapshotJson.Replace("DecisionTreeClassifierPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["isLeaf"]!).RemoveAt(0);
            Action shortArrays = () => DecisionTreeClassifierRecall.FromSnapshot(tampered.ToString());
            shortArrays.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => DecisionTreeClassifierRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleMetricsAndImportancesThatSumToOne()
        {
            var data = ClearlySeparableData(900, 19);

            var result = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2 });

            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.7);
            result.Details.Importances.Should().HaveCount(3);
            result.Details.Importances.Sum(i => i.Importance).Should().BeApproximately(1d, 1e-9);
            result.Details.Importances.Should().Contain(i => i.Name == "x0" && i.Importance > 0.5);
            result.Warnings.Should().NotContain(w => w.Code == DecisionTreeWarningCodes.RootIsLeaf);
        }

        [Fact]
        public void CalibrateFalseLeavesTheModelUncalibratedAndCalibrateTrueCalibratesIt()
        {
            var data = ClearlySeparableData(900, 23);

            var uncalibrated = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 1, Calibrate = false });

            var calibrated = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 1, Calibrate = true });

            uncalibrated.Details.Calibrated.Should().BeFalse();
            calibrated.Details.Calibrated.Should().BeTrue();

            var uncalibratedRecall = DecisionTreeClassifierRecall.FromSnapshot(uncalibrated.SnapshotJson);
            var calibratedRecall = DecisionTreeClassifierRecall.FromSnapshot(calibrated.SnapshotJson);

            uncalibratedRecall.Calibrated.Should().BeFalse();
            calibratedRecall.Calibrated.Should().BeTrue();
        }

        [Fact]
        public void MaxDepthOneProducesAtMostOneSplit()
        {
            var data = ClearlySeparableData(900, 31);

            var result = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 1, MaxDepth = 1 });

            result.Details.TreeDepth.Should().BeLessOrEqualTo(1);
            result.Details.Nodes.Should().BeLessOrEqualTo(3);
        }

        [Fact]
        public void AConstantColumnIsExcludedFromTheFitAndWarnedAbout()
        {
            var random = new Random(51);
            var n = 500;
            var inputs = new double[n][];
            var labels = new int[n];

            for (var i = 0; i < n; i++)
            {
                var x0 = SliceData.Gaussian(random);
                inputs[i] = [x0, 7d];
                labels[i] = random.NextDouble() < SliceData.Sigmoid(3d * x0) ? 1 : 0;
            }

            var result = DecisionTreeClassifierTrainer.Train(inputs, labels, ["x0", "constant"],
                new DecisionTreeClassifierOptions { Seed = 1 });

            result.Warnings.Should().Contain(w => w.Code == DecisionTreeWarningCodes.ConstantColumn);
            result.Preprocessing.Single(p => p.Name == "constant").Excluded.Should().BeTrue();
            result.Details.Importances.Single(i => i.Name == "constant").Importance.Should().Be(0d);
        }

        [Fact]
        public void BothClassesMustBePresentInTheTrainingSplit()
        {
            var inputs = SliceData.Repeat(50, _ => new[] { 1d, 2d }).ToArray();
            var labels = Enumerable.Repeat(0, 50).ToArray();

            Action act = () => DecisionTreeClassifierTrainer.Train(inputs, labels, ["x0", "x1"],
                new DecisionTreeClassifierOptions { Seed = 1, ValidationFraction = 0, TestFraction = 0 });

            act.Should().Throw<ArgumentException>().WithMessage("*both classes*");
        }

        [Fact]
        public void ExplainsRootProbabilityMatchesTheEvidenceAndContributionsMoveTowardTheLeaf()
        {
            var data = ClearlySeparableData(900, 61);

            var result = DecisionTreeClassifierTrainer.Train(data.Inputs, data.Labels, data.Names,
                new DecisionTreeClassifierOptions { Seed = 1, Calibrate = false });

            var recall = DecisionTreeClassifierRecall.FromSnapshot(result.SnapshotJson);

            var explain = recall.Explain([8d, 0d, 0d]);

            explain.RootProbability.Should().Be(result.Details.RootProbability);
            explain.Contributions.Should().NotBeEmpty();
            explain.LeafProbability.Should().Be(recall.Predict([8d, 0d, 0d])[0]);
        }
    }
}