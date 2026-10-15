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
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Engine.AdvancedAnalytics.Anomaly
{
    public static class MahalanobisAnomalyTrainer
    {
        public const int MinimumTestRows = 50;
        public const double NearConstantShare = 0.995;
        public const double MinimumRowsPerVariableRatio = 5d;

        public static MahalanobisAnomalyResult Train(double[][] inputs, int[] labels, string[] variableNames,
            MahalanobisAnomalyOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new MahalanobisAnomalyOptions();
            options.Validate();
            ValidateData(inputs, labels, variableNames);

            var hasLabels = labels != null;
            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;
            var splitLabels = hasLabels ? labels!.Select(l => (double)l).ToArray() : new double[inputs.Length];

            var split = DataSplit.Split(splitLabels, trainFraction, options.ValidationFraction, options.TestFraction,
                options.Seed, hasLabels);

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

            return TrainWithSplit(inputs, labels, variableNames, options, split, cancellationToken);
        }

        public static MahalanobisAnomalyResult TrainWithSplit(double[][] inputs, int[] labels, string[] variableNames,
            MahalanobisAnomalyOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(split);
            options.Validate();
            ValidateData(inputs, labels, variableNames);
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

            if (trainX.Length <= variableCount)
            {
                throw new ArgumentException("The training split has no more rows than there are variables.");
            }

            var fill = new double[variableCount];
            var means = new double[variableCount];
            var deviations = new double[variableCount];
            var excluded = new string[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                var column = new double[trainX.Length];

                for (var i = 0; i < column.Length; i++)
                {
                    column[i] = trainX[i][j];
                }

                var finite = column.Where(double.IsFinite).ToArray();

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
                    excluded[j] = "Constant";

                    warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split and was excluded from the covariance."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.NearConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows; its contribution to the distance will be unstable."));
                }
            }

            var activeInputs = Enumerable.Range(0, variableCount).Where(j => excluded[j] == null).ToArray();

            if (activeInputs.Length == 0)
            {
                throw new ArgumentException(
                    "Every variable is constant in the training split; there is nothing to fit.");
            }

            var standardise = options.Standardise;

            double[] Design(double[] row)
            {
                var design = new double[activeInputs.Length];

                for (var k = 0; k < activeInputs.Length; k++)
                {
                    var j = activeInputs[k];
                    var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                    design[k] = standardise ? Standardisation.ZScore(value, means[j], deviations[j]) : value;
                }

                return design;
            }

            var trainDesign = trainX.Select(Design).ToArray();
            cancellationToken.ThrowIfCancellationRequested();

            var d = activeInputs.Length;
            var centre = new double[d];

            foreach (var row in trainDesign)
            {
                for (var k = 0; k < d; k++)
                {
                    centre[k] += row[k] / trainDesign.Length;
                }
            }

            var covariance = new double[d][];

            for (var a = 0; a < d; a++)
            {
                covariance[a] = new double[d];
            }

            foreach (var row in trainDesign)
            {
                for (var a = 0; a < d; a++)
                {
                    var da = row[a] - centre[a];

                    for (var b = a; b < d; b++)
                    {
                        covariance[a][b] += da * (row[b] - centre[b]);
                    }
                }
            }

            var denominator = Math.Max(1, trainDesign.Length - 1);

            for (var a = 0; a < d; a++)
            {
                for (var b = a; b < d; b++)
                {
                    covariance[a][b] /= denominator;
                    covariance[b][a] = covariance[a][b];
                }
            }

            var trace = 0d;

            for (var a = 0; a < d; a++)
            {
                trace += covariance[a][a];
            }

            var averageVariance = Math.Max(trace / d, 1e-12);

            cancellationToken.ThrowIfCancellationRequested();

            var inverse = SmallLinearAlgebra.Inverse(covariance);
            var regularised = false;

            if (inverse == null)
            {
                regularised = true;

                for (var a = 0; a < d; a++)
                {
                    covariance[a][a] += options.RidgeEpsilon * averageVariance;
                }

                inverse = SmallLinearAlgebra.Inverse(covariance);

                if (inverse == null)
                {
                    throw new InvalidOperationException(
                        "The covariance matrix is singular and could not be regularised; reduce the variables or provide more rows.");
                }

                warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.CovarianceRegularised,
                    $"The training covariance matrix was singular; a ridge of {options.RidgeEpsilon:g} times the average variance was added to its diagonal before it could be inverted."));
            }

            var minDiagonal = double.MaxValue;
            var maxDiagonal = double.MinValue;

            for (var a = 0; a < d; a++)
            {
                minDiagonal = Math.Min(minDiagonal, covariance[a][a]);
                maxDiagonal = Math.Max(maxDiagonal, covariance[a][a]);
            }

            var conditionNumber = minDiagonal > 0 ? maxDiagonal / minDiagonal : double.PositiveInfinity;

            if (!double.IsFinite(conditionNumber) || conditionNumber > options.HighConditionNumberThreshold)
            {
                warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.HighConditionNumber,
                    $"The ratio of the largest to smallest variance on the covariance diagonal is " +
                    $"{(double.IsFinite(conditionNumber) ? conditionNumber.ToString("0.#") : "infinite")}, above the " +
                    $"{options.HighConditionNumberThreshold:0} threshold; this diagonal ratio is a cheap proxy for a " +
                    "true eigenvalue condition number, and some directions are far better determined than others."));
            }

            var rowsPerVariable = trainDesign.Length / (double)d;

            if (rowsPerVariable < MinimumRowsPerVariableRatio)
            {
                warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.LowRowsPerVariable,
                    $"There are {rowsPerVariable:0.#} training rows per active variable, below " +
                    $"{MinimumRowsPerVariableRatio:0}; the covariance estimate is likely unstable."));
            }

            double SquaredDistance(double[] designRow)
            {
                var centred = new double[d];

                for (var k = 0; k < d; k++)
                {
                    centred[k] = designRow[k] - centre[k];
                }

                var sum = 0d;

                for (var a = 0; a < d; a++)
                for (var b = 0; b < d; b++)
                {
                    sum += centred[a] * inverse![a][b] * centred[b];
                }

                return sum;
            }

            double[] Distances(double[][] rows)
            {
                return [.. rows.Select(row => Math.Sqrt(Math.Max(SquaredDistance(Design(row)), 0d)))];
            }

            cancellationToken.ThrowIfCancellationRequested();

            var trainDistances = Distances(trainX);
            var validationDistances = Distances(validationX);
            var testDistances = Distances(testX);

            var validationLabels = labels != null ? DataSplit.Take(labels, validationRows) : null;
            var testLabels = labels != null ? DataSplit.Take(labels, testRows) : null;

            Calibrator? calibrator = null;
            string calibrationReason;
            var calibrationRows = 0;

            if (!options.Calibrate)
            {
                calibrationReason = "Calibration was switched off.";
            }
            else if (labels == null)
            {
                calibrationReason = "No labels were supplied, so the model is uncalibrated.";

                warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.CalibrationSkipped,
                    "No labels were supplied for calibration; the model reports the raw Mahalanobis distance rather " +
                    "than a calibrated probability."));
            }
            else if (validationLabels == null || validationLabels.Length == 0 ||
                     validationLabels.All(l => l == 0) || validationLabels.All(l => l == 1))
            {
                calibrationReason = "The validation split does not contain both a normal and a known-anomalous row.";

                warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the model " +
                    "is uncalibrated."));
            }
            else
            {
                try
                {
                    calibrator = PlattCalibration.Fit(validationDistances, validationLabels,
                        cancellationToken: cancellationToken);

                    calibrationRows = validationLabels.Length;
                    calibrationReason = null;
                }
                catch (InvalidOperationException exception)
                {
                    calibrationReason = exception.Message;

                    warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            if (calibrator is { IsIncreasing: false })
            {
                warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.CalibratorNotIncreasing,
                    "The calibrator has a non-positive slope: on the validation split, a larger distance does not " +
                    "predict a higher rate of known anomalies."));
            }

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out distance statistics; do not promote on " +
                    "training statistics alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(MahalanobisWarningCodes.TinyTestSet,
                    $"The test split has {testX.Length} rows, below {MinimumTestRows}; held-out statistics will be " +
                    "noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    excluded[j] != null, excluded[j]);
            }

            var details = new MahalanobisEvidenceDetails
            {
                Mean = [.. activeInputs.Select(j => means[j])],
                CovarianceConditionNumber = double.IsFinite(conditionNumber) ? conditionNumber : double.MaxValue,
                CovarianceRegularised = regularised,
                RidgeEpsilon = options.RidgeEpsilon,
                TrainDistances = PercentilesOf(trainDistances),
                ValidationDistances = PercentilesOf(validationDistances),
                TestDistances = PercentilesOf(testDistances),
                Calibration = new MahalanobisCalibrationInfo(calibrator.HasValue, calibrator?.Slope,
                    calibrator?.Intercept, calibrationRows, calibrationReason),
                ValidationLabelledMetrics = LabelledMetricsOf(validationDistances, validationLabels),
                TestLabelledMetrics = LabelledMetricsOf(testDistances, testLabels)
            };

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsMahalanobisAnomalyPipelineSnapshot>.Create(
                MahalanobisAnomalyRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsMahalanobisAnomalyPipelineSnapshot
                {
                    Standardise = standardise,
                    FillValues = fill,
                    Means = means,
                    StandardDeviations = deviations,
                    ActiveInputs = activeInputs,
                    Centre = centre,
                    InverseCovariance = inverse
                },
                calibrator.HasValue
                    ? new AdvancedAnalyticsCalibratorSnapshot
                    {
                        Method = MahalanobisAnomalyRecall.CalibratorMethod,
                        Parameters = calibrator.Value.ToPrimitives()
                    }
                    : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new MahalanobisAnomalyResult
            {
                Kind = MahalanobisAnomalyRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                Details = details,
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                HasLabels = labels != null,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone(),
                TrainDistances = trainDistances,
                TestDistances = testDistances,
                TestLabels = testLabels
            };
        }

        private static MahalanobisDistancePercentiles PercentilesOf(double[] values)
        {
            var sorted = values.Where(double.IsFinite).OrderBy(v => v).ToArray();

            if (sorted.Length == 0)
            {
                return null;
            }

            return new MahalanobisDistancePercentiles(sorted.Length, Percentile(sorted, 0.5), Percentile(sorted, 0.9),
                Percentile(sorted, 0.95), Percentile(sorted, 0.99), sorted[^1]);
        }

        private static MahalanobisLabelledMetrics LabelledMetricsOf(double[] distances, int[] splitLabels)
        {
            if (splitLabels == null || splitLabels.Length == 0)
            {
                return null;
            }

            var known = splitLabels.Count(l => l == 1);

            if (known == 0 || known == splitLabels.Length)
            {
                return null;
            }

            var auc = BinaryMetrics.RocAuc(distances, splitLabels);
            var averagePrecision = BinaryMetrics.AveragePrecision(distances, splitLabels);

            return new MahalanobisLabelledMetrics(splitLabels.Length, known,
                double.IsFinite(auc) ? auc : null, double.IsFinite(averagePrecision) ? averagePrecision : null);
        }

        private static double Percentile(double[] sortedAscending, double q)
        {
            var position = q * (sortedAscending.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = Math.Min(lower + 1, sortedAscending.Length - 1);

            return sortedAscending[lower] + (position - lower) * (sortedAscending[upper] - sortedAscending[lower]);
        }

        private static void ValidateData(double[][] inputs, int[] labels, string[] variableNames)
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

            if (labels != null)
            {
                if (labels.Length != inputs.Length)
                {
                    throw new ArgumentException($"There are {inputs.Length} input rows but {labels.Length} labels.");
                }

                if (labels.Any(l => l != 0 && l != 1))
                {
                    throw new ArgumentException("Labels must be 0 or 1.");
                }
            }

            if (inputs.Length <= variableNames.Length)
            {
                throw new ArgumentException(
                    $"There are {inputs.Length} rows for {variableNames.Length} variables; more rows than variables are needed.");
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