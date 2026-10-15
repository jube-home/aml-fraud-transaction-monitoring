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
using FluentAssertions;
using Jube.Analytics;
using Xunit;

namespace Jube.Test.Analytics
{
    public class RegressionAndThresholdTests
    {
        private static readonly double[] actual = [3, -0.5, 2, 7, 0, 4];
        private static readonly double[] predicted = [2.5, 0.0, 2, 8, 0.3, 3];
        private static readonly double[] training = [1, 2, 4, 7, 11, 16, 22];

        [Fact]
        public void Regression_KnownAnswers()
        {
            RegressionMetrics.MeanAbsoluteError(actual, predicted).Should().BeApproximately(0.5499999999999999, 1e-12);
            RegressionMetrics.RootMeanSquaredError(actual, predicted).Should()
                .BeApproximately(0.6570134448142341, 1e-12);
            RegressionMetrics.MeanAbsolutePercentageError(actual, predicted)
                .Should().BeApproximately(0.3119047619047619, 1e-12);
            RegressionMetrics.SymmetricMeanAbsolutePercentageError(actual, predicted)
                .Should().BeApproximately(0.7668109668109668, 1e-12);

            RegressionMetrics.MeanAbsoluteScaledError(actual, predicted, training)
                .Should().BeApproximately(0.1571428571428571, 1e-12);

            RegressionMetrics.MeanAbsoluteScaledError(actual, predicted, training, 2)
                .Should().BeApproximately(0.07857142857142856, 1e-12);

            RegressionMetrics.RSquared(actual, predicted).Should().BeApproximately(0.9322137404580153, 1e-12);
        }

        [Fact]
        public void Regression_PerfectAndNaNAndZeroCases()
        {
            RegressionMetrics.MeanAbsoluteError(actual, actual).Should().Be(0);
            RegressionMetrics.RSquared(actual, actual).Should().Be(1);
            RegressionMetrics.SymmetricMeanAbsolutePercentageError([0, 0], [0, 0]).Should().Be(0);
            double.IsNaN(RegressionMetrics.MeanAbsolutePercentageError([0, 0], [1, 2])).Should().BeTrue();
            double.IsNaN(RegressionMetrics.RSquared([2, 2, 2], [1, 2, 3])).Should().BeTrue();
            double.IsNaN(RegressionMetrics.MeanAbsoluteScaledError([1, 2], [1, 2], [5, 5, 5])).Should().BeTrue();
            RegressionMetrics.RSquared([1, 2, 3, 4], [4, 3, 2, 1]).Should().BeLessThan(0);
        }

        [Fact]
        public void Regression_NaNPairsAreDropped()
        {
            var a = new[] { 3, -0.5, 2, 7, 0, 4, double.NaN, 1 };
            var p = new[] { 2.5, 0.0, 2, 8, 0.3, 3, 5, double.NaN };
            RegressionMetrics.MeanAbsoluteError(a, p).Should().BeApproximately(0.5499999999999999, 1e-12);
        }

        [Fact]
        public void Regression_InvalidInputsThrow()
        {
            Action act = () => RegressionMetrics.MeanAbsoluteError([1, 2], [1]);
            act.Should().Throw<ArgumentException>();
            act = () => RegressionMetrics.RootMeanSquaredError([], []);
            act.Should().Throw<ArgumentException>();
            act = () => RegressionMetrics.RSquared(null!, [1]);
            act.Should().Throw<ArgumentNullException>();
            act = () => RegressionMetrics.MeanAbsoluteScaledError([1], [1], [1, 2], 0);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => RegressionMetrics.MeanAbsoluteScaledError([1], [1], [1]);
            act.Should().Throw<ArgumentException>();
            act = () => RegressionMetrics.MeanAbsoluteScaledError([1], [1], null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void MinimumRiskThreshold_KnownAnswers()
        {
            new CostMatrix(100, 1).MinimumRiskThreshold().Should().BeApproximately(0.009900990099009901, 1e-15);
            new CostMatrix(50, 5, 1).MinimumRiskThreshold().Should().BeApproximately(0.09259259259259259, 1e-15);
            new CostMatrix(10, 2, 0, -1).MinimumRiskThreshold().Should().BeApproximately(0.23076923076923078, 1e-15);
            new CostMatrix(1, 1).MinimumRiskThreshold().Should().Be(0.5);
        }

        [Fact]
        public void MinimumRiskThreshold_IsWhereExpectedCostsAreEqual()
        {
            var costs = new CostMatrix(50, 5, 1, 0.5);
            var t = costs.MinimumRiskThreshold();
            var flag = t * costs.TruePositive + (1 - t) * costs.FalsePositive;
            var pass = t * costs.FalseNegative + (1 - t) * costs.TrueNegative;
            flag.Should().BeApproximately(pass, 1e-12);
        }

        [Fact]
        public void MinimumRiskThreshold_InvalidCostsThrow()
        {
            Action act = () => new CostMatrix(0, 1).MinimumRiskThreshold();
            act.Should().Throw<ArgumentException>();
            act = () => new CostMatrix(1, 0).MinimumRiskThreshold();
            act.Should().Throw<ArgumentException>();
            act = () => new CostMatrix(1, 1, 2).MinimumRiskThreshold();
            act.Should().Throw<ArgumentException>();
            act = () => new CostMatrix(double.NaN, 1).MinimumRiskThreshold();
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void MinimumRiskThreshold_MinimisesEmpiricalCostOnCalibratedProbabilities()
        {
            var rng = new Lcg(5);
            var probabilities = new double[5000];
            var labels = new int[5000];

            for (var i = 0; i < probabilities.Length; i++)
            {
                probabilities[i] = rng.Next();
                labels[i] = rng.Next() < probabilities[i] ? 1 : 0;
            }

            var costs = new CostMatrix(20, 1);

            var bayes = BinaryMetrics.Confusion(probabilities, labels, costs.MinimumRiskThreshold())
                .ExpectedCost(costs);

            foreach (var other in new[] { 0.3, 0.5, 0.8 })
            {
                bayes.Should().BeLessThanOrEqualTo(BinaryMetrics.Confusion(probabilities, labels, other)
                    .ExpectedCost(costs) + 1e-9);
            }
        }

        [Fact]
        public void ExpectedCost_KnownAnswer()
        {
            new ConfusionMatrix(3, 1, 4, 2).ExpectedCost(new CostMatrix(10, 2)).Should().BeApproximately(2.2, 1e-12);
            double.IsNaN(new ConfusionMatrix(0, 0, 0, 0).ExpectedCost(new CostMatrix(1, 1))).Should().BeTrue();
        }
    }
}