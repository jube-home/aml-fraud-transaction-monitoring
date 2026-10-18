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
using Accord.Statistics.Distributions.DensityKernels;
using Accord.Statistics.Distributions.Fitting;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    [AdvancedAnalyticsToolGroup("MultivariateAndClustering")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ClusteringTools
    {
        private const string DataText =
            "Rows of finite numbers, all the same length. At least as many rows as clusters, at most 100000.";

        private const string StandardiseText =
            "Whether to z-score every column before clustering so variables on large scales do not dominate. Constant columns are left as they are. Reported centroids and profiles are always in the original units. Default true.";

        private const string SeedText = "Seed for the random starting points; reuse it to repeat a run. Default 1.";

        private const string ClusteringText =
            "It returns a cluster label per row (0 to k minus 1), the centroids (cluster means in original units), a profile per cluster (size, share of rows, mean, standard deviation per column, within cluster sum of squares and mean and maximum distance to the centroid), the total inertia (sum of squared distances to centroids, lower is tighter, and it always falls as k rises) and the silhouette score (between -1 and 1, higher means better separated; NaN if there is a single cluster or more than 5000 rows). " +
            "A cluster left empty has size 0 and NaN centroid values.";

        [AdvancedAnalyticsTool("RunKMeansClustering")]
        [Description(
            "Groups rows into k clusters with k-means: starting centres are chosen (k-means++ spreads them apart, Uniform picks random rows), each row is assigned to its nearest centre and centres are moved to the cluster means until assignments stop changing or the iteration limit is reached. " +
            ClusteringText +
            " Bounded by 100000 rows, 200 clusters and 1000 iterations. Converged is false if the iteration limit was reached first. The result depends on the starting points, so fix the seed and compare several seeds before trusting a solution.")]
        public ClusteringResult RunKMeansClustering(
            [Description(DataText)] double[][] data,
            [Description("Number of clusters, from 1 up to 200 and not more than the number of rows.")]
            int clusters,
            [Description(
                "Distance used to assign rows to centres. Default Euclidean; Manhattan, Chebyshev and Cosine are common alternatives.")]
            VectorDistanceMetric distance = VectorDistanceMetric.Euclidean,
            [Description("KMeansPlusPlus (default, spreads the starting centres) or Uniform (random rows).")]
            ClusterSeeding seeding = ClusterSeeding.KMeansPlusPlus,
            [Description("Maximum number of iterations, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance on the change in centres, at least 0. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description(StandardiseText)] bool standardise = true,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            Validate(data, clusters, maximumIterations, tolerance);
            var metric = DistanceFactory.Create(distance);
            var prepared = standardise ? Standardised(data) : data;

            using var scope = new SeededRandomScope(seed);

            var model = new KMeans(clusters, Distance.GetDistance(metric))
            {
                MaxIterations = maximumIterations,
                Tolerance = tolerance,
                UseSeeding = seeding == ClusterSeeding.Uniform ? Seeding.Uniform : Seeding.KMeansPlusPlus,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            var learned = model.Learn(prepared);

            return Finish("KMeans", data, learned.Decide(prepared), model.Iterations,
                model.Iterations < maximumIterations, cancellationToken);
        }

        [AdvancedAnalyticsTool("RunMiniBatchKMeansClustering")]
        [Description(
            "Groups rows into k clusters with mini-batch k-means, a faster variant of k-means that updates the centres from small random batches of rows instead of every row each iteration; use it when there are many rows and exact k-means is slow. " +
            ClusteringText +
            " Results are close to, but not identical with, ordinary k-means and depend on the seed and batch size. Bounded by 100000 rows, 200 clusters and 1000 iterations.")]
        public ClusteringResult RunMiniBatchKMeansClustering(
            [Description(DataText)] double[][] data,
            [Description("Number of clusters, from 1 up to 200 and not more than the number of rows.")]
            int clusters,
            [Description(
                "Rows sampled per iteration, between 1 and the number of rows. Default 100 (or all rows if fewer).")]
            int batchSize = 100,
            [Description("Distance used to assign rows to centres. Default Euclidean.")]
            VectorDistanceMetric distance = VectorDistanceMetric.Euclidean,
            [Description("Maximum number of iterations, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance, at least 0. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description(StandardiseText)] bool standardise = true,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            Validate(data, clusters, maximumIterations, tolerance);
            MultivariateGuard.Bounded(batchSize, nameof(batchSize), 1, MultivariateLimits.MaximumClusteringRows);
            var metric = DistanceFactory.Create(distance);
            var prepared = standardise ? Standardised(data) : data;

            using var scope = new SeededRandomScope(seed);

            var model = new MiniBatchKMeans(clusters, Math.Min(batchSize, data.Length),
                new DelegateDistance(metric))
            {
                MaxIterations = maximumIterations,
                Tolerance = tolerance,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            var learned = model.Learn(prepared);

            return Finish("MiniBatchKMeans", data, learned.Decide(prepared), model.Iterations,
                model.Iterations < maximumIterations, cancellationToken);
        }

        [AdvancedAnalyticsTool("RunBinarySplitClustering")]
        [Description(
            "Groups rows into k clusters by repeatedly splitting the current clusters in two (binary split, the splitting stage of the LBG vector quantisation algorithm), which is deterministic given the seed and gives a hierarchy-like nested partition as k grows. " +
            ClusteringText +
            " Bounded by 100000 rows, 200 clusters and 1000 iterations.")]
        public ClusteringResult RunBinarySplitClustering(
            [Description(DataText)] double[][] data,
            [Description("Number of clusters, from 1 up to 200 and not more than the number of rows.")]
            int clusters,
            [Description("Distance used to assign rows to centres. Default Euclidean.")]
            VectorDistanceMetric distance = VectorDistanceMetric.Euclidean,
            [Description("Maximum number of iterations in each refinement, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance, at least 0. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description(StandardiseText)] bool standardise = true,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            Validate(data, clusters, maximumIterations, tolerance);
            var metric = DistanceFactory.Create(distance);
            var prepared = standardise ? Standardised(data) : data;

            using var scope = new SeededRandomScope(seed);

            var model = new BinarySplit(clusters, new DelegateDistance(metric))
            {
                MaxIterations = maximumIterations,
                Tolerance = tolerance,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            var learned = model.Learn(prepared);

            return Finish("BinarySplit", data, learned.Decide(prepared), model.Iterations,
                model.Iterations < maximumIterations, cancellationToken);
        }

        [AdvancedAnalyticsTool("RunBalancedKMeansClustering")]
        [Description(
            "Groups rows into k clusters of nearly equal size using balanced k-means: rows are assigned to centres subject to a size constraint, so no cluster swallows the rest. " +
            "Use it when you need evenly sized groups, for example to split a portfolio for review workloads; it trades some tightness for balance, so inertia is usually higher than for plain k-means. " +
            ClusteringText +
            " Bounded by 5000 rows because the assignment step is quadratic, 200 clusters and 1000 iterations.")]
        public ClusteringResult RunBalancedKMeansClustering(
            [Description(
                "Rows of finite numbers, all the same length. At least as many rows as clusters, at most 5000.")]
            double[][] data,
            [Description("Number of clusters, from 1 up to 200 and not more than the number of rows.")]
            int clusters,
            [Description("Distance used to assign rows to centres. Default Euclidean.")]
            VectorDistanceMetric distance = VectorDistanceMetric.Euclidean,
            [Description("Maximum number of iterations, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance, at least 0. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description(StandardiseText)] bool standardise = true,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            Validate(data, clusters, maximumIterations, tolerance, MultivariateLimits.MaximumQuadraticRows);
            var metric = DistanceFactory.Create(distance);
            var prepared = standardise ? Standardised(data) : data;

            using var scope = new SeededRandomScope(seed);

            var model = new BalancedKMeans(clusters, new DelegateDistance(metric))
            {
                MaxIterations = maximumIterations,
                Tolerance = tolerance,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            model.Learn(prepared);

            return Finish("BalancedKMeans", data, model.Labels, model.Iterations,
                model.Iterations < maximumIterations, cancellationToken);
        }

        [AdvancedAnalyticsTool("RunKMedoidsClustering")]
        [Description(
            "Groups rows into k clusters around medoids, which are actual rows of the data chosen as cluster centres (k-medoids), so each cluster has a real, explainable representative case and the method tolerates outliers and non Euclidean distances better than k-means. " +
            "It returns the label per row, the row number of each medoid and its values, a profile per cluster, the total distance of rows to their medoids under the chosen metric (lower is tighter) and the silhouette score under the same metric (NaN for a single cluster). " +
            "Cost grows with the square of the row count, so at most 5000 rows, 200 clusters and 1000 iterations are accepted. PamBuild seeding is deterministic; the other seedings use the seed.")]
        public MedoidClusteringResult RunKMedoidsClustering(
            [Description(
                "Rows of finite numbers, all the same length. At least as many rows as clusters, at most 5000.")]
            double[][] data,
            [Description("Number of clusters, from 1 up to 200 and not more than the number of rows.")]
            int clusters,
            [Description(
                "Distance between rows. Default Manhattan, which is robust; Euclidean, Cosine and others are allowed.")]
            VectorDistanceMetric distance = VectorDistanceMetric.Manhattan,
            [Description("PamBuild (default, greedy deterministic build), KMeansPlusPlus or Uniform starting medoids.")]
            MedoidSeeding seeding = MedoidSeeding.PamBuild,
            [Description("Maximum number of iterations, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance, at least 0. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description(StandardiseText)] bool standardise = true,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            Validate(data, clusters, maximumIterations, tolerance, MultivariateLimits.MaximumQuadraticRows);
            var metric = DistanceFactory.Create(distance);
            var prepared = standardise ? Standardised(data) : data;

            using var scope = new SeededRandomScope(seed);

            var model = new KMedoids<double>(clusters, new DelegateDistance(metric))
            {
                MaxIterations = maximumIterations,
                Tolerance = tolerance,
                Initialization = seeding switch
                {
                    MedoidSeeding.KMeansPlusPlus => Seeding.KMeansPlusPlus,
                    MedoidSeeding.Uniform => Seeding.Uniform,
                    _ => Seeding.PamBuild
                },
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            var learned = model.Learn(prepared);
            var labels = learned.Decide(prepared);
            var medoidRows = new int[clusters];

            for (var c = 0; c < clusters; c++)
            {
                var medoid = learned.Centroids[c];
                medoidRows[c] = Array.FindIndex(prepared, row => row.SequenceEqual(medoid));
            }

            var (mapped, distinct) = ClusterMath.MapLabels(labels, nameof(data), 1);
            var count = distinct.Length;
            var centroids = ClusterMath.Centroids(data, mapped, count);
            var total = 0.0;

            for (var i = 0; i < data.Length; i++)
            {
                total += metric(prepared[i], learned.Centroids[labels[i]]);
            }

            var silhouette = count >= 2 && data.Length <= MultivariateLimits.MaximumQuadraticRows
                ? ClusterMath.Mean(ClusterMath.Silhouette(prepared, mapped, count, metric, cancellationToken))
                : double.NaN;

            return new MedoidClusteringResult(data.Length, clusters, labels, medoidRows,
                medoidRows.Select(r => r >= 0 ? data[r] : Array.Empty<double>()).ToArray(),
                ClusterMath.Profiles(data, mapped, count, centroids), total, silhouette, model.Iterations,
                model.Iterations < maximumIterations);
        }

        [AdvancedAnalyticsTool("RunKModesClustering")]
        [Description(
            "Groups rows of categorical values into k clusters with k-modes: each cluster is represented by its mode (the most common category in each column) and rows are assigned to the mode they differ from in the fewest columns (Hamming distance). " +
            "Use it for categorical profiles such as merchant category, channel and country encoded as integer codes; encode text categories to integer codes first. Returns the label per row, the mode of each cluster, cluster sizes, the total number of mismatched columns between rows and their modes (lower is tighter) and whether it converged. " +
            "Bounded by 100000 rows, 200 clusters, 500 columns and 1000 iterations. Uses the seed to choose starting modes.")]
        public ModeClusteringResult RunKModesClustering(
            [Description(
                "Rows of integer category codes, all the same length. At least as many rows as clusters, at most 100000.")]
            int[][] data,
            [Description("Number of clusters, from 1 up to 200 and not more than the number of rows.")]
            int clusters,
            [Description("Maximum number of iterations, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance, at least 0. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("data is required: pass one array of integer codes per row.", nameof(data));
            }

            if (data.Length > MultivariateLimits.MaximumClusteringRows)
            {
                throw new ArgumentException(
                    $"data has {data.Length} rows but at most {MultivariateLimits.MaximumClusteringRows} are accepted.",
                    nameof(data));
            }

            var columns = data[0]?.Length ?? 0;

            if (columns < 1 || columns > MultivariateLimits.MaximumColumns ||
                data.Any(r => r == null || r.Length != columns))
            {
                throw new ArgumentException(
                    $"Every row of data must be present and have the same length, between 1 and {MultivariateLimits.MaximumColumns} columns.",
                    nameof(data));
            }

            MultivariateGuard.Bounded(clusters, nameof(clusters), 1,
                Math.Min(MultivariateLimits.MaximumClusters, data.Length));

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            MultivariateGuard.BoundedDouble(tolerance, nameof(tolerance), 0, 1);

            using var scope = new SeededRandomScope(seed);

            var model = new KModes(clusters)
            {
                MaxIterations = maximumIterations,
                Tolerance = tolerance,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            var learned = model.Learn(data);
            var labels = learned.Decide(data);
            var modes = learned.Centroids;
            var sizes = new int[clusters];
            var mismatches = 0.0;

            for (var i = 0; i < data.Length; i++)
            {
                sizes[labels[i]]++;

                for (var j = 0; j < columns; j++)
                {
                    if (data[i][j] != modes[labels[i]][j])
                    {
                        mismatches++;
                    }
                }
            }

            return new ModeClusteringResult(data.Length, clusters, labels, modes, sizes, mismatches, model.Iterations,
                model.Iterations < maximumIterations);
        }

        [AdvancedAnalyticsTool("RunMeanShiftClustering")]
        [Description(
            "Finds clusters as the peaks (modes) of the data density with mean shift: every row climbs uphill in a smoothed density until it settles on a peak, and rows that reach the same peak form a cluster, so the number of clusters is discovered rather than chosen. " +
            "The bandwidth is the radius of smoothing and is the only setting that matters: small values give many small clusters, large values merge them. With standardise true the bandwidth is in standard deviations, so 0.5 to 2 is a sensible search range. " +
            "It returns the label per row, the mode of each cluster, the cluster sizes and a profile per cluster. Bounded by 5000 rows and 1000 iterations because each row is compared with all the others.")]
        public MeanShiftClusteringResult RunMeanShiftClustering(
            [Description("Rows of finite numbers, all the same length. At least 2 rows, at most 5000.")]
            double[][] data,
            [Description("Smoothing radius, above zero. Default 1 (in standard deviations when standardise is true).")]
            double bandwidth = 1,
            [Description(
                "Gaussian (default) smooth weighting, Uniform (flat window) or Epanechnikov (parabolic window).")]
            MeanShiftKernelKind kernel = MeanShiftKernelKind.Gaussian,
            [Description("Maximum number of iterations per row, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance, above zero. Default 0.001.")]
            double tolerance = 1e-3,
            [Description(StandardiseText)] bool standardise = true,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 2, MultivariateLimits.MaximumQuadraticRows);
            MultivariateGuard.Positive(bandwidth, nameof(bandwidth));
            MultivariateGuard.Positive(tolerance, nameof(tolerance));

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            var prepared = standardise ? Standardised(data) : data;
            var dimension = data[0].Length;

            IRadiallySymmetricKernel function = kernel switch
            {
                MeanShiftKernelKind.Uniform => new UniformKernel(),
                MeanShiftKernelKind.Epanechnikov => new EpanechnikovKernel(dimension),
                _ => new GaussianKernel(dimension)
            };

            var model = new MeanShift(function, bandwidth)
            {
                MaxIterations = maximumIterations,
                Tolerance = tolerance,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            cancellationToken.ThrowIfCancellationRequested();
            var learned = model.Learn(prepared);
            var labels = learned.Decide(prepared);
            var (mapped, distinct) = ClusterMath.MapLabels(labels, nameof(data), 1);
            var count = distinct.Length;
            var centroids = ClusterMath.Centroids(data, mapped, count);
            var profiles = ClusterMath.Profiles(data, mapped, count, centroids);

            return new MeanShiftClusteringResult(data.Length, count, mapped, learned.Modes, profiles,
                profiles.Select(p => p.Size).ToArray(), bandwidth);
        }

        [AdvancedAnalyticsTool("RunGaussianMixtureClustering")]
        [Description(
            "Fits a Gaussian mixture by expectation maximisation: the data are modelled as a blend of k bell shaped groups, each with its own centre and covariance, giving soft clusters with probabilities rather than hard borders and allowing elongated or differently sized groups that k-means cannot represent. " +
            "It returns the most likely component per row, each component's weight (share of the data), mean and covariance matrix, every row's responsibilities (probability of belonging to each component, rows sum to 1), the log-likelihood (higher fits better), the number of free parameters, AIC and BIC (lower is better; BIC penalises complexity more and is the usual choice for picking k). " +
            "Diagonal covariances estimate only variances and are far cheaper for many columns. The regularisation added to the covariance diagonal prevents collapse on tiny or degenerate clusters. Bounded by 50000 rows, 100 columns, 50 components and 1000 iterations; needs at least twice as many rows as components.")]
        public MixtureClusteringResult RunGaussianMixtureClustering(
            [Description("Rows of finite numbers, all the same length, at most 100 columns. At most 50000 rows.")]
            double[][] data,
            [Description("Number of mixture components, from 1 up to 50 and at most half the number of rows.")]
            int components,
            [Description(
                "Full estimates a complete covariance per component; Diagonal estimates variances only. Default Full.")]
            MixtureCovarianceKind covariance = MixtureCovarianceKind.Full,
            [Description("Maximum number of expectation maximisation iterations, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance on the log-likelihood, above zero. Default 0.001.")]
            double tolerance = 1e-3,
            [Description(
                "Value added to the covariance diagonals to keep them invertible, at least 0. Default 0.000001.")]
            double regularization = 1e-6,
            [Description("Whether to return every row's responsibilities. Default true; set false to save space.")]
            bool returnResponsibilities = true,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 2, MultivariateLimits.MaximumMixtureRows, 1, 100);
            MultivariateGuard.Bounded(components, nameof(components), 1, Math.Min(50, Math.Max(1, data.Length / 2)));

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            MultivariateGuard.Positive(tolerance, nameof(tolerance));
            MultivariateGuard.BoundedDouble(regularization, nameof(regularization), 0, 1e6);

            using var scope = new SeededRandomScope(seed);

            var fit = FitMixture(data, components, covariance, maximumIterations, tolerance, regularization,
                cancellationToken);

            var responsibilities = fit.Clusters.Probabilities(data);

            return new MixtureClusteringResult(
                data.Length,
                components,
                fit.Clusters.Decide(data),
                fit.Clusters.Proportions,
                fit.Clusters.Means,
                fit.Clusters.Covariance.Select(ToJagged).ToArray(),
                returnResponsibilities ? responsibilities : Array.Empty<double[]>(),
                fit.LogLikelihood,
                fit.ParameterCount,
                2.0 * fit.ParameterCount - 2.0 * fit.LogLikelihood,
                fit.ParameterCount * Math.Log(data.Length) - 2.0 * fit.LogLikelihood,
                fit.Iterations,
                fit.Iterations < maximumIterations);
        }

        [AdvancedAnalyticsTool("SelectGaussianMixtureComponentsByBic")]
        [Description(
            "Chooses how many Gaussian mixture components the data support by fitting mixtures for every count from minComponents to maxComponents and comparing information criteria. " +
            "The table lists, per count, the log-likelihood, number of free parameters, AIC and BIC (lower is better) and whether the fit converged; BestComponentsByBic and BestComponentsByAic give the winners. BIC penalises extra components more strongly and usually gives the more conservative answer. " +
            "Use it before RunGaussianMixtureClustering to pick k, or to estimate how many distinct customer or transaction populations exist. At most 15 counts are compared; the other bounds are those of RunGaussianMixtureClustering.")]
        public GaussianMixtureSelectionResult SelectGaussianMixtureComponentsByBic(
            [Description("Rows of finite numbers, all the same length, at most 100 columns. At most 50000 rows.")]
            double[][] data,
            [Description("Smallest number of components to try, at least 1. Default 1.")]
            int minComponents = 1,
            [Description(
                "Largest number of components to try, at most min(50, rows divided by 2) and at most 14 above minComponents. Default 6.")]
            int maxComponents = 6,
            [Description("Full or Diagonal covariance for every fit. Default Full.")]
            MixtureCovarianceKind covariance = MixtureCovarianceKind.Full,
            [Description("Maximum iterations for each fit, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Convergence tolerance on the log-likelihood, above zero. Default 0.001.")]
            double tolerance = 1e-3,
            [Description("Value added to covariance diagonals, at least 0. Default 0.000001.")]
            double regularization = 1e-6,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 2, MultivariateLimits.MaximumMixtureRows, 1, 100);
            var ceiling = Math.Min(50, Math.Max(1, data.Length / 2));
            MultivariateGuard.Bounded(minComponents, nameof(minComponents), 1, ceiling);

            MultivariateGuard.Bounded(maxComponents, nameof(maxComponents), minComponents,
                Math.Min(ceiling, minComponents + 14));

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            MultivariateGuard.Positive(tolerance, nameof(tolerance));
            MultivariateGuard.BoundedDouble(regularization, nameof(regularization), 0, 1e6);

            var table = new List<GaussianMixtureSelectionRow>();

            for (var k = minComponents; k <= maxComponents; k++)
            {
                using var scope = new SeededRandomScope(seed);

                var fit = FitMixture(data, k, covariance, maximumIterations, tolerance, regularization,
                    cancellationToken);

                table.Add(new GaussianMixtureSelectionRow(k, fit.LogLikelihood, fit.ParameterCount,
                    2.0 * fit.ParameterCount - 2.0 * fit.LogLikelihood,
                    fit.ParameterCount * Math.Log(data.Length) - 2.0 * fit.LogLikelihood, fit.Iterations,
                    fit.Iterations < maximumIterations));
            }

            return new GaussianMixtureSelectionResult(data.Length, table.ToArray(),
                table.OrderBy(r => r.Bic).First().Components, table.OrderBy(r => r.Aic).First().Components);
        }

        [AdvancedAnalyticsTool("DetectAnomaliesWithGaussianMixture")]
        [Description(
            "Finds unusual rows by fitting a Gaussian mixture to a reference population and flagging new rows whose probability density under it is low. " +
            "It returns each new row's log density (lower means more unusual), its most likely component, the threshold (the log density below which the lowest contamination share of the reference rows fall) and which new rows are flagged. Because a mixture can describe several normal groups, it flags rows that fit none of them, which a single centre Mahalanobis distance cannot do. " +
            "Test rows default to the reference rows. Bounded by 50000 rows, 100 columns, 50 components.")]
        public MixtureAnomalyResult DetectAnomaliesWithGaussianMixture(
            [Description("Reference rows that define normal behaviour, at most 50000 and at most 100 columns.")]
            double[][] referenceData,
            [Description("Rows to score, with the same number of columns. Leave empty to score the reference rows.")]
            double[][]? newData = null,
            [Description(
                "Number of mixture components, from 1 up to 50 and at most half the reference rows. Default 3.")]
            int components = 3,
            [Description(
                "Share of the reference rows treated as abnormal when setting the threshold, between 0.0001 and 0.5. Default 0.01.")]
            double contamination = 0.01,
            [Description("Full or Diagonal covariance. Default Full.")]
            MixtureCovarianceKind covariance = MixtureCovarianceKind.Full,
            [Description("Maximum iterations, between 1 and 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("Value added to covariance diagonals, at least 0. Default 0.000001.")]
            double regularization = 1e-6,
            [Description(SeedText)] int seed = 1,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(referenceData, nameof(referenceData), 4, MultivariateLimits.MaximumMixtureRows, 1,
                100);

            MultivariateGuard.Bounded(components, nameof(components),
                1, Math.Min(50, Math.Max(1, referenceData.Length / 2)));

            MultivariateGuard.BoundedDouble(contamination, nameof(contamination), 0.0001, 0.5);

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            MultivariateGuard.BoundedDouble(regularization, nameof(regularization), 0, 1e6);
            var rows = newData ?? referenceData;

            if (newData != null)
            {
                MultivariateGuard.Matrix(newData, nameof(newData), 1, MultivariateLimits.MaximumMixtureRows, 1, 100);
                ProjectionTools.RequireSameColumns(referenceData, newData, nameof(newData));
            }

            using var scope = new SeededRandomScope(seed);

            var fit = FitMixture(referenceData, components, covariance, maximumIterations, 1e-3, regularization,
                cancellationToken);

            var mixture = fit.Clusters.ToMixtureDistribution();

            var referenceScores = referenceData.Select(r => mixture.LogProbabilityDensityFunction(r)).OrderBy(v => v)
                .ToArray();

            var position = Math.Min(referenceScores.Length - 1,
                (int)Math.Floor(contamination * referenceScores.Length));

            var threshold = referenceScores[position];
            var scores = rows.Select(r => mixture.LogProbabilityDensityFunction(r)).ToArray();
            var flags = scores.Select(s => s < threshold).ToArray();

            return new MixtureAnomalyResult(rows.Length, components, scores, fit.Clusters.Decide(rows), threshold,
                flags,
                Enumerable.Range(0, flags.Length).Where(i => flags[i]).ToArray());
        }

        [AdvancedAnalyticsTool("SelectKMeansPlusPlusSeeds")]
        [Description(
            "Chooses k well spread starting rows with the k-means++ rule: the first is random and each next row is drawn with probability proportional to its squared distance from the nearest already chosen one, which avoids starting several centres inside the same group. " +
            "It returns the row numbers and the values of the chosen rows. Use it to pick diverse representative cases, to sample a spread of examples for review, or to initialise other methods. The same seed gives the same rows. At most 100000 rows and 200 seeds.")]
        public ClusterSeedResult SelectKMeansPlusPlusSeeds(
            [Description(DataText)] double[][] data,
            [Description("How many seed rows to choose, from 1 up to 200 and not more than the number of rows.")]
            int seeds,
            [Description(StandardiseText)] bool standardise = true,
            [Description(SeedText)] int seed = 1)
        {
            MultivariateGuard.Matrix(data, nameof(data), 1, MultivariateLimits.MaximumClusteringRows);

            MultivariateGuard.Bounded(seeds, nameof(seeds), 1,
                Math.Min(MultivariateLimits.MaximumClusters, data.Length));

            var prepared = standardise ? Standardised(data) : data;

            using var scope = new SeededRandomScope(seed);
            var model = new KMeans(seeds);
            model.Clusters.Randomize(prepared, Seeding.KMeansPlusPlus);

            var rows = model.Clusters.Centroids
                .Select(centroid => Array.FindIndex(prepared, row => row.SequenceEqual(centroid))).ToArray();

            return new ClusterSeedResult(data.Length, rows, rows.Select(r => data[r]).ToArray());
        }

        private static void Validate(double[][] data, int clusters, int maximumIterations, double tolerance,
            int maximumRows = MultivariateLimits.MaximumClusteringRows)
        {
            MultivariateGuard.Matrix(data, nameof(data), 1, maximumRows);

            MultivariateGuard.Bounded(clusters, nameof(clusters), 1,
                Math.Min(MultivariateLimits.MaximumClusters, data.Length));

            MultivariateGuard.Bounded(maximumIterations, nameof(maximumIterations), 1,
                MultivariateLimits.MaximumIterations);

            MultivariateGuard.BoundedDouble(tolerance, nameof(tolerance), 0, 1);
        }

        private static ClusteringResult Finish(string algorithm, double[][] data, int[] labels,
            int iterations, bool converged, CancellationToken token)
        {
            var (mapped, distinct) = ClusterMath.MapLabels(labels, nameof(data), 1);
            var count = distinct.Length;
            var centroids = ClusterMath.Centroids(data, mapped, count);

            var silhouette = count >= 2 && data.Length <= MultivariateLimits.MaximumQuadraticRows
                ? ClusterMath.Mean(ClusterMath.Silhouette(data, mapped, count, Distance.Euclidean, token))
                : double.NaN;

            return new ClusteringResult(algorithm, data.Length, count, mapped, centroids,
                ClusterMath.Profiles(data, mapped, count, centroids), ClusterMath.Inertia(data, mapped, centroids),
                silhouette, iterations, converged);
        }

        internal static double[][] Standardised(double[][] data)
        {
            var means = data.Mean(0);
            var deviations = data.StandardDeviation(means);
            var result = new double[data.Length][];

            for (var i = 0; i < data.Length; i++)
            {
                result[i] = new double[means.Length];

                for (var j = 0; j < means.Length; j++)
                {
                    result[i][j] = deviations[j] > 0 ? (data[i][j] - means[j]) / deviations[j] : data[i][j] - means[j];
                }
            }

            return result;
        }

        private static double[][] ToJagged(double[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var columns = matrix.GetLength(1);
            var result = new double[rows][];

            for (var i = 0; i < rows; i++)
            {
                result[i] = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    result[i][j] = matrix[i, j];
                }
            }

            return result;
        }

        private static MixtureFit FitMixture(double[][] data, int components, MixtureCovarianceKind covariance,
            int maximumIterations, double tolerance, double regularization, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var starter = new KMeans(components)
            {
                ComputeCovariances = true,
                ComputeError = true,
                UseSeeding = Seeding.KMeansPlusPlus,
                Tolerance = tolerance,
                ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = token }
            };
            starter.Learn(data);

            var model = new GaussianMixtureModel(starter)
            {
                MaxIterations = maximumIterations,
                Tolerance = tolerance,
                ComputeLogLikelihood = true,
                Options = new NormalOptions
                {
                    Regularization = regularization,
                    Diagonal = covariance == MixtureCovarianceKind.Diagonal
                },
                ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = token }
            };

            var clusters = model.Learn(data);
            var d = data[0].Length;
            var perComponent = covariance == MixtureCovarianceKind.Diagonal ? d : d * (d + 1) / 2;
            var parameters = components * d + components * perComponent + (components - 1);

            return new MixtureFit(clusters, model.LogLikelihood, parameters, model.Iterations);
        }

        private sealed record MixtureFit(
            GaussianClusterCollection Clusters,
            double LogLikelihood,
            int ParameterCount,
            int Iterations);
    }
}