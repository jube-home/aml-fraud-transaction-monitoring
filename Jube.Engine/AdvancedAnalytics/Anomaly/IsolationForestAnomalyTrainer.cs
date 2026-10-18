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
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Engine.AdvancedAnalytics.Anomaly
{
    public static class IsolationForestAnomalyTrainer
    {
        public const int MinimumTestRows = 50;
        public const double NearConstantShare = 0.995;

        public static IsolationForestAnomalyResult Train(double[][] inputs, int[] labels, string[] variableNames,
            IsolationForestAnomalyOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new IsolationForestAnomalyOptions();
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

        public static IsolationForestAnomalyResult TrainWithSplit(double[][] inputs, int[] labels,
            string[] variableNames, IsolationForestAnomalyOptions options, DataSplitIndices split,
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

            if (trainX.Length == 0)
            {
                throw new ArgumentException("The training split has no rows.");
            }

            var fill = new double[variableCount];
            var means = new double[variableCount];
            var deviations = new double[variableCount];
            var excluded = new bool[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                var column = new double[trainX.Length];

                for (var i = 0; i < column.Length; i++)
                {
                    column[i] = trainX[i][j];
                }

                var finite = column.Where(double.IsFinite).ToArray();

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

                    warnings.Add(new AdvancedAnalyticsWarning(IsolationForestWarningCodes.ConstantColumn,
                        $"Variable '{variableNames[j]}' is constant in the training split; a random split can never " +
                        "land on it, so it never isolates a row."));
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

            var subsampleSize = Math.Min(options.SubsampleSize, trainFilled.Length);
            var depthLimit = (int)Math.Ceiling(Math.Log2(Math.Max(subsampleSize, 2)));
            var builtTrees = new BuiltTree[options.TreeCount];

            for (var t = 0; t < options.TreeCount; t++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var random = new Random(DeriveSeed(options.Seed, t));

                var rows = Enumerable.Range(0, trainFilled.Length).OrderBy(_ => random.Next()).Take(subsampleSize)
                    .ToArray();

                var featureIndex = new List<int>();
                var splitValue = new List<double>();
                var left = new List<int>();
                var right = new List<int>();
                var size = new List<int>();

                BuildNode(trainFilled, rows, 0, depthLimit, variableCount, random, featureIndex, splitValue, left,
                    right, size);

                builtTrees[t] = new BuiltTree([.. featureIndex], [.. splitValue], [.. left],
                    [.. right], [.. size]);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var normaliser = Math.Max(IsolationForestAnomalyPipeline.AveragePath(subsampleSize), 1e-9);

            double PathOf(BuiltTree tree, double[] row)
            {
                var node = 0;
                var depth = 0d;

                while (tree.FeatureIndex[node] != -1)
                {
                    var feature = tree.FeatureIndex[node];
                    node = row[feature] < tree.SplitValue[node] ? tree.Left[node] : tree.Right[node];
                    depth++;
                }

                return depth + IsolationForestAnomalyPipeline.AveragePath(tree.Size[node]);
            }

            double ScoreOf(double[] row)
            {
                var total = 0d;

                foreach (var tree in builtTrees)
                {
                    total += PathOf(tree, row);
                }

                return Math.Pow(2d, -(total / builtTrees.Length) / normaliser);
            }

            double[] Scores(double[][] rows)
            {
                return [.. rows.Select(row => ScoreOf(Fill(row)))];
            }

            var trainScores = Scores(trainX);
            var validationScores = Scores(validationX);
            var testScores = Scores(testX);

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

                warnings.Add(new AdvancedAnalyticsWarning(IsolationForestWarningCodes.CalibrationSkipped,
                    "No labels were supplied for calibration; the model reports the raw isolation forest anomaly " +
                    "score rather than a calibrated probability."));
            }
            else if (validationLabels.Length == 0 ||
                     validationLabels.All(l => l == 0) || validationLabels.All(l => l == 1))
            {
                calibrationReason = "The validation split does not contain both a normal and a known-anomalous row.";

                warnings.Add(new AdvancedAnalyticsWarning(IsolationForestWarningCodes.CalibrationSkipped,
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

                    warnings.Add(new AdvancedAnalyticsWarning(IsolationForestWarningCodes.CalibrationSkipped,
                        "Calibration could not be fitted on the validation split: " + exception.Message));
                }
            }

            if (calibrator is { IsIncreasing: false })
            {
                warnings.Add(new AdvancedAnalyticsWarning(IsolationForestWarningCodes.CalibratorNotIncreasing,
                    "The calibrator has a non-positive slope: on the validation split, a higher raw score does not " +
                    "predict a higher rate of known anomalies."));
            }

            if (testX.Length == 0)
            {
                warnings.Add(new AdvancedAnalyticsWarning(IsolationForestWarningCodes.NoTestSet,
                    "There is no test split, so there are no held-out score statistics; do not promote on training " +
                    "statistics alone."));
            }
            else if (testX.Length < MinimumTestRows)
            {
                warnings.Add(new AdvancedAnalyticsWarning(IsolationForestWarningCodes.TinyTestSet,
                    $"The test split has {testX.Length} rows, below {MinimumTestRows}; held-out statistics will be " +
                    "noisy."));
            }

            var preprocessing = new AdvancedAnalyticsVariablePreprocessing[variableCount];

            for (var j = 0; j < variableCount; j++)
            {
                preprocessing[j] = new AdvancedAnalyticsVariablePreprocessing(variableNames[j], means[j], deviations[j],
                    excluded[j], excluded[j] ? "Constant" : null);
            }

            var details = new IsolationForestEvidenceDetails
            {
                TreeCount = options.TreeCount,
                SubsampleSize = subsampleSize,
                TrainScores = PercentilesOf(trainScores),
                ValidationScores = PercentilesOf(validationScores),
                TestScores = PercentilesOf(testScores),
                Calibration = new IsolationForestCalibrationInfo(calibrator.HasValue, calibrator?.Slope,
                    calibrator?.Intercept, calibrationRows, calibrationReason),
                ValidationLabelledMetrics = LabelledMetricsOf(validationScores, validationLabels),
                TestLabelledMetrics = LabelledMetricsOf(testScores, testLabels)
            };

            var treeSnapshots = builtTrees.Select(tree => new IsolationForestTreeSnapshot
            {
                FeatureIndex = tree.FeatureIndex,
                SplitValue = tree.SplitValue,
                Left = tree.Left,
                Right = tree.Right,
                Size = tree.Size
            }).ToArray();

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsIsolationForestAnomalyPipelineSnapshot>
                .Create(
                    IsolationForestAnomalyRecall.ModelKind, 1, variableNames,
                    new AdvancedAnalyticsIsolationForestAnomalyPipelineSnapshot
                    {
                        FillValues = fill,
                        SubsampleSize = subsampleSize,
                        Trees = treeSnapshots
                    },
                    calibrator.HasValue
                        ? new AdvancedAnalyticsCalibratorSnapshot
                        {
                            Method = IsolationForestAnomalyRecall.CalibratorMethod,
                            Parameters = calibrator.Value.ToPrimitives()
                        }
                        : null);

            var snapshotJson = AdvancedAnalyticsModelJson.Serialise(envelope);

            return new IsolationForestAnomalyResult
            {
                Kind = IsolationForestAnomalyRecall.ModelKind,
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

        private static void BuildNode(double[][] filledRows, int[] rows, int depth, int depthLimit, int variableCount,
            Random random, List<int> featureIndex, List<double> splitValue, List<int> left, List<int> right,
            List<int> size)
        {
            var index = featureIndex.Count;
            featureIndex.Add(-1);
            splitValue.Add(double.NaN);
            left.Add(-1);
            right.Add(-1);
            size.Add(rows.Length);

            if (depth >= depthLimit || rows.Length <= 1)
            {
                return;
            }

            for (var attempt = 0; attempt < variableCount * 2; attempt++)
            {
                var feature = random.Next(variableCount);
                var low = double.MaxValue;
                var high = double.MinValue;

                foreach (var r in rows)
                {
                    var value = filledRows[r][feature];
                    low = Math.Min(low, value);
                    high = Math.Max(high, value);
                }

                if (high <= low)
                {
                    continue;
                }

                var threshold = low + random.NextDouble() * (high - low);
                var leftRows = rows.Where(r => filledRows[r][feature] < threshold).ToArray();
                var rightRows = rows.Where(r => filledRows[r][feature] >= threshold).ToArray();

                if (leftRows.Length == 0 || rightRows.Length == 0)
                {
                    continue;
                }

                featureIndex[index] = feature;
                splitValue[index] = threshold;

                var leftIndex = featureIndex.Count;

                BuildNode(filledRows, leftRows, depth + 1, depthLimit, variableCount, random, featureIndex, splitValue,
                    left, right, size);

                var rightIndex = featureIndex.Count;

                BuildNode(filledRows, rightRows, depth + 1, depthLimit, variableCount, random, featureIndex, splitValue,
                    left, right, size);

                left[index] = leftIndex;
                right[index] = rightIndex;

                return;
            }
        }

        private static int DeriveSeed(int seed, int index)
        {
            return unchecked(seed * 7919 + index * 104729 + 17);
        }

        private static IsolationForestScorePercentiles PercentilesOf(double[] values)
        {
            var sorted = values.Where(double.IsFinite).OrderBy(v => v).ToArray();

            if (sorted.Length == 0)
            {
                return null;
            }

            return new IsolationForestScorePercentiles(sorted.Length, Percentile(sorted, 0.5), Percentile(sorted, 0.9),
                Percentile(sorted, 0.95), Percentile(sorted, 0.99), sorted[^1]);
        }

        private static IsolationForestLabelledMetrics LabelledMetricsOf(double[] scores, int[] splitLabels)
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

            return new IsolationForestLabelledMetrics(splitLabels.Length, known,
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

        private sealed record BuiltTree(int[] FeatureIndex, double[] SplitValue, int[] Left, int[] Right, int[] Size);
    }
}