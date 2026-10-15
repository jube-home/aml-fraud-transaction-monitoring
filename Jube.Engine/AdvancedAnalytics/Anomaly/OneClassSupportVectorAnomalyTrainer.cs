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
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Engine.AdvancedAnalytics.Anomaly
{
    public static class OneClassSupportVectorAnomalyTrainer
    {
        public const int MinimumTestRows = 50;
        public const double NearConstantShare = 0.995;
        public const double DenseSupportVectorShare = 0.9;
        public const int LargeSnapshotWarningThreshold = 200_000;

        public static OneClassSupportVectorAnomalyResult Train(double[][] inputs, int[] labels, string[] variableNames,
            OneClassSupportVectorAnomalyOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new OneClassSupportVectorAnomalyOptions();
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

        public static OneClassSupportVectorAnomalyResult TrainWithSplit(double[][] inputs, int[] labels,
            string[] variableNames, OneClassSupportVectorAnomalyOptions options, DataSplitIndices split,
            CancellationToken cancellationToken = default)
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
                    warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to the kernel similarity."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.ConstantColumn,
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

            cancellationToken.ThrowIfCancellationRequested();

            var gamma = options.Gamma > 0 ? options.Gamma : 1d / variableCount;

            var learner = new OneclassSupportVectorLearning<IKernel<double[]>>
            {
                Kernel = Gaussian.FromGamma(gamma),
                Nu = options.Nu,
                Token = cancellationToken
            };

            var machine = learner.Learn(trainDesign);

            cancellationToken.ThrowIfCancellationRequested();

            var supportVectors = (machine.SupportVectors ?? [])
                .Select(r => (double[])r.Clone()).ToArray();

            var alphas = (double[])(machine.Weights ?? []).Clone();
            var bias = machine.Threshold;

            if (supportVectors.Length == 0)
            {
                throw new InvalidOperationException(
                    "The one-class sequential minimal optimisation solver returned no support vectors.");
            }

            var preliminaryPipeline = new OneClassSupportVectorAnomalyPipeline(fill, means, deviations, supportVectors,
                alphas, bias, gamma, null);

            var trainScores = trainX.Select(preliminaryPipeline.Score).ToArray();
            var validationScores = validationX.Select(preliminaryPipeline.Score).ToArray();
            var testScores = testX.Select(preliminaryPipeline.Score).ToArray();

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

                warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.CalibrationSkipped,
                    "No labels were supplied for calibration; the model reports the raw one-class decision score " +
                    "rather than a calibrated probability."));
            }
            else if (validationLabels.Length == 0 ||
                     validationLabels.All(l => l == 0) || validationLabels.All(l => l == 1))
            {
                calibrationReason = "The validation split does not contain both a normal and a known-anomalous row.";

                warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the model " +
                    "is uncalibrated."));
            }
            else
            {
                try
                {
                    calibrator = PlattCalibration.Fit(validationScores, validationLabels,
                        cancellationToken: cancellationToken);

                    calibrationRows = validationLabels.Length;
                    calibrationReason = null;
                }
                catch (InvalidOperationException exception)
                {
                    calibrationReason = exception.Message;

                    warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            if (calibrator is { IsIncreasing: false })
            {
                warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.CalibratorNotIncreasing,
                    "The calibrator has a non-positive slope: on the validation split, a higher score does not " +
                    "predict a higher rate of known anomalies."));
            }

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out score statistics; do not promote on training " +
                    "statistics alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.TinyTestSet,
                    $"The test split has {testX.Length} rows, below {MinimumTestRows}; held-out statistics will be " +
                    "noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            if (supportVectors.Length / (double)trainDesign.Length >= DenseSupportVectorShare)
            {
                warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.DenseSupportVectorSet,
                    $"{supportVectors.Length} of the {trainDesign.Length} training rows ({supportVectors.Length / (double)trainDesign.Length:P0}) became support vectors; consider a smaller Nu if the boundary should be tighter around fewer rows."));
            }

            if (supportVectors.Length > LargeSnapshotWarningThreshold)
            {
                warnings.Add(new AdvancedAnalyticsWarning(OneClassSupportVectorWarningCodes.LargeSnapshot,
                    $"The stored model snapshot carries all {supportVectors.Length} support vectors, since recalling a " +
                    "one-class support vector machine means weighting the kernel similarity to every stored support " +
                    "vector; this is expected for this family, but it makes the snapshot and its storage cost grow " +
                    "with the number of support vectors kept."));
            }

            var details = new OneClassSupportVectorEvidenceDetails
            {
                Nu = options.Nu,
                Gamma = gamma,
                SupportVectorCount = supportVectors.Length,
                TrainScores = PercentilesOf(trainScores),
                ValidationScores = PercentilesOf(validationScores),
                TestScores = PercentilesOf(testScores),
                Calibration = new OneClassSupportVectorCalibrationInfo(calibrator.HasValue, calibrator?.Slope,
                    calibrator?.Intercept, calibrationRows, calibrationReason),
                ValidationLabelledMetrics = LabelledMetricsOf(validationScores, validationLabels),
                TestLabelledMetrics = LabelledMetricsOf(testScores, testLabels)
            };

            var envelope =
                AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsOneClassSupportVectorAnomalyPipelineSnapshot>.Create(
                    OneClassSupportVectorAnomalyRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsOneClassSupportVectorAnomalyPipelineSnapshot
                    {
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        SupportVectors = supportVectors,
                        Alphas = alphas,
                        Bias = bias,
                        Gamma = gamma
                    },
                    calibrator.HasValue
                        ? new AdvancedAnalyticsCalibratorSnapshot
                        {
                            Method = OneClassSupportVectorAnomalyRecall.CalibratorMethod,
                            Parameters = calibrator.Value.ToPrimitives()
                        }
                        : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new OneClassSupportVectorAnomalyResult
            {
                Kind = OneClassSupportVectorAnomalyRecall.ModelKind,
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
                TrainScores = trainScores,
                TestScores = testScores,
                TestLabels = testLabels
            };
        }

        private static OneClassSupportVectorScorePercentiles PercentilesOf(double[] values)
        {
            var sorted = values.Where(double.IsFinite).OrderBy(v => v).ToArray();

            if (sorted.Length == 0)
            {
                return null;
            }

            return new OneClassSupportVectorScorePercentiles(sorted.Length, Percentile(sorted, 0.5),
                Percentile(sorted, 0.9), Percentile(sorted, 0.95), Percentile(sorted, 0.99), sorted[^1]);
        }

        private static OneClassSupportVectorLabelledMetrics LabelledMetricsOf(double[] scores, int[] splitLabels)
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

            var auc = BinaryMetrics.RocAuc(scores, splitLabels);
            var averagePrecision = BinaryMetrics.AveragePrecision(scores, splitLabels);

            return new OneClassSupportVectorLabelledMetrics(splitLabels.Length, known,
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