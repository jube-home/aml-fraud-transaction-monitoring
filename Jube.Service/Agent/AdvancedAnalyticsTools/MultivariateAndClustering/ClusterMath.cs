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

using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    internal static class ClusterMath
    {
        public static (int[] Mapped, int[] Distinct) MapLabels(int[] labels, string name, int minimumClusters)
        {
            var distinct = labels.Distinct().OrderBy(v => v).ToArray();

            if (distinct.Length < minimumClusters)
            {
                throw new ArgumentException(
                    $"{name} holds {distinct.Length} distinct cluster label(s) but at least {minimumClusters} are needed for this measure.",
                    name);
            }

            var index = distinct.Select((label, i) => (label, i)).ToDictionary(t => t.label, t => t.i);

            return (labels.Select(v => index[v]).ToArray(), distinct);
        }

        public static double[][] Centroids(double[][] data, int[] mapped, int clusters)
        {
            var columns = data[0].Length;
            var sums = new double[clusters][];
            var counts = new int[clusters];

            for (var c = 0; c < clusters; c++)
            {
                sums[c] = new double[columns];
            }

            for (var i = 0; i < data.Length; i++)
            {
                counts[mapped[i]]++;

                for (var j = 0; j < columns; j++)
                {
                    sums[mapped[i]][j] += data[i][j];
                }
            }

            for (var c = 0; c < clusters; c++)
            {
                for (var j = 0; j < columns; j++)
                {
                    sums[c][j] = counts[c] == 0 ? double.NaN : sums[c][j] / counts[c];
                }
            }

            return sums;
        }

        public static double Inertia(double[][] data, int[] mapped, double[][] centroids)
        {
            var total = 0.0;

            for (var i = 0; i < data.Length; i++)
            {
                total += SquaredEuclidean(data[i], centroids[mapped[i]]);
            }

            return total;
        }

        public static ClusterProfile[] Profiles(double[][] data, int[] mapped, int clusters, double[][] centroids)
        {
            var columns = data[0].Length;
            var sizes = new int[clusters];
            var squares = new double[clusters][];
            var within = new double[clusters];
            var distanceSum = new double[clusters];
            var distanceMax = new double[clusters];

            for (var c = 0; c < clusters; c++)
            {
                squares[c] = new double[columns];
            }

            for (var i = 0; i < data.Length; i++)
            {
                var c = mapped[i];
                sizes[c]++;
                var squared = 0.0;

                for (var j = 0; j < columns; j++)
                {
                    var d = data[i][j] - centroids[c][j];
                    squares[c][j] += d * d;
                    squared += d * d;
                }

                within[c] += squared;
                var distance = Math.Sqrt(squared);
                distanceSum[c] += distance;
                distanceMax[c] = Math.Max(distanceMax[c], distance);
            }

            var profiles = new ClusterProfile[clusters];

            for (var c = 0; c < clusters; c++)
            {
                var deviations = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    deviations[j] = sizes[c] > 1 ? Math.Sqrt(squares[c][j] / (sizes[c] - 1)) : double.NaN;
                }

                profiles[c] = new ClusterProfile(c, sizes[c], sizes[c] / (double)data.Length, centroids[c], deviations,
                    within[c], sizes[c] == 0 ? double.NaN : distanceSum[c] / sizes[c],
                    sizes[c] == 0 ? double.NaN : distanceMax[c]);
            }

            return profiles;
        }

        public static double[] Silhouette(double[][] data, int[] mapped, int clusters,
            Func<double[], double[], double> distance, CancellationToken token)
        {
            var sizes = new int[clusters];

            foreach (var c in mapped)
            {
                sizes[c]++;
            }

            var scores = new double[data.Length];
            var sums = new double[clusters];

            for (var i = 0; i < data.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                Array.Clear(sums, 0, sums.Length);

                for (var j = 0; j < data.Length; j++)
                {
                    if (i != j)
                    {
                        sums[mapped[j]] += distance(data[i], data[j]);
                    }
                }

                var own = mapped[i];

                if (sizes[own] <= 1)
                {
                    scores[i] = 0.0;
                    continue;
                }

                var a = sums[own] / (sizes[own] - 1);
                var b = double.PositiveInfinity;

                for (var c = 0; c < clusters; c++)
                {
                    if (c != own && sizes[c] > 0)
                    {
                        b = Math.Min(b, sums[c] / sizes[c]);
                    }
                }

                var denominator = Math.Max(a, b);
                scores[i] = denominator == 0 ? 0.0 : (b - a) / denominator;
            }

            return scores;
        }

        public static double DaviesBouldin(double[][] data, int[] mapped, int clusters, double[][] centroids)
        {
            var scatter = new double[clusters];
            var sizes = new int[clusters];

            for (var i = 0; i < data.Length; i++)
            {
                scatter[mapped[i]] += Math.Sqrt(SquaredEuclidean(data[i], centroids[mapped[i]]));
                sizes[mapped[i]]++;
            }

            var present = Enumerable.Range(0, clusters).Where(c => sizes[c] > 0).ToArray();

            foreach (var c in present)
            {
                scatter[c] /= sizes[c];
            }

            var total = 0.0;

            foreach (var i in present)
            {
                var worst = 0.0;

                foreach (var j in present)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var separation = Math.Sqrt(SquaredEuclidean(centroids[i], centroids[j]));
                    var ratio = separation == 0 ? double.PositiveInfinity : (scatter[i] + scatter[j]) / separation;
                    worst = Math.Max(worst, ratio);
                }

                total += worst;
            }

            return total / present.Length;
        }

        public static (double CalinskiHarabasz, double Between) CalinskiHarabasz(double[][] data, int[] mapped,
            int clusters, double[][] centroids)
        {
            var columns = data[0].Length;
            var overall = new double[columns];

            for (var i = 0; i < data.Length; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    overall[j] += data[i][j];
                }
            }

            for (var j = 0; j < columns; j++)
            {
                overall[j] /= data.Length;
            }

            var sizes = new int[clusters];

            foreach (var c in mapped)
            {
                sizes[c]++;
            }

            var between = 0.0;

            for (var c = 0; c < clusters; c++)
            {
                if (sizes[c] > 0)
                {
                    between += sizes[c] * SquaredEuclidean(centroids[c], overall);
                }
            }

            var within = Inertia(data, mapped, centroids);
            var present = sizes.Count(s => s > 0);

            if (present < 2 || data.Length <= present)
            {
                return (double.NaN, between);
            }

            var index = within == 0
                ? double.PositiveInfinity
                : between / (present - 1) / (within / (data.Length - present));

            return (index, between);
        }

        public static double SquaredEuclidean(double[] a, double[] b)
        {
            var total = 0.0;

            for (var i = 0; i < a.Length; i++)
            {
                var d = a[i] - b[i];
                total += d * d;
            }

            return total;
        }

        public static double Mean(double[] values)
        {
            return values.Length == 0 ? double.NaN : values.Average();
        }
    }
}