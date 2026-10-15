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

using System.ComponentModel;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    [AdvancedAnalyticsToolGroup("MultivariateAndClustering")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ClusterEvaluationTools
    {
        private const string LabelsText =
            "One integer cluster label per row, in row order; labels can be any integers and rows sharing a label form a cluster.";

        [AdvancedAnalyticsTool("ComputeSilhouetteScores")]
        [Description(
            "Measures how well each row fits its assigned cluster with the silhouette: for a row, a is its mean distance to the other rows of its own cluster and b is its mean distance to the nearest other cluster, and the score is (b minus a) divided by the larger of the two, between -1 and 1. " +
            "Near 1 means the row sits firmly inside its cluster, near 0 means it is on a border, negative means it may belong to another cluster; a row alone in its cluster scores 0. It returns every row's score, each cluster's mean score (ClusterLabels gives the label for each entry) and the overall mean, which is the usual single quality number (above about 0.5 is a clear structure, below 0.25 is weak). " +
            "Needs at least 2 clusters and at most 5000 rows because every pair of rows is compared.")]
        public SilhouetteResult ComputeSilhouetteScores(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 5000.")]
            double[][] data,
            [Description(LabelsText)] int[] labels,
            [Description("Distance between rows. Default Euclidean.")]
            VectorDistanceMetric distance = VectorDistanceMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1. Default 3.")]
            double minkowskiOrder = 3,
            CancellationToken cancellationToken = default)
        {
            var (mapped, distinct) = Prepare(data, labels, MultivariateLimits.MaximumQuadraticRows, 2);

            var scores = ClusterMath.Silhouette(data, mapped, distinct.Length,
                DistanceFactory.Create(distance, minkowskiOrder), cancellationToken);

            var perCluster = Enumerable.Range(0, distinct.Length)
                .Select(c => scores.Where((_, i) => mapped[i] == c).Average()).ToArray();

            return new SilhouetteResult(data.Length, distinct.Length, scores, perCluster, distinct, scores.Average());
        }

        [AdvancedAnalyticsTool("ComputeDaviesBouldinIndex")]
        [Description(
            "Computes the Davies-Bouldin index of a clustering: for every cluster it finds the most similar other cluster, where similarity is the sum of the two clusters' average distances to their centres divided by the distance between the centres, and it averages those worst case ratios. " +
            "Lower is better and 0 is the ideal (tight, far apart clusters); values above about 1 mean clusters overlap. It uses Euclidean distance to the cluster means, needs at least 2 clusters and at most 100000 rows. Use it to compare clusterings of the same data; it is not comparable across different data sets.")]
        public double ComputeDaviesBouldinIndex(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 100000.")]
            double[][] data,
            [Description(LabelsText)] int[] labels)
        {
            var (mapped, distinct) = Prepare(data, labels, MultivariateLimits.MaximumClusteringRows, 2);
            var centroids = ClusterMath.Centroids(data, mapped, distinct.Length);

            return ClusterMath.DaviesBouldin(data, mapped, distinct.Length, centroids);
        }

        [AdvancedAnalyticsTool("ComputeCalinskiHarabaszIndex")]
        [Description(
            "Computes the Calinski-Harabasz index (variance ratio criterion) of a clustering: the between cluster sum of squares divided by (k minus 1) over the within cluster sum of squares divided by (n minus k). " +
            "Higher is better: it grows when clusters are compact and far apart. It is infinite when every row equals its centroid and NaN when there is one cluster or as many clusters as rows. Compare it across different k on the same data to pick k; it is not comparable across different data sets. Needs at least 2 clusters, at most 100000 rows.")]
        public double ComputeCalinskiHarabaszIndex(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 100000.")]
            double[][] data,
            [Description(LabelsText)] int[] labels)
        {
            var (mapped, distinct) = Prepare(data, labels, MultivariateLimits.MaximumClusteringRows, 2);
            var centroids = ClusterMath.Centroids(data, mapped, distinct.Length);

            return ClusterMath.CalinskiHarabasz(data, mapped, distinct.Length, centroids).CalinskiHarabasz;
        }

        [AdvancedAnalyticsTool("EvaluateClusteringQuality")]
        [Description(
            "Summarises how good a clustering is with the standard internal quality measures at once: inertia (within cluster sum of squared distances to the cluster means, lower is tighter), the between cluster sum of squares, the mean silhouette (between -1 and 1, higher is better; NaN above 5000 rows), the Davies-Bouldin index (lower is better) and the Calinski-Harabasz index (higher is better). " +
            "Use it to judge a clustering produced by any method, including one made outside these tools, and to compare candidate clusterings of the same data. Needs at least 2 clusters, at most 100000 rows.")]
        public ClusterQualityResult EvaluateClusteringQuality(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 100000.")]
            double[][] data,
            [Description(LabelsText)] int[] labels,
            [Description(
                "Distance used by the silhouette only; the other measures use Euclidean distance. Default Euclidean.")]
            VectorDistanceMetric distance = VectorDistanceMetric.Euclidean,
            CancellationToken cancellationToken = default)
        {
            var (mapped, distinct) = Prepare(data, labels, MultivariateLimits.MaximumClusteringRows, 2);
            var centroids = ClusterMath.Centroids(data, mapped, distinct.Length);
            var quality = ClusterMath.CalinskiHarabasz(data, mapped, distinct.Length, centroids);

            var silhouette = data.Length <= MultivariateLimits.MaximumQuadraticRows
                ? ClusterMath.Silhouette(data, mapped, distinct.Length, DistanceFactory.Create(distance),
                    cancellationToken).Average()
                : double.NaN;

            return new ClusterQualityResult(data.Length, distinct.Length,
                ClusterMath.Inertia(data, mapped, centroids), silhouette,
                ClusterMath.DaviesBouldin(data, mapped, distinct.Length, centroids), quality.CalinskiHarabasz,
                quality.Between);
        }

        [AdvancedAnalyticsTool("ComputeKMeansElbowTable")]
        [Description(
            "Helps choose the number of clusters by running k-means for every k from minClusters to maxClusters with the same seed and tabulating, per k, the inertia (always falling as k rises), the mean silhouette, the Davies-Bouldin index and the Calinski-Harabasz index. " +
            "ElbowClusters is the k where the inertia curve bends most sharply (the point furthest below the straight line joining the first and last points); BestClustersBySilhouette, ...ByDaviesBouldin and ...ByCalinskiHarabasz name the k each measure prefers. The measures often disagree, so look for a k several of them support. " +
            "Measures are computed on the standardised data when standardise is true. At most 30 values of k, 20000 rows; the silhouette is NaN above 5000 rows.")]
        public KMeansElbowResult ComputeKMeansElbowTable(
            [Description("Rows of finite numbers, all the same length. At least 3 rows, at most 20000.")]
            double[][] data,
            [Description(
                "Smallest number of clusters to try, at least 1 (k of 1 has no silhouette, Davies-Bouldin or Calinski-Harabasz value, shown as NaN). Default 2.")]
            int minClusters = 2,
            [Description(
                "Largest number of clusters to try, at most 29 above minClusters, 200 and the number of rows minus 1. Default 10.")]
            int maxClusters = 10,
            [Description("Maximum k-means iterations per run, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Whether to z-score every column before clustering. Default true.")]
            bool standardise = true,
            [Description("Seed for the starting points; the same seed is used for every k. Default 1.")]
            int seed = 1,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 3, 20000);

            MultivariateGuard.Bounded(minClusters, nameof(minClusters), 1,
                Math.Min(MultivariateLimits.MaximumClusters, data.Length - 1));

            MultivariateGuard.Bounded(maxClusters, nameof(maxClusters), minClusters,
                Math.Min(Math.Min(MultivariateLimits.MaximumClusters, data.Length - 1), minClusters + 29));

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            var prepared = standardise ? ClusteringTools.Standardised(data) : data;

            var table = new List<KMeansElbowRow>();

            for (var k = minClusters; k <= maxClusters; k++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (labels, iterations) = FitKMeans(prepared, k, maximumIterations, seed, cancellationToken);
                var (mapped, distinct) = ClusterMath.MapLabels(labels, nameof(data), 1);
                var centroids = ClusterMath.Centroids(prepared, mapped, distinct.Length);
                var inertia = ClusterMath.Inertia(prepared, mapped, centroids);
                var present = distinct.Length;

                table.Add(new KMeansElbowRow(
                    k,
                    inertia,
                    present >= 2 && prepared.Length <= MultivariateLimits.MaximumQuadraticRows
                        ? ClusterMath.Silhouette(prepared, mapped, present, Distance.Euclidean,
                            cancellationToken).Average()
                        : double.NaN,
                    present >= 2 ? ClusterMath.DaviesBouldin(prepared, mapped, present, centroids) : double.NaN,
                    present >= 2
                        ? ClusterMath.CalinskiHarabasz(prepared, mapped, present, centroids).CalinskiHarabasz
                        : double.NaN,
                    iterations));
            }

            return new KMeansElbowResult(data.Length, table.ToArray(), Elbow(table),
                BestBy(table, r => r.SilhouetteScore, true),
                BestBy(table, r => r.DaviesBouldinIndex, false), BestBy(table, r => r.CalinskiHarabaszIndex, true));
        }

        [AdvancedAnalyticsTool("ComputeKMeansGapStatistic")]
        [Description(
            "Chooses the number of clusters with the gap statistic: for each k it compares the log of the k-means within cluster dispersion of the real data with its average over several reference data sets drawn uniformly at random inside the bounding box of the data, so a large gap means the data cluster far more than random noise would. " +
            "The table lists per k the log dispersion of the data, the expected log dispersion under the reference, the gap and its standard error. BestClusters is the smallest k whose gap is at least the next gap minus that next standard error (Tibshirani's rule), or the largest k tried if none qualifies. " +
            "The reference sets use the seed so results repeat. Bounded: at most 5000 rows, 15 values of k and 20 reference sets; runs many k-means fits so keep ranges small.")]
        public GapStatisticResult ComputeKMeansGapStatistic(
            [Description("Rows of finite numbers, all the same length. At least 3 rows, at most 5000.")]
            double[][] data,
            [Description("Smallest number of clusters to try, at least 1. Default 1.")]
            int minClusters = 1,
            [Description(
                "Largest number of clusters to try, at most 14 above minClusters and less than the number of rows. Default 6.")]
            int maxClusters = 6,
            [Description("Number of reference data sets, between 1 and 20. Default 10.")]
            int referenceSets = 10,
            [Description("Maximum k-means iterations per fit, between 1 and 1000. Default 50.")]
            int maximumIterations = 50,
            [Description("Whether to z-score every column first. Default true.")]
            bool standardise = true,
            [Description("Seed for both the reference data and the starting points. Default 1.")]
            int seed = 1,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 3, MultivariateLimits.MaximumQuadraticRows);
            MultivariateGuard.Bounded(minClusters, nameof(minClusters), 1, data.Length - 1);

            MultivariateGuard.Bounded(maxClusters, nameof(maxClusters), minClusters,
                Math.Min(data.Length - 1, minClusters + 14));

            MultivariateGuard.Bounded(referenceSets, nameof(referenceSets), 1, 20);

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            var prepared = standardise ? ClusteringTools.Standardised(data) : data;
            var columns = prepared[0].Length;
            var lows = new double[columns];
            var highs = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                lows[j] = prepared.Min(r => r[j]);
                highs[j] = prepared.Max(r => r[j]);
            }

            var references = new List<double[][]>();

            for (var b = 0; b < referenceSets; b++)
            {
                var random = new Random(unchecked(seed * 7919 + b * 104729 + 17));
                var sample = new double[prepared.Length][];

                for (var i = 0; i < sample.Length; i++)
                {
                    sample[i] = new double[columns];

                    for (var j = 0; j < columns; j++)
                    {
                        sample[i][j] = lows[j] + random.NextDouble() * (highs[j] - lows[j]);
                    }
                }

                references.Add(sample);
            }

            var table = new List<GapStatisticRow>();

            for (var k = minClusters; k <= maxClusters; k++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var observed = Math.Log(Dispersion(prepared, k, maximumIterations, seed, cancellationToken));

                var logs = references
                    .Select(r => Math.Log(Dispersion(r, k, maximumIterations, seed, cancellationToken)))
                    .ToArray();

                var expected = logs.Average();
                var deviation = Math.Sqrt(logs.Select(v => (v - expected) * (v - expected)).Average());

                table.Add(new GapStatisticRow(k, observed, expected, expected - observed,
                    deviation * Math.Sqrt(1.0 + 1.0 / referenceSets)));
            }

            var best = table[^1].Clusters;

            for (var i = 0; i < table.Count - 1; i++)
            {
                if (table[i].Gap >= table[i + 1].Gap - table[i + 1].StandardError)
                {
                    best = table[i].Clusters;
                    break;
                }
            }

            return new GapStatisticResult(data.Length, referenceSets, table.ToArray(), best);
        }

        [AdvancedAnalyticsTool("ProfileClusters")]
        [Description(
            "Describes each cluster of an existing labelling in plain numbers so it can be interpreted: size and share of the rows, the centroid (mean of every variable), the standard deviation of every variable, the within cluster sum of squares, the mean and maximum Euclidean distance of members to the centroid, and the variables that most distinguish the cluster. " +
            "DistinguishingVariables lists, for the topVariables variables with the largest effect, the standardised difference (cluster mean minus overall mean, divided by the overall standard deviation): +1.5 means the cluster averages 1.5 standard deviations above everyone else on that variable. A constant variable has effect 0. " +
            "Works with labels from any clustering. A cluster of one row has NaN standard deviations. At most 100000 rows.")]
        public ClusterProfilingResult ProfileClusters(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 100000.")]
            double[][] data,
            [Description(LabelsText)] int[] labels,
            [Description("One unique name per column, in column order. Leave empty to use V0, V1 and so on.")]
            string[]? variableNames = null,
            [Description(
                "How many distinguishing variables to list per cluster, between 1 and the number of columns. Default 3.")]
            int topVariables = 3)
        {
            var (mapped, distinct) = Prepare(data, labels, MultivariateLimits.MaximumClusteringRows, 1);
            var names = MultivariateGuard.Names(variableNames, nameof(variableNames), data[0].Length);
            MultivariateGuard.Bounded(topVariables, nameof(topVariables), 1, data[0].Length);

            var centroids = ClusterMath.Centroids(data, mapped, distinct.Length);
            var profiles = ClusterMath.Profiles(data, mapped, distinct.Length, centroids);
            var means = data.Mean(0);
            var deviations = data.StandardDeviation(means);

            var summaries = profiles.Select(p => new ClusterProfileSummary(
                distinct[p.Cluster], p.Size, p.Proportion, p.Centroid, p.StandardDeviations, p.WithinSumOfSquares,
                p.MeanDistanceToCentroid, p.MaximumDistanceToCentroid,
                Enumerable.Range(0, names.Length)
                    .Select(j => new ClusterVariableEffect(names[j],
                        deviations[j] > 0 ? (p.Centroid[j] - means[j]) / deviations[j] : 0.0))
                    .OrderByDescending(e => Math.Abs(e.StandardisedDifference))
                    .Take(topVariables)
                    .ToArray())).ToArray();

            return new ClusterProfilingResult(data.Length, distinct.Length, names, means, deviations, distinct,
                summaries);
        }

        [AdvancedAnalyticsTool("AssignRowsToNearestCentroid")]
        [Description(
            "Assigns each new row to the nearest of a set of supplied centroids, for example centroids returned by a clustering tool, using the chosen distance. " +
            "It returns, per row, the index of the nearest centroid (the position in the centroids array, starting at 0), the distance to it and the distances to every centroid so borderline rows can be spotted (a small gap between the two smallest distances is an ambiguous case). Rows and centroids must have the same columns and the same scaling: if the clustering was run on standardised data, standardise the new rows the same way. At most 20000 rows and 200 centroids.")]
        public CentroidAssignmentResult AssignRowsToNearestCentroid(
            [Description("Rows to assign, finite numbers, all the same length. At most 20000 rows.")]
            double[][] newData,
            [Description(
                "The centroids, one array of numbers per centroid, with the same number of columns as newData. At most 200.")]
            double[][] centroids,
            [Description("Distance between rows and centroids. Default Euclidean.")]
            VectorDistanceMetric distance = VectorDistanceMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1. Default 3.")]
            double minkowskiOrder = 3)
        {
            MultivariateGuard.Matrix(newData, nameof(newData), 1, 20000);
            MultivariateGuard.Matrix(centroids, nameof(centroids), 1, MultivariateLimits.MaximumClusters);
            ProjectionTools.RequireSameColumns(centroids, newData, nameof(newData));
            var metric = DistanceFactory.Create(distance, minkowskiOrder);

            var labels = new int[newData.Length];
            var nearest = new double[newData.Length];
            var all = new double[newData.Length][];

            for (var i = 0; i < newData.Length; i++)
            {
                all[i] = centroids.Select(c => metric(newData[i], c)).ToArray();
                var best = 0;

                for (var c = 1; c < all[i].Length; c++)
                {
                    if (all[i][c] < all[i][best])
                    {
                        best = c;
                    }
                }

                labels[i] = best;
                nearest[i] = all[i][best];
            }

            return new CentroidAssignmentResult(newData.Length, labels, nearest, all);
        }

        [AdvancedAnalyticsTool("CompareClusterAssignments")]
        [Description(
            "Measures how much two clusterings of the same rows agree, ignoring how the clusters are numbered, using pair counting: every pair of rows is either together or apart in each clustering. " +
            "RandIndex is the share of pairs on which they agree (0 to 1). AdjustedRandIndex corrects that for chance, so 1 means identical partitions, about 0 means no better than random and negative means worse than random; it is the number to quote. PairJaccard is the share of pairs together in either clustering that are together in both. " +
            "Use it to test how stable a clustering is across seeds, methods or samples. Two clusterings that are both a single cluster, or both all singletons, score 1. At most 1000000 rows; at least 2.")]
        public ClusterAgreementResult CompareClusterAssignments(
            [Description("First clustering: one integer label per row.")]
            int[] labelsA,
            [Description("Second clustering: one integer label per row, the same length as labelsA.")]
            int[] labelsB)
        {
            if (labelsA == null || labelsB == null)
            {
                throw new ArgumentException("labelsA and labelsB are both required.", nameof(labelsA));
            }

            if (labelsA.Length != labelsB.Length)
            {
                throw new ArgumentException(
                    $"labelsA has {labelsA.Length} values and labelsB has {labelsB.Length}; they must be the same length.",
                    nameof(labelsB));
            }

            if (labelsA.Length < 2 || labelsA.Length > 1000000)
            {
                throw new ArgumentException("At least 2 and at most 1000000 rows are needed.", nameof(labelsA));
            }

            var (a, distinctA) = ClusterMath.MapLabels(labelsA, nameof(labelsA), 1);
            var (b, distinctB) = ClusterMath.MapLabels(labelsB, nameof(labelsB), 1);
            var table = new Dictionary<(int, int), long>();
            var rowsA = new long[distinctA.Length];
            var rowsB = new long[distinctB.Length];

            for (var i = 0; i < a.Length; i++)
            {
                rowsA[a[i]]++;
                rowsB[b[i]]++;
                table[(a[i], b[i])] = table.GetValueOrDefault((a[i], b[i])) + 1;
            }

            double Pairs(long n)
            {
                return n * (n - 1) / 2.0;
            }

            var both = table.Values.Sum(v => Pairs(v));
            var inA = rowsA.Sum(v => Pairs(v));
            var inB = rowsB.Sum(v => Pairs(v));
            var total = Pairs(a.Length);
            var onlyA = inA - both;
            var onlyB = inB - both;
            var neither = total - both - onlyA - onlyB;
            var expected = inA * inB / total;
            var maximum = 0.5 * (inA + inB);

            return new ClusterAgreementResult(
                a.Length,
                (both + neither) / total,
                maximum - expected == 0 ? 1.0 : (both - expected) / (maximum - expected),
                both + onlyA + onlyB == 0 ? 1.0 : both / (both + onlyA + onlyB),
                distinctA.Length,
                distinctB.Length);
        }

        private static (int[] Mapped, int[] Distinct) Prepare(double[][] data, int[] labels, int maximumRows,
            int minimumClusters)
        {
            MultivariateGuard.Matrix(data, nameof(data), 2, maximumRows);
            MultivariateGuard.Labels(labels, nameof(labels), data.Length, nameof(data));

            return ClusterMath.MapLabels(labels, nameof(labels), minimumClusters);
        }

        private static (int[] Labels, int Iterations) FitKMeans(double[][] prepared, int k, int maximumIterations,
            int seed, CancellationToken token)
        {
            using var scope = new SeededRandomScope(seed);

            var model = new KMeans(k)
            {
                MaxIterations = maximumIterations,
                ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = token }
            };

            var learned = model.Learn(prepared);

            return (learned.Decide(prepared), model.Iterations);
        }

        private static double Dispersion(double[][] data, int k, int maximumIterations, int seed,
            CancellationToken token)
        {
            var (labels, _) = FitKMeans(data, k, maximumIterations, seed, token);
            var (mapped, distinct) = ClusterMath.MapLabels(labels, "data", 1);
            var centroids = ClusterMath.Centroids(data, mapped, distinct.Length);
            var inertia = ClusterMath.Inertia(data, mapped, centroids);

            return Math.Max(inertia, 1e-300);
        }

        private static int Elbow(List<KMeansElbowRow> table)
        {
            if (table.Count < 3)
            {
                return table[0].Clusters;
            }

            var lowest = table.Min(r => r.Inertia);
            var highest = table.Max(r => r.Inertia);

            if (highest - lowest <= 0)
            {
                return table[0].Clusters;
            }

            var best = 0;
            var bestGap = double.NegativeInfinity;

            for (var i = 0; i < table.Count; i++)
            {
                var x = i / (double)(table.Count - 1);
                var y = (table[i].Inertia - lowest) / (highest - lowest);
                var gap = 1.0 - x - y;

                if (gap > bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }

            return table[best].Clusters;
        }

        private static int BestBy(List<KMeansElbowRow> table, Func<KMeansElbowRow, double> measure, bool higherIsBetter)
        {
            var candidates = table.Where(r => !double.IsNaN(measure(r))).ToList();

            if (candidates.Count == 0)
            {
                return table[0].Clusters;
            }

            return (higherIsBetter ? candidates.OrderByDescending(measure) : candidates.OrderBy(measure)).First()
                .Clusters;
        }
    }
}