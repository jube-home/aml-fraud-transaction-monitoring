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
using Accord.Statistics;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Testing;
using Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models;
using static Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.HypothesisTestSupport;
using LibraryBartlettTest = Accord.Statistics.Testing.BartlettTest;
using LibraryLeveneTest = Accord.Statistics.Testing.LeveneTest;
using LibraryPairedTTest = Accord.Statistics.Testing.PairedTTest;
using LibraryTwoSampleTTest = Accord.Statistics.Testing.TwoSampleTTest;
using LibraryTwoSampleZTest = Accord.Statistics.Testing.TwoSampleZTest;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests
{
    [AdvancedAnalyticsToolGroup("HypothesisTests")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class MeanAndVarianceTestTools
    {
        [AdvancedAnalyticsTool("OneSampleTTest")]
        [Description(
            "Tests whether the mean of one numeric sample differs from, exceeds or falls below a stated value using Student's t distribution. " +
            "Use it for small or moderate samples when the population standard deviation is unknown and the data are roughly bell shaped. " +
            "Statistic is the t value, DegreesOfFreedom is n-1, EstimatedValue is the sample mean, EffectSize is Cohen's d (mean minus hypothesised value divided by the sample standard deviation) and ConfidenceLower/ConfidenceUpper are the two-sided interval for the mean at 1-alpha. " +
            "RejectNull is true when the p-value is below alpha. Fewer than 2 values, NaN or identical values are rejected with an explanatory error.")]
        public HypothesisTestResult OneSampleTTest(
            [Description("The observed numeric values, at least 2, with no NaN or infinity.")]
            double[] sample,
            [Description("The mean stated by the null hypothesis. Default 0.")]
            double hypothesizedMean = 0,
            [Description(
                "TwoSided tests for any difference, Greater tests whether the true mean exceeds the hypothesised value, Less whether it is below it.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description(
                "Significance level between 0 and 1 used for the decision and for a 1-alpha interval. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 2);
            RequireVariation(sample, nameof(sample));
            RequireFinite(hypothesizedMean, nameof(hypothesizedMean));
            RequireAlpha(alpha);

            var test = Guard(() => new TTest(sample, hypothesizedMean, ToOneSample(tail)), nameof(sample));

            var interval = Guard(() =>
                new TTest(sample, hypothesizedMean)
                    .GetConfidenceInterval(1 - alpha), nameof(sample));

            var mean = sample.Mean();
            var standardDeviation = sample.StandardDeviation(mean);
            var effect = (mean - hypothesizedMean) / standardDeviation;

            var warnings = new List<string>();

            if (sample.Length < 10)
            {
                warnings.Add(
                    "Small sample: the t test assumes approximately normal data and cannot be checked reliably with fewer than 10 values.");
            }

            var result = Build("One-sample t test", "t", test.Statistic, test.PValue, tail, alpha,
                $"the population mean equals {Format(hypothesizedMean)}",
                TailPhrase(tail, "the population mean", "differs from", "is greater than", "is less than",
                    Format(hypothesizedMean)),
                test.StatisticDistribution.DegreesOfFreedom,
                detail:
                $"The sample mean is {Format(mean)} (Cohen's d = {Format(effect)}, {Magnitude(effect)}); the {Format(1 - alpha)} interval for the mean is {Format(interval.Min)} to {Format(interval.Max)}.",
                warnings: warnings);

            result.EstimatedValue = mean;
            result.NullValue = hypothesizedMean;
            result.EffectSize = effect;
            result.EffectSizeName = "Cohen's d";
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Min;
            result.ConfidenceUpper = interval.Max;
            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("OneSampleTTestFromSummary")]
        [Description(
            "Runs the one-sample t test from summary statistics (mean, sample standard deviation and count) when the raw values are not available. " +
            "Fields are read exactly as for OneSampleTTest: Statistic is t, DegreesOfFreedom is n-1, EffectSize is Cohen's d and the confidence bounds are the two-sided interval for the mean at 1-alpha. " +
            "The standard deviation must be the sample (n-1) standard deviation and be greater than zero.")]
        public HypothesisTestResult OneSampleTTestFromSummary(
            [Description("The sample mean.")] double mean,
            [Description("The sample standard deviation (n-1 denominator), greater than zero.")]
            double standardDeviation,
            [Description("The number of observations behind the mean, at least 2.")]
            int sampleSize,
            [Description("The mean stated by the null hypothesis. Default 0.")]
            double hypothesizedMean = 0,
            [Description("TwoSided, Greater (true mean above the hypothesised value) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireFinite(mean, nameof(mean));
            RequirePositive(standardDeviation, nameof(standardDeviation));
            RequireCount(sampleSize, nameof(sampleSize), 2);
            RequireFinite(hypothesizedMean, nameof(hypothesizedMean));
            RequireAlpha(alpha);

            var test = Guard(() => new TTest(mean, standardDeviation, sampleSize, hypothesizedMean, ToOneSample(tail)),
                nameof(mean));

            var interval = Guard(() => new TTest(mean, standardDeviation, sampleSize, hypothesizedMean)
                .GetConfidenceInterval(1 - alpha), nameof(mean));

            var effect = (mean - hypothesizedMean) / standardDeviation;

            var result = Build("One-sample t test", "t", test.Statistic, test.PValue, tail, alpha,
                $"the population mean equals {Format(hypothesizedMean)}",
                TailPhrase(tail, "the population mean", "differs from", "is greater than", "is less than",
                    Format(hypothesizedMean)),
                test.StatisticDistribution.DegreesOfFreedom,
                detail:
                $"The sample mean is {Format(mean)} (Cohen's d = {Format(effect)}, {Magnitude(effect)}); the {Format(1 - alpha)} interval for the mean is {Format(interval.Min)} to {Format(interval.Max)}.");

            result.EstimatedValue = mean;
            result.NullValue = hypothesizedMean;
            result.EffectSize = effect;
            result.EffectSizeName = "Cohen's d";
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Min;
            result.ConfidenceUpper = interval.Max;
            result.SampleSize1 = sampleSize;

            return result;
        }

        [AdvancedAnalyticsTool("TwoSampleTTest")]
        [Description(
            "Compares the means of two independent groups with a t test. The default is Welch's test, which does not assume equal variances and is the safe choice; set assumeEqualVariances to true for Student's pooled test. " +
            "Statistic is t for (mean1 - mean2 - hypothesizedDifference), DegreesOfFreedom is the Welch or pooled value, EstimatedValue is mean1 - mean2, EffectSize is Cohen's d using the pooled standard deviation and the confidence bounds are the two-sided interval for the mean difference at 1-alpha. " +
            "Greater means group 1 has the larger mean. Use PairedTTest instead when each value in one group is matched to a value in the other.")]
        public HypothesisTestResult TwoSampleTTest(
            [Description("Observed values of the first group, at least 2, no NaN.")]
            double[] sample1,
            [Description("Observed values of the second group, at least 2, no NaN.")]
            double[] sample2,
            [Description(
                "False (default) for Welch's unequal-variance test, true for Student's pooled equal-variance test.")]
            bool assumeEqualVariances = false,
            [Description("The difference mean1 - mean2 stated by the null hypothesis. Default 0.")]
            double hypothesizedDifference = 0,
            [Description("TwoSided, Greater (group 1 mean larger than group 2) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample1, nameof(sample1), 2);
            RequireValues(sample2, nameof(sample2), 2);
            RequireFinite(hypothesizedDifference, nameof(hypothesizedDifference));
            RequireAlpha(alpha);

            var mean1 = sample1.Mean();
            var mean2 = sample2.Mean();
            var variance1 = sample1.Variance(mean1);
            var variance2 = sample2.Variance(mean2);

            if (variance1 == 0 && variance2 == 0)
            {
                throw new ArgumentException(
                    "Both groups are constant, so the variance is zero and the t statistic is undefined.",
                    nameof(sample1));
            }

            return TwoSampleTTestCore(mean1, variance1, sample1.Length, mean2, variance2, sample2.Length,
                assumeEqualVariances, hypothesizedDifference, tail, alpha);
        }

        [AdvancedAnalyticsTool("TwoSampleTTestFromSummary")]
        [Description(
            "Compares two independent group means from summary statistics (mean, sample standard deviation, count for each group) using Welch's test by default or Student's pooled test. " +
            "Fields are read exactly as for TwoSampleTTest. Use it when only aggregates are known, for example from a report.")]
        public HypothesisTestResult TwoSampleTTestFromSummary(
            [Description("Mean of the first group.")]
            double mean1,
            [Description("Sample standard deviation of the first group (n-1 denominator).")]
            double standardDeviation1,
            [Description("Number of observations in the first group, at least 2.")]
            int sampleSize1,
            [Description("Mean of the second group.")]
            double mean2,
            [Description("Sample standard deviation of the second group (n-1 denominator).")]
            double standardDeviation2,
            [Description("Number of observations in the second group, at least 2.")]
            int sampleSize2,
            [Description("False (default) for Welch's test, true for Student's pooled test.")]
            bool assumeEqualVariances = false,
            [Description("The difference mean1 - mean2 stated by the null hypothesis. Default 0.")]
            double hypothesizedDifference = 0,
            [Description("TwoSided, Greater (group 1 larger) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireFinite(mean1, nameof(mean1));
            RequireFinite(mean2, nameof(mean2));
            RequirePositive(standardDeviation1, nameof(standardDeviation1));
            RequirePositive(standardDeviation2, nameof(standardDeviation2));
            RequireCount(sampleSize1, nameof(sampleSize1), 2);
            RequireCount(sampleSize2, nameof(sampleSize2), 2);
            RequireFinite(hypothesizedDifference, nameof(hypothesizedDifference));
            RequireAlpha(alpha);

            return TwoSampleTTestCore(mean1, standardDeviation1 * standardDeviation1, sampleSize1, mean2,
                standardDeviation2 * standardDeviation2, sampleSize2, assumeEqualVariances, hypothesizedDifference,
                tail, alpha);
        }

        [AdvancedAnalyticsTool("PairedTTest")]
        [Description(
            "Tests whether the mean of paired differences (first array minus second array, matched element by element) differs from zero, such as before versus after on the same cases. " +
            "Statistic is t on the differences, DegreesOfFreedom is pairs-1, EstimatedValue is the mean difference, EffectSize is Cohen's d for paired data (mean difference divided by the standard deviation of the differences) and the confidence bounds are the two-sided interval for the mean difference at 1-alpha. " +
            "Greater means the first array tends to be larger. The arrays must have equal length; identical differences (zero variance) are rejected.")]
        public HypothesisTestResult PairedTTest(
            [Description("First measurement of every pair, for example the before values.")]
            double[] sample1,
            [Description("Second measurement of every pair in the same order, for example the after values.")]
            double[] sample2,
            [Description("TwoSided, Greater (first larger than second on average) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample1, nameof(sample1), 2);
            RequireValues(sample2, nameof(sample2), 2);
            RequireSameLength(sample1, nameof(sample1), sample2, nameof(sample2));
            RequireAlpha(alpha);

            var differences = sample1.Zip(sample2, (a, b) => a - b).ToArray();
            RequireVariation(differences, nameof(sample1));

            var test = Guard(() => new LibraryPairedTTest(sample1, sample2, ToTwoSample(tail)), nameof(sample1));

            var interval = new LibraryPairedTTest(sample1, sample2)
                .GetConfidenceInterval(1 - alpha);

            var meanDifference = differences.Mean();
            var effect = meanDifference / differences.StandardDeviation(meanDifference);

            var warnings = new List<string>();

            if (sample1.Length < 10)
            {
                warnings.Add(
                    "Few pairs: the paired t test assumes the differences are approximately normal, which cannot be checked reliably with fewer than 10 pairs.");
            }

            var result = Build("Paired t test", "t", test.Statistic, test.PValue, tail, alpha,
                "the mean paired difference is zero",
                TailPhrase(tail, "the mean of first minus second", "differs from", "is greater than", "is less than",
                    "zero"),
                test.StatisticDistribution.DegreesOfFreedom,
                detail:
                $"The mean difference is {Format(meanDifference)} (Cohen's d = {Format(effect)}, {Magnitude(effect)}); the {Format(1 - alpha)} interval is {Format(interval.Min)} to {Format(interval.Max)}.",
                warnings: warnings);

            result.EstimatedValue = meanDifference;
            result.NullValue = 0;
            result.EffectSize = effect;
            result.EffectSizeName = "Cohen's d (paired)";
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Min;
            result.ConfidenceUpper = interval.Max;
            result.SampleSize1 = sample1.Length;
            result.SampleSize2 = sample2.Length;

            return result;
        }

        [AdvancedAnalyticsTool("OneSampleZTest")]
        [Description(
            "Tests a sample mean against a stated value using the normal distribution. Use it for large samples (about 30 or more), or when the population standard deviation is genuinely known and supplied in knownStandardDeviation; otherwise prefer OneSampleTTest. " +
            "Statistic is z, EstimatedValue is the sample mean, EffectSize is Cohen's d when the sample standard deviation is used and the confidence bounds are the two-sided normal interval for the mean at 1-alpha. " +
            "Samples under 30 raise a warning because the sample standard deviation is then too uncertain for a normal reference.")]
        public HypothesisTestResult OneSampleZTest(
            [Description("The observed numeric values, at least 2, no NaN.")]
            double[] sample,
            [Description("The mean stated by the null hypothesis. Default 0.")]
            double hypothesizedMean = 0,
            [Description(
                "The known population standard deviation, greater than zero. Leave empty to use the sample standard deviation.")]
            double? knownStandardDeviation = null,
            [Description("TwoSided, Greater (true mean above the hypothesised value) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 2);
            RequireFinite(hypothesizedMean, nameof(hypothesizedMean));
            RequireAlpha(alpha);

            var mean = sample.Mean();
            var sampleStandardDeviation = sample.StandardDeviation(mean);
            double standardDeviation;

            if (knownStandardDeviation.HasValue)
            {
                RequirePositive(knownStandardDeviation.Value, nameof(knownStandardDeviation));
                standardDeviation = knownStandardDeviation.Value;
            }
            else
            {
                RequireVariation(sample, nameof(sample));
                standardDeviation = sampleStandardDeviation;
            }

            var standardError = standardDeviation / Math.Sqrt(sample.Length);

            var test = Guard(() => new ZTest(mean, standardError,
                hypothesizedMean,
                ToOneSample(tail)), nameof(sample));

            var interval = new ZTest(mean, standardError,
                    hypothesizedMean, OneSampleHypothesis.ValueIsDifferentFromHypothesis)
                .GetConfidenceInterval(1 - alpha);

            var effect = (mean - hypothesizedMean) / standardDeviation;

            var warnings = new List<string>();

            if (!knownStandardDeviation.HasValue && sample.Length < 30)
            {
                warnings.Add(
                    "Fewer than 30 observations with an estimated standard deviation: the t test (OneSampleTTest) is more appropriate.");
            }

            var result = Build("One-sample z test", "z", test.Statistic, test.PValue, tail, alpha,
                $"the population mean equals {Format(hypothesizedMean)}",
                TailPhrase(tail, "the population mean", "differs from", "is greater than", "is less than",
                    Format(hypothesizedMean)),
                detail:
                $"The sample mean is {Format(mean)} (Cohen's d = {Format(effect)}, {Magnitude(effect)}); the {Format(1 - alpha)} interval for the mean is {Format(interval.Min)} to {Format(interval.Max)}.",
                warnings: warnings);

            result.EstimatedValue = mean;
            result.NullValue = hypothesizedMean;
            result.EffectSize = effect;
            result.EffectSizeName = "Cohen's d";
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Min;
            result.ConfidenceUpper = interval.Max;
            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("TwoSampleZTest")]
        [Description(
            "Compares the means of two independent large samples using the normal distribution with each group's own variance (no pooling). " +
            "Use it when both groups have about 30 or more observations; for smaller groups use TwoSampleTTest. " +
            "Statistic is z for (mean1 - mean2 - hypothesizedDifference), EstimatedValue is mean1 - mean2 and the confidence bounds are the two-sided normal interval for the difference at 1-alpha. Greater means group 1 has the larger mean.")]
        public HypothesisTestResult TwoSampleZTest(
            [Description("Observed values of the first group, at least 2, no NaN.")]
            double[] sample1,
            [Description("Observed values of the second group, at least 2, no NaN.")]
            double[] sample2,
            [Description("The difference mean1 - mean2 stated by the null hypothesis. Default 0.")]
            double hypothesizedDifference = 0,
            [Description("TwoSided, Greater (group 1 larger) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample1, nameof(sample1), 2);
            RequireValues(sample2, nameof(sample2), 2);
            RequireFinite(hypothesizedDifference, nameof(hypothesizedDifference));
            RequireAlpha(alpha);

            var mean1 = sample1.Mean();
            var mean2 = sample2.Mean();
            var variance1 = sample1.Variance(mean1);
            var variance2 = sample2.Variance(mean2);

            if (variance1 == 0 && variance2 == 0)
            {
                throw new ArgumentException("Both groups are constant, so the z statistic is undefined.",
                    nameof(sample1));
            }

            return TwoSampleZTestCore(mean1, variance1, sample1.Length, mean2, variance2, sample2.Length,
                hypothesizedDifference, tail, alpha);
        }

        [AdvancedAnalyticsTool("TwoSampleZTestFromSummary")]
        [Description(
            "Compares two independent group means from summary statistics using the normal distribution: each group needs a mean, a standard deviation and a count. " +
            "Fields are read exactly as for TwoSampleZTest. Use it for large groups when only aggregates are known.")]
        public HypothesisTestResult TwoSampleZTestFromSummary(
            [Description("Mean of the first group.")]
            double mean1,
            [Description("Standard deviation of the first group, greater than zero.")]
            double standardDeviation1,
            [Description("Number of observations in the first group, at least 2.")]
            int sampleSize1,
            [Description("Mean of the second group.")]
            double mean2,
            [Description("Standard deviation of the second group, greater than zero.")]
            double standardDeviation2,
            [Description("Number of observations in the second group, at least 2.")]
            int sampleSize2,
            [Description("The difference mean1 - mean2 stated by the null hypothesis. Default 0.")]
            double hypothesizedDifference = 0,
            [Description("TwoSided, Greater (group 1 larger) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireFinite(mean1, nameof(mean1));
            RequireFinite(mean2, nameof(mean2));
            RequirePositive(standardDeviation1, nameof(standardDeviation1));
            RequirePositive(standardDeviation2, nameof(standardDeviation2));
            RequireCount(sampleSize1, nameof(sampleSize1), 2);
            RequireCount(sampleSize2, nameof(sampleSize2), 2);
            RequireFinite(hypothesizedDifference, nameof(hypothesizedDifference));
            RequireAlpha(alpha);

            return TwoSampleZTestCore(mean1, standardDeviation1 * standardDeviation1, sampleSize1, mean2,
                standardDeviation2 * standardDeviation2, sampleSize2, hypothesizedDifference, tail, alpha);
        }

        [AdvancedAnalyticsTool("WaldZTestForEstimate")]
        [Description(
            "Tests any estimate that is approximately normally distributed (a coefficient, a difference, a log odds ratio) against a stated value given its standard error, always two-sided. " +
            "Statistic is the Wald z = (estimate - hypothesizedValue) / standardError and the confidence bounds are the normal interval for the estimate at 1-alpha. " +
            "Use it when another tool has given an estimate and a standard error but no p-value.")]
        public HypothesisTestResult WaldZTestForEstimate(
            [Description("The estimated value, for example a fitted coefficient.")]
            double estimate,
            [Description("The value stated by the null hypothesis, usually 0.")]
            double hypothesizedValue,
            [Description("The standard error of the estimate, greater than zero.")]
            double standardError,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireFinite(estimate, nameof(estimate));
            RequireFinite(hypothesizedValue, nameof(hypothesizedValue));
            RequirePositive(standardError, nameof(standardError));
            RequireAlpha(alpha);

            var test = Guard(() => new WaldTest(estimate, hypothesizedValue, standardError), nameof(estimate));

            var interval = new ZTest(estimate, standardError,
                hypothesizedValue,
                OneSampleHypothesis.ValueIsDifferentFromHypothesis).GetConfidenceInterval(1 - alpha);

            var result = Build("Wald z test", "z", test.Statistic, test.PValue, HypothesisTail.TwoSided, alpha,
                $"the true value equals {Format(hypothesizedValue)}",
                $"the true value differs from {Format(hypothesizedValue)}",
                detail:
                $"The estimate is {Format(estimate)} with standard error {Format(standardError)}; the {Format(1 - alpha)} interval is {Format(interval.Min)} to {Format(interval.Max)}.");

            result.EstimatedValue = estimate;
            result.NullValue = hypothesizedValue;
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Min;
            result.ConfidenceUpper = interval.Max;

            return result;
        }

        [AdvancedAnalyticsTool("OneProportionZTest")]
        [Description(
            "Tests whether an observed proportion (successes out of trials) differs from a stated proportion using the normal approximation with the standard error taken under the null hypothesis. " +
            "Use it when both trials*p0 and trials*(1-p0) are at least about 5; otherwise use BinomialExactTest. " +
            "Statistic is z, EstimatedValue is the observed proportion and the confidence bounds are the Wilson score interval at 1-alpha.")]
        public HypothesisTestResult OneProportionZTest(
            [Description("Number of successes observed, between 0 and trials.")]
            int successes,
            [Description("Total number of independent trials, at least 1.")]
            int trials,
            [Description("The proportion stated by the null hypothesis, strictly between 0 and 1. Default 0.5.")]
            double hypothesizedProportion = 0.5,
            [Description("TwoSided, Greater (true proportion above the hypothesised one) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireCount(trials, nameof(trials), 1);
            RequireSuccesses(successes, trials, nameof(successes));
            RequireProbability(hypothesizedProportion, nameof(hypothesizedProportion));
            RequireAlpha(alpha);

            var observed = successes / (double)trials;
            var standardError = Math.Sqrt(hypothesizedProportion * (1 - hypothesizedProportion) / trials);

            var test = Guard(() => new ZTest(observed, standardError,
                hypothesizedProportion, ToOneSample(tail)), nameof(successes));

            var interval = ProportionInterval(successes, trials, 1 - alpha, HypothesisProportionIntervalMethod.Wilson);

            var warnings = new List<string>();

            if (trials * hypothesizedProportion < 5 || trials * (1 - hypothesizedProportion) < 5)
            {
                warnings.Add(
                    "The expected successes or failures under the null are below 5: the normal approximation is unreliable, use BinomialExactTest.");
            }

            var result = Build("One-proportion z test", "z", test.Statistic, test.PValue, tail, alpha,
                $"the true proportion equals {Format(hypothesizedProportion)}",
                TailPhrase(tail, "the true proportion", "differs from", "is greater than", "is less than",
                    Format(hypothesizedProportion)),
                detail:
                $"The observed proportion is {Format(observed)} ({successes} of {trials}); the {Format(1 - alpha)} Wilson interval is {Format(interval.Lower)} to {Format(interval.Upper)}.",
                warnings: warnings);

            result.EstimatedValue = observed;
            result.NullValue = hypothesizedProportion;
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Lower;
            result.ConfidenceUpper = interval.Upper;
            result.SampleSize1 = trials;

            return result;
        }

        [AdvancedAnalyticsTool("TwoProportionZTest")]
        [Description(
            "Compares two independent proportions (successes out of trials in each group) with the pooled two-proportion z test, for example the fraud rate in two segments. " +
            "Statistic is z, EstimatedValue is proportion1 - proportion2 and Greater means group 1 has the higher proportion. " +
            "It needs roughly 5 or more successes and failures in each group; otherwise use FisherExactTest on the 2x2 counts.")]
        public HypothesisTestResult TwoProportionZTest(
            [Description("Successes in the first group, between 0 and trials1.")]
            int successes1,
            [Description("Number of trials in the first group, at least 1.")]
            int trials1,
            [Description("Successes in the second group, between 0 and trials2.")]
            int successes2,
            [Description("Number of trials in the second group, at least 1.")]
            int trials2,
            [Description("TwoSided, Greater (group 1 proportion higher) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireCount(trials1, nameof(trials1), 1);
            RequireCount(trials2, nameof(trials2), 1);
            RequireSuccesses(successes1, trials1, nameof(successes1));
            RequireSuccesses(successes2, trials2, nameof(successes2));
            RequireAlpha(alpha);

            var totalSuccesses = successes1 + successes2;
            var totalTrials = trials1 + trials2;

            if (totalSuccesses == 0 || totalSuccesses == totalTrials)
            {
                throw new ArgumentException(
                    "Every trial in both groups has the same outcome, so the pooled variance is zero and z is undefined.",
                    nameof(successes1));
            }

            var test = Guard(() => new TwoProportionZTest(successes1, trials1, successes2, trials2, ToTwoSample(tail)),
                nameof(successes1));

            var p1 = successes1 / (double)trials1;
            var p2 = successes2 / (double)trials2;

            var warnings = new List<string>();
            var pooled = totalSuccesses / (double)totalTrials;

            if (trials1 * pooled < 5 || trials1 * (1 - pooled) < 5 || trials2 * pooled < 5 ||
                trials2 * (1 - pooled) < 5)
            {
                warnings.Add(
                    "An expected count is below 5: the normal approximation is unreliable, use FisherExactTest.");
            }

            var result = Build("Two-proportion z test", "z", test.Statistic, test.PValue, tail, alpha,
                "the two groups share the same true proportion",
                TailPhrase(tail, "the true proportion of group 1", "differs from that of group 2",
                    "is greater than that of group 2", "is less than that of group 2", string.Empty),
                detail: $"The proportions are {Format(p1)} and {Format(p2)}, a difference of {Format(p1 - p2)}.",
                warnings: warnings);

            result.EstimatedValue = p1 - p2;
            result.NullValue = 0;
            result.SampleSize1 = trials1;
            result.SampleSize2 = trials2;

            return result;
        }

        [AdvancedAnalyticsTool("BinomialExactTest")]
        [Description(
            "Exact test that the probability of success equals a stated value, given a count of successes out of trials, with no normal approximation, so it is valid for small counts and extreme proportions. " +
            "Statistic is the success count, EstimatedValue is the observed proportion and the confidence bounds are the Clopper-Pearson exact interval at 1-alpha. " +
            "The two-sided p-value adds every outcome no more likely than the observed one.")]
        public HypothesisTestResult BinomialExactTest(
            [Description("Number of successes observed, between 0 and trials.")]
            int successes,
            [Description("Total number of independent trials, at least 1.")]
            int trials,
            [Description(
                "The success probability stated by the null hypothesis, strictly between 0 and 1. Default 0.5.")]
            double hypothesizedProbability = 0.5,
            [Description("TwoSided, Greater (true probability above the hypothesised one) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireCount(trials, nameof(trials), 1);
            RequireSuccesses(successes, trials, nameof(successes));
            RequireProbability(hypothesizedProbability, nameof(hypothesizedProbability));
            RequireAlpha(alpha);

            return BinomialCore(successes, trials, hypothesizedProbability, tail, alpha);
        }

        [AdvancedAnalyticsTool("BinomialExactTestFromOutcomes")]
        [Description(
            "Runs the exact binomial test directly from a list of true/false outcomes (true counts as a success), for example flagged or not flagged per transaction. " +
            "Fields are read exactly as for BinomialExactTest: Statistic is the number of true values and EstimatedValue the observed proportion.")]
        public HypothesisTestResult BinomialExactTestFromOutcomes(
            [Description("One true or false outcome per trial, at least one.")]
            bool[] outcomes,
            [Description(
                "The success probability stated by the null hypothesis, strictly between 0 and 1. Default 0.5.")]
            double hypothesizedProbability = 0.5,
            [Description("TwoSided, Greater (true probability above the hypothesised one) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            if (outcomes == null || outcomes.Length == 0)
            {
                throw new ArgumentException("Supply at least one true/false outcome.", nameof(outcomes));
            }

            RequireProbability(hypothesizedProbability, nameof(hypothesizedProbability));
            RequireAlpha(alpha);

            return BinomialCore(outcomes.Count(o => o), outcomes.Length, hypothesizedProbability, tail, alpha);
        }

        [AdvancedAnalyticsTool("FTestTwoVariances")]
        [Description(
            "F test comparing the variances of two independent samples, statistic = variance1 / variance2. " +
            "It is sensitive to non-normal data, so treat a significant result on skewed or heavy tailed data with caution and prefer LeveneTest there. " +
            "Statistic is the variance ratio, DegreesOfFreedom and DegreesOfFreedom2 are n1-1 and n2-1, EstimatedValue is the ratio and the confidence bounds are the interval for the true variance ratio at 1-alpha. Greater means group 1 is more variable.")]
        public HypothesisTestResult FTestTwoVariances(
            [Description("Observed values of the first group, at least 2, no NaN.")]
            double[] sample1,
            [Description("Observed values of the second group, at least 2, no NaN.")]
            double[] sample2,
            [Description("TwoSided, Greater (group 1 more variable) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample1, nameof(sample1), 2);
            RequireValues(sample2, nameof(sample2), 2);
            RequireAlpha(alpha);
            RequireVariation(sample1, nameof(sample1));
            RequireVariation(sample2, nameof(sample2));

            return VarianceRatioCore(sample1.Variance(), sample1.Length, sample2.Variance(),
                sample2.Length, tail, alpha);
        }

        [AdvancedAnalyticsTool("FTestTwoVariancesFromSummary")]
        [Description(
            "F test comparing two variances from summary statistics: each group needs its sample variance (n-1 denominator) and its count. " +
            "Fields are read exactly as for FTestTwoVariances.")]
        public HypothesisTestResult FTestTwoVariancesFromSummary(
            [Description("Sample variance of the first group, greater than zero.")]
            double variance1,
            [Description("Number of observations in the first group, at least 2.")]
            int sampleSize1,
            [Description("Sample variance of the second group, greater than zero.")]
            double variance2,
            [Description("Number of observations in the second group, at least 2.")]
            int sampleSize2,
            [Description("TwoSided, Greater (group 1 more variable) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequirePositive(variance1, nameof(variance1));
            RequirePositive(variance2, nameof(variance2));
            RequireCount(sampleSize1, nameof(sampleSize1), 2);
            RequireCount(sampleSize2, nameof(sampleSize2), 2);
            RequireAlpha(alpha);

            return VarianceRatioCore(variance1, sampleSize1, variance2, sampleSize2, tail, alpha);
        }

        [AdvancedAnalyticsTool("LeveneTest")]
        [Description(
            "Tests whether two or more groups have equal variances (homogeneity of variance), robust to non-normal data. " +
            "Centre Mean is the original Levene test, Median is the Brown-Forsythe variant (best default for skewed data) and TrimmedMean removes trimFraction of each tail before taking the centre. " +
            "Statistic is F, DegreesOfFreedom is groups-1 and DegreesOfFreedom2 is total observations minus groups. A small p-value means the variances differ, so use Welch based tests or non-parametric tests.")]
        public HypothesisTestResult LeveneTest(
            [Description("One array of observations per group, at least 2 groups with at least 2 values each.")]
            double[][] groups,
            [Description(
                "Mean, Median (Brown-Forsythe) or TrimmedMean: the centre each value's deviation is measured from. Default Median.")]
            HypothesisVarianceHomogeneityCentre centre = HypothesisVarianceHomogeneityCentre.Median,
            [Description("Share trimmed from each tail when centre is TrimmedMean, between 0 and 0.5. Default 0.1.")]
            double trimFraction = 0.1,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireGroups(groups, nameof(groups), 2, 2);
            RequireAlpha(alpha);

            if (double.IsNaN(trimFraction) || trimFraction < 0 || trimFraction >= 0.5)
            {
                throw new ArgumentException("The trim fraction must be at least 0 and below 0.5.",
                    nameof(trimFraction));
            }

            var test = centre switch
            {
                HypothesisVarianceHomogeneityCentre.Mean => new LibraryLeveneTest(groups),
                HypothesisVarianceHomogeneityCentre.Median => new LibraryLeveneTest(groups, true),
                _ => new LibraryLeveneTest(groups, trimFraction)
            };

            var result = Build("Levene test for equal variances", "F", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, "all group variances are equal", "at least one group variance differs",
                test.DegreesOfFreedom1, test.DegreesOfFreedom2,
                $"Deviations were measured from the group {centre.ToString().ToLowerInvariant()}.");

            result.SampleSize1 = groups.Sum(g => g.Length);

            return result;
        }

        [AdvancedAnalyticsTool("BartlettTest")]
        [Description(
            "Tests whether two or more groups have equal variances assuming every group is normally distributed; it is more powerful than LeveneTest for normal data but unreliable for skewed data. " +
            "Statistic is the chi-square value with groups-1 degrees of freedom. A small p-value means at least one variance differs.")]
        public HypothesisTestResult BartlettTest(
            [Description(
                "One array of observations per group, at least 2 groups with at least 2 values each and some spread.")]
            double[][] groups,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireGroups(groups, nameof(groups), 2, 2);
            RequireAlpha(alpha);

            foreach (var group in groups)
            {
                RequireVariation(group, nameof(groups));
            }

            var test = Guard(() => new LibraryBartlettTest(groups), nameof(groups));

            var result = Build("Bartlett test for equal variances", "chi-square", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, "all group variances are equal", "at least one group variance differs",
                test.DegreesOfFreedom);

            result.SampleSize1 = groups.Sum(g => g.Length);

            return result;
        }

        [AdvancedAnalyticsTool("OneWayAnova")]
        [Description(
            "One-way analysis of variance: tests whether three or more independent groups share the same mean. " +
            "FStatistic is between-group variance over within-group variance; DegreesOfFreedomBetween is groups-1 and DegreesOfFreedomWithin is N-groups. EtaSquared is the share of total variance explained by group membership (about 0.01 small, 0.06 medium, 0.14 large). " +
            "Table lists sum of squares, degrees of freedom and mean squares for between, within and total. A significant result says only that some group differs; it does not say which. " +
            "It assumes roughly normal groups with similar variances, so check LeveneTest, and use a rank-based test such as KruskalWallisTest when that fails.")]
        public HypothesisAnovaResult OneWayAnova(
            [Description(
                "One array of observations per group, at least 2 groups, each with at least 2 values, no NaN.")]
            double[][] groups,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireGroups(groups, nameof(groups), 2, 2);
            RequireAlpha(alpha);

            return OneWayAnovaCore(groups, alpha);
        }

        [AdvancedAnalyticsTool("OneWayAnovaFromLabels")]
        [Description(
            "One-way analysis of variance when the data are one long list of values plus a parallel list of group labels, for example amount and merchant category. " +
            "Values with the same label form a group; the fields are read exactly as for OneWayAnova. Labels are matched as whole numbers and need at least two distinct labels.")]
        public HypothesisAnovaResult OneWayAnovaFromLabels(
            [Description("All observations in one array, no NaN.")]
            double[] values,
            [Description("The group label of each value, in the same order and length as the values.")]
            int[] labels,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(values, nameof(values), 4);
            RequireAlpha(alpha);

            if (labels == null || labels.Length != values.Length)
            {
                throw new ArgumentException("Give exactly one label for every value.", nameof(labels));
            }

            var groups = values.Zip(labels, (v, l) => new { v, l }).GroupBy(x => x.l).OrderBy(g => g.Key)
                .Select(g => g.Select(x => x.v).ToArray()).ToArray();

            RequireGroups(groups, nameof(labels), 2, 2);

            return OneWayAnovaCore(groups, alpha);
        }

        [AdvancedAnalyticsTool("TwoWayAnova")]
        [Description(
            "Two-way analysis of variance for a balanced design: tests the effect of factor A, the effect of factor B and their interaction on a numeric outcome. " +
            "Provide one value per observation with zero-based whole-number level labels for each factor; every combination of levels must have the same number of replicates, at least 2. " +
            "FixedEffects (default) tests all effects against the error mean square, RandomEffects tests both factors against the interaction, FactorAFixedFactorBRandom is the mixed model. " +
            "Read the interaction first: when it is significant the factor effects depend on each other. PartialEtaSquared is the effect share of effect-plus-error variance.")]
        public HypothesisTwoWayAnovaResult TwoWayAnova(
            [Description("All observations in one array, no NaN.")]
            double[] values,
            [Description("Zero-based level of factor A for each value (0, 1, 2 ...), same length as values.")]
            int[] factorALabels,
            [Description("Zero-based level of factor B for each value (0, 1, 2 ...), same length as values.")]
            int[] factorBLabels,
            [Description("FixedEffects, RandomEffects or FactorAFixedFactorBRandom. Default FixedEffects.")]
            HypothesisTwoWayAnovaModel model = HypothesisTwoWayAnovaModel.FixedEffects,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(values, nameof(values), 8);
            RequireAlpha(alpha);

            if (factorALabels == null || factorALabels.Length != values.Length)
            {
                throw new ArgumentException("Give exactly one factor A label for every value.", nameof(factorALabels));
            }

            if (factorBLabels == null || factorBLabels.Length != values.Length)
            {
                throw new ArgumentException("Give exactly one factor B label for every value.", nameof(factorBLabels));
            }

            if (factorALabels.Any(l => l < 0) || factorBLabels.Any(l => l < 0))
            {
                throw new ArgumentException("Factor labels are zero-based levels and must not be negative.",
                    nameof(factorALabels));
            }

            var levelsA = factorALabels.Max() + 1;
            var levelsB = factorBLabels.Max() + 1;

            if (levelsA < 2 || levelsB < 2)
            {
                throw new ArgumentException("Each factor needs at least two levels.", nameof(factorALabels));
            }

            var cells = new int[levelsA, levelsB];

            for (var i = 0; i < values.Length; i++)
            {
                cells[factorALabels[i], factorBLabels[i]]++;
            }

            var replications = cells[0, 0];

            if (replications < 2)
            {
                throw new ArgumentException("Each combination of levels needs at least 2 replicates.",
                    nameof(factorALabels));
            }

            for (var a = 0; a < levelsA; a++)
            {
                for (var b = 0; b < levelsB; b++)
                {
                    if (cells[a, b] != replications)
                    {
                        throw new ArgumentException(
                            $"The design is unbalanced: combination A={a}, B={b} has {cells[a, b]} values but A=0, B=0 has {replications}. Every combination must have the same number of replicates.",
                            nameof(factorALabels));
                    }
                }
            }

            var libraryModel = model switch
            {
                HypothesisTwoWayAnovaModel.FixedEffects => TwoWayAnovaModel.Mixed,
                HypothesisTwoWayAnovaModel.RandomEffects => TwoWayAnovaModel.Fixed,
                _ => TwoWayAnovaModel.Random
            };

            var anova = Guard(() => new TwoWayAnova(values, factorALabels, factorBLabels, libraryModel),
                nameof(values));

            var error = anova.Sources.Error.SumOfSquares;

            double Partial(AnovaVariationSource source)
            {
                return source.SumOfSquares / (source.SumOfSquares + error);
            }

            var a1 = anova.Sources.FactorA;
            var b1 = anova.Sources.FactorB;
            var ab = anova.Sources.Interaction;

            bool Rejects(AnovaVariationSource source)
            {
                return !double.IsNaN(source.Significance.PValue) && source.Significance.PValue < alpha;
            }

            string Sentence(string name, AnovaVariationSource source)
            {
                return
                    $"{name}: F = {Format(source.Significance.Statistic)}, p = {Format(source.Significance.PValue)} ({(Rejects(source) ? "significant" : "not significant")})";
            }

            return new HypothesisTwoWayAnovaResult
            {
                TestName = "Two-way ANOVA",
                Model = model,
                FactorAfStatistic = a1.Significance.Statistic,
                FactorApValue = a1.Significance.PValue,
                FactorAPartialEtaSquared = Partial(a1),
                FactorARejectNull = Rejects(a1),
                FactorBfStatistic = b1.Significance.Statistic,
                FactorBpValue = b1.Significance.PValue,
                FactorBPartialEtaSquared = Partial(b1),
                FactorBRejectNull = Rejects(b1),
                InteractionFStatistic = ab.Significance.Statistic,
                InteractionPValue = ab.Significance.PValue,
                InteractionPartialEtaSquared = Partial(ab),
                InteractionRejectNull = Rejects(ab),
                FactorALevels = levelsA,
                FactorBLevels = levelsB,
                Replications = replications,
                Alpha = alpha,
                Table = anova.Table.Select(ToRow).ToArray(),
                Interpretation =
                    $"Two-way ANOVA ({model}) at alpha = {Format(alpha)}. {Sentence("Factor A", a1)}; {Sentence("Factor B", b1)}; {Sentence("Interaction", ab)}."
            };
        }

        [AdvancedAnalyticsTool("GrubbsOutlierTest")]
        [Description(
            "Grubbs test for a single outlier in a sample assumed to come from a normal distribution. " +
            "Statistic is the largest standardised deviation G (distance of the most extreme value from the mean in standard deviations); a small p-value means that extreme value is unlikely under normality and is an outlier. " +
            "Either tests the more extreme end, Maximum only the largest value and Minimum only the smallest. Run it repeatedly, removing one outlier at a time, to find several, and needs at least 3 values.")]
        public HypothesisTestResult GrubbsOutlierTest(
            [Description("The observed values, at least 3, no NaN, with some spread.")]
            double[] sample,
            [Description("Either (default), Maximum or Minimum: which extreme value is being tested as an outlier.")]
            HypothesisOutlierSide side = HypothesisOutlierSide.Either,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 3);
            RequireVariation(sample, nameof(sample));
            RequireAlpha(alpha);

            var hypothesis = side switch
            {
                HypothesisOutlierSide.Maximum => GrubbTestHypothesis.TheMaximumIsAnOutlier,
                HypothesisOutlierSide.Minimum => GrubbTestHypothesis.TheMinimumIsAnOutlier,
                _ => GrubbTestHypothesis.ThereAreOutliers
            };

            var test = Guard(() => new GrubbTest(sample, hypothesis), nameof(sample));

            var extreme = side switch
            {
                HypothesisOutlierSide.Maximum => test.Mean + test.Max,
                HypothesisOutlierSide.Minimum => test.Mean + test.Min,
                _ => test.Abs == test.Max ? test.Mean + test.Max : test.Mean + test.Min
            };

            var result = Build("Grubbs outlier test", "G", test.Statistic, test.PValue, HypothesisTail.Greater, alpha,
                "there is no outlier", "the most extreme value is an outlier",
                detail:
                $"The most extreme value tested is {Format(extreme)} against a mean of {Format(test.Mean)} and standard deviation {Format(test.StandardDeviation)}.");

            result.EstimatedValue = extreme;
            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("EffectSizeTwoSamples")]
        [Description(
            "Computes standardised effect sizes for the difference between two independent groups: Cohen's d (mean difference over the pooled standard deviation), Hedges' g (d with a small sample bias correction) and Glass's delta (mean difference over the second group's standard deviation, for when group 2 is a control). " +
            "Magnitude uses the conventional labels negligible (below 0.2), small, medium (from 0.5) and large (from 0.8) on the absolute size. Positive values mean group 1 has the higher mean. " +
            "Use it alongside a t test to say whether a significant difference is also practically meaningful.")]
        public HypothesisEffectSizeResult EffectSizeTwoSamples(
            [Description("Observed values of the first group, at least 2, no NaN.")]
            double[] sample1,
            [Description("Observed values of the second group, at least 2, no NaN.")]
            double[] sample2)
        {
            RequireValues(sample1, nameof(sample1), 2);
            RequireValues(sample2, nameof(sample2), 2);

            if (sample1.All(v => v == sample1[0]) && sample2.All(v => v == sample2[0]))
            {
                throw new ArgumentException("Both groups are constant, so the standardised effect size is undefined.",
                    nameof(sample1));
            }

            return EffectSizeCore(sample1.Mean(), sample1.StandardDeviation(), sample1.Length,
                sample2.Mean(), sample2.StandardDeviation(), sample2.Length);
        }

        [AdvancedAnalyticsTool("EffectSizeFromSummary")]
        [Description(
            "Computes Cohen's d, Hedges' g and Glass's delta for two groups from summary statistics (mean, sample standard deviation, count per group). " +
            "Fields are read exactly as for EffectSizeTwoSamples; positive values mean group 1 has the higher mean.")]
        public HypothesisEffectSizeResult EffectSizeFromSummary(
            [Description("Mean of the first group.")]
            double mean1,
            [Description("Sample standard deviation of the first group.")]
            double standardDeviation1,
            [Description("Number of observations in the first group, at least 2.")]
            int sampleSize1,
            [Description("Mean of the second group.")]
            double mean2,
            [Description("Sample standard deviation of the second group.")]
            double standardDeviation2,
            [Description("Number of observations in the second group, at least 2.")]
            int sampleSize2)
        {
            RequireFinite(mean1, nameof(mean1));
            RequireFinite(mean2, nameof(mean2));
            RequirePositive(standardDeviation1, nameof(standardDeviation1));
            RequirePositive(standardDeviation2, nameof(standardDeviation2));
            RequireCount(sampleSize1, nameof(sampleSize1), 2);
            RequireCount(sampleSize2, nameof(sampleSize2), 2);

            return EffectSizeCore(mean1, standardDeviation1, sampleSize1, mean2, standardDeviation2, sampleSize2);
        }

        private static HypothesisTestResult TwoSampleTTestCore(double mean1, double variance1, int n1, double mean2,
            double variance2, int n2, bool equalVariances, double hypothesizedDifference, HypothesisTail tail,
            double alpha)
        {
            var test = Guard(() => new LibraryTwoSampleTTest(mean1, variance1, n1, mean2, variance2, n2,
                equalVariances, hypothesizedDifference, ToTwoSample(tail)), "mean1");

            var interval = Guard(() => new LibraryTwoSampleTTest(mean1, variance1, n1, mean2, variance2, n2,
                    equalVariances, hypothesizedDifference)
                .GetConfidenceInterval(1 - alpha), "mean1");

            var pooledVariance = ((n1 - 1) * variance1 + (n2 - 1) * variance2) / (n1 + n2 - 2);
            var effect = (mean1 - mean2) / Math.Sqrt(pooledVariance);

            var warnings = new List<string>();

            if (n1 < 10 || n2 < 10)
            {
                warnings.Add(
                    "Small group: the t test assumes approximately normal data and cannot be checked reliably with fewer than 10 values per group.");
            }

            var ratio = Math.Max(variance1, variance2) / Math.Min(variance1, variance2);

            if (equalVariances && ratio > 4)
            {
                warnings.Add(
                    $"The group variances differ by a factor of {Format(ratio)}; the pooled test assumes they are equal, so prefer Welch's test (assumeEqualVariances = false).");
            }

            var result = Build(equalVariances ? "Two-sample t test (pooled)" : "Two-sample t test (Welch)", "t",
                test.Statistic, test.PValue, tail, alpha,
                $"the difference in group means equals {Format(hypothesizedDifference)}",
                TailPhrase(tail, "the mean of group 1 minus the mean of group 2", "differs from", "is greater than",
                    "is less than", Format(hypothesizedDifference)),
                test.DegreesOfFreedom,
                detail:
                $"The observed difference is {Format(mean1 - mean2)} (Cohen's d = {Format(effect)}, {Magnitude(effect)}); the {Format(1 - alpha)} interval for the difference is {Format(interval.Min)} to {Format(interval.Max)}.",
                warnings: warnings);

            result.EstimatedValue = mean1 - mean2;
            result.NullValue = hypothesizedDifference;
            result.EffectSize = effect;
            result.EffectSizeName = "Cohen's d";
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Min;
            result.ConfidenceUpper = interval.Max;
            result.SampleSize1 = n1;
            result.SampleSize2 = n2;

            return result;
        }

        private static HypothesisTestResult TwoSampleZTestCore(double mean1, double variance1, int n1, double mean2,
            double variance2, int n2, double hypothesizedDifference, HypothesisTail tail, double alpha)
        {
            var test = Guard(() => new LibraryTwoSampleZTest(mean1, variance1, n1, mean2, variance2, n2,
                hypothesizedDifference, ToTwoSample(tail)), "mean1");

            var interval = Guard(() => new LibraryTwoSampleZTest(mean1, variance1, n1, mean2, variance2, n2,
                hypothesizedDifference).GetConfidenceInterval(1 - alpha), "mean1");

            var pooledVariance = ((n1 - 1) * variance1 + (n2 - 1) * variance2) / (n1 + n2 - 2);
            var effect = (mean1 - mean2) / Math.Sqrt(pooledVariance);

            var warnings = new List<string>();

            if (n1 < 30 || n2 < 30)
            {
                warnings.Add(
                    "A group has fewer than 30 observations: the t test (TwoSampleTTest) is more appropriate.");
            }

            var result = Build("Two-sample z test", "z", test.Statistic, test.PValue, tail, alpha,
                $"the difference in group means equals {Format(hypothesizedDifference)}",
                TailPhrase(tail, "the mean of group 1 minus the mean of group 2", "differs from", "is greater than",
                    "is less than", Format(hypothesizedDifference)),
                detail:
                $"The observed difference is {Format(mean1 - mean2)} (Cohen's d = {Format(effect)}, {Magnitude(effect)}); the {Format(1 - alpha)} interval for the difference is {Format(interval.Min)} to {Format(interval.Max)}.",
                warnings: warnings);

            result.EstimatedValue = mean1 - mean2;
            result.NullValue = hypothesizedDifference;
            result.EffectSize = effect;
            result.EffectSizeName = "Cohen's d";
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Min;
            result.ConfidenceUpper = interval.Max;
            result.SampleSize1 = n1;
            result.SampleSize2 = n2;

            return result;
        }

        private static HypothesisTestResult BinomialCore(int successes, int trials, double hypothesizedProbability,
            HypothesisTail tail, double alpha)
        {
            var test = Guard(() => new BinomialTest(successes, trials, hypothesizedProbability, ToOneSample(tail)),
                nameof(hypothesizedProbability));

            var interval = ProportionInterval(successes, trials, 1 - alpha,
                HypothesisProportionIntervalMethod.ClopperPearson);

            var observed = successes / (double)trials;

            var result = Build("Exact binomial test", "successes", test.Statistic, test.PValue, tail, alpha,
                $"the true success probability equals {Format(hypothesizedProbability)}",
                TailPhrase(tail, "the true success probability", "differs from", "is greater than", "is less than",
                    Format(hypothesizedProbability)),
                detail:
                $"The observed proportion is {Format(observed)} ({successes} of {trials}); the {Format(1 - alpha)} Clopper-Pearson interval is {Format(interval.Lower)} to {Format(interval.Upper)}.");

            result.EstimatedValue = observed;
            result.NullValue = hypothesizedProbability;
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = interval.Lower;
            result.ConfidenceUpper = interval.Upper;
            result.SampleSize1 = trials;

            return result;
        }

        private static HypothesisTestResult VarianceRatioCore(double variance1, int n1, double variance2, int n2,
            HypothesisTail tail, double alpha)
        {
            var test = Guard(() => new FTest(variance1, variance2, n1 - 1, n2 - 1, ToTwoSample(tail)),
                nameof(variance1));

            var distribution = new FDistribution(n1 - 1, n2 - 1);
            var ratio = variance1 / variance2;
            var lower = ratio / distribution.InverseDistributionFunction(1 - alpha / 2);
            var upper = ratio / distribution.InverseDistributionFunction(alpha / 2);

            var result = Build("F test for two variances", "F", test.Statistic, test.PValue, tail, alpha,
                "the two population variances are equal",
                TailPhrase(tail, "the variance of group 1", "differs from that of group 2",
                    "is greater than that of group 2", "is less than that of group 2", string.Empty),
                n1 - 1, n2 - 1,
                $"The variance ratio is {Format(ratio)}; the {Format(1 - alpha)} interval for the true ratio is {Format(lower)} to {Format(upper)}.");

            result.EstimatedValue = ratio;
            result.NullValue = 1;
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = lower;
            result.ConfidenceUpper = upper;
            result.SampleSize1 = n1;
            result.SampleSize2 = n2;

            return result;
        }

        private static HypothesisAnovaResult OneWayAnovaCore(double[][] groups, double alpha)
        {
            var anova = Guard(() => new OneWayAnova(groups), nameof(groups));
            var test = anova.FTest;

            var total = anova.Table.Single(r => r.Source == "Total");
            var between = anova.Table.Single(r => r.Source == "Between-Groups");
            var eta = total.SumOfSquares > 0 ? between.SumOfSquares / total.SumOfSquares : double.NaN;

            var means = groups.Select(g => g.Mean()).ToArray();
            var rejected = !double.IsNaN(test.PValue) && test.PValue < alpha;

            var warnings = new List<string>();
            var sizes = groups.Select(g => g.Length).ToArray();

            if (sizes.Max() > 2 * sizes.Min())
            {
                warnings.Add(
                    "Group sizes are quite unbalanced; the equal-variance assumption matters more, check LeveneTest.");
            }

            var interpretation = double.IsNaN(test.PValue)
                ? $"One-way ANOVA: F = {Format(test.Statistic)}, but the p-value could not be computed because the within-group variance is zero."
                : $"One-way ANOVA: F = {Format(test.Statistic)} with {test.DegreesOfFreedom1} and {test.DegreesOfFreedom2} degrees of freedom, p = {Format(test.PValue)}. At alpha = {Format(alpha)} " +
                  (rejected
                      ? "reject the null hypothesis that all group means are equal; at least one group mean differs"
                      : "fail to reject the null hypothesis that all group means are equal; there is not enough evidence that any group mean differs") +
                  $". Group membership explains {Format(eta * 100)}% of the variance (eta squared = {Format(eta)}).";

            return new HypothesisAnovaResult
            {
                TestName = "One-way ANOVA",
                FStatistic = test.Statistic,
                PValue = test.PValue,
                DegreesOfFreedomBetween = test.DegreesOfFreedom1,
                DegreesOfFreedomWithin = test.DegreesOfFreedom2,
                Alpha = alpha,
                RejectNull = rejected,
                EtaSquared = eta,
                GroupMeans = means,
                GroupSizes = sizes,
                Table = anova.Table.Select(ToRow).ToArray(),
                Warnings = warnings.ToArray(),
                Interpretation = interpretation
            };
        }

        private static HypothesisAnovaRow ToRow(AnovaVariationSource source)
        {
            return new HypothesisAnovaRow
            {
                Source = source.Source,
                SumOfSquares = source.SumOfSquares,
                DegreesOfFreedom = source.DegreesOfFreedom,
                MeanSquares = source.MeanSquares,
                FStatistic = source.Statistic,
                PValue = source.Significance?.PValue
            };
        }

        private static HypothesisEffectSizeResult EffectSizeCore(double mean1, double sd1, int n1, double mean2,
            double sd2, int n2)
        {
            var pooled = Math.Sqrt(((n1 - 1) * sd1 * sd1 + (n2 - 1) * sd2 * sd2) / (n1 + n2 - 2));
            var d = (mean1 - mean2) / pooled;
            var correction = 1 - 3.0 / (4.0 * (n1 + n2 - 2) - 1);
            var g = d * correction;
            var glass = sd2 > 0 ? (mean1 - mean2) / sd2 : double.NaN;

            return new HypothesisEffectSizeResult
            {
                MeanDifference = mean1 - mean2,
                CohensD = d,
                HedgesG = g,
                GlassDelta = glass,
                PooledStandardDeviation = pooled,
                Magnitude = Magnitude(d),
                Interpretation =
                    $"Group 1 minus group 2 is {Format(mean1 - mean2)}, which is {Format(d)} pooled standard deviations (Cohen's d; Hedges' g = {Format(g)}), a {Magnitude(d)} effect."
            };
        }
    }
}