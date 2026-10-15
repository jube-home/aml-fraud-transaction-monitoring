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

using System.ComponentModel;
using Jube.Analytics;
using Jube.Analytics.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SamplingTools
    {
        [AdvancedAnalyticsTool("SampleSimpleRandom")]
        [Description(
            "Draws a simple random sample of row positions (0-based indices) from a table of rowCount rows, reproducibly from a seed. " +
            "Without replacement every row appears at most once and the size cannot exceed rowCount; with replacement rows can repeat. " +
            "Returns the selected indices (sorted when sampling without replacement, in draw order otherwise) and the indices not selected. Use the same seed to reproduce the draw.")]
        public IndexSampleResult SampleSimpleRandom(
            [Description("Number of rows in the population, at least 1.")]
            int rowCount,
            [Description("Number of rows to draw, at least 1.")]
            int sampleSize,
            [Description("When true a row can be drawn more than once. Default false.")]
            bool withReplacement = false,
            [Description("Seed that makes the draw reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Positive(rowCount, nameof(rowCount));
            DataPreparationGuard.Positive(sampleSize, nameof(sampleSize));

            if (!withReplacement && sampleSize > rowCount)
            {
                throw new ArgumentException(
                    $"Cannot draw {sampleSize} rows without replacement from {rowCount}. Lower sampleSize or set withReplacement to true.",
                    nameof(sampleSize));
            }

            var random = new Random(seed);
            int[] indices;

            if (withReplacement)
            {
                indices = Enumerable.Range(0, sampleSize).Select(_ => random.Next(rowCount)).ToArray();
            }
            else
            {
                var all = DataPreparationGuard.Range(rowCount);
                DataPreparationGuard.Shuffle(all, random);
                indices = all.Take(sampleSize).OrderBy(i => i).ToArray();
            }

            return new IndexSampleResult(indices, DataPreparationGuard.Complement(indices, rowCount), seed);
        }

        [AdvancedAnalyticsTool("SampleStratified")]
        [Description(
            "Draws a random sample within each stratum (for example each customer segment or class) so every stratum is represented. " +
            "With proportional allocation each stratum contributes in proportion to its size (largest remainder rounding); with equal allocation each contributes the same number, capped at its size. " +
            "Returns the selected row indices sorted, the indices not selected and an allocation table of stratum, population and number sampled. Reproducible from the seed.")]
        public StratifiedSampleResult SampleStratified(
            [Description(
                "The stratum name for each row, in row order; rows with the same text belong to the same stratum.")]
            string[] strata,
            [Description("Total number of rows to draw, at least 1 and not more than the number of rows.")]
            int sampleSize,
            [Description(
                "When true, every stratum contributes equally instead of in proportion to its size. Default false.")]
            bool equalAllocation = false,
            [Description("Seed that makes the draw reproducible. Default 1.")]
            int seed = 1)
        {
            if (strata == null || strata.Length == 0)
            {
                throw new ArgumentException("Give one stratum name per row.", nameof(strata));
            }

            if (sampleSize < 1 || sampleSize > strata.Length)
            {
                throw new ArgumentException($"sampleSize must be between 1 and {strata.Length}.", nameof(sampleSize));
            }

            var groups = strata.Select((s, i) => (Key: s ?? string.Empty, Row: i))
                .GroupBy(x => x.Key, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => (g.Key, Rows: g.Select(x => x.Row).ToArray()))
                .ToArray();

            var allocation = equalAllocation
                ? EqualAllocation(groups.Select(g => g.Rows.Length).ToArray(), sampleSize)
                : ProportionalAllocation(groups.Select(g => g.Rows.Length).ToArray(), sampleSize);

            var random = new Random(seed);
            var chosen = new List<int>();

            for (var g = 0; g < groups.Length; g++)
            {
                var rows = (int[])groups[g].Rows.Clone();
                DataPreparationGuard.Shuffle(rows, random);
                chosen.AddRange(rows.Take(allocation[g]));
            }

            var indices = chosen.OrderBy(i => i).ToArray();

            return new StratifiedSampleResult(indices, DataPreparationGuard.Complement(indices, strata.Length),
                groups.Select((g, k) => new StratumAllocation(g.Key, g.Rows.Length, allocation[k])).ToArray(), seed);
        }

        [AdvancedAnalyticsTool("SampleSystematic")]
        [Description(
            "Draws a systematic sample: every k-th row after a random start, where k = rowCount / sampleSize. Rows should be in an order with no hidden periodicity that lines up with k (for example not every 7th day when k is 7). " +
            "Use it for evenly spread audit samples over time-ordered rows. Returns the selected indices ascending, the interval k used and the random start.")]
        public SystematicSampleResult SampleSystematic(
            [Description("Number of rows in the population, at least 1.")]
            int rowCount,
            [Description("Number of rows to select, at least 1 and not more than rowCount.")]
            int sampleSize,
            [Description("Seed that makes the random start reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Positive(rowCount, nameof(rowCount));

            if (sampleSize < 1 || sampleSize > rowCount)
            {
                throw new ArgumentException($"sampleSize must be between 1 and {rowCount}.", nameof(sampleSize));
            }

            var interval = rowCount / (double)sampleSize;
            var start = new Random(seed).NextDouble() * interval;

            var indices = Enumerable.Range(0, sampleSize)
                .Select(i => Math.Min(rowCount - 1, (int)Math.Floor(start + i * interval)))
                .ToArray();

            return new SystematicSampleResult(indices, interval, start);
        }

        [AdvancedAnalyticsTool("SampleReservoir")]
        [Description(
            "Draws a uniform random sample of k row positions from a stream of streamLength rows using reservoir sampling (Algorithm R), which needs a single pass and memory for k rows only. " +
            "Every row ends up equally likely to be selected. Returns the selected indices ascending. If k is at least streamLength every row is returned. Reproducible from the seed.")]
        public int[] SampleReservoir(
            [Description("Total number of rows seen in the stream, at least 1.")]
            int streamLength,
            [Description("Number of rows to keep in the reservoir, at least 1.")]
            int reservoirSize,
            [Description("Seed that makes the draw reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Positive(streamLength, nameof(streamLength));
            DataPreparationGuard.Positive(reservoirSize, nameof(reservoirSize));

            var random = new Random(seed);
            var reservoir = new List<int>();

            for (var i = 0; i < streamLength; i++)
            {
                if (i < reservoirSize)
                {
                    reservoir.Add(i);
                }
                else
                {
                    var j = random.Next(i + 1);

                    if (j < reservoirSize)
                    {
                        reservoir[j] = i;
                    }
                }
            }

            return reservoir.OrderBy(i => i).ToArray();
        }

        [AdvancedAnalyticsTool("SampleWeighted")]
        [Description(
            "Draws a random sample where each row's chance of selection is proportional to its weight (probability proportional to size), for example sampling transactions by amount so large ones are audited more often. " +
            "Without replacement it uses the exponential-key method (each row gets key u^(1/weight) and the largest keys win); with replacement each draw is independent. " +
            "Rows with weight 0 are never selected. Returns the selected indices (ascending without replacement, draw order with replacement). Reproducible from the seed.")]
        public int[] SampleWeighted(
            [Description("A non-negative weight for each row, in row order. At least one must be positive.")]
            double[] weights,
            [Description("Number of rows to draw, at least 1.")]
            int sampleSize,
            [Description("When true a row can be drawn more than once. Default false.")]
            bool withReplacement = false,
            [Description("Seed that makes the draw reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Vector(weights, nameof(weights));
            DataPreparationGuard.Positive(sampleSize, nameof(sampleSize));

            if (weights.Any(w => double.IsNaN(w) || double.IsInfinity(w) || w < 0))
            {
                throw new ArgumentException("Weights must be finite and not negative.", nameof(weights));
            }

            var positive = weights.Count(w => w > 0);

            if (positive == 0)
            {
                throw new ArgumentException("At least one weight must be positive.", nameof(weights));
            }

            var random = new Random(seed);

            if (!withReplacement)
            {
                if (sampleSize > positive)
                {
                    throw new ArgumentException(
                        $"Only {positive} rows have a positive weight, so {sampleSize} cannot be drawn without replacement.",
                        nameof(sampleSize));
                }

                return Enumerable.Range(0, weights.Length).Where(i => weights[i] > 0)
                    .Select(i => (Row: i, Key: Math.Log(1 - random.NextDouble()) / weights[i]))
                    .OrderByDescending(x => x.Key).Take(sampleSize).Select(x => x.Row).OrderBy(i => i).ToArray();
            }

            var cumulative = new double[weights.Length];
            var total = 0d;

            for (var i = 0; i < weights.Length; i++)
            {
                total += weights[i];
                cumulative[i] = total;
            }

            return Enumerable.Range(0, sampleSize).Select(_ =>
            {
                var u = random.NextDouble() * total;
                var position = Array.FindIndex(cumulative, c => c > u);

                return position < 0 ? weights.Length - 1 : position;
            }).ToArray();
        }

        [AdvancedAnalyticsTool("SampleClusters")]
        [Description(
            "Draws a cluster sample: chooses whole groups (for example accounts or devices) at random and returns every row that belongs to a chosen group. " +
            "Use it when rows within a group are not independent and you must not split a group. Returns the chosen group names, the row indices of all their rows ascending and the rows not selected. Reproducible from the seed.")]
        public ClusterSampleResult SampleClusters(
            [Description(
                "The group name for each row, in row order; rows with the same text belong to the same group.")]
            string[] groupKeys,
            [Description("Number of groups to choose, at least 1 and not more than the number of distinct groups.")]
            int groupCount,
            [Description("Seed that makes the draw reproducible. Default 1.")]
            int seed = 1)
        {
            if (groupKeys == null || groupKeys.Length == 0)
            {
                throw new ArgumentException("Give one group name per row.", nameof(groupKeys));
            }

            var distinct = groupKeys.Select(g => g ?? string.Empty).Distinct(StringComparer.Ordinal)
                .OrderBy(g => g, StringComparer.Ordinal).ToArray();

            if (groupCount < 1 || groupCount > distinct.Length)
            {
                throw new ArgumentException(
                    $"groupCount must be between 1 and {distinct.Length} (the number of groups).",
                    nameof(groupCount));
            }

            var random = new Random(seed);
            var order = DataPreparationGuard.Range(distinct.Length);
            DataPreparationGuard.Shuffle(order, random);

            var chosen = order.Take(groupCount).Select(i => distinct[i]).OrderBy(g => g, StringComparer.Ordinal)
                .ToArray();

            var set = new HashSet<string>(chosen, StringComparer.Ordinal);

            var indices = Enumerable.Range(0, groupKeys.Length).Where(i => set.Contains(groupKeys[i] ?? string.Empty))
                .ToArray();

            return new ClusterSampleResult(chosen, indices, DataPreparationGuard.Complement(indices, groupKeys.Length));
        }

        [AdvancedAnalyticsTool("SplitTrainValidationTest")]
        [Description(
            "Randomly splits row positions into train, validation and test index sets, reproducibly from a seed. " +
            "The three fractions may sum to less than 1 (leftover rows are left out) but not more. When labels are given and stratify is true, each class is split in the same proportions so rare classes appear in every set. " +
            "Returns the three index arrays; use them to filter features and labels identically. Fractions of 0 give an empty set.")]
        public SplitResult SplitTrainValidationTest(
            [Description("Number of rows to split, at least 1.")]
            int rowCount,
            [Description("Share of rows for training, between 0 and 1. Default 0.6.")]
            double trainFraction = 0.6,
            [Description("Share of rows for validation, between 0 and 1. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of rows for testing, between 0 and 1. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Optional class label per row to stratify on (same length as rowCount). Leave empty for a plain random split.")]
            int[]? labels = null,
            [Description("When true and labels are given, split within each class. Default true.")]
            bool stratify = true,
            [Description("Seed that makes the split reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Positive(rowCount, nameof(rowCount));
            DataPreparationGuard.Fraction(trainFraction, nameof(trainFraction));
            DataPreparationGuard.Fraction(validationFraction, nameof(validationFraction));
            DataPreparationGuard.Fraction(testFraction, nameof(testFraction));

            if (trainFraction + validationFraction + testFraction > 1 + 1e-9)
            {
                throw new ArgumentException("The three fractions must not sum to more than 1.", nameof(trainFraction));
            }

            var useLabels = labels is { Length: > 0 } && stratify;

            if (labels is { Length: > 0 })
            {
                DataPreparationGuard.SameLength(rowCount, labels.Length, nameof(labels), nameof(rowCount));
            }

            var split = DataSplit.Split(
                useLabels ? labels!.Select(l => (double)l).ToArray() : new double[rowCount],
                trainFraction, validationFraction, testFraction, seed, useLabels);

            return new SplitResult(split.Train, split.Validation, split.Test, seed);
        }

        [AdvancedAnalyticsTool("SplitByGroup")]
        [Description(
            "Splits row positions into train, validation and test sets so that all rows of a group (account, customer, device) land in the same set, which prevents leakage between sets when rows of one entity are correlated. " +
            "Groups are assigned greedily, largest first, to whichever set is furthest below its target share, so the realised fractions are approximate when groups are large. Reproducible from the seed. " +
            "Fractions may sum to less than 1 but not more. Returns the three index arrays.")]
        public SplitResult SplitByGroup(
            [Description(
                "The group name for each row, in row order; rows with the same text belong to the same group.")]
            string[] groupKeys,
            [Description("Target share of rows for training, between 0 and 1. Default 0.6.")]
            double trainFraction = 0.6,
            [Description("Target share of rows for validation, between 0 and 1. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Target share of rows for testing, between 0 and 1. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the split reproducible. Default 1.")]
            int seed = 1)
        {
            if (groupKeys == null || groupKeys.Length == 0)
            {
                throw new ArgumentException("Give one group name per row.", nameof(groupKeys));
            }

            DataPreparationGuard.Fraction(trainFraction, nameof(trainFraction));
            DataPreparationGuard.Fraction(validationFraction, nameof(validationFraction));
            DataPreparationGuard.Fraction(testFraction, nameof(testFraction));

            if (trainFraction + validationFraction + testFraction > 1 + 1e-9 ||
                trainFraction + validationFraction + testFraction <= 0)
            {
                throw new ArgumentException("The fractions must sum to more than 0 and at most 1.",
                    nameof(trainFraction));
            }

            var split = GroupSplit.Split(groupKeys.Select(g => g ?? string.Empty).ToArray(), trainFraction,
                validationFraction, testFraction, seed);

            return new SplitResult(split.Train, split.Validation, split.Test, seed);
        }

        [AdvancedAnalyticsTool("SplitTimeOrdered")]
        [Description(
            "Splits rows by time so that training precedes validation, which precedes testing, the honest way to evaluate models that will score future data. " +
            "Rows are ordered by their timestamp (ties keep row order); the earliest trainFraction go to train, the next validationFraction to validation, the next testFraction to test. " +
            "An optional purge gap leaves out that many rows at the start of the validation and test sets, to stop information bleeding across each boundary. Returns the three index arrays (each in time order) and the timestamp boundaries between sets.")]
        public TimeSplitResult SplitTimeOrdered(
            [Description(
                "A timestamp for each row (epoch seconds or any increasing number), in row order. NaN is not allowed.")]
            double[] times,
            [Description("Share of the earliest rows for training, between 0 and 1. Default 0.6.")]
            double trainFraction = 0.6,
            [Description("Share of the next rows for validation, between 0 and 1. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the latest rows for testing, between 0 and 1. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Number of rows to drop at the start of the validation and test sets. Default 0.")]
            int purgeGap = 0)
        {
            DataPreparationGuard.Vector(times, nameof(times));
            DataPreparationGuard.Fraction(trainFraction, nameof(trainFraction));
            DataPreparationGuard.Fraction(validationFraction, nameof(validationFraction));
            DataPreparationGuard.Fraction(testFraction, nameof(testFraction));

            if (times.Any(double.IsNaN))
            {
                throw new ArgumentException("Times must not contain NaN. Remove or impute the rows first.",
                    nameof(times));
            }

            if (trainFraction + validationFraction + testFraction > 1 + 1e-9)
            {
                throw new ArgumentException("The three fractions must not sum to more than 1.", nameof(trainFraction));
            }

            if (purgeGap < 0)
            {
                throw new ArgumentException("purgeGap must be 0 or more.", nameof(purgeGap));
            }

            var order = Enumerable.Range(0, times.Length).OrderBy(i => times[i]).ThenBy(i => i).ToArray();
            var n = order.Length;
            var trainCount = (int)Math.Floor(n * trainFraction);
            var validationCount = (int)Math.Floor(n * validationFraction);
            var testCount = (int)Math.Floor(n * testFraction);

            var validationStart = trainCount;
            var testStart = validationStart + validationCount;

            var train = order.Take(trainCount).ToArray();

            var validation = order.Skip(validationStart + (validationCount > 0 ? purgeGap : 0))
                .Take(Math.Max(0, validationCount - (validationCount > 0 ? purgeGap : 0))).ToArray();
            var test = order.Skip(testStart + (testCount > 0 ? purgeGap : 0))
                .Take(Math.Max(0, testCount - (testCount > 0 ? purgeGap : 0))).ToArray();

            return new TimeSplitResult(train, validation, test,
                validationCount > 0 && testStart > 0 ? times[order[validationStart]] : double.NaN,
                testCount > 0 && testStart < n ? times[order[testStart]] : double.NaN);
        }

        [AdvancedAnalyticsTool("GenerateKFolds")]
        [Description(
            "Generates k-fold cross validation splits: rows are shuffled by the seed and dealt into k folds of near equal size; fold i uses its rows as the test set and all other rows for training. " +
            "Returns one entry per fold with its train and test row indices. Every row appears in exactly one test set. Use GenerateStratifiedKFolds for imbalanced targets and GenerateGroupKFolds when rows of one entity must stay together.")]
        public FoldResult[] GenerateKFolds(
            [Description("Number of rows, at least as large as the number of folds.")]
            int rowCount,
            [Description("Number of folds, at least 2. Default 5.")]
            int folds = 5,
            [Description("Seed that makes the shuffle reproducible. Default 1.")]
            int seed = 1)
        {
            ValidateFolds(rowCount, folds);

            return ToFolds(KFoldSplit.Split(rowCount, folds, seed));
        }

        [AdvancedAnalyticsTool("GenerateStratifiedKFolds")]
        [Description(
            "Generates k-fold splits that keep each class in every fold in roughly its overall proportion, which matters for rare events such as fraud. " +
            "Rows of each class are shuffled by the seed and dealt round robin across folds. Returns one entry per fold with train and test row indices; every row is in exactly one test set. A class with fewer rows than folds simply misses some folds.")]
        public FoldResult[] GenerateStratifiedKFolds(
            [Description("The class label for each row (any integers, for example 0/1), in row order.")]
            int[] labels,
            [Description("Number of folds, at least 2. Default 5.")]
            int folds = 5,
            [Description("Seed that makes the shuffle reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Labels(labels, nameof(labels));
            ValidateFolds(labels.Length, folds);

            return ToFolds(KFoldSplit.Stratified(labels.Select(l => (double)l).ToArray(), folds, seed));
        }

        [AdvancedAnalyticsTool("GenerateGroupKFolds")]
        [Description(
            "Generates k-fold splits where every group (account, customer, device) sits wholly inside one test fold, so a model is never tested on an entity it trained on. " +
            "Groups are shuffled by the seed and assigned largest first to the currently smallest fold to keep fold sizes balanced. Returns one entry per fold with train and test row indices. There must be at least as many distinct groups as folds.")]
        public FoldResult[] GenerateGroupKFolds(
            [Description(
                "The group name for each row, in row order; rows with the same text belong to the same group.")]
            string[] groupKeys,
            [Description("Number of folds, at least 2. Default 5.")]
            int folds = 5,
            [Description("Seed that makes the shuffle reproducible. Default 1.")]
            int seed = 1)
        {
            if (groupKeys == null || groupKeys.Length == 0)
            {
                throw new ArgumentException("Give one group name per row.", nameof(groupKeys));
            }

            if (folds < 2)
            {
                throw new ArgumentException("folds must be at least 2.", nameof(folds));
            }

            var keys = groupKeys.Select(g => g ?? string.Empty).ToArray();

            if (keys.Distinct(StringComparer.Ordinal).Count() < folds)
            {
                throw new ArgumentException(
                    $"There are only {keys.Distinct(StringComparer.Ordinal).Count()} distinct groups for {folds} folds. Lower folds.",
                    nameof(folds));
            }

            return ToFolds(GroupSplit.KFold(keys, folds, seed));
        }

        [AdvancedAnalyticsTool("GenerateTimeSeriesFolds")]
        [Description(
            "Generates forward-chaining (expanding window) folds for time ordered data: each fold trains on all earlier rows and tests on the next block, so the model is never trained on the future. " +
            "Rows are ordered by timestamp; the data is cut into folds+1 blocks, the first block plus any previous test blocks form the training set. An optional gap drops that many rows just before each test block. Returns one entry per fold with train and test row indices.")]
        public FoldResult[] GenerateTimeSeriesFolds(
            [Description(
                "A timestamp for each row (epoch seconds or any increasing number), in row order. NaN is not allowed.")]
            double[] times,
            [Description("Number of test blocks, at least 1. Default 5.")]
            int folds = 5,
            [Description("Rows to leave out between the end of training and the start of the test block. Default 0.")]
            int gap = 0)
        {
            DataPreparationGuard.Vector(times, nameof(times));

            if (folds < 1)
            {
                throw new ArgumentException("folds must be at least 1.", nameof(folds));
            }

            if (gap < 0)
            {
                throw new ArgumentException("gap must be 0 or more.", nameof(gap));
            }

            if (times.Any(double.IsNaN))
            {
                throw new ArgumentException("Times must not contain NaN.", nameof(times));
            }

            if (times.Length / (folds + 1) < 1)
            {
                throw new ArgumentException($"{times.Length} rows are too few for {folds} folds. Lower folds.",
                    nameof(folds));
            }

            try
            {
                return ToFolds(TimeSplit.ForwardChaining(times, folds, gap));
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(exception.Message + " Lower the gap or the number of folds.", nameof(gap));
            }
        }

        [AdvancedAnalyticsTool("GenerateLeaveOneOutFolds")]
        [Description(
            "Generates leave-one-out splits: fold i tests on row i alone and trains on every other row. Use it only for small tables, since n folds of n-1 rows each are returned (the size is limited to 1000 rows for that reason). " +
            "It has low bias but high variance and is costly to evaluate; k-fold is usually preferable.")]
        public FoldResult[] GenerateLeaveOneOutFolds(
            [Description("Number of rows, between 2 and 1000.")]
            int rowCount)
        {
            if (rowCount < 2 || rowCount > 1000)
            {
                throw new ArgumentException("rowCount must be between 2 and 1000 for leave-one-out.", nameof(rowCount));
            }

            return Enumerable.Range(0, rowCount).Select(i => new FoldResult(i,
                Enumerable.Range(0, rowCount).Where(r => r != i).ToArray(), new[] { i })).ToArray();
        }

        [AdvancedAnalyticsTool("GenerateRepeatedKFolds")]
        [Description(
            "Generates k-fold splits repeated several times with a different shuffle each repeat, giving repeats * folds train/test pairs for a steadier performance estimate. " +
            "With labels given and stratify true each repeat is stratified. Returns one entry per fold across all repeats, tagged with its repeat number; repeat r uses seed + r.")]
        public RepeatedFoldResult[] GenerateRepeatedKFolds(
            [Description("Number of rows, at least as large as the number of folds.")]
            int rowCount,
            [Description("Number of folds per repeat, at least 2. Default 5.")]
            int folds = 5,
            [Description("Number of repeats, at least 1. Default 3.")]
            int repeats = 3,
            [Description("Base seed; repeat r uses seed + r. Default 1.")]
            int seed = 1,
            [Description("Optional class label per row to stratify each repeat. Leave empty for plain random folds.")]
            int[]? labels = null)
        {
            ValidateFolds(rowCount, folds);
            DataPreparationGuard.Positive(repeats, nameof(repeats));

            if (labels is { Length: > 0 })
            {
                DataPreparationGuard.SameLength(rowCount, labels.Length, nameof(labels), nameof(rowCount));
            }

            var results = new List<RepeatedFoldResult>();

            for (var r = 0; r < repeats; r++)
            {
                var split = labels is { Length: > 0 }
                    ? KFoldSplit.Stratified(labels.Select(l => (double)l).ToArray(), folds, seed + r)
                    : KFoldSplit.Split(rowCount, folds, seed + r);

                results.AddRange(split.Select((f, i) => new RepeatedFoldResult(r, i, f.Train, f.Test)));
            }

            return results.ToArray();
        }

        [AdvancedAnalyticsTool("GenerateBootstrapSamples")]
        [Description(
            "Generates bootstrap resamples: each is a set of row indices drawn with replacement from the table, so some rows repeat and about 36.8 percent are left out. " +
            "Returns for each resample the in-bag indices (the training set, with repeats) and the out-of-bag indices (never drawn, usable as a test set). Reproducible from the seed; resample r uses a generator seeded from seed and r.")]
        public BootstrapSampleResult[] GenerateBootstrapSamples(
            [Description("Number of rows in the original table, at least 1.")]
            int rowCount,
            [Description("Number of resamples to generate, at least 1. Default 10.")]
            int resamples = 10,
            [Description("Rows in each resample; 0 means the same as rowCount. Default 0.")]
            int sampleSize = 0,
            [Description("Seed that makes the draws reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Positive(rowCount, nameof(rowCount));
            DataPreparationGuard.Positive(resamples, nameof(resamples));

            if (sampleSize < 0)
            {
                throw new ArgumentException("sampleSize must be 0 (same as rowCount) or more.", nameof(sampleSize));
            }

            var size = sampleSize == 0 ? rowCount : sampleSize;

            return Enumerable.Range(0, resamples).Select(r =>
            {
                var random = new Random(unchecked(seed * 486187739 + r));
                var inBag = Enumerable.Range(0, size).Select(_ => random.Next(rowCount)).ToArray();

                return new BootstrapSampleResult(r, inBag,
                    DataPreparationGuard.Complement(inBag.Distinct().ToArray(), rowCount));
            }).ToArray();
        }

        [AdvancedAnalyticsTool("GeneratePermutation")]
        [Description(
            "Generates a random permutation of the row positions 0 to rowCount-1, reproducibly from a seed. Use it to shuffle rows or, applied to labels, to build a null distribution for a permutation test. " +
            "The returned array lists the original index that goes into each new position.")]
        public int[] GeneratePermutation(
            [Description("Number of rows, at least 1.")]
            int rowCount,
            [Description("Seed that makes the shuffle reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Positive(rowCount, nameof(rowCount));
            var indices = DataPreparationGuard.Range(rowCount);
            DataPreparationGuard.Shuffle(indices, new Random(seed));

            return indices;
        }

        private static void ValidateFolds(int rowCount, int folds)
        {
            if (folds < 2)
            {
                throw new ArgumentException("folds must be at least 2.", nameof(folds));
            }

            if (rowCount < folds)
            {
                throw new ArgumentException($"There are {rowCount} rows for {folds} folds. Lower folds or add rows.",
                    nameof(folds));
            }
        }

        private static FoldResult[] ToFolds(SplitFold[] folds)
        {
            return folds.Select((f, i) => new FoldResult(i, f.Train, f.Test)).ToArray();
        }

        private static int[] ProportionalAllocation(int[] sizes, int sample)
        {
            var total = sizes.Sum();
            var exact = sizes.Select(s => sample * (double)s / total).ToArray();
            var allocation = exact.Select(e => (int)Math.Floor(e)).ToArray();
            var remaining = sample - allocation.Sum();

            foreach (var g in Enumerable.Range(0, sizes.Length)
                         .OrderByDescending(g => exact[g] - allocation[g]).ThenBy(g => g).Take(remaining))
            {
                allocation[g]++;
            }

            return allocation.Select((a, g) => Math.Min(a, sizes[g])).ToArray();
        }

        private static int[] EqualAllocation(int[] sizes, int sample)
        {
            var allocation = new int[sizes.Length];
            var remaining = sample;

            while (remaining > 0)
            {
                var open = Enumerable.Range(0, sizes.Length).Where(g => allocation[g] < sizes[g]).ToArray();

                if (open.Length == 0)
                {
                    break;
                }

                var share = Math.Max(1, remaining / open.Length);

                foreach (var g in open)
                {
                    var add = Math.Min(Math.Min(share, sizes[g] - allocation[g]), remaining);
                    allocation[g] += add;
                    remaining -= add;

                    if (remaining == 0)
                    {
                        break;
                    }
                }
            }

            return allocation;
        }
    }

    public sealed record IndexSampleResult(int[] Indices, int[] NotSelected, int Seed);

    public sealed record StratumAllocation(string Stratum, int Population, int Sampled);

    public sealed record StratifiedSampleResult(
        int[] Indices,
        int[] NotSelected,
        StratumAllocation[] Allocation,
        int Seed);

    public sealed record SystematicSampleResult(int[] Indices, double Interval, double Start);

    public sealed record ClusterSampleResult(string[] ChosenGroups, int[] Indices, int[] NotSelected);

    public sealed record SplitResult(int[] Train, int[] Validation, int[] Test, int Seed);

    public sealed record TimeSplitResult(
        int[] Train,
        int[] Validation,
        int[] Test,
        double ValidationStartTime,
        double TestStartTime);

    public sealed record FoldResult(int Fold, int[] Train, int[] Test);

    public sealed record RepeatedFoldResult(int Repeat, int Fold, int[] Train, int[] Test);

    public sealed record BootstrapSampleResult(int Resample, int[] InBag, int[] OutOfBag);
}