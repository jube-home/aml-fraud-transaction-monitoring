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
    public static class NeuralNetworkClassifierTrainer
    {
        public const int MinimumTestRows = 30;
        public const double NearConstantShare = 0.995;

        public static NeuralNetworkClassifierResult Train(double[][] inputs, int[] labels, string[] variableNames,
            NeuralNetworkClassifierOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new NeuralNetworkClassifierOptions();
            options.Validate();
            ValidateData(inputs, labels, variableNames);

            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;

            var split = DataSplit.Split([.. labels.Select(l => (double)l)], trainFraction,
                options.ValidationFraction, options.TestFraction, options.Seed);

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

        public static NeuralNetworkClassifierResult TrainWithSplit(double[][] inputs, int[] labels,
            string[] variableNames, NeuralNetworkClassifierOptions options, DataSplitIndices split,
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
                    warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to the fit."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkWarningCodes.NearConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkWarningCodes.MissingValuesImputed,
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

            cancellationToken.ThrowIfCancellationRequested();

            var hiddenUnits = options.HiddenUnits;
            var random = new Random(options.Seed);
            var inputLimit = 1d / Math.Sqrt(variableCount);
            var outputLimit = 1d / Math.Sqrt(hiddenUnits);

            var w1 = new double[hiddenUnits][];
            var b1 = new double[hiddenUnits];
            var w2 = new double[hiddenUnits];
            var b2 = 0d;

            for (var h = 0; h < hiddenUnits; h++)
            {
                w1[h] = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    w1[h][j] = (random.NextDouble() * 2d - 1d) * inputLimit;
                }

                w2[h] = (random.NextDouble() * 2d - 1d) * outputLimit;
            }

            var n = trainDesign.Length;
            var hidden = new double[n][];

            for (var i = 0; i < n; i++)
            {
                hidden[i] = new double[hiddenUnits];
            }

            var probabilities = new double[n];
            var diverged = false;
            var epochsRun = 0;

            for (var epoch = 0; epoch < options.Epochs; epoch++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                epochsRun = epoch + 1;

                for (var i = 0; i < n; i++)
                {
                    var row = trainDesign[i];
                    var logit = b2;

                    for (var h = 0; h < hiddenUnits; h++)
                    {
                        var z = b1[h];
                        var weights = w1[h];

                        for (var j = 0; j < variableCount; j++)
                        {
                            z += weights[j] * row[j];
                        }

                        var activation = NeuralNetworkClassifierPipeline.Sigmoid(z);
                        hidden[i][h] = activation;
                        logit += w2[h] * activation;
                    }

                    probabilities[i] = NeuralNetworkClassifierPipeline.Sigmoid(logit);
                }

                var gw2 = new double[hiddenUnits];
                var gb2 = 0d;
                var gw1 = new double[hiddenUnits][];
                var gb1 = new double[hiddenUnits];

                for (var h = 0; h < hiddenUnits; h++)
                {
                    gw1[h] = new double[variableCount];
                }

                for (var i = 0; i < n; i++)
                {
                    var dLogit = probabilities[i] - trainY[i];
                    gb2 += dLogit;

                    for (var h = 0; h < hiddenUnits; h++)
                    {
                        var activation = hidden[i][h];
                        gw2[h] += dLogit * activation;

                        var dHidden = dLogit * w2[h] * activation * (1d - activation);
                        gb1[h] += dHidden;
                        var row = trainDesign[i];
                        var gradientRow = gw1[h];

                        for (var j = 0; j < variableCount; j++)
                        {
                            gradientRow[j] += dHidden * row[j];
                        }
                    }
                }

                var learningRate = options.LearningRate;
                var scale = learningRate / n;

                b2 -= scale * gb2;

                for (var h = 0; h < hiddenUnits; h++)
                {
                    w2[h] -= scale * gw2[h];
                    b1[h] -= scale * gb1[h];
                    var weights = w1[h];
                    var gradientRow = gw1[h];

                    for (var j = 0; j < variableCount; j++)
                    {
                        weights[j] -= scale * gradientRow[j];
                    }
                }

                if (!double.IsFinite(b2) || !double.IsFinite(gb2) || w2.Any(v => !double.IsFinite(v)) ||
                    b1.Any(v => !double.IsFinite(v)) || w1.Any(row => row.Any(v => !double.IsFinite(v))))
                {
                    diverged = true;
                    break;
                }
            }

            if (diverged)
            {
                throw new InvalidOperationException(
                    "The fit diverged to non-finite weights; reduce the learning rate or the number of epochs.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var finalTrainLoss = BinaryCrossEntropy(probabilities, trainY);

            var preliminary = new NeuralNetworkClassifierPipeline(standardise, fill, means, deviations, w1, b1, w2, b2,
                null);

            double[] Logits(NeuralNetworkClassifierPipeline pipeline, double[][] rows)
            {
                return [.. rows.Select(pipeline.RawLogit)];
            }

            Calibrator? calibrator = null;
            string calibrationReason;
            var calibrationRows = 0;
            var validationPositives = validationY.Sum();

            if (!options.Calibrate)
            {
                calibrationReason = "Calibration was switched off.";
            }
            else if (validationY.Length == 0 || validationPositives == 0 || validationPositives == validationY.Length)
            {
                calibrationReason = "The validation split does not contain both classes.";

                warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the model is uncalibrated."));
            }
            else
            {
                var validationLogits = Logits(preliminary, validationX);

                try
                {
                    calibrator = PlattCalibration.Fit(validationLogits, validationY,
                        cancellationToken: cancellationToken);

                    calibrationRows = validationY.Length;
                    calibrationReason = null;
                }
                catch (InvalidOperationException exception)
                {
                    calibrationReason = exception.Message;

                    warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            if (calibrator is { IsIncreasing: false })
            {
                warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkWarningCodes.CalibratorNotIncreasing,
                    "The calibrator has a non-positive slope: on the validation split, a higher raw output does not predict a higher rate of positives."));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pipeline = new NeuralNetworkClassifierPipeline(standardise, fill, means, deviations, w1, b1, w2, b2,
                calibrator);

            var trainLogits = Logits(pipeline, trainX);
            var validationLogits2 = Logits(pipeline, validationX);
            var testLogits = Logits(pipeline, testX);

            var trainMetrics = MetricsOf("Train", trainLogits, trainY, calibrator);
            var validationMetrics = MetricsOf("Validation", validationLogits2, validationY, calibrator);
            var testMetrics = MetricsOf("Test", testLogits, testY, calibrator);

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows, below {MinimumTestRows}; held-out metrics will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);
            }

            var details = new NeuralNetworkEvidenceDetails
            {
                HiddenUnits = hiddenUnits,
                Epochs = epochsRun,
                LearningRate = options.LearningRate,
                FinalTrainingLoss = finalTrainLoss,
                Converged = true,
                TrainMetrics = trainMetrics,
                ValidationMetrics = validationMetrics,
                TestMetrics = testMetrics,
                Calibration = new NeuralNetworkCalibrationInfo(calibrator.HasValue, calibrator?.Slope,
                    calibrator?.Intercept, calibrationRows, calibrationReason)
            };

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsNeuralNetworkClassifierPipelineSnapshot>
                .Create(NeuralNetworkClassifierRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsNeuralNetworkClassifierPipelineSnapshot
                    {
                        Standardise = standardise,
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        HiddenUnits = hiddenUnits,
                        InputWeights = w1,
                        HiddenBiases = b1,
                        OutputWeights = w2,
                        OutputBias = b2
                    },
                    calibrator.HasValue
                        ? new AdvancedAnalyticsCalibratorSnapshot
                        {
                            Method = NeuralNetworkClassifierRecall.CalibratorMethod,
                            Parameters = calibrator.Value.ToPrimitives()
                        }
                        : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new NeuralNetworkClassifierResult
            {
                Kind = NeuralNetworkClassifierRecall.ModelKind,
                Seed = options.Seed,
                Variables = (string[])variableNames.Clone(),
                Options = options,
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = details,
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static double BinaryCrossEntropy(double[] probabilities, int[] labels)
        {
            const double epsilon = 1e-12;
            var sum = 0d;

            for (var i = 0; i < probabilities.Length; i++)
            {
                var p = Math.Min(Math.Max(probabilities[i], epsilon), 1d - epsilon);
                sum -= labels[i] == 1 ? Math.Log(p) : Math.Log(1d - p);
            }

            return sum / probabilities.Length;
        }

        private static NeuralNetworkSplitMetrics MetricsOf(string split, double[] logits, int[] labels,
            Calibrator? calibrator)
        {
            if (labels.Length == 0)
            {
                return null;
            }

            var positives = labels.Sum();
            var bothClasses = positives > 0 && positives < labels.Length;

            var probabilities = logits.Select(l =>
            {
                var adjusted = calibrator is { } fitted ? fitted.Slope * l + fitted.Intercept : l;

                return NeuralNetworkClassifierPipeline.Sigmoid(adjusted);
            }).ToArray();

            double? Number(double value)
            {
                return double.IsFinite(value) ? value : null;
            }

            double? auc = null;
            double? ap = null;

            if (bothClasses)
            {
                auc = Number(BinaryMetrics.RocAuc(logits, labels));
                ap = Number(BinaryMetrics.AveragePrecision(logits, labels));
            }

            return new NeuralNetworkSplitMetrics(split, labels.Length, positives, auc, ap,
                Number(BinaryMetrics.LogLoss(probabilities, labels)),
                Number(BinaryMetrics.BrierScore(probabilities, labels)),
                Number(BinaryMetrics.ExpectedCalibrationError(probabilities, labels)));
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