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
using Jube.Data.Repository.Models;
using Jube.Engine.Exhaustive.Algorithms;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class ChallengerModelTests
    {
        [Fact]
        public void ChallengerProbability_IsBaseRateWhenComplexityIsZero()
        {
            ChallengerModelSelector.ChallengerProbability(0.1, 0, 10000).Should().Be(0.1);
        }

        [Fact]
        public void ChallengerProbability_RampsTowardsOneAsComplexityApproachesTheLimit()
        {
            var atLimit = ChallengerModelSelector.ChallengerProbability(0.1, 10000, 10000);
            var halfway = ChallengerModelSelector.ChallengerProbability(0.1, 5000, 10000);

            atLimit.Should().BeApproximately(1d, 0.0001);
            halfway.Should().BeGreaterThan(0.1).And.BeLessThan(atLimit);
        }

        [Fact]
        public void ChallengerProbability_IsUnaffectedByAZeroLimit()
        {
            ChallengerModelSelector.ChallengerProbability(0.25, 500, 0).Should().Be(0.25);
        }

        [Fact]
        public void SelectFamily_NeverChallengesWhenProbabilityIsZero()
        {
            var random = new Random(11);

            for (var i = 0; i < 50; i++)
            {
                ChallengerModelSelector.SelectFamily(random, 0d).Should().BeNull();
            }
        }

        [Fact]
        public void SelectFamily_AlwaysReturnsARecognisedKindWhenProbabilityIsOne()
        {
            var random = new Random(23);

            var recognised = new[]
            {
                AdvancedAnalyticsModelKind.LogisticRegressionPipeline,
                AdvancedAnalyticsModelKind.RandomForestClassifierPipeline,
                AdvancedAnalyticsModelKind.DecisionTreeClassifierPipeline,
                AdvancedAnalyticsModelKind.NaiveBayesClassifierPipeline,
                AdvancedAnalyticsModelKind.TreeAugmentedNaiveBayesClassifierPipeline,
                AdvancedAnalyticsModelKind.KNearestNeighboursClassifierPipeline,
                AdvancedAnalyticsModelKind.BaggingClassifierPipeline,
                AdvancedAnalyticsModelKind.AdaBoostClassifierPipeline,
                AdvancedAnalyticsModelKind.GradientBoostingClassifierPipeline,
                AdvancedAnalyticsModelKind.LinearSupportVectorClassifierPipeline,
                AdvancedAnalyticsModelKind.KernelSupportVectorClassifierPipeline,
                AdvancedAnalyticsModelKind.NeuralNetworkClassifierPipeline
            };

            for (var i = 0; i < 50; i++)
            {
                var kind = ChallengerModelSelector.SelectFamily(random, 1d);
                kind.Should().NotBeNull();
                recognised.Should().Contain(kind!.Value);
            }
        }

        private static (double[][] Inputs, int[] Labels, string[] Names) SyntheticData()
        {
            var random = new Random(41);
            var names = new[] { "Amount", "RiskScore", "TenureDays" };
            var inputs = new double[300][];
            var labels = new int[300];

            for (var i = 0; i < inputs.Length; i++)
            {
                var a = Normal(random);
                var b = Normal(random);
                var c = Normal(random);
                inputs[i] = [a, b, c];
                var z = 1.2 * a - 0.8 * b + 0.3 * c;
                labels[i] = random.NextDouble() < 1 / (1 + Math.Exp(-z)) ? 1 : 0;
            }

            return (inputs, labels, names);
        }

        private static double Normal(Random random)
        {
            var u = 1 - random.NextDouble();
            var v = random.NextDouble();

            return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
        }

        [Theory]
        [InlineData(AdvancedAnalyticsModelKind.LogisticRegressionPipeline)]
        [InlineData(AdvancedAnalyticsModelKind.RandomForestClassifierPipeline)]
        [InlineData(AdvancedAnalyticsModelKind.KNearestNeighboursClassifierPipeline)]
        [InlineData(AdvancedAnalyticsModelKind.KernelSupportVectorClassifierPipeline)]
        public void Train_ProducesARecognisableModelWithSaneRecall(AdvancedAnalyticsModelKind kind)
        {
            var (inputs, labels, names) = SyntheticData();
            var random = new Random(7);

            var result = ChallengerModelTrainer.Train(kind, random, inputs, labels, names);

            result.Kind.Should().Be(kind.ToString());
            result.ModelJson.Should().NotBeNullOrWhiteSpace();
            result.MetricsJson.Should().NotBeNullOrWhiteSpace();
            result.Variables.Should().BeEquivalentTo(names);

            var recalled = inputs.Take(20).Select(row => result.Recall.Predict(row)[0]).ToArray();
            recalled.Should().OnlyContain(v => double.IsFinite(v) && v >= 0d && v <= 1d);
        }

        [Fact]
        public void Train_UnrecognisedKindThrows()
        {
            var (inputs, labels, names) = SyntheticData();
            var random = new Random(7);

            var act = () => ChallengerModelTrainer.Train(AdvancedAnalyticsModelKind.KMeansClustererPipeline, random,
                inputs, labels, names);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}