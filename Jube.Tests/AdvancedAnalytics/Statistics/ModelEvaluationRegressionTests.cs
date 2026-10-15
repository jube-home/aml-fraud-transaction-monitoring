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
    public class ModelEvaluationRegressionTests
    {
        private static readonly string[] names = ModelEvaluationData.Names;
        private readonly RegressionEvaluationTools tools = new();

        [Fact]
        [Trait("Covers", "EvaluateLinearRegression")]
        public void LinearRegressionRecoversTwoXPlusOneExactly()
        {
            var (x, t) = ModelEvaluationData.Linear();
            var result = tools.EvaluateLinearRegression(x, t, names);
            result.Test!.RSquared.Should().BeGreaterThan(0.999999);
            result.Test.MeanAbsoluteError.Should().BeLessThan(1e-6);
            result.Coefficients.Single(c => c.Name == "a").Value.Should().BeApproximately(2d, 1e-6);
            result.Coefficients.Single(c => c.Name == "Intercept").Value.Should().BeApproximately(1d, 1e-6);
            var lasso = tools.EvaluateLinearRegression(x, t, names, LinearPenalty.Lasso, [0.05]);
            lasso.Coefficients.Single(c => c.Name == "c").Value.Should().Be(0d);
            var ridge = tools.EvaluateLinearRegression(x, t, names, LinearPenalty.Ridge);
            ridge.Sweep.Should().HaveCount(3);

            ModelEvaluationData.Json(tools.EvaluateLinearRegression(x, t, names)).Should()
                .Be(ModelEvaluationData.Json(result));
        }

        [Fact]
        [Trait("Covers", "EvaluateLinearRegression")]
        public void RidgeHandlesCollinearPredictorsAndConstantTargetsAreRejected()
        {
            var (x, t) = ModelEvaluationData.Linear(100);
            var collinear = x.Select(r => new[] { r[0], r[1], r[0] * 2 }).ToArray();

            tools.EvaluateLinearRegression(collinear, t, names, LinearPenalty.Ridge).Test!.RSquared.Should()
                .BeGreaterThan(0.99);

            tools.Invoking(v => v.EvaluateLinearRegression(x, [.. t.Select(_ => 1d)], names)).Should()
                .Throw<ArgumentException>().Where(e => e.ParamName == "targets");
        }

        [Fact]
        [Trait("Covers", "EvaluateTreeRegression")]
        public void TreeRegressionLearnsAStepFunction()
        {
            var (x, _) = ModelEvaluationData.Linear();
            var t = x.Select(r => r[0] > 5 ? 10d : 0d).ToArray();

            foreach (var algorithm in new[]
                     {
                         RegressionFamily.DecisionTree, RegressionFamily.RandomForest, RegressionFamily.GradientBoosting
                     })
            {
                var result = tools.EvaluateTreeRegression(x, t, names, algorithm, 30);
                result.Test!.RSquared.Should().BeGreaterThan(0.95);
            }

            var tree = tools.EvaluateTreeRegression(x, t, names, RegressionFamily.DecisionTree, maxDepth: 2);
            tree.Model.TreeText.Should().Contain("a <=");

            tools.Invoking(v => v.EvaluateTreeRegression(x, t, names, RegressionFamily.LinearRegression)).Should()
                .Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "EvaluateNeighbourAndKernelRegression")]
        public void NeighbourAndKernelRegressionFitASmoothCurve()
        {
            var (x, _) = ModelEvaluationData.Linear(300);
            var t = x.Select(r => r[0] * r[0]).ToArray();
            var knn = tools.EvaluateNeighbourAndKernelRegression(x, t, names);
            knn.Sweep.Should().HaveCount(5);
            knn.Test!.RSquared.Should().BeGreaterThan(0.95);
            var kernel = tools.EvaluateNeighbourAndKernelRegression(x, t, names, RegressionFamily.KernelRidge);
            kernel.Test!.RSquared.Should().BeGreaterThan(0.95);
        }

        [Fact]
        [Trait("Covers", "EvaluateNeuralNetworkRegression")]
        public void NeuralRegressionIsSeededAndFitsALine()
        {
            var (x, t) = ModelEvaluationData.Linear(300);
            var result = tools.EvaluateNeuralNetworkRegression(x, t, names);
            result.Test!.RSquared.Should().BeGreaterThan(0.95);

            ModelEvaluationData.Json(tools.EvaluateNeuralNetworkRegression(x, t, names)).Should()
                .Be(ModelEvaluationData.Json(result));
        }

        [Fact]
        [Trait("Covers", "CompareRegressors")]
        public void RegressorComparisonRanksLinearFirstOnLinearData()
        {
            var (x, t) = ModelEvaluationData.Linear(300);

            var result = tools.CompareRegressors(x, t, names,
            [
                RegressionFamily.LinearRegression, RegressionFamily.DecisionTree,
                RegressionFamily.KNearestNeighbours
            ], folds: 3);
            result.Ranking.Should().HaveCount(3);
            result.Ranking[0].Model.Should().Be("LinearRegression");
            result.Recommended.Should().Be("LinearRegression");

            ModelEvaluationData.Json(tools.CompareRegressors(x, t, names,
                [
                    RegressionFamily.LinearRegression, RegressionFamily.DecisionTree,
                    RegressionFamily.KNearestNeighbours
                ], folds: 3))
                .Should().Be(ModelEvaluationData.Json(result));
        }

        [Fact]
        [Trait("Covers", "ComputeRegressionPermutationImportance")]
        public void RegressionImportanceFindsTheSignalVariable()
        {
            var (x, t) = ModelEvaluationData.Linear(300);
            var result = tools.ComputeRegressionPermutationImportance(x, t, names, RegressionFamily.LinearRegression);
            result.Importance[0].Variable.Should().Be("a");
            result.NegligibleVariables.Should().Contain("c");

            ModelEvaluationData
                .Json(tools.ComputeRegressionPermutationImportance(x, t, names, RegressionFamily.LinearRegression))
                .Should().Be(ModelEvaluationData.Json(result));
        }

        [Fact]
        [Trait("Covers", "ComputeRegressionPartialDependence")]
        public void RegressionPartialDependenceIsIncreasingForALine()
        {
            var (x, t) = ModelEvaluationData.Linear(300);
            var result = tools.ComputeRegressionPartialDependence(x, t, names, "a", RegressionFamily.LinearRegression);
            result.Shape.Should().StartWith("Increasing");
            result.Points[^1].Mean.Should().BeGreaterThan(result.Points[0].Mean);

            tools.ComputeRegressionPartialDependence(x, t, names, "c", RegressionFamily.LinearRegression).Shape.Should()
                .StartWith("Flat");

            tools.Invoking(v => v.ComputeRegressionPartialDependence(x, t, names, "zzz")).Should()
                .Throw<ArgumentException>().Where(e => e.ParamName == "variable");
        }
    }
}