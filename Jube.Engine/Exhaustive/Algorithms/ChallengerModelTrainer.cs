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
using System.Collections.Generic;
using Jube.Data.Repository.Models;
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Engine.AdvancedAnalytics.Recall.Models;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.Exhaustive.Algorithms
{
    internal sealed record ChallengerTrainingResult(
        string Kind,
        string ModelJson,
        string MetricsJson,
        IReadOnlyList<string> Variables,
        AdvancedAnalyticsRecallModel Recall);

    internal static class ChallengerModelTrainer
    {
        internal static ChallengerTrainingResult Train(AdvancedAnalyticsModelKind kind, Random seeded,
            double[][] inputs, int[] labels, string[] names)
        {
            var (modelJson, metricsJson) = kind switch
            {
                AdvancedAnalyticsModelKind.LogisticRegressionPipeline => FromLogisticRegression(
                    LogisticRegressionTrainer.Train(inputs, labels, names,
                        new LogisticRegressionOptions { Seed = seeded.Next() })),
                AdvancedAnalyticsModelKind.RandomForestClassifierPipeline => FromRandomForest(
                    RandomForestClassifierTrainer.Train(inputs, labels, names,
                        ChallengerModelSelector.RandomForest(seeded))),
                AdvancedAnalyticsModelKind.DecisionTreeClassifierPipeline => FromDecisionTree(
                    DecisionTreeClassifierTrainer.Train(inputs, labels, names,
                        ChallengerModelSelector.DecisionTree(seeded))),
                AdvancedAnalyticsModelKind.NaiveBayesClassifierPipeline => FromNaiveBayes(
                    NaiveBayesClassifierTrainer.Train(inputs, labels, names,
                        new NaiveBayesClassifierOptions { Seed = seeded.Next() })),
                AdvancedAnalyticsModelKind.TreeAugmentedNaiveBayesClassifierPipeline => FromTreeAugmentedNaiveBayes(
                    TreeAugmentedNaiveBayesClassifierTrainer.Train(inputs, labels, names,
                        new TreeAugmentedNaiveBayesClassifierOptions { Seed = seeded.Next() })),
                AdvancedAnalyticsModelKind.KNearestNeighboursClassifierPipeline => FromKNearestNeighbours(
                    KNearestNeighboursClassifierTrainer.Train(inputs, labels, names,
                        ChallengerModelSelector.KNearestNeighbours(seeded))),
                AdvancedAnalyticsModelKind.BaggingClassifierPipeline => FromBagging(
                    BaggingClassifierTrainer.Train(inputs, labels, names, ChallengerModelSelector.Bagging(seeded))),
                AdvancedAnalyticsModelKind.AdaBoostClassifierPipeline => FromAdaBoost(
                    AdaBoostClassifierTrainer.Train(inputs, labels, names, ChallengerModelSelector.AdaBoost(seeded))),
                AdvancedAnalyticsModelKind.GradientBoostingClassifierPipeline => FromGradientBoosting(
                    GradientBoostingClassifierTrainer.Train(inputs, labels, names,
                        ChallengerModelSelector.GradientBoosting(seeded))),
                AdvancedAnalyticsModelKind.LinearSupportVectorClassifierPipeline => FromLinearSupportVector(
                    LinearSupportVectorClassifierTrainer.Train(inputs, labels, names,
                        ChallengerModelSelector.LinearSupportVector(seeded))),
                AdvancedAnalyticsModelKind.KernelSupportVectorClassifierPipeline => FromKernelSupportVector(
                    KernelSupportVectorClassifierTrainer.Train(inputs, labels, names,
                        ChallengerModelSelector.KernelSupportVector(seeded))),
                AdvancedAnalyticsModelKind.NeuralNetworkClassifierPipeline => FromNeuralNetwork(
                    NeuralNetworkClassifierTrainer.Train(inputs, labels, names,
                        ChallengerModelSelector.NeuralNetwork(seeded))),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unrecognised challenger model kind.")
            };

            var recall = AdvancedAnalyticsModelRegistry.Build(modelJson);

            return new ChallengerTrainingResult(recall.Kind, modelJson, metricsJson, recall.Variables, recall);
        }

        private static (string ModelJson, string MetricsJson) FromLogisticRegression(LogisticRegressionResult result)
        {
            return (result.SnapshotJson,
                AdvancedAnalyticsEvidenceJson.Serialise(AdvancedAnalyticsEvidenceJson.FromResult(result)));
        }

        private static (string ModelJson, string MetricsJson) FromRandomForest(RandomForestClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<RandomForestEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromDecisionTree(DecisionTreeClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<DecisionTreeEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromNaiveBayes(NaiveBayesClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<NaiveBayesEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromTreeAugmentedNaiveBayes(
            TreeAugmentedNaiveBayesClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<TreeAugmentedNaiveBayesEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromKNearestNeighbours(
            KNearestNeighboursClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<KNearestNeighboursEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromBagging(BaggingClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<BaggingEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromAdaBoost(AdaBoostClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<AdaBoostEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromGradientBoosting(
            GradientBoostingClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<GradientBoostingEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromLinearSupportVector(
            LinearSupportVectorClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<LinearSupportVectorEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromKernelSupportVector(
            KernelSupportVectorClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<KernelSupportVectorEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }

        private static (string ModelJson, string MetricsJson) FromNeuralNetwork(NeuralNetworkClassifierResult result)
        {
            return (result.SnapshotJson, AdvancedAnalyticsEvidenceJson.Serialise(
                new AdvancedAnalyticsEvidence<NeuralNetworkEvidenceDetails>
                {
                    Kind = result.Kind,
                    Seed = result.Seed,
                    Variables = result.Variables,
                    SplitSizes = result.SplitSizes,
                    Preprocessing = result.Preprocessing,
                    Warnings = result.Warnings,
                    Details = result.Details
                }));
        }
    }
}