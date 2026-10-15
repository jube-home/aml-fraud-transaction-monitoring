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
using System.Linq;
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Models;
using Jube.Analytics.Anomaly.Interfaces;
using log4net;

namespace Jube.Engine.Exhaustive.Anomaly
{
    internal static class AnomalyCandidates
    {
        internal const double DefaultBudgetSeconds = 120d;

        internal static IAnomalyDetector Create(AnomalyAlgorithm algorithm, AnomalyParameters parameters, ILog log)
        {
            return algorithm == AnomalyAlgorithm.OneClassSvm
                ? new OneClassSvmDetector(log)
                : AnomalyDetectorFactory.Create(algorithm, parameters);
        }

        internal static IReadOnlyList<AutoCandidate> For(int dimensions, int rows, AnomalyParameters parameters,
            ILog log)
        {
            IEnumerable<AnomalyAlgorithm> algorithms = parameters.Auto?.Candidates ?? Default(dimensions, rows);

            return
            [
                .. algorithms
                    .Where(a => a != AnomalyAlgorithm.OneClassSvm && a != AnomalyAlgorithm.Automatic)
                    .Distinct()
                    .Select(a => new AutoCandidate(a, a.ToString(), AnomalyDetectorFactory.Factory(a, parameters)))
            ];
        }

        internal static IReadOnlyList<AnomalyAlgorithm> Default(int dimensions, int rows)
        {
            var candidates = new List<AnomalyAlgorithm>
            {
                AnomalyAlgorithm.IsolationForest,
                AnomalyAlgorithm.Hbos,
                AnomalyAlgorithm.MarginalLikelihood,
                AnomalyAlgorithm.Pca
            };

            if (rows >= 6)
            {
                candidates.Add(AnomalyAlgorithm.GaussianMixture);
            }

            if (dimensions <= 100)
            {
                if (dimensions < rows / 2)
                {
                    candidates.Add(AnomalyAlgorithm.Mahalanobis);
                }

                if (dimensions < 0.5 * Math.Min(rows, 2000))
                {
                    candidates.Add(AnomalyAlgorithm.RobustMahalanobis);
                }

                candidates.Add(AnomalyAlgorithm.KnnDistance);
                candidates.Add(AnomalyAlgorithm.Consensus);
            }

            return candidates;
        }
    }
}