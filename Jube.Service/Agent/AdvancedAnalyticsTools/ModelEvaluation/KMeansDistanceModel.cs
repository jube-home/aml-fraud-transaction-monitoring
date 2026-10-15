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

using Accord.MachineLearning;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class KMeansDistanceModel : IAnomalyModel
    {
        private readonly double[][] centroids;
        private readonly double inertia;

        private KMeansDistanceModel(double[][] centroids, double inertia)
        {
            this.centroids = centroids;
            this.inertia = inertia;
        }

        public string Summary =>
            "k-means distance: rows are clustered, and the score is the Euclidean distance to the nearest cluster centre; rows far from every centre are anomalous.";

        public EvaluationNumber[] Facts()
        {
            return [ModelText.Fact("Clusters", centroids.Length), ModelText.Fact("TrainInertia", inertia)];
        }

        public double[] Score(double[][] rows)
        {
            return rows.Select(r => centroids.Min(c => Math.Sqrt(r.Zip(c, (a, b) => (a - b) * (a - b)).Sum())))
                .ToArray();
        }

        public static KMeansDistanceModel Fit(double[][] x, int clusters, int seed, CancellationToken token)
        {
            var k = Math.Max(1, Math.Min(clusters, x.Length));

            return SeededLibrary.Run(seed, () =>
            {
                var kmeans = new KMeans(k) { MaxIterations = 100, ParallelOptions = { MaxDegreeOfParallelism = 1 } };
                kmeans.Learn(x);
                var centroids = kmeans.Centroids.Select(c => (double[])c.Clone()).ToArray();
                var inertia = x.Sum(r => centroids.Min(c => r.Zip(c, (a, b) => (a - b) * (a - b)).Sum())) / x.Length;

                return new KMeansDistanceModel(centroids, inertia);
            });
        }
    }
}