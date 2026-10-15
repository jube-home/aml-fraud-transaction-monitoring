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
using System.Numerics;
using Accord;
using Accord.Math;
using Accord.Math.Comparers;
using Accord.Math.Optimization;
using Accord.Math.Random;
using Accord.Math.Transforms;
using Accord.Math.Wavelets;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public enum NumericsCountKind
    {
        Permutations,
        PartialPermutations,
        Combinations,
        CombinationsWithRepetition,
        Sequences
    }

    public enum NumericsSamplingDistribution
    {
        Uniform,
        Normal,
        Exponential
    }

    public enum NumericsTransformDirection
    {
        Forward,
        Inverse
    }

    public sealed record NumericsEnumerationResult
    {
        public double[][] Items { get; init; } = Array.Empty<double[]>();
        public double TotalCount { get; init; }
        public int Returned { get; init; }
        public bool Truncated { get; init; }
    }

    public sealed record NumericsIntegerEnumerationResult
    {
        public int[][] Items { get; init; } = Array.Empty<int[]>();
        public double TotalCount { get; init; }
        public int Returned { get; init; }
        public bool Truncated { get; init; }
    }

    public sealed record NumericsCountResult
    {
        public double Count { get; init; }
        public double NaturalLogOfCount { get; init; }
        public bool IsExact { get; init; }
    }

    public sealed record NumericsSortResult
    {
        public double[] Values { get; init; } = Array.Empty<double>();
        public int[] OriginalIndices { get; init; } = Array.Empty<int>();
    }

    public sealed record NumericsMonteCarloResult
    {
        public double Estimate { get; init; }
        public double StandardError { get; init; }
        public double SampleStandardDeviation { get; init; }
        public double ConfidenceLower95 { get; init; }
        public double ConfidenceUpper95 { get; init; }
        public int Samples { get; init; }
    }

    public sealed record NumericsNamedConstant
    {
        public string Name { get; init; } = string.Empty;
        public double Value { get; init; }
        public string Meaning { get; init; } = string.Empty;
    }

    public sealed record NumericsTransformResult
    {
        public double[] Real { get; init; } = Array.Empty<double>();
        public double[] Imaginary { get; init; } = Array.Empty<double>();
        public double[] Magnitude { get; init; } = Array.Empty<double>();
    }

    [AdvancedAnalyticsToolGroup("NumericsAndOptimisation")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class CombinatoricsAndUtilityTools
    {
        private const int MaximumEnumerated = 20000;

        private static void RequireCap(int maxResults)
        {
            NumericsGuard.RequireInRange(maxResults, nameof(maxResults), 1, MaximumEnumerated);
        }

        private static double[][] Take(IEnumerable<double[]> source, int maxResults, out bool truncated)
        {
            var items = new List<double[]>();
            truncated = false;

            foreach (var item in source)
            {
                if (items.Count >= maxResults)
                {
                    truncated = true;
                    break;
                }

                items.Add((double[])item.Clone());
            }

            return items.ToArray();
        }

        [AdvancedAnalyticsTool("EnumeratePermutations")]
        [Description(
            "Lists the orderings (permutations) of a set of up to 10 distinct positions holding the supplied values, at most 'maxResults' of them (each ordering is one row). n items have n factorial orderings, so TotalCount reports the full number and Truncated says whether the list was cut short. " +
            "Equal values are still treated as different positions, so duplicates repeat rows. Use it to explore small assignment or sequencing problems exhaustively.")]
        public NumericsEnumerationResult EnumeratePermutations(
            [Description("The items to arrange, at most 10 numbers.")]
            double[] values,
            [Description("Maximum number of permutations returned, from 1 to 20000.")]
            int maxResults = 1000)
        {
            NumericsGuard.RequireVector(values, nameof(values), 1, 10);
            RequireCap(maxResults);
            var items = Take(Combinatorics.Permutations(values), maxResults, out var truncated);

            return new NumericsEnumerationResult
            {
                Items = items,
                TotalCount = Special.Factorial(values.Length),
                Returned = items.Length,
                Truncated = truncated
            };
        }

        [AdvancedAnalyticsTool("EnumerateCombinations")]
        [Description(
            "Lists every way to choose k of the supplied values without regard to order (each selection is one row), at most 'maxResults' of them. There are n choose k of them, reported in TotalCount, and Truncated says whether the list was cut short. " +
            "Use it to enumerate feature subsets or small portfolios. At most 25 values.")]
        public NumericsEnumerationResult EnumerateCombinations(
            [Description("The items to choose from, at most 25 numbers.")]
            double[] values,
            [Description("How many items to choose, from 1 to the number of values.")]
            int k,
            [Description("Maximum number of combinations returned, from 1 to 20000.")]
            int maxResults = 1000)
        {
            NumericsGuard.RequireVector(values, nameof(values), 1, 25);
            NumericsGuard.RequireInRange(k, nameof(k), 1, values.Length);
            RequireCap(maxResults);
            var items = Take(values.Combinations(k), maxResults, out var truncated);

            return new NumericsEnumerationResult
            {
                Items = items,
                TotalCount = Special.Binomial(values.Length, k),
                Returned = items.Length,
                Truncated = truncated
            };
        }

        [AdvancedAnalyticsTool("EnumerateSubsets")]
        [Description(
            "Lists every non-empty subset of the supplied values, including the full set (each subset is one row of varying length), at most 'maxResults' of them. A set of n items has 2 to the power n minus 1 non-empty subsets, reported in TotalCount; Truncated says whether the list was cut short. " +
            "At most 20 values.")]
        public NumericsEnumerationResult EnumerateSubsets(
            [Description("The items whose subsets are wanted, at most 20 numbers.")]
            double[] values,
            [Description("Maximum number of subsets returned, from 1 to 20000.")]
            int maxResults = 1000)
        {
            NumericsGuard.RequireVector(values, nameof(values), 1, 20);
            RequireCap(maxResults);
            var all = values.Combinations();
            var items = Take(all, maxResults, out var truncated);

            return new NumericsEnumerationResult
            {
                Items = items,
                TotalCount = Math.Pow(2, values.Length) - 1,
                Returned = items.Length,
                Truncated = truncated
            };
        }

        [AdvancedAnalyticsTool("EnumerateSequences")]
        [Description(
            "Lists every sequence of a given length over an alphabet of 'symbols' symbols numbered 0 to symbols-1 (each sequence is one row), at most 'maxResults' of them; this is the Cartesian power with symbols to the power length entries, reported in TotalCount. " +
            "Use it for exhaustive grids of categorical settings. Symbols 2 to 20 and length 1 to 12.")]
        public NumericsIntegerEnumerationResult EnumerateSequences(
            [Description("Number of distinct symbols, from 2 to 20.")]
            int symbols,
            [Description("Sequence length, from 1 to 12.")]
            int length,
            [Description("Maximum number of sequences returned, from 1 to 20000.")]
            int maxResults = 1000)
        {
            NumericsGuard.RequireInRange(symbols, nameof(symbols), 2, 20);
            NumericsGuard.RequireInRange(length, nameof(length), 1, 12);
            RequireCap(maxResults);
            var items = new List<int[]>();
            var truncated = false;

            foreach (var sequence in Combinatorics.Sequences(symbols, length))
            {
                if (items.Count >= maxResults)
                {
                    truncated = true;
                    break;
                }

                items.Add((int[])sequence.Clone());
            }

            return new NumericsIntegerEnumerationResult
            {
                Items = items.ToArray(),
                TotalCount = Math.Pow(symbols, length),
                Returned = items.Count,
                Truncated = truncated
            };
        }

        [AdvancedAnalyticsTool("GenerateTruthTable")]
        [Description(
            "Returns the truth table of the given number of boolean inputs: one row per combination of 0 and 1 values, 2 to the power inputs rows in all, in counting order. Use it to enumerate every on/off combination of rule conditions. " +
            "Inputs are limited to 1 through 14.")]
        public NumericsIntegerEnumerationResult GenerateTruthTable(
            [Description("Number of boolean inputs, from 1 to 14.")]
            int inputs)
        {
            NumericsGuard.RequireInRange(inputs, nameof(inputs), 1, 14);
            var table = Combinatorics.TruthTable(inputs);

            return new NumericsIntegerEnumerationResult
            {
                Items = table,
                TotalCount = table.Length,
                Returned = table.Length,
                Truncated = false
            };
        }

        [AdvancedAnalyticsTool("CountCombinatorialObjects")]
        [Description(
            "Counts arrangements without listing them, for any size: Permutations is n!, PartialPermutations is n!/(n-k)! (ordered choices of k from n), Combinations is n choose k, CombinationsWithRepetition is (n+k-1) choose k (unordered choices with repeats allowed), Sequences is n to the power k. " +
            "Count is a floating point number (infinity when it overflows), NaturalLogOfCount stays finite for huge answers, and IsExact says whether the count is below about 10 trillion so every digit is exact (larger counts are accurate to roughly 12 significant digits). n and k are limited to 1000000.")]
        public NumericsCountResult CountCombinatorialObjects(
            [Description("Which kind of arrangement to count.")]
            NumericsCountKind kind,
            [Description("Size of the set, from 0 to 1000000.")]
            int n,
            [Description("How many are chosen or the sequence length, from 0 to 1000000; ignored for Permutations.")]
            int k = 0)
        {
            NumericsGuard.RequireInRange(n, nameof(n), 0, 1000000);
            NumericsGuard.RequireInRange(k, nameof(k), 0, 1000000);
            double log;

            switch (kind)
            {
                case NumericsCountKind.Permutations:
                    log = Special.LogFactorial(n);
                    break;
                case NumericsCountKind.PartialPermutations:
                    if (k > n)
                    {
                        return new NumericsCountResult
                            { Count = 0, NaturalLogOfCount = double.NegativeInfinity, IsExact = true };
                    }

                    log = Special.LogFactorial(n) - Special.LogFactorial(n - k);
                    break;
                case NumericsCountKind.Combinations:
                    if (k > n)
                    {
                        return new NumericsCountResult
                            { Count = 0, NaturalLogOfCount = double.NegativeInfinity, IsExact = true };
                    }

                    log = Special.LogBinomial(n, k);
                    break;
                case NumericsCountKind.CombinationsWithRepetition:
                    if (n == 0)
                    {
                        return k == 0
                            ? new NumericsCountResult { Count = 1, NaturalLogOfCount = 0, IsExact = true }
                            : new NumericsCountResult
                                { Count = 0, NaturalLogOfCount = double.NegativeInfinity, IsExact = true };
                    }

                    log = Special.LogBinomial(n + k - 1, k);
                    break;
                default:
                    log = n == 0 ? k == 0 ? 0 : double.NegativeInfinity : k * Math.Log(n);
                    break;
            }

            var count = Math.Exp(log);
            var exact = false;

            if (log < 30.0)
            {
                count = Math.Round(count);
                exact = true;
            }

            return new NumericsCountResult { Count = count, NaturalLogOfCount = log, IsExact = exact };
        }

        [AdvancedAnalyticsTool("GreatestCommonDivisorAndLeastCommonMultiple")]
        [Description(
            "Returns the greatest common divisor and the least common multiple of each pair of whole numbers taken from two equal-length arrays: Values holds all the divisors first, then all the multiples. " +
            "Use it to find common cycles or to simplify ratios. Numbers are limited to absolute values up to 1000000000 and zero is allowed (gcd(0, x) is |x|; lcm with zero is 0). Up to 100000 pairs.")]
        public NumericsVectorResult GreatestCommonDivisorAndLeastCommonMultiple(
            [Description("First whole numbers of each pair.")]
            int[] a,
            [Description("Second whole numbers of each pair, same length as a.")]
            int[] b)
        {
            if (a == null || a.Length == 0 || a.Length > 100000)
            {
                throw new ArgumentException("'a' must contain between 1 and 100000 whole numbers.", nameof(a));
            }

            if (b == null || b.Length != a.Length)
            {
                throw new ArgumentException("'b' must have the same length as 'a'.", nameof(b));
            }

            var gcds = new double[a.Length];
            var lcms = new double[a.Length];

            for (var i = 0; i < a.Length; i++)
            {
                if (Math.Abs((long)a[i]) > 1000000000 || Math.Abs((long)b[i]) > 1000000000)
                {
                    throw new ArgumentException($"Pair {i} has a value beyond +/-1000000000; use smaller numbers.",
                        nameof(a));
                }

                var g = Tools.GreatestCommonDivisor(Math.Abs(a[i]), Math.Abs(b[i]));
                gcds[i] = g;
                lcms[i] = g == 0 ? 0 : Math.Abs((double)a[i] / g * b[i]);
            }

            return NumericsGuard.ToVectorResult(gcds.Concat(lcms).ToArray());
        }

        [AdvancedAnalyticsTool("EuclideanModulo")]
        [Description(
            "Computes the mathematical modulo x mod m for each x, which is always between 0 and m (never negative for a positive modulus), unlike the remainder operator of many languages. Use it to wrap angles, hours or cyclic indices. " +
            "The modulus m must be greater than 0. Works on fractional numbers too. Maximum 200000 values.")]
        public NumericsVectorResult EuclideanModulo(
            [Description("Numbers to wrap.")] double[] x,
            [Description("Positive modulus m, for example 24 for hours or 360 for degrees.")]
            double modulus)
        {
            NumericsGuard.RequireVector(x, nameof(x));
            NumericsGuard.RequirePositive(modulus, nameof(modulus));

            return NumericsGuard.ToVectorResult(x.Select(v => Tools.Mod(v, modulus)).ToArray());
        }

        [AdvancedAnalyticsTool("PowerOfTwoProperties")]
        [Description(
            "For each positive whole number reports the next power of two at or above it, the previous power of two at or below it, its base-2 logarithm rounded up (the number of bits needed) and whether it is itself a power of two; Values holds four arrays back to back in that order. " +
            "Use it to size FFT buffers or hash tables. Numbers must be between 1 and 1073741823. Up to 100000 numbers.")]
        public NumericsVectorResult PowerOfTwoProperties(
            [Description("Positive whole numbers, each from 1 to 1073741823.")]
            int[] values)
        {
            if (values == null || values.Length == 0 || values.Length > 100000)
            {
                throw new ArgumentException("'values' must contain between 1 and 100000 whole numbers.",
                    nameof(values));
            }

            foreach (var value in values)
            {
                NumericsGuard.RequireInRange(value, nameof(values), 1, 1073741823);
            }

            return NumericsGuard.ToVectorResult(
                values.Select(v => (double)Tools.NextPowerOf2(v))
                    .Concat(values.Select(v => (double)Tools.PreviousPowerOf2(v)))
                    .Concat(values.Select(v => (double)Tools.Log2(v)))
                    .Concat(values.Select(v => Tools.IsPowerOf2(v) ? 1.0 : 0.0))
                    .ToArray());
        }

        [AdvancedAnalyticsTool("FindIntegerByBinarySearch")]
        [Description(
            "Finds the whole number between 'lower' and 'upper' (upper excluded) at which a monotone built-in function is closest to a target value, by binary search; it assumes the function only rises or only falls over the range. " +
            "Use it to ask 'how many of X are needed before Y exceeds Z'. Solution is the integer and FunctionValue the function there. Limits are +/-1000000000.")]
        public NumericsRootResult FindIntegerByBinarySearch(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Smallest integer considered.")]
            int lower,
            [Description("One past the largest integer considered; must exceed lower.")]
            int upper,
            [Description("Target value of the function.")]
            double target)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            NumericsGuard.RequireInRange(lower, nameof(lower), -1000000000, 1000000000);
            NumericsGuard.RequireInRange(upper, nameof(upper), -1000000000, 1000000000);
            NumericsGuard.RequireFinite(target, nameof(target));

            if (upper <= lower)
            {
                throw new ArgumentException("'upper' must be greater than 'lower'.", nameof(upper));
            }

            var search = new BinarySearch(i => f(i), lower, upper);
            var solution = search.Find(target);

            return new NumericsRootResult
            {
                Solution = solution,
                FunctionValue = f(solution),
                Converged = true,
                Status = "Completed"
            };
        }

        [AdvancedAnalyticsTool("SortValuesWithOriginalIndices")]
        [Description(
            "Sorts numbers ascending or descending using a stable sort (equal values keep their original order) and returns the sorted values together with the original position of each, so results can be mapped back. " +
            "NaN values sort as the smallest. Maximum 200000 values.")]
        public NumericsSortResult SortValuesWithOriginalIndices(
            [Description("Numbers to sort.")] double[] values,
            [Description("True sorts from largest to smallest.")]
            bool descending = false)
        {
            NumericsGuard.RequireVector(values, nameof(values), finite: false);
            var copy = (double[])values.Clone();

            copy.Sort(out var order, true,
                descending ? ComparerDirection.Descending : ComparerDirection.Ascending);

            return new NumericsSortResult { Values = copy, OriginalIndices = order };
        }

        [AdvancedAnalyticsTool("SortValuesByKeys")]
        [Description(
            "Reorders one array of numbers following the ascending sort order of a parallel array of keys, for example ordering amounts by their timestamps. The keys must be the same length as the values. " +
            "Values with equal keys keep their relative order. Maximum 200000 pairs.")]
        public NumericsVectorResult SortValuesByKeys(
            [Description("Sort keys.")] double[] keys,
            [Description("Values to reorder along with the keys, same length as keys.")]
            double[] values)
        {
            NumericsGuard.RequireVector(keys, nameof(keys));
            NumericsGuard.RequireVector(values, nameof(values), keys.Length, keys.Length);
            var order = Enumerable.Range(0, keys.Length).OrderBy(i => keys[i]).ThenBy(i => i).ToArray();

            return NumericsGuard.ToVectorResult(order.Select(i => values[i]).ToArray());
        }

        [AdvancedAnalyticsTool("SelectNthSmallestValue")]
        [Description(
            "Returns the n-th smallest value (n counted from 0, so 0 is the minimum) or n-th largest when descending, by quickselect, which is faster than sorting the whole array. Use it for medians and other order statistics of large arrays. " +
            "n must be less than the number of values. Maximum 200000 values.")]
        public double SelectNthSmallestValue(
            [Description("Numbers to search.")] double[] values,
            [Description("Zero-based rank to select, from 0 to count minus 1.")]
            int n,
            [Description("True counts from the largest instead.")]
            bool descending = false)
        {
            NumericsGuard.RequireVector(values, nameof(values));
            NumericsGuard.RequireInRange(n, nameof(n), 0, values.Length - 1);

            return Sort.NthElement((double[])values.Clone(), n, !descending);
        }

        [AdvancedAnalyticsTool("SelectSmallestValuesSorted")]
        [Description(
            "Returns the 'count' smallest values (or largest when descending) already in sorted order, using a partial sort that avoids sorting the rest. Use it for top-N lists such as the ten largest transactions. " +
            "Count must not exceed the number of values. Maximum 200000 values.")]
        public NumericsVectorResult SelectSmallestValuesSorted(
            [Description("Numbers to search.")] double[] values,
            [Description("How many values to return, from 1 to the number of values.")]
            int count,
            [Description("True returns the largest values, largest first.")]
            bool descending = false)
        {
            NumericsGuard.RequireVector(values, nameof(values));
            NumericsGuard.RequireInRange(count, nameof(count), 1, values.Length);
            var copy = (double[])values.Clone();

            if (count == copy.Length)
            {
                Array.Sort(copy);

                if (descending)
                {
                    Array.Reverse(copy);
                }

                return NumericsGuard.ToVectorResult(copy);
            }

            Sort.Partial(copy, count, !descending);

            return NumericsGuard.ToVectorResult(copy.Take(count).ToArray());
        }

        [AdvancedAnalyticsTool("GenerateUniformRandomNumbers")]
        [Description(
            "Generates reproducible pseudo-random numbers uniformly distributed between a minimum and a maximum using the Ziggurat method, for simulation and test data. The same seed always gives the same numbers. " +
            "Up to 1000000 numbers; the maximum must exceed the minimum.")]
        public NumericsVectorResult GenerateUniformRandomNumbers(
            [Description("How many numbers to generate, from 1 to 1000000.")]
            int count,
            [Description("Lower end of the range.")]
            double minimum,
            [Description("Upper end of the range, greater than the minimum.")]
            double maximum,
            [Description("Seed making the sequence reproducible.")]
            int seed = 1)
        {
            NumericsGuard.RequireInRange(count, nameof(count), 1, 1000000);
            NumericsGuard.RequireFinite(minimum, nameof(minimum));
            NumericsGuard.RequireFinite(maximum, nameof(maximum));

            if (!(maximum > minimum))
            {
                throw new ArgumentException("'maximum' must be greater than 'minimum'.", nameof(maximum));
            }

            return NumericsGuard.ToVectorResult(new ZigguratUniformGenerator(minimum, maximum, seed)
                .Generate(count));
        }

        [AdvancedAnalyticsTool("GenerateNormalRandomNumbers")]
        [Description(
            "Generates reproducible pseudo-random numbers from a normal (Gaussian) distribution with the given mean and standard deviation using the Ziggurat method, for simulation and noise. The same seed always gives the same numbers. " +
            "Up to 1000000 numbers; the standard deviation must be positive.")]
        public NumericsVectorResult GenerateNormalRandomNumbers(
            [Description("How many numbers to generate, from 1 to 1000000.")]
            int count,
            [Description("Mean of the distribution.")]
            double mean = 0,
            [Description("Standard deviation, greater than 0.")]
            double standardDeviation = 1,
            [Description("Seed making the sequence reproducible.")]
            int seed = 1)
        {
            NumericsGuard.RequireInRange(count, nameof(count), 1, 1000000);
            NumericsGuard.RequireFinite(mean, nameof(mean));
            NumericsGuard.RequirePositive(standardDeviation, nameof(standardDeviation));
            var standard = new ZigguratNormalGenerator(seed).Generate(count);

            return NumericsGuard.ToVectorResult(standard.Select(z => mean + standardDeviation * z).ToArray());
        }

        [AdvancedAnalyticsTool("GenerateExponentialRandomNumbers")]
        [Description(
            "Generates reproducible pseudo-random numbers from an exponential distribution with the given rate (mean 1/rate) using the Ziggurat method, for waiting times and inter-arrival gaps. The same seed always gives the same numbers. " +
            "Up to 1000000 numbers; the rate must be positive.")]
        public NumericsVectorResult GenerateExponentialRandomNumbers(
            [Description("How many numbers to generate, from 1 to 1000000.")]
            int count,
            [Description("Rate lambda, greater than 0; the mean of the numbers is 1/rate.")]
            double rate = 1,
            [Description("Seed making the sequence reproducible.")]
            int seed = 1)
        {
            NumericsGuard.RequireInRange(count, nameof(count), 1, 1000000);
            NumericsGuard.RequirePositive(rate, nameof(rate));
            var standard = new ZigguratExponentialGenerator(seed).Generate(count);

            return NumericsGuard.ToVectorResult(standard.Select(z => z / rate).ToArray());
        }

        private static double[] Draw(NumericsSamplingDistribution distribution, double[]? distributionParameters,
            int samples, int seed)
        {
            var p = distributionParameters ?? Array.Empty<double>();

            switch (distribution)
            {
                case NumericsSamplingDistribution.Uniform:
                    NumericsGuard.RequireVector(p, nameof(distributionParameters), 2, 2);

                    if (!(p[1] > p[0]))
                    {
                        throw new ArgumentException("For Uniform give [minimum, maximum] with maximum above minimum.",
                            nameof(distributionParameters));
                    }

                    return new ZigguratUniformGenerator(p[0], p[1], seed).Generate(samples);
                case NumericsSamplingDistribution.Normal:
                    NumericsGuard.RequireVector(p, nameof(distributionParameters), 2, 2);
                    NumericsGuard.RequirePositive(p[1], nameof(distributionParameters));

                    return new ZigguratNormalGenerator(seed).Generate(samples)
                        .Select(z => p[0] + p[1] * z).ToArray();
                default:
                    NumericsGuard.RequireVector(p, nameof(distributionParameters), 1, 1);
                    NumericsGuard.RequirePositive(p[0], nameof(distributionParameters));

                    return new ZigguratExponentialGenerator(seed).Generate(samples).Select(z => z / p[0])
                        .ToArray();
            }
        }

        [AdvancedAnalyticsTool("MonteCarloExpectationOfNamedFunction")]
        [Description(
            "Estimates the expected value E[f(X)] of a built-in function of a random variable X by drawing seeded random samples from a Uniform, Normal or Exponential distribution and averaging f. " +
            "Estimate is the sample mean, StandardError its uncertainty (sample standard deviation over the square root of the sample count) and ConfidenceLower95 and ConfidenceUpper95 an approximate 95 percent interval. " +
            "Quadruple the samples to halve the error. distributionParameters: Uniform [minimum, maximum], Normal [mean, standard deviation], Exponential [rate]. Samples are limited to 5000000.")]
        public NumericsMonteCarloResult MonteCarloExpectationOfNamedFunction(
            [Description("Distribution of X: Uniform, Normal or Exponential.")]
            NumericsSamplingDistribution distribution,
            [Description("Uniform [minimum, maximum], Normal [mean, standard deviation], Exponential [rate].")]
            double[] distributionParameters,
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? functionParameters,
            [Description("Number of random samples, from 10 to 5000000.")]
            int samples = 100000,
            [Description("Seed making the estimate reproducible.")]
            int seed = 1)
        {
            var f = NumericsFunctions.Univariate(function, functionParameters, nameof(functionParameters));
            NumericsGuard.RequireInRange(samples, nameof(samples), 10, NumericsGuard.MaxSamples);
            var draws = Draw(distribution, distributionParameters, samples, seed);
            var sum = 0.0;
            var sumSquares = 0.0;

            foreach (var draw in draws)
            {
                var value = f(draw);
                sum += value;
                sumSquares += value * value;
            }

            var mean = sum / samples;
            var variance = Math.Max(0.0, (sumSquares - samples * mean * mean) / (samples - 1));
            var deviation = Math.Sqrt(variance);
            var standardError = deviation / Math.Sqrt(samples);

            return new NumericsMonteCarloResult
            {
                Estimate = mean,
                StandardError = standardError,
                SampleStandardDeviation = deviation,
                ConfidenceLower95 = mean - 1.959963984540054 * standardError,
                ConfidenceUpper95 = mean + 1.959963984540054 * standardError,
                Samples = samples
            };
        }

        [AdvancedAnalyticsTool("MonteCarloProbabilityOfNamedFunction")]
        [Description(
            "Estimates the probability that a built-in function of a random variable X falls inside a range, P(lower <= f(X) <= upper), by drawing seeded random samples from a Uniform, Normal or Exponential distribution and counting the share inside the range. " +
            "Leave 'lower' or 'upper' empty for an open-ended range. Estimate is the observed proportion, StandardError its binomial uncertainty sqrt(p(1-p)/n) and the confidence bounds are clipped to 0 and 1; rare events (probabilities below 1e-4) need many samples. " +
            "distributionParameters: Uniform [minimum, maximum], Normal [mean, standard deviation], Exponential [rate]. Samples are limited to 5000000.")]
        public NumericsMonteCarloResult MonteCarloProbabilityOfNamedFunction(
            [Description("Distribution of X: Uniform, Normal or Exponential.")]
            NumericsSamplingDistribution distribution,
            [Description("Uniform [minimum, maximum], Normal [mean, standard deviation], Exponential [rate].")]
            double[] distributionParameters,
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? functionParameters,
            [Description("Lowest accepted value of f(X); leave empty for no lower limit.")]
            double? lower = null,
            [Description("Highest accepted value of f(X); leave empty for no upper limit.")]
            double? upper = null,
            [Description("Number of random samples, from 10 to 5000000.")]
            int samples = 100000,
            [Description("Seed making the estimate reproducible.")]
            int seed = 1)
        {
            var f = NumericsFunctions.Univariate(function, functionParameters, nameof(functionParameters));
            NumericsGuard.RequireInRange(samples, nameof(samples), 10, NumericsGuard.MaxSamples);

            if (lower.HasValue && upper.HasValue && lower.Value > upper.Value)
            {
                throw new ArgumentException("'lower' must not exceed 'upper'.", nameof(lower));
            }

            var draws = Draw(distribution, distributionParameters, samples, seed);
            var inside = 0;

            foreach (var draw in draws)
            {
                var value = f(draw);

                if ((!lower.HasValue || value >= lower.Value) && (!upper.HasValue || value <= upper.Value))
                {
                    inside++;
                }
            }

            var p = (double)inside / samples;
            var standardError = Math.Sqrt(p * (1 - p) / samples);

            return new NumericsMonteCarloResult
            {
                Estimate = p,
                StandardError = standardError,
                SampleStandardDeviation = Math.Sqrt(p * (1 - p)),
                ConfidenceLower95 = Math.Max(0.0, p - 1.959963984540054 * standardError),
                ConfidenceUpper95 = Math.Min(1.0, p + 1.959963984540054 * standardError),
                Samples = samples
            };
        }

        [AdvancedAnalyticsTool("GetMathematicalConstants")]
        [Description(
            "Returns a table of named mathematical and machine constants with their double precision values and meaning: pi, e, the Euler-Mascheroni constant, Catalan's constant, square roots and logs of common numbers used in normal densities, and the floating point epsilons and log range limits. " +
            "Use it instead of typing constants from memory. Every entry has Name, Value and Meaning; no inputs are needed.")]
        public NumericsNamedConstant[] GetMathematicalConstants()
        {
            return new[]
            {
                new NumericsNamedConstant
                    { Name = "Pi", Value = Math.PI, Meaning = "Ratio of a circle's circumference to its diameter" },
                new NumericsNamedConstant { Name = "E", Value = Math.E, Meaning = "Base of the natural logarithm" },
                new NumericsNamedConstant
                {
                    Name = "EulerGamma", Value = Constants.EulerGamma,
                    Meaning = "Euler-Mascheroni constant, limit of harmonic sum minus ln n; equals minus digamma(1)"
                },
                new NumericsNamedConstant
                {
                    Name = "Catalan", Value = Constants.Catalan,
                    Meaning = "Catalan's constant, alternating sum of 1 over odd squares"
                },
                new NumericsNamedConstant { Name = "Sqrt2", Value = Constants.Sqrt2, Meaning = "Square root of 2" },
                new NumericsNamedConstant
                {
                    Name = "Sqrt2OverTwo", Value = Constants.Sqrt2H,
                    Meaning = "Square root of 2 divided by 2, that is 1/sqrt(2)"
                },
                new NumericsNamedConstant
                {
                    Name = "Sqrt2Pi", Value = Constants.Sqrt2PI,
                    Meaning = "Square root of 2 pi, the normalising constant of the normal density"
                },
                new NumericsNamedConstant
                    { Name = "SqrtHalfPi", Value = Constants.SqrtHalfPI, Meaning = "Square root of pi over 2" },
                new NumericsNamedConstant { Name = "LogPi", Value = Constants.LogPI, Meaning = "Natural log of pi" },
                new NumericsNamedConstant { Name = "Log2", Value = Constants.Log2, Meaning = "Natural log of 2" },
                new NumericsNamedConstant { Name = "Log3", Value = Constants.Log3, Meaning = "Natural log of 3" },
                new NumericsNamedConstant
                    { Name = "LogSqrt2Pi", Value = Constants.LogSqrt2PI, Meaning = "Natural log of sqrt(2 pi)" },
                new NumericsNamedConstant
                    { Name = "Log2Pi", Value = Constants.Log2PI, Meaning = "Natural log of 2 pi" },
                new NumericsNamedConstant
                {
                    Name = "DoubleEpsilon", Value = Constants.DoubleEpsilon,
                    Meaning = "Machine precision of 64-bit floating point used as a comparison tolerance"
                },
                new NumericsNamedConstant
                {
                    Name = "SingleEpsilon", Value = Constants.SingleEpsilon,
                    Meaning = "Machine precision of 32-bit floating point"
                },
                new NumericsNamedConstant
                {
                    Name = "DoubleSmall", Value = Constants.DoubleSmall,
                    Meaning = "A tiny positive double used to avoid division by zero"
                },
                new NumericsNamedConstant
                {
                    Name = "LogMax", Value = Constants.LogMax,
                    Meaning = "Largest x for which exp(x) does not overflow a double"
                },
                new NumericsNamedConstant
                {
                    Name = "LogMin", Value = Constants.LogMin,
                    Meaning = "Smallest x for which exp(x) does not underflow to zero"
                }
            };
        }

        private static NumericsTransformResult ComplexResult(Complex[] data)
        {
            return new NumericsTransformResult
            {
                Real = data.Select(c => c.Real).ToArray(),
                Imaginary = data.Select(c => c.Imaginary).ToArray(),
                Magnitude = data.Select(c => c.Magnitude).ToArray()
            };
        }

        [AdvancedAnalyticsTool("NumericsFastFourierTransform")]
        [Description(
            "Computes the discrete Fourier transform of a real or complex sequence by the fast Fourier transform (any length, fastest for powers of two): Real and Imaginary are the coefficients and Magnitude their absolute values, where coefficient k measures the strength of the cycle that repeats k times over the sequence. " +
            "A Forward call of [1, 2, 3, 4] gives 10, -2+2i, -2, -2-2i. Inverse undoes Forward exactly (with the 1/n scaling), so passing back the Real and Imaginary parts recovers the original. Length is limited to 65536.")]
        public NumericsTransformResult NumericsFastFourierTransform(
            [Description("Real parts of the sequence.")]
            double[] real,
            [Description("Optional imaginary parts, same length as real; empty for a real signal.")]
            double[]? imaginary = null,
            [Description("Forward transforms a signal to frequencies; Inverse goes back.")]
            NumericsTransformDirection direction = NumericsTransformDirection.Forward)
        {
            NumericsGuard.RequireVector(real, nameof(real), 1, 65536);
            var imag = imaginary == null || imaginary.Length == 0 ? new double[real.Length] : imaginary;
            NumericsGuard.RequireVector(imag, nameof(imaginary), real.Length, real.Length);
            var data = real.Select((r, i) => new Complex(r, imag[i])).ToArray();

            FourierTransform2.FFT(data,
                direction == NumericsTransformDirection.Forward
                    ? FourierTransform.Direction.Forward
                    : FourierTransform.Direction.Backward);

            return ComplexResult(data);
        }

        [AdvancedAnalyticsTool("NumericsDiscreteCosineTransform")]
        [Description(
            "Computes the orthonormal discrete cosine transform (type II) of a real sequence, which packs smooth signals into the first few coefficients; it underlies image and audio compression. Inverse undoes Forward exactly. " +
            "Coefficient 0 is the scaled mean level. Length is limited to 20000.")]
        public NumericsVectorResult NumericsDiscreteCosineTransform(
            [Description("Real sequence.")] double[] values,
            [Description("Forward transforms; Inverse goes back.")]
            NumericsTransformDirection direction = NumericsTransformDirection.Forward)
        {
            NumericsGuard.RequireVector(values, nameof(values), 1, 20000);
            var copy = (double[])values.Clone();

            if (direction == NumericsTransformDirection.Forward)
            {
                CosineTransform.DCT(copy);
            }
            else
            {
                CosineTransform.IDCT(copy);
            }

            return NumericsGuard.ToVectorResult(copy);
        }

        [AdvancedAnalyticsTool("NumericsDiscreteSineTransform")]
        [Description(
            "Computes the discrete sine transform of a real sequence, useful for signals that vanish at the boundaries. Inverse undoes Forward. " +
            "Length is limited to 20000.")]
        public NumericsVectorResult NumericsDiscreteSineTransform(
            [Description("Real sequence.")] double[] values,
            [Description("Forward transforms; Inverse goes back.")]
            NumericsTransformDirection direction = NumericsTransformDirection.Forward)
        {
            NumericsGuard.RequireVector(values, nameof(values), 1, 20000);
            var copy = (double[])values.Clone();

            if (direction == NumericsTransformDirection.Forward)
            {
                SineTransform.DST(copy);
            }
            else
            {
                SineTransform.IDST(copy);
            }

            return NumericsGuard.ToVectorResult(copy);
        }

        [AdvancedAnalyticsTool("NumericsHartleyTransform")]
        [Description(
            "Computes the discrete Hartley transform of a real sequence, a real-valued cousin of the Fourier transform using cosine plus sine waves, scaled by 1/sqrt(n) so applying it twice returns the original signal. " +
            "Length is limited to 20000.")]
        public NumericsVectorResult NumericsHartleyTransform(
            [Description("Real sequence.")] double[] values)
        {
            NumericsGuard.RequireVector(values, nameof(values), 1, 20000);
            var copy = (double[])values.Clone();
            HartleyTransform.DHT(copy);

            return NumericsGuard.ToVectorResult(copy);
        }

        [AdvancedAnalyticsTool("NumericsHilbertTransform")]
        [Description(
            "Computes the discrete Hilbert transform of a real signal, which shifts every frequency component by 90 degrees; for example the Hilbert transform of a cosine is the matching sine. Together with the signal it forms the analytic signal whose magnitude is the envelope. " +
            "Length must be a power of two up to 65536.")]
        public NumericsVectorResult NumericsHilbertTransform(
            [Description("Real signal whose length is a power of two.")]
            double[] values)
        {
            NumericsGuard.RequireVector(values, nameof(values), 2, 65536);

            if (!Tools.IsPowerOf2(values.Length))
            {
                throw new ArgumentException(
                    $"The length must be a power of two but is {values.Length}; pad or trim the signal to 2, 4, 8, 16 and so on.",
                    nameof(values));
            }

            var analytic = values.Select(v => new Complex(v, 0)).ToArray();
#pragma warning disable CS0618
            HilbertTransform.FHT(analytic, FourierTransform.Direction.Forward);
#pragma warning restore CS0618

            return NumericsGuard.ToVectorResult(analytic.Select(c => c.Imaginary).ToArray());
        }

        [AdvancedAnalyticsTool("NumericsHaarWaveletTransform")]
        [Description(
            "Applies one full pass of the Haar wavelet transform to a signal: the first half of the output holds pairwise averages and the second half the half-differences (detail) at that scale. Inverse undoes Forward. " +
            "Repeat on the first half yourself for more levels. Length must be even and at most 65536.")]
        public NumericsVectorResult NumericsHaarWaveletTransform(
            [Description("Real signal of even length.")]
            double[] values,
            [Description("Forward decomposes; Inverse reconstructs.")]
            NumericsTransformDirection direction = NumericsTransformDirection.Forward)
        {
            NumericsGuard.RequireVector(values, nameof(values), 2, 65536);

            if (values.Length % 2 != 0)
            {
                throw new ArgumentException("The length must be even for a Haar transform.", nameof(values));
            }

            var copy = (double[])values.Clone();

            if (direction == NumericsTransformDirection.Forward)
            {
                Haar.FWT(copy);
            }
            else
            {
                Haar.IWT(copy);
            }

            return NumericsGuard.ToVectorResult(copy);
        }

        [AdvancedAnalyticsTool("NumericsCdf97WaveletTransform")]
        [Description(
            "Applies one pass of the Cohen-Daubechies-Feauveau 9/7 biorthogonal wavelet transform (the lossy image compression wavelet): smooth approximation coefficients first, then detail coefficients. Inverse undoes Forward. " +
            "The length must be even and at least 4, and at most 65536.")]
        public NumericsVectorResult NumericsCdf97WaveletTransform(
            [Description("Real signal of even length, at least 4.")]
            double[] values,
            [Description("Forward decomposes; Inverse reconstructs.")]
            NumericsTransformDirection direction = NumericsTransformDirection.Forward)
        {
            NumericsGuard.RequireVector(values, nameof(values), 4, 65536);

            if (values.Length % 2 != 0)
            {
                throw new ArgumentException("The length must be even for a CDF 9/7 transform.", nameof(values));
            }

            var copy = (double[])values.Clone();

            if (direction == NumericsTransformDirection.Forward)
            {
                CDF97.FWT97(copy);
            }
            else
            {
                CDF97.IWT97(copy);
            }

            return NumericsGuard.ToVectorResult(copy);
        }
    }
}