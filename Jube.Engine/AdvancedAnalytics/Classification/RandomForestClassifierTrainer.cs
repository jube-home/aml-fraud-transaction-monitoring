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
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public static class RandomForestClassifierTrainer
    {
        public const int MinimumTestRows = 100;
        public const int MinimumTestClassRows = 10;
        public const double NearConstantShare = 0.995;
        public const double SevereImbalanceRatio = 100d;
        public const double CalibrationSlopeLow = 0.7;
        public const double CalibrationSlopeHigh = 1.3;
        public const int MinimumOutOfBagRows = 10;

        public static RandomForestClassifierResult Train(double[][] inputs, int[] labels, string[] variableNames,
            RandomForestClassifierOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new RandomForestClassifierOptions();
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

        public static RandomForestClassifierResult TrainWithSplit(double[][] inputs, int[] labels,
            string[] variableNames, RandomForestClassifierOptions options, DataSplitIndices split,
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
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it can never be chosen for a split."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.MissingValuesImputed,
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
                : Math.Min(variableCount, Math.Max(1, (int)Math.Floor(Math.Sqrt(variableCount))));

            var sampleSize = Math.Max(1, (int)Math.Round(options.SampleFraction * trainDesign.Length));

            var built = new (RandomForestTree Tree, bool[] InBag, double[] Importance)[options.TreeCount];

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
                        options.MinLeafSize, random, importance);

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
            double? outOfBagAuc = null;

            if (covered.Length > MinimumOutOfBagRows)
            {
                var oobScores = covered.Select(i => oobSums[i] / oobCounts[i]).ToArray();
                var oobLabels = covered.Select(i => trainY[i]).ToArray();
                var auc = BinaryMetrics.RocAuc(oobScores, oobLabels);
                outOfBagAuc = double.IsFinite(auc) ? auc : null;
            }
            else
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.OutOfBagUnavailable,
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

            var trainRaw = RawScores(trainDesign);
            var validationRaw = RawScores(validationDesign);
            var testRaw = RawScores(testDesign);

            Calibrator? calibrator = null;
            var validationPositives = validationY.Sum();

            if (options.Calibrate)
            {
                if (validationY.Length == 0 || validationPositives == 0 || validationPositives == validationY.Length)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.CalibrationSkipped,
                        "Calibration was requested but the validation split does not contain both classes, so the raw " +
                        "vote fraction is used as the probability."));
                }
                else
                {
                    try
                    {
                        calibrator = PlattCalibration.Fit(validationRaw, validationY,
                            cancellationToken: cancellationToken);
                    }
                    catch (InvalidOperationException exception)
                    {
                        warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.CalibrationSkipped,
                            "Calibration could not be fitted on the validation split: " + exception.Message));
                    }
                }
            }

            double[] Calibrate(double[] raw)
            {
                return calibrator.HasValue ? calibrator.Value.Apply(raw) : raw;
            }

            var trainProbabilities = Calibrate(trainRaw);
            var validationProbabilities = Calibrate(validationRaw);
            var testProbabilities = Calibrate(testRaw);

            var threshold = options.DecisionThreshold ?? 0.5;

            var trainMetrics = Metrics("Train", trainProbabilities, trainY, threshold);
            var validationMetrics = Metrics("Validation", validationProbabilities, validationY, threshold);
            var testMetrics = Metrics("Test", testProbabilities, testY, threshold);

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows ||
                     Math.Min(testMetrics.Positives, testMetrics.Rows - testMetrics.Positives) < MinimumTestClassRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows with {testMetrics.Positives} positives; held-out metrics will be noisy."));
            }

            if (testMetrics?.RocAuc is { } testAuc and <= 0.55)
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.NoSignal,
                    $"The test ROC AUC is {testAuc:0.###}, little better than chance; the variables show no usable signal for this forest."));
            }

            if (calibrator is { } fitted)
            {
                if (!fitted.IsIncreasing)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.CalibratorNotIncreasing,
                        "The calibrator has a non-positive slope: on the validation split the forest ranks cases the wrong way round."));
                }

                if (fitted.Slope < CalibrationSlopeLow || fitted.Slope > CalibrationSlopeHigh)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.CalibrationSlopeFar,
                        $"The validation calibration slope is {fitted.Slope:0.###}, far from 1: the raw vote fraction was over- or under-confident."));
                }
            }

            var minority = Math.Min(trainPositives, trainY.Length - trainPositives);
            var majority = Math.Max(trainPositives, trainY.Length - trainPositives);

            if (majority / (double)minority > SevereImbalanceRatio)
            {
                warnings.Add(new AdvancedAnalyticsWarning(RandomForestWarningCodes.SevereImbalance,
                    $"The training classes are imbalanced beyond 1:{SevereImbalanceRatio:0}; consider a smaller sample fraction or a different family."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);
            }

            var treeSnapshots = trees.Select(t => new RandomForestTreeSnapshot
            {
                FeatureIndex = t.FeatureIndex,
                Threshold = t.Threshold,
                Left = t.Left,
                Right = t.Right,
                Value = t.Value
            }).ToArray();

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsRandomForestClassifierPipelineSnapshot>
                .Create(
                    RandomForestClassifierRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsRandomForestClassifierPipelineSnapshot
                    {
                        FillValues = fill,
                        Means = means,
                        StandardDeviations = deviations,
                        Trees = treeSnapshots
                    },
                    calibrator.HasValue
                        ? new AdvancedAnalyticsCalibratorSnapshot
                        {
                            Method = RandomForestClassifierRecall.CalibratorMethod,
                            Parameters = calibrator.Value.ToPrimitives()
                        }
                        : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new RandomForestClassifierResult
            {
                Kind = RandomForestClassifierRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new RandomForestEvidenceDetails
                {
                    TreeCount = options.TreeCount,
                    MaxDepth = options.MaxDepth,
                    MinLeafSize = options.MinLeafSize,
                    FeaturesPerSplit = featuresPerSplit,
                    SampleFraction = options.SampleFraction,
                    OutOfBagRocAuc = outOfBagAuc,
                    TrainMetrics = trainMetrics,
                    ValidationMetrics = validationMetrics,
                    TestMetrics = testMetrics,
                    Calibrated = calibrator.HasValue,
                    CalibrationSlope = calibrator?.Slope,
                    CalibrationIntercept = calibrator?.Intercept,
                    FeatureImportances = featureImportances
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static RandomForestTree BuildTree(double[][] design, int[] y, int[] sampleRows, int featuresPerSplit,
            int maxDepth, int minLeafSize, Random random, double[] importance)
        {
            var featureIndex = new List<int>();
            var threshold = new List<double>();
            var left = new List<int>();
            var right = new List<int>();
            var value = new List<double>();

            int BuildNode(int[] rows, int depth)
            {
                var mean = rows.Average(i => (double)y[i]);
                var nodeIndex = value.Count;
                featureIndex.Add(-1);
                threshold.Add(0d);
                left.Add(-1);
                right.Add(-1);
                value.Add(mean);

                if (depth >= maxDepth || rows.Length < 2 * minLeafSize || mean <= 0d || mean >= 1d)
                {
                    return nodeIndex;
                }

                var parentImpurity = mean * (1d - mean);
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
                        var leftMean = leftRows.Average(i => (double)y[i]);
                        var rightMean = rightRows.Average(i => (double)y[i]);

                        var weightedImpurity =
                            (leftRows.Length * leftMean * (1d - leftMean) +
                             rightRows.Length * rightMean * (1d - rightMean)) / rows.Length;

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

            return new RandomForestTree([.. featureIndex], [.. threshold], [.. left], [.. right],
                [.. value]);
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

        private static RandomForestSplitMetrics Metrics(string split, double[] probabilities, int[] labels,
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

            return new RandomForestSplitMetrics(split, labels.Length, positives, auc, ap,
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