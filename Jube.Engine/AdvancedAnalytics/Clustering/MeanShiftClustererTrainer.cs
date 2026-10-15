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
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.Statistics.Distributions.DensityKernels;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Clustering.Models;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    public static class MeanShiftClustererTrainer
    {
        public const int MinimumTestRows = 20;
        public const double NearConstantShare = 0.995;
        public const int MaximumSilhouetteRows = 2000;
        public const double ManyModesShare = 0.5;
        public const int ManyModesMinimumRows = 10;

        public static MeanShiftClustererResult Train(double[][] inputs, string[] variableNames,
            MeanShiftClustererOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new MeanShiftClustererOptions();
            options.Validate();
            ValidateData(inputs, variableNames);

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

        public static MeanShiftClustererResult TrainWithSplit(double[][] inputs, string[] variableNames,
            MeanShiftClustererOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, variableNames);
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

            if (trainX.Length == 0)
            {
                throw new ArgumentException("The training split has no rows.");
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
                    warnings.Add(new AdvancedAnalyticsWarning(MeanShiftWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to distances."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(MeanShiftWarningCodes.ConstantColumn,
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

            cancellationToken.ThrowIfCancellationRequested();

            var model = new MeanShift(KernelOf(options.Kernel, variableCount), options.Bandwidth)
            {
                MaxIterations = options.MaximumIterations,
                Tolerance = options.Tolerance,
                Maximum = options.MaxNeighbours,
                ParallelOptions = new ParallelOptions
                    { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }
            };

            var learned = model.Learn(trainDesign);

            cancellationToken.ThrowIfCancellationRequested();

            var modesInDesignSpace = learned.Modes.Select(c => (double[])c.Clone()).ToArray();
            var trainLabels = learned.Decide(trainDesign);

            var trainXFilled = trainX
                .Select(row => row.Select((v, j) => double.IsFinite(v) ? v : fill[j]).ToArray())
                .ToArray();

            var modeSummaries = BuildModeSummaries(modesInDesignSpace.Length, trainLabels, trainXFilled,
                modesInDesignSpace, trainDesign, variableCount, standardise, means, deviations);

            foreach (var summary in modeSummaries.Where(s => s.Size == 0))
            {
                warnings.Add(new AdvancedAnalyticsWarning(MeanShiftWarningCodes.EmptyMode,
                    $"Mode {summary.Index} received no training rows."));
            }

            if (modesInDesignSpace.Length == 1 && trainX.Length > 1)
            {
                warnings.Add(new AdvancedAnalyticsWarning(MeanShiftWarningCodes.SingleMode,
                    $"The fit collapsed to a single mode for {trainX.Length} training rows; consider a smaller " +
                    "bandwidth if more than one group is expected."));
            }
            else if (trainX.Length >= ManyModesMinimumRows &&
                     modesInDesignSpace.Length >= ManyModesShare * trainX.Length)
            {
                warnings.Add(new AdvancedAnalyticsWarning(MeanShiftWarningCodes.ManyModes,
                    $"The fit produced {modesInDesignSpace.Length} modes for {trainX.Length} training rows; consider " +
                    "a larger bandwidth if this many distinct groups is unexpected."));
            }

            var trainInertia = modeSummaries.Sum(s => s.WithinModeSumOfSquares);
            double? validationInertia = null;
            double? testInertia = null;

            if (validationX.Length > 0)
            {
                var validationDesign = validationX.Select(Design).ToArray();
                validationInertia = TotalNearestDistanceSquared(validationDesign, modesInDesignSpace);
            }

            if (testX.Length > 0)
            {
                var testDesign = testX.Select(Design).ToArray();
                testInertia = TotalNearestDistanceSquared(testDesign, modesInDesignSpace);
            }

            var trainSilhouette = modesInDesignSpace.Length >= 2 && trainDesign.Length <= MaximumSilhouetteRows
                ? Silhouette(trainDesign, trainLabels, modesInDesignSpace.Length)
                : null;

            double? testSilhouette = null;

            if (testX.Length > 0)
            {
                var testDesign = testX.Select(Design).ToArray();

                if (modesInDesignSpace.Length >= 2 && testDesign.Length <= MaximumSilhouetteRows)
                {
                    var testLabels = AssignNearest(testDesign, modesInDesignSpace);
                    testSilhouette = Silhouette(testDesign, testLabels, modesInDesignSpace.Length);
                }
            }

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(MeanShiftWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out diagnostics; do not promote on training figures alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(MeanShiftWarningCodes.TinyTestSet,
                    $"The test split has {testX.Length} rows; held-out diagnostics will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(MeanShiftWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            var snapshotClusters = modeSummaries.Select(s => new MeanShiftSnapshotMode
            {
                Index = s.Index,
                Proportion = s.Proportion,
                Centroid = (double[])s.Centroid.Clone()
            }).ToArray();

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsMeanShiftClustererPipelineSnapshot>.Create(
                MeanShiftClustererRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsMeanShiftClustererPipelineSnapshot
                {
                    Standardise = standardise,
                    FillValues = fill,
                    Means = means,
                    StandardDeviations = deviations,
                    Modes = modesInDesignSpace,
                    Clusters = snapshotClusters
                });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new MeanShiftClustererResult
            {
                Kind = MeanShiftClustererRecall.ModelKind,
                Seed = options.Seed,
                Variables = (string[])variableNames.Clone(),
                Options = options,
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new MeanShiftEvidenceDetails
                {
                    Bandwidth = options.Bandwidth,
                    Kernel = options.Kernel,
                    MaxNeighbours = options.MaxNeighbours,
                    Modes = modesInDesignSpace.Length,
                    TrainInertia = trainInertia,
                    ValidationInertia = validationInertia,
                    TestInertia = testInertia,
                    TrainSilhouette = trainSilhouette,
                    TestSilhouette = testSilhouette,
                    ModeSummaries = modeSummaries
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static IRadiallySymmetricKernel KernelOf(MeanShiftKernel kernel, int dimension)
        {
            return kernel switch
            {
                MeanShiftKernel.Uniform => new UniformKernel(),
                MeanShiftKernel.Gaussian => new GaussianKernel(dimension),
                MeanShiftKernel.Epanechnikov => new EpanechnikovKernel(dimension),
                _ => throw new ArgumentException("Kernel must be one of the recognised kernel functions.")
            };
        }

        private static MeanShiftModeSummary[] BuildModeSummaries(int modes, int[] labels, double[][] originalRows,
            double[][] modesInDesignSpace, double[][] designRows, int variableCount, bool standardise, double[] means,
            double[] deviations)
        {
            var sizes = new int[modes];
            var originalSums = new double[modes][];
            var withinSumOfSquares = new double[modes];

            for (var c = 0; c < modes; c++)
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

                withinSumOfSquares[c] += SquaredDistance(designRows[i], modesInDesignSpace[c]);
            }

            var summaries = new MeanShiftModeSummary[modes];

            for (var c = 0; c < modes; c++)
            {
                var centroid = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    centroid[j] = sizes[c] == 0
                        ? OriginalUnitsOf(modesInDesignSpace[c][j], standardise, means[j], deviations[j])
                        : originalSums[c][j] / sizes[c];
                }

                summaries[c] = new MeanShiftModeSummary
                {
                    Index = c,
                    Size = sizes[c],
                    Proportion = labels.Length == 0 ? 0d : sizes[c] / (double)labels.Length,
                    Centroid = centroid,
                    WithinModeSumOfSquares = withinSumOfSquares[c]
                };
            }

            return summaries;
        }

        private static double OriginalUnitsOf(double designValue, bool standardise, double mean, double deviation)
        {
            return standardise ? mean + designValue * deviation : designValue;
        }

        private static int[] AssignNearest(double[][] rows, double[][] modes)
        {
            var labels = new int[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var best = 0;
                var bestDistance = SquaredDistance(rows[i], modes[0]);

                for (var c = 1; c < modes.Length; c++)
                {
                    var distance = SquaredDistance(rows[i], modes[c]);

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

        private static double TotalNearestDistanceSquared(double[][] rows, double[][] modes)
        {
            var total = 0d;

            foreach (var row in rows)
            {
                var best = SquaredDistance(row, modes[0]);

                for (var c = 1; c < modes.Length; c++)
                {
                    best = Math.Min(best, SquaredDistance(row, modes[c]));
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

        private static double? Silhouette(double[][] rows, int[] labels, int modes)
        {
            var sizes = new int[modes];

            foreach (var c in labels)
            {
                sizes[c]++;
            }

            if (sizes.Count(s => s > 0) < 2)
            {
                return null;
            }

            var scores = new double[rows.Length];
            var sums = new double[modes];

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

                for (var c = 0; c < modes; c++)
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

        private static void ValidateData(double[][] inputs, string[] variableNames)
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