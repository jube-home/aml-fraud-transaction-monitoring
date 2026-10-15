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
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public static class KNearestNeighboursClassifierTrainer
    {
        public const int MinimumTestRows = 100;
        public const int MinimumTestClassRows = 10;
        public const double NearConstantShare = 0.995;
        public const double CalibrationSlopeLow = 0.7;
        public const double CalibrationSlopeHigh = 1.3;
        public const int SmallSnapshotWarningThreshold = 3;
        public const int LargeSnapshotWarningThreshold = 200_000;

        public static KNearestNeighboursClassifierResult Train(double[][] inputs, int[] labels, string[] variableNames,
            KNearestNeighboursClassifierOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new KNearestNeighboursClassifierOptions();
            options.Validate();
            ValidateData(inputs, labels, variableNames);

            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;

            var split = DataSplit.Split([.. labels.Select(l => (double)l)], trainFraction,
                options.ValidationFraction, options.TestFraction, options.Seed);

            var assigned = new bool[labels.Length];

            foreach (var row in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[row] = true;
            }

            var leftovers = Enumerable.Range(0, labels.Length).Where(i => !assigned[i]).ToArray();

            if (leftovers.Length > 0)
            {
                split = new DataSplitIndices([.. split.Train, .. leftovers], split.Validation, split.Test);
            }

            return TrainWithSplit(inputs, labels, variableNames, options, split, cancellationToken);
        }

        public static KNearestNeighboursClassifierResult TrainWithSplit(double[][] inputs, int[] labels,
            string[] variableNames, KNearestNeighboursClassifierOptions options, DataSplitIndices split,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, labels, variableNames);
            ValidateSplit(split, labels.Length);
            cancellationToken.ThrowIfCancellationRequested();

            var warnings = new List<AdvancedAnalyticsWarning>();
            var variableCount = variableNames.Length;

            var trainRows = split.Train;
            var validationRows = split.Validation;
            var testRows = split.Test;

            var trainX = DataSplit.Take(inputs, trainRows);
            var trainY = DataSplit.Take(labels, trainRows);
            var validationX = DataSplit.Take(inputs, validationRows);
            var validationY = DataSplit.Take(labels, validationRows);
            var testX = DataSplit.Take(inputs, testRows);
            var testY = DataSplit.Take(labels, testRows);

            var trainPositives = trainY.Sum();
            var trainNegatives = trainY.Length - trainPositives;

            if (trainPositives == 0 || trainNegatives == 0)
            {
                throw new ArgumentException("The training split must contain both classes.");
            }

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
                    warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to distances."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.ConstantColumn,
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

            var preliminaryPipeline = new KNearestNeighboursClassifierPipeline(standardise, fill, means, deviations,
                trainDesign, trainY, options.NeighbourCount, null);

            var validationVotes = validationX.Select(preliminaryPipeline.VoteFraction).ToArray();
            var validationPositives = validationY.Sum();

            Calibrator? calibrator = null;
            string calibrationReason = null;
            var calibrationRows = 0;

            if (!options.Calibrate)
            {
                calibrationReason = "Calibration was switched off.";
            }
            else if (validationY.Length == 0 || validationPositives == 0 || validationPositives == validationY.Length)
            {
                calibrationReason = "The validation split does not contain both classes.";

                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the model is uncalibrated."));
            }
            else
            {
                try
                {
                    calibrator = PlattCalibration.Fit(validationVotes, validationY,
                        cancellationToken: cancellationToken);

                    calibrationRows = validationY.Length;
                }
                catch (InvalidOperationException exception)
                {
                    calibrationReason = exception.Message;

                    warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pipeline = new KNearestNeighboursClassifierPipeline(standardise, fill, means, deviations, trainDesign,
                trainY, options.NeighbourCount, calibrator);

            var threshold = options.DecisionThreshold ?? 0.5;

            var trainProbabilities = trainX.Select(pipeline.Probability).ToArray();
            var validationProbabilities = validationX.Select(pipeline.Probability).ToArray();
            var testProbabilities = testX.Select(pipeline.Probability).ToArray();

            var trainMetrics = Metrics("Train", trainProbabilities, trainY, threshold);
            var validationMetrics = Metrics("Validation", validationProbabilities, validationY, threshold);
            var testMetrics = Metrics("Test", testProbabilities, testY, threshold);

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows ||
                     Math.Min(testMetrics.Positives, testMetrics.Rows - testMetrics.Positives) < MinimumTestClassRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows with {testMetrics.Positives} positives; held-out metrics will be noisy."));
            }

            if (calibrator is { } fitted)
            {
                if (!fitted.IsIncreasing)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.CalibratorNotIncreasing,
                        "The calibrator has a non-positive slope: on the validation split the model ranks cases the wrong way round."));
                }

                if (fitted.Slope < CalibrationSlopeLow || fitted.Slope > CalibrationSlopeHigh)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.CalibrationSlopeFar,
                        $"The validation calibration slope is {fitted.Slope:0.###}, far from 1: the raw vote fractions were over- or under-confident."));
                }
            }

            if (options.NeighbourCount <= SmallSnapshotWarningThreshold)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.SmallNeighbourCount,
                    $"The neighbour count is only {options.NeighbourCount}; the vote fraction can only take a few " +
                    "values and will be a noisy, jumpy estimate of probability."));
            }

            if (trainDesign.Length > LargeSnapshotWarningThreshold)
            {
                warnings.Add(new AdvancedAnalyticsWarning(KNearestNeighboursWarningCodes.LargeSnapshot,
                    $"The stored model snapshot carries all {trainDesign.Length} training rows, since recalling a " +
                    "k nearest neighbours model means searching the stored rows themselves; this is expected for this " +
                    "family, but it makes the snapshot and its storage cost grow linearly with the training split."));
            }

            var envelope =
                AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsKNearestNeighboursClassifierPipelineSnapshot>.Create(
                    KNearestNeighboursClassifierRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsKNearestNeighboursClassifierPipelineSnapshot
                    {
                        Standardise = standardise,
                        FillValues = pipeline.Fill,
                        Means = pipeline.Means,
                        StandardDeviations = pipeline.StandardDeviations,
                        Rows = pipeline.Rows,
                        Labels = pipeline.Labels,
                        NeighbourCount = options.NeighbourCount
                    },
                    calibrator.HasValue
                        ? new AdvancedAnalyticsCalibratorSnapshot
                        {
                            Method = KNearestNeighboursClassifierRecall.CalibratorMethod,
                            Parameters = calibrator.Value.ToPrimitives()
                        }
                        : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new KNearestNeighboursClassifierResult
            {
                Kind = KNearestNeighboursClassifierRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                Details = new KNearestNeighboursEvidenceDetails
                {
                    NeighbourCount = options.NeighbourCount,
                    DistanceMetric = "Euclidean",
                    StoredTrainingRows = trainDesign.Length,
                    Calibration = new KNearestNeighboursCalibrationInfo(calibrator.HasValue, calibrator?.Slope,
                        calibrator?.Intercept, calibrationRows, calibrationReason),
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

        private static KNearestNeighboursSplitMetrics Metrics(string split, double[] probabilities, int[] labels,
            double threshold)
        {
            if (labels.Length == 0)
            {
                return null;
            }

            var positives = labels.Sum();
            var bothClasses = positives > 0 && positives < labels.Length;

            double? Number(double value)
            {
                return double.IsFinite(value) ? value : null;
            }

            double? auc = null;
            double? ap = null;

            if (bothClasses)
            {
                auc = Number(BinaryMetrics.RocAuc(probabilities, labels));
                ap = Number(BinaryMetrics.AveragePrecision(probabilities, labels));
            }

            var confusion = BinaryMetrics.Confusion(probabilities, labels, threshold);

            return new KNearestNeighboursSplitMetrics(split, labels.Length, positives, auc, ap,
                Number(BinaryMetrics.LogLoss(probabilities, labels)),
                Number(BinaryMetrics.BrierScore(probabilities, labels)), threshold, confusion.TruePositives,
                confusion.FalsePositives, confusion.TrueNegatives, confusion.FalseNegatives,
                Number(confusion.Precision),
                Number(confusion.Recall), Number(confusion.F1));
        }

        private static void ValidateData(double[][] inputs, int[] labels, string[] variableNames)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(labels);
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

            if (inputs.Length != labels.Length)
            {
                throw new ArgumentException($"There are {inputs.Length} input rows but {labels.Length} labels.");
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

            if (labels.Any(l => l != 0 && l != 1))
            {
                throw new ArgumentException("Labels must be 0 or 1.");
            }

            if (labels.Sum() == 0 || labels.Sum() == labels.Length)
            {
                throw new ArgumentException("Both classes must be present in the labels.");
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