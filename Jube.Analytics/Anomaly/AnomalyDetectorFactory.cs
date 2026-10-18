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

using Jube.Analytics.Anomaly.Interfaces;
using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public static class AnomalyDetectorFactory
    {
        public static bool IsNative(AnomalyAlgorithm algorithm)
        {
            return algorithm is not (AnomalyAlgorithm.OneClassSvm or AnomalyAlgorithm.Automatic);
        }

        public static Func<IAnomalyDetector> Factory(AnomalyAlgorithm algorithm, AnomalyParameters parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);

            return algorithm switch
            {
                AnomalyAlgorithm.IsolationForest => () => new IsolationForestDetector(parameters.Trees ?? 100,
                    parameters.SubsampleSize ?? 256),
                AnomalyAlgorithm.Hbos => () => new HbosDetector(parameters.Bins ?? 0),
                AnomalyAlgorithm.Mahalanobis => () => new MahalanobisDetector(
                    parameters.Regularisation ?? CovarianceRegularisation.LedoitWolf),
                AnomalyAlgorithm.RobustMahalanobis => () => new MahalanobisDetector(
                    parameters.Regularisation ?? CovarianceRegularisation.LedoitWolf, true),
                AnomalyAlgorithm.Pca => () => new PcaDetector(
                    retention: parameters.Retention ?? PcaRetention.ParallelAnalysis,
                    explainedVariance: parameters.ExplainedVariance ?? 0.95),
                AnomalyAlgorithm.GaussianMixture => () => new GaussianMixtureDetector(parameters.Components ?? 3,
                    parameters.FullCovariance ?? false),
                AnomalyAlgorithm.KnnDistance => () => new KnnDistanceDetector(parameters.K ?? 10,
                    parameters.RowCap ?? 3000),
                AnomalyAlgorithm.LocalOutlierFactor => () => new LocalOutlierFactorDetector(parameters.K ?? 20,
                    parameters.RowCap ?? 3000),
                AnomalyAlgorithm.MarginalLikelihood => () => new MarginalLikelihoodDetector(),
                AnomalyAlgorithm.FeatureBagging => FeatureBagging(parameters),
                AnomalyAlgorithm.Consensus => () => new ConsensusDetector(
                [
                    Factory(AnomalyAlgorithm.IsolationForest, parameters),
                    Factory(AnomalyAlgorithm.Hbos, parameters),
                    Factory(AnomalyAlgorithm.Mahalanobis, parameters),
                    Factory(AnomalyAlgorithm.Pca, parameters)
                ], parameters.Combination ?? ScoreCombination.RankAverage),
                _ => throw new ArgumentException($"{algorithm} is not a native detector.")
            };
        }

        public static IAnomalyDetector Create(AnomalyAlgorithm algorithm, AnomalyParameters parameters)
        {
            return Factory(algorithm, parameters)();
        }

        private static Func<IAnomalyDetector> FeatureBagging(AnomalyParameters parameters)
        {
            var baseAlgorithm = parameters.BaseAlgorithm ?? AnomalyAlgorithm.LocalOutlierFactor;
            var baseFactory = Factory(baseAlgorithm, parameters);

            return () => new FeatureBaggingDetector(baseFactory, parameters.Members ?? 20, parameters.SubspaceSize ?? 0,
                parameters.Combination ?? ScoreCombination.RankAverage);
        }
    }
}