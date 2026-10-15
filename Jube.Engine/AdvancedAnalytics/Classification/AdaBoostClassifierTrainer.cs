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
    public static class AdaBoostClassifierTrainer
    {
        public const int MinimumTestRows = 100;
        public const int MinimumTestClassRows = 10;
        public const double NearConstantShare = 0.995;
        public const double SevereImbalanceRatio = 100d;
        public const double CalibrationSlopeLow = 0.7;
        public const double CalibrationSlopeHigh = 1.3;
        public const double ErrorEpsilon = 1e-10;

        public static AdaBoostClassifierResult Train(double[][] inputs, int[] labels, string[] variableNames,
            AdaBoostClassifierOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new AdaBoostClassifierOptions();
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

        public static AdaBoostClassifierResult TrainWithSplit(double[][] inputs, int[] labels, string[] variableNames,
            AdaBoostClassifierOptions options, DataSplitIndices split, CancellationToken cancellationToken = default)
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
                    warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; it can never be chosen for a split."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.MissingValuesImputed,
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

            var rowCount = trainDesign.Length;
            var weights = new double[rowCount];
            var uniform = 1d / rowCount;

            for (var i = 0; i < rowCount; i++)
            {
                weights[i] = uniform;
            }

            var trees = new List<AdaBoostTree>();
            var alphas = new List<double>();
            var roundErrors = new List<double>();
            var featureImportance = new double[variableCount];
            string earlyStopReason = null;

            for (var m = 0; m < options.Rounds; m++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var importanceThisRound = new double[variableCount];

                var tree = BuildWeightedTree(trainDesign, trainY, weights, options.MaxDepth, options.MinimumLeafSize,
                    importanceThisRound);

                var signs = new int[rowCount];
                var err = 0d;

                for (var i = 0; i < rowCount; i++)
                {
                    signs[i] = tree.Sign(trainDesign[i]);
                    var actual = trainY[i] == 1 ? 1 : -1;

                    if (signs[i] != actual)
                    {
                        err += weights[i];
                    }
                }

                err = Math.Min(Math.Max(err, ErrorEpsilon), 1d - ErrorEpsilon);

                if (err >= 0.5)
                {
                    earlyStopReason = $"Round {trees.Count} fitted a weak learner no better than chance " +
                                      $"(weighted error {err:0.###}); boosting stopped after {trees.Count} round(s).";

                    break;
                }

                var alpha = 0.5 * Math.Log((1d - err) / err);

                trees.Add(tree);
                alphas.Add(alpha);
                roundErrors.Add(err);

                for (var j = 0; j < variableCount; j++)
                {
                    featureImportance[j] += alpha * importanceThisRound[j];
                }

                var newWeights = new double[rowCount];
                var sum = 0d;

                for (var i = 0; i < rowCount; i++)
                {
                    var actual = trainY[i] == 1 ? 1 : -1;
                    var factor = Math.Exp(alpha * (signs[i] == actual ? -1d : 1d));
                    newWeights[i] = weights[i] * factor;
                    sum += newWeights[i];
                }

                if (!double.IsFinite(sum) || sum <= 0)
                {
                    earlyStopReason = $"Boosting stopped after {trees.Count} round(s): the row weights became " +
                                      "numerically unstable.";

                    break;
                }

                for (var i = 0; i < rowCount; i++)
                {
                    weights[i] = newWeights[i] / sum;
                }
            }

            if (trees.Count == 0)
            {
                throw new ArgumentException(
                    "No round fitted a tree better than chance; AdaBoost found nothing to boost with these variables.");
            }

            if (earlyStopReason != null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.EarlyStopping, earlyStopReason));
            }

            var importanceSum = featureImportance.Sum();

            var featureImportances = importanceSum > 0
                ? [.. featureImportance.Select(v => v / importanceSum)]
                : featureImportance;

            var alphaArray = alphas.ToArray();

            double[] RawScores(double[][] design)
            {
                var scores = new double[design.Length];
                var alphaTotal = alphaArray.Sum();

                for (var i = 0; i < design.Length; i++)
                {
                    if (alphaTotal <= 0)
                    {
                        scores[i] = 0d;
                        continue;
                    }

                    var weighted = 0d;

                    for (var t = 0; t < trees.Count; t++)
                    {
                        weighted += alphaArray[t] * trees[t].Sign(design[i]);
                    }

                    scores[i] = weighted / alphaTotal;
                }

                return scores;
            }

            var trainRaw = RawScores(trainDesign);
            var validationRaw = RawScores(validationDesign);
            var testRaw = RawScores(testDesign);

            Calibrator? calibrator = null;
            var validationPositives = validationY.Sum();

            if (!options.Calibrate)
            {
            }
            else if (validationY.Length == 0 || validationPositives == 0 || validationPositives == validationY.Length)
            {
                warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the raw " +
                    "decision-function margin is used as the probability."));
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
                    warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
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
                warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows ||
                     Math.Min(testMetrics.Positives, testMetrics.Rows - testMetrics.Positives) < MinimumTestClassRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows with {testMetrics.Positives} positives; held-out metrics will be noisy."));
            }

            if (testMetrics?.RocAuc is { } testAuc and <= 0.55)
            {
                warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.NoSignal,
                    $"The test ROC AUC is {testAuc:0.###}, little better than chance; the variables show no usable signal for this ensemble."));
            }

            if (calibrator is { } fitted)
            {
                if (!fitted.IsIncreasing)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.CalibratorNotIncreasing,
                        "The calibrator has a non-positive slope: on the validation split the ensemble ranks cases the wrong way round."));
                }

                if (fitted.Slope < CalibrationSlopeLow || fitted.Slope > CalibrationSlopeHigh)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.CalibrationSlopeFar,
                        $"The validation calibration slope is {fitted.Slope:0.###}, far from 1: the raw decision-function margin was over- or under-confident."));
                }
            }

            var minority = Math.Min(trainPositives, trainNegatives);
            var majority = Math.Max(trainPositives, trainNegatives);

            if (majority / (double)minority > SevereImbalanceRatio)
            {
                warnings.Add(new AdvancedAnalyticsWarning(AdaBoostWarningCodes.SevereImbalance,
                    $"The training classes are imbalanced beyond 1:{SevereImbalanceRatio:0}; consider a different family or a resampled training set."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    false, null);
            }

            var treeSnapshots = trees.Select(t => new AdaBoostTreeSnapshot
            {
                IsLeaf = t.IsLeaf,
                AttributeIndex = t.AttributeIndex,
                Threshold = t.Threshold,
                LeftChild = t.LeftChild,
                RightChild = t.RightChild,
                NodeVote = t.NodeVote
            }).ToArray();

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsAdaBoostClassifierPipelineSnapshot>.Create(
                AdaBoostClassifierRecall.ModelKind, 1, variableNames,
                new AdvancedAnalyticsAdaBoostClassifierPipelineSnapshot
                {
                    FillValues = fill,
                    Means = means,
                    StandardDeviations = deviations,
                    Trees = treeSnapshots,
                    Alphas = alphaArray
                },
                calibrator.HasValue
                    ? new AdvancedAnalyticsCalibratorSnapshot
                    {
                        Method = AdaBoostClassifierRecall.CalibratorMethod,
                        Parameters = calibrator.Value.ToPrimitives()
                    }
                    : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new AdaBoostClassifierResult
            {
                Kind = AdaBoostClassifierRecall.ModelKind,
                Seed = options.Seed,
                Options = options,
                Variables = (string[])variableNames.Clone(),
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new AdaBoostEvidenceDetails
                {
                    Rounds = options.Rounds,
                    RoundsFitted = trees.Count,
                    MaxDepth = options.MaxDepth,
                    MinimumLeafSize = options.MinimumLeafSize,
                    RoundErrors = [.. roundErrors],
                    RoundAlphas = alphaArray,
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

        private static AdaBoostTree BuildWeightedTree(double[][] design, int[] y, double[] weights, int maxDepth,
            int minLeafSize, double[] importance)
        {
            var isLeaf = new List<bool>();
            var featureIndex = new List<int>();
            var threshold = new List<double>();
            var left = new List<int>();
            var right = new List<int>();
            var vote = new List<double>();
            var variableCount = design[0].Length;

            int BuildNode(int[] rows, int depth)
            {
                var totalWeight = 0d;
                var weightedPositive = 0d;

                foreach (var i in rows)
                {
                    totalWeight += weights[i];
                    weightedPositive += weights[i] * y[i];
                }

                var weightedMean = totalWeight > 0 ? weightedPositive / totalWeight : 0.5d;
                var nodeIndex = vote.Count;
                isLeaf.Add(true);
                featureIndex.Add(-1);
                threshold.Add(0d);
                left.Add(-1);
                right.Add(-1);
                vote.Add(weightedMean);

                if (depth >= maxDepth || rows.Length < 2 * minLeafSize || weightedMean <= 0d || weightedMean >= 1d)
                {
                    return nodeIndex;
                }

                var parentImpurity = weightedMean * (1d - weightedMean);

                var bestGain = 0d;
                var bestFeature = -1;
                var bestThreshold = 0d;
                int[] bestLeftRows = null;
                int[] bestRightRows = null;

                for (var feature = 0; feature < variableCount; feature++)
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

                        var leftWeight = 0d;
                        var leftPositive = 0d;

                        foreach (var i in leftRows)
                        {
                            leftWeight += weights[i];
                            leftPositive += weights[i] * y[i];
                        }

                        var rightWeight = 0d;
                        var rightPositive = 0d;

                        foreach (var i in rightRows)
                        {
                            rightWeight += weights[i];
                            rightPositive += weights[i] * y[i];
                        }

                        var leftMean = leftWeight > 0 ? leftPositive / leftWeight : 0d;
                        var rightMean = rightWeight > 0 ? rightPositive / rightWeight : 0d;

                        var weightedImpurity =
                            (leftWeight * leftMean * (1d - leftMean) + rightWeight * rightMean * (1d - rightMean)) /
                            totalWeight;

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

                importance[bestFeature] += totalWeight * bestGain;
                isLeaf[nodeIndex] = false;
                featureIndex[nodeIndex] = bestFeature;
                threshold[nodeIndex] = bestThreshold;

                var leftIndex = BuildNode(bestLeftRows!, depth + 1);
                var rightIndex = BuildNode(bestRightRows!, depth + 1);
                left[nodeIndex] = leftIndex;
                right[nodeIndex] = rightIndex;

                return nodeIndex;
            }

            BuildNode([.. Enumerable.Range(0, design.Length)], 0);

            return new AdaBoostTree([.. isLeaf], [.. featureIndex], [.. threshold], [.. left],
                [.. right], [.. vote]);
        }

        private static AdaBoostSplitMetrics Metrics(string split, double[] probabilities, int[] labels,
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

            return new AdaBoostSplitMetrics(split, labels.Length, positives, auc, ap,
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