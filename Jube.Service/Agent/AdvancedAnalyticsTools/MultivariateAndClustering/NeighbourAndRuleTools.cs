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
using Accord.Collections;
using Accord.MachineLearning;
using Accord.MachineLearning.Rules;
using Accord.Math.Distances;
using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    public enum NeighbourMetric
    {
        Euclidean,
        Manhattan,
        Chebyshev,
        Minkowski
    }

    public enum NeighbourSearchIndex
    {
        KdTree,
        VpTree,
        BruteForce
    }

    [AdvancedAnalyticsToolGroup("MultivariateAndClustering")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class NeighbourAndRuleTools
    {
        [AdvancedAnalyticsTool("FindKNearestNeighbours")]
        [Description(
            "For each query row, finds the k closest rows of a reference set and their distances, nearest first, using a spatial index (KDTree or VPTree, both exact) or a plain scan (BruteForce, identical results, useful as a cross check). " +
            "Use it to find look-alike customers, similar past cases or duplicates. If queryData is left empty every reference row is used as a query and is not returned as its own neighbour. Distances are in the metric chosen (Euclidean, Manhattan, Chebyshev or Minkowski with minkowskiOrder); scale columns first if they are on different units. " +
            "Rows at exactly the same distance may be returned in any order, and which of them is kept at the cut off is not specified. Bounded: at most 100000 reference rows, 5000 queries and 1000 neighbours.")]
        public NeighbourQueryResult[] FindKNearestNeighbours(
            [Description("Reference rows of finite numbers, all the same length. At most 100000.")]
            double[][] referenceData,
            [Description(
                "Query rows with the same number of columns, at most 5000. Leave empty to use the reference rows themselves.")]
            double[][]? queryData = null,
            [Description(
                "How many neighbours to return per query, at least 1 and at most 1000 (and fewer than the reference rows when queries are the reference rows). Default 5.")]
            int neighbours = 5,
            [Description("Distance metric. Default Euclidean.")]
            NeighbourMetric metric = NeighbourMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1. Default 3.")]
            double minkowskiOrder = 3,
            [Description(
                "Search structure: KDTree (default, best for few columns), VPTree (any dimension) or BruteForce.")]
            NeighbourSearchIndex index = NeighbourSearchIndex.KdTree,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(referenceData, nameof(referenceData), 1, MultivariateLimits.MaximumClusteringRows);
            var selfQuery = queryData == null;
            var queries = queryData ?? referenceData;

            if (!selfQuery)
            {
                MultivariateGuard.Matrix(queryData, nameof(queryData), 1, MultivariateLimits.MaximumQuadraticRows);
                ProjectionTools.RequireSameColumns(referenceData, queryData!, nameof(queryData));
            }
            else if (queries.Length > MultivariateLimits.MaximumQuadraticRows)
            {
                throw new ArgumentException(
                    $"With no queryData every reference row is a query, which is limited to {MultivariateLimits.MaximumQuadraticRows} rows. Provide a smaller queryData.",
                    nameof(queryData));
            }

            var available = selfQuery ? referenceData.Length - 1 : referenceData.Length;

            if (available < 1)
            {
                throw new ArgumentException("There are no other reference rows to find neighbours among.",
                    nameof(referenceData));
            }

            MultivariateGuard.Bounded(neighbours, nameof(neighbours), 1,
                Math.Min(MultivariateLimits.MaximumNeighbours, available));

            var function = NeighbourDistance(metric, minkowskiOrder);
            var extra = selfQuery ? 1 : 0;
            var searcher = BuildSearcher(referenceData, metric, minkowskiOrder, index, function);
            var results = new NeighbourQueryResult[queries.Length];

            for (var q = 0; q < queries.Length; q++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var found = searcher(queries[q], neighbours + extra)
                    .Select(row => new NeighbourMatch(row, function(queries[q], referenceData[row])))
                    .OrderBy(m => m.Distance)
                    .ThenBy(m => m.Row)
                    .ToList();

                if (selfQuery)
                {
                    var self = found.FindIndex(m => m.Row == q);

                    if (self >= 0)
                    {
                        found.RemoveAt(self);
                    }
                    else if (found.Count > neighbours)
                    {
                        found.RemoveAt(found.Count - 1);
                    }
                }

                results[q] = new NeighbourQueryResult(q, found.Take(neighbours).ToArray());
            }

            return results;
        }

        [AdvancedAnalyticsTool("FindNeighboursWithinRadius")]
        [Description(
            "For each query row, finds every reference row within a given distance (a radius search) using a KD tree, returning the reference row numbers and distances, nearest first. " +
            "Use it to count or list cases inside a similarity threshold, for example accounts within a small distance of a suspicious profile. If queryData is left empty every reference row is a query and is not returned as its own neighbour. maxPerQuery caps the list per query (the nearest are kept). " +
            "Bounded: at most 100000 reference rows, 5000 queries and 1000 neighbours per query.")]
        public NeighbourQueryResult[] FindNeighboursWithinRadius(
            [Description("Reference rows of finite numbers, all the same length. At most 100000.")]
            double[][] referenceData,
            [Description("The search radius in the units of the chosen metric, above zero.")]
            double radius,
            [Description(
                "Query rows with the same number of columns, at most 5000. Leave empty to use the reference rows themselves.")]
            double[][]? queryData = null,
            [Description("Maximum neighbours returned per query, between 1 and 1000. Default 100.")]
            int maxPerQuery = 100,
            [Description("Distance metric. Default Euclidean.")]
            NeighbourMetric metric = NeighbourMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1. Default 3.")]
            double minkowskiOrder = 3,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(referenceData, nameof(referenceData), 1, MultivariateLimits.MaximumClusteringRows);
            MultivariateGuard.Positive(radius, nameof(radius));
            MultivariateGuard.Bounded(maxPerQuery, nameof(maxPerQuery), 1, MultivariateLimits.MaximumNeighbours);
            var selfQuery = queryData == null;
            var queries = queryData ?? referenceData;

            if (!selfQuery)
            {
                MultivariateGuard.Matrix(queryData, nameof(queryData), 1, MultivariateLimits.MaximumQuadraticRows);
                ProjectionTools.RequireSameColumns(referenceData, queryData!, nameof(queryData));
            }
            else if (queries.Length > MultivariateLimits.MaximumQuadraticRows)
            {
                throw new ArgumentException(
                    $"With no queryData every reference row is a query, which is limited to {MultivariateLimits.MaximumQuadraticRows} rows. Provide a smaller queryData.",
                    nameof(queryData));
            }

            var function = NeighbourDistance(metric, minkowskiOrder);
            var indices = Enumerable.Range(0, referenceData.Length).ToArray();
            var tree = KDTree.FromData(referenceData, indices, TreeMetric(metric, minkowskiOrder));
            var results = new NeighbourQueryResult[queries.Length];

            for (var q = 0; q < queries.Length; q++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var found = tree.Nearest(queries[q], radius)
                    .Select(n => n.Node.Value)
                    .Where(row => !(selfQuery && row == q))
                    .Select(row => new NeighbourMatch(row, function(queries[q], referenceData[row])))
                    .Where(m => m.Distance <= radius)
                    .OrderBy(m => m.Distance)
                    .ThenBy(m => m.Row)
                    .Take(maxPerQuery)
                    .ToArray();

                results[q] = new NeighbourQueryResult(q, found);
            }

            return results;
        }

        [AdvancedAnalyticsTool("ClassifyByNearestNeighbours")]
        [Description(
            "Classifies each new row by a vote among the k nearest labelled training rows (the k-nearest-neighbours rule), with nothing saved. " +
            "It returns the predicted label per row (the class with most votes among the k neighbours, ties broken by the closer neighbours), per row the share of the k neighbours voting for each class in the order of ClassLabels (a 0.6 means 6 of 10 neighbours belong to it) and per row the distance weighted share, where each neighbour counts 1 divided by 1 plus its distance so nearer neighbours matter more. Use it as a transparent baseline or to see how mixed the neighbourhood of a case is. " +
            "Scale columns first if they use different units. Labels can be any integers. Bounded: at most 100000 training rows, 5000 new rows, 1000 neighbours.")]
        public NeighbourClassificationResult ClassifyByNearestNeighbours(
            [Description("Training rows of finite numbers, all the same length. At most 100000.")]
            double[][] trainingData,
            [Description("One integer class label per training row.")]
            int[] trainingLabels,
            [Description("Rows to classify, with the same number of columns as trainingData. At most 5000.")]
            double[][] newData,
            [Description(
                "How many neighbours vote, from 1 to the number of training rows and at most 1000. Default 5.")]
            int neighbours = 5,
            [Description("Distance metric. Default Euclidean.")]
            NeighbourMetric metric = NeighbourMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1. Default 3.")]
            double minkowskiOrder = 3)
        {
            MultivariateGuard.Matrix(trainingData, nameof(trainingData), 1, MultivariateLimits.MaximumClusteringRows);
            MultivariateGuard.Labels(trainingLabels, nameof(trainingLabels), trainingData.Length, nameof(trainingData));
            MultivariateGuard.Matrix(newData, nameof(newData), 1, MultivariateLimits.MaximumQuadraticRows);
            ProjectionTools.RequireSameColumns(trainingData, newData, nameof(newData));

            MultivariateGuard.Bounded(neighbours, nameof(neighbours), 1,
                Math.Min(MultivariateLimits.MaximumNeighbours, trainingData.Length));

            var distinct = trainingLabels.Distinct().OrderBy(v => v).ToArray();
            var index = distinct.Select((label, i) => (label, i)).ToDictionary(t => t.label, t => t.i);
            var mapped = trainingLabels.Select(v => index[v]).ToArray();

            var model = new KNearestNeighbors(neighbours, TreeMetric(metric, minkowskiOrder));
            model.Learn(trainingData, mapped);
            var predicted = new int[newData.Length];
            var shares = new double[newData.Length][];
            var weighted = new double[newData.Length][];

            for (var i = 0; i < newData.Length; i++)
            {
                model.GetNearestNeighbors(newData[i], out var found);
                var votes = new double[distinct.Length];

                foreach (var label in found)
                {
                    votes[label]++;
                }

                var scores = model.Scores(newData[i]);
                var total = scores.Sum();
                shares[i] = votes.Select(v => v / found.Length).ToArray();
                weighted[i] = scores.Select(sc => total > 0 ? sc / total : 0.0).ToArray();
                var best = 0;

                for (var c = 1; c < shares[i].Length; c++)
                {
                    if (shares[i][c] > shares[i][best]
                        || (shares[i][c] == shares[i][best] && weighted[i][c] > weighted[i][best]))
                    {
                        best = c;
                    }
                }

                predicted[i] = distinct[best];
            }

            return new NeighbourClassificationResult(newData.Length, distinct, predicted, shares, weighted);
        }

        [AdvancedAnalyticsTool("DetectOutliersByNearestNeighbourDistance")]
        [Description(
            "Flags rows that lie unusually far from their neighbours, a density based outlier check: for each row it computes the distance to its k-th nearest other row and the mean distance to its k nearest, then flags rows whose k-th neighbour distance exceeds the upper quartile by iqrMultiplier interquartile ranges (the usual box plot rule). " +
            "Isolated rows have large distances; a tight group of several similar outliers can hide each other if k is not larger than the group size, so choose k above the size of the smallest anomalous group you care about. Scale columns first if they use different units. Bounded: at most 5000 rows and 1000 neighbours.")]
        public NeighbourOutlierResult DetectOutliersByNearestNeighbourDistance(
            [Description("Rows of finite numbers, all the same length. At least 3 rows, at most 5000.")]
            double[][] data,
            [Description(
                "How many neighbours define the local scale, from 1 to rows minus 1 (at most 1000). Default 5.")]
            int neighbours = 5,
            [Description(
                "Multiple of the interquartile range added to the upper quartile to set the threshold, between 0 and 100. Default 1.5.")]
            double iqrMultiplier = 1.5,
            [Description("Distance metric. Default Euclidean.")]
            NeighbourMetric metric = NeighbourMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1. Default 3.")]
            double minkowskiOrder = 3,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 3, MultivariateLimits.MaximumQuadraticRows);

            MultivariateGuard.Bounded(neighbours, nameof(neighbours), 1,
                Math.Min(MultivariateLimits.MaximumNeighbours, data.Length - 1));

            MultivariateGuard.BoundedDouble(iqrMultiplier, nameof(iqrMultiplier), 0, 100);
            var function = NeighbourDistance(metric, minkowskiOrder);

            var kth = new double[data.Length];
            var mean = new double[data.Length];

            for (var i = 0; i < data.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var distances = new List<double>(data.Length - 1);

                for (var j = 0; j < data.Length; j++)
                {
                    if (i != j)
                    {
                        distances.Add(function(data[i], data[j]));
                    }
                }

                distances.Sort();
                kth[i] = distances[neighbours - 1];
                mean[i] = distances.Take(neighbours).Average();
            }

            var q1 = kth.Quantile(0.25);
            var q3 = kth.Quantile(0.75);
            var threshold = q3 + iqrMultiplier * (q3 - q1);
            var flags = kth.Select(v => v > threshold).ToArray();

            return new NeighbourOutlierResult(data.Length, neighbours, kth, mean, threshold, flags,
                Enumerable.Range(0, flags.Length).Where(i => flags[i]).ToArray());
        }

        [AdvancedAnalyticsTool("MineAssociationRules")]
        [Description(
            "Finds items that frequently occur together in transactions (market basket analysis) and turns them into if-then rules with the Apriori algorithm. " +
            "Each transaction is a list of item names such as the merchant categories, products or alert types seen together for one customer or basket; repeats inside one transaction are ignored. It returns the frequent itemsets (item groups present in at least minSupport of transactions) and rules 'antecedent implies consequent' with support (share of transactions containing both sides), confidence (share of antecedent transactions that also have the consequent), lift (confidence divided by the consequent's own support; above 1 means the items occur together more than by chance, 1 means independent) and leverage (support minus the product of the two sides' supports). " +
            "Rules are ordered by lift then confidence. Bounded so it cannot explode: at most 20000 transactions, 12 distinct items in one transaction, 200 distinct items in total, minSupport of at least 0.001, and at most 5000 itemsets and 1000 rules returned.")]
        public AssociationMiningResult MineAssociationRules(
            [Description(
                "One array of item names per transaction; at least 1 transaction, at most 20000, at most 12 distinct items in each and 200 distinct items overall.")]
            string[][] transactions,
            [Description("Minimum share of transactions an itemset must appear in, between 0.001 and 1. Default 0.1.")]
            double minSupport = 0.1,
            [Description("Minimum confidence for a rule, between 0 and 1. Default 0.5.")]
            double minConfidence = 0.5,
            [Description("Maximum number of rules to return, between 1 and 1000. Default 100.")]
            int maxRules = 100,
            [Description("Maximum number of frequent itemsets to return, between 1 and 5000. Default 500.")]
            int maxItemsets = 500)
        {
            if (transactions == null || transactions.Length == 0)
            {
                throw new ArgumentException("transactions is required: give at least one array of item names.",
                    nameof(transactions));
            }

            if (transactions.Length > MultivariateLimits.MaximumTransactions)
            {
                throw new ArgumentException(
                    $"transactions has {transactions.Length} entries but at most 20000 are accepted.",
                    nameof(transactions));
            }

            MultivariateGuard.BoundedDouble(minSupport, nameof(minSupport), 0.001, 1);
            MultivariateGuard.BoundedDouble(minConfidence, nameof(minConfidence), 0, 1);
            MultivariateGuard.Bounded(maxRules, nameof(maxRules), 1, 1000);
            MultivariateGuard.Bounded(maxItemsets, nameof(maxItemsets), 1, 5000);

            var cleaned = new string[transactions.Length][];

            for (var i = 0; i < transactions.Length; i++)
            {
                if (transactions[i] == null)
                {
                    throw new ArgumentException($"transactions[{i}] is null; use an empty array for an empty basket.",
                        nameof(transactions));
                }

                cleaned[i] = transactions[i].Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                if (cleaned[i].Length > 12)
                {
                    throw new ArgumentException(
                        $"transactions[{i}] has {cleaned[i].Length} distinct items but at most 12 are accepted per transaction.",
                        nameof(transactions));
                }
            }

            var vocabulary = cleaned.SelectMany(t => t).Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToArray();

            if (vocabulary.Length > 200)
            {
                throw new ArgumentException(
                    $"transactions hold {vocabulary.Length} distinct items but at most 200 are accepted. Group rare items first.",
                    nameof(transactions));
            }

            var codes = vocabulary.Select((item, i) => (item, i))
                .ToDictionary(t => t.item, t => t.i, StringComparer.Ordinal);

            var encoded = cleaned.Select(t => t.Select(item => codes[item]).ToArray()).ToArray();
            var count = (int)Math.Max(1, Math.Ceiling(minSupport * transactions.Length - 1e-9));

            var apriori = new Apriori(count, minConfidence);
            var matcher = apriori.Learn(encoded);
            var n = (double)transactions.Length;

            int Count(IEnumerable<int> items)
            {
                var key = new SortedSet<int>(items);

                return apriori.Frequent.TryGetValue(key, out var found) ? found : 0;
            }

            var itemsets = apriori.Frequent
                .OrderByDescending(p => p.Value)
                .ThenBy(p => p.Key.Count)
                .ThenBy(p => string.Join("\u0001", p.Key.Select(c => vocabulary[c])), StringComparer.Ordinal)
                .Take(maxItemsets)
                .Select(p => new FrequentItemset(p.Key.Select(c => vocabulary[c]).ToArray(), p.Value, p.Value / n))
                .ToArray();

            var rules = matcher.Rules
                .Select(r =>
                {
                    var xCount = Count(r.X);
                    var yCount = Count(r.Y);
                    var both = (int)Math.Round(r.Support);
                    var support = both / n;
                    var confidence = xCount == 0 ? 0.0 : both / (double)xCount;
                    var yShare = yCount / n;

                    return new AssociationRuleResult(
                        r.X.Select(c => vocabulary[c]).ToArray(),
                        r.Y.Select(c => vocabulary[c]).ToArray(),
                        support,
                        confidence,
                        yShare > 0 ? confidence / yShare : double.NaN,
                        support - xCount / n * yShare,
                        xCount,
                        both);
                })
                .OrderByDescending(r => r.Lift)
                .ThenByDescending(r => r.Confidence)
                .ThenByDescending(r => r.Support)
                .ThenBy(r => string.Join("\u0001", r.Antecedent) + "\u0002" + string.Join("\u0001", r.Consequent),
                    StringComparer.Ordinal)
                .Take(maxRules)
                .ToArray();

            return new AssociationMiningResult(transactions.Length, vocabulary.Length, itemsets, rules);
        }

        private static Func<double[], double[], double> NeighbourDistance(NeighbourMetric metric, double order)
        {
            return DistanceFactory.Create(metric switch
            {
                NeighbourMetric.Manhattan => VectorDistanceMetric.Manhattan,
                NeighbourMetric.Chebyshev => VectorDistanceMetric.Chebyshev,
                NeighbourMetric.Minkowski => VectorDistanceMetric.Minkowski,
                _ => VectorDistanceMetric.Euclidean
            }, order);
        }

        private static IMetric<double[]> TreeMetric(NeighbourMetric metric, double order)
        {
            switch (metric)
            {
                case NeighbourMetric.Manhattan:
                    return new Manhattan();
                case NeighbourMetric.Chebyshev:
                    return new Chebyshev();
                case NeighbourMetric.Minkowski:
                    if (double.IsNaN(order) || order < 1)
                    {
                        throw new ArgumentException($"minkowskiOrder must be at least 1 but was {order}.",
                            "minkowskiOrder");
                    }

                    return new Minkowski(order);
                default:
                    return new Euclidean();
            }
        }

        private static Func<double[], int, IEnumerable<int>> BuildSearcher(double[][] reference, NeighbourMetric metric,
            double order, NeighbourSearchIndex index, Func<double[], double[], double> function)
        {
            var indices = Enumerable.Range(0, reference.Length).ToArray();

            switch (index)
            {
                case NeighbourSearchIndex.KdTree:
                {
                    var tree = KDTree.FromData(reference, indices, TreeMetric(metric, order));

                    return (query, k) => tree.Nearest(query, Math.Min(k, reference.Length)).Select(n => n.Node.Value)
                        .ToList();
                }
                case NeighbourSearchIndex.VpTree:
                {
                    var tree = VPTree.FromData(reference, indices,
                        new DelegateDistance(function));

                    return (query, k) => tree.Nearest(query, Math.Min(k, reference.Length)).Select(n => n.Node.Value)
                        .ToList();
                }
                default:
                    return (query, k) => Enumerable.Range(0, reference.Length)
                        .Select(i => (Row: i, Distance: function(query, reference[i])))
                        .OrderBy(t => t.Distance)
                        .ThenBy(t => t.Row)
                        .Take(k)
                        .Select(t => t.Row)
                        .ToList();
            }
        }
    }
}