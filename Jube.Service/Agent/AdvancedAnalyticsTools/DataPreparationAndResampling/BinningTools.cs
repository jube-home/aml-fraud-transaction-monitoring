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
using Accord.MachineLearning;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class BinningTools
    {
        [AdvancedAnalyticsTool("DiscretiseEqualWidth")]
        [Description(
            "Splits the range of a numeric column into bins of equal width and returns the interior edges, each row's bin index and a bin table (lower and upper bound, count, share of rows). " +
            "A value belongs to the bin whose lower bound it exceeds and whose upper bound it does not exceed, so the minimum sits in the first bin and the maximum in the last. Bin index -1 marks a missing value. " +
            "Give 0/1 labels to also get each bin's events, event rate, weight of evidence and information value. Equal widths suit uniform data; heavy tails leave many bins nearly empty, so consider DiscretiseEqualFrequency. A constant column yields one bin.")]
        public BinningResult DiscretiseEqualWidth(
            [Description("The numeric values to bin, one per row. NaN or infinity marks a missing value.")]
            double[] values,
            [Description("The number of bins wanted, at least 1. Default 10.")]
            int bins = 10,
            [Description(
                "Optional 0/1 target per row, in row order, to add event statistics to each bin. Leave empty for none.")]
            int[]? labels = null)
        {
            var finite = Prepare(values, labels, bins);
            var min = finite.Min();
            var max = finite.Max();

            var edges = Math.Abs(max - min) < 0.0001
                ? Array.Empty<double>()
                : Enumerable.Range(1, bins - 1).Select(i => min + (max - min) * i / bins).ToArray();

            return Build(values, labels, edges);
        }

        [AdvancedAnalyticsTool("DiscretiseEqualFrequency")]
        [Description(
            "Splits a numeric column into bins holding roughly the same number of rows, by cutting at evenly spaced quantiles, and returns the interior edges, each row's bin index and a bin table. " +
            "Repeated cut points caused by ties are merged, so fewer bins than requested can come back; the table shows the actual counts. Bin index -1 marks a missing value. " +
            "Give 0/1 labels to also get each bin's events, event rate, weight of evidence and information value. Best default for skewed money and count variables.")]
        public BinningResult DiscretiseEqualFrequency(
            [Description("The numeric values to bin, one per row. NaN or infinity marks a missing value.")]
            double[] values,
            [Description("The number of bins wanted, at least 1. Default 10.")]
            int bins = 10,
            [Description(
                "Optional 0/1 target per row, in row order, to add event statistics to each bin. Leave empty for none.")]
            int[]? labels = null)
        {
            var finite = Prepare(values, labels, bins);
            var sorted = finite.OrderBy(v => v).ToArray();

            var edges = Enumerable.Range(1, bins - 1)
                .Select(i => DataPreparationGuard.Quantile(sorted, i / (double)bins))
                .Where(e => e > sorted[0] && e < sorted[^1])
                .Distinct()
                .OrderBy(e => e)
                .ToArray();

            return Build(values, labels, edges);
        }

        [AdvancedAnalyticsTool("DiscretiseKMeans")]
        [Description(
            "Bins a numeric column by clustering its values into k groups with one dimensional k-means and cutting halfway between neighbouring cluster centres. " +
            "Use it when the data has natural clumps of unequal size. Initial centres are set from evenly spaced quantiles, so the result is deterministic. " +
            "Returns the interior edges, each row's bin index (-1 for missing) and a bin table, with event statistics when 0/1 labels are given. Fewer bins than requested can result when values have few distinct levels.")]
        public BinningResult DiscretiseKMeans(
            [Description("The numeric values to bin, one per row. NaN or infinity marks a missing value.")]
            double[] values,
            [Description("The number of clusters and so bins wanted, at least 1. Default 5.")]
            int bins = 5,
            [Description(
                "Optional 0/1 target per row, in row order, to add event statistics to each bin. Leave empty for none.")]
            int[]? labels = null)
        {
            var finite = Prepare(values, labels, bins);
            var distinct = finite.Distinct().Count();
            var k = Math.Min(bins, distinct);

            if (k <= 1)
            {
                return Build(values, labels, Array.Empty<double>());
            }

            var sorted = finite.OrderBy(v => v).ToArray();

            var model = new KMeans(k)
            {
                MaxIterations = 200,
                Tolerance = 1e-10
            };

            var seeds = Enumerable.Range(0, k).Select(i => DataPreparationGuard.Quantile(sorted, (i + 0.5) / k))
                .ToArray();

            for (var i = 1; i < k; i++)
            {
                if (seeds[i] <= seeds[i - 1])
                {
                    seeds[i] = seeds[i - 1] + 1e-9 * Math.Max(1, Math.Abs(seeds[i - 1]));
                }
            }

            var points = finite.Select(v => new[] { v }).ToArray();
            model.UseSeeding = Seeding.Fixed;
            model.Randomize(points);

            for (var c = 0; c < k; c++)
            {
                model.Clusters.Centroids[c] = new[] { seeds[c] };
            }

            model.Learn(points);
            var centres = model.Clusters.Centroids.Select(c => c[0]).OrderBy(c => c).ToArray();
            var edges = centres.Zip(centres.Skip(1), (a, b) => (a + b) / 2).Distinct().ToArray();

            return Build(values, labels, edges);
        }

        [AdvancedAnalyticsTool("DiscretiseSupervised")]
        [Description(
            "Finds the cut points that best separate the classes of a target, using a one variable decision tree on entropy, and returns them as bin edges with each row's bin index and a bin table. " +
            "This is optimal binning for a target: bins are placed where the event rate changes most, which suits building scorecards. The tree grows at most maximumBins-1 cuts, keeping the cuts nearest the root (the most informative) when it finds more. " +
            "Every bin must contain at least minimumBinFraction of the labelled rows. Labels must be 0/1 and both classes must be present; rows with a missing value are ignored when learning. The table always includes events, event rate, weight of evidence and information value.")]
        public BinningResult DiscretiseSupervised(
            [Description("The numeric values to bin, one per row. NaN or infinity marks a missing value.")]
            double[] values,
            [Description("The 0/1 target for each row (1 is the event), in row order. Both classes must appear.")]
            int[] labels,
            [Description("The maximum number of bins wanted, at least 2. Default 5.")]
            int maximumBins = 5,
            [Description("Smallest share of the usable rows allowed in a bin, between 0 and 0.5. Default 0.05.")]
            double minimumBinFraction = 0.05)
        {
            DataPreparationGuard.Vector(values, nameof(values));
            DataPreparationGuard.Binary(labels, nameof(labels));
            DataPreparationGuard.SameLength(values.Length, labels.Length, nameof(labels), nameof(values));

            if (maximumBins < 2)
            {
                throw new ArgumentException("maximumBins must be at least 2 for supervised binning.",
                    nameof(maximumBins));
            }

            if (double.IsNaN(minimumBinFraction) || minimumBinFraction < 0 || minimumBinFraction > 0.5)
            {
                throw new ArgumentException("minimumBinFraction must be between 0 and 0.5.",
                    nameof(minimumBinFraction));
            }

            var usable = Enumerable.Range(0, values.Length).Where(i => !DataPreparationGuard.IsMissing(values[i]))
                .ToArray();

            if (usable.Select(i => labels[i]).Distinct().Count() < 2)
            {
                throw new ArgumentException(
                    "Both classes must be present among the rows with a value to learn supervised bins.",
                    nameof(labels));
            }

            var height = (int)Math.Ceiling(Math.Log2(maximumBins));

            var learner = new C45Learning(new[] { DecisionVariable.Continuous("value") })
            {
                MaxHeight = height,
                Join = height,
                MinimumLeafSize = Math.Max(1, (int)Math.Ceiling(minimumBinFraction * usable.Length))
            };

            var tree = learner.Learn(usable.Select(i => new[] { values[i] }).ToArray(),
                usable.Select(i => labels[i]).ToArray());

            var thresholds = new List<double>();
            var queue = new Queue<DecisionNode>();
            queue.Enqueue(tree.Root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                if (node.Branches == null || node.Branches.Count == 0)
                {
                    continue;
                }

                foreach (var branch in node.Branches)
                {
                    if (branch.Value.HasValue && !thresholds.Contains(branch.Value.Value))
                    {
                        thresholds.Add(branch.Value.Value);
                    }

                    queue.Enqueue(branch);
                }
            }

            var edges = thresholds.Take(maximumBins - 1).OrderBy(t => t).ToArray();

            return Build(values, labels, edges);
        }

        [AdvancedAnalyticsTool("DiscretiseCustomEdges")]
        [Description(
            "Bins a numeric column at cut points you choose (for example regulatory bands such as 1000, 5000, 10000) and returns each row's bin index and a bin table with counts and shares. " +
            "Edges are interior cut points in ascending order; with n edges there are n+1 bins and a value equal to an edge falls in the lower bin. Bin index -1 marks a missing value. " +
            "Give 0/1 labels to also get each bin's events, event rate, weight of evidence and information value.")]
        public BinningResult DiscretiseCustomEdges(
            [Description("The numeric values to bin, one per row. NaN or infinity marks a missing value.")]
            double[] values,
            [Description("Interior cut points, strictly ascending and finite. An empty array gives a single bin.")]
            double[] edges,
            [Description(
                "Optional 0/1 target per row, in row order, to add event statistics to each bin. Leave empty for none.")]
            int[]? labels = null)
        {
            DataPreparationGuard.Vector(values, nameof(values));
            ValidateEdges(edges, nameof(edges));
            ValidateLabels(values, labels);

            return Build(values, labels, edges);
        }

        [AdvancedAnalyticsTool("ApplyBinEdges")]
        [Description(
            "Assigns each value to a bin using fixed edges from any Discretise tool, so new data is binned identically to training data. " +
            "The bin index is the number of edges strictly below the value, so a value equal to an edge falls in the lower bin; values beyond the outer edges land in the first or last bin. Index -1 marks a missing value.")]
        public int[] ApplyBinEdges(
            [Description("The numeric values to bin, one per row. NaN or infinity marks a missing value.")]
            double[] values,
            [Description("Interior cut points, strictly ascending, as returned by a Discretise tool.")]
            double[] edges)
        {
            DataPreparationGuard.Vector(values, nameof(values), 0);
            ValidateEdges(edges, nameof(edges));

            return values.Select(v => Assign(v, edges)).ToArray();
        }

        private static double[] Prepare(double[] values, int[]? labels, int bins)
        {
            DataPreparationGuard.Vector(values, nameof(values));
            DataPreparationGuard.Positive(bins, nameof(bins));
            ValidateLabels(values, labels);

            var usable = Enumerable.Range(0, values.Length).Where(i => !DataPreparationGuard.IsMissing(values[i]))
                .ToArray();

            if (usable.Length == 0)
            {
                throw new ArgumentException("Every value is missing, so there is nothing to bin.", nameof(values));
            }

            return usable.Select(i => values[i]).ToArray();
        }

        private static void ValidateLabels(double[] values, int[]? labels)
        {
            if (labels is { Length: > 0 })
            {
                DataPreparationGuard.Binary(labels, nameof(labels));
                DataPreparationGuard.SameLength(values.Length, labels.Length, nameof(labels), nameof(values));
            }
        }

        private static void ValidateEdges(double[]? edges, string parameterName)
        {
            if (edges == null)
            {
                throw new ArgumentException("The edges are missing. Give an ascending array of cut points.",
                    parameterName);
            }

            for (var i = 0; i < edges.Length; i++)
            {
                if (!double.IsFinite(edges[i]) || (i > 0 && edges[i] <= edges[i - 1]))
                {
                    throw new ArgumentException("Edges must be finite and strictly ascending.", parameterName);
                }
            }
        }

        private static int Assign(double value, double[] edges)
        {
            if (DataPreparationGuard.IsMissing(value))
            {
                return -1;
            }

            var index = 0;

            while (index < edges.Length && edges[index] < value)
            {
                index++;
            }

            return index;
        }

        private static BinningResult Build(double[] values, int[]? labels, double[] edges)
        {
            var assignments = values.Select(v => Assign(v, edges)).ToArray();
            var binCount = edges.Length + 1;
            var labelled = labels is { Length: > 0 };
            var usable = assignments.Count(a => a >= 0);

            var totalEvents = labelled
                ? Enumerable.Range(0, values.Length).Where(i => assignments[i] >= 0).Sum(i => labels![i])
                : 0;

            var totalNon = usable - totalEvents;
            var rows = new List<BinRow>();
            var informationValue = 0d;

            for (var b = 0; b < binCount; b++)
            {
                var members = Enumerable.Range(0, values.Length).Where(i => assignments[i] == b).ToArray();
                var events = labelled ? members.Sum(i => labels![i]) : 0;
                var rate = double.NaN;
                var woe = double.NaN;

                if (labelled && members.Length > 0)
                {
                    rate = events / (double)members.Length;
                }

                if (labelled && totalEvents > 0 && totalNon > 0)
                {
                    var eventShare = (events + 0.5) / (totalEvents + 0.5 * binCount);
                    var nonShare = (members.Length - events + 0.5) / (totalNon + 0.5 * binCount);
                    woe = Math.Log(eventShare / nonShare);
                    informationValue += (eventShare - nonShare) * woe;
                }

                rows.Add(new BinRow(b, b == 0 ? null : edges[b - 1],
                    b == binCount - 1 ? null : edges[b], members.Length,
                    usable == 0 ? 0 : members.Length / (double)usable, labelled ? events : 0, rate, woe));
            }

            return new BinningResult(edges, assignments, rows.ToArray(), labelled ? informationValue : double.NaN,
                values.Length - usable);
        }
    }

    public sealed record BinRow(
        // ReSharper disable once NotAccessedPositionalProperty.Global
        int Bin,
        double? Lower,
        double? Upper,
        int Count,
        double Fraction,
        int Events,
        double EventRate,
        double WeightOfEvidence);

    public sealed record BinningResult(
        double[] Edges,
        int[] Assignments,
        BinRow[] Table,
        double InformationValue,
        int MissingCount);
}