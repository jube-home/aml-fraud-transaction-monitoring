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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class ModelEvaluationToolsTests
    {
        private readonly ModelEvaluationTools tools = new();

        [Fact]
        [Trait("Covers", "EvaluateDecisionTree")]
        public void DecisionTreeRecoversTheKnownInteractionForEveryAlgorithm()
        {
            var (x, y) = ModelEvaluationData.Interaction();

            foreach (var algorithm in new[] { TreeAlgorithm.C45, TreeAlgorithm.Id3, TreeAlgorithm.Cart })
            {
                var result = tools.EvaluateDecisionTree(x, y, ModelEvaluationData.Names, algorithm);
                result.Test!.RocAuc.Should().BeGreaterThan(0.98);
                result.Model.IntrinsicImportance.Take(2).Select(i => i.Variable).Should().BeEquivalentTo("a", "b");
                result.Model.IntrinsicImportance.Single(i => i.Variable == "c").Importance.Should().BeLessThan(0.05);
                result.Model.TreeText.Should().Contain("a");
                result.Model.Rules.Should().NotBeEmpty();
            }
        }

        [Fact]
        [Trait("Covers", "EvaluateDecisionTree")]
        public void DecisionTreeIsDeterministicAndPruningNeverGrowsIt()
        {
            var (x, y) = ModelEvaluationData.Interaction();

            var first = tools.EvaluateDecisionTree(x, y, ModelEvaluationData.Names, TreeAlgorithm.Cart, 12,
                1);
            var again = tools.EvaluateDecisionTree(x, y, ModelEvaluationData.Names, TreeAlgorithm.Cart, 12,
                1);

            ModelEvaluationData.Json(again).Should().Be(ModelEvaluationData.Json(first));

            var pruned = tools.EvaluateDecisionTree(x, y, ModelEvaluationData.Names, TreeAlgorithm.Cart, 12,
                1, TreePruning.ReducedError);

            pruned.Model.Facts.Single(f => f.Name == "Leaves").Value!.Value.Should()
                .BeLessThanOrEqualTo(first.Model.Facts.Single(f => f.Name == "Leaves").Value!.Value);
        }

        [Fact]
        [Trait("Covers", "EvaluateDecisionTree")]
        public void ClassificationToolsRejectBadInput()
        {
            var (x, y) = ModelEvaluationData.Interaction(100);
            var bad = (int[])y.Clone();
            bad[0] = 2;

            tools.Invoking(t => t.EvaluateDecisionTree(x, bad, ModelEvaluationData.Names)).Should()
                .Throw<ArgumentException>().Where(e => e.ParamName == "labels");

            tools.Invoking(t => t.EvaluateDecisionTree(x, y, ModelEvaluationData.Names, maxDepth: 0)).Should()
                .Throw<ArgumentException>().Where(e => e.ParamName == "maxDepth");

            tools.Invoking(t => t.EvaluateDecisionTree(x, y, ["a", "a", "c"])).Should()
                .Throw<ArgumentException>().Where(e => e.ParamName == "variableNames");

            tools.Invoking(t => t.EvaluateDecisionTree(x, y, ModelEvaluationData.Names, crossValidationFolds: 1))
                .Should()
                .Throw<ArgumentException>().Where(e => e.ParamName == "crossValidationFolds");

            var many = Enumerable.Range(0, 20001).Select(_ => new double[3]).ToArray();

            tools.Invoking(t => t.EvaluateDecisionTree(many, new int[20001], ModelEvaluationData.Names)).Should()
                .Throw<ArgumentException>().Where(e => e.ParamName == "inputs");

            tools.Invoking(t =>
                    t.EvaluateKNearestNeighbours(x, y, ModelEvaluationData.Names, [.. Enumerable.Range(1, 17)]))
                .Should().Throw<ArgumentException>().Where(e => e.ParamName == "neighbourCounts");

            tools.Invoking(t =>
                    t.EvaluateNeuralNetworkClassifier(x, y, ModelEvaluationData.Names, [4, 4, 4, 4]))
                .Should().Throw<ArgumentException>().Where(e => e.ParamName == "hiddenLayers");
        }

        [Fact]
        [Trait("Covers", "EvaluateDecisionTree")]
        public void SmallSamplesAndLeakedPredictorsAreWarnedAbout()
        {
            var (x, y) = ModelEvaluationData.Interaction(60);

            tools.EvaluateDecisionTree(x, y, ModelEvaluationData.Names).Warnings.Select(w => w.Code)
                .Should().Contain("TinyTestSet");

            var (x2, y2) = ModelEvaluationData.Interaction(400);
            var leaked = x2.Select((r, i) => new[] { r[0], r[1], y2[i] * 1.0 }).ToArray();

            tools.EvaluateDecisionTree(leaked, y2, ModelEvaluationData.Names).Warnings.Select(w => w.Code)
                .Should().Contain("SuspiciouslyPerfectScore");

            var withMissing = x2.Select(r => (double[])r.Clone()).ToArray();
            withMissing[3][2] = double.NaN;
            withMissing[4][1] = double.NaN;

            tools.EvaluateDecisionTree(withMissing, y2, ModelEvaluationData.Names).Warnings.Select(w => w.Code)
                .Should().Contain("MissingValuesImputed");
        }

        [Fact]
        [Trait("Covers", "EvaluateDecisionTree")]
        public void SevereImbalanceWarnsUnlessHandled()
        {
            var random = new Random(4);

            var x = Enumerable.Range(0, 1200)
                .Select(_ => new[] { random.NextDouble(), random.NextDouble(), random.NextDouble() }).ToArray();

            var y = new int[1200];

            for (var i = 0; i < 10; i++)
            {
                y[i * 100] = 1;
                x[i * 100][0] += 2;
            }

            var plain = tools.EvaluateRandomForest(x, y, ModelEvaluationData.Names, treeCount: 20,
                permutationRepeats: 0);

            plain.Warnings.Select(w => w.Code).Should().Contain("SevereImbalanceWithoutHandling");

            var handled = tools.EvaluateRandomForest(x, y, ModelEvaluationData.Names, treeCount: 20,
                classImbalance: ClassImbalanceHandling.BalancedWeights, permutationRepeats: 0);

            handled.Warnings.Select(w => w.Code).Should().NotContain("SevereImbalanceWithoutHandling");
        }

        [Fact]
        [Trait("Covers", "EvaluateRandomForest")]
        public void RandomForestAndBaggingAreAccurateAndDeterministic()
        {
            var (x, y) = ModelEvaluationData.Separable();

            foreach (var mode in new[] { ForestMode.RandomForest, ForestMode.Bagging })
            {
                var first = tools.EvaluateRandomForest(x, y, ModelEvaluationData.Names, mode, 40);
                var again = tools.EvaluateRandomForest(x, y, ModelEvaluationData.Names, mode, 40);
                first.Test!.RocAuc.Should().BeGreaterThan(0.99);
                first.Model.Facts.Should().Contain(f => f.Name == "OutOfBagRocAuc");
                first.Importance[0].Variable.Should().Be("a");
                ModelEvaluationData.Json(again).Should().Be(ModelEvaluationData.Json(first));
            }
        }

        [Fact]
        [Trait("Covers", "EvaluateBoostedTrees")]
        public void BothBoostingAlgorithmsSeparateTheClasses()
        {
            var (x, y) = ModelEvaluationData.Separable();

            foreach (var algorithm in new[] { BoostingAlgorithm.AdaBoost, BoostingAlgorithm.GradientBoosting })
            {
                var result = tools.EvaluateBoostedTrees(x, y, ModelEvaluationData.Names, algorithm, 30);
                result.Test!.RocAuc.Should().BeGreaterThan(0.99);

                ModelEvaluationData
                    .Json(tools.EvaluateBoostedTrees(x, y, ModelEvaluationData.Names, algorithm, 30))
                    .Should().Be(ModelEvaluationData.Json(result));
            }
        }

        [Fact]
        [Trait("Covers", "EvaluateNaiveBayes")]
        public void NaiveBayesKindsRankTheClasses()
        {
            var (x, y) = ModelEvaluationData.Separable();

            foreach (var kind in new[]
                         { NaiveBayesKind.Gaussian, NaiveBayesKind.Categorical, NaiveBayesKind.TreeAugmented })
            {
                var result = tools.EvaluateNaiveBayes(x, y, ModelEvaluationData.Names, kind);
                result.Test!.RocAuc.Should().BeGreaterThan(0.98);

                ModelEvaluationData.Json(tools.EvaluateNaiveBayes(x, y, ModelEvaluationData.Names, kind))
                    .Should().Be(ModelEvaluationData.Json(result));
            }
        }

        [Fact]
        [Trait("Covers", "EvaluateKNearestNeighbours")]
        public void NearestNeighboursReportTheKSweepWithOneSelection()
        {
            var (x, y) = ModelEvaluationData.Separable();
            var result = tools.EvaluateKNearestNeighbours(x, y, ModelEvaluationData.Names);
            result.Sweep.Should().HaveCount(7);
            result.Sweep.Count(s => s.Selected).Should().Be(1);
            result.Test!.RocAuc.Should().BeGreaterThan(0.98);
        }

        [Fact]
        [Trait("Covers", "EvaluateSupportVectorMachine")]
        public void SupportVectorMachinesSweepComplexity()
        {
            var (x, y) = ModelEvaluationData.Separable(300);
            var linear = tools.EvaluateSupportVectorMachine(x, y, ModelEvaluationData.Names);
            linear.Sweep.Should().HaveCount(3);
            linear.Test!.RocAuc.Should().BeGreaterThan(0.98);
            var kernel = tools.EvaluateSupportVectorMachine(x, y, ModelEvaluationData.Names, SvmKernel.Gaussian);
            kernel.Test!.RocAuc.Should().BeGreaterThan(0.98);

            ModelEvaluationData.Json(tools.EvaluateSupportVectorMachine(x, y, ModelEvaluationData.Names)).Should()
                .Be(ModelEvaluationData.Json(linear));

            var big = Enumerable.Range(0, 4001).Select(_ => new double[3]).ToArray();

            tools.Invoking(t =>
                    t.EvaluateSupportVectorMachine(big, new int[4001], ModelEvaluationData.Names, SvmKernel.Gaussian))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "EvaluateNeuralNetworkClassifier")]
        public void NeuralNetworkClassifierIsSeededAndAccurate()
        {
            var (x, y) = ModelEvaluationData.Separable(300);
            var first = tools.EvaluateNeuralNetworkClassifier(x, y, ModelEvaluationData.Names);
            first.Test!.RocAuc.Should().BeGreaterThan(0.98);

            ModelEvaluationData.Json(tools.EvaluateNeuralNetworkClassifier(x, y, ModelEvaluationData.Names))
                .Should().Be(ModelEvaluationData.Json(first));
        }

        [Fact]
        [Trait("Covers", "CrossValidateLogisticRegression")]
        public void LogisticCrossValidationPoolsEveryRowOnce()
        {
            var (x, y) = ModelEvaluationData.Separable();
            var result = tools.CrossValidateLogisticRegression(x, y, ModelEvaluationData.Names);
            result.Folds.Should().HaveCount(5);
            result.Test!.Rows.Should().Be(x.Length);
            result.FoldSummary.Should().NotBeEmpty();
            result.Test.RocAuc.Should().BeGreaterThan(0.98);
        }

        [Fact]
        public void TestRowsNeverInfluenceTheFitAndValidationRowsOnlyTheCalibration()
        {
            var (x, y) = ModelEvaluationData.Interaction(400);
            var options = new EvaluationOptions(0.2, 0.2, 0, 1, true, null, 0);
            var split = BinaryEvaluationEngine.Plan(y, options)[0];

            foreach (var family in new[]
                     {
                         EvaluationFamily.DecisionTree, EvaluationFamily.RandomForest, EvaluationFamily.NaiveBayes,
                         EvaluationFamily.KNearestNeighbours, EvaluationFamily.GradientBoosting
                     })
            {
                var config = new LearnerConfig { Family = family, Algorithm = TreeAlgorithm.Cart, Trees = 15 };

                var baseline = BinaryEvaluationEngine.RunOne([config], ["x"], x, y,
                    ModelEvaluationData.Names, split, options, 1, default);

                var flipped = (int[])y.Clone();
                var garbled = x.Select(r => (double[])r.Clone()).ToArray();

                foreach (var row in split.Test)
                {
                    flipped[row] = 1 - flipped[row];
                    garbled[row][0] = 1e6;
                    garbled[row][1] = double.NaN;
                }

                var tampered = BinaryEvaluationEngine.RunOne([config], ["x"], garbled, flipped,
                    ModelEvaluationData.Names, split, options, 1, default);

                tampered.Calibration.Should().BeEquivalentTo(baseline.Calibration);
                tampered.TrainMetrics.Should().BeEquivalentTo(baseline.TrainMetrics);
                tampered.ValidationMetrics.Should().BeEquivalentTo(baseline.ValidationMetrics);
                tampered.Description.Should().BeEquivalentTo(baseline.Description);
                tampered.TestMetrics.Should().NotBeEquivalentTo(baseline.TestMetrics);

                var relabelled = (int[])y.Clone();

                foreach (var row in split.Validation)
                {
                    relabelled[row] = 1 - relabelled[row];
                }

                var changed = BinaryEvaluationEngine.RunOne([config], ["x"], x, relabelled,
                    ModelEvaluationData.Names, split, options, 1, default);

                var trainRows = split.Train.Take(50).Select(i => x[i]).ToArray();
                changed.PredictRaw(trainRows).Should().Equal(baseline.PredictRaw(trainRows));
                changed.Calibration.Should().NotBeEquivalentTo(baseline.Calibration);
            }
        }
    }
}