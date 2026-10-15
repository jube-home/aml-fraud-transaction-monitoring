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
    public static class KMedoidsClustererTrainer
    {
        public const int MinimumTestRows = 20;
        public const double NearConstantShare = 0.995;
        public const int MaximumSilhouetteRows = 2000;

        public static KMedoidsClustererResult Train(double[][] inputs, string[] variableNames,
            KMedoidsClustererOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new KMedoidsClustererOptions();
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

        public static KMedoidsClustererResult TrainWithSplit(double[][] inputs, string[] variableNames,
            KMedoidsClustererOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
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
                    warnings.Add(new AdvancedAnalyticsWarning(KMedoidsWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to distances."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KMedoidsWarningCodes.ConstantColumn,
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
            KMedoidsClusterCollection<double> learned;

            var model = new KMedoids(options.Clusters)
            {
                MaxIterations = options.MaximumIterations,
                Initialization = options.Seeding == KMedoidsSeeding.Uniform ? Seeding.Uniform : Seeding.PamBuild,
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

            var iterations = model.Iterations;
            var converged = iterations < options.MaximumIterations;
            var rawMedoids = learned.Centroids;

            var medoidTrainIndices = new int[options.Clusters];

            for (var c = 0; c < options.Clusters; c++)
            {
                var index = Array.FindIndex(trainDesign, row => ReferenceEquals(row, rawMedoids[c]));

                if (index < 0)
                {
                    throw new InvalidOperationException("Could not locate a fitted medoid among the training rows.");
                }

                medoidTrainIndices[c] = index;
            }

            var medoidsInDesignSpace = rawMedoids.Select(c => (double[])c.Clone()).ToArray();
            var trainLabels = learned.Decide(trainDesign);

            if (!converged)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KMedoidsWarningCodes.NonConvergence,
                    $"The fit did not converge within {options.MaximumIterations} iterations."));
            }

            var trainXFilled = trainX
                .Select(row => row.Select((v, j) => double.IsFinite(v) ? v : fill[j]).ToArray())
                .ToArray();

            var medoidsInOriginalUnits = medoidTrainIndices.Select(i => trainXFilled[i]).ToArray();

            var clusterSummaries = BuildClusterSummaries(options.Clusters, trainLabels, medoidsInOriginalUnits,
                medoidsInDesignSpace, trainDesign);

            foreach (var summary in clusterSummaries.Where(s => s.Size == 0))
            {
                warnings.Add(new AdvancedAnalyticsWarning(KMedoidsWarningCodes.EmptyCluster,
                    $"Cluster {summary.Index} received no training rows."));
            }

            var trainTotalDistance = clusterSummaries.Sum(s => s.TotalDistanceToMedoid);
            double? validationTotalDistance = null;
            double? testTotalDistance = null;

            if (validationX.Length > 0)
            {
                var validationDesign = validationX.Select(Design).ToArray();
                validationTotalDistance = TotalNearestDistance(validationDesign, medoidsInDesignSpace);
            }

            if (testX.Length > 0)
            {
                var testDesign = testX.Select(Design).ToArray();
                testTotalDistance = TotalNearestDistance(testDesign, medoidsInDesignSpace);
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
                    var testLabels = AssignNearest(testDesign, medoidsInDesignSpace);
                    testSilhouette = Silhouette(testDesign, testLabels, options.Clusters);
                }
            }

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KMedoidsWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out diagnostics; do not promote on training figures alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KMedoidsWarningCodes.TinyTestSet,
                    $"The test split has {testX.Length} rows; held-out diagnostics will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KMedoidsWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            var snapshotClusters = clusterSummaries.Select(s => new KMedoidsSnapshotCluster
            {
                Index = s.Index,
                Proportion = s.Proportion,
                Medoid = (double[])s.Medoid.Clone()
            }).ToArray();

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsKMedoidsClustererPipelineSnapshot>.Create(
                KMedoidsClustererRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsKMedoidsClustererPipelineSnapshot
                {
                    Standardise = standardise,
                    FillValues = fill,
                    Means = means,
                    StandardDeviations = deviations,
                    Medoids = medoidsInDesignSpace,
                    Clusters = snapshotClusters
                });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new KMedoidsClustererResult
            {
                Kind = KMedoidsClustererRecall.ModelKind,
                Seed = options.Seed,
                Variables = (string[])variableNames.Clone(),
                Options = options,
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new KMedoidsEvidenceDetails
                {
                    Clusters = options.Clusters,
                    Converged = converged,
                    Iterations = iterations,
                    TrainTotalDistance = trainTotalDistance,
                    ValidationTotalDistance = validationTotalDistance,
                    TestTotalDistance = testTotalDistance,
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

        private static KMedoidsClusterSummary[] BuildClusterSummaries(int clusters, int[] labels,
            double[][] medoidsInOriginalUnits, double[][] medoidsInDesignSpace, double[][] designRows)
        {
            var sizes = new int[clusters];
            var totalDistances = new double[clusters];

            for (var i = 0; i < labels.Length; i++)
            {
                var c = labels[i];
                sizes[c]++;
                totalDistances[c] += Distance(designRows[i], medoidsInDesignSpace[c]);
            }

            var summaries = new KMedoidsClusterSummary[clusters];

            for (var c = 0; c < clusters; c++)
            {
                summaries[c] = new KMedoidsClusterSummary
                {
                    Index = c,
                    Size = sizes[c],
                    Proportion = labels.Length == 0 ? 0d : sizes[c] / (double)labels.Length,
                    Medoid = (double[])medoidsInOriginalUnits[c].Clone(),
                    TotalDistanceToMedoid = totalDistances[c]
                };
            }

            return summaries;
        }

        private static int[] AssignNearest(double[][] rows, double[][] medoids)
        {
            var labels = new int[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var best = 0;
                var bestDistance = Distance(rows[i], medoids[0]);

                for (var c = 1; c < medoids.Length; c++)
                {
                    var distance = Distance(rows[i], medoids[c]);

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

        private static double TotalNearestDistance(double[][] rows, double[][] medoids)
        {
            var total = 0d;

            foreach (var row in rows)
            {
                var best = Distance(row, medoids[0]);

                for (var c = 1; c < medoids.Length; c++)
                {
                    best = Math.Min(best, Distance(row, medoids[c]));
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

        private static double Distance(double[] a, double[] b)
        {
            return Math.Sqrt(SquaredDistance(a, b));
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
                        sums[labels[j]] += Distance(rows[i], rows[j]);
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

        private static void ValidateData(double[][] inputs, string[] variableNames, KMedoidsClustererOptions options)
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