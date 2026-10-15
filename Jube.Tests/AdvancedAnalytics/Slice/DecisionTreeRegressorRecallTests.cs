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
    public class DecisionTreeRegressorRecallTests
    {
        private static (double[][] Inputs, double[] Outputs, string[] Names) StepData(int n, int seed,
            double noiseStandardDeviation = 0.3, int constantColumns = 0)
        {
            var random = new Random(seed);
            var columns = 3 + constantColumns;
            var inputs = new double[n][];
            var outputs = new double[n];

            for (var i = 0; i < n; i++)
            {
                var row = new double[columns];
                row[0] = SliceData.Gaussian(random);
                row[1] = SliceData.Gaussian(random);
                row[2] = SliceData.Gaussian(random);

                for (var j = 3; j < columns; j++)
                {
                    row[j] = 7d;
                }

                outputs[i] = (row[0] > 0d ? 5d : -5d) + noiseStandardDeviation * SliceData.Gaussian(random);
                inputs[i] = row;
            }

            var names = Enumerable.Range(0, columns).Select(j => "x" + j).ToArray();

            return (inputs, outputs, names);
        }

        [Fact]
        [Trait("Covers", "RecallDecisionTreeRegressor")]
        public void PredictReturnsAHighValueForAClearlyPositiveRowAndLowForAClearlyNegativeRow()
        {
            var data = StepData(900, 11);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 5 });

            var recall = DecisionTreeRegressorRecall.FromSnapshot(result.SnapshotJson);

            var positiveProbe = new[] { 8d, 0d, 0d };
            var negativeProbe = new[] { -8d, 0d, 0d };

            recall.Predict(positiveProbe).Should().HaveCount(1);
            recall.Predict(positiveProbe)[0].Should().BeGreaterThan(3d);
            recall.Predict(negativeProbe)[0].Should().BeLessThan(-3d);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var data = StepData(500, 42);
            var options = new DecisionTreeRegressorOptions { Seed = 9 };

            var first = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, options);
            var second = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = DecisionTreeRegressorRecall.FromSnapshot(first.SnapshotJson);
            var recallB = DecisionTreeRegressorRecall.FromSnapshot(second.SnapshotJson);

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
            var data = StepData(500, 77);

            var a = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 1 });
            var b = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var data = StepData(400, 5);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 1 });

            var recall = DecisionTreeRegressorRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 3*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedianAndStillProducesAFiniteValue()
        {
            var data = StepData(400, 6);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 1 });

            var recall = DecisionTreeRegressorRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 0d, 0d };
            var value = recall.Predict(withMissing)[0];

            double.IsFinite(value).Should().BeTrue();
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var data = StepData(400, 13);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 4 });

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsDecisionTreeRegressorPipelineSnapshot>(
                    result.SnapshotJson, DecisionTreeRegressorRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("DecisionTreeRegressorPipeline");
            envelope.Payload.IsLeaf.Length.Should().BeGreaterThan(0);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var data = StepData(400, 8);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 1 });

            Action wrongKind = () => DecisionTreeRegressorRecall.FromSnapshot(
                result.SnapshotJson.Replace("DecisionTreeRegressorPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["isLeaf"]!).RemoveAt(0);
            Action shortArrays = () => DecisionTreeRegressorRecall.FromSnapshot(tampered.ToString());
            shortArrays.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => DecisionTreeRegressorRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleMetricsAndImportancesThatSumToOne()
        {
            var data = StepData(900, 19);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2 });

            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RSquared.Should().NotBeNull();
            result.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.7);
            result.Details.Importances.Should().HaveCount(3);
            result.Details.Importances.Sum(i => i.Importance).Should().BeApproximately(1d, 1e-9);
            result.Details.Importances.Should().Contain(i => i.Name == "x0" && i.Importance > 0.5);
            result.Warnings.Should().NotContain(w => w.Code == DecisionTreeRegressorWarningCodes.RootIsLeaf);
        }

        [Fact]
        public void MaxDepthOneProducesAtMostOneSplit()
        {
            var data = StepData(900, 31);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 1, MaxDepth = 1 });

            result.Details.TreeDepth.Should().BeLessOrEqualTo(1);
            result.Details.Nodes.Should().BeLessOrEqualTo(3);
        }

        [Fact]
        public void AConstantColumnIsExcludedFromTheFitAndWarnedAbout()
        {
            var data = StepData(500, 51, constantColumns: 1);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 1 });

            result.Warnings.Should().Contain(w => w.Code == DecisionTreeRegressorWarningCodes.ConstantColumn);
            result.Preprocessing.Single(p => p.Name == "x3").Excluded.Should().BeTrue();
            result.Details.Importances.Single(i => i.Name == "x3").Importance.Should().Be(0d);
        }

        [Fact]
        public void ExplainsRootValueMatchesTheEvidenceAndContributionsMoveTowardTheLeaf()
        {
            var data = StepData(900, 61);

            var result = DecisionTreeRegressorTrainer.Train(data.Inputs, data.Outputs, data.Names,
                new DecisionTreeRegressorOptions { Seed = 1 });

            var recall = DecisionTreeRegressorRecall.FromSnapshot(result.SnapshotJson);

            var explain = recall.Explain([8d, 0d, 0d]);

            explain.RootValue.Should().Be(result.Details.RootValue);
            explain.Contributions.Should().NotBeEmpty();
            explain.LeafValue.Should().Be(recall.Predict([8d, 0d, 0d])[0]);
        }
    }
}