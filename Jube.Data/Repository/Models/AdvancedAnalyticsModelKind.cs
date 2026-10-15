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

namespace Jube.Data.Repository.Models
{
    public enum AdvancedAnalyticsModelKind : byte
    {
        ExhaustiveTopologyNetwork = 0,
        LogisticRegressionPipeline = 1,
        AdaBoostClassifierPipeline = 2,
        BaggingClassifierPipeline = 3,
        DecisionTreeClassifierPipeline = 4,
        GradientBoostingClassifierPipeline = 5,
        KernelSupportVectorClassifierPipeline = 6,
        KNearestNeighboursClassifierPipeline = 7,
        LinearSupportVectorClassifierPipeline = 8,
        NaiveBayesClassifierPipeline = 9,
        NeuralNetworkClassifierPipeline = 10,
        RandomForestClassifierPipeline = 11,
        TreeAugmentedNaiveBayesClassifierPipeline = 12,
        IsolationForestAnomalyPipeline = 13,
        MahalanobisAnomalyPipeline = 14,
        OneClassSupportVectorAnomalyPipeline = 15,
        BalancedKMeansClustererPipeline = 16,
        BinarySplitClustererPipeline = 17,
        GaussianMixtureClustererPipeline = 18,
        KMeansClustererPipeline = 19,
        KMedoidsClustererPipeline = 20,
        KModesClustererPipeline = 21,
        MeanShiftClustererPipeline = 22,
        MiniBatchKMeansClustererPipeline = 23,
        GaussianMixtureDensityPipeline = 24,
        PrincipalComponentPipeline = 25,
        DecisionTreeRegressorPipeline = 26,
        GradientBoostingRegressorPipeline = 27,
        KernelRidgeRegressorPipeline = 28,
        KNearestNeighboursRegressorPipeline = 29,
        MultipleLinearRegressionPipeline = 30,
        NeuralNetworkRegressorPipeline = 31,
        RandomForestRegressorPipeline = 32
    }
}