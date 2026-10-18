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
using Accord.Math.Distances;
using Accord.Math.Random;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Clustering.Models;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    public static class KModesClustererTrainer
    {
        public const int MinimumTestRows = 20;
        public const double NearConstantShare = 0.995;
        public const int MaximumSilhouetteRows = 2000;

        public static KModesClustererResult Train(double[][] inputs, string[] variableNames,
            KModesClustererOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new KModesClustererOptions();
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

        public static KModesClustererResult TrainWithSplit(double[][] inputs, string[] variableNames,
            KModesClustererOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
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
                var rawColumn = new double[trainX.Length];

                for (var i = 0; i < rawColumn.Length; i++)
                {
                    rawColumn[i] = trainX[i][j];
                }

                var finiteRaw = rawColumn.Where(double.IsFinite).ToArray();
                missingCounts[j] = rawColumn.Length - finiteRaw.Length;

                if (finiteRaw.Length == 0)
                {
                    throw new ArgumentException(
                        $"Variable '{variableNames[j]}' has no observed value in the training split.");
                }

                if (finiteRaw.Any(v => Math.Abs(v - KModesClustererPipeline.Quantise(v)) > 0.0001))
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KModesWarningCodes.NonIntegerValuesRounded,
                        $"Variable '{variableNames[j]}' had at least one non-integer value in the training split; " +
                        "every value is rounded to the nearest whole number and treated as its own category."));
                }

                var column = finiteRaw.Select(KModesClustererPipeline.Quantise).ToArray();

                fill[j] = Imputer.Fit(column, ImputationStrategy.Mode).FillValue;
                var scaler = Standardisation.Fit(column);
                means[j] = scaler.Mean;
                deviations[j] = scaler.StandardDeviation;

                var groups = column.GroupBy(v => v).Select(g => g.Count()).ToArray();

                if (groups.Length == 1)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KModesWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to distances."));
                }
                else if (groups.Max() / (double)column.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KModesWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }
            }

            double[] Design(double[] row)
            {
                var design = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    design[j] = double.IsFinite(row[j]) ? KModesClustererPipeline.Quantise(row[j]) : fill[j];
                }

                return design;
            }

            int[] DesignInt(double[] row, int rowIndex)
            {
                var design = Design(row);
                var result = new int[design.Length];

                for (var j = 0; j < design.Length; j++)
                {
                    var value = design[j];

                    if (value < int.MinValue || value > int.MaxValue)
                    {
                        throw new ArgumentException(
                            $"Row {rowIndex}, variable '{variableNames[j]}' has category code {value:g6} which does " +
                            "not fit a 32-bit integer; K-Modes treats every already-numeric input as its own " +
                            "discrete category and needs small integer-coded categories.");
                    }

                    result[j] = (int)value;
                }

                return result;
            }

            var trainDesign = trainX.Select(Design).ToArray();
            var trainInts = trainX.Select((row, i) => DesignInt(row, i)).ToArray();

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
            KModesClusterCollection<int> learned;

            var model = new KModes<int>(options.Clusters, new SimpleMatchingDistance())
            {
                MaxIterations = options.MaximumIterations,
                Tolerance = options.Tolerance,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            try
            {
                Generator.ThreadSeed = options.Seed;

                var seedingStrategy =
                    options.Seeding == KModesSeeding.Uniform ? Seeding.Uniform : Seeding.KMeansPlusPlus;

                model.Clusters.Randomize(trainInts, seedingStrategy);
                learned = model.Learn(trainInts);
            }
            finally
            {
                Generator.ThreadSeed = previousSeed;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var iterations = model.Iterations;
            var converged = iterations < options.MaximumIterations;
            var modes = learned.Centroids.Select(c => c.Select(v => (double)v).ToArray()).ToArray();
            var trainLabels = learned.Decide(trainInts);

            if (!converged)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KModesWarningCodes.NonConvergence,
                    $"The fit did not converge within {options.MaximumIterations} iterations (tolerance {options.Tolerance:g})."));
            }

            var clusterSummaries = BuildClusterSummaries(options.Clusters, trainLabels, modes, trainDesign);

            foreach (var summary in clusterSummaries.Where(s => s.Size == 0))
            {
                warnings.Add(new AdvancedAnalyticsWarning(KModesWarningCodes.EmptyCluster,
                    $"Cluster {summary.Index} received no training rows."));
            }

            var trainInertia = clusterSummaries.Sum(s => s.WithinClusterMismatchTotal);
            double? validationInertia = null;
            double? testInertia = null;

            if (validationX.Length > 0)
            {
                var validationDesign = validationX.Select(Design).ToArray();
                validationInertia = TotalNearestMismatchCount(validationDesign, modes);
            }

            if (testX.Length > 0)
            {
                var testDesign = testX.Select(Design).ToArray();
                testInertia = TotalNearestMismatchCount(testDesign, modes);
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
                    var testLabels = AssignNearest(testDesign, modes);
                    testSilhouette = Silhouette(testDesign, testLabels, options.Clusters);
                }
            }

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KModesWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out diagnostics; do not promote on training figures alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KModesWarningCodes.TinyTestSet,
                    $"The test split has {testX.Length} rows; held-out diagnostics will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KModesWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the training mode ({fill[j]:g6})."));
                }
            }

            var snapshotClusters = clusterSummaries.Select(s => new KModesSnapshotCluster
            {
                Index = s.Index,
                Proportion = s.Proportion,
                Mode = (double[])s.Mode.Clone()
            }).ToArray();

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsKModesClustererPipelineSnapshot>.Create(
                KModesClustererRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsKModesClustererPipelineSnapshot
                {
                    FillValues = fill,
                    Modes = modes,
                    Clusters = snapshotClusters
                });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new KModesClustererResult
            {
                Kind = KModesClustererRecall.ModelKind,
                Seed = options.Seed,
                Variables = (string[])variableNames.Clone(),
                Options = options,
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new KModesEvidenceDetails
                {
                    Clusters = options.Clusters,
                    Converged = converged,
                    Iterations = iterations,
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

        private static KModesClusterSummary[] BuildClusterSummaries(int clusters, int[] labels, double[][] modes,
            double[][] designRows)
        {
            var sizes = new int[clusters];
            var withinMismatchTotal = new double[clusters];

            for (var i = 0; i < labels.Length; i++)
            {
                var c = labels[i];
                sizes[c]++;
                withinMismatchTotal[c] += MismatchCount(designRows[i], modes[c]);
            }

            var summaries = new KModesClusterSummary[clusters];

            for (var c = 0; c < clusters; c++)
            {
                summaries[c] = new KModesClusterSummary
                {
                    Index = c,
                    Size = sizes[c],
                    Proportion = labels.Length == 0 ? 0d : sizes[c] / (double)labels.Length,
                    Mode = (double[])modes[c].Clone(),
                    WithinClusterMismatchTotal = withinMismatchTotal[c]
                };
            }

            return summaries;
        }

        private static int[] AssignNearest(double[][] rows, double[][] modes)
        {
            var labels = new int[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var best = 0;
                var bestDistance = MismatchCount(rows[i], modes[0]);

                for (var c = 1; c < modes.Length; c++)
                {
                    var distance = MismatchCount(rows[i], modes[c]);

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

        private static double TotalNearestMismatchCount(double[][] rows, double[][] modes)
        {
            var total = 0d;

            foreach (var row in rows)
            {
                var best = MismatchCount(row, modes[0]);

                for (var c = 1; c < modes.Length; c++)
                {
                    best = Math.Min(best, MismatchCount(row, modes[c]));
                }

                total += best;
            }

            return total;
        }

        private static double MismatchCount(double[] a, double[] b)
        {
            var total = 0d;

            for (var i = 0; i < a.Length; i++)
            {
                if (Math.Abs(a[i] - b[i]) > 0.0001)
                {
                    total += 1d;
                }
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
                        sums[labels[j]] += MismatchCount(rows[i], rows[j]);
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

        private static void ValidateData(double[][] inputs, string[] variableNames, KModesClustererOptions options)
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

        private readonly struct SimpleMatchingDistance : IDistance<int[]>
        {
            public double Distance(int[] x, int[] y)
            {
                var sum = 0d;

                for (var i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                    {
                        sum += 1d;
                    }
                }

                return sum;
            }
        }
    }
}