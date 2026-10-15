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
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public static class KNearestNeighboursRegressorTrainer
    {
        public const int MinimumTestRows = 30;
        public const double NearConstantShare = 0.995;
        public const int SmallNeighbourCountWarningThreshold = 3;
        public const int LargeSnapshotWarningThreshold = 200_000;

        public static KNearestNeighboursRegressorResult Train(double[][] inputs, double[] outputs,
            string[] variableNames,
            KNearestNeighboursRegressorOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new KNearestNeighboursRegressorOptions();
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

        public static KNearestNeighboursRegressorResult TrainWithSplit(double[][] inputs, double[] outputs,
            string[] variableNames, KNearestNeighboursRegressorOptions options, DataSplitIndices split,
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

            if (trainX.Length < options.NeighbourCount)
            {
                throw new ArgumentException(
                    $"The training split has {trainX.Length} row(s) but a neighbour count of {options.NeighbourCount} " +
                    "was requested; either lower the neighbour count or supply more rows.");
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
                    warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to distances."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursRegressorWarningCodes.ConstantColumn,
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
            var trainingMeanTarget = trainY.Average();

            cancellationToken.ThrowIfCancellationRequested();

            var pipeline = new KNearestNeighboursRegressorPipeline(standardise, fill, means, deviations, trainDesign,
                trainY, options.NeighbourCount, trainingMeanTarget);

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
                    warnings.Add(new AdvancedAnalyticsWarning(
                        KNearestNeighboursRegressorWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            if (trainMetrics is not { RSquared: not null })
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursRegressorWarningCodes.NoVariation,
                    "The target has no variation in the training split, so R squared is undefined."));
            }

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursRegressorWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursRegressorWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows, below {MinimumTestRows}; held-out metrics will be noisy."));
            }

            if (options.NeighbourCount <= SmallNeighbourCountWarningThreshold)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursRegressorWarningCodes.SmallNeighbourCount,
                    $"The neighbour count is only {options.NeighbourCount}; the averaged prediction is formed from " +
                    "very few targets and will be a noisy, jumpy estimate."));
            }

            if (trainDesign.Length > LargeSnapshotWarningThreshold)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursRegressorWarningCodes.LargeSnapshot,
                    $"The stored model snapshot carries all {trainDesign.Length} training rows, since recalling a " +
                    "k nearest neighbours model means searching the stored rows themselves; this is expected for this " +
                    "family, but it makes the snapshot and its storage cost grow linearly with the training split."));
            }

            var envelope =
                AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsKNearestNeighboursRegressorPipelineSnapshot>.Create(
                    KNearestNeighboursRegressorRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsKNearestNeighboursRegressorPipelineSnapshot
                    {
                        Standardise = standardise,
                        FillValues = pipeline.Fill,
                        Means = pipeline.Means,
                        StandardDeviations = pipeline.StandardDeviations,
                        Rows = pipeline.Rows,
                        Targets = pipeline.Targets,
                        NeighbourCount = options.NeighbourCount,
                        TrainingMeanTarget = trainingMeanTarget
                    });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new KNearestNeighboursRegressorResult
            {
                Kind = KNearestNeighboursRegressorRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                Details = new KNearestNeighboursRegressorEvidenceDetails
                {
                    NeighbourCount = options.NeighbourCount,
                    DistanceMetric = "Euclidean",
                    StoredTrainingRows = trainDesign.Length,
                    TrainingMeanTarget = trainingMeanTarget,
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

        private static KNearestNeighboursRegressorSplitMetrics Metrics(string split, double[] actual,
            double[] predicted,
            int variableCount)
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
            var denominatorDf = n - variableCount - 1;

            double? adjustedRSquared = rSquared.HasValue && denominatorDf > 0
                ? 1d - (1d - rSquared.Value) * (n - 1) / denominatorDf
                : null;

            return new KNearestNeighboursRegressorSplitMetrics(split, n, rSquared, adjustedRSquared,
                Math.Sqrt(sse / n), sae / n, sumError / n);
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