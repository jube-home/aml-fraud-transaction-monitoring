/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    public static class AdvancedAnalyticsTrainingAgentToolNames
    {
        private static readonly string[] runControl =
        [
            "GetAdvancedAnalyticsTrainingRunStatus",
            "CancelAdvancedAnalyticsTrainingRun",
            "GetAdvancedAnalyticsTrainingRunResult"
        ];

        public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> All = Build();

        public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> KindsOf =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["advanced-analytics-training-classifier-trees"] = ["Classifier"],
                ["advanced-analytics-training-classifier-probabilistic"] = ["Classifier"],
                ["advanced-analytics-training-classifier-margin"] = ["Classifier"],
                ["advanced-analytics-training-clusterer-centroid"] = ["Clusterer"],
                ["advanced-analytics-training-clusterer-density"] = ["Clusterer"],
                ["advanced-analytics-training-regressor"] = ["Regressor"],
                ["advanced-analytics-training-anomaly"] = ["AnomalyDetector"],
                ["advanced-analytics-training-latent-structure"] = ["DimensionReducer", "DensityEstimator"]
            };

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> Build()
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["advanced-analytics-training-classifier-trees"] = With(
                    "SubmitDecisionTreeClassifierTraining",
                    "SubmitRandomForestClassifierTraining",
                    "SubmitBaggingClassifierTraining",
                    "SubmitAdaBoostClassifierTraining",
                    "SubmitGradientBoostingClassifierTraining"),

                ["advanced-analytics-training-classifier-probabilistic"] = With(
                    "SubmitLogisticRegressionTraining",
                    "SubmitNaiveBayesClassifierTraining",
                    "SubmitTreeAugmentedNaiveBayesClassifierTraining"),

                ["advanced-analytics-training-classifier-margin"] = With(
                    "SubmitLinearSupportVectorClassifierTraining",
                    "SubmitKernelSupportVectorClassifierTraining",
                    "SubmitKNearestNeighboursClassifierTraining",
                    "SubmitNeuralNetworkClassifierTraining"),

                ["advanced-analytics-training-clusterer-centroid"] = With(
                    "SubmitKMeansClustererTraining",
                    "SubmitMiniBatchKMeansClustererTraining",
                    "SubmitBalancedKMeansClustererTraining",
                    "SubmitKMedoidsClustererTraining",
                    "SubmitKModesClustererTraining"),

                ["advanced-analytics-training-clusterer-density"] = With(
                    "SubmitBinarySplitClustererTraining",
                    "SubmitMeanShiftClustererTraining",
                    "SubmitGaussianMixtureClustererTraining"),

                ["advanced-analytics-training-regressor"] = With(
                    "SubmitLinearRegressionTraining",
                    "SubmitKernelRidgeRegressorTraining",
                    "SubmitDecisionTreeRegressorTraining",
                    "SubmitRandomForestRegressorTraining",
                    "SubmitGradientBoostingRegressorTraining",
                    "SubmitKNearestNeighboursRegressorTraining",
                    "SubmitNeuralNetworkRegressorTraining"),

                ["advanced-analytics-training-anomaly"] = With(
                    "SubmitIsolationForestAnomalyTraining",
                    "SubmitMahalanobisAnomalyTraining",
                    "SubmitOneClassSupportVectorAnomalyTraining"),

                ["advanced-analytics-training-latent-structure"] = With(
                    "SubmitPrincipalComponentTraining",
                    "SubmitGaussianMixtureDensityTraining")
            };
        }

        private static IReadOnlyList<string> With(params string[] submissions)
        {
            return [.. submissions, .. runControl];
        }
    }
}