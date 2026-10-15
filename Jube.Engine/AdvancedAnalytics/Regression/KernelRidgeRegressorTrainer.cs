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
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics.Kernels;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public static class KernelRidgeRegressorTrainer
    {
        public const int MinimumTestRows = 30;
        public const double NearConstantShare = 0.995;
        public const int LargeSnapshotWarningThreshold = 200_000;

        public static KernelRidgeRegressorResult Train(double[][] inputs, double[] outputs, string[] variableNames,
            KernelRidgeRegressorOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new KernelRidgeRegressorOptions();
            options.Validate();
            ValidateData(inputs, outputs, variableNames);

            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;

            var split = DataSplit.Split(outputs, trainFraction, options.ValidationFraction, options.TestFraction,
                options.Seed, false);

            var assigned = new bool[outputs.Length];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[row] = true;
            }

            var leftovers = Enumerable.Range(0, outputs.Length).Where(i => !assigned[i]).ToArray();

            if (leftovers.Length > 0)
            {
                split = new DataSplitIndices([.. split.Train, .. leftovers], split.Validation, split.Test);
            }

            return TrainWithSplit(inputs, outputs, variableNames, options, split, cancellationToken);
        }

        public static KernelRidgeRegressorResult TrainWithSplit(double[][] inputs, double[] outputs,
            string[] variableNames, KernelRidgeRegressorOptions options, DataSplitIndices split,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, outputs, variableNames);
            ValidateSplit(split, outputs.Length);
            cancellationToken.ThrowIfCancellationRequested();

            var warnings = new List<AdvancedAnalyticsWarning>();
            var variableCount = variableNames.Length;

            var trainRows = split.Train;
            var validationRows = split.Validation;
            var testRows = split.Test;

            var trainX = DataSplit.Take(inputs, trainRows);
            var trainY = DataSplit.Take(outputs, trainRows);
            var validationX = DataSplit.Take(inputs, validationRows);
            var validationY = DataSplit.Take(outputs, validationRows);
            var testX = DataSplit.Take(inputs, testRows);
            var testY = DataSplit.Take(outputs, testRows);

            if (trainY.Length < 2)
            {
                throw new ArgumentException("The training split has fewer than two rows.");
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
                    warnings.Add(new AdvancedAnalyticsWarning(KernelRidgeRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to the kernel similarity."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KernelRidgeRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }
            }

            double[] Design(double[] row)
            {
                var design = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                    design[j] = Standardisation.ZScore(value, means[j], deviations[j]);
                }

                return design;
            }

            var trainDesign = trainX.Select(Design).ToArray();
            var n = trainDesign.Length;

            cancellationToken.ThrowIfCancellationRequested();

            var gamma = options.Gamma > 0 ? options.Gamma : 1d / variableCount;
            var kernel = BuildKernel(options.Kernel, gamma, options.Degree);
            var lambda = 1d / options.Complexity;

            var matrix = new double[n][];

            for (var i = 0; i < n; i++)
            {
                matrix[i] = new double[n];
            }

            for (var i = 0; i < n; i++)
            {
                for (var j = i; j < n; j++)
                {
                    var value = kernel.Function(trainDesign[i], trainDesign[j]);
                    matrix[i][j] = value;
                    matrix[j][i] = value;
                }

                matrix[i][i] += lambda;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var mean = trainY.Average();
            var centred = trainY.Select(v => v - mean).ToArray();
            var decomposition = new JaggedCholeskyDecomposition(matrix);

            double[] alpha;

            if (decomposition.IsPositiveDefinite)
            {
                alpha = decomposition.Solve(centred);
            }
            else
            {
                alpha = matrix.Solve(centred);

                warnings.Add(new AdvancedAnalyticsWarning(KernelRidgeRegressorWarningCodes.NearSingularSystem,
                    "The kernel Gram matrix plus the ridge penalty was not positive definite, so the dual system " +
                    "was solved with a general linear solve rather than a Cholesky decomposition; the fit may be " +
                    "unstable. A larger ridge penalty (a smaller Complexity) usually restores a well conditioned system."));
            }

            if (alpha.Any(v => !double.IsFinite(v)))
            {
                throw new InvalidOperationException(
                    "The kernel system could not be solved; try a smaller Complexity or a different kernel.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pipeline = new KernelRidgeRegressorPipeline(fill, means, deviations, trainDesign, alpha, mean,
                options.Kernel, gamma, options.Degree);

            var trainPredicted = trainX.Select(pipeline.Predict).ToArray();
            var validationPredicted = validationX.Select(pipeline.Predict).ToArray();
            var testPredicted = testX.Select(pipeline.Predict).ToArray();

            var trainMetrics = Metrics("Train", trainY, trainPredicted, variableCount);
            var validationMetrics = Metrics("Validation", validationY, validationPredicted, variableCount);
            var testMetrics = Metrics("Test", testY, testPredicted, variableCount);

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KernelRidgeRegressorWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KernelRidgeRegressorWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KernelRidgeRegressorWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows, below {MinimumTestRows}; held-out metrics will be noisy."));
            }

            if (n > LargeSnapshotWarningThreshold)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KernelRidgeRegressorWarningCodes.LargeSnapshot,
                    $"The stored model snapshot carries all {n} training rows as support rows, since kernel ridge " +
                    "regression has no sparsity mechanism and recalling it means weighting the kernel similarity of " +
                    "a new row against every stored training row; this is expected for this family, but it makes " +
                    "the snapshot and its storage cost grow directly with the number of training rows kept."));
            }

            var envelope =
                AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsKernelRidgeRegressorPipelineSnapshot>.Create(
                    KernelRidgeRegressorRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsKernelRidgeRegressorPipelineSnapshot
                    {
                        FillValues = pipeline.Fill,
                        Means = pipeline.Means,
                        StandardDeviations = pipeline.StandardDeviations,
                        SupportRows = pipeline.SupportRows,
                        Alpha = pipeline.Alpha,
                        Mean = pipeline.Mean,
                        Kernel = options.Kernel.ToString(),
                        Gamma = gamma,
                        Degree = options.Degree
                    });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new KernelRidgeRegressorResult
            {
                Kind = KernelRidgeRegressorRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                Details = new KernelRidgeRegressorEvidenceDetails
                {
                    Kernel = options.Kernel.ToString(),
                    Complexity = options.Complexity,
                    Gamma = gamma,
                    Degree = options.Kernel == KernelRidgeRegressorKernel.Polynomial ? options.Degree : null,
                    TrainingRowsInExpansion = n,
                    RidgePenalty = lambda,
                    TrainMetrics = trainMetrics,
                    ValidationMetrics = validationMetrics,
                    TestMetrics = testMetrics
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static IKernel<double[]> BuildKernel(KernelRidgeRegressorKernel kernel, double gamma, int degree)
        {
            return kernel switch
            {
                KernelRidgeRegressorKernel.Polynomial => new Polynomial(degree, 1d),
                KernelRidgeRegressorKernel.Sigmoid => new Sigmoid(gamma, 0d),
                KernelRidgeRegressorKernel.Laplacian => new Laplacian(Math.Sqrt(1d / (2d * gamma))),
                KernelRidgeRegressorKernel.Linear => new Linear(0d),
                _ => Gaussian.FromGamma(gamma)
            };
        }

        private static KernelRidgeRegressorSplitMetrics Metrics(string split, double[] actual, double[] predicted,
            int predictorCount)
        {
            if (actual.Length == 0)
            {
                return null;
            }

            var n = actual.Length;
            var mean = actual.Average();
            var sse = 0d;
            var sst = 0d;
            var sae = 0d;
            var sumError = 0d;

            for (var i = 0; i < n; i++)
            {
                var e = actual[i] - predicted[i];
                sse += e * e;
                sst += (actual[i] - mean) * (actual[i] - mean);
                sae += Math.Abs(e);
                sumError += e;
            }

            double? rSquared = sst > 0 ? 1d - sse / sst : null;
            var denominatorDf = n - predictorCount - 1;

            double? adjustedRSquared = rSquared.HasValue && denominatorDf > 0
                ? 1d - (1d - rSquared.Value) * (n - 1) / denominatorDf
                : null;

            return new KernelRidgeRegressorSplitMetrics(split, n, rSquared, adjustedRSquared, Math.Sqrt(sse / n),
                sae / n, sumError / n);
        }

        private static void ValidateData(double[][] inputs, double[] outputs, string[] variableNames)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(outputs);
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

            if (inputs.Length != outputs.Length)
            {
                throw new ArgumentException($"There are {inputs.Length} input rows but {outputs.Length} outputs.");
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

            if (outputs.Any(o => !double.IsFinite(o)))
            {
                throw new ArgumentException("Outputs must be finite numbers.");
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