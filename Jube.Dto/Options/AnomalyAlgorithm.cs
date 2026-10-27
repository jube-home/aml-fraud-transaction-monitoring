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

namespace Jube.Dto.Options
{
    using System.ComponentModel;

    public enum AnomalyAlgorithm
    {
        [Description("One class support vector machine (default)")] OneClassSupportVectorMachine = 0,
        [Description("Isolation forest")] IsolationForest = 1,
        [Description("Histogram based outlier score")] HistogramBasedOutlierScore = 2,
        [Description("Mahalanobis distance")] MahalanobisDistance = 3,
        [Description("Robust Mahalanobis distance")] RobustMahalanobisDistance = 4,
        [Description("Principal component analysis")] PrincipalComponentAnalysis = 5,
        [Description("Gaussian mixture")] GaussianMixture = 6,
        [Description("k nearest neighbour distance")] KNearestNeighbourDistance = 7,
        [Description("Local outlier factor")] LocalOutlierFactor = 8,
        [Description("Marginal likelihood")] MarginalLikelihood = 9,
        [Description("Feature bagging ensemble")] FeatureBaggingEnsemble = 10,
        [Description("Consensus ensemble")] ConsensusEnsemble = 11,
        [Description("Automatic")] Automatic = 100
    }
}
