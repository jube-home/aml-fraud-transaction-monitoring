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
    public static class GradientBoostingRegressorTrainer
    {
        public const int MinimumTestRows = 30;
        public const double NearConstantShare = 0.995;

        public static GradientBoostingRegressorResult Train(double[][] inputs, double[] outputs, string[] variableNames,
            GradientBoostingRegressorOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new GradientBoostingRegressorOptions();
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

        public static GradientBoostingRegressorResult TrainWithSplit(double[][] inputs, double[] outputs,
            string[] variableNames, GradientBoostingRegressorOptions options, DataSplitIndices split,
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
                    warnings.Add(new AdvancedAnalyticsWarning(GradientBoostingRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it can never be chosen for a split."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(GradientBoostingRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(
                        GradientBoostingRegressorWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            double[] Design(double[] row)
            {
                var design = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    design[j] = double.IsFinite(row[j]) ? row[j] : fill[j];
                }

                return design;
            }

            var trainDesign = trainX.Select(Design).ToArray();
            var validationDesign = validationX.Select(Design).ToArray();
            var testDesign = testX.Select(Design).ToArray();

            var trainCount = trainDesign.Length;
            var baseValue = trainY.Average();

            var prediction = new double[trainCount];

            for (var i = 0; i < trainCount; i++)
            {
                prediction[i] = baseValue;
            }

            var sampleSize = Math.Max(1, (int)Math.Round(options.SampleFraction * trainCount));
            var trees = new List<GradientBoostingRegressorTree>(options.Rounds);
            var trainingLossByRound = new double[options.Rounds];
            var importance = new double[variableCount];

            for (var round = 0; round < options.Rounds; round++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var residual = new double[trainCount];

                for (var i = 0; i < trainCount; i++)
                {
                    residual[i] = trainY[i] - prediction[i];
                }

                var sampleRows = options.SampleFraction >= 1d
                    ? [.. Enumerable.Range(0, trainCount)]
                    : SampleRows(new Random(DeriveSeed(options.Seed, round)), trainCount, sampleSize);

                var tree = BuildTree(trainDesign, residual, sampleRows, options.MaxDepth, options.MinimumLeafSize,
                    importance);
                trees.Add(tree);

                for (var i = 0; i < trainCount; i++)
                {
                    prediction[i] += options.LearningRate * tree.Predict(trainDesign[i]);
                }

                var sse = 0d;

                for (var i = 0; i < trainCount; i++)
                {
                    var e = trainY[i] - prediction[i];
                    sse += e * e;
                }

                trainingLossByRound[round] = sse / trainCount;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var importanceSum = importance.Sum();

            var featureImportances = importanceSum > 0
                ? [.. importance.Select(v => v / importanceSum)]
                : importance;

            double[] Predictions(double[][] design)
            {
                var result = new double[design.Length];

                for (var i = 0; i < design.Length; i++)
                {
                    var value = baseValue;

                    foreach (var tree in trees)
                    {
                        value += options.LearningRate * tree.Predict(design[i]);
                    }

                    result[i] = value;
                }

                return result;
            }

            var trainPredicted = prediction;
            var validationPredicted = Predictions(validationDesign);
            var testPredicted = Predictions(testDesign);

            var trainMetrics = Metrics("Train", trainY, trainPredicted, variableCount);
            var validationMetrics = Metrics("Validation", validationY, validationPredicted, variableCount);
            var testMetrics = Metrics("Test", testY, testPredicted, variableCount);

            if (!trainMetrics!.RSquared.HasValue)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GradientBoostingRegressorWarningCodes.NoVariation,
                    "The outcome has no variation in the training split, so R squared is undefined."));
            }

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GradientBoostingRegressorWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GradientBoostingRegressorWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows, below {MinimumTestRows}; held-out metrics will be noisy."));
            }

            if (testMetrics?.RSquared is { } testRSquared and <= 0.05)
            {
                warnings.Add(new AdvancedAnalyticsWarning(GradientBoostingRegressorWarningCodes.NoSignal,
                    $"The test R squared is {testRSquared:0.###}, little better than always predicting the mean; the " +
                    "variables show no usable signal for this ensemble."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);
            }

            var treeSnapshots = trees.Select(t => new GradientBoostingTreeSnapshot
            {
                FeatureIndex = t.FeatureIndex,
                Threshold = t.Threshold,
                Left = t.Left,
                Right = t.Right,
                Value = t.Value
            }).ToArray();

            var envelope =
                AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsGradientBoostingRegressorPipelineSnapshot>.Create(
                    GradientBoostingRegressorRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsGradientBoostingRegressorPipelineSnapshot
                    {
                        BaseValue = baseValue,
                        LearningRate = options.LearningRate,
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        Trees = treeSnapshots
                    });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new GradientBoostingRegressorResult
            {
                Kind = GradientBoostingRegressorRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new GradientBoostingRegressorEvidenceDetails
                {
                    Rounds = options.Rounds,
                    TreesFitted = trees.Count,
                    MaxDepth = options.MaxDepth,
                    LearningRate = options.LearningRate,
                    MinimumLeafSize = options.MinimumLeafSize,
                    SampleFraction = options.SampleFraction,
                    BaseValue = baseValue,
                    TrainingLossByRound = trainingLossByRound,
                    TrainMetrics = trainMetrics,
                    ValidationMetrics = validationMetrics,
                    TestMetrics = testMetrics,
                    FeatureImportances = featureImportances
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static GradientBoostingRegressorTree BuildTree(double[][] design, double[] target, int[] rows,
            int maxDepth, int minLeafSize, double[] importance)
        {
            var featureIndex = new List<int>();
            var threshold = new List<double>();
            var left = new List<int>();
            var right = new List<int>();
            var value = new List<double>();

            int BuildNode(int[] nodeRows, int depth)
            {
                var mean = nodeRows.Average(i => target[i]);
                var nodeIndex = value.Count;
                featureIndex.Add(-1);
                threshold.Add(0d);
                left.Add(-1);
                right.Add(-1);
                value.Add(mean);

                if (depth >= maxDepth || nodeRows.Length < 2 * minLeafSize)
                {
                    return nodeIndex;
                }

                var parentSse = SumSquares(nodeRows, target, mean);

                if (parentSse <= 1e-12)
                {
                    return nodeIndex;
                }

                var featureCount = design[0].Length;
                var bestGain = 0d;
                var bestFeature = -1;
                var bestThreshold = 0d;
                int[] bestLeftRows = null;
                int[] bestRightRows = null;

                for (var feature = 0; feature < featureCount; feature++)
                {
                    var values = nodeRows.Select(i => design[i][feature]).Distinct().OrderBy(v => v).ToArray();

                    if (values.Length < 2)
                    {
                        continue;
                    }

                    for (var k = 0; k < values.Length - 1; k++)
                    {
                        var candidateThreshold = (values[k] + values[k + 1]) / 2d;
                        var leftRows = nodeRows.Where(i => design[i][feature] <= candidateThreshold).ToArray();

                        if (leftRows.Length < minLeafSize || nodeRows.Length - leftRows.Length < minLeafSize)
                        {
                            continue;
                        }

                        var rightRows = nodeRows.Where(i => design[i][feature] > candidateThreshold).ToArray();
                        var leftMean = leftRows.Average(i => target[i]);
                        var rightMean = rightRows.Average(i => target[i]);
                        var leftSse = SumSquares(leftRows, target, leftMean);
                        var rightSse = SumSquares(rightRows, target, rightMean);
                        var gain = parentSse - leftSse - rightSse;

                        if (gain > bestGain + 1e-12)
                        {
                            bestGain = gain;
                            bestFeature = feature;
                            bestThreshold = candidateThreshold;
                            bestLeftRows = leftRows;
                            bestRightRows = rightRows;
                        }
                    }
                }

                if (bestFeature == -1)
                {
                    return nodeIndex;
                }

                importance[bestFeature] += bestGain;
                featureIndex[nodeIndex] = bestFeature;
                threshold[nodeIndex] = bestThreshold;

                var leftIndex = BuildNode(bestLeftRows!, depth + 1);
                var rightIndex = BuildNode(bestRightRows!, depth + 1);
                left[nodeIndex] = leftIndex;
                right[nodeIndex] = rightIndex;

                return nodeIndex;
            }

            BuildNode(rows, 0);

            return new GradientBoostingRegressorTree([.. featureIndex], [.. threshold], [.. left],
                [.. right], [.. value]);
        }

        private static double SumSquares(int[] rows, double[] target, double mean)
        {
            var sse = 0d;

            foreach (var i in rows)
            {
                var d = target[i] - mean;
                sse += d * d;
            }

            return sse;
        }

        private static int[] SampleRows(Random random, int total, int count)
        {
            var pool = Enumerable.Range(0, total).ToArray();
            var take = Math.Min(count, total);

            for (var i = 0; i < take; i++)
            {
                var j = i + random.Next(total - i);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return [.. pool.Take(take)];
        }

        private static int DeriveSeed(int seed, int index)
        {
            return unchecked(seed * 7919 + index * 104729 + 17);
        }

        private static GradientBoostingRegressorSplitMetrics Metrics(string split, double[] actual, double[] predicted,
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

            return new GradientBoostingRegressorSplitMetrics(split, n, rSquared, adjustedRSquared, Math.Sqrt(sse / n),
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