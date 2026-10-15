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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Engine.AdvancedAnalytics.Density;
using Jube.Engine.AdvancedAnalytics.DimensionReduction;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Jube.Engine.AdvancedAnalytics.Recall.Models;

namespace Jube.Engine.AdvancedAnalytics.Recall
{
    public static class AdvancedAnalyticsModelRegistry
    {
        private static readonly ConcurrentDictionary<string, Func<string, AdvancedAnalyticsRecallModel>> builders =
            new(StringComparer.Ordinal);

        static AdvancedAnalyticsModelRegistry()
        {
            Register(LogisticRegressionRecall.ModelKind, snapshotJson =>
            {
                var recall = LogisticRegressionRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(LogisticRegressionRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(MahalanobisAnomalyRecall.ModelKind, snapshotJson =>
            {
                var recall = MahalanobisAnomalyRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(MahalanobisAnomalyRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(MultipleLinearRegressionRecall.ModelKind, snapshotJson =>
            {
                var recall = MultipleLinearRegressionRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(MultipleLinearRegressionRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(DecisionTreeRegressorRecall.ModelKind, snapshotJson =>
            {
                var recall = DecisionTreeRegressorRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(DecisionTreeRegressorRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(KNearestNeighboursRegressorRecall.ModelKind, snapshotJson =>
            {
                var recall = KNearestNeighboursRegressorRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(KNearestNeighboursRegressorRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(RandomForestRegressorRecall.ModelKind, snapshotJson =>
            {
                var recall = RandomForestRegressorRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(RandomForestRegressorRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(KernelRidgeRegressorRecall.ModelKind, snapshotJson =>
            {
                var recall = KernelRidgeRegressorRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(KernelRidgeRegressorRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(NeuralNetworkRegressorRecall.ModelKind, snapshotJson =>
            {
                var recall = NeuralNetworkRegressorRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(NeuralNetworkRegressorRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(GradientBoostingRegressorRecall.ModelKind, snapshotJson =>
            {
                var recall = GradientBoostingRegressorRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(GradientBoostingRegressorRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(KMeansClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = KMeansClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(KMeansClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(PrincipalComponentRecall.ModelKind, snapshotJson =>
            {
                var recall = PrincipalComponentRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(PrincipalComponentRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(GaussianMixtureDensityRecall.ModelKind, snapshotJson =>
            {
                var recall = GaussianMixtureDensityRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(GaussianMixtureDensityRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(NeuralNetworkClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = NeuralNetworkClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(NeuralNetworkClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(RandomForestClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = RandomForestClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(RandomForestClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(DecisionTreeClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = DecisionTreeClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(DecisionTreeClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(NaiveBayesClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = NaiveBayesClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(NaiveBayesClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(TreeAugmentedNaiveBayesClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = TreeAugmentedNaiveBayesClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(TreeAugmentedNaiveBayesClassifierRecall.ModelKind,
                    recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(KNearestNeighboursClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = KNearestNeighboursClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(KNearestNeighboursClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(BaggingClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = BaggingClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(BaggingClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(AdaBoostClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = AdaBoostClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(AdaBoostClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(GradientBoostingClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = GradientBoostingClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(GradientBoostingClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(LinearSupportVectorClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = LinearSupportVectorClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(LinearSupportVectorClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(KernelSupportVectorClassifierRecall.ModelKind, snapshotJson =>
            {
                var recall = KernelSupportVectorClassifierRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(KernelSupportVectorClassifierRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(IsolationForestAnomalyRecall.ModelKind, snapshotJson =>
            {
                var recall = IsolationForestAnomalyRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(IsolationForestAnomalyRecall.ModelKind, recall.Variables,
                    recall.AsFunction(), recall.Explain);
            });

            Register(OneClassSupportVectorAnomalyRecall.ModelKind, snapshotJson =>
            {
                var recall = OneClassSupportVectorAnomalyRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(OneClassSupportVectorAnomalyRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(BinarySplitClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = BinarySplitClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(BinarySplitClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(MiniBatchKMeansClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = MiniBatchKMeansClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(MiniBatchKMeansClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(KMedoidsClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = KMedoidsClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(KMedoidsClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(BalancedKMeansClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = BalancedKMeansClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(BalancedKMeansClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(GaussianMixtureClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = GaussianMixtureClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(GaussianMixtureClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(KModesClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = KModesClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(KModesClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });

            Register(MeanShiftClustererRecall.ModelKind, snapshotJson =>
            {
                var recall = MeanShiftClustererRecall.FromSnapshot(snapshotJson);

                return new AdvancedAnalyticsRecallModel(MeanShiftClustererRecall.ModelKind, recall.Variables,
                    recall.AsFunction());
            });
        }

        public static IReadOnlyCollection<string> Kinds => [.. builders.Keys.Order(StringComparer.Ordinal)];

        public static void Register(string kind, Func<string, AdvancedAnalyticsRecallModel> builder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(kind);
            ArgumentNullException.ThrowIfNull(builder);

            builders[kind] = builder;
        }

        public static bool IsRegistered(string kind)
        {
            return !string.IsNullOrEmpty(kind) && builders.ContainsKey(kind);
        }

        public static AdvancedAnalyticsRecallModel Build(string modelJson)
        {
            var kind = AdvancedAnalyticsModelJson.ReadKind(modelJson);

            if (!builders.TryGetValue(kind, out var builder))
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"No recall builder is registered for model kind '{kind}'. Registered kinds: {string.Join(", ", Kinds)}.");
            }

            var model = builder(modelJson);

            if (model.Variables.Count < 1)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Model kind '{kind}' produced a recall with no variables.");
            }

            return model;
        }
    }
}