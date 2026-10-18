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
    public static class DecisionTreeRegressorTrainer
    {
        public const int MinimumTestRows = 30;
        public const double NearConstantShare = 0.995;

        public static DecisionTreeRegressorResult Train(double[][] inputs, double[] outputs, string[] variableNames,
            DecisionTreeRegressorOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new DecisionTreeRegressorOptions();
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

        public static DecisionTreeRegressorResult TrainWithSplit(double[][] inputs, double[] outputs,
            string[] variableNames, DecisionTreeRegressorOptions options, DataSplitIndices split,
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

                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeRegressorWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split and cannot be split on."));
                }
                else if (groups.Max() / (double)finite.Length >= NearConstantShare)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeRegressorWarningCodes.NearConstantColumn,
                        $"Variable '{variableNames[j]}' takes one value in at least {NearConstantShare:P1} of the training rows."));
                }
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

            cancellationToken.ThrowIfCancellationRequested();

            var isLeaf = new List<bool>();
            var attributeIndex = new List<int>();
            var threshold = new List<double>();
            var leftChild = new List<int>();
            var rightChild = new List<int>();
            var nodeValue = new List<double>();
            var gainByVariable = new double[variableCount];

            BuildNode(trainFilled, trainY, [.. Enumerable.Range(0, trainFilled.Length)], 0, variableCount,
                options.MaxDepth, options.MinimumLeafSize, isLeaf, attributeIndex, threshold, leftChild, rightChild,
                nodeValue, gainByVariable, cancellationToken);

            if (isLeaf[0])
            {
                warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeRegressorWarningCodes.RootIsLeaf,
                    "The tree did not find any useful split and always predicts the training mean."));
            }

            var pipeline = new DecisionTreeRegressorPipeline(fill, [.. isLeaf], [.. attributeIndex],
                [.. threshold], [.. leftChild], [.. rightChild], [.. nodeValue]);

            double[] Predict(double[][] rows)
            {
                return [.. rows.Select(pipeline.LeafValue)];
            }

            var trainPredicted = Predict(trainFilled);
            var validationFilled = validationX.Select(Fill).ToArray();
            var validationPredicted = Predict(validationFilled);
            var testFilled = testX.Select(Fill).ToArray();
            var testPredicted = Predict(testFilled);

            var leafCount = isLeaf.Count(v => v);

            var trainMetrics = Metrics("Train", trainY, trainPredicted, leafCount);
            var validationMetrics = Metrics("Validation", validationY, validationPredicted, leafCount);
            var testMetrics = Metrics("Test", testY, testPredicted, leafCount);

            if (testMetrics == null)
            {
                warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeRegressorWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out metrics; do not promote on training metrics alone."));
            }
            else if (testMetrics.Rows < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeRegressorWarningCodes.TinyTestSet,
                    $"The test split has {testMetrics.Rows} rows, below {MinimumTestRows}; held-out metrics will be noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    excluded[j], excluded[j] ? "Constant" : null);

                if (missingCounts[j] > 0)
                {
                    warnings.Add(new AdvancedAnalyticsWarning(DecisionTreeRegressorWarningCodes.MissingValuesImputed,
                        $"Variable '{variableNames[j]}' had {missingCounts[j]} missing value(s) in the training split, " +
                        $"imputed with the median ({fill[j]:g6})."));
                }
            }

            var importances = Importances(gainByVariable, variableNames);

            var snapshot = new AdvancedAnalyticsDecisionTreeRegressorPipelineSnapshot
            {
                FillValues = (double[])fill.Clone(),
                IsLeaf = [.. isLeaf],
                AttributeIndex = [.. attributeIndex],
                Threshold = [.. threshold],
                LeftChild = [.. leftChild],
                RightChild = [.. rightChild],
                NodeValue = [.. nodeValue]
            };

            var envelope =
                AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsDecisionTreeRegressorPipelineSnapshot>.Create(
                    DecisionTreeRegressorRecall.ModelKind, 1, variableNames, snapshot);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new DecisionTreeRegressorResult
            {
                Kind = DecisionTreeRegressorRecall.ModelKind,
                Seed = options.Seed,
                Variables = (string[])variableNames.Clone(),
                Options = options,
                SplitSizes = new AdvancedAnalyticsSplitSizes(trainRows.Length, validationRows.Length, testRows.Length),
                Preprocessing = preprocessing,
                FillValues = (double[])fill.Clone(),
                MissingCounts = (int[])missingCounts.Clone(),
                Details = new DecisionTreeRegressorEvidenceDetails
                {
                    MaxDepthAllowed = options.MaxDepth,
                    TreeDepth = Depth([.. isLeaf], [.. leftChild], [.. rightChild]),
                    Leaves = leafCount,
                    Nodes = isLeaf.Count,
                    MinimumLeafSize = options.MinimumLeafSize,
                    RootValue = nodeValue[0],
                    TrainMetrics = trainMetrics,
                    ValidationMetrics = validationMetrics,
                    TestMetrics = testMetrics,
                    Importances = importances
                },
                Warnings = [.. warnings],
                SnapshotJson = snapshotJson,
                TrainRows = (int[])trainRows.Clone(),
                ValidationRows = (int[])validationRows.Clone(),
                TestRows = (int[])testRows.Clone()
            };
        }

        private static void BuildNode(double[][] filledRows, double[] outputs, int[] rows, int depth,
            int variableCount, int maxDepth, int minimumLeafSize, List<bool> isLeaf, List<int> attributeIndex,
            List<double> threshold, List<int> leftChild, List<int> rightChild, List<double> nodeValue,
            double[] gainByVariable, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var index = isLeaf.Count;
            isLeaf.Add(false);
            attributeIndex.Add(-1);
            threshold.Add(double.NaN);
            leftChild.Add(-1);
            rightChild.Add(-1);

            var sum = 0d;

            foreach (var r in rows)
            {
                sum += outputs[r];
            }

            var mean = sum / rows.Length;
            nodeValue.Add(mean);

            var sse = 0d;

            foreach (var r in rows)
            {
                var d = outputs[r] - mean;
                sse += d * d;
            }

            if (depth >= maxDepth || rows.Length < 2 * minimumLeafSize || sse <= 1e-9)
            {
                isLeaf[index] = true;

                return;
            }

            var bestGain = 1e-9;
            var bestVariable = -1;
            var bestThreshold = 0d;
            int[] bestLeft = null;
            int[] bestRight = null;

            for (var variable = 0; variable < variableCount; variable++)
            {
                var ordered = rows.OrderBy(r => filledRows[r][variable]).ToArray();
                var leftSum = 0d;

                for (var k = 0; k < ordered.Length - 1; k++)
                {
                    leftSum += outputs[ordered[k]];
                    var leftCount = k + 1;
                    var rightCount = ordered.Length - leftCount;

                    if (filledRows[ordered[k]][variable] >= filledRows[ordered[k + 1]][variable])
                    {
                        continue;
                    }

                    if (leftCount < minimumLeafSize || rightCount < minimumLeafSize)
                    {
                        continue;
                    }

                    var rightSum = sum - leftSum;

                    var gain = leftSum * leftSum / leftCount + rightSum * rightSum / rightCount
                               - sum * sum / rows.Length;

                    if (gain > bestGain)
                    {
                        bestGain = gain;
                        bestVariable = variable;
                        bestThreshold = (filledRows[ordered[k]][variable] + filledRows[ordered[k + 1]][variable]) / 2d;
                        bestLeft = ordered[..leftCount];
                        bestRight = ordered[leftCount..];
                    }
                }
            }

            if (bestVariable < 0)
            {
                isLeaf[index] = true;

                return;
            }

            gainByVariable[bestVariable] += bestGain;
            attributeIndex[index] = bestVariable;
            threshold[index] = bestThreshold;

            var leftIndex = isLeaf.Count;

            BuildNode(filledRows, outputs, bestLeft!, depth + 1, variableCount, maxDepth, minimumLeafSize, isLeaf,
                attributeIndex, threshold, leftChild, rightChild, nodeValue, gainByVariable, cancellationToken);

            var rightIndex = isLeaf.Count;

            BuildNode(filledRows, outputs, bestRight!, depth + 1, variableCount, maxDepth, minimumLeafSize, isLeaf,
                attributeIndex, threshold, leftChild, rightChild, nodeValue, gainByVariable, cancellationToken);

            leftChild[index] = leftIndex;
            rightChild[index] = rightIndex;
        }

        private static int Depth(bool[] isLeaf, int[] leftChild, int[] rightChild)
        {
            int Walk(int node, int depth)
            {
                return isLeaf[node]
                    ? depth
                    : Math.Max(Walk(leftChild[node], depth + 1), Walk(rightChild[node], depth + 1));
            }

            return Walk(0, 0);
        }

        private static DecisionTreeRegressorVariableImportance[] Importances(double[] gainByVariable,
            string[] variableNames)
        {
            var sum = gainByVariable.Sum();

            return
            [
                .. variableNames.Select((name, j) => new DecisionTreeRegressorVariableImportance
                {
                    Name = name,
                    Importance = sum > 0 ? gainByVariable[j] / sum : 0d
                })
            ];
        }

        private static Models.DecisionTreeRegressorSplitMetrics Metrics(string split, double[] actual,
            double[] predicted,
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

            return new Models.DecisionTreeRegressorSplitMetrics(split, n, rSquared, adjustedRSquared,
                Math.Sqrt(sse / n),
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