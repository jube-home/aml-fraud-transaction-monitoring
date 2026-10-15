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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    public enum ClassWeightMethod
    {
        Balanced = 0,
        InverseFrequency = 1,
        SquareRootInverse = 2,
        EffectiveNumber = 3
    }

    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ClassImbalanceTools
    {
        [AdvancedAnalyticsTool("ComputeClassWeights")]
        [Description(
            "Computes a weight for each class so a model can pay more attention to rare classes without changing the data, and a weight for every row to pass straight to a weighted fit. " +
            "Balanced is n / (classes * count), so each class carries equal total weight and the average row weight is 1. InverseFrequency is 1/count, SquareRootInverse is 1/sqrt(count) (a softer correction), and EffectiveNumber is (1 - beta) / (1 - beta^count), which lets a few extra rows of an already large class count for little; these three are rescaled so the average row weight is 1. " +
            "Returns the classes, their counts, the class weights and the per-row weights.")]
        public ClassWeightResult ComputeClassWeights(
            [Description("The class label for each row (any integers, for example 0/1), in row order.")]
            int[] labels,
            [Description("Balanced, InverseFrequency, SquareRootInverse or EffectiveNumber. Default Balanced.")]
            ClassWeightMethod method = ClassWeightMethod.Balanced,
            [Description(
                "For EffectiveNumber only: beta between 0 and 1 exclusive, close to 1 such as 0.999. Default 0.999.")]
            double beta = 0.999)
        {
            DataPreparationGuard.Labels(labels, nameof(labels));

            if (double.IsNaN(beta) || beta <= 0 || beta >= 1)
            {
                throw new ArgumentException("beta must be strictly between 0 and 1, for example 0.999.", nameof(beta));
            }

            var classes = labels.Distinct().OrderBy(c => c).ToArray();
            var counts = classes.Select(c => labels.Count(l => l == c)).ToArray();

            var raw = counts.Select(n => method switch
            {
                ClassWeightMethod.Balanced => labels.Length / (double)(classes.Length * n),
                ClassWeightMethod.InverseFrequency => 1d / n,
                ClassWeightMethod.SquareRootInverse => 1 / Math.Sqrt(n),
                _ => (1 - beta) / (1 - Math.Pow(beta, n))
            }).ToArray();

            var mean = raw.Select((w, c) => w * counts[c]).Sum() / labels.Length;
            var weights = raw.Select(w => w / mean).ToArray();
            var lookup = classes.Select((c, k) => (c, k)).ToDictionary(x => x.c, x => weights[x.k]);

            return new ClassWeightResult(classes, counts, weights, labels.Select(l => lookup[l]).ToArray());
        }

        [AdvancedAnalyticsTool("RandomUnderSample")]
        [Description(
            "Balances classes by randomly discarding rows from the larger classes. The smallest class is kept whole; every class larger than smallestCount / targetRatio is cut down to that size (targetRatio 1 makes all classes equal). " +
            "It is fast and simple but throws information away, so use it when there is plenty of data. Returns the sorted row indices to keep and the class counts before and after; apply the same indices to features and labels. Reproducible from the seed.")]
        public ResamplingIndexResult RandomUnderSample(
            [Description("The class label for each row (any integers, for example 0/1), in row order.")]
            int[] labels,
            [Description(
                "Desired ratio of the smallest class to each larger class after sampling, above 0 and at most 1. Default 1.")]
            double targetRatio = 1,
            [Description("Seed that makes the draw reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Labels(labels, nameof(labels));
            Ratio(targetRatio);

            var groups = Groups(labels);
            var limit = (int)Math.Ceiling(groups.Min(g => g.Value.Length) / targetRatio);
            var random = new Random(seed);
            var kept = new List<int>();

            foreach (var group in groups.OrderBy(g => g.Key))
            {
                var rows = (int[])group.Value.Clone();
                DataPreparationGuard.Shuffle(rows, random);
                kept.AddRange(rows.Take(limit));
            }

            var indices = kept.OrderBy(i => i).ToArray();

            return new ResamplingIndexResult(indices, Counts(labels), Counts(indices.Select(i => labels[i]).ToArray()));
        }

        [AdvancedAnalyticsTool("RandomOverSample")]
        [Description(
            "Balances classes by randomly repeating rows of the smaller classes until each has at least largestCount * targetRatio rows (targetRatio 1 makes all classes as large as the biggest). " +
            "No information is lost but duplicated rows make models overconfident and cross validation optimistic, so oversample only inside training folds, never before splitting. " +
            "Returns the row indices to use (each original row once, then the extra draws with replacement, sorted) and the class counts before and after. Reproducible from the seed.")]
        public ResamplingIndexResult RandomOverSample(
            [Description("The class label for each row (any integers, for example 0/1), in row order.")]
            int[] labels,
            [Description(
                "Desired ratio of each smaller class to the largest class after sampling, above 0 and at most 1. Default 1.")]
            double targetRatio = 1,
            [Description("Seed that makes the draw reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Labels(labels, nameof(labels));
            Ratio(targetRatio);

            var groups = Groups(labels);
            var target = (int)Math.Floor(groups.Max(g => g.Value.Length) * targetRatio);
            var random = new Random(seed);
            var result = new List<int>();

            foreach (var group in groups.OrderBy(g => g.Key))
            {
                result.AddRange(group.Value);

                for (var extra = group.Value.Length; extra < target; extra++)
                {
                    result.Add(group.Value[random.Next(group.Value.Length)]);
                }
            }

            var indices = result.OrderBy(i => i).ToArray();

            return new ResamplingIndexResult(indices, Counts(labels), Counts(indices.Select(i => labels[i]).ToArray()));
        }

        [AdvancedAnalyticsTool("GenerateSmoteSamples")]
        [Description(
            "Creates synthetic minority-class rows by SMOTE: pick a random minority row, pick one of its k nearest minority neighbours (Euclidean distance) and place a new row at a random point on the line between them. " +
            "This enlarges the minority class without exact duplicates. Use it only on training data, never before splitting. Features are used raw for interpolation; standardiseForDistance makes the neighbour search fair across columns of different size. " +
            "Every value must be finite: impute first. Returns the synthetic rows, for each the source row and neighbour row it came from, and (when includeOriginalRows is true) the combined table and labels ready for fitting. By default enough rows are made to match the largest class. Reproducible from the seed.")]
        public SmoteResult GenerateSmoteSamples(
            [Description("Feature rows: one array of finite numbers per case, all the same length.")]
            double[][] data,
            [Description("The class label for each row (any integers), in row order.")]
            int[] labels,
            [Description("The label of the class to synthesise rows for. It needs at least 2 rows.")]
            int minorityLabel,
            [Description("Number of nearest minority neighbours to choose from, at least 1. Default 5.")]
            int neighbours = 5,
            [Description("Number of synthetic rows to create; 0 means enough to match the largest class. Default 0.")]
            int syntheticCount = 0,
            [Description(
                "When true, distances are measured on columns scaled to unit standard deviation. Default true.")]
            bool standardiseForDistance = true,
            [Description(
                "When true, also return the original rows followed by the synthetic ones as a ready-to-use table. Default true.")]
            bool includeOriginalRows = true,
            [Description("Seed that makes the draw reproducible. Default 1.")]
            int seed = 1)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            DataPreparationGuard.Labels(labels, nameof(labels));
            DataPreparationGuard.SameLength(data.Length, labels.Length, nameof(labels), "the table");
            DataPreparationGuard.Positive(neighbours, nameof(neighbours));

            if (syntheticCount < 0)
            {
                throw new ArgumentException("syntheticCount must be 0 (match the largest class) or more.",
                    nameof(syntheticCount));
            }

            for (var i = 0; i < data.Length; i++)
            {
                if (data[i].Any(DataPreparationGuard.IsMissing))
                {
                    throw new ArgumentException(
                        $"Row {i} has a missing value. SMOTE needs finite values: run ImputeMissingValues first.",
                        nameof(data));
                }
            }

            var minority = Enumerable.Range(0, data.Length).Where(i => labels[i] == minorityLabel).ToArray();

            if (minority.Length < 2)
            {
                throw new ArgumentException(
                    $"Label {minorityLabel} has {minority.Length} row(s); SMOTE needs at least 2 to interpolate between.",
                    nameof(minorityLabel));
            }

            var count = syntheticCount > 0
                ? syntheticCount
                : Math.Max(0, labels.GroupBy(l => l).Max(g => g.Count()) - minority.Length);

            var scales = Enumerable.Range(0, columns).Select(j =>
            {
                var column = DataPreparationGuard.Column(data, j);
                var mean = column.Average();

                var sd = column.Length < 2
                    ? 0
                    : Math.Sqrt(column.Sum(v => (v - mean) * (v - mean)) / (column.Length - 1));

                return standardiseForDistance && sd > 0 ? sd : 1d;
            }).ToArray();

            var k = Math.Min(neighbours, minority.Length - 1);

            var neighbourLists = minority.Select(row => minority.Where(o => o != row)
                .OrderBy(o => Distance(data[row], data[o], scales)).ThenBy(o => o).Take(k).ToArray()).ToArray();

            var random = new Random(seed);
            var synthetic = new double[count][];
            var sources = new int[count];
            var partners = new int[count];

            for (var s = 0; s < count; s++)
            {
                var pick = random.Next(minority.Length);
                var partner = neighbourLists[pick][random.Next(neighbourLists[pick].Length)];
                var gap = random.NextDouble();
                sources[s] = minority[pick];
                partners[s] = partner;
                synthetic[s] = data[minority[pick]].Select((v, j) => v + gap * (data[partner][j] - v)).ToArray();
            }

            var table = includeOriginalRows ? data.Concat(synthetic).ToArray() : Array.Empty<double[]>();

            var outLabels = includeOriginalRows
                ? labels.Concat(Enumerable.Repeat(minorityLabel, count)).ToArray()
                : Array.Empty<int>();

            return new SmoteResult(synthetic, sources, partners, table, outLabels);
        }

        private static double Distance(double[] a, double[] b, double[] scales)
        {
            double sum = 0;

            for (var j = 0; j < a.Length; j++)
            {
                var d = (a[j] - b[j]) / scales[j];
                sum += d * d;
            }

            return sum;
        }

        private static void Ratio(double targetRatio)
        {
            if (double.IsNaN(targetRatio) || targetRatio <= 0 || targetRatio > 1)
            {
                throw new ArgumentException("targetRatio must be above 0 and at most 1.", nameof(targetRatio));
            }
        }

        private static Dictionary<int, int[]> Groups(int[] labels)
        {
            return Enumerable.Range(0, labels.Length).GroupBy(i => labels[i])
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        private static ClassCount[] Counts(int[] labels)
        {
            return labels.GroupBy(l => l).OrderBy(g => g.Key).Select(g => new ClassCount(g.Key, g.Count())).ToArray();
        }
    }

    public sealed record ClassWeightResult(int[] Classes, int[] Counts, double[] ClassWeights, double[] RowWeights);

    public sealed record ClassCount(int Label, int Count);

    public sealed record ResamplingIndexResult(int[] Indices, ClassCount[] CountsBefore, ClassCount[] CountsAfter);

    public sealed record SmoteResult(
        double[][] SyntheticRows,
        int[] SourceRows,
        int[] NeighbourRows,
        double[][] Table,
        int[] Labels);
}