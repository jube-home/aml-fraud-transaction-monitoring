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
using Accord.Statistics;
using Accord.Statistics.Distributions.Univariate;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    public enum ResamplingStatistic
    {
        Mean = 0,
        Median = 1,
        StandardDeviation = 2,
        Variance = 3,
        Minimum = 4,
        Maximum = 5,
        Quantile = 6,
        TrimmedMean = 7,
        Skewness = 8,
        Kurtosis = 9
    }

    public enum BootstrapIntervalMethod
    {
        Percentile = 0,
        Basic = 1,
        Normal = 2,
        BiasCorrectedAccelerated = 3
    }

    public enum PermutationStatistic
    {
        DifferenceInMeans = 0,
        DifferenceInMedians = 1,
        DifferenceInStandardDeviations = 2
    }

    public enum TestAlternative
    {
        TwoSided = 0,
        Greater = 1,
        Less = 2
    }

    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ResamplingInferenceTools
    {
        private const int MaximumExactEnumeration = 20000;

        [AdvancedAnalyticsTool("BootstrapStatisticInterval")]
        [Description(
            "Estimates the sampling uncertainty of any of a set of statistics (mean, median, standard deviation, variance, minimum, maximum, a quantile, a trimmed mean, skewness or kurtosis) by resampling the data with replacement, and returns a confidence interval. " +
            "Use it when no closed-form standard error is trustworthy, for example the median or a 95th percentile of skewed amounts. " +
            "Interval methods: Percentile (quantiles of the resampled statistic), Basic (reflects them around the estimate), Normal (estimate minus bias, plus or minus z times the bootstrap standard error) and BiasCorrectedAccelerated (BCa: corrects for bias and skew, the most accurate for smooth statistics). " +
            "Returns the estimate on the real data, the bootstrap mean, bias (bootstrap mean minus estimate), standard error, the interval bounds, and for BCa the bias constant z0 and acceleration. Missing values are dropped and counted; a constant sample gives a zero-width interval. Reproducible from the seed.")]
        public BootstrapResult BootstrapStatisticInterval(
            [Description(
                "The sample, one number per observation. NaN or infinity marks a missing value and is dropped. At least 2 finite values are required.")]
            double[] values,
            [Description(
                "Which statistic to bootstrap: Mean, Median, StandardDeviation, Variance, Minimum, Maximum, Quantile, TrimmedMean, Skewness or Kurtosis. Default Mean.")]
            ResamplingStatistic statistic = ResamplingStatistic.Mean,
            [Description("Confidence level of the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Number of bootstrap resamples, between 100 and 200000. Default 2000.")]
            int resamples = 2000,
            [Description("Percentile, Basic, Normal or BiasCorrectedAccelerated. Default Percentile.")]
            BootstrapIntervalMethod method = BootstrapIntervalMethod.Percentile,
            [Description(
                "For the Quantile statistic: the probability, between 0 and 1, for example 0.95. Default 0.5.")]
            double probability = 0.5,
            [Description(
                "For the TrimmedMean statistic: the fraction cut from each end, between 0 and 0.5. Default 0.1.")]
            double trimFraction = 0.1,
            [Description("Seed that makes the resampling reproducible. Default 1.")]
            int seed = 1)
        {
            var finite = DataPreparationGuard.FiniteVector(values, nameof(values), 2);
            Validate(confidenceLevel, resamples, probability, trimFraction);

            double Stat(double[] x)
            {
                return Compute(x, statistic, probability, trimFraction);
            }

            var estimate = Stat(finite);
            var random = new Random(seed);
            var boot = new double[resamples];
            var buffer = new double[finite.Length];

            for (var b = 0; b < resamples; b++)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = finite[random.Next(finite.Length)];
                }

                boot[b] = Stat(buffer);
            }

            var interval = Interval(estimate, boot, confidenceLevel, method,
                () => LeaveOneOut(finite, Stat));

            return new BootstrapResult(estimate, interval.BootstrapMean, interval.BootstrapMean - estimate,
                interval.StandardError, interval.Lower, interval.Upper, confidenceLevel, method, resamples,
                finite.Length,
                values.Length - finite.Length, interval.Z0, interval.Acceleration);
        }

        [AdvancedAnalyticsTool("BootstrapTwoSampleDifference")]
        [Description(
            "Bootstraps the difference between the same statistic in two independent samples (first minus second) and returns a confidence interval, for example the difference in median transaction amount between two segments. " +
            "Each sample is resampled separately with replacement. If the interval excludes 0 the difference is unlikely to be chance at that confidence level. " +
            "Returns the observed difference, bootstrap standard error, bias and interval; Percentile, Basic and Normal methods are supported. Missing values are dropped and counted. Reproducible from the seed.")]
        public BootstrapResult BootstrapTwoSampleDifference(
            [Description(
                "The first sample. NaN or infinity marks a missing value and is dropped. At least 2 finite values are required.")]
            double[] first,
            [Description(
                "The second sample. NaN or infinity marks a missing value and is dropped. At least 2 finite values are required.")]
            double[] second,
            [Description(
                "Which statistic to compare: Mean, Median, StandardDeviation, Variance, Minimum, Maximum, Quantile, TrimmedMean, Skewness or Kurtosis. Default Mean.")]
            ResamplingStatistic statistic = ResamplingStatistic.Mean,
            [Description("Confidence level of the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Number of bootstrap resamples, between 100 and 200000. Default 2000.")]
            int resamples = 2000,
            [Description("Percentile, Basic or Normal. Default Percentile.")]
            BootstrapIntervalMethod method = BootstrapIntervalMethod.Percentile,
            [Description("For the Quantile statistic: the probability, between 0 and 1. Default 0.5.")]
            double probability = 0.5,
            [Description(
                "For the TrimmedMean statistic: the fraction cut from each end, between 0 and 0.5. Default 0.1.")]
            double trimFraction = 0.1,
            [Description("Seed that makes the resampling reproducible. Default 1.")]
            int seed = 1)
        {
            var a = DataPreparationGuard.FiniteVector(first, nameof(first), 2);
            var b = DataPreparationGuard.FiniteVector(second, nameof(second), 2);
            Validate(confidenceLevel, resamples, probability, trimFraction);

            if (method == BootstrapIntervalMethod.BiasCorrectedAccelerated)
            {
                throw new ArgumentException(
                    "BiasCorrectedAccelerated is available for a single sample only. Use Percentile, Basic or Normal.",
                    nameof(method));
            }

            double Stat(double[] x)
            {
                return Compute(x, statistic, probability, trimFraction);
            }

            var estimate = Stat(a) - Stat(b);
            var random = new Random(seed);
            var boot = new double[resamples];
            var bufferA = new double[a.Length];
            var bufferB = new double[b.Length];

            for (var r = 0; r < resamples; r++)
            {
                for (var i = 0; i < bufferA.Length; i++)
                {
                    bufferA[i] = a[random.Next(a.Length)];
                }

                for (var i = 0; i < bufferB.Length; i++)
                {
                    bufferB[i] = b[random.Next(b.Length)];
                }

                boot[r] = Stat(bufferA) - Stat(bufferB);
            }

            var interval = Interval(estimate, boot, confidenceLevel, method, () => Array.Empty<double>());

            return new BootstrapResult(estimate, interval.BootstrapMean, interval.BootstrapMean - estimate,
                interval.StandardError, interval.Lower, interval.Upper, confidenceLevel, method, resamples,
                a.Length + b.Length, first.Length + second.Length - a.Length - b.Length, double.NaN, double.NaN);
        }

        [AdvancedAnalyticsTool("BootstrapCorrelationInterval")]
        [Description(
            "Bootstraps the correlation between two paired variables: whole pairs are resampled with replacement, so the pairing is preserved. Returns the correlation on the real data and a confidence interval, which is more honest than a normal-theory interval for skewed data or small samples. " +
            "Pearson measures linear association, Spearman monotone association using ranks. Pairs where either value is missing are dropped and counted. Percentile, Basic, Normal and BiasCorrectedAccelerated methods are supported. A resample with no variation gives NaN and is skipped. Reproducible from the seed.")]
        public BootstrapResult BootstrapCorrelationInterval(
            [Description("The first variable, one number per pair. NaN or infinity marks a missing value.")]
            double[] x,
            [Description("The second variable, same length as the first.")]
            double[] y,
            [Description("Pearson or Spearman. Default Pearson.")]
            CorrelationKind kind = CorrelationKind.Pearson,
            [Description("Confidence level of the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Number of bootstrap resamples, between 100 and 200000. Default 2000.")]
            int resamples = 2000,
            [Description("Percentile, Basic, Normal or BiasCorrectedAccelerated. Default Percentile.")]
            BootstrapIntervalMethod method = BootstrapIntervalMethod.Percentile,
            [Description("Seed that makes the resampling reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Vector(x, nameof(x));
            DataPreparationGuard.Vector(y, nameof(y));
            DataPreparationGuard.SameLength(x.Length, y.Length, nameof(y), nameof(x));
            Validate(confidenceLevel, resamples, 0.5, 0.1);

            var idx = Enumerable.Range(0, x.Length)
                .Where(i => !DataPreparationGuard.IsMissing(x[i]) && !DataPreparationGuard.IsMissing(y[i])).ToArray();

            if (idx.Length < 3)
            {
                throw new ArgumentException($"Only {idx.Length} complete pairs; at least 3 are needed.", nameof(x));
            }

            var px = idx.Select(i => x[i]).ToArray();
            var py = idx.Select(i => y[i]).ToArray();
            var estimate = Correlate(px, py, kind);

            if (double.IsNaN(estimate))
            {
                throw new ArgumentException("One of the variables is constant, so its correlation is undefined.",
                    nameof(x));
            }

            var random = new Random(seed);
            var boot = new List<double>();
            var bx = new double[px.Length];
            var by = new double[px.Length];

            for (var r = 0; r < resamples; r++)
            {
                for (var i = 0; i < px.Length; i++)
                {
                    var k = random.Next(px.Length);
                    bx[i] = px[k];
                    by[i] = py[k];
                }

                var value = Correlate(bx, by, kind);

                if (!double.IsNaN(value))
                {
                    boot.Add(value);
                }
            }

            if (boot.Count < 10)
            {
                throw new ArgumentException(
                    "Almost every resample was degenerate; the sample is too small or too constant.",
                    nameof(x));
            }

            var interval = Interval(estimate, boot.ToArray(), confidenceLevel, method, () =>
                Enumerable.Range(0, px.Length).Select(leave => Correlate(
                        px.Where((_, i) => i != leave).ToArray(), py.Where((_, i) => i != leave).ToArray(), kind))
                    .ToArray());

            return new BootstrapResult(estimate, interval.BootstrapMean, interval.BootstrapMean - estimate,
                interval.StandardError, interval.Lower, interval.Upper, confidenceLevel, method, boot.Count, idx.Length,
                x.Length - idx.Length, interval.Z0, interval.Acceleration);
        }

        [AdvancedAnalyticsTool("JackknifeStatistic")]
        [Description(
            "Estimates the bias and standard error of a statistic by leaving out one observation at a time (the jackknife). It is deterministic and needs no random numbers. " +
            "Returns the estimate on all data, the n leave-one-out estimates (a large deviation of one shows an influential observation), the jackknife bias, the bias-corrected estimate, the standard error and a Student t confidence interval around the estimate. " +
            "For the mean the standard error equals s / sqrt(n) and the bias is 0. The jackknife works poorly for non-smooth statistics such as the median or the minimum: prefer BootstrapStatisticInterval for those. Missing values are dropped and counted.")]
        public JackknifeResult JackknifeStatistic(
            [Description(
                "The sample, one number per observation. NaN or infinity marks a missing value and is dropped. At least 3 finite values are required.")]
            double[] values,
            [Description(
                "Which statistic to jackknife: Mean, Median, StandardDeviation, Variance, Minimum, Maximum, Quantile, TrimmedMean, Skewness or Kurtosis. Default Mean.")]
            ResamplingStatistic statistic = ResamplingStatistic.Mean,
            [Description("Confidence level of the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("For the Quantile statistic: the probability, between 0 and 1. Default 0.5.")]
            double probability = 0.5,
            [Description(
                "For the TrimmedMean statistic: the fraction cut from each end, between 0 and 0.5. Default 0.1.")]
            double trimFraction = 0.1)
        {
            var finite = DataPreparationGuard.FiniteVector(values, nameof(values), 3);
            Validate(confidenceLevel, 100, probability, trimFraction);

            double Stat(double[] x)
            {
                return Compute(x, statistic, probability, trimFraction);
            }

            var estimate = Stat(finite);
            var loo = LeaveOneOut(finite, Stat);
            var n = finite.Length;
            var mean = loo.Average();
            var bias = (n - 1) * (mean - estimate);
            var standardError = Math.Sqrt((n - 1d) / n * loo.Sum(v => (v - mean) * (v - mean)));
            var t = new TDistribution(n - 1).InverseDistributionFunction(1 - (1 - confidenceLevel) / 2);

            return new JackknifeResult(estimate, loo, bias, estimate - bias, standardError,
                estimate - t * standardError,
                estimate + t * standardError, confidenceLevel, values.Length - n);
        }

        [AdvancedAnalyticsTool("PermutationTestTwoSample")]
        [Description(
            "Tests whether two samples differ in a statistic (difference in means, medians or standard deviations, first minus second) without distributional assumptions: group labels are shuffled many times and the observed difference is compared with the shuffled differences. " +
            "When the number of distinct ways to split the pooled data is at most 20000 the test is exact and enumerates them all; otherwise it is a Monte Carlo test and the p-value is (extreme + 1) / (permutations + 1), which is never zero. " +
            "Alternative: TwoSided, Greater (first larger) or Less. Returns the observed statistic, p-value, the permutations used, whether it was exact, and the mean, standard deviation and 2.5th / 97.5th percentiles of the null distribution. Missing values are dropped and counted. Reproducible from the seed.")]
        public PermutationTestResult PermutationTestTwoSample(
            [Description(
                "The first sample. NaN or infinity marks a missing value and is dropped. At least 2 finite values are required.")]
            double[] first,
            [Description(
                "The second sample. NaN or infinity marks a missing value and is dropped. At least 2 finite values are required.")]
            double[] second,
            [Description(
                "DifferenceInMeans, DifferenceInMedians or DifferenceInStandardDeviations. Default DifferenceInMeans.")]
            PermutationStatistic statistic = PermutationStatistic.DifferenceInMeans,
            [Description("TwoSided, Greater (first larger than second) or Less. Default TwoSided.")]
            TestAlternative alternative = TestAlternative.TwoSided,
            [Description(
                "Number of random shuffles when the test is not exact, between 100 and 1000000. Default 10000.")]
            int permutations = 10000,
            [Description("Seed that makes the shuffles reproducible. Default 1.")]
            int seed = 1)
        {
            var a = DataPreparationGuard.FiniteVector(first, nameof(first), 2);
            var b = DataPreparationGuard.FiniteVector(second, nameof(second), 2);

            if (permutations < 100 || permutations > 1000000)
            {
                throw new ArgumentException("permutations must be between 100 and 1000000.", nameof(permutations));
            }

            var pooled = a.Concat(b).ToArray();
            var n1 = a.Length;

            double Stat(double[] group1, double[] group2)
            {
                return statistic switch
                {
                    PermutationStatistic.DifferenceInMeans => group1.Mean() - group2.Mean(),
                    PermutationStatistic.DifferenceInMedians => group1.Median() - group2.Median(),
                    _ => group1.StandardDeviation() - group2.StandardDeviation()
                };
            }

            var observed = Stat(a, b);
            var nulls = new List<double>();
            var exact = BinomialAtMost(pooled.Length, n1, MaximumExactEnumeration);

            if (exact)
            {
                var chosen = new int[n1];

                EnumerateCombinations(pooled.Length, n1, 0, 0, chosen, () =>
                {
                    var set = new HashSet<int>(chosen);
                    var g1 = chosen.Select(i => pooled[i]).ToArray();

                    var g2 = Enumerable.Range(0, pooled.Length).Where(i => !set.Contains(i)).Select(i => pooled[i])
                        .ToArray();

                    nulls.Add(Stat(g1, g2));
                });
            }
            else
            {
                var random = new Random(seed);
                var order = DataPreparationGuard.Range(pooled.Length);

                for (var p = 0; p < permutations; p++)
                {
                    DataPreparationGuard.Shuffle(order, random);
                    var g1 = order.Take(n1).Select(i => pooled[i]).ToArray();
                    var g2 = order.Skip(n1).Select(i => pooled[i]).ToArray();
                    nulls.Add(Stat(g1, g2));
                }
            }

            return Summarise(observed, nulls, alternative, exact, first.Length + second.Length - pooled.Length);
        }

        [AdvancedAnalyticsTool("PermutationTestCorrelation")]
        [Description(
            "Tests whether two paired variables are associated without assuming normality: one variable is shuffled many times and the observed correlation is compared with the shuffled correlations. " +
            "Pearson tests linear association, Spearman monotone association. With 7 or fewer complete pairs all n! orderings are enumerated (exact); otherwise the p-value is Monte Carlo, (extreme + 1) / (permutations + 1). " +
            "Alternative: TwoSided, Greater (positive association) or Less (negative). Returns the observed correlation, p-value, permutations used, whether exact, and the null mean, standard deviation and 2.5th / 97.5th percentiles. Pairs with a missing value are dropped and counted; a constant variable is rejected. Reproducible from the seed.")]
        public PermutationTestResult PermutationTestCorrelation(
            [Description("The first variable, one number per pair. NaN or infinity marks a missing value.")]
            double[] x,
            [Description("The second variable, same length as the first.")]
            double[] y,
            [Description("Pearson or Spearman. Default Pearson.")]
            CorrelationKind kind = CorrelationKind.Pearson,
            [Description("TwoSided, Greater or Less. Default TwoSided.")]
            TestAlternative alternative = TestAlternative.TwoSided,
            [Description(
                "Number of random shuffles when the test is not exact, between 100 and 1000000. Default 10000.")]
            int permutations = 10000,
            [Description("Seed that makes the shuffles reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Vector(x, nameof(x));
            DataPreparationGuard.Vector(y, nameof(y));
            DataPreparationGuard.SameLength(x.Length, y.Length, nameof(y), nameof(x));

            if (permutations < 100 || permutations > 1000000)
            {
                throw new ArgumentException("permutations must be between 100 and 1000000.", nameof(permutations));
            }

            var idx = Enumerable.Range(0, x.Length)
                .Where(i => !DataPreparationGuard.IsMissing(x[i]) && !DataPreparationGuard.IsMissing(y[i])).ToArray();

            if (idx.Length < 3)
            {
                throw new ArgumentException($"Only {idx.Length} complete pairs; at least 3 are needed.", nameof(x));
            }

            var px = idx.Select(i => x[i]).ToArray();
            var py = idx.Select(i => y[i]).ToArray();
            var observed = Correlate(px, py, kind);

            if (double.IsNaN(observed))
            {
                throw new ArgumentException("One of the variables is constant, so its correlation is undefined.",
                    nameof(x));
            }

            var nulls = new List<double>();
            var exact = idx.Length <= 7;

            if (exact)
            {
                var order = DataPreparationGuard.Range(idx.Length);

                EnumeratePermutations(order, 0,
                    () => nulls.Add(Correlate(px, order.Select(k => py[k]).ToArray(), kind)));
            }
            else
            {
                var random = new Random(seed);
                var order = DataPreparationGuard.Range(idx.Length);

                for (var p = 0; p < permutations; p++)
                {
                    DataPreparationGuard.Shuffle(order, random);
                    nulls.Add(Correlate(px, order.Select(k => py[k]).ToArray(), kind));
                }
            }

            return Summarise(observed, nulls.Where(v => !double.IsNaN(v)).ToList(), alternative, exact,
                x.Length - idx.Length);
        }

        [AdvancedAnalyticsTool("PermutationTestPaired")]
        [Description(
            "Tests whether the average paired difference (first minus second, for example before and after a control change) is zero without assuming normality, by randomly flipping the sign of each difference, which is what the null hypothesis of no effect implies. " +
            "With 14 or fewer non-zero differences all 2^n sign patterns are enumerated (exact); otherwise the p-value is Monte Carlo, (extreme + 1) / (permutations + 1). " +
            "Alternative: TwoSided, Greater (first larger) or Less. Returns the observed mean difference, p-value, permutations used, whether exact, and the null mean, standard deviation and 2.5th / 97.5th percentiles. Pairs with a missing value are dropped and counted; zero differences carry no information and are ignored. Reproducible from the seed.")]
        public PermutationTestResult PermutationTestPaired(
            [Description("The first measurement of each pair. NaN or infinity marks a missing value.")]
            double[] first,
            [Description("The second measurement of each pair, same length as the first.")]
            double[] second,
            [Description("TwoSided, Greater (first larger than second) or Less. Default TwoSided.")]
            TestAlternative alternative = TestAlternative.TwoSided,
            [Description(
                "Number of random sign flips when the test is not exact, between 100 and 1000000. Default 10000.")]
            int permutations = 10000,
            [Description("Seed that makes the flips reproducible. Default 1.")]
            int seed = 1)
        {
            DataPreparationGuard.Vector(first, nameof(first));
            DataPreparationGuard.Vector(second, nameof(second));
            DataPreparationGuard.SameLength(first.Length, second.Length, nameof(second), nameof(first));

            if (permutations < 100 || permutations > 1000000)
            {
                throw new ArgumentException("permutations must be between 100 and 1000000.", nameof(permutations));
            }

            var idx = Enumerable.Range(0, first.Length)
                .Where(i => !DataPreparationGuard.IsMissing(first[i]) && !DataPreparationGuard.IsMissing(second[i]))
                .ToArray();

            var diffs = idx.Select(i => first[i] - second[i]).ToArray();
            var informative = diffs.Where(d => d != 0).ToArray();

            if (informative.Length < 2)
            {
                throw new ArgumentException(
                    "Fewer than 2 pairs differ, so there is nothing to test. Check that the two lists are not identical.",
                    nameof(first));
            }

            var observed = diffs.Average();
            var nulls = new List<double>();
            var exact = informative.Length <= 14;
            var total = diffs.Length;

            if (exact)
            {
                var patterns = 1 << informative.Length;

                for (var mask = 0; mask < patterns; mask++)
                {
                    double sum = 0;

                    for (var k = 0; k < informative.Length; k++)
                    {
                        sum += ((mask >> k) & 1) == 1 ? -informative[k] : informative[k];
                    }

                    nulls.Add(sum / total);
                }
            }
            else
            {
                var random = new Random(seed);

                for (var p = 0; p < permutations; p++)
                {
                    double sum = 0;

                    foreach (var d in informative)
                    {
                        sum += random.Next(2) == 1 ? -d : d;
                    }

                    nulls.Add(sum / total);
                }
            }

            return Summarise(observed, nulls, alternative, exact, first.Length - idx.Length);
        }

        private static void Validate(double confidenceLevel, int resamples, double probability, double trimFraction)
        {
            if (double.IsNaN(confidenceLevel) || confidenceLevel <= 0 || confidenceLevel >= 1)
            {
                throw new ArgumentException("confidenceLevel must be strictly between 0 and 1, for example 0.95.",
                    nameof(confidenceLevel));
            }

            if (resamples < 100 || resamples > 200000)
            {
                throw new ArgumentException("resamples must be between 100 and 200000.", nameof(resamples));
            }

            if (double.IsNaN(probability) || probability < 0 || probability > 1)
            {
                throw new ArgumentException("probability must be between 0 and 1.", nameof(probability));
            }

            if (double.IsNaN(trimFraction) || trimFraction < 0 || trimFraction >= 0.5)
            {
                throw new ArgumentException("trimFraction must be at least 0 and below 0.5.", nameof(trimFraction));
            }
        }

        private static double Compute(double[] values, ResamplingStatistic statistic, double probability, double trim)
        {
            return statistic switch
            {
                ResamplingStatistic.Mean => values.Mean(),
                ResamplingStatistic.Median => values.Median(),
                ResamplingStatistic.StandardDeviation => values.StandardDeviation(),
                ResamplingStatistic.Variance => values.Variance(),
                ResamplingStatistic.Minimum => values.Min(),
                ResamplingStatistic.Maximum => values.Max(),
                ResamplingStatistic.Quantile => values.Quantile(probability, false, QuantileMethod.R),
                ResamplingStatistic.TrimmedMean => values.TruncatedMean(trim),
                ResamplingStatistic.Skewness => values.Skewness(),
                _ => values.Kurtosis()
            };
        }

        private static double[] LeaveOneOut(double[] values, Func<double[], double> statistic)
        {
            return Enumerable.Range(0, values.Length)
                .Select(leave => statistic(values.Where((_, i) => i != leave).ToArray())).ToArray();
        }

        private static double Correlate(double[] x, double[] y, CorrelationKind kind)
        {
            if (x.Length < 3)
            {
                return double.NaN;
            }

            var a = kind == CorrelationKind.Spearman ? Ranks(x) : x;
            var b = kind == CorrelationKind.Spearman ? Ranks(y) : y;

            if (a.Distinct().Count() < 2 || b.Distinct().Count() < 2)
            {
                return double.NaN;
            }

            var ma = a.Average();
            var mb = b.Average();
            double sab = 0;
            double saa = 0;
            double sbb = 0;

            for (var i = 0; i < a.Length; i++)
            {
                sab += (a[i] - ma) * (b[i] - mb);
                saa += (a[i] - ma) * (a[i] - ma);
                sbb += (b[i] - mb) * (b[i] - mb);
            }

            return sab / Math.Sqrt(saa * sbb);
        }

        private static double[] Ranks(double[] values)
        {
            var order = Enumerable.Range(0, values.Length).OrderBy(i => values[i]).ToArray();
            var ranks = new double[values.Length];
            var start = 0;

            while (start < order.Length)
            {
                var end = start;

                while (end + 1 < order.Length && values[order[end + 1]] == values[order[start]])
                {
                    end++;
                }

                for (var k = start; k <= end; k++)
                {
                    ranks[order[k]] = (start + end) / 2d + 1;
                }

                start = end + 1;
            }

            return ranks;
        }

        private static IntervalSummary Interval(double estimate, double[] boot, double level,
            BootstrapIntervalMethod method, Func<double[]> jackknife)
        {
            var sorted = boot.OrderBy(v => v).ToArray();
            var mean = boot.Average();

            var standardError = boot.Length > 1
                ? Math.Sqrt(boot.Sum(v => (v - mean) * (v - mean)) / (boot.Length - 1))
                : 0;

            var alpha = 1 - level;
            var z0 = double.NaN;
            var acceleration = double.NaN;
            double lower;
            double upper;

            switch (method)
            {
                case BootstrapIntervalMethod.Basic:
                    lower = 2 * estimate - DataPreparationGuard.Quantile(sorted, 1 - alpha / 2);
                    upper = 2 * estimate - DataPreparationGuard.Quantile(sorted, alpha / 2);
                    break;
                case BootstrapIntervalMethod.Normal:
                    var z = Normal.Inverse(1 - alpha / 2);
                    var corrected = estimate - (mean - estimate);
                    lower = corrected - z * standardError;
                    upper = corrected + z * standardError;
                    break;
                case BootstrapIntervalMethod.BiasCorrectedAccelerated:
                    var below = boot.Count(v => v < estimate) + 0.5 * boot.Count(v => v == estimate);
                    var fraction = Math.Min(Math.Max(below / boot.Length, 0.5 / boot.Length), 1 - 0.5 / boot.Length);
                    z0 = Normal.Inverse(fraction);
                    var loo = jackknife();
                    var looMean = loo.Average();
                    var numerator = loo.Sum(v => Math.Pow(looMean - v, 3));
                    var denominator = 6 * Math.Pow(loo.Sum(v => (looMean - v) * (looMean - v)), 1.5);
                    acceleration = denominator > 0 ? numerator / denominator : 0;

                    double Adjust(double p)
                    {
                        var zp = Normal.Inverse(p);

                        return Normal.Function(z0 + (z0 + zp) / (1 - acceleration * (z0 + zp)));
                    }

                    lower = DataPreparationGuard.Quantile(sorted, Math.Min(Math.Max(Adjust(alpha / 2), 0), 1));
                    upper = DataPreparationGuard.Quantile(sorted, Math.Min(Math.Max(Adjust(1 - alpha / 2), 0), 1));
                    break;
                default:
                    lower = DataPreparationGuard.Quantile(sorted, alpha / 2);
                    upper = DataPreparationGuard.Quantile(sorted, 1 - alpha / 2);
                    break;
            }

            return new IntervalSummary(mean, standardError, lower, upper, z0, acceleration);
        }

        private static PermutationTestResult Summarise(double observed, List<double> nulls, TestAlternative alternative,
            bool exact, int values)
        {
            var tolerance = 1e-9 * Math.Max(1, Math.Abs(observed));

            var extreme = alternative switch
            {
                TestAlternative.Greater => nulls.Count(v => v >= observed - tolerance),
                TestAlternative.Less => nulls.Count(v => v <= observed + tolerance),
                _ => nulls.Count(v => Math.Abs(v) >= Math.Abs(observed) - tolerance)
            };

            var p = exact ? extreme / (double)nulls.Count : (extreme + 1d) / (nulls.Count + 1d);
            var sorted = nulls.OrderBy(v => v).ToArray();
            var mean = nulls.Average();
            var sd = nulls.Count > 1 ? Math.Sqrt(nulls.Sum(v => (v - mean) * (v - mean)) / (nulls.Count - 1)) : 0;

            return new PermutationTestResult(observed, p, nulls.Count, exact, mean, sd,
                DataPreparationGuard.Quantile(sorted, 0.025), DataPreparationGuard.Quantile(sorted, 0.975), values);
        }

        private static bool BinomialAtMost(int n, int k, int limit)
        {
            double value = 1;
            var m = Math.Min(k, n - k);

            for (var i = 1; i <= m; i++)
            {
                value = value * (n - m + i) / i;

                if (value > limit)
                {
                    return false;
                }
            }

            return true;
        }

        private static void EnumerateCombinations(int n, int k, int start, int depth, int[] chosen, Action visit)
        {
            if (depth == k)
            {
                visit();

                return;
            }

            for (var i = start; i <= n - (k - depth); i++)
            {
                chosen[depth] = i;
                EnumerateCombinations(n, k, i + 1, depth + 1, chosen, visit);
            }
        }

        private static void EnumeratePermutations(int[] order, int index, Action visit)
        {
            if (index == order.Length)
            {
                visit();

                return;
            }

            for (var i = index; i < order.Length; i++)
            {
                (order[index], order[i]) = (order[i], order[index]);
                EnumeratePermutations(order, index + 1, visit);
                (order[index], order[i]) = (order[i], order[index]);
            }
        }

        private sealed record IntervalSummary(
            double BootstrapMean,
            double StandardError,
            double Lower,
            double Upper,
            double Z0,
            double Acceleration);
    }

    public sealed record BootstrapResult(
        double Estimate,
        double BootstrapMean,
        double Bias,
        double StandardError,
        double Lower,
        double Upper,
        double ConfidenceLevel,
        BootstrapIntervalMethod Method,
        int Resamples,
        int SampleSize,
        int MissingDropped,
        double BiasConstantZ0,
        double Acceleration);

    public sealed record JackknifeResult(
        double Estimate,
        double[] LeaveOneOutEstimates,
        double Bias,
        double BiasCorrectedEstimate,
        double StandardError,
        double Lower,
        double Upper,
        double ConfidenceLevel,
        int MissingDropped);

    public sealed record PermutationTestResult(
        double Observed,
        double PValue,
        int Permutations,
        bool Exact,
        double NullMean,
        double NullStandardDeviation,
        double NullLower95,
        double NullUpper95,
        int MissingDropped);
}