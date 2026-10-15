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
using Accord.Math.Distances;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    public enum ThresholdComparison
    {
        Above,
        AtOrAbove,
        Below,
        AtOrBelow
    }

    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SequencePatternTools
    {
        private static string[] Symbols(string[]? sequence, string name, int minimum)
        {
            if (sequence == null || sequence.Length < minimum)
            {
                throw new ArgumentException(
                    $"{name} needs at least {minimum} symbols in order but has {(sequence == null ? 0 : sequence.Length)}.",
                    name);
            }

            if (sequence.Any(s => s == null))
            {
                throw new ArgumentException(
                    $"{name} contains a missing (null) symbol. Replace it with a label such as 'Unknown'.", name);
            }

            return sequence;
        }

        [AdvancedAnalyticsTool("NGramFrequencyTable")]
        [Description(
            "Counts the most frequent runs of n consecutive symbols (n-grams) in an ordered sequence of categories, such as transaction types, channels, merchant categories or event codes. Use it to find recurring behavioural patterns, for example the typical three-step path before a chargeback. " +
            "NGrams[i] is the i-th most frequent pattern as an array of n symbols, with Counts[i] occurrences and Shares[i] of all windows; ties are ordered alphabetically. DistinctNGrams is how many different patterns exist and TotalNGrams the number of windows (length - n + 1). Symbols are compared exactly (case-sensitive). " +
            "Needs at least n symbols; topK limits the returned list.")]
        public NGramResult NGramFrequencyTable(
            [Description("The ordered symbols, for example ['login','transfer','transfer','logout'].")]
            string[] sequence,
            [Description("Pattern length, at least 1. Default 2.")]
            int n = 2,
            [Description("How many of the most frequent patterns to return, at least 1. Default 20.")]
            int topK = 20)
        {
            TimeSeriesGuard.AtLeast(n, 1, nameof(n));
            TimeSeriesGuard.AtLeast(topK, 1, nameof(topK));
            var seq = Symbols(sequence, nameof(sequence), n);
            var windows = new Dictionary<string, (string[] Gram, int Count)>(StringComparer.Ordinal);

            for (var i = 0; i + n <= seq.Length; i++)
            {
                var gram = seq.Skip(i).Take(n).ToArray();
                var key = string.Join("\u001f", gram);

                windows[key] = windows.TryGetValue(key, out var existing)
                    ? (existing.Gram, existing.Count + 1)
                    : (gram, 1);
            }

            var total = seq.Length - n + 1;

            var ordered = windows.Values.OrderByDescending(w => w.Count)
                .ThenBy(w => string.Join("\u001f", w.Gram), StringComparer.Ordinal).Take(topK).ToArray();

            return new NGramResult(ordered.Select(w => w.Gram).ToArray(), ordered.Select(w => w.Count).ToArray(),
                ordered.Select(w => w.Count / (double)total).ToArray(), windows.Count, total);
        }

        private static double BlockEntropy(string[] seq, int k)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i + k <= seq.Length; i++)
            {
                var key = string.Join("\u001f", seq.Skip(i).Take(k));
                counts[key] = counts.TryGetValue(key, out var c) ? c + 1 : 1;
            }

            var total = (double)(seq.Length - k + 1);

            return -counts.Values.Sum(c => c / total * MarkovMath.Log2(c / total));
        }

        private static double ConditionalEntropy(string[] seq, int order)
        {
            if (order == 0)
            {
                return BlockEntropy(seq, 1);
            }

            var grams = new Dictionary<string, int>(StringComparer.Ordinal);
            var contexts = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i + order + 1 <= seq.Length; i++)
            {
                var window = seq.Skip(i).Take(order + 1).ToArray();
                var gram = string.Join("\u001f", window);
                var context = string.Join("\u001f", window.Take(order));
                grams[gram] = grams.TryGetValue(gram, out var g) ? g + 1 : 1;
                contexts[context] = contexts.TryGetValue(context, out var c) ? c + 1 : 1;
            }

            var total = (double)(seq.Length - order);
            var entropy = 0.0;

            foreach (var (gram, count) in grams)
            {
                var context = string.Join("\u001f", gram.Split('\u001f').Take(order));
                entropy -= count / total * MarkovMath.Log2((double)count / contexts[context]);
            }

            return entropy;
        }

        [AdvancedAnalyticsTool("SymbolSequenceEntropyProfile")]
        [Description(
            "Shannon entropy profile of an ordered sequence of symbols: how unpredictable it is on its own and once earlier symbols are known. Use it to distinguish rigid scripted or automated behaviour (low entropy, predictable) from varied human behaviour, or to see how much memory a sequence has. " +
            "BlockEntropiesBits[k-1] is the entropy in bits of blocks of k consecutive symbols. ConditionalEntropiesBits[k] is the remaining uncertainty about the next symbol given the previous k (order 0 is plain symbol entropy); it falls as the sequence becomes more predictable from its history, reaching 0 for a deterministic sequence. NormalisedFirstOrderEntropy is entropy divided by log2 of the alphabet size (0 to 1; NaN for a one-symbol alphabet) and Redundancy is 1 minus it. " +
            "Estimates from short sequences are biased low, especially at higher orders. Needs at least 2 symbols; maxOrder is limited by the length.")]
        public SequenceEntropyResult SymbolSequenceEntropyProfile(
            [Description("The ordered symbols, at least 2.")]
            string[] sequence,
            [Description(
                "Largest block length and conditioning order to report, at least 1. Default 3; reduced automatically to fit the length.")]
            int maxOrder = 3)
        {
            var seq = Symbols(sequence, nameof(sequence), 2);
            TimeSeriesGuard.AtLeast(maxOrder, 1, nameof(maxOrder));
            var limit = Math.Min(maxOrder, seq.Length - 1);
            var alphabet = seq.Distinct(StringComparer.Ordinal).Count();
            var h1 = BlockEntropy(seq, 1);
            var normalised = alphabet > 1 ? h1 / MarkovMath.Log2(alphabet) : double.NaN;

            return new SequenceEntropyResult(
                Enumerable.Range(1, limit).Select(k => BlockEntropy(seq, k)).ToArray(),
                Enumerable.Range(0, limit).Select(k => ConditionalEntropy(seq, k)).ToArray(),
                normalised,
                double.IsNaN(normalised) ? double.NaN : 1 - normalised,
                alphabet,
                seq.Length);
        }

        [AdvancedAnalyticsTool("LempelZivSequenceComplexity")]
        [Description(
            "Lempel-Ziv (1976) complexity of a symbol sequence: the number of distinct new phrases met when the sequence is parsed from left to right, so repetitive sequences score low and irregular ones high. Use it as a compression-based measure of how patterned or random a sequence of events or statuses is, without assuming a probability model. " +
            "PhraseCount is the raw count (for the classic example 0001101001000101 it is 6). NormalisedComplexity is PhraseCount x log(n) / n using a logarithm to the base of the alphabet size, which is close to 1 for a random sequence and near 0 for a highly repetitive one (NaN for a single-symbol alphabet). " +
            "Needs at least 2 symbols; reliable normalised values need a few dozen symbols or more.")]
        public LempelZivResult LempelZivSequenceComplexity(
            [Description("The ordered symbols, at least 2.")]
            string[] sequence)
        {
            var seq = Symbols(sequence, nameof(sequence), 2);
            var n = seq.Length;
            var alphabet = seq.Distinct(StringComparer.Ordinal).Count();
            var c = 1;
            var l = 1;
            var i = 0;
            var k = 1;
            var kMax = 1;

            while (true)
            {
                if (seq[i + k - 1] == seq[l + k - 1])
                {
                    k++;

                    if (l + k > n)
                    {
                        c++;
                        break;
                    }
                }
                else
                {
                    kMax = Math.Max(kMax, k);
                    i++;

                    if (i == l)
                    {
                        c++;
                        l += kMax;

                        if (l + 1 > n)
                        {
                            break;
                        }

                        i = 0;
                        k = 1;
                        kMax = 1;
                    }
                    else
                    {
                        k = 1;
                    }
                }
            }

            var normalised = alphabet > 1 ? c * (Math.Log(n) / Math.Log(alphabet)) / n : double.NaN;

            return new LempelZivResult(c, normalised, alphabet, n);
        }

        [AdvancedAnalyticsTool("RunLengthEncodeSequence")]
        [Description(
            "Run-length encodes an ordered sequence of symbols: collapses each unbroken stretch of the same symbol into one run. Use it to measure how long a customer stays in one status or repeats the same action, and to find the longest streak. " +
            "Symbols, Lengths and StartIndices describe each run in order; RunCount is the number of runs, LongestRun the longest length and MeanRunLength the average. For ['a','a','b','c','c','c'] the runs are a x 2, b x 1 and c x 3. " +
            "Needs at least 1 symbol; symbols are compared exactly.")]
        public RunLengthResult RunLengthEncodeSequence(
            [Description("The ordered symbols, at least 1.")]
            string[] sequence)
        {
            var seq = Symbols(sequence, nameof(sequence), 1);
            var symbols = new List<string>();
            var lengths = new List<int>();
            var starts = new List<int>();

            for (var i = 0; i < seq.Length; i++)
            {
                if (i > 0 && seq[i] == seq[i - 1])
                {
                    lengths[^1]++;
                }
                else
                {
                    symbols.Add(seq[i]);
                    lengths.Add(1);
                    starts.Add(i);
                }
            }

            return new RunLengthResult(symbols.ToArray(), lengths.ToArray(), starts.ToArray(), lengths.Count,
                lengths.Max(),
                lengths.Average());
        }

        [AdvancedAnalyticsTool("ThresholdRunLengths")]
        [Description(
            "Finds the unbroken streaks of values that satisfy a threshold condition, for example consecutive days with volume above a limit or consecutive negative balance changes. Use it to measure persistence of an alert condition. " +
            "comparison chooses Above, AtOrAbove, Below or AtOrBelow the threshold. StartIndices and Lengths describe each streak in time order; RunCount is how many there are, LongestRun the longest, MeanRunLength their average and ShareOfPointsInRuns the fraction of all points that are inside a streak. " +
            "Missing (NaN) values never satisfy the condition and so break a streak. Returns zeros and NaN when no value satisfies the condition.")]
        public ThresholdRunsResult ThresholdRunLengths(
            [Description("The series in time order. NaN is allowed and breaks streaks.")]
            double[] values,
            [Description("The threshold value the comparison is made against.")]
            double threshold,
            [Description("Above, AtOrAbove, Below or AtOrBelow. Default Above.")]
            ThresholdComparison comparison = ThresholdComparison.Above)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);

            if (!double.IsFinite(threshold))
            {
                throw new ArgumentException("threshold must be a finite number.", nameof(threshold));
            }

            var starts = new List<int>();
            var lengths = new List<int>();

            for (var i = 0; i < v.Length; i++)
            {
                var hit = !double.IsNaN(v[i]) && comparison switch
                {
                    ThresholdComparison.Above => v[i] > threshold,
                    ThresholdComparison.AtOrAbove => v[i] >= threshold,
                    ThresholdComparison.Below => v[i] < threshold,
                    _ => v[i] <= threshold
                };

                if (!hit)
                {
                    continue;
                }

                if (i > 0 && lengths.Count > 0 && starts[^1] + lengths[^1] == i)
                {
                    lengths[^1]++;
                }
                else
                {
                    starts.Add(i);
                    lengths.Add(1);
                }
            }

            if (lengths.Count == 0)
            {
                return new ThresholdRunsResult(Array.Empty<int>(), Array.Empty<int>(), 0, 0, double.NaN, 0);
            }

            return new ThresholdRunsResult(starts.ToArray(), lengths.ToArray(), lengths.Count, lengths.Max(),
                lengths.Average(), lengths.Sum() / (double)v.Length);
        }

        private static double[] Paa(double[] v, int segments)
        {
            var n = v.Length;
            var result = new double[segments];
            var width = (double)n / segments;

            for (var s = 0; s < segments; s++)
            {
                var start = s * width;
                var end = start + width;
                var sum = 0.0;

                for (var i = (int)Math.Floor(start); i < Math.Min(n, (int)Math.Ceiling(end)); i++)
                {
                    var overlap = Math.Min(i + 1, end) - Math.Max(i, start);

                    if (overlap > 0)
                    {
                        sum += v[i] * overlap;
                    }
                }

                result[s] = sum / width;
            }

            return result;
        }

        [AdvancedAnalyticsTool("PiecewiseAggregateApproximation")]
        [Description(
            "Piecewise aggregate approximation (PAA): shrinks a long series to a fixed number of segments by averaging equal-width pieces, giving a compact shape summary with a fixed length regardless of the original length. Use it to compare series of different lengths, to build fixed-size features or as the first step of symbolic approximation. " +
            "When the length is not a multiple of the number of segments, points that straddle a boundary are split proportionally between the neighbouring segments. Values[s] is the mean of the s-th segment; for [1, 2, 3, 4, 5, 6] with 3 segments it is [1.5, 3.5, 5.5]. Requires finite values and segments between 1 and the series length.")]
        public PaaResult PiecewiseAggregateApproximation(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Number of output segments, between 1 and the series length.")]
            int segments)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 1);

            if (segments < 1 || segments > v.Length)
            {
                throw new ArgumentException(
                    $"segments must be between 1 and the series length {v.Length} but was {segments}.",
                    nameof(segments));
            }

            return new PaaResult(Paa(v, segments), segments);
        }

        [AdvancedAnalyticsTool("SymbolicAggregateApproximation")]
        [Description(
            "Symbolic aggregate approximation (SAX): turns a numeric series into a short word of letters. The series is optionally standardised to mean 0 and standard deviation 1, reduced to a fixed number of segment averages (PAA) and each average is mapped to a letter using breakpoints that split a standard normal distribution into equally likely bins (a is the lowest). Use it to compare shapes of series, to find repeated patterns with NGramFrequencyTable or to feed a sequence tool. " +
            "Word is the concatenated letters, Symbols the same as an array, Paa the segment averages (in standardised units when standardising) and Breakpoints the cut points used (for alphabetSize 4 they are -0.6745, 0 and 0.6745). Mean and StandardDeviation are the values used to standardise (population deviation). " +
            "A constant series cannot be standardised: it is mapped to the middle letters. Requires finite values, segments between 1 and the length, and alphabetSize from 2 to 20.")]
        public SaxResult SymbolicAggregateApproximation(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Number of letters in the word, between 1 and the series length.")]
            int segments,
            [Description("Number of distinct letters, from 2 to 20. Default 4.")]
            int alphabetSize = 4,
            [Description(
                "True (default) standardises the series first so only shape matters; false uses the raw values with the standard normal breakpoints.")]
            bool standardise = true)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 1);

            if (segments < 1 || segments > v.Length)
            {
                throw new ArgumentException(
                    $"segments must be between 1 and the series length {v.Length} but was {segments}.",
                    nameof(segments));
            }

            if (alphabetSize < 2 || alphabetSize > 20)
            {
                throw new ArgumentException($"alphabetSize must be between 2 and 20 but was {alphabetSize}.",
                    nameof(alphabetSize));
            }

            var mean = TimeSeriesGuard.Mean(v);
            var sd = Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / v.Length);
            var work = v;

            if (standardise)
            {
                work = sd > 0 ? v.Select(x => (x - mean) / sd).ToArray() : v.Select(_ => 0.0).ToArray();
            }

            var paa = Paa(work, segments);

            var breakpoints = Enumerable.Range(1, alphabetSize - 1)
                .Select(i => NormalDistribution.Standard.InverseDistributionFunction(i / (double)alphabetSize))
                .ToArray();

            var letters = paa.Select(x => ((char)('a' + breakpoints.Count(b => x >= b))).ToString()).ToArray();

            return new SaxResult(string.Concat(letters), letters, paa, breakpoints, mean, sd);
        }

        [AdvancedAnalyticsTool("DynamicTimeWarpingDistance")]
        [Description(
            "Dynamic time warping (DTW) distance between two numeric series: the smallest total cost of aligning them when either series may be stretched or compressed in time, so similar shapes that are shifted or run at different speeds still match. Use it to compare behavioural profiles or trajectories of different lengths where a plain point-by-point distance would fail. " +
            "The cost of aligning two points is their absolute difference (or the squared difference with squaredCost true). Distance is the total cost along the best path; NormalisedDistance divides by the path length; Path lists the aligned index pairs [index in x, index in y] from the start to the end. A band (bandRadius, in samples) limits how far the alignment may drift from the diagonal and speeds up the calculation; it is widened automatically to at least the difference in lengths. " +
            "Identical series give 0. Requires finite values; work grows with the product of the lengths (limit 25 million cells).")]
        public DtwResult DynamicTimeWarpingDistance(
            [Description("The first series, finite, at least 1 value.")]
            double[] x,
            [Description("The second series, finite, at least 1 value; the lengths may differ.")]
            double[] y,
            [Description(
                "Half-width of the allowed alignment band in samples, 0 or more; -1 (default) allows any alignment.")]
            int bandRadius = -1,
            [Description("True uses squared differences as the point cost; false (default) uses absolute differences.")]
            bool squaredCost = false)
        {
            var a = TimeSeriesGuard.Finite(x, nameof(x), 1);
            var b = TimeSeriesGuard.Finite(y, nameof(y), 1);
            var n = a.Length;
            var m = b.Length;

            if ((double)n * m > 25e6)
            {
                throw new ArgumentException(
                    "x length times y length exceeds 25 million cells. Reduce the series first (for example with PiecewiseAggregateApproximation).",
                    nameof(y));
            }

            if (bandRadius < -1)
            {
                throw new ArgumentException($"bandRadius must be -1 (no band) or 0 or more but was {bandRadius}.",
                    nameof(bandRadius));
            }

            var radius = bandRadius < 0 ? int.MaxValue : Math.Max(bandRadius, Math.Abs(n - m));
            var d = new double[n + 1, m + 1];

            for (var i = 0; i <= n; i++)
            {
                for (var j = 0; j <= m; j++)
                {
                    d[i, j] = double.PositiveInfinity;
                }
            }

            d[0, 0] = 0;

            for (var i = 1; i <= n; i++)
            {
                var centre = (long)Math.Round((double)i * m / n);
                var lo = radius == int.MaxValue ? 1 : (int)Math.Max(1, centre - radius);
                var hi = radius == int.MaxValue ? m : (int)Math.Min(m, centre + radius);

                for (var j = lo; j <= hi; j++)
                {
                    var diff = a[i - 1] - b[j - 1];
                    var cost = squaredCost ? diff * diff : Math.Abs(diff);
                    d[i, j] = cost + Math.Min(d[i - 1, j - 1], Math.Min(d[i - 1, j], d[i, j - 1]));
                }
            }

            var path = new List<int[]>();
            var pi = n;
            var pj = m;

            while (pi > 0 && pj > 0)
            {
                path.Add(new[] { pi - 1, pj - 1 });
                var diagonal = d[pi - 1, pj - 1];
                var up = d[pi - 1, pj];
                var left = d[pi, pj - 1];

                if (diagonal <= up && diagonal <= left)
                {
                    pi--;
                    pj--;
                }
                else if (up <= left)
                {
                    pi--;
                }
                else
                {
                    pj--;
                }
            }

            path.Reverse();

            return new DtwResult(d[n, m], d[n, m] / path.Count, path.ToArray(), path.Count);
        }

        [AdvancedAnalyticsTool("SymbolSequenceEditDistance")]
        [Description(
            "Levenshtein edit distance between two ordered sequences of symbols: the smallest number of insertions, deletions and substitutions of single symbols needed to turn one sequence into the other. Use it to compare behavioural paths or event sequences (for example the ordered steps of two customer journeys). " +
            "Distance is the raw count; NormalisedDistance divides it by the length of the longer sequence (0 identical, 1 completely different) and Similarity is 1 minus that. Symbols are compared exactly. Either sequence may be empty (the distance is then the length of the other); both empty give distance 0 and similarity 1.")]
        public EditDistanceResult SymbolSequenceEditDistance(
            [Description("The first ordered sequence of symbols; may be empty.")]
            string[] first,
            [Description("The second ordered sequence of symbols; may be empty.")]
            string[] second)
        {
            var a = Symbols(first, nameof(first), 0);
            var b = Symbols(second, nameof(second), 0);
            var longest = Math.Max(a.Length, b.Length);

            if (longest == 0)
            {
                return new EditDistanceResult(0, 0, 1);
            }

            var distance = a.Length == 0 || b.Length == 0 ? longest : new Levenshtein<string>().Distance(a, b);

            return new EditDistanceResult(distance, distance / longest, 1 - distance / longest);
        }
    }
}