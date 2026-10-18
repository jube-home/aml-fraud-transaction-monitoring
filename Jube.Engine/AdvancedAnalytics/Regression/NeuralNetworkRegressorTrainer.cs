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
    public static class NeuralNetworkRegressorTrainer
    {
        public const int MinimumTestRows = 30;
        public const double NearConstantShare = 0.995;
        private const double CompressedLow = 0.05;
        private const double CompressedSpan = 0.9;
        private static readonly int[] traceCheckpoints = [1, 10, 50, 100, 200, 500, 1000];

        public static NeuralNetworkRegressorResult Train(double[][] inputs, double[] outputs, string[] variableNames,
            NeuralNetworkRegressorOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new NeuralNetworkRegressorOptions();
            options.Validate();
            ValidateData(inputs, outputs, variableNames);

            var trainFraction = 1d - options.ValidationFraction - options.TestFraction;

            var split = DataSplit.Split(outputs, trainFraction, options.ValidationFraction, options.TestFraction,
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

            return TrainWithSplit(inputs, outputs, variableNames, options, split, cancellationToken);
        }

        public static NeuralNetworkRegressorResult TrainWithSplit(double[][] inputs, double[] outputs,
            string[] variableNames, NeuralNetworkRegressorOptions options, DataSplitIndices split,
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

            if (trainY.Length == 0)
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
                    warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it contributes nothing to the fit."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkRegressorWarningCodes.NearConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkRegressorWarningCodes.MissingValuesImputed,
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

            var targetLow = trainY.Min();
            var targetSpan = trainY.Max() - targetLow;
            var constantTarget = !(targetSpan > 0);

            if (constantTarget)
            {
                targetSpan = 1d;

                warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkRegressorWarningCodes.ConstantTarget,
                    "The outcome has no variation in the training split; the network was fitted to reproduce that " +
                    "constant value only, and the reported R squared is undefined."));
            }

            var n = trainDesign.Length;
            var compressedTargets = new double[n];

            for (var i = 0; i < n; i++)
            {
                compressedTargets[i] = CompressedLow + CompressedSpan * (trainY[i] - targetLow) / targetSpan;
            }

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

            var hidden = new double[n][];

            for (var i = 0; i < n; i++)
            {
                hidden[i] = new double[hiddenUnits];
            }

            var boundedOutputs = new double[n];
            var diverged = false;
            var epochsRun = 0;
            var trace = new List<NeuralNetworkRegressorLossCheckpoint>();

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

                        var activation = NeuralNetworkRegressorPipeline.Sigmoid(z);
                        hidden[i][h] = activation;
                        logit += w2[h] * activation;
                    }

                    boundedOutputs[i] = NeuralNetworkRegressorPipeline.Sigmoid(logit);
                }

                if (traceCheckpoints.Contains(epochsRun) || epochsRun == options.Epochs)
                {
                    trace.Add(new NeuralNetworkRegressorLossCheckpoint(epochsRun,
                        MeanSquaredError(boundedOutputs, compressedTargets)));
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
                    var output = boundedOutputs[i];
                    var dLogit = 2d * (output - compressedTargets[i]) * output * (1d - output);
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

            var finalTrainLoss = MeanSquaredError(boundedOutputs, compressedTargets);

            var pipeline = new NeuralNetworkRegressorPipeline(standardise, fill, means, deviations, w1, b1, w2, b2,
                targetLow, targetSpan);

            var trainPredicted = pipeline.PredictedValues(trainX);
            var validationPredicted = pipeline.PredictedValues(validationX);
            var testPredicted = pipeline.PredictedValues(testX);

            var trainMetrics = Metrics("Train", trainY, trainPredicted, variableCount);
            var validationMetrics = Metrics("Validation", validationY, validationPredicted, variableCount);
            var testMetrics = Metrics("Test", testY, testPredicted, variableCount);

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkRegressorWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(NeuralNetworkRegressorWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows, below {MinimumTestRows}; held-out metrics will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);
            }

            var details = new NeuralNetworkRegressorEvidenceDetails
            {
                HiddenLayers = [hiddenUnits],
                EpochsRun = epochsRun,
                LearningRate = options.LearningRate,
                TrainingLossTrace = [.. trace],
                FinalTrainingLoss = finalTrainLoss,
                Converged = true,
                TargetSpan = constantTarget ? 0d : targetSpan,
                TrainMetrics = trainMetrics,
                ValidationMetrics = validationMetrics,
                TestMetrics = testMetrics
            };

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsNeuralNetworkRegressorPipelineSnapshot>
                .Create(NeuralNetworkRegressorRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsNeuralNetworkRegressorPipelineSnapshot
                    {
                        Standardise = standardise,
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        HiddenUnits = hiddenUnits,
                        InputWeights = w1,
                        HiddenBiases = b1,
                        OutputWeights = w2,
                        OutputBias = b2,
                        TargetLow = targetLow,
                        TargetSpan = targetSpan
                    });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new NeuralNetworkRegressorResult
            {
                Kind = NeuralNetworkRegressorRecall.ModelKind,
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

        private static double MeanSquaredError(double[] predicted, double[] targets)
        {
            var sum = 0d;

            for (var i = 0; i < predicted.Length; i++)
            {
                var e = predicted[i] - targets[i];
                sum += e * e;
            }

            return predicted.Length == 0 ? 0d : sum / predicted.Length;
        }

        private static NeuralNetworkRegressorSplitMetrics Metrics(string split, double[] actual, double[] predicted,
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

            return new NeuralNetworkRegressorSplitMetrics(split, n, rSquared, adjustedRSquared, Math.Sqrt(sse / n),
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