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
using Accord.Statistics.Analysis;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.DimensionReduction.Models;

namespace Jube.Engine.AdvancedAnalytics.DimensionReduction
{
    public static class PrincipalComponentTrainer
    {
        public const int MinimumTestRows = 20;
        public const double NearConstantShare = 0.995;

        public static PrincipalComponentResult Train(double[][] inputs, string[] variableNames,
            PrincipalComponentOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new PrincipalComponentOptions();
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

        public static PrincipalComponentResult TrainWithSplit(double[][] inputs, string[] variableNames,
            PrincipalComponentOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
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

            if (trainX.Length < 2)
            {
                throw new ArgumentException("The training split has fewer than two rows; variance cannot be computed.");
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
                    warnings.Add(new AdvancedAnalyticsWarning(PrincipalComponentWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes no variance."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(PrincipalComponentWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(PrincipalComponentWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            var standardise = options.Standardise;

            double[] Standardised(double[] row)
            {
                var result = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                    result[j] = standardise ? Standardisation.ZScore(value, means[j], deviations[j]) : value - means[j];
                }

                return result;
            }

            var design = new double[trainX.Length][];

            for (var i = 0; i < trainX.Length; i++)
            {
                design[i] = Standardised(trainX[i]);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var analysis = new PrincipalComponentAnalysis();
            analysis.Learn(design);

            cancellationToken.ThrowIfCancellationRequested();

            var available = analysis.Eigenvalues.Length;

            if (available == 0)
            {
                throw new InvalidOperationException(
                    "Principal component analysis produced no components; the training rows have no variance.");
            }

            var eigenvalues = analysis.Eigenvalues;
            var proportions = analysis.ComponentProportions;
            var rawVectors = analysis.ComponentVectors;

            var vectors = new double[available][];

            for (var c = 0; c < available; c++)
            {
                var vector = (double[])rawVectors[c].Clone();
                var largest = 0d;

                foreach (var value in vector)
                {
                    if (Math.Abs(value) > Math.Abs(largest))
                    {
                        largest = value;
                    }
                }

                var sign = largest < 0 ? -1d : 1d;

                for (var j = 0; j < vector.Length; j++)
                {
                    vector[j] *= sign;
                }

                vectors[c] = vector;
            }

            var cumulative = new double[available];
            var running = 0d;

            for (var c = 0; c < available; c++)
            {
                running += proportions[c];
                cumulative[c] = running;
            }

            var retained = available;

            for (var c = 0; c < available; c++)
            {
                if (cumulative[c] >= options.VarianceThreshold)
                {
                    retained = c + 1;
                    break;
                }
            }

            if (options.MaximumComponents is { } cap)
            {
                retained = Math.Min(retained, cap);
            }

            retained = Math.Clamp(retained, 1, available);

            if (options.MaximumComponents.HasValue && cumulative[retained - 1] < options.VarianceThreshold)
            {
                warnings.Add(new AdvancedAnalyticsWarning(PrincipalComponentWarningCodes.VarianceThresholdNotReached,
                    $"Retaining {retained} components under the configured maximum explains {cumulative[retained - 1]:P1} of the variance, short of the {options.VarianceThreshold:P0} threshold."));
            }

            if (trainX.Length <= variableCount)
            {
                warnings.Add(new AdvancedAnalyticsWarning(PrincipalComponentWarningCodes.FewRowsRelativeToVariables,
                    $"The training split has {trainX.Length} rows for {variableCount} variables; only {available} components are identifiable."));
            }

            var retainedVectors = vectors.Take(retained).ToArray();
            var pipeline = new PrincipalComponentPipeline(standardise, fill, means, deviations, retainedVectors);

            double? ReconstructionError(double[][] rows)
            {
                if (rows.Length == 0)
                {
                    return null;
                }

                var total = 0d;

                foreach (var row in rows)
                {
                    var prepared = pipeline.Prepare(row);
                    var scores = pipeline.Project(row);
                    var reconstructed = new double[variableCount];

                    for (var c = 0; c < retained; c++)
                    {
                        var vector = retainedVectors[c];
                        var score = scores[c];

                        for (var j = 0; j < variableCount; j++)
                        {
                            reconstructed[j] += vector[j] * score;
                        }
                    }

                    var sumOfSquares = 0d;

                    for (var j = 0; j < variableCount; j++)
                    {
                        var difference = prepared[j] - reconstructed[j];
                        sumOfSquares += difference * difference;
                    }

                    total += Math.Sqrt(sumOfSquares / variableCount);
                }

                return total / rows.Length;
            }

            var trainReconstruction = ReconstructionError(trainX) ?? 0d;
            var validationReconstruction = ReconstructionError(validationX);
            var testReconstruction = ReconstructionError(testX);

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(PrincipalComponentWarningCodes.NoTestSet,
                    "There is no test split, so there is no held-out reconstruction check; do not promote on training diagnostics alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(PrincipalComponentWarningCodes.TinyTestSet,
                    $"The test split has only {testX.Length} rows; the held-out reconstruction error will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);
            }

            var components = new PrincipalComponentSnapshotComponent[retained];

            for (var c = 0; c < retained; c++)
            {
                components[c] = new PrincipalComponentSnapshotComponent
                {
                    Eigenvalue = eigenvalues[c],
                    Proportion = proportions[c],
                    Eigenvector = retainedVectors[c]
                };
            }

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsPrincipalComponentPipelineSnapshot>.Create(
                PrincipalComponentRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsPrincipalComponentPipelineSnapshot
                {
                    Standardise = standardise,
                    FillValues = pipeline.Fill,
                    Means = pipeline.Means,
                    StandardDeviations = pipeline.StandardDeviations,
                    Components = components
                });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new PrincipalComponentResult
            {
                Kind = PrincipalComponentRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new PrincipalComponentEvidenceDetails
                {
                    AvailableComponents = available,
                    RetainedComponents = retained,
                    VarianceThreshold = options.VarianceThreshold,
                    Eigenvalues = [.. eigenvalues.Take(available)],
                    ExplainedVarianceRatio = [.. proportions.Take(available)],
                    CumulativeVariance = cumulative,
                    TrainReconstructionError = trainReconstruction,
                    ValidationReconstructionError = validationReconstruction,
                    TestReconstructionError = testReconstruction
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
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

            if (inputs.Length < 2)
            {
                throw new ArgumentException("At least two rows are needed to compute variance.");
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