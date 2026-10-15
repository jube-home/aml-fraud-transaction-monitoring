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
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Math.Random;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public static class DecisionTreeClassifierTrainer
    {
        public const int MinimumTestRows = 50;
        public const int MinimumTestClassRows = 5;
        public const double NearConstantShare = 0.995;
        public const double SevereImbalanceRatio = 100d;
        public const double CalibrationSlopeLow = 0.7;
        public const double CalibrationSlopeHigh = 1.3;
        public const double LaplaceSmoothing = 1d;

        public static DecisionTreeClassifierResult Train(double[][] inputs, int[] labels, string[] variableNames,
            DecisionTreeClassifierOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new DecisionTreeClassifierOptions();
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

        public static DecisionTreeClassifierResult TrainWithSplit(double[][] inputs, int[] labels,
            string[] variableNames, DecisionTreeClassifierOptions options, DataSplitIndices split,
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
            var excluded = new bool[variableCount];

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
                    excluded[j] = true;

                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split and was excluded from the fit."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.NearConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }
            }

            var activeInputs = Enumerable.Range(0, variableCount).Where(j => !excluded[j]).ToArray();

            if (activeInputs.Length == 0)
            {
                throw new ArgumentException(
                    "Every variable is constant in the training split; there is nothing to fit.");
            }

            double[] Fill(double[] row)
            {
                var filled = new double[variableCount];

                for (var j = 0; j < variableCount; j++)
                {
                    filled[j] = double.IsFinite(row[j]) ? row[j] : fill[j];
                }

                return filled;
            }

            var trainFilled = trainX.Select(Fill).ToArray();
            var activeTrainDesign = trainFilled.Select(row => activeInputs.Select(j => row[j]).ToArray()).ToArray();
            var trainWeights = ClassWeights(trainY, options.ClassWeighting);

            cancellationToken.ThrowIfCancellationRequested();

            var minimumSplit = Math.Max(2, 2 * options.MinimumLeafSize);
            var variables = activeInputs.Select(j => DecisionVariable.Continuous(variableNames[j])).ToArray();

            var learner = new C45Learning(variables)
            {
                MaxHeight = options.MaxDepth,
                Join = 0,
                MinimumLeafSize = options.MinimumLeafSize,
                MinimumSplitSize = minimumSplit,
                Token = cancellationToken
            };

            DecisionTree tree;
            var previousSeed = Generator.ThreadSeed;

            try
            {
                Generator.ThreadSeed = options.Seed;
                tree = learner.Learn(activeTrainDesign, trainY, trainWeights);
            }
            finally
            {
                Generator.ThreadSeed = previousSeed;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var flattened = Flatten(tree, activeInputs);
            var stats = AccumulateStats(flattened, trainFilled, trainY, trainWeights);
            var nodeProbability = SmoothedProbabilities(flattened, stats);

            if (flattened.IsLeaf[0])
            {
                warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.RootIsLeaf,
                    "The tree did not find any useful split and always predicts the same probability."));
            }

            var pipeline = new DecisionTreeClassifierPipeline(fill, flattened.IsLeaf, flattened.AttributeIndex,
                flattened.Threshold, flattened.LeftChild, flattened.RightChild, nodeProbability);

            double[] RawProbabilities(double[][] filledRows)
            {
                return [.. filledRows.Select(pipeline.LeafProbability)];
            }

            var trainRaw = RawProbabilities(trainFilled);
            var validationFilled = validationX.Select(Fill).ToArray();
            var validationRaw = RawProbabilities(validationFilled);
            var testFilled = testX.Select(Fill).ToArray();
            var testRaw = RawProbabilities(testFilled);

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

                warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.CalibrationSkipped,
                    "Calibration was requested but the validation split does not contain both classes, so the model is uncalibrated."));
            }
            else
            {
                var validationWeights = options.ClassWeighting == DecisionTreeClassWeighting.Balanced
                    ? ClassWeights(validationY, DecisionTreeClassWeighting.Balanced)
                    : null;

                try
                {
                    calibrator = PlattCalibration.Fit(validationRaw, validationY, validationWeights,
                        CalibrationInput.LogitOfBoundedScore, cancellationToken: cancellationToken);

                    calibrationRows = validationY.Length;
                }
                catch (InvalidOperationException exception)
                {
                    calibrationReason = exception.Message;

                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            double[] Calibrated(double[] raw)
            {
                return calibrator.HasValue ? calibrator.Value.Apply(raw) : raw;
            }

            var trainFinal = Calibrated(trainRaw);
            var validationFinal = Calibrated(validationRaw);
            var testFinal = Calibrated(testRaw);

            var trainMetrics = Metrics("Train", trainFinal, trainY, options.DecisionThreshold);
            var validationMetrics = Metrics("Validation", validationFinal, validationY, options.DecisionThreshold);
            var testMetrics = Metrics("Test", testFinal, testY, options.DecisionThreshold);

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows ||
                     Math.Min(testMetrics.Positives, testMetrics.Rows - testMetrics.Positives) < MinimumTestClassRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows with {testMetrics.Positives} positives; held-out metrics will be noisy."));
            }

            if (calibrator is { } fitted)
            {
                if (!fitted.IsIncreasing)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.CalibratorNotIncreasing,
                        "The calibrator has a non-positive slope: on the validation split the model ranks cases the wrong way round."));
                }

                if (fitted.Slope < CalibrationSlopeLow || fitted.Slope > CalibrationSlopeHigh)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.CalibrationSlopeFar,
                        $"The validation calibration slope is {fitted.Slope:0.###}, far from 1: the raw leaf probabilities were over- or under-confident."));
                }
            }

            var minority = Math.Min(trainPositives, trainNegatives);
            var majority = Math.Max(trainPositives, trainNegatives);

            if (options.ClassWeighting == DecisionTreeClassWeighting.None &&
                majority / (double)minority > SevereImbalanceRatio)
            {
                warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.SevereImbalance,
                    $"The training classes are imbalanced beyond 1:{SevereImbalanceRatio:0} and no class weighting was used; consider Balanced weights."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    excluded[j], excluded[j] ? "Constant" : null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            var importances = Importances(flattened, stats, variableNames);

            var snapshot = new AdvancedAnalyticsDecisionTreeClassifierPipelineSnapshot
            {
                FillValues = (double[])fill.Clone(),
                IsLeaf = flattened.IsLeaf,
                AttributeIndex = flattened.AttributeIndex,
                Threshold = flattened.Threshold,
                LeftChild = flattened.LeftChild,
                RightChild = flattened.RightChild,
                NodeProbability = nodeProbability
            };

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsDecisionTreeClassifierPipelineSnapshot>
                .Create(
                    DecisionTreeClassifierRecall.ModelKind, 1, variableNames, snapshot,
                    calibrator.HasValue
                        ? new AdvancedAnalyticsCalibratorSnapshot
                        {
                            Method = DecisionTreeClassifierRecall.CalibratorMethod,
                            Parameters = calibrator.Value.ToPrimitives()
                        }
                        : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new DecisionTreeClassifierResult
            {
                Kind = DecisionTreeClassifierRecall.ModelKind,
                Seed = options.Seed,
                Variables = (string[])variableNames.Clone(),
                Options = options,
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new DecisionTreeEvidenceDetails
                {
                    MaxDepthAllowed = options.MaxDepth,
                    TreeDepth = Depth(flattened),
                    Leaves = flattened.IsLeaf.Count(v => v),
                    Nodes = flattened.IsLeaf.Length,
                    MinimumLeafSize = options.MinimumLeafSize,
                    RootProbability = nodeProbability[0],
                    TrainMetrics = trainMetrics,
                    ValidationMetrics = validationMetrics,
                    TestMetrics = testMetrics,
                    Calibrated = calibrator.HasValue,
                    CalibrationSlope = calibrator?.Slope,
                    CalibrationIntercept = calibrator?.Intercept,
                    CalibrationRowsUsed = calibrationRows,
                    CalibrationSkippedReason = calibrationReason,
                    Importances = importances
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static FlattenedTree Flatten(DecisionTree tree, int[] activeInputs)
        {
            var isLeaf = new List<bool>();
            var attributeIndex = new List<int>();
            var threshold = new List<double>();
            var leftChild = new List<int>();
            var rightChild = new List<int>();

            int Visit(DecisionNode node)
            {
                var index = isLeaf.Count;
                isLeaf.Add(false);
                attributeIndex.Add(-1);
                threshold.Add(double.NaN);
                leftChild.Add(-1);
                rightChild.Add(-1);

                if (node.IsLeaf)
                {
                    isLeaf[index] = true;

                    return index;
                }

                var branches = node.Branches;
                var left = branches.FirstOrDefault(b => b.Comparison == ComparisonKind.LessThanOrEqual);
                var right = branches.FirstOrDefault(b => b.Comparison == ComparisonKind.GreaterThan);

                if (branches.Count != 2 || left == null || right == null || left.Value is not { } leftValue ||
                    right.Value is not { } rightValue || Math.Abs(leftValue - rightValue) > 0.0001)
                {
                    throw new InvalidOperationException(
                        "The fitted tree has a split shape this recall pipeline does not support.");
                }

                attributeIndex[index] = activeInputs[branches.AttributeIndex];
                threshold[index] = leftValue;

                var leftIndex = Visit(left);
                var rightIndex = Visit(right);

                leftChild[index] = leftIndex;
                rightChild[index] = rightIndex;

                return index;
            }

            Visit(tree.Root);

            return new FlattenedTree
            {
                IsLeaf = [.. isLeaf],
                AttributeIndex = [.. attributeIndex],
                Threshold = [.. threshold],
                LeftChild = [.. leftChild],
                RightChild = [.. rightChild]
            };
        }

        private static (double[] Positive, double[] Total) AccumulateStats(FlattenedTree flattened,
            double[][] filledRows, int[] labels, double[] weights)
        {
            var nodeCount = flattened.IsLeaf.Length;
            var positive = new double[nodeCount];
            var total = new double[nodeCount];

            for (var i = 0; i < filledRows.Length; i++)
            {
                var w = weights?[i] ?? 1d;
                var node = 0;

                while (true)
                {
                    positive[node] += w * labels[i];
                    total[node] += w;

                    if (flattened.IsLeaf[node])
                    {
                        break;
                    }

                    var value = filledRows[i][flattened.AttributeIndex[node]];
                    node = value <= flattened.Threshold[node] ? flattened.LeftChild[node] : flattened.RightChild[node];
                }
            }

            return (positive, total);
        }

        private static double[] SmoothedProbabilities(FlattenedTree flattened,
            (double[] Positive, double[] Total) stats)
        {
            var nodeCount = flattened.IsLeaf.Length;
            var probability = new double[nodeCount];
            var rootProbability = (stats.Positive[0] + LaplaceSmoothing) / (stats.Total[0] + 2d * LaplaceSmoothing);

            for (var i = 0; i < nodeCount; i++)
            {
                probability[i] = stats.Total[i] > 0
                    ? (stats.Positive[i] + LaplaceSmoothing) / (stats.Total[i] + 2d * LaplaceSmoothing)
                    : rootProbability;
            }

            return probability;
        }

        private static DecisionTreeVariableImportance[] Importances(FlattenedTree flattened,
            (double[] Positive, double[] Total) stats, string[] variableNames)
        {
            var raw = new double[variableNames.Length];

            for (var i = 0; i < flattened.IsLeaf.Length; i++)
            {
                if (flattened.IsLeaf[i] || stats.Total[i] <= 0)
                {
                    continue;
                }

                var left = flattened.LeftChild[i];
                var right = flattened.RightChild[i];

                var gain = Gini(stats.Positive[i], stats.Total[i]) * stats.Total[i]
                           - Gini(stats.Positive[left], stats.Total[left]) * stats.Total[left]
                           - Gini(stats.Positive[right], stats.Total[right]) * stats.Total[right];

                if (gain > 0)
                {
                    raw[flattened.AttributeIndex[i]] += gain;
                }
            }

            var sum = raw.Sum();

            return
            [
                .. variableNames.Select((name, j) => new DecisionTreeVariableImportance
                {
                    Name = name,
                    Importance = sum > 0 ? raw[j] / sum : 0d
                })
            ];
        }

        private static double Gini(double positive, double total)
        {
            if (total <= 0)
            {
                return 0d;
            }

            var p = positive / total;

            return 2d * p * (1d - p);
        }

        private static int Depth(FlattenedTree flattened)
        {
            int Walk(int node, int depth)
            {
                return flattened.IsLeaf[node]
                    ? depth
                    : Math.Max(Walk(flattened.LeftChild[node], depth + 1), Walk(flattened.RightChild[node], depth + 1));
            }

            return Walk(0, 0);
        }

        private static double[] ClassWeights(int[] labels, DecisionTreeClassWeighting weighting)
        {
            if (weighting == DecisionTreeClassWeighting.None)
            {
                return null;
            }

            var positives = labels.Sum();
            var negatives = labels.Length - positives;

            if (positives == 0 || negatives == 0)
            {
                return null;
            }

            var positiveWeight = labels.Length / (2d * positives);
            var negativeWeight = labels.Length / (2d * negatives);

            return [.. labels.Select(l => l == 1 ? positiveWeight : negativeWeight)];
        }

        private static DecisionTreeSplitMetrics Metrics(string split, double[] probabilities, int[] labels,
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

            var confusion = BinaryMetrics.Confusion(probabilities, labels, threshold);

            return new DecisionTreeSplitMetrics
            {
                Split = split,
                Rows = labels.Length,
                Positives = positives,
                RocAuc = bothClasses ? Number(BinaryMetrics.RocAuc(probabilities, labels)) : null,
                LogLoss = Number(BinaryMetrics.LogLoss(probabilities, labels)),
                BrierScore = Number(BinaryMetrics.BrierScore(probabilities, labels)),
                Threshold = threshold,
                TruePositives = confusion.TruePositives,
                FalsePositives = confusion.FalsePositives,
                TrueNegatives = confusion.TrueNegatives,
                FalseNegatives = confusion.FalseNegatives,
                Precision = Number(confusion.Precision),
                Recall = Number(confusion.Recall),
                F1 = Number(confusion.F1),
                MatthewsCorrelation = Number(confusion.Mcc)
            };
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

        private sealed class FlattenedTree
        {
            public required bool[] IsLeaf { get; init; }
            public required int[] AttributeIndex { get; init; }
            public required double[] Threshold { get; init; }
            public required int[] LeftChild { get; init; }
            public required int[] RightChild { get; init; }
        }
    }
}