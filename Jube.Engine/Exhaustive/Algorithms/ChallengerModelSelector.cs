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
using Jube.Data.Repository.Models;
using Jube.Engine.AdvancedAnalytics.Classification;

namespace Jube.Engine.Exhaustive.Algorithms
{
    internal static class ChallengerModelSelector
    {
        private static readonly AdvancedAnalyticsModelKind[] kinds =
        [
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
        ];

        internal static double ChallengerProbability(double baseProbability, int bestTopologyComplexity,
            int topologyComplexityLimit)
        {
            if (topologyComplexityLimit <= 0)
            {
                return baseProbability;
            }

            var complexityRatio = Math.Clamp(bestTopologyComplexity / (double)topologyComplexityLimit, 0d, 1d);

            return Math.Clamp(baseProbability + (1d - baseProbability) * complexityRatio, 0d, 1d);
        }

        internal static AdvancedAnalyticsModelKind? SelectFamily(Random seeded, double challengerProbability)
        {
            return seeded.NextDouble() < challengerProbability
                ? kinds[seeded.Next(kinds.Length)]
                : null;
        }

        internal static RandomForestClassifierOptions RandomForest(Random seeded)
        {
            return new RandomForestClassifierOptions
            {
                TreeCount = seeded.Next(50, 201),
                MaxDepth = seeded.Next(4, 11),
                MinLeafSize = seeded.Next(2, 11),
                SampleFraction = 0.6 + seeded.NextDouble() * 0.4,
                Seed = seeded.Next()
            };
        }

        internal static DecisionTreeClassifierOptions DecisionTree(Random seeded)
        {
            return new DecisionTreeClassifierOptions
            {
                MaxDepth = seeded.Next(3, 11),
                MinimumLeafSize = seeded.Next(2, 11),
                Seed = seeded.Next()
            };
        }

        internal static KNearestNeighboursClassifierOptions KNearestNeighbours(Random seeded)
        {
            return new KNearestNeighboursClassifierOptions
            {
                NeighbourCount = seeded.Next(5, 26),
                Seed = seeded.Next()
            };
        }

        internal static BaggingClassifierOptions Bagging(Random seeded)
        {
            return new BaggingClassifierOptions
            {
                TreeCount = seeded.Next(50, 201),
                MaxDepth = seeded.Next(4, 11),
                MinLeafSize = seeded.Next(2, 11),
                Seed = seeded.Next()
            };
        }

        internal static AdaBoostClassifierOptions AdaBoost(Random seeded)
        {
            return new AdaBoostClassifierOptions
            {
                Rounds = seeded.Next(50, 201),
                MaxDepth = seeded.Next(1, 4),
                Seed = seeded.Next()
            };
        }

        internal static GradientBoostingClassifierOptions GradientBoosting(Random seeded)
        {
            return new GradientBoostingClassifierOptions
            {
                Rounds = seeded.Next(50, 201),
                MaxDepth = seeded.Next(2, 6),
                LearningRate = 0.05 + seeded.NextDouble() * 0.15,
                Seed = seeded.Next()
            };
        }

        internal static LinearSupportVectorClassifierOptions LinearSupportVector(Random seeded)
        {
            return new LinearSupportVectorClassifierOptions
            {
                Complexity = Math.Pow(10, seeded.NextDouble() * 2 - 1),
                Seed = seeded.Next()
            };
        }

        internal static KernelSupportVectorClassifierOptions KernelSupportVector(Random seeded)
        {
            return new KernelSupportVectorClassifierOptions
            {
                Complexity = Math.Pow(10, seeded.NextDouble() * 2 - 1),
                Seed = seeded.Next()
            };
        }

        internal static NeuralNetworkClassifierOptions NeuralNetwork(Random seeded)
        {
            return new NeuralNetworkClassifierOptions
            {
                HiddenUnits = seeded.Next(4, 17),
                Epochs = seeded.Next(100, 301),
                Seed = seeded.Next()
            };
        }
    }
}