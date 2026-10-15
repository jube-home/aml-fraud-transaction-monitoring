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
using Accord.MachineLearning.Bayes;
using Accord.MachineLearning.Boosting;
using Accord.MachineLearning.Boosting.Learners;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math.Random;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Kernels;
using FluentAssertions;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Library
{
    public class LearningLibraryFixesTests
    {
        [Fact]
        public void WeightedNaiveBayesMatchesReplicatedRows()
        {
            double[][] x = [[1.0], [2.0], [6.0], [4.0], [8.0], [9.0]];
            int[] y = [0, 0, 0, 1, 1, 1];
            double[] weights = [1, 1, 2, 1, 3, 1];

            double[][] replicatedX = [x[0], x[1], x[2], x[2], x[3], x[4], x[4], x[4], x[5]];
            int[] replicatedY = [0, 0, 0, 0, 1, 1, 1, 1, 1];

            var weighted = new NaiveBayesLearning<NormalDistribution>
                { ParallelOptions = { MaxDegreeOfParallelism = 1 } };

            var weightedModel = weighted.Learn(x, y, weights);

            var replicated = new NaiveBayesLearning<NormalDistribution>
                { ParallelOptions = { MaxDegreeOfParallelism = 1 } };

            var replicatedModel = replicated.Learn(replicatedX, replicatedY);

            weightedModel.Priors[0].Should().BeApproximately(4.0 / 9.0, 1e-12);
            weightedModel.Priors[1].Should().BeApproximately(5.0 / 9.0, 1e-12);
            weightedModel.Priors[0].Should().BeApproximately(replicatedModel.Priors[0], 1e-12);

            weightedModel.Distributions[0, 0].Mean.Should()
                .BeApproximately(replicatedModel.Distributions[0, 0].Mean, 1e-9);
            weightedModel.Distributions[1, 0].Mean.Should()
                .BeApproximately(replicatedModel.Distributions[1, 0].Mean, 1e-9);
        }

        [Fact]
        public void EpsilonSupportVectorRegressionRecoversStraightLine()
        {
            var x = Enumerable.Range(0, 11).Select(i => new[] { (double)i }).ToArray();
            var y = x.Select(r => 2 * r[0] + 1).ToArray();

            var learner = new FanChenLinSupportVectorRegression
            {
                Complexity = 1000,
                Epsilon = 0.01,
                Kernel = new Linear(),
                Tolerance = 1e-6
            };

            var model = learner.Learn(x, y);

            foreach (var row in x)
            {
                model.Score(row).Should().BeApproximately(2 * row[0] + 1, 0.1);
            }
        }

        [Fact]
        public void EpsilonSupportVectorRegressionEpsilonFlattensTheFittedSlope()
        {
            var x = Enumerable.Range(0, 11).Select(i => new[] { (double)i }).ToArray();
            var y = x.Select(r => 2 * r[0] + 1).ToArray();

            var narrow = new FanChenLinSupportVectorRegression
                { Complexity = 1000, Epsilon = 0.01, Kernel = new Linear() }.Learn(x, y);
            var wide = new FanChenLinSupportVectorRegression { Complexity = 1000, Epsilon = 3.0, Kernel = new Linear() }
                .Learn(x, y);

            var narrowSlope = (narrow.Score([10.0]) - narrow.Score([0.0])) / 10;
            var wideSlope = (wide.Score([10.0]) - wide.Score([0.0])) / 10;

            narrowSlope.Should().BeApproximately(2.0, 0.05);
            wideSlope.Should().BeApproximately(1.4, 0.1);
        }

        [Fact]
        public void AdaBoostPerfectWeakLearnerGivesFiniteWeightAndKeepsCallerWeights()
        {
            double[][] x = [[1.0], [2.0], [3.0], [4.0], [5.0], [6.0]];
            int[] y = [0, 0, 0, 1, 1, 1];
            double[] weights = [1, 1, 1, 1, 1, 1];

            var boost = new AdaBoost<DecisionStump> { Learner = _ => new ThresholdLearning(), MaxIterations = 10 };
            var model = boost.Learn(x, y, weights);

            weights.Should().Equal(1, 1, 1, 1, 1, 1);
            model.Models.Should().NotBeEmpty();
            model.Models.Select(m => m.Weight).Should().OnlyContain(w => !double.IsNaN(w) && !double.IsInfinity(w));
            model.Decide(x).Should().Equal([.. y.Select(v => v == 1)]);
        }

        [Fact]
        public void DecisionStumpGreaterThanIsStrict()
        {
            var stump = new DecisionStump { Index = 0, Threshold = 3, Comparison = ComparisonKind.GreaterThan };

            stump.Decide([3.0]).Should().BeFalse();
            stump.Decide([3.5]).Should().BeTrue();

            new DecisionStump { Index = 0, Threshold = 3, Comparison = ComparisonKind.GreaterThanOrEqual }
                .Decide([3.0]).Should().BeTrue();
        }

        [Fact]
        public void RandomForestWithSameSeedReproducesTheSameForest()
        {
            var (x, y) = ForestData();

            var first = LearnForest(x, y, 11);
            var second = LearnForest(x, y, 11);
            var other = LearnForest(x, y, 12);

            first.Should().Equal(second);
            first.Should().NotEqual(other);
        }

        [Fact]
        public void LinearDualCoordinateDescentWithSameSeedReproducesTheSameWeights()
        {
            var (x, y) = ForestData();

            var first = LearnLinear(x, y, 5);
            var second = LearnLinear(x, y, 5);

            first.Should().Equal(second);
        }

        private static (double[][] x, int[] y) ForestData()
        {
            var random = new Random(1);
            var x = new double[80][];
            var y = new int[80];

            for (var i = 0; i < x.Length; i++)
            {
                x[i] = [random.NextDouble(), random.NextDouble(), random.NextDouble()];
                y[i] = x[i][0] + x[i][1] + 0.4 * (random.NextDouble() - 0.5) > 1 ? 1 : 0;
            }

            return (x, y);
        }

        private static double[] LearnForest(double[][] x, int[] y, int seed)
        {
            Generator.ThreadSeed = seed;

            try
            {
                var learner = new RandomForestLearning { NumberOfTrees = 12, BootstrapSample = true };
                var forest = learner.Learn(x, y);
                Generator.ThreadSeed = seed;
                var probe = new Random(99);

                return
                [
                    .. Enumerable.Range(0, 60)
                        .Select(_ =>
                            forest.Decide([probe.NextDouble(), probe.NextDouble(), probe.NextDouble()]) + 0.0),

                    .. forest.Trees.Select(t => (double)t.ToString()!.Length)
                ];
            }
            finally
            {
                Generator.ThreadSeed = null;
            }
        }

        private static double[] LearnLinear(double[][] x, int[] y, int seed)
        {
            Generator.ThreadSeed = seed;

            try
            {
                var learner = new LinearDualCoordinateDescent { Complexity = 1 };
                var machine = learner.Learn(x, [.. y.Select(v => v == 1)]);

                return [.. machine.Weights, machine.Threshold];
            }
            finally
            {
                Generator.ThreadSeed = null;
            }
        }
    }
}