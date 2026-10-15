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
using System.Threading.Tasks;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public static class RandomForestRegressorTrainer
    {
        public const int MinimumTestRows = 30;
        public const double NearConstantShare = 0.995;
        public const int MinimumOutOfBagRows = 10;

        public static RandomForestRegressorResult Train(double[][] inputs, double[] outputs, string[] variableNames,
            RandomForestRegressorOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new RandomForestRegressorOptions();
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

        public static RandomForestRegressorResult TrainWithSplit(double[][] inputs, double[] outputs,
            string[] variableNames, RandomForestRegressorOptions options, DataSplitIndices split,
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
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it can never be chosen for a split."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestRegressorWarningCodes.MissingValuesImputed,
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

            var featuresPerSplit = options.FeatureFraction > 0
                ? Math.Min(variableCount, Math.Max(1, (int)Math.Ceiling(options.FeatureFraction * variableCount)))
                : Math.Min(variableCount, Math.Max(1, variableCount / 3));

            var sampleSize = Math.Max(1, (int)Math.Round(options.SampleFraction * trainDesign.Length));

            var built = new (RandomForestRegressorTree Tree, bool[] InBag, double[] Importance)[options.TreeCount];

            Parallel.For(0, options.TreeCount,
                new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 8)) },
                t =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var random = new Random(DeriveSeed(options.Seed, t));
                    var sampleRows = new int[sampleSize];
                    var inBag = new bool[trainDesign.Length];

                    for (var i = 0; i < sampleSize; i++)
                    {
                        var row = random.Next(trainDesign.Length);
                        sampleRows[i] = row;
                        inBag[row] = true;
                    }

                    var importance = new double[variableCount];

                    var tree = BuildTree(trainDesign, trainY, sampleRows, featuresPerSplit, options.MaxDepth,
                        options.MinimumLeafSize, random, importance);

                    built[t] = (tree, inBag, importance);
                });

            cancellationToken.ThrowIfCancellationRequested();

            var trees = built.Select(b => b.Tree).ToArray();
            var totalImportance = new double[variableCount];

            foreach (var entry in built)
            {
                for (var j = 0; j < variableCount; j++)
                {
                    totalImportance[j] += entry.Importance[j];
                }
            }

            var importanceSum = totalImportance.Sum();

            var featureImportances = importanceSum > 0
                ? [.. totalImportance.Select(v => v / importanceSum)]
                : totalImportance;

            var oobSums = new double[trainDesign.Length];
            var oobCounts = new int[trainDesign.Length];

            for (var t = 0; t < trees.Length; t++)
            for (var i = 0; i < trainDesign.Length; i++)
            {
                if (!built[t].InBag[i])
                {
                    oobSums[i] += trees[t].Predict(trainDesign[i]);
                    oobCounts[i]++;
                }
            }

            var covered = Enumerable.Range(0, trainDesign.Length).Where(i => oobCounts[i] > 0).ToArray();
            double? outOfBagRSquared = null;

            if (covered.Length > MinimumOutOfBagRows)
            {
                var oobPredicted = covered.Select(i => oobSums[i] / oobCounts[i]).ToArray();
                var oobActual = covered.Select(i => trainY[i]).ToArray();
                outOfBagRSquared = Metrics("OutOfBag", oobActual, oobPredicted, variableCount)?.RSquared;
            }
            else
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestRegressorWarningCodes.OutOfBagUnavailable,
                    "Too few training rows were left out of every tree's bootstrap sample to compute an honest " +
                    "out-of-bag estimate."));
            }

            double[] RawScores(double[][] design)
            {
                var scores = new double[design.Length];

                for (var i = 0; i < design.Length; i++)
                {
                    var sum = 0d;

                    foreach (var tree in trees)
                    {
                        sum += tree.Predict(design[i]);
                    }

                    scores[i] = sum / trees.Length;
                }

                return scores;
            }

            var trainPredicted = RawScores(trainDesign);
            var validationPredicted = RawScores(validationDesign);
            var testPredicted = RawScores(testDesign);

            var trainMetrics = Metrics("Train", trainY, trainPredicted, variableCount);
            var validationMetrics = Metrics("Validation", validationY, validationPredicted, variableCount);
            var testMetrics = Metrics("Test", testY, testPredicted, variableCount);

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestRegressorWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestRegressorWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows, below {MinimumTestRows}; held-out metrics will be noisy."));
            }

            if (testMetrics?.RSquared is { } testRSquared and <= 0d)
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestRegressorWarningCodes.NoSignal,
                    $"The test R squared is {testRSquared:0.###}, no better than predicting the training mean; the " +
                    "variables show no usable signal for this forest."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);
            }

            var treeSnapshots = trees.Select(t => new RandomForestRegressorTreeSnapshot
            {
                FeatureIndex = t.FeatureIndex,
                Threshold = t.Threshold,
                Left = t.Left,
                Right = t.Right,
                Value = t.Value
            }).ToArray();

            var envelope =
                AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsRandomForestRegressorPipelineSnapshot>.Create(
                    RandomForestRegressorRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsRandomForestRegressorPipelineSnapshot
                    {
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        Trees = treeSnapshots
                    });

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new RandomForestRegressorResult
            {
                Kind = RandomForestRegressorRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new RandomForestRegressorEvidenceDetails
                {
                    TreeCount = options.TreeCount,
                    MaxDepth = options.MaxDepth,
                    MinimumLeafSize = options.MinimumLeafSize,
                    FeaturesPerSplit = featuresPerSplit,
                    SampleFraction = options.SampleFraction,
                    OutOfBagRSquared = outOfBagRSquared,
                    TrainMetrics = trainMetrics,
                    ValidationMetrics = validationMetrics,
                    TestMetrics = testMetrics,
                    FeatureImportances = featureImportances
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone(),
                TrainPredictions =
                    new RandomForestRegressorPredictions((int[])trainRows.Clone(), trainY, trainPredicted),
                ValidationPredictions = new RandomForestRegressorPredictions((int[])validationRows.Clone(), validationY,
                    validationPredicted),
                TestPredictions = new RandomForestRegressorPredictions((int[])testRows.Clone(), testY, testPredicted)
            };
        }

        private static RandomForestRegressorTree BuildTree(double[][] design, double[] y, int[] sampleRows,
            int featuresPerSplit, int maxDepth, int minLeafSize, Random random, double[] importance)
        {
            var featureIndex = new List<int>();
            var threshold = new List<double>();
            var left = new List<int>();
            var right = new List<int>();
            var value = new List<double>();

            int BuildNode(int[] rows, int depth)
            {
                var mean = rows.Average(i => y[i]);
                var sse = rows.Sum(i => (y[i] - mean) * (y[i] - mean));
                var nodeIndex = value.Count;
                featureIndex.Add(-1);
                threshold.Add(0d);
                left.Add(-1);
                right.Add(-1);
                value.Add(mean);

                if (depth >= maxDepth || rows.Length < 2 * minLeafSize || sse <= 1e-12)
                {
                    return nodeIndex;
                }

                var parentImpurity = sse / rows.Length;
                var candidateFeatures = SampleFeatures(random, design[0].Length, featuresPerSplit);

                var bestGain = 0d;
                var bestFeature = -1;
                var bestThreshold = 0d;
                int[] bestLeftRows = null;
                int[] bestRightRows = null;

                foreach (var feature in candidateFeatures)
                {
                    var values = rows.Select(i => design[i][feature]).Distinct().OrderBy(v => v).ToArray();

                    if (values.Length < 2)
                    {
                        continue;
                    }

                    for (var k = 0; k < values.Length - 1; k++)
                    {
                        var candidateThreshold = (values[k] + values[k + 1]) / 2d;
                        var leftRows = rows.Where(i => design[i][feature] <= candidateThreshold).ToArray();

                        if (leftRows.Length < minLeafSize || rows.Length - leftRows.Length < minLeafSize)
                        {
                            continue;
                        }

                        var rightRows = rows.Where(i => design[i][feature] > candidateThreshold).ToArray();
                        var leftMean = leftRows.Average(i => y[i]);
                        var rightMean = rightRows.Average(i => y[i]);
                        var leftSse = leftRows.Sum(i => (y[i] - leftMean) * (y[i] - leftMean));
                        var rightSse = rightRows.Sum(i => (y[i] - rightMean) * (y[i] - rightMean));
                        var weightedImpurity = (leftSse + rightSse) / rows.Length;
                        var gain = parentImpurity - weightedImpurity;

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

                importance[bestFeature] += rows.Length * bestGain;
                featureIndex[nodeIndex] = bestFeature;
                threshold[nodeIndex] = bestThreshold;

                var leftIndex = BuildNode(bestLeftRows!, depth + 1);
                var rightIndex = BuildNode(bestRightRows!, depth + 1);
                left[nodeIndex] = leftIndex;
                right[nodeIndex] = rightIndex;

                return nodeIndex;
            }

            BuildNode(sampleRows, 0);

            return new RandomForestRegressorTree([.. featureIndex], [.. threshold], [.. left],
                [.. right], [.. value]);
        }

        private static int[] SampleFeatures(Random random, int total, int count)
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

        private static RandomForestRegressorSplitMetrics Metrics(string split, double[] actual, double[] predicted,
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

            return new RandomForestRegressorSplitMetrics(split, n, rSquared, adjustedRSquared, Math.Sqrt(sse / n),
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
                throw new ArgumentException(
                    $"There are {inputs.Length} input rows but {outputs.Length} outputs.");
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