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
using Accord.Math;
using Accord.Math.Distances;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    [AdvancedAnalyticsToolGroup("MultivariateAndClustering")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class DistanceAndKernelTools
    {
        [AdvancedAnalyticsTool("ComputeVectorDistance")]
        [Description(
            "Computes one distance between two numeric vectors of the same length using the chosen metric: Euclidean, SquaredEuclidean, Manhattan, Chebyshev, Minkowski (order minkowskiOrder), Cosine (1 minus cosine similarity), Angular (radians), Canberra, BrayCurtis, Hellinger (for non negative probability vectors), PearsonCorrelation (1 minus correlation) and the binary metrics Hamming, Jaccard, Dice, Matching, Kulczynski, RogersTanimoto, RusselRao, SokalMichener, SokalSneath and Yule which read every non zero value as true. " +
            "Zero means identical; larger means more different. For Cosine, Jaccard and Dice the similarity is one minus the distance. Non finite numbers or unequal lengths are rejected with an explanation.")]
        public double ComputeVectorDistance(
            [Description("First vector of finite numbers.")]
            double[] x,
            [Description("Second vector of finite numbers, the same length as x.")]
            double[] y,
            [Description("The distance metric. Default Euclidean.")]
            VectorDistanceMetric metric = VectorDistanceMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1; 1 is Manhattan and 2 is Euclidean. Default 3.")]
            double minkowskiOrder = 3,
            [Description(
                "One non negative weight per dimension, used only by the WeightedEuclidean and WeightedSquaredEuclidean metrics. Leave empty otherwise.")]
            double[]? weights = null)
        {
            MultivariateGuard.Vector(x, nameof(x), 1, MultivariateLimits.MaximumColumns * 100);
            MultivariateGuard.Vector(y, nameof(y), 1, MultivariateLimits.MaximumColumns * 100);

            if (x.Length != y.Length)
            {
                throw new ArgumentException(
                    $"x has {x.Length} values and y has {y.Length}; they must be the same length.", nameof(y));
            }

            RequireWeightLength(weights, metric, x.Length);

            return DistanceFactory.Create(metric, minkowskiOrder, weights)(x, y);
        }

        [AdvancedAnalyticsTool("ComputeDistanceMatrixWithinSet")]
        [Description(
            "Computes the full pairwise distance matrix between every pair of rows of one data set: cell [i][j] is the distance from row i to row j, the diagonal is zero and the matrix is symmetric. " +
            "Use it to inspect similarity structure, feed a clustering or find near duplicates. At most 2000 rows are accepted because the result has rows squared cells. The summary reports the smallest and largest off diagonal distance and their mean.")]
        public DistanceMatrixResult ComputeDistanceMatrixWithinSet(
            [Description("Rows of finite numbers, all the same length. At most 2000 rows.")]
            double[][] data,
            [Description("The distance metric. Default Euclidean; see ComputeVectorDistance for the choices.")]
            VectorDistanceMetric metric = VectorDistanceMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1. Default 3.")]
            double minkowskiOrder = 3,
            [Description("One non negative weight per column for the weighted metrics only. Leave empty otherwise.")]
            double[]? weights = null,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 1, MultivariateLimits.MaximumPairwiseRows);
            RequireWeightLength(weights, metric, data[0].Length);
            var distance = DistanceFactory.Create(metric, minkowskiOrder, weights);

            var matrix = new double[data.Length][];
            var minimum = double.PositiveInfinity;
            var maximum = double.NegativeInfinity;
            var sum = 0.0;
            var count = 0;

            for (var i = 0; i < data.Length; i++)
            {
                matrix[i] = new double[data.Length];
            }

            for (var i = 0; i < data.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var j = i + 1; j < data.Length; j++)
                {
                    var value = distance(data[i], data[j]);
                    matrix[i][j] = value;
                    matrix[j][i] = value;
                    minimum = Math.Min(minimum, value);
                    maximum = Math.Max(maximum, value);
                    sum += value;
                    count++;
                }
            }

            return new DistanceMatrixResult(data.Length, data.Length, metric.ToString(), matrix,
                count == 0 ? double.NaN : minimum, count == 0 ? double.NaN : maximum,
                count == 0 ? double.NaN : sum / count);
        }

        [AdvancedAnalyticsTool("ComputeDistanceMatrixBetweenSets")]
        [Description(
            "Computes the rectangular distance matrix between two data sets: cell [i][j] is the distance from row i of setA to row j of setB. " +
            "Use it to compare new cases against a reference population. Each set has at most 2000 rows and both must have the same number of columns. The summary reports the smallest, largest and mean distance over all cells.")]
        public DistanceMatrixResult ComputeDistanceMatrixBetweenSets(
            [Description("First set of rows of finite numbers. At most 2000 rows.")]
            double[][] setA,
            [Description(
                "Second set of rows of finite numbers with the same number of columns as setA. At most 2000 rows.")]
            double[][] setB,
            [Description("The distance metric. Default Euclidean.")]
            VectorDistanceMetric metric = VectorDistanceMetric.Euclidean,
            [Description("Order p for the Minkowski metric, at least 1. Default 3.")]
            double minkowskiOrder = 3,
            [Description("One non negative weight per column for the weighted metrics only. Leave empty otherwise.")]
            double[]? weights = null,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(setA, nameof(setA), 1, MultivariateLimits.MaximumPairwiseRows);
            MultivariateGuard.Matrix(setB, nameof(setB), 1, MultivariateLimits.MaximumPairwiseRows);

            if (setA[0].Length != setB[0].Length)
            {
                throw new ArgumentException(
                    $"setA has {setA[0].Length} columns and setB has {setB[0].Length}; they must match.", nameof(setB));
            }

            RequireWeightLength(weights, metric, setA[0].Length);
            var distance = DistanceFactory.Create(metric, minkowskiOrder, weights);

            var matrix = new double[setA.Length][];
            var minimum = double.PositiveInfinity;
            var maximum = double.NegativeInfinity;
            var sum = 0.0;

            for (var i = 0; i < setA.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                matrix[i] = new double[setB.Length];

                for (var j = 0; j < setB.Length; j++)
                {
                    var value = distance(setA[i], setB[j]);
                    matrix[i][j] = value;
                    minimum = Math.Min(minimum, value);
                    maximum = Math.Max(maximum, value);
                    sum += value;
                }
            }

            return new DistanceMatrixResult(setA.Length, setB.Length, metric.ToString(), matrix, minimum, maximum,
                sum / ((double)setA.Length * setB.Length));
        }

        [AdvancedAnalyticsTool("ComputeKernelValue")]
        [Description(
            "Evaluates one kernel function between two vectors, returning a single similarity number where larger means more alike. " +
            "Use it to inspect how a kernel and its parameters treat a pair of cases before using the kernel in kernel PCA or discriminant analysis. Non finite input and invalid kernel parameters are rejected with an explanation.")]
        public double ComputeKernelValue(
            [Description("First vector of finite numbers.")]
            double[] x,
            [Description("Second vector of finite numbers, the same length as x.")]
            double[] y,
            [Description(KernelText.Kernel)] SimilarityKernelKind kernel = SimilarityKernelKind.Gaussian,
            [Description(KernelText.Sigma)] double kernelSigma = 1,
            [Description(KernelText.Gamma)] double kernelGamma = 1,
            [Description(KernelText.Constant)] double? kernelConstant = null,
            [Description(KernelText.Degree)] int kernelDegree = 2,
            [Description(KernelText.Alpha)] double kernelAlpha = 1,
            [Description(KernelText.Beta)] double kernelBeta = 1)
        {
            MultivariateGuard.Vector(x, nameof(x), 1, MultivariateLimits.MaximumColumns * 100);
            MultivariateGuard.Vector(y, nameof(y), 1, MultivariateLimits.MaximumColumns * 100);

            if (x.Length != y.Length)
            {
                throw new ArgumentException(
                    $"x has {x.Length} values and y has {y.Length}; they must be the same length.", nameof(y));
            }

            return KernelFactory.Create(kernel, kernelSigma, kernelGamma, kernelConstant, kernelDegree, kernelAlpha,
                kernelBeta, x.Length).Function(x, y);
        }

        [AdvancedAnalyticsTool("ComputeKernelGramMatrix")]
        [Description(
            "Computes the kernel (Gram) matrix of one data set: cell [i][j] is the kernel similarity of row i and row j, so the matrix is symmetric and the diagonal holds each row's similarity to itself. " +
            "Use it to see cluster structure a kernel exposes, or to hand a similarity matrix onward. At most 1000 rows. With normalise true each cell is divided by the square root of the two diagonal cells so the diagonal is exactly 1 and values are comparable across kernels.")]
        public KernelMatrixResult ComputeKernelGramMatrix(
            [Description("Rows of finite numbers, all the same length. At most 1000 rows.")]
            double[][] data,
            [Description(KernelText.Kernel)] SimilarityKernelKind kernel = SimilarityKernelKind.Gaussian,
            [Description(KernelText.Sigma)] double kernelSigma = 1,
            [Description(KernelText.Gamma)] double kernelGamma = 1,
            [Description(KernelText.Constant)] double? kernelConstant = null,
            [Description(KernelText.Degree)] int kernelDegree = 2,
            [Description(KernelText.Alpha)] double kernelAlpha = 1,
            [Description(KernelText.Beta)] double kernelBeta = 1,
            [Description(
                "Whether to rescale so the diagonal is 1 (cosine normalisation of the kernel). Default false.")]
            bool normalise = false,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(data, nameof(data), 1, MultivariateLimits.MaximumKernelRows);

            var function = KernelFactory.Create(kernel, kernelSigma, kernelGamma, kernelConstant, kernelDegree,
                kernelAlpha, kernelBeta, data[0].Length);

            var n = data.Length;
            var matrix = new double[n][];

            for (var i = 0; i < n; i++)
            {
                matrix[i] = new double[n];
            }

            for (var i = 0; i < n; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var j = i; j < n; j++)
                {
                    var value = function.Function(data[i], data[j]);
                    matrix[i][j] = value;
                    matrix[j][i] = value;
                }
            }

            var diagonal = new double[n];

            for (var i = 0; i < n; i++)
            {
                diagonal[i] = matrix[i][i];
            }

            if (normalise)
            {
                var raw = (double[])diagonal.Clone();

                for (var i = 0; i < n; i++)
                {
                    for (var j = 0; j < n; j++)
                    {
                        matrix[i][j] = matrix[i][j] / Math.Sqrt(raw[i] * raw[j]);
                    }
                }

                for (var i = 0; i < n; i++)
                {
                    diagonal[i] = matrix[i][i];
                }
            }

            return new KernelMatrixResult(n, n, kernel.ToString(), matrix, diagonal);
        }

        [AdvancedAnalyticsTool("ComputeKernelCrossMatrix")]
        [Description(
            "Computes the kernel similarity between every row of setA and every row of setB, giving a rectangular matrix where cell [i][j] compares row i of setA with row j of setB. " +
            "Use it to compare new cases with a reference set under a kernel. Each set has at most 1000 rows and both must have the same number of columns. The diagonal field is empty because the matrix is not square.")]
        public KernelMatrixResult ComputeKernelCrossMatrix(
            [Description("First set of rows of finite numbers. At most 1000 rows.")]
            double[][] setA,
            [Description(
                "Second set of rows of finite numbers with the same number of columns as setA. At most 1000 rows.")]
            double[][] setB,
            [Description(KernelText.Kernel)] SimilarityKernelKind kernel = SimilarityKernelKind.Gaussian,
            [Description(KernelText.Sigma)] double kernelSigma = 1,
            [Description(KernelText.Gamma)] double kernelGamma = 1,
            [Description(KernelText.Constant)] double? kernelConstant = null,
            [Description(KernelText.Degree)] int kernelDegree = 2,
            [Description(KernelText.Alpha)] double kernelAlpha = 1,
            [Description(KernelText.Beta)] double kernelBeta = 1,
            CancellationToken cancellationToken = default)
        {
            MultivariateGuard.Matrix(setA, nameof(setA), 1, MultivariateLimits.MaximumKernelRows);
            MultivariateGuard.Matrix(setB, nameof(setB), 1, MultivariateLimits.MaximumKernelRows);

            if (setA[0].Length != setB[0].Length)
            {
                throw new ArgumentException(
                    $"setA has {setA[0].Length} columns and setB has {setB[0].Length}; they must match.", nameof(setB));
            }

            var function = KernelFactory.Create(kernel, kernelSigma, kernelGamma, kernelConstant, kernelDegree,
                kernelAlpha, kernelBeta, setA[0].Length);

            var matrix = new double[setA.Length][];

            for (var i = 0; i < setA.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                matrix[i] = new double[setB.Length];

                for (var j = 0; j < setB.Length; j++)
                {
                    matrix[i][j] = function.Function(setA[i], setB[j]);
                }
            }

            return new KernelMatrixResult(setA.Length, setB.Length, kernel.ToString(), matrix, Array.Empty<double>());
        }

        [AdvancedAnalyticsTool("CompareHistogramsBhattacharyya")]
        [Description(
            "Measures how much two discrete distributions overlap, given as two arrays of counts or proportions over the same categories or bins in the same order. " +
            "Each array is first scaled to sum to 1. It returns the Bhattacharyya coefficient (1 means identical distributions, 0 means no overlap), the Bhattacharyya distance (minus the natural log of the coefficient, 0 for identical and infinite for no overlap) and the Hellinger distance (the square root of one minus the coefficient, between 0 and 1). " +
            "Use it to compare a current distribution with a baseline, for example transaction amount bands this month against last month.")]
        public DiscreteDistributionOverlapResult CompareHistogramsBhattacharyya(
            [Description(
                "Non negative counts or proportions per category or bin for the first distribution; at least one must be above zero.")]
            double[] histogramA,
            [Description(
                "Non negative counts or proportions for the second distribution over the same categories, the same length as histogramA.")]
            double[] histogramB)
        {
            MultivariateGuard.Vector(histogramA, nameof(histogramA), 1, 100000);
            MultivariateGuard.Vector(histogramB, nameof(histogramB), 1, 100000);

            if (histogramA.Length != histogramB.Length)
            {
                throw new ArgumentException(
                    $"histogramA has {histogramA.Length} bins and histogramB has {histogramB.Length}; they must match.",
                    nameof(histogramB));
            }

            var a = Normalise(histogramA, nameof(histogramA));
            var b = Normalise(histogramB, nameof(histogramB));
            var hellinger = new Bhattacharyya().Distance(a, b);
            var coefficient = Math.Max(0.0, Math.Min(1.0, 1.0 - hellinger * hellinger));

            return new DiscreteDistributionOverlapResult(coefficient,
                coefficient <= 0 ? double.PositiveInfinity : -Math.Log(coefficient), hellinger);
        }

        [AdvancedAnalyticsTool("CompareSamplesBhattacharyyaGaussian")]
        [Description(
            "Measures how far apart two multivariate samples are by fitting a Gaussian (mean and covariance) to each and returning the Bhattacharyya distance between the two fitted Gaussians. " +
            "The distance grows with both the separation of the means and the difference in covariance; 0 means the fitted Gaussians coincide. For two Gaussians with identity covariance it equals one eighth of the squared distance between the means. " +
            "Use it to quantify drift between a baseline population and a current one. Each sample needs at least 3 rows and the same number of columns.")]
        public double CompareSamplesBhattacharyyaGaussian(
            [Description("First sample: rows of finite numbers, at least 3 rows, at most 20000.")]
            double[][] sampleA,
            [Description(
                "Second sample: rows of finite numbers with the same number of columns as sampleA, at least 3 rows.")]
            double[][] sampleB)
        {
            MultivariateGuard.Matrix(sampleA, nameof(sampleA), 3, MultivariateLimits.MaximumProjectionRows);
            MultivariateGuard.Matrix(sampleB, nameof(sampleB), 3, MultivariateLimits.MaximumProjectionRows);

            if (sampleA[0].Length != sampleB[0].Length)
            {
                throw new ArgumentException(
                    $"sampleA has {sampleA[0].Length} columns and sampleB has {sampleB[0].Length}; they must match.",
                    nameof(sampleB));
            }

            return new Bhattacharyya().Distance(sampleA, sampleB);
        }

        [AdvancedAnalyticsTool("ComputeStringDistance")]
        [Description(
            "Computes the edit distance between two strings: Levenshtein is the minimum number of single character insertions, deletions and substitutions; NormalisedLevenshtein divides that by the longer length so 0 is identical and 1 is completely different; Hamming counts differing positions and requires equal lengths. " +
            "Use it to compare names, references or account descriptions that may contain typing errors. Strings longer than 2000 characters are rejected. The returned similarity is 1 minus the normalised distance and is always between 0 and 1.")]
        public StringMatch ComputeStringDistance(
            [Description("The first string.")] string first,
            [Description("The second string.")] string second,
            [Description("Levenshtein (default), NormalisedLevenshtein or Hamming.")]
            StringDistanceKind kind = StringDistanceKind.Levenshtein,
            [Description("Whether upper and lower case letters are treated as the same. Default true.")]
            bool ignoreCase = true)
        {
            RequireString(first, nameof(first));
            RequireString(second, nameof(second));

            var (a, b) = ignoreCase ? (first.ToLowerInvariant(), second.ToLowerInvariant()) : (first, second);
            var distance = StringDistance(a, b, kind);

            return new StringMatch(0, second, distance, StringSimilarity(a, b, kind, distance));
        }

        [AdvancedAnalyticsTool("FindClosestStringMatches")]
        [Description(
            "Ranks a list of candidate strings by how close each is to a query string and returns the closest ones, nearest first. " +
            "Use it for fuzzy name or reference matching: the query is compared with each candidate using the chosen edit distance, and similarity is 1 minus the distance divided by the longer of the two lengths, so 1 is an exact match. " +
            "At most 5000 candidates of at most 2000 characters each; ties keep the input order.")]
        public StringMatch[] FindClosestStringMatches(
            [Description("The string to look for.")]
            string query,
            [Description("The candidate strings to search, at most 5000.")]
            string[] candidates,
            [Description("How many of the closest candidates to return, between 1 and 1000. Default 5.")]
            int top = 5,
            [Description(
                "Levenshtein (default), NormalisedLevenshtein or Hamming. Hamming candidates of a different length are skipped.")]
            StringDistanceKind kind = StringDistanceKind.Levenshtein,
            [Description("Whether upper and lower case letters are treated as the same. Default true.")]
            bool ignoreCase = true,
            CancellationToken cancellationToken = default)
        {
            RequireString(query, nameof(query));

            if (candidates == null || candidates.Length == 0)
            {
                throw new ArgumentException("candidates is required: give at least one string to search.",
                    nameof(candidates));
            }

            if (candidates.Length > MultivariateLimits.MaximumStrings)
            {
                throw new ArgumentException(
                    $"candidates has {candidates.Length} strings but at most {MultivariateLimits.MaximumStrings} are accepted.",
                    nameof(candidates));
            }

            MultivariateGuard.Bounded(top, nameof(top), 1, MultivariateLimits.MaximumNeighbours);
            var q = ignoreCase ? query.ToLowerInvariant() : query;
            var matches = new List<StringMatch>();

            for (var i = 0; i < candidates.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RequireString(candidates[i], $"{nameof(candidates)}[{i}]");
                var c = ignoreCase ? candidates[i].ToLowerInvariant() : candidates[i];

                if (kind == StringDistanceKind.Hamming && c.Length != q.Length)
                {
                    continue;
                }

                var distance = StringDistance(q, c, kind);
                matches.Add(new StringMatch(i, candidates[i], distance, StringSimilarity(q, c, kind, distance)));
            }

            return matches
                .Select((m, order) => (m, order))
                .OrderBy(t => t.m.Distance)
                .ThenBy(t => t.order)
                .Take(top)
                .Select(t => t.m)
                .ToArray();
        }

        [AdvancedAnalyticsTool("ComputeStringDistanceMatrix")]
        [Description(
            "Computes the pairwise edit distance matrix between every pair of strings in one list, symmetric with a zero diagonal. " +
            "Use it to find near duplicate names or references, or to prepare a clustering of text values. At most 500 strings of at most 2000 characters each; for Hamming every string must have the same length.")]
        public StringDistanceMatrixResult ComputeStringDistanceMatrix(
            [Description("The strings to compare, at most 500.")]
            string[] values,
            [Description("Levenshtein (default), NormalisedLevenshtein or Hamming.")]
            StringDistanceKind kind = StringDistanceKind.Levenshtein,
            [Description("Whether upper and lower case letters are treated as the same. Default true.")]
            bool ignoreCase = true,
            CancellationToken cancellationToken = default)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is required: give at least one string.", nameof(values));
            }

            if (values.Length > 500)
            {
                throw new ArgumentException($"values has {values.Length} strings but at most 500 are accepted.",
                    nameof(values));
            }

            var prepared = new string[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                RequireString(values[i], $"{nameof(values)}[{i}]");
                prepared[i] = ignoreCase ? values[i].ToLowerInvariant() : values[i];
            }

            var matrix = new double[values.Length][];

            for (var i = 0; i < values.Length; i++)
            {
                matrix[i] = new double[values.Length];
            }

            for (var i = 0; i < values.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var j = i + 1; j < values.Length; j++)
                {
                    var value = StringDistance(prepared[i], prepared[j], kind);
                    matrix[i][j] = value;
                    matrix[j][i] = value;
                }
            }

            return new StringDistanceMatrixResult(values.Length, kind.ToString(), matrix);
        }

        [AdvancedAnalyticsTool("ComputeTokenSequenceEditDistance")]
        [Description(
            "Computes the edit distance between two ordered sequences of tokens, such as the sequence of actions in two sessions or the sequence of merchant categories in two customers' histories: the minimum number of single token insertions, deletions and substitutions that turns one sequence into the other. " +
            "It returns the distance and a similarity between 0 and 1 (1 minus the distance divided by the longer sequence length, so 1 means identical order and content; two empty sequences give 1). Unlike ComputeTokenSetSimilarity the order matters. At most 2000 tokens per sequence.")]
        public StringMatch ComputeTokenSequenceEditDistance(
            [Description("Tokens of the first sequence, in order; empty is allowed.")]
            string[] sequenceA,
            [Description("Tokens of the second sequence, in order; empty is allowed.")]
            string[] sequenceB,
            [Description("Whether upper and lower case are treated as the same. Default true.")]
            bool ignoreCase = true)
        {
            if (sequenceA == null || sequenceB == null)
            {
                throw new ArgumentException(
                    "sequenceA and sequenceB are both required; pass an empty array for no tokens.",
                    sequenceA == null ? nameof(sequenceA) : nameof(sequenceB));
            }

            if (sequenceA.Length > MultivariateLimits.MaximumStringLength ||
                sequenceB.Length > MultivariateLimits.MaximumStringLength)
            {
                throw new ArgumentException(
                    $"Each sequence may hold at most {MultivariateLimits.MaximumStringLength} tokens.",
                    nameof(sequenceA));
            }

            var a = sequenceA.Select(t => t == null ? string.Empty : ignoreCase ? t.ToLowerInvariant() : t).ToArray();
            var b = sequenceB.Select(t => t == null ? string.Empty : ignoreCase ? t.ToLowerInvariant() : t).ToArray();
            var distance = new Levenshtein<string>().Distance(a, b);
            var longest = Math.Max(a.Length, b.Length);

            return new StringMatch(0, string.Join(" > ", sequenceB), distance,
                longest == 0 ? 1.0 : 1.0 - distance / longest);
        }

        [AdvancedAnalyticsTool("ComputeTokenSetSimilarity")]
        [Description(
            "Compares two lists of tokens (words, merchant categories, device identifiers, any strings) as sets, ignoring order and repeats. " +
            "It returns the intersection and union sizes, the Jaccard similarity (intersection divided by union), the Dice similarity (twice the intersection divided by the sum of the set sizes) and the overlap coefficient (intersection divided by the smaller set). All three similarities are between 0 and 1 and are 1 for identical sets; two empty sets give 1.")]
        public SetSimilarityResult ComputeTokenSetSimilarity(
            [Description("Tokens of the first set; duplicates are collapsed.")]
            string[] tokensA,
            [Description("Tokens of the second set; duplicates are collapsed.")]
            string[] tokensB,
            [Description("Whether upper and lower case are treated as the same. Default true.")]
            bool ignoreCase = true)
        {
            if (tokensA == null)
            {
                throw new ArgumentException("tokensA is required: pass an array of strings, which may be empty.",
                    nameof(tokensA));
            }

            if (tokensB == null)
            {
                throw new ArgumentException("tokensB is required: pass an array of strings, which may be empty.",
                    nameof(tokensB));
            }

            if (tokensA.Length > 100000 || tokensB.Length > 100000)
            {
                throw new ArgumentException("Each token list may hold at most 100000 tokens.", nameof(tokensA));
            }

            var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var setA = new HashSet<string>(tokensA.Where(t => t != null), comparer);
            var setB = new HashSet<string>(tokensB.Where(t => t != null), comparer);
            var universe = setA.Union(setB, comparer).ToArray();

            if (universe.Length == 0)
            {
                return new SetSimilarityResult(0, 0, 0, 0, 1, 1, 1);
            }

            var a = universe.Select(t => setA.Contains(t) ? 1.0 : 0.0).ToArray();
            var b = universe.Select(t => setB.Contains(t) ? 1.0 : 0.0).ToArray();
            var intersection = Enumerable.Count(universe, t => setA.Contains(t) && setB.Contains(t));
            var smaller = Math.Min(setA.Count, setB.Count);

            return new SetSimilarityResult(setA.Count, setB.Count, intersection, universe.Length,
                1.0 - Distance.Jaccard(a, b),
                1.0 - Distance.Dice(a, b),
                smaller == 0 ? 0.0 : intersection / (double)smaller);
        }

        private static void RequireWeightLength(double[]? weights, VectorDistanceMetric metric, int length)
        {
            if (metric != VectorDistanceMetric.WeightedEuclidean &&
                metric != VectorDistanceMetric.WeightedSquaredEuclidean)
            {
                return;
            }

            if (weights == null || weights.Length != length)
            {
                throw new ArgumentException(
                    $"The {metric} metric needs exactly one weight per dimension ({length}) in weights.",
                    nameof(weights));
            }
        }

        private static double[] Normalise(double[] histogram, string name)
        {
            if (histogram.Any(v => v < 0))
            {
                throw new ArgumentException($"{name} must not contain negative values.", name);
            }

            var total = Enumerable.Sum(histogram);

            if (total <= 0)
            {
                throw new ArgumentException($"{name} must have at least one value above zero.", name);
            }

            return histogram.Select(v => v / total).ToArray();
        }

        private static void RequireString(string? value, string name)
        {
            if (value == null)
            {
                throw new ArgumentException($"{name} is required.", name);
            }

            if (value.Length > MultivariateLimits.MaximumStringLength)
            {
                throw new ArgumentException(
                    $"{name} has {value.Length} characters but at most {MultivariateLimits.MaximumStringLength} are accepted.",
                    name);
            }
        }

        private static double StringDistance(string a, string b, StringDistanceKind kind)
        {
            switch (kind)
            {
                case StringDistanceKind.Hamming:
                    if (a.Length != b.Length)
                    {
                        throw new ArgumentException(
                            $"Hamming distance needs strings of equal length but got {a.Length} and {b.Length}. Use Levenshtein for unequal lengths.",
                            "kind");
                    }

                    return new Hamming<char>().Distance(a.ToCharArray(), b.ToCharArray());
                case StringDistanceKind.NormalisedLevenshtein:
                    var longest = Math.Max(a.Length, b.Length);

                    return longest == 0 ? 0.0 : new Levenshtein().Distance(a, b) / longest;
                default:
                    return new Levenshtein().Distance(a, b);
            }
        }

        private static double StringSimilarity(string a, string b, StringDistanceKind kind, double distance)
        {
            var longest = Math.Max(a.Length, b.Length);

            if (longest == 0)
            {
                return 1.0;
            }

            return kind == StringDistanceKind.NormalisedLevenshtein ? 1.0 - distance : 1.0 - distance / longest;
        }
    }
}