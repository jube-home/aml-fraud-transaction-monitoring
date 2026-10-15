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
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Testing;
using Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models;
using static Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.HypothesisTestSupport;
using LibraryWilcoxonSignedRankTest = Accord.Statistics.Testing.WilcoxonSignedRankTest;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests
{
    [AdvancedAnalyticsToolGroup("HypothesisTests")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class RankAndDistributionTestTools
    {
        [AdvancedAnalyticsTool("MannWhitneyUTest")]
        [Description(
            "Rank-based test comparing two independent groups without assuming normality (also called the Wilcoxon rank-sum test): do values in one group tend to be larger than values in the other. " +
            "Use it instead of TwoSampleTTest for skewed, heavy tailed or ordinal data. " +
            "Statistic is U1, the number of pairs (one value from each group) where group 1 is larger, counting ties as half; EstimatedValue and EffectSize are U1/(n1*n2), the probability that a random value from group 1 exceeds one from group 2 (0.5 means no difference, 1 means group 1 always larger). " +
            "Greater means group 1 tends to be larger. The p-value is exact for small samples without ties and normal-approximated otherwise; a warning is given when ties are present.")]
        public HypothesisTestResult MannWhitneyUTest(
            [Description("Observed values of the first group, at least 1, no NaN.")]
            double[] sample1,
            [Description("Observed values of the second group, at least 1, no NaN.")]
            double[] sample2,
            [Description("TwoSided, Greater (group 1 tends larger) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05,
            [Description(
                "Empty lets the method choose, true forces the exact distribution (only valid without ties), false forces the normal approximation.")]
            bool? exact = null,
            [Description("Whether the tie correction is applied. Default true.")]
            bool adjustForTies = true)
        {
            RequireValues(sample1, nameof(sample1), 1);
            RequireValues(sample2, nameof(sample2), 1);
            RequireAlpha(alpha);

            var test = Guard(
                () => new MannWhitneyWilcoxonTest(sample1, sample2, ToTwoSample(tail), exact, adjustForTies),
                nameof(sample1));

            var probability = test.Statistic1 / ((double)sample1.Length * sample2.Length);

            var warnings = new List<string>();

            if (test.HasTies)
            {
                warnings.Add(
                    "Tied values are present, so the normal approximation with tie correction was used instead of the exact distribution.");
            }

            if (!test.IsExact && (sample1.Length < 8 || sample2.Length < 8))
            {
                warnings.Add("A group has fewer than 8 values and the p-value is approximate.");
            }

            var result = Build("Mann-Whitney U test", "U", test.Statistic1, test.PValue, tail, alpha,
                "the two groups come from the same distribution",
                TailPhrase(tail, "values in group 1", "tend to differ from values in group 2",
                    "tend to be larger than values in group 2", "tend to be smaller than values in group 2",
                    string.Empty),
                detail:
                $"U1 = {Format(test.Statistic1)}, U2 = {Format(test.Statistic2)}; the probability that a random group 1 value exceeds a group 2 value is {Format(probability)}.",
                warnings: warnings);

            result.EstimatedValue = probability;
            result.NullValue = 0.5;
            result.EffectSize = probability;
            result.EffectSizeName = "Probability of superiority U1/(n1*n2)";
            result.SampleSize1 = sample1.Length;
            result.SampleSize2 = sample2.Length;

            return result;
        }

        [AdvancedAnalyticsTool("WilcoxonSignedRankTest")]
        [Description(
            "Rank-based test that the median of one sample differs from a stated value, or that the median of paired differences is zero if you pass the differences, without assuming normality but assuming the distribution is symmetric. " +
            "Values equal to the hypothesised median are dropped. Statistic is W+, the sum of the ranks of the values above the hypothesised median. " +
            "Greater means the true median is above the stated value. The p-value is exact for small samples without ties or zeros and normal-approximated otherwise, with a warning.")]
        public HypothesisTestResult WilcoxonSignedRankTest(
            [Description("Observed values, at least 1 non-zero difference from the hypothesised median, no NaN.")]
            double[] sample,
            [Description("The median stated by the null hypothesis. Default 0.")]
            double hypothesizedMedian = 0,
            [Description("TwoSided, Greater (true median above the stated value) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05,
            [Description(
                "Empty lets the method choose, true forces the exact distribution, false forces the normal approximation.")]
            bool? exact = null,
            [Description("Whether the tie correction is applied. Default true.")]
            bool adjustForTies = true)
        {
            RequireValues(sample, nameof(sample), 1);
            RequireFinite(hypothesizedMedian, nameof(hypothesizedMedian));
            RequireAlpha(alpha);

            if (sample.All(v => v == hypothesizedMedian))
            {
                throw new ArgumentException(
                    "Every value equals the hypothesised median, so there are no differences to rank.", nameof(sample));
            }

            var test = Guard(() => new LibraryWilcoxonSignedRankTest(sample, hypothesizedMedian,
                ToOneSample(tail), exact, adjustForTies), nameof(sample));

            return SignedRankResult(test, tail, alpha, sample.Length, null, "One-sample Wilcoxon signed-rank test",
                "the median", Format(hypothesizedMedian), hypothesizedMedian);
        }

        [AdvancedAnalyticsTool("WilcoxonSignedRankPairedTest")]
        [Description(
            "Rank-based paired test: do matched measurements differ systematically, without assuming the differences are normal (the non-parametric alternative to PairedTTest). " +
            "Differences are first minus second element by element and pairs with zero difference are dropped. Statistic is W+, the sum of the ranks of the positive differences. " +
            "Greater means the first array tends to be larger. Arrays must have equal length.")]
        public HypothesisTestResult WilcoxonSignedRankPairedTest(
            [Description("First measurement of every pair, for example before values.")]
            double[] sample1,
            [Description("Second measurement of every pair in the same order, for example after values.")]
            double[] sample2,
            [Description("TwoSided, Greater (first tends larger than second) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05,
            [Description(
                "Empty lets the method choose, true forces the exact distribution, false forces the normal approximation.")]
            bool? exact = null,
            [Description("Whether the tie correction is applied. Default true.")]
            bool adjustForTies = true)
        {
            RequireValues(sample1, nameof(sample1), 1);
            RequireValues(sample2, nameof(sample2), 1);
            RequireSameLength(sample1, nameof(sample1), sample2, nameof(sample2));
            RequireAlpha(alpha);

            if (sample1.Zip(sample2, (a, b) => a - b).All(d => d == 0))
            {
                throw new ArgumentException("Every pair is identical, so there are no differences to rank.",
                    nameof(sample1));
            }

            var test = Guard(
                () => new TwoSampleWilcoxonSignedRankTest(sample1, sample2, ToTwoSample(tail), exact, adjustForTies),
                nameof(sample1));

            return SignedRankResult(test, tail, alpha, sample1.Length, sample2.Length,
                "Paired Wilcoxon signed-rank test", "the first measurement minus the second", "zero", 0);
        }

        [AdvancedAnalyticsTool("SignTestOneSample")]
        [Description(
            "Sign test that the median of a sample differs from a stated value: it counts only how many values lie above versus below the median and applies the exact binomial distribution. " +
            "It makes the fewest assumptions of any location test but has the least power; prefer WilcoxonSignedRankTest when the data are at least ordinal and roughly symmetric. " +
            "Statistic is the number of values above the stated median, Values equal to it are dropped, EstimatedValue is the share of non-tied values above it. Greater means the true median exceeds the stated value.")]
        public HypothesisTestResult SignTestOneSample(
            [Description("Observed values, at least 1 differing from the hypothesised median, no NaN.")]
            double[] sample,
            [Description("The median stated by the null hypothesis. Default 0.")]
            double hypothesizedMedian = 0,
            [Description("TwoSided, Greater (true median above the stated value) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 1);
            RequireFinite(hypothesizedMedian, nameof(hypothesizedMedian));
            RequireAlpha(alpha);

            var above = sample.Count(v => v > hypothesizedMedian);
            var below = sample.Count(v => v < hypothesizedMedian);

            if (above + below == 0)
            {
                throw new ArgumentException("Every value equals the hypothesised median, so no signs can be counted.",
                    nameof(sample));
            }

            var test = Guard(() => new SignTest(sample, hypothesizedMedian, ToOneSample(tail)), nameof(sample));

            return SignCore(above, above + below, test.PValue, tail, alpha, "One-sample sign test", "the median",
                Format(hypothesizedMedian), sample.Length);
        }

        [AdvancedAnalyticsTool("SignTestPaired")]
        [Description(
            "Sign test on paired data: counts in how many pairs the first value exceeds the second and tests whether that share differs from one half using the exact binomial distribution. " +
            "It ignores the size of the differences, so it is robust to outliers but weak; prefer WilcoxonSignedRankPairedTest when the sizes carry information. " +
            "Statistic is the number of pairs where the first value is larger, tied pairs are dropped, and Greater means the first array tends to be larger.")]
        public HypothesisTestResult SignTestPaired(
            [Description("First measurement of every pair, for example before values.")]
            double[] sample1,
            [Description("Second measurement of every pair in the same order, for example after values.")]
            double[] sample2,
            [Description("TwoSided, Greater (first tends larger than second) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample1, nameof(sample1), 1);
            RequireValues(sample2, nameof(sample2), 1);
            RequireSameLength(sample1, nameof(sample1), sample2, nameof(sample2));
            RequireAlpha(alpha);

            var first = 0;
            var second = 0;

            for (var i = 0; i < sample1.Length; i++)
            {
                if (sample1[i] > sample2[i])
                {
                    first++;
                }
                else if (sample1[i] < sample2[i])
                {
                    second++;
                }
            }

            if (first + second == 0)
            {
                throw new ArgumentException("Every pair is tied, so no signs can be counted.", nameof(sample1));
            }

            var test = Guard(() => new TwoSampleSignTest(sample1, sample2, ToTwoSample(tail)), nameof(sample1));

            return SignCore(first, first + second, test.PValue, tail, alpha, "Paired sign test",
                "the first value relative to the second", "the second value", sample1.Length);
        }

        [AdvancedAnalyticsTool("SignTestFromCounts")]
        [Description(
            "Sign test from counts when you already know how many comparisons went one way: positiveCount out of totalCount non-tied comparisons, tested against an even split with the exact binomial distribution. " +
            "Greater means positives are more frequent than negatives. Statistic is positiveCount.")]
        public HypothesisTestResult SignTestFromCounts(
            [Description("Number of comparisons with a positive sign, between 0 and totalCount.")]
            int positiveCount,
            [Description("Number of non-tied comparisons, at least 1.")]
            int totalCount,
            [Description("TwoSided, Greater (positives more frequent) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireCount(totalCount, nameof(totalCount), 1);
            RequireSuccesses(positiveCount, totalCount, nameof(positiveCount));
            RequireAlpha(alpha);

            var test = new SignTest(positiveCount, totalCount, ToOneSample(tail));

            return SignCore(positiveCount, totalCount, test.PValue, tail, alpha, "Sign test", "the comparisons",
                "an even split", totalCount);
        }

        [AdvancedAnalyticsTool("KolmogorovSmirnovOneSampleTest")]
        [Description(
            "Kolmogorov-Smirnov test of whether a sample comes from a fully specified reference distribution (Normal with mean and standard deviation, Uniform with minimum and maximum, Exponential with rate, LogNormal with the mean and standard deviation of the logarithm). " +
            "Statistic D is the largest vertical gap between the sample's empirical distribution function and the reference. A small p-value means the sample does not follow that distribution. " +
            "Do not fit the parameters from the same sample and then use this tool: that makes the p-value too large. For normality with estimated parameters use LillieforsNormalityTest or ShapiroWilkNormalityTest.")]
        public HypothesisTestResult KolmogorovSmirnovOneSampleTest(
            [Description("Observed values, at least 2, no NaN.")]
            double[] sample,
            [Description("Reference distribution: Normal, Uniform, Exponential or LogNormal.")]
            HypothesisReferenceDistribution distribution,
            [Description(
                "First parameter: Normal mean, Uniform minimum, Exponential rate (above 0) or LogNormal location (mean of the log).")]
            double parameter1,
            [Description(
                "Second parameter: Normal standard deviation (above 0), Uniform maximum, LogNormal shape (standard deviation of the log, above 0). Ignored for Exponential.")]
            double parameter2 = 1,
            [Description(
                "TwoSided tests any difference; Greater and Less test whether the sample distribution function lies above or below the reference.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 2);
            RequireAlpha(alpha);

            var reference = CreateDistribution(distribution, parameter1, parameter2, out var description);

            var test = Guard(() => new KolmogorovSmirnovTest(sample, reference, ToKolmogorovSmirnov(tail)),
                nameof(sample));

            var result = Build("One-sample Kolmogorov-Smirnov test", "D", test.Statistic, test.PValue, tail, alpha,
                $"the sample follows {description}", $"the sample does not follow {description}");

            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("KolmogorovSmirnovTwoSampleTest")]
        [Description(
            "Two-sample Kolmogorov-Smirnov test of whether two independent samples come from the same continuous distribution, sensitive to any difference in location, spread or shape. " +
            "Statistic D is the largest vertical gap between the two empirical distribution functions (0 identical, 1 completely separated). " +
            "Greater tests whether the first sample tends to be larger than the second. Ties reduce accuracy because the test assumes continuous data.")]
        public HypothesisTestResult KolmogorovSmirnovTwoSampleTest(
            [Description("Observed values of the first sample, at least 1, no NaN.")]
            double[] sample1,
            [Description("Observed values of the second sample, at least 1, no NaN.")]
            double[] sample2,
            [Description("TwoSided, Greater (first sample stochastically larger) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample1, nameof(sample1), 1);
            RequireValues(sample2, nameof(sample2), 1);
            RequireAlpha(alpha);

            var test = Guard(
                () => new TwoSampleKolmogorovSmirnovTest(sample1, sample2, ToTwoSampleKolmogorovSmirnov(tail)),
                nameof(sample1));

            var result = Build("Two-sample Kolmogorov-Smirnov test", "D", test.Statistic, test.PValue, tail, alpha,
                "both samples come from the same distribution",
                TailPhrase(tail, "the two samples", "come from different distributions",
                    "differ, with the first tending larger", "differ, with the first tending smaller", string.Empty));

            result.SampleSize1 = sample1.Length;
            result.SampleSize2 = sample2.Length;

            return result;
        }

        [AdvancedAnalyticsTool("ShapiroWilkNormalityTest")]
        [Description(
            "Shapiro-Wilk test of normality: the most powerful general test that a sample came from some normal distribution (mean and spread need not be known). " +
            "Statistic W lies between 0 and 1 and is close to 1 for normal data; a small p-value means the data are not normal. " +
            "Needs at least 4 values and works well up to a few thousand; with very large samples tiny departures become significant, so also look at skewness and a plot. Constant data are rejected.")]
        public HypothesisTestResult ShapiroWilkNormalityTest(
            [Description("Observed values, at least 4, no NaN, not all identical.")]
            double[] sample,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 4);
            RequireVariation(sample, nameof(sample));
            RequireAlpha(alpha);

            var test = Guard(() => new ShapiroWilkTest(sample), nameof(sample));

            var result = Build("Shapiro-Wilk normality test", "W", test.Statistic, test.PValue, HypothesisTail.Less,
                alpha, "the sample comes from a normal distribution",
                "the sample does not come from a normal distribution");

            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("AndersonDarlingNormalityTest")]
        [Description(
            "Anderson-Darling test of whether a sample follows a normal distribution, giving extra weight to the tails, which makes it a good check for heavy tails that matter for outlier and extreme value analysis. " +
            "The mean and standard deviation are estimated from the sample. Statistic A2 is larger when the tails depart more from normal; a small p-value means the data are not normal. " +
            "Needs at least 8 values, and constant data are rejected.")]
        public HypothesisTestResult AndersonDarlingNormalityTest(
            [Description("Observed values, at least 8, no NaN, not all identical.")]
            double[] sample,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 8);
            RequireVariation(sample, nameof(sample));
            RequireAlpha(alpha);

            var mean = sample.Mean();

            var test = Guard(
                () => new AndersonDarlingTest(sample,
                    new NormalDistribution(mean, sample.StandardDeviation(mean))), nameof(sample));

            var result = Build("Anderson-Darling normality test", "A2", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, "the sample comes from a normal distribution",
                "the sample does not come from a normal distribution");

            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("LillieforsNormalityTest")]
        [Description(
            "Lilliefors test of normality: a Kolmogorov-Smirnov test whose p-value is corrected for the mean and standard deviation being estimated from the same sample, obtained by Monte Carlo simulation. " +
            "Statistic D is the largest gap between the sample and the fitted normal distribution function. Because the p-value is simulated it can vary in the third decimal between calls; increase iterations to stabilise it. " +
            "Needs at least 5 values and constant data are rejected.")]
        public HypothesisTestResult LillieforsNormalityTest(
            [Description("Observed values, at least 5, no NaN, not all identical.")]
            double[] sample,
            [Description(
                "Number of Monte Carlo simulations used for the p-value, between 1000 and 200000. Default 10000.")]
            int iterations = 10000,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 5);
            RequireVariation(sample, nameof(sample));
            RequireAlpha(alpha);

            if (iterations < 1000 || iterations > 200000)
            {
                throw new ArgumentException("The iteration count must be between 1000 and 200000.", nameof(iterations));
            }

            var mean = sample.Mean();

            var test = Guard(() => new LillieforsTest(sample,
                new NormalDistribution(mean, sample.StandardDeviation(mean)),
                KolmogorovSmirnovTestHypothesis.SampleIsDifferent, iterations), nameof(sample));

            var result = Build("Lilliefors normality test", "D", test.Statistic, test.PValue, HypothesisTail.Greater,
                alpha, "the sample comes from a normal distribution",
                "the sample does not come from a normal distribution",
                warnings: new[] { "The p-value is a Monte Carlo estimate and can vary slightly between calls." });

            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("JarqueBeraNormalityTest")]
        [Description(
            "Jarque-Bera test of normality built on skewness and excess kurtosis: it asks whether the sample is symmetric with normal tail weight. " +
            "Statistic is n/6 * (skewness squared + excess kurtosis squared / 4), compared with a chi-square distribution with 2 degrees of freedom; EstimatedValue is the skewness. " +
            "It is reliable for large samples (hundreds or more) and should not be trusted below about 30 values. Interpretation names whether skewness or kurtosis drives the result.")]
        public HypothesisTestResult JarqueBeraNormalityTest(
            [Description("Observed values, at least 4, no NaN, not all identical.")]
            double[] sample,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 4);
            RequireVariation(sample, nameof(sample));
            RequireAlpha(alpha);

            var n = (double)sample.Length;
            var skewness = sample.Skewness(false);
            var kurtosis = sample.Kurtosis(false);
            var statistic = n / 6.0 * (skewness * skewness + kurtosis * kurtosis / 4.0);
            var p = new ChiSquareTest(statistic, 2).PValue;

            var warnings = new List<string>();

            if (sample.Length < 30)
            {
                warnings.Add(
                    "Fewer than 30 values: the chi-square reference is inaccurate, prefer ShapiroWilkNormalityTest.");
            }

            var driver = Math.Abs(skewness) * Math.Sqrt(2) > Math.Abs(kurtosis)
                ? "asymmetry (skewness)"
                : "tail weight (kurtosis)";

            var result = Build("Jarque-Bera normality test", "JB", statistic, p, HypothesisTail.Greater, alpha,
                "the sample has the skewness and kurtosis of a normal distribution",
                "the sample is not normally distributed", 2,
                detail:
                $"Skewness is {Format(skewness)} and excess kurtosis {Format(kurtosis)}; the larger departure comes from {driver}.",
                warnings: warnings);

            result.EstimatedValue = skewness;
            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("ChiSquareDistributionFitTest")]
        [Description(
            "Chi-square goodness-of-fit test of a continuous sample against a fully specified reference distribution (Normal, Uniform, Exponential or LogNormal), using equal-probability bins under the reference chosen from the sample size, with sparse bins merged. " +
            "Statistic is the chi-square value and DegreesOfFreedom the number of bins minus 1. It is less powerful than the Kolmogorov-Smirnov and Anderson-Darling tests for continuous data but is the classic choice for binned checks. " +
            "Parameters must be specified in advance, not fitted from the same sample.")]
        public HypothesisTestResult ChiSquareDistributionFitTest(
            [Description("Observed values, at least 20, no NaN.")]
            double[] sample,
            [Description("Reference distribution: Normal, Uniform, Exponential or LogNormal.")]
            HypothesisReferenceDistribution distribution,
            [Description(
                "First parameter: Normal mean, Uniform minimum, Exponential rate (above 0) or LogNormal location.")]
            double parameter1,
            [Description(
                "Second parameter: Normal standard deviation, Uniform maximum, LogNormal shape. Ignored for Exponential.")]
            double parameter2 = 1,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(sample, nameof(sample), 20);
            RequireAlpha(alpha);

            var reference = CreateDistribution(distribution, parameter1, parameter2, out var description);
            var test = Guard(() => new ChiSquareTest(sample, reference), nameof(sample));

            var result = Build("Chi-square distribution fit test", "chi-square", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, $"the sample follows {description}",
                $"the sample does not follow {description}", test.DegreesOfFreedom);

            result.SampleSize1 = sample.Length;

            return result;
        }

        [AdvancedAnalyticsTool("KruskalWallisTest")]
        [Description(
            "Kruskal-Wallis rank test: do three or more independent groups come from the same distribution, without assuming normality (the non-parametric counterpart of OneWayAnova). " +
            "All values are ranked together; Statistic is H with a tie correction, compared with a chi-square distribution with groups-1 degrees of freedom, so it is an approximation that wants at least 5 values per group. " +
            "EffectSize is epsilon squared, H/(N-1), the share of rank variation explained by group. A significant result says some group differs, not which one.")]
        public HypothesisTestResult KruskalWallisTest(
            [Description("One array of observations per group, at least 2 groups, each with at least 1 value, no NaN.")]
            double[][] groups,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireGroups(groups, nameof(groups), 2, 1);
            RequireAlpha(alpha);

            var all = groups.SelectMany(g => g).ToArray();
            var total = all.Length;

            if (all.All(v => v == all[0]))
            {
                throw new ArgumentException(
                    "Every value in every group is identical, so the ranks carry no information.",
                    nameof(groups));
            }

            var ranks = all.Rank(out _, adjustForTies: true);
            var offset = 0;
            var sum = 0.0;

            foreach (var group in groups)
            {
                var rankSum = 0.0;

                for (var i = 0; i < group.Length; i++)
                {
                    rankSum += ranks[offset + i];
                }

                sum += rankSum * rankSum / group.Length;
                offset += group.Length;
            }

            var h = 12.0 / ((double)total * (total + 1)) * sum - 3.0 * (total + 1);

            var tieTerm = all.GroupBy(v => v).Sum(g => Math.Pow(g.Count(), 3) - g.Count());
            var correction = 1 - tieTerm / (Math.Pow(total, 3) - total);
            h /= correction;

            var degrees = groups.Length - 1;
            var p = new ChiSquareTest(h, degrees).PValue;
            var epsilon = h / (total - 1);

            var warnings = new List<string>();

            if (groups.Any(g => g.Length < 5))
            {
                warnings.Add("A group has fewer than 5 values, so the chi-square approximation is rough.");
            }

            var result = Build("Kruskal-Wallis test", "H", h, p, HypothesisTail.Greater, alpha,
                "all groups come from the same distribution",
                "at least one group tends to have larger or smaller values",
                degrees,
                detail: $"Epsilon squared = {Format(epsilon)}.", warnings: warnings);

            result.EffectSize = epsilon;
            result.EffectSizeName = "Epsilon squared H/(N-1)";
            result.SampleSize1 = total;

            return result;
        }

        private static HypothesisTestResult SignedRankResult(WilcoxonTest test, HypothesisTail tail, double alpha,
            int n1, int? n2, string name, string subject, string reference, double nullValue)
        {
            var warnings = new List<string>();

            if (!test.IsExact)
            {
                warnings.Add(
                    "The p-value uses the normal approximation because the sample is large or contains ties or zero differences.");
            }

            if (test.HasZeros)
            {
                warnings.Add("Zero differences were dropped before ranking.");
            }

            var result = Build(name, "W+", test.Statistic, test.PValue, tail, alpha,
                $"{subject} has a symmetric distribution centred on {reference}",
                TailPhrase(tail, subject, "differs from", "is greater than", "is less than", reference),
                detail: $"{test.Ranks.Length} non-zero differences were ranked.",
                warnings: warnings);

            result.NullValue = nullValue;
            result.SampleSize1 = n1;
            result.SampleSize2 = n2;

            return result;
        }

        private static HypothesisTestResult SignCore(int positive, int total, double pValue, HypothesisTail tail,
            double alpha, string name, string subject, string reference, int sampleSize)
        {
            var share = positive / (double)total;

            var result = Build(name, "positives", positive, pValue, tail, alpha,
                $"positive and negative signs are equally likely ({subject} versus {reference})",
                TailPhrase(tail, "positive signs", "are more or less common than negative signs",
                    "are more common than negative signs", "are less common than negative signs", string.Empty),
                detail: $"{positive} of {total} non-tied comparisons were positive ({Format(share)}).");

            result.EstimatedValue = share;
            result.NullValue = 0.5;
            result.SampleSize1 = sampleSize;

            return result;
        }

        private static IUnivariateDistribution<double> CreateDistribution(
            HypothesisReferenceDistribution distribution, double parameter1, double parameter2, out string description)
        {
            RequireFinite(parameter1, nameof(parameter1));
            RequireFinite(parameter2, nameof(parameter2));

            switch (distribution)
            {
                case HypothesisReferenceDistribution.Normal:
                    RequirePositive(parameter2, nameof(parameter2));

                    description =
                        $"a normal distribution (mean {Format(parameter1)}, standard deviation {Format(parameter2)})";

                    return new NormalDistribution(parameter1, parameter2);
                case HypothesisReferenceDistribution.Uniform:
                    if (parameter2 <= parameter1)
                    {
                        throw new ArgumentException(
                            "For a Uniform reference parameter2 (maximum) must exceed parameter1 (minimum).",
                            nameof(parameter2));
                    }

                    description = $"a uniform distribution on {Format(parameter1)} to {Format(parameter2)}";

                    return new UniformContinuousDistribution(parameter1, parameter2);
                case HypothesisReferenceDistribution.Exponential:
                    RequirePositive(parameter1, nameof(parameter1));
                    description = $"an exponential distribution (rate {Format(parameter1)})";

                    return new ExponentialDistribution(parameter1);
                default:
                    RequirePositive(parameter2, nameof(parameter2));

                    description =
                        $"a log-normal distribution (location {Format(parameter1)}, shape {Format(parameter2)})";

                    return new LognormalDistribution(parameter1, parameter2);
            }
        }
    }
}