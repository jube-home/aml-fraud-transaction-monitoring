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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.Math.Random;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Clustering.Models;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    public static class BalancedKMeansClustererTrainer
    {
        public const int MinimumTestRows = 20;
        public const double NearConstantShare = 0.995;
        public const int MaximumSilhouetteRows = 2000;
        public const int MaximumTrainingRows = 2000;

        public static BalancedKMeansClustererResult Train(double[][] inputs, string[] variableNames,
            BalancedKMeansClustererOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new BalancedKMeansClustererOptions();
            options.Validate();
            ValidateData(inputs, variableNames, options);

            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;
            var dummyLabels = new double[inputs.Length];

            var split = DataSplit.Split(dummyLabels, trainFraction, options.ValidationFraction, options.TestFraction,
                options.Seed, false);

            var assigned = new bool[inputs.Length];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[row] = true;
            }

            var leftovers = Enumerable.Range(0, inputs.Length).Where(i => !assigned[i]).ToArray();

            if (leftovers.Length > 0)
            {
                split = new DataSplitIndices([.. split.Train, .. leftovers], split.Validation, split.Test);
            }

            return TrainWithSplit(inputs, variableNames, options, split, cancellationToken);
        }

        public static BalancedKMeansClustererResult TrainWithSplit(double[][] inputs, string[] variableNames,
            BalancedKMeansClustererOptions options, DataSplitIndices split,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, variableNames, options);
            ValidateSplit(split, inputs.Length);
            cancellationToken.ThrowIfCancellationRequested();

            var warnings = new List<AdvancedAnalyticsWarning>();
            var variableCount = variableNames.Length;

            var trainRows = split.Train;
            var validationRows = split.Validation;
            var testRows = split.Test;

            var trainX = DataSplit.Take(inputs, trainRows);
            var validationX = DataSplit.Take(inputs, validationRows);
            var testX = DataSplit.Take(inputs, testRows);

            if (trainX.Length < options.Clusters)
            {
                throw new ArgumentException(
                    $"The training split has {trainX.Length} rows but {options.Clusters} clusters were requested.");
            }

            if (trainX.Length > MaximumTrainingRows)
            {
                throw new ArgumentException(
                    $"The training split has {trainX.Length} rows, above the {MaximumTrainingRows} row limit for this " +
                    "family. Balanced k-means solves an assignment problem whose cost grows with the cube of the row " +
                    "count, so very large training splits are impractical; reduce the rows or use plain k-means.");
            }

            var fill = new double[variableCount];
            var means = new double[variableCount];
            var deviations = new double[variableCount];
            var missingCounts = new int[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                var column = new double[trainX.Length];

                for (var i = 0; i < column.Length; i++)
                {
                    column[i] = trainX[i][j];
                }

                var finite = column.Where(double.IsFinite).ToArray();
                missingCounts[j] = column.Length - finite.Length;

                if (finite.Length == 0)
                {
                    throw new ArgumentException(
                        $"Variable '{variableNames[j]}' has no observed value in the training split.");
                }

                fill[j] = Imputer.Fit(column, ImputationStrategy.Median).FillValue;
                var scaler = Standardisation.Fit(column);
                means[j] = scaler.Mean;
                deviations[j] = scaler.StandardDeviation;

                var groups = finite.GroupBy(v => v).Select(g => g.Count()).ToArray();

                if (groups.Length == 1)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(BalancedKMeansWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to distances."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(BalancedKMeansWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }
            }

            var standardise = options.Standardise;

            double[] Design(double[] row)
            {
                var design = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                    design[j] = standardise ? Standardisation.ZScore(value, means[j], deviations[j]) : value;
                }

                return design;
            }

            var trainDesign = trainX.Select(Design).ToArray();

            var distinctTrainRows = trainDesign
                .Select(row => string.Join("|", row.Select(v => v.ToString("R", CultureInfo.InvariantCulture))))
                .Distinct()
                .Count();

            if (distinctTrainRows < options.Clusters)
            {
                throw new ArgumentException(
                    $"The training split has {distinctTrainRows} distinct row(s) but {options.Clusters} clusters were " +
                    "requested. Reduce the number of clusters or provide more varied rows.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var previousSeed = Generator.ThreadSeed;
            KMeansClusterCollection learned;

            var model = new BalancedKMeans(options.Clusters)
            {
                MaxIterations = options.MaximumIterations,
                Tolerance = options.Tolerance,
                UseSeeding = options.Seeding == KMeansSeeding.Uniform ? Seeding.Uniform : Seeding.KMeansPlusPlus,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            try
            {
                Generator.ThreadSeed = options.Seed;
                learned = model.Learn(trainDesign);
            }
            finally
            {
                Generator.ThreadSeed = previousSeed;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var centroidsInDesignSpace = learned.Centroids.Select(c => (double[])c.Clone()).ToArray();
            var trainLabels = model.Labels;

            warnings.Add(new AdvancedAnalyticsWarning(BalancedKMeansWarningCodes.IterationCountUnavailable,
                "The vendored balanced k-means implementation does not report how many assign-then-recentre " +
                "iterations it took, and does not enforce the maximum iterations as a hard stop; it only stops once " +
                "the centroids settle within the tolerance. Convergence is assumed once training returns."));

            var trainXFilled = trainX
                .Select(row => row.Select((v, j) => double.IsFinite(v) ? v : fill[j]).ToArray())
                .ToArray();

            var clusterSummaries = BuildClusterSummaries(options.Clusters, trainLabels, trainXFilled,
                centroidsInDesignSpace, trainDesign, variableCount);

            var trainInertia = clusterSummaries.Sum(s => s.WithinClusterSumOfSquares);

            var trainSizeImbalance = clusterSummaries.Length == 0
                ? 0
                : clusterSummaries.Max(s => s.Size) - clusterSummaries.Min(s => s.Size);

            double? validationInertia = null;
            double? testInertia = null;

            if (validationX.Length > 0)
            {
                var validationDesign = validationX.Select(Design).ToArray();
                validationInertia = TotalNearestDistanceSquared(validationDesign, centroidsInDesignSpace);
            }

            if (testX.Length > 0)
            {
                var testDesign = testX.Select(Design).ToArray();
                testInertia = TotalNearestDistanceSquared(testDesign, centroidsInDesignSpace);
            }

            var trainSilhouette = options.Clusters >= 2 && trainDesign.Length <= MaximumSilhouetteRows
                ? Silhouette(trainDesign, trainLabels, options.Clusters)
                : null;

            double? testSilhouette = null;

            if (testX.Length > 0)
            {
                var testDesign = testX.Select(Design).ToArray();

                if (options.Clusters >= 2 && testDesign.Length <= MaximumSilhouetteRows)
                {
                    var testLabels = AssignNearest(testDesign, centroidsInDesignSpace);
                    testSilhouette = Silhouette(testDesign, testLabels, options.Clusters);
                }
            }

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(BalancedKMeansWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out diagnostics; do not promote on training figures alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(BalancedKMeansWarningCodes.TinyTestSet,
                    $"The test split has {testX.Length} rows; held-out diagnostics will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(BalancedKMeansWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            var snapshotClusters = clusterSummaries.Select(s => new BalancedKMeansSnapshotCluster
            {
                Index = s.Index,
                Proportion = s.Proportion,
                Centroid = (double[])s.Centroid.Clone()
            }).ToArray();

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsBalancedKMeansClustererPipelineSnapshot>
                .Create(
                    BalancedKMeansClustererRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsBalancedKMeansClustererPipelineSnapshot
                    {
                        Standardise = standardise,
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        Centroids = centroidsInDesignSpace,
                        Clusters = snapshotClusters
                    });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new BalancedKMeansClustererResult
            {
                Kind = BalancedKMeansClustererRecall.ModelKind,
                Seed = options.Seed,
                Variables = (string[])variableNames.Clone(),
                Options = options,
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new BalancedKMeansEvidenceDetails
                {
                    Clusters = options.Clusters,
                    TrainSizeImbalance = trainSizeImbalance,
                    TrainInertia = trainInertia,
                    ValidationInertia = validationInertia,
                    TestInertia = testInertia,
                    TrainSilhouette = trainSilhouette,
                    TestSilhouette = testSilhouette,
                    ClusterSummaries = clusterSummaries
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static BalancedKMeansClusterSummary[] BuildClusterSummaries(int clusters, int[] labels,
            double[][] originalRows, double[][] centroidsInDesignSpace, double[][] designRows, int variableCount)
        {
            var sizes = new int[clusters];
            var originalSums = new double[clusters][];
            var withinSumOfSquares = new double[clusters];

            for (var c = 0; c < clusters; c++)
            {
                originalSums[c] = new double[variableCount];
            }

            for (var i = 0; i < labels.Length; i++)
            {
                var c = labels[i];
                sizes[c]++;

                for (var j = 0; j < variableCount; j++)
                {
                    originalSums[c][j] += originalRows[i][j];
                }

                withinSumOfSquares[c] += SquaredDistance(designRows[i], centroidsInDesignSpace[c]);
            }

            var summaries = new BalancedKMeansClusterSummary[clusters];

            for (var c = 0; c < clusters; c++)
            {
                var centroid = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    centroid[j] = sizes[c] == 0 ? double.NaN : originalSums[c][j] / sizes[c];
                }

                summaries[c] = new BalancedKMeansClusterSummary
                {
                    Index = c,
                    Size = sizes[c],
                    Proportion = labels.Length == 0 ? 0d : sizes[c] / (double)labels.Length,
                    Centroid = centroid,
                    WithinClusterSumOfSquares = withinSumOfSquares[c]
                };
            }

            return summaries;
        }

        private static int[] AssignNearest(double[][] rows, double[][] centroids)
        {
            var labels = new int[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var best = 0;
                var bestDistance = SquaredDistance(rows[i], centroids[0]);

                for (var c = 1; c < centroids.Length; c++)
                {
                    var distance = SquaredDistance(rows[i], centroids[c]);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = c;
                    }
                }

                labels[i] = best;
            }

            return labels;
        }

        private static double TotalNearestDistanceSquared(double[][] rows, double[][] centroids)
        {
            var total = 0d;

            foreach (var row in rows)
            {
                var best = SquaredDistance(row, centroids[0]);

                for (var c = 1; c < centroids.Length; c++)
                {
                    best = Math.Min(best, SquaredDistance(row, centroids[c]));
                }

                total += best;
            }

            return total;
        }

        private static double SquaredDistance(double[] a, double[] b)
        {
            var total = 0d;

            for (var i = 0; i < a.Length; i++)
            {
                var d = a[i] - b[i];
                total += d * d;
            }

            return total;
        }

        private static double? Silhouette(double[][] rows, int[] labels, int clusters)
        {
            var sizes = new int[clusters];

            foreach (var c in labels)
            {
                sizes[c]++;
            }

            if (sizes.Count(s => s > 0) < 2)
            {
                return null;
            }

            var scores = new double[rows.Length];
            var sums = new double[clusters];

            for (var i = 0; i < rows.Length; i++)
            {
                Array.Clear(sums, 0, sums.Length);

                for (var j = 0; j < rows.Length; j++)
                {
                    if (i != j)
                    {
                        sums[labels[j]] += Math.Sqrt(SquaredDistance(rows[i], rows[j]));
                    }
                }

                var own = labels[i];

                if (sizes[own] <= 1)
                {
                    scores[i] = 0d;
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
                scores[i] = denominator == 0 ? 0d : (b - a) / denominator;
            }

            return scores.Average();
        }

        private static void ValidateData(double[][] inputs, string[] variableNames,
            BalancedKMeansClustererOptions options)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(variableNames);

            if (variableNames.Length == 0)
            {
                throw new ArgumentException("At least one variable is needed.");
            }

            if (variableNames.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Variable names must not be blank.");
            }

            if (variableNames.Distinct(StringComparer.Ordinal).Count() != variableNames.Length)
            {
                throw new ArgumentException("Variable names must be unique.");
            }

            if (inputs.Length == 0)
            {
                throw new ArgumentException("There are no rows.");
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentException($"Row {i} is null.");
                }

                if (inputs[i].Length != variableNames.Length)
                {
                    throw new ArgumentException(
                        $"Row {i} has {inputs[i].Length} values but there are {variableNames.Length} variable names (rows must all be the same length).");
                }
            }

            if (inputs.Length < options.Clusters)
            {
                throw new ArgumentException(
                    $"There are {inputs.Length} rows but {options.Clusters} clusters were requested.");
            }

            if (inputs.Length > MaximumTrainingRows)
            {
                throw new ArgumentException(
                    $"There are {inputs.Length} rows, above the {MaximumTrainingRows} row limit for this family. " +
                    "Balanced k-means solves an assignment problem whose cost grows with the cube of the row count, " +
                    "so very large row counts are impractical; reduce the rows or use plain k-means.");
            }
        }

        private static void ValidateSplit(DataSplitIndices split, int rowCount)
        {
            var seen = new bool[rowCount];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                if (row < 0 || row >= rowCount)
                {
                    throw new ArgumentException($"Split row {row} is out of range.");
                }

                if (seen[row])
                {
                    throw new ArgumentException($"Split row {row} appears in more than one split.");
                }

                seen[row] = true;
            }
        }
    }
}