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
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math.Random;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public static class LinearSupportVectorClassifierTrainer
    {
        public const int MinimumTestRows = 100;
        public const int MinimumTestClassRows = 10;
        public const double NearConstantShare = 0.995;
        public const double CalibrationSlopeLow = 0.7;
        public const double CalibrationSlopeHigh = 1.3;
        public const double SevereImbalanceRatio = 100d;
        public const double LagrangeMultiplierFloor = 1e-9;

        public static LinearSupportVectorClassifierResult Train(double[][] inputs, int[] labels, string[] variableNames,
            LinearSupportVectorClassifierOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new LinearSupportVectorClassifierOptions();
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

        public static LinearSupportVectorClassifierResult TrainWithSplit(double[][] inputs, int[] labels,
            string[] variableNames, LinearSupportVectorClassifierOptions options, DataSplitIndices split,
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
                    warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to the decision function."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
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
            var trainLabelsBool = trainY.Select(y => y == 1).ToArray();

            cancellationToken.ThrowIfCancellationRequested();

            var learner = new LinearDualCoordinateDescent
            {
                Complexity = options.Complexity,
                Token = cancellationToken
            };

            SupportVectorMachine machine;
            var previousSeed = Generator.ThreadSeed;

            try
            {
                Generator.ThreadSeed = options.Seed;
                machine = learner.Learn(trainDesign, trainLabelsBool);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new InvalidOperationException(
                    "The linear support vector fit could not be completed: " + exception.Message, exception);
            }
            finally
            {
                Generator.ThreadSeed = previousSeed;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var weights = (double[])machine.SupportVectors[0].Clone();
            var bias = machine.Threshold;
            var supportVectorCount = learner.Lagrange.Count(a => a > LagrangeMultiplierFloor);

            if (weights.Any(w => !double.IsFinite(w)) || !double.IsFinite(bias))
            {
                throw new InvalidOperationException(
                    "The fit diverged to non-finite weights, which usually means the classes are separable and the " +
                    "complexity is too high for this data.");
            }

            var preliminary = new LinearSupportVectorClassifierPipeline(standardise, fill, means, deviations, weights,
                bias, null);

            Calibrator? calibrator = null;
            string calibrationReason = null;
            var calibrationRows = 0;
            var validationPositives = validationY.Sum();

            if (!options.Calibrate)
            {
                calibrationReason = "Calibration was switched off.";
            }
            else if (validationY.Length == 0 || validationPositives == 0 || validationPositives == validationY.Length)
            {
                calibrationReason = "The validation split does not contain both classes.";

                warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the raw " +
                    "decision function is reported instead of a probability."));
            }
            else
            {
                var validationScores = validationX.Select(preliminary.DecisionFunction).ToArray();

                try
                {
                    calibrator = PlattCalibration.Fit(validationScores, validationY,
                        cancellationToken: cancellationToken);

                    calibrationRows = validationY.Length;
                }
                catch (InvalidOperationException exception)
                {
                    calibrationReason = exception.Message;

                    warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pipeline = new LinearSupportVectorClassifierPipeline(standardise, fill, means, deviations, weights,
                bias,
                calibrator);

            if (calibrator is { } fitted)
            {
                if (!fitted.IsIncreasing)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.CalibratorNotIncreasing,
                        "The calibrator has a non-positive slope: on the validation split the model ranks cases the " +
                        "wrong way round."));
                }
                else if (fitted.Slope < CalibrationSlopeLow || fitted.Slope > CalibrationSlopeHigh)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.CalibrationSlopeFar,
                        $"The validation calibration slope is {fitted.Slope:0.###}, far from 1: the raw decision " +
                        "function was over- or under-confident."));
                }
            }

            var threshold = options.DecisionThreshold;

            var trainMetrics = Metrics("Train", pipeline, trainX, trainY, threshold);
            var validationMetrics = Metrics("Validation", pipeline, validationX, validationY, threshold);
            var testMetrics = Metrics("Test", pipeline, testX, testY, threshold);

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);
            }

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows ||
                     Math.Min(testMetrics.Positives, testMetrics.Rows - testMetrics.Positives) < MinimumTestClassRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows with {testMetrics.Positives} positives; held-out metrics will be noisy."));
            }

            var minority = Math.Min(trainPositives, trainNegatives);
            var majority = Math.Max(trainPositives, trainNegatives);

            if (majority / (double)minority > SevereImbalanceRatio)
            {
                warnings.Add(new AdvancedAnalyticsWarning(LinearSupportVectorWarningCodes.SevereImbalance,
                    $"The training classes are imbalanced beyond 1:{SevereImbalanceRatio:0}; the linear dual coordinate " +
                    "descent solver was not given class weights, so it may lean towards the majority class."));
            }

            var envelope =
                AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLinearSupportVectorClassifierPipelineSnapshot>.Create(
                    LinearSupportVectorClassifierRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsLinearSupportVectorClassifierPipelineSnapshot
                    {
                        Standardise = standardise,
                        FillValues = pipeline.Fill,
                        Means = pipeline.Means,
                        StandardDeviations = pipeline.StandardDeviations,
                        Weights = pipeline.Weights,
                        Bias = pipeline.Bias
                    },
                    calibrator.HasValue
                        ? new AdvancedAnalyticsCalibratorSnapshot
                        {
                            Method = LinearSupportVectorClassifierRecall.CalibratorMethod,
                            Parameters = calibrator.Value.ToPrimitives()
                        }
                        : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new LinearSupportVectorClassifierResult
            {
                Kind = LinearSupportVectorClassifierRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                Details = new LinearSupportVectorEvidenceDetails
                {
                    Complexity = options.Complexity,
                    Bias = bias,
                    SupportVectorCount = supportVectorCount,
                    Calibration = new LinearSupportVectorCalibrationInfo(calibrator.HasValue, calibrator?.Slope,
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

        private static LinearSupportVectorSplitMetrics Metrics(string split,
            LinearSupportVectorClassifierPipeline pipeline,
            double[][] inputs, int[] labels, double threshold)
        {
            if (labels.Length == 0)
            {
                return null;
            }

            var scores = inputs.Select(pipeline.DecisionFunction).ToArray();
            var probabilities = inputs.Select(pipeline.Probability).ToArray();
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
                auc = Number(BinaryMetrics.RocAuc(scores, labels));
                ap = Number(BinaryMetrics.AveragePrecision(scores, labels));
            }

            var confusion = BinaryMetrics.Confusion(probabilities, labels, threshold);

            return new LinearSupportVectorSplitMetrics(split, labels.Length, positives, auc, ap,
                Number(BinaryMetrics.LogLoss(probabilities, labels)),
                Number(BinaryMetrics.BrierScore(probabilities, labels)), threshold, confusion.TruePositives,
                confusion.FalsePositives, confusion.TrueNegatives, confusion.FalseNegatives,
                Number(confusion.Precision), Number(confusion.Recall), Number(confusion.F1), Number(confusion.Mcc));
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