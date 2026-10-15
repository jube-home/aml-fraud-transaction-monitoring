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
using Accord.Statistics.Testing.Power;
using Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models;
using static Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.HypothesisTestSupport;
using LibraryConvergenceException = Accord.ConvergenceException;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests
{
    [AdvancedAnalyticsToolGroup("HypothesisTests")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class PowerAndIntervalTools
    {
        [AdvancedAnalyticsTool("OneSampleTTestPowerAnalysis")]
        [Description(
            "Power and sample size planning for the one-sample or paired t test. Choose what to solve for: Power (needs effectSize and sampleSize), SampleSize (needs effectSize and power) or EffectSize (needs sampleSize and power, gives the smallest standardised effect the test can detect). " +
            "effectSize is Cohen's d, the true mean difference divided by the standard deviation (0.2 small, 0.5 medium, 0.8 large). Power is the chance of detecting a true effect, conventionally 0.8. " +
            "SampleSize is rounded up to a whole number of observations and the returned Power is the power actually achieved at that size. For a paired design use the standard deviation of the differences.")]
        public HypothesisPowerResult OneSampleTTestPowerAnalysis(
            [Description(
                "Which quantity to compute: Power, SampleSize or EffectSize. The other inputs it needs must be given.")]
            HypothesisPowerSolveFor solveFor,
            [Description(
                "Cohen's d, true difference over standard deviation, above 0. Needed for Power and SampleSize.")]
            double? effectSize = null,
            [Description("Number of observations, at least 2. Needed for Power and EffectSize.")]
            double? sampleSize = null,
            [Description("Required power between 0 and 1, for example 0.8. Needed for SampleSize and EffectSize.")]
            double? power = null,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05,
            [Description(
                "TwoSided, Greater or Less alternative hypothesis; Less is analysed as its mirror image Greater with the effect size as a magnitude. Default TwoSided.")]
            HypothesisTail tail = HypothesisTail.TwoSided)
        {
            return OneSample(solveFor, effectSize, sampleSize, power, alpha, tail, false);
        }

        [AdvancedAnalyticsTool("OneSampleZTestPowerAnalysis")]
        [Description(
            "Power and sample size planning for the one-sample z test (known or well estimated standard deviation, large samples). Solve for Power (needs effectSize and sampleSize), SampleSize (needs effectSize and power) or EffectSize (needs sampleSize and power). " +
            "effectSize is the true difference divided by the standard deviation. SampleSize is rounded up and the returned Power is that achieved at the rounded size.")]
        public HypothesisPowerResult OneSampleZTestPowerAnalysis(
            [Description(
                "Which quantity to compute: Power, SampleSize or EffectSize. The other inputs it needs must be given.")]
            HypothesisPowerSolveFor solveFor,
            [Description("True difference over standard deviation, above 0. Needed for Power and SampleSize.")]
            double? effectSize = null,
            [Description("Number of observations, at least 2. Needed for Power and EffectSize.")]
            double? sampleSize = null,
            [Description("Required power between 0 and 1, for example 0.8. Needed for SampleSize and EffectSize.")]
            double? power = null,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05,
            [Description(
                "TwoSided, Greater or Less alternative hypothesis; Less is analysed as its mirror image Greater with the effect size as a magnitude. Default TwoSided.")]
            HypothesisTail tail = HypothesisTail.TwoSided)
        {
            return OneSample(solveFor, effectSize, sampleSize, power, alpha, tail, true);
        }

        [AdvancedAnalyticsTool("TwoSampleTTestPowerAnalysis")]
        [Description(
            "Power and sample size planning for the two-sample t test comparing two independent means. Solve for Power (needs effectSize and sampleSize1), SampleSize (needs effectSize and power; group 2 is allocationRatio times group 1) or EffectSize (needs sampleSize1 and power). " +
            "effectSize is Cohen's d, the true mean difference over the common standard deviation (0.2 small, 0.5 medium, 0.8 large). Sizes are rounded up and Power is that achieved at the rounded sizes. " +
            "For Power and EffectSize give sampleSize2 as well if the groups differ in size; otherwise it defaults to sampleSize1 times allocationRatio.")]
        public HypothesisPowerResult TwoSampleTTestPowerAnalysis(
            [Description(
                "Which quantity to compute: Power, SampleSize or EffectSize. The other inputs it needs must be given.")]
            HypothesisPowerSolveFor solveFor,
            [Description(
                "Cohen's d, true mean difference over standard deviation, above 0. Needed for Power and SampleSize.")]
            double? effectSize = null,
            [Description("Observations in group 1, at least 2. Needed for Power and EffectSize.")]
            double? sampleSize1 = null,
            [Description(
                "Observations in group 2, at least 2. Optional; defaults to sampleSize1 times allocationRatio.")]
            double? sampleSize2 = null,
            [Description("Required power between 0 and 1, for example 0.8. Needed for SampleSize and EffectSize.")]
            double? power = null,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05,
            [Description(
                "TwoSided, Greater (group 1 larger) or Less alternative; Less is analysed as its mirror image Greater with the effect size as a magnitude. Default TwoSided.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Group 2 size divided by group 1 size, above 0. Default 1 for equal groups.")]
            double allocationRatio = 1)
        {
            return TwoSample(solveFor, effectSize, sampleSize1, sampleSize2, power, alpha, tail, allocationRatio,
                false);
        }

        [AdvancedAnalyticsTool("TwoSampleZTestPowerAnalysis")]
        [Description(
            "Power and sample size planning for the two-sample z test comparing two independent means with large samples. Solve for Power (needs effectSize and sampleSize1), SampleSize (needs effectSize and power; group 2 is allocationRatio times group 1) or EffectSize (needs sampleSize1 and power). " +
            "effectSize is the true difference over the standard deviation. Sizes are rounded up and Power is that achieved at the rounded sizes.")]
        public HypothesisPowerResult TwoSampleZTestPowerAnalysis(
            [Description(
                "Which quantity to compute: Power, SampleSize or EffectSize. The other inputs it needs must be given.")]
            HypothesisPowerSolveFor solveFor,
            [Description("True difference over standard deviation, above 0. Needed for Power and SampleSize.")]
            double? effectSize = null,
            [Description("Observations in group 1, at least 2. Needed for Power and EffectSize.")]
            double? sampleSize1 = null,
            [Description(
                "Observations in group 2, at least 2. Optional; defaults to sampleSize1 times allocationRatio.")]
            double? sampleSize2 = null,
            [Description("Required power between 0 and 1, for example 0.8. Needed for SampleSize and EffectSize.")]
            double? power = null,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05,
            [Description(
                "TwoSided, Greater (group 1 larger) or Less alternative; Less is analysed as its mirror image Greater with the effect size as a magnitude. Default TwoSided.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Group 2 size divided by group 1 size, above 0. Default 1 for equal groups.")]
            double allocationRatio = 1)
        {
            return TwoSample(solveFor, effectSize, sampleSize1, sampleSize2, power, alpha, tail, allocationRatio, true);
        }

        [AdvancedAnalyticsTool("MeanConfidenceInterval")]
        [Description(
            "Confidence interval for a population mean from a sample, using Student's t distribution: mean plus or minus t times the standard error. " +
            "Estimate is the sample mean, Lower and Upper bound the plausible values for the true mean at the stated level. A 0.95 level means that 95 percent of intervals built this way cover the true mean. " +
            "Needs at least 2 values; constant data give a zero-width interval.")]
        public HypothesisIntervalResult MeanConfidenceInterval(
            [Description("Observed numeric values, at least 2, no NaN.")]
            double[] sample,
            [Description("Confidence level strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            RequireValues(sample, nameof(sample), 2);
            RequireProbability(confidenceLevel, nameof(confidenceLevel));

            var mean = sample.Mean();
            var standardError = sample.StandardDeviation(mean) / Math.Sqrt(sample.Length);

            var critical =
                new TDistribution(sample.Length - 1).InverseDistributionFunction(1 - (1 - confidenceLevel) / 2);

            var lower = mean - critical * standardError;
            var upper = mean + critical * standardError;

            return new HypothesisIntervalResult
            {
                Quantity = "mean",
                Method = "Student t",
                Estimate = mean,
                Lower = lower,
                Upper = upper,
                ConfidenceLevel = confidenceLevel,
                SampleSize = sample.Length,
                Interpretation =
                    $"The sample mean is {Format(mean)}; the true mean lies between {Format(lower)} and {Format(upper)} with {Format(confidenceLevel * 100)}% confidence."
            };
        }

        [AdvancedAnalyticsTool("ProportionConfidenceInterval")]
        [Description(
            "Confidence interval for a proportion from successes out of trials. Method Wilson (default) is accurate for small samples and proportions near 0 or 1; ClopperPearson is the conservative exact interval; AgrestiCoull is a simple good approximation; Wald is the textbook interval that is poor for small samples or extreme proportions and is offered for comparison. " +
            "Estimate is the observed proportion; Lower and Upper are clipped to 0 to 1.")]
        public HypothesisIntervalResult ProportionConfidenceInterval(
            [Description("Number of successes, between 0 and trials.")]
            int successes,
            [Description("Number of trials, at least 1.")]
            int trials,
            [Description("Confidence level strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Wilson, ClopperPearson, AgrestiCoull or Wald. Default Wilson.")]
            HypothesisProportionIntervalMethod method = HypothesisProportionIntervalMethod.Wilson)
        {
            RequireCount(trials, nameof(trials), 1);
            RequireSuccesses(successes, trials, nameof(successes));
            RequireProbability(confidenceLevel, nameof(confidenceLevel));

            var result = ProportionInterval(successes, trials, confidenceLevel, method);
            var warnings = new List<string>();

            if (method == HypothesisProportionIntervalMethod.Wald && (successes < 10 || trials - successes < 10))
            {
                warnings.Add(
                    "The Wald interval is unreliable with fewer than 10 successes or failures; prefer Wilson.");
            }

            result.Warnings = warnings.ToArray();

            result.Interpretation =
                $"{successes} of {trials} is a proportion of {Format(result.Estimate)}; the true proportion lies between {Format(result.Lower)} and {Format(result.Upper)} with {Format(confidenceLevel * 100)}% confidence ({method}).";

            return result;
        }

        [AdvancedAnalyticsTool("VarianceConfidenceInterval")]
        [Description(
            "Confidence interval for a population variance, or for the standard deviation when forStandardDeviation is true, from a sample, based on the chi-square distribution. " +
            "It assumes the data are normally distributed and is sensitive to departures from that; it is wide for small samples. Estimate is the sample variance (or standard deviation); Lower and Upper are the bounds. Needs at least 2 values with some spread.")]
        public HypothesisIntervalResult VarianceConfidenceInterval(
            [Description("Observed numeric values, at least 2, no NaN, not all identical.")]
            double[] sample,
            [Description("Confidence level strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "True to report the interval for the standard deviation instead of the variance. Default false.")]
            bool forStandardDeviation = false)
        {
            RequireValues(sample, nameof(sample), 2);
            RequireVariation(sample, nameof(sample));
            RequireProbability(confidenceLevel, nameof(confidenceLevel));

            var n = sample.Length;
            var variance = sample.Variance();
            var distribution = new ChiSquareDistribution(n - 1);
            var tail = (1 - confidenceLevel) / 2;
            var lower = (n - 1) * variance / distribution.InverseDistributionFunction(1 - tail);
            var upper = (n - 1) * variance / distribution.InverseDistributionFunction(tail);

            if (forStandardDeviation)
            {
                variance = Math.Sqrt(variance);
                lower = Math.Sqrt(lower);
                upper = Math.Sqrt(upper);
            }

            var label = forStandardDeviation ? "standard deviation" : "variance";

            return new HypothesisIntervalResult
            {
                Quantity = label,
                Method = "Chi-square",
                Estimate = variance,
                Lower = lower,
                Upper = upper,
                ConfidenceLevel = confidenceLevel,
                SampleSize = n,
                Warnings = n < 30
                    ? new[] { "The interval assumes normal data and is wide and fragile with fewer than 30 values." }
                    : Array.Empty<string>(),
                Interpretation =
                    $"The sample {label} is {Format(variance)}; the true {label} lies between {Format(lower)} and {Format(upper)} with {Format(confidenceLevel * 100)}% confidence."
            };
        }

        [AdvancedAnalyticsTool("MedianConfidenceInterval")]
        [Description(
            "Distribution-free confidence interval for the population median, built from order statistics with the exact binomial distribution, so it needs no normality. " +
            "The bounds are actual observed values, so the achieved confidence (AchievedConfidenceLevel) is at least the requested level and can be higher; with very small samples the requested level may be unreachable and the widest possible interval (minimum to maximum) is returned with a warning. " +
            "Estimate is the sample median.")]
        public HypothesisIntervalResult MedianConfidenceInterval(
            [Description("Observed numeric values, at least 2, no NaN.")]
            double[] sample,
            [Description("Confidence level strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            RequireValues(sample, nameof(sample), 2);
            RequireProbability(confidenceLevel, nameof(confidenceLevel));

            var sorted = Sorted(sample);
            var n = sorted.Length;
            var binomial = new BinomialDistribution(n, 0.5);
            var tail = (1 - confidenceLevel) / 2;

            var rank = 0;

            for (var candidate = n / 2; candidate >= 1; candidate--)
            {
                if (binomial.DistributionFunction(candidate - 1) <= tail)
                {
                    rank = candidate;
                    break;
                }
            }

            var warnings = new List<string>();

            if (rank == 0)
            {
                warnings.Add(
                    "The sample is too small to reach the requested confidence with order statistics; the minimum and maximum are returned.");
            }

            var used = Math.Max(rank, 1);
            var lowerIndex = used - 1;
            var upperIndex = n - used;
            var achieved = 1 - 2 * binomial.DistributionFunction(used - 1);

            var median = sample.Median();

            return new HypothesisIntervalResult
            {
                Quantity = "median",
                Method = "Order statistics with the exact binomial distribution",
                Estimate = median,
                Lower = sorted[lowerIndex],
                Upper = sorted[upperIndex],
                ConfidenceLevel = confidenceLevel,
                AchievedConfidenceLevel = achieved,
                SampleSize = n,
                Warnings = warnings.ToArray(),
                Interpretation =
                    $"The sample median is {Format(median)}; the true median lies between {Format(sorted[lowerIndex])} and {Format(sorted[upperIndex])} with at least {Format(Math.Round(achieved * 100, 1))}% confidence."
            };
        }

        [AdvancedAnalyticsTool("AdjustPValuesBonferroni")]
        [Description(
            "Bonferroni multiple-testing correction: multiplies each p-value by the number of tests (capped at 1) to control the chance of any false positive across the whole family. " +
            "It is simple and conservative, best for a small number of tests. AdjustedPValues are in the input order and RejectNull marks the tests still significant at alpha after adjustment.")]
        public HypothesisPValueAdjustmentResult AdjustPValuesBonferroni(
            [Description("The raw p-values of all tests in the family, each between 0 and 1, in any order.")]
            double[] pValues,
            [Description("Family-wise significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            return Adjust(pValues, alpha, HypothesisPValueAdjustment.Bonferroni);
        }

        [AdvancedAnalyticsTool("AdjustPValuesHolm")]
        [Description(
            "Holm step-down multiple-testing correction: controls the chance of any false positive like Bonferroni but is never less powerful, so prefer it over Bonferroni. " +
            "Tests are ordered by p-value and the smallest is multiplied by m, the next by m-1 and so on, keeping the adjusted values non-decreasing. AdjustedPValues are in the input order and RejectNull marks tests still significant at alpha.")]
        public HypothesisPValueAdjustmentResult AdjustPValuesHolm(
            [Description("The raw p-values of all tests in the family, each between 0 and 1, in any order.")]
            double[] pValues,
            [Description("Family-wise significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            return Adjust(pValues, alpha, HypothesisPValueAdjustment.Holm);
        }

        [AdvancedAnalyticsTool("AdjustPValuesBenjaminiHochberg")]
        [Description(
            "Benjamini-Hochberg multiple-testing correction controlling the false discovery rate (the expected share of false positives among the tests declared significant) rather than the chance of any false positive. " +
            "Use it when screening many tests, such as thousands of features or rules, where a few false discoveries are tolerable; it is far more powerful than Bonferroni or Holm. AdjustedPValues are q-values in the input order and RejectNull marks tests with q below alpha.")]
        public HypothesisPValueAdjustmentResult AdjustPValuesBenjaminiHochberg(
            [Description("The raw p-values of all tests in the family, each between 0 and 1, in any order.")]
            double[] pValues,
            [Description("Target false discovery rate between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            return Adjust(pValues, alpha, HypothesisPValueAdjustment.BenjaminiHochberg);
        }

        [AdvancedAnalyticsTool("AdjustPValuesByMethod")]
        [Description(
            "Multiple-testing correction with a chosen method: Bonferroni, Sidak (exact for independent tests), Holm (step-down, family-wise), Hochberg (step-up, family-wise, valid for independent or positively dependent tests), BenjaminiHochberg (false discovery rate) or BenjaminiYekutieli (false discovery rate under any dependence, more conservative). " +
            "AdjustedPValues are in the input order and RejectNull marks tests significant at alpha after adjustment.")]
        public HypothesisPValueAdjustmentResult AdjustPValuesByMethod(
            [Description("The raw p-values of all tests in the family, each between 0 and 1, in any order.")]
            double[] pValues,
            [Description("The correction method to apply.")]
            HypothesisPValueAdjustment method,
            [Description("Significance level (family-wise or false discovery rate) between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            return Adjust(pValues, alpha, method);
        }

        [AdvancedAnalyticsTool("PValueFromTestStatistic")]
        [Description(
            "Converts a test statistic into a p-value for a standard reference distribution when a value was reported without its p-value: StandardNormal (z), StudentT (t, needs degreesOfFreedom), ChiSquare (needs degreesOfFreedom) or F (needs degreesOfFreedom and degreesOfFreedom2). " +
            "TwoSided doubles the smaller tail; for ChiSquare and F the usual test is Greater (upper tail). PValue is the probability of a statistic at least this extreme if the null hypothesis is true.")]
        public HypothesisPValueLookupResult PValueFromTestStatistic(
            [Description("The distribution the statistic is referred to: StandardNormal, StudentT, ChiSquare or F.")]
            HypothesisStatisticDistribution distribution,
            [Description("The value of the test statistic.")]
            double statistic,
            [Description("Degrees of freedom (numerator degrees of freedom for F). Ignored for StandardNormal.")]
            double degreesOfFreedom = 0,
            [Description("Denominator degrees of freedom, used only by F.")]
            double degreesOfFreedom2 = 0,
            [Description("TwoSided, Greater (upper tail) or Less (lower tail).")]
            HypothesisTail tail = HypothesisTail.TwoSided)
        {
            RequireFinite(statistic, nameof(statistic));
            var reference = CreateReference(distribution, degreesOfFreedom, degreesOfFreedom2);

            var lower = reference.DistributionFunction(statistic);
            var upper = reference.ComplementaryDistributionFunction(statistic);

            var p = tail switch
            {
                HypothesisTail.Greater => upper,
                HypothesisTail.Less => lower,
                _ => Math.Min(1, 2 * Math.Min(lower, upper))
            };

            return new HypothesisPValueLookupResult
            {
                Distribution = distribution.ToString(),
                Statistic = statistic,
                PValue = p,
                Tail = tail,
                Interpretation =
                    $"A {distribution} statistic of {Format(statistic)} has a {tail} p-value of {Format(p)}."
            };
        }

        [AdvancedAnalyticsTool("CriticalValueForSignificanceLevel")]
        [Description(
            "Returns the critical value(s) of a standard reference distribution for a significance level: reject the null hypothesis when the test statistic falls beyond them. " +
            "Distribution is StandardNormal (z), StudentT (needs degreesOfFreedom), ChiSquare (needs degreesOfFreedom) or F (needs degreesOfFreedom and degreesOfFreedom2). " +
            "TwoSided returns both bounds, Greater only the upper bound and Less only the lower bound; the unused bound is empty.")]
        public HypothesisCriticalValueResult CriticalValueForSignificanceLevel(
            [Description("The distribution the statistic is referred to: StandardNormal, StudentT, ChiSquare or F.")]
            HypothesisStatisticDistribution distribution,
            [Description("Significance level strictly between 0 and 1. Default 0.05.")]
            double alpha = 0.05,
            [Description("Degrees of freedom (numerator degrees of freedom for F). Ignored for StandardNormal.")]
            double degreesOfFreedom = 0,
            [Description("Denominator degrees of freedom, used only by F.")]
            double degreesOfFreedom2 = 0,
            [Description("TwoSided, Greater (upper tail) or Less (lower tail).")]
            HypothesisTail tail = HypothesisTail.TwoSided)
        {
            RequireAlpha(alpha);
            var reference = CreateReference(distribution, degreesOfFreedom, degreesOfFreedom2);

            double? lower = null;
            double? upper = null;

            switch (tail)
            {
                case HypothesisTail.Greater:
                    upper = reference.InverseDistributionFunction(1 - alpha);
                    break;
                case HypothesisTail.Less:
                    lower = reference.InverseDistributionFunction(alpha);
                    break;
                default:
                    lower = reference.InverseDistributionFunction(alpha / 2);
                    upper = reference.InverseDistributionFunction(1 - alpha / 2);
                    break;
            }

            return new HypothesisCriticalValueResult
            {
                Distribution = distribution.ToString(),
                Alpha = alpha,
                Tail = tail,
                LowerCriticalValue = lower,
                UpperCriticalValue = upper,
                Interpretation =
                    $"At alpha {Format(alpha)} ({tail}) reject the null hypothesis when the {distribution} statistic is below {Format(lower)} or above {Format(upper)} (an empty bound does not apply)."
            };
        }

        private static HypothesisPowerResult OneSample(HypothesisPowerSolveFor solveFor, double? effectSize,
            double? sampleSize, double? power, double alpha, HypothesisTail tail, bool z)
        {
            RequireAlpha(alpha);
            var name = z ? "One-sample z test power analysis" : "One-sample t test power analysis";

            if (solveFor != HypothesisPowerSolveFor.Power)
            {
                RequireGiven(power, nameof(power), "power", solveFor);
                RequireProbability(power!.Value, nameof(power));
            }

            if (solveFor != HypothesisPowerSolveFor.EffectSize)
            {
                RequireGiven(effectSize, nameof(effectSize), "effectSize", solveFor);
                RequirePositive(effectSize!.Value, nameof(effectSize));

                if (effectSize.Value > 100)
                {
                    throw new ArgumentException("An effect size above 100 standard deviations is not plausible.",
                        nameof(effectSize));
                }
            }

            if (solveFor != HypothesisPowerSolveFor.SampleSize)
            {
                RequireGiven(sampleSize, nameof(sampleSize), "sampleSize", solveFor);

                if (double.IsNaN(sampleSize!.Value) || sampleSize.Value < 2 || sampleSize.Value > 1e7)
                {
                    throw new ArgumentException("The sample size must be between 2 and 10 million.",
                        nameof(sampleSize));
                }
            }

            var libraryTail = tail == HypothesisTail.Less ? HypothesisTail.Greater : tail;

            BaseOneSamplePowerAnalysis analysis = z
                ? new ZTestPowerAnalysis(ToOneSample(libraryTail))
                : new TTestPowerAnalysis(ToOneSample(libraryTail));

            analysis.Size = alpha;
            analysis.Power = power ?? 0.8;
            analysis.Effect = effectSize ?? 1;
            analysis.Samples = sampleSize ?? 2;

            Evaluate(() =>
            {
                switch (solveFor)
                {
                    case HypothesisPowerSolveFor.Power:
                        analysis.ComputePower();
                        break;
                    case HypothesisPowerSolveFor.SampleSize:
                        analysis.ComputeSamples();
                        RequireSolved(analysis.Samples, "sample size");
                        analysis.Samples = Math.Ceiling(analysis.Samples);
                        analysis.ComputePower();
                        break;
                    default:
                        analysis.ComputeEffect();
                        RequireSolved(analysis.Effect, "effect size");
                        break;
                }
            });

            return new HypothesisPowerResult
            {
                TestName = name,
                SolvedFor = solveFor,
                Power = solveFor == HypothesisPowerSolveFor.EffectSize ? power!.Value : analysis.Power,
                EffectSize = analysis.Effect,
                Alpha = alpha,
                Tail = tail,
                SampleSize1 = analysis.Samples,
                SampleSize2 = 0,
                TotalSampleSize = analysis.Samples,
                Interpretation = PowerSentence(name, solveFor, analysis.Power, analysis.Effect, alpha, analysis.Samples,
                    0, power)
            };
        }

        private static HypothesisPowerResult TwoSample(HypothesisPowerSolveFor solveFor, double? effectSize,
            double? sampleSize1, double? sampleSize2, double? power, double alpha, HypothesisTail tail,
            double allocationRatio, bool z)
        {
            RequireAlpha(alpha);
            RequirePositive(allocationRatio, nameof(allocationRatio));

            if (allocationRatio > 1000)
            {
                throw new ArgumentException("The allocation ratio must not exceed 1000.", nameof(allocationRatio));
            }

            var name = z ? "Two-sample z test power analysis" : "Two-sample t test power analysis";

            if (solveFor != HypothesisPowerSolveFor.Power)
            {
                RequireGiven(power, nameof(power), "power", solveFor);
                RequireProbability(power!.Value, nameof(power));
            }

            if (solveFor != HypothesisPowerSolveFor.EffectSize)
            {
                RequireGiven(effectSize, nameof(effectSize), "effectSize", solveFor);
                RequirePositive(effectSize!.Value, nameof(effectSize));

                if (effectSize.Value > 100)
                {
                    throw new ArgumentException("An effect size above 100 standard deviations is not plausible.",
                        nameof(effectSize));
                }
            }

            double n1 = 2;
            double n2 = 2;

            if (solveFor != HypothesisPowerSolveFor.SampleSize)
            {
                RequireGiven(sampleSize1, nameof(sampleSize1), "sampleSize1", solveFor);
                n1 = sampleSize1!.Value;
                n2 = sampleSize2 ?? n1 * allocationRatio;

                if (double.IsNaN(n1) || double.IsNaN(n2) || n1 < 2 || n2 < 2 || n1 > 1e7 || n2 > 1e7)
                {
                    throw new ArgumentException("Both group sizes must be between 2 and 10 million.",
                        nameof(sampleSize1));
                }
            }

            var libraryTail = tail == HypothesisTail.Less ? HypothesisTail.Greater : tail;

            BaseTwoSamplePowerAnalysis analysis = z
                ? new TwoSampleZTestPowerAnalysis(ToTwoSample(libraryTail))
                : new TwoSampleTTestPowerAnalysis(ToTwoSample(libraryTail));

            analysis.Size = alpha;
            analysis.Power = power ?? 0.8;
            analysis.Effect = effectSize ?? 1;
            analysis.Samples1 = n1;
            analysis.Samples2 = n2;

            Evaluate(() =>
            {
                switch (solveFor)
                {
                    case HypothesisPowerSolveFor.Power:
                        analysis.ComputePower();
                        break;
                    case HypothesisPowerSolveFor.SampleSize:
                        analysis.ComputeSamples(allocationRatio);
                        RequireSolved(analysis.Samples1, "sample size");
                        analysis.Samples1 = Math.Ceiling(analysis.Samples1);
                        analysis.Samples2 = Math.Ceiling(analysis.Samples1 * allocationRatio);
                        analysis.ComputePower();
                        break;
                    default:
                        analysis.ComputeEffect();
                        RequireSolved(analysis.Effect, "effect size");
                        break;
                }
            });

            return new HypothesisPowerResult
            {
                TestName = name,
                SolvedFor = solveFor,
                Power = solveFor == HypothesisPowerSolveFor.EffectSize ? power!.Value : analysis.Power,
                EffectSize = analysis.Effect,
                Alpha = alpha,
                Tail = tail,
                SampleSize1 = analysis.Samples1,
                SampleSize2 = analysis.Samples2,
                TotalSampleSize = analysis.Samples1 + analysis.Samples2,
                Interpretation = PowerSentence(name, solveFor, analysis.Power, analysis.Effect, alpha,
                    analysis.Samples1,
                    analysis.Samples2, power)
            };
        }

        private static void Evaluate(Action calculation)
        {
            try
            {
                calculation();
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArithmeticException
                                                  or LibraryConvergenceException)
            {
                throw new ArgumentException(
                    "The power calculation cannot be evaluated for this combination of effect size, sample size, power and alpha; try smaller effect or sample sizes or a less extreme power.");
            }
        }

        private static void RequireGiven(double? value, string parameterName, string label,
            HypothesisPowerSolveFor solveFor)
        {
            if (!value.HasValue)
            {
                throw new ArgumentException($"Solving for {solveFor} needs {label} to be given.", parameterName);
            }
        }

        private static void RequireSolved(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentException(
                    $"No {label} within the searchable range reaches the requested power; relax the power, alpha or effect size.");
            }
        }

        private static string PowerSentence(string name, HypothesisPowerSolveFor solveFor, double power, double effect,
            double alpha, double n1, double n2, double? requestedPower)
        {
            var sizes = n2 > 0
                ? $"{Format(n1)} and {Format(n2)} observations in the two groups"
                : $"{Format(n1)} observations";

            return solveFor switch
            {
                HypothesisPowerSolveFor.Power =>
                    $"{name}: with {sizes}, effect size {Format(effect)} and alpha {Format(alpha)} the chance of detecting the effect (power) is {Format(power)}.",
                HypothesisPowerSolveFor.SampleSize =>
                    $"{name}: about {sizes} are needed to detect effect size {Format(effect)} at alpha {Format(alpha)} with the requested power {Format(requestedPower)}; power actually achieved is {Format(power)}.",
                _ =>
                    $"{name}: with {sizes} and alpha {Format(alpha)}, an effect of at least {Format(effect)} standard deviations can be detected with power {Format(requestedPower)}."
            };
        }

        private static HypothesisPValueAdjustmentResult Adjust(double[]? pValues, double alpha,
            HypothesisPValueAdjustment method)
        {
            RequireAlpha(alpha);

            if (pValues == null || pValues.Length == 0)
            {
                throw new ArgumentException("Supply at least one p-value.", nameof(pValues));
            }

            if (pValues.Any(p => double.IsNaN(p) || p < 0 || p > 1))
            {
                throw new ArgumentException(
                    "Every p-value must be a number between 0 and 1; remove NaN and out-of-range values.",
                    nameof(pValues));
            }

            var m = pValues.Length;
            var order = Enumerable.Range(0, m).OrderBy(i => pValues[i]).ToArray();
            var adjusted = new double[m];

            switch (method)
            {
                case HypothesisPValueAdjustment.Bonferroni:
                    for (var i = 0; i < m; i++)
                    {
                        adjusted[i] = Math.Min(1, pValues[i] * m);
                    }

                    break;
                case HypothesisPValueAdjustment.Sidak:
                    for (var i = 0; i < m; i++)
                    {
                        adjusted[i] = 1 - Math.Pow(1 - pValues[i], m);
                    }

                    break;
                case HypothesisPValueAdjustment.Holm:
                {
                    var running = 0.0;

                    for (var rank = 0; rank < m; rank++)
                    {
                        running = Math.Max(running, Math.Min(1, (m - rank) * pValues[order[rank]]));
                        adjusted[order[rank]] = running;
                    }

                    break;
                }
                case HypothesisPValueAdjustment.Hochberg:
                {
                    var running = 1.0;

                    for (var rank = m - 1; rank >= 0; rank--)
                    {
                        running = Math.Min(running, Math.Min(1, (m - rank) * pValues[order[rank]]));
                        adjusted[order[rank]] = running;
                    }

                    break;
                }
                default:
                {
                    var harmonic = method == HypothesisPValueAdjustment.BenjaminiYekutieli
                        ? Enumerable.Range(1, m).Sum(i => 1.0 / i)
                        : 1.0;

                    var running = 1.0;

                    for (var rank = m - 1; rank >= 0; rank--)
                    {
                        running = Math.Min(running, Math.Min(1, pValues[order[rank]] * m * harmonic / (rank + 1)));
                        adjusted[order[rank]] = running;
                    }

                    break;
                }
            }

            var reject = adjusted.Select(p => p < alpha).ToArray();
            var count = reject.Count(r => r);

            var goal = method is HypothesisPValueAdjustment.BenjaminiHochberg
                or HypothesisPValueAdjustment.BenjaminiYekutieli
                ? "false discovery rate"
                : "family-wise error rate";

            return new HypothesisPValueAdjustmentResult
            {
                Method = method.ToString(),
                OriginalPValues = pValues,
                AdjustedPValues = adjusted,
                RejectNull = reject,
                Alpha = alpha,
                RejectedCount = count,
                Interpretation =
                    $"{method} correction over {m} tests controls the {goal} at {Format(alpha)}: {count} of {m} tests remain significant after adjustment."
            };
        }

        private static void RequireDegrees(double value, string parameterName)
        {
            if (value > 1e7)
            {
                throw new ArgumentException(
                    "Degrees of freedom above 10 million are not supported; use StandardNormal instead.",
                    parameterName);
            }
        }

        private static IUnivariateDistribution<double> CreateReference(
            HypothesisStatisticDistribution distribution, double degreesOfFreedom, double degreesOfFreedom2)
        {
            switch (distribution)
            {
                case HypothesisStatisticDistribution.StandardNormal:
                    return NormalDistribution.Standard;
                case HypothesisStatisticDistribution.StudentT:
                    RequirePositive(degreesOfFreedom, nameof(degreesOfFreedom));
                    RequireDegrees(degreesOfFreedom, nameof(degreesOfFreedom));

                    return new TDistribution(degreesOfFreedom);
                case HypothesisStatisticDistribution.ChiSquare:
                    RequirePositive(degreesOfFreedom, nameof(degreesOfFreedom));
                    RequireDegrees(degreesOfFreedom, nameof(degreesOfFreedom));

                    return new ChiSquareDistribution((int)Math.Round(degreesOfFreedom));
                default:
                    RequirePositive(degreesOfFreedom, nameof(degreesOfFreedom));
                    RequirePositive(degreesOfFreedom2, nameof(degreesOfFreedom2));
                    RequireDegrees(degreesOfFreedom, nameof(degreesOfFreedom));
                    RequireDegrees(degreesOfFreedom2, nameof(degreesOfFreedom2));

                    return new FDistribution((int)Math.Round(degreesOfFreedom), (int)Math.Round(degreesOfFreedom2));
            }
        }
    }
}