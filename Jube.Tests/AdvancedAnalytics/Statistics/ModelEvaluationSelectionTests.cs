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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class ModelEvaluationSelectionTests
    {
        private static readonly string[] names = ModelEvaluationData.Names;
        private readonly ModelSelectionTools tools = new();

        [Fact]
        [Trait("Covers", "GridSearchHyperParametersAsync")]
        public async Task GridSearchPrefersDepthTwoForAnInteractionAndReportsNestedScoreAsync()
        {
            var (x, y) = ModelEvaluationData.Interaction();

            var result = await tools.GridSearchHyperParametersAsync(names, EvaluationFamily.DecisionTree,
                HyperParameter.MaxDepth, [1d, 2d, 4d], inputs: x, labels: y, nested: true);
            result.Trials.Should().HaveCount(3);
            result.Trials[0].Best.Should().BeTrue();
            result.BestParameters.Single().Value.Should().NotBe("1");
            result.NestedScore.Should().NotBeNull();
            result.Nested.Should().HaveCount(3);

            ModelEvaluationData.Json(await tools.GridSearchHyperParametersAsync(names, EvaluationFamily.DecisionTree,
                    HyperParameter.MaxDepth, [1d, 2d, 4d], inputs: x, labels: y, nested: true))
                .Should().Be(ModelEvaluationData.Json(result));

            await tools.Awaiting(t => t.GridSearchHyperParametersAsync(names, EvaluationFamily.DecisionTree,
                    HyperParameter.Complexity, [1d], inputs: x, labels: y)).Should()
                .ThrowAsync<ArgumentException>();

            await tools.Awaiting(t => t.GridSearchHyperParametersAsync(names, EvaluationFamily.DecisionTree,
                    HyperParameter.MaxDepth,
                    [.. Enumerable.Range(1, 9).Select(v => (double)v)], inputs: x, labels: y,
                    parameterB: HyperParameter.MinLeafSize,
                    valuesB: [.. Enumerable.Range(1, 9).Select(v => (double)v)]))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "RandomSearchHyperParametersAsync")]
        public async Task RandomSearchIsSeededAndBoundedAsync()
        {
            var (x, y) = ModelEvaluationData.Separable(300);

            var call = () => tools.RandomSearchHyperParametersAsync(names, EvaluationFamily.GradientBoosting,
                [HyperParameter.LearningRate, HyperParameter.MaxDepth], [0.01, 1d], [0.5, 4d],
                inputs: x, labels: y, logScale: [true, false], trials: 5, folds: 3);

            var first = await call();
            first.Trials.Should().HaveCount(5);
            ModelEvaluationData.Json(await call()).Should().Be(ModelEvaluationData.Json(first));

            await tools.Awaiting(t => t.RandomSearchHyperParametersAsync(names, EvaluationFamily.GradientBoosting,
                    [HyperParameter.LearningRate], [0d], [1d], inputs: x, labels: y,
                    logScale: [true])).Should()
                .ThrowAsync<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeLearningCurveAsync")]
        public async Task LearningCurveGrowsTheTrainingRowsAsync()
        {
            var (x, y) = ModelEvaluationData.Interaction();

            var result = await tools.ComputeLearningCurveAsync(names, inputs: x, labels: y,
                family: EvaluationFamily.DecisionTree, trainingFractions: [0.25, 0.5, 1d], folds: 3);
            result.Points.Should().HaveCount(3);
            result.Points.Select(p => p.TrainRows).Should().BeInAscendingOrder();
            result.Diagnosis.Should().NotBeEmpty();

            await tools.Awaiting(t => t.ComputeLearningCurveAsync(names, inputs: x, labels: y,
                    trainingFractions: [0.5])).Should()
                .ThrowAsync<ArgumentException>().Where(e => e.ParamName == "trainingFractions");
        }

        [Fact]
        [Trait("Covers", "ComputeValidationCurveAsync")]
        public async Task ValidationCurveShowsDepthOneUnderfitsAnInteractionAsync()
        {
            var (x, y) = ModelEvaluationData.Interaction();

            var result = await tools.ComputeValidationCurveAsync(names, EvaluationFamily.DecisionTree,
                HyperParameter.MaxDepth, [1d, 2d, 4d], inputs: x, labels: y);
            result.Points.Should().HaveCount(3);
            result.Points[0].ValidationScore.Should().BeLessThan(result.Points[1].ValidationScore!.Value);
        }

        [Fact]
        [Trait("Covers", "ComputePermutationImportanceAsync")]
        public async Task PermutationImportanceFlagsTheNoiseVariableAsync()
        {
            var (x, y) = ModelEvaluationData.Separable();

            var result = await tools.ComputePermutationImportanceAsync(names, inputs: x, labels: y,
                family: EvaluationFamily.RandomForest, fixedParameters: [HyperParameter.TreeCount],
                fixedValues: [30d]);

            result.Importance[0].Variable.Should().Be("a");
            result.NegligibleVariables.Should().Contain("c");

            ModelEvaluationData.Json(await tools.ComputePermutationImportanceAsync(names, inputs: x, labels: y,
                    family: EvaluationFamily.RandomForest, fixedParameters: [HyperParameter.TreeCount],
                    fixedValues: [30d]))
                .Should().Be(ModelEvaluationData.Json(result));
        }

        [Fact]
        [Trait("Covers", "ComputePartialDependenceAsync")]
        public async Task PartialDependenceIsIncreasingForTheSignalAndFlatForNoiseAsync()
        {
            var (x, y) = ModelEvaluationData.Separable();

            (await tools.ComputePartialDependenceAsync(names, "a", inputs: x, labels: y)).Shape.Should()
                .StartWith("Increasing");

            (await tools.ComputePartialDependenceAsync(names, "c", inputs: x, labels: y)).Range.Should()
                .BeLessThan(0.1);
        }

        [Fact]
        [Trait("Covers", "CompareCalibrationMethods")]
        public void CalibrationComparisonListsFourMethodsAndPlattBeatsRawNaiveBayes()
        {
            var (x, y) = ModelEvaluationData.Separable();
            var result = tools.CompareCalibrationMethods(x, y, names, EvaluationFamily.NaiveBayes);
            result.Methods.Select(m => m.Method).Should().Contain(["Platt", "Isotonic", "ConstantBaseRate"]);
            result.Methods.Single(m => m.Method == "Platt").LogLoss.Should().NotBeNull();

            result.Methods.Single(m => m.Method == "ConstantBaseRate").LogLoss!.Value.Should()
                .BeGreaterThan(result.Methods[0].LogLoss!.Value);

            result.BestMethod.Should().NotBe("ConstantBaseRate");
        }

        [Fact]
        [Trait("Covers", "CompareScoreCalibrations")]
        public void ScoreCalibrationCorrectsOverconfidentScores()
        {
            var random = new Random(2);
            var z = Enumerable.Range(0, 1000).Select(_ => random.NextDouble() * 6 - 3).ToArray();
            var labels = z.Select(v => random.NextDouble() < 1d / (1d + Math.Exp(-v)) ? 1 : 0).ToArray();
            var scores = z.Select(v => 1d / (1d + Math.Exp(-3 * v))).ToArray();
            var result = tools.CompareScoreCalibrations(scores, labels);

            result.Methods.Single(m => m.Method == "Platt").LogLoss!.Value.Should()
                .BeLessThan(result.Methods[0].LogLoss!.Value);

            tools.Invoking(t => t.CompareScoreCalibrations([.. new double[40].Select(_ => 2d)], new int[40]))
                .Should().Throw<ArgumentException>();

            ModelEvaluationData.Json(tools.CompareScoreCalibrations(scores, labels)).Should()
                .Be(ModelEvaluationData.Json(result));
        }

        [Fact]
        [Trait("Covers", "CompareModelsAsync")]
        public async Task ModelComparisonUsesIdenticalFoldsAndRecommendsAnEquivalentModelAsync()
        {
            var (x, y) = ModelEvaluationData.Separable(300);

            var families = new[]
            {
                EvaluationFamily.LogisticRegression, EvaluationFamily.DecisionTree, EvaluationFamily.NaiveBayes,
                EvaluationFamily.RandomForest
            };

            var result = await tools.CompareModelsAsync(names, inputs: x, labels: y, families: families, folds: 3);
            result.Ranking.Select(r => r.Rank).Should().Equal(1, 2, 3, 4);
            result.Ranking.Single(r => r.Model == result.Recommended).IndistinguishableFromBest.Should().BeTrue();
            result.Recommendation.Should().NotBeEmpty();

            ModelEvaluationData.Json(
                    await tools.CompareModelsAsync(names, inputs: x, labels: y, families: families, folds: 3))
                .Should().Be(ModelEvaluationData.Json(result));

            await tools.Awaiting(t =>
                    t.CompareModelsAsync(names, inputs: x, labels: y,
                        families: [.. Enumerable.Repeat(EvaluationFamily.DecisionTree, 1)],
                        folds: 1)).Should()
                .ThrowAsync<ArgumentException>().Where(e => e.ParamName == "folds");
        }
    }
}