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
using Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests
{
    [AdvancedAnalyticsToolGroup("HypothesisTests")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class HypothesisTestSelectionTools
    {
        [AdvancedAnalyticsTool("RecommendHypothesisTest")]
        [Description(
            "Chooses which hypothesis test tool to call from simple facts about the question and the data, so no statistical reasoning is needed. " +
            "State the design (one sample against a value, two independent groups, paired data, three or more groups, factorial, association of two categorical variables, rater agreement, comparing variances, normality, distribution fit or outliers) and the data kind (Continuous, Ordinal, Binary or Categorical). " +
            "Optionally add whether the data look normal, the size of the smallest group and whether the group variances are equal. PrimaryTool is the tool name to call next, AlternativeTools are sensible substitutes, Reason explains the choice in plain language, Assumptions lists what must hold, and CompanionTools are checks or effect size tools worth running alongside.")]
        public HypothesisTestRecommendation RecommendHypothesisTest(
            [Description("What is being asked, for example TwoIndependentGroups or TwoPairedOrMatchedGroups.")]
            HypothesisDesign design,
            [Description(
                "What the measured variable is: Continuous (amounts, durations), Ordinal (ranks or ordered scores), Binary (yes/no) or Categorical (unordered labels).")]
            HypothesisDataKind dataKind = HypothesisDataKind.Continuous,
            [Description(
                "Whether the data (or the differences for paired data) look roughly normal, for example from ShapiroWilkNormalityTest. Leave empty when unknown.")]
            bool? dataLookNormal = null,
            [Description(
                "Number of observations in the smallest group (or the sample size for one sample). Leave empty when unknown.")]
            int? smallestGroupSize = null,
            [Description(
                "Whether the group variances look equal, for example from LeveneTest. Leave empty when unknown.")]
            bool? variancesEqual = null,
            [Description(
                "Number of groups being compared, at least 2. Used for comparing variances and for group comparisons.")]
            int groups = 2)
        {
            if (groups < 2)
            {
                throw new ArgumentException("At least 2 groups are needed to compare.", nameof(groups));
            }

            if (smallestGroupSize.HasValue && smallestGroupSize.Value < 1)
            {
                throw new ArgumentException("The group size must be at least 1 when given.", nameof(smallestGroupSize));
            }

            var large = smallestGroupSize is >= 30;
            var small = smallestGroupSize is < 30;
            var parametricOk = dataLookNormal == true || (dataLookNormal == null && large);
            var kind = dataKind;

            switch (design)
            {
                case HypothesisDesign.OneSampleAgainstValue:
                    return OneSample(kind, parametricOk, large);
                case HypothesisDesign.TwoIndependentGroups:
                    return TwoIndependent(kind, parametricOk, small, variancesEqual);
                case HypothesisDesign.TwoPairedOrMatchedGroups:
                    return Paired(kind, parametricOk);
                case HypothesisDesign.ThreeOrMoreIndependentGroups:
                    return Multiple(kind, parametricOk, variancesEqual, groups);
                case HypothesisDesign.TwoWayFactorial:
                    return Recommend("TwoWayAnova",
                        new[] { "KruskalWallisTest", "OneWayAnovaFromLabels" },
                        "Two crossed factors on a continuous outcome call for two-way ANOVA, which tests each factor and their interaction. There is no rank-based two-way test in this tool set; if the data are far from normal, transform them or run KruskalWallisTest on each factor separately.",
                        new[]
                        {
                            "Balanced design: every combination of levels has the same number of replicates.",
                            "Residuals roughly normal with similar variance across cells."
                        },
                        new[] { "LeveneTest", "ShapiroWilkNormalityTest" });
                case HypothesisDesign.AssociationBetweenTwoCategoricalVariables:
                    return Association(smallestGroupSize);
                case HypothesisDesign.AgreementBetweenTwoRaters:
                    return kind == HypothesisDataKind.Ordinal
                        ? Recommend("WeightedKappaTest", new[] { "CohensKappaTest", "BowkerSymmetryTest" },
                            "Ordered ratings deserve weighted kappa, which gives partial credit to near misses.",
                            new[] { "Both raters rated the same cases on the same ordered scale." },
                            new[] { "BowkerSymmetryTest" })
                        : Recommend("CohensKappaTest",
                            new[] { "WeightedKappaTest", "McNemarTest", "BowkerSymmetryTest" },
                            "Agreement of two raters on unordered categories is measured by Cohen's kappa, which corrects for chance agreement.",
                            new[] { "Both raters rated the same cases using the same categories." },
                            new[] { "McNemarTest" });
                case HypothesisDesign.ComparingVariances:
                    return Variances(parametricOk, groups);
                case HypothesisDesign.NormalityOfOneSample:
                    return Recommend("ShapiroWilkNormalityTest",
                        new[] { "AndersonDarlingNormalityTest", "LillieforsNormalityTest", "JarqueBeraNormalityTest" },
                        "Shapiro-Wilk is the most powerful general normality test for samples up to a few thousand; Anderson-Darling emphasises the tails and Jarque-Bera suits large samples.",
                        new[] { "Independent observations." }, new[] { "GrubbsOutlierTest" });
                case HypothesisDesign.DistributionFitOfOneSample:
                    return Recommend("KolmogorovSmirnovOneSampleTest",
                        new[]
                        {
                            "ChiSquareDistributionFitTest", "AndersonDarlingNormalityTest", "LillieforsNormalityTest"
                        },
                        "Kolmogorov-Smirnov compares the sample with a fully specified reference distribution. If the parameters were estimated from the same sample, use LillieforsNormalityTest for normality instead.",
                        new[] { "Reference distribution parameters fixed in advance, continuous data." },
                        Array.Empty<string>());
                default:
                    return Recommend("GrubbsOutlierTest", new[] { "ShapiroWilkNormalityTest" },
                        "Grubbs tests whether the most extreme value in a roughly normal sample is an outlier.",
                        new[] { "The bulk of the data is normal.", "Only one outlier at a time is tested." },
                        new[] { "ShapiroWilkNormalityTest" });
            }
        }

        private static HypothesisTestRecommendation Recommend(string primary, string[] alternatives, string reason,
            string[] assumptions, string[] companions)
        {
            return new HypothesisTestRecommendation
            {
                PrimaryTool = primary,
                AlternativeTools = alternatives,
                Reason = reason,
                Assumptions = assumptions,
                CompanionTools = companions
            };
        }

        private static HypothesisTestRecommendation OneSample(HypothesisDataKind kind, bool parametricOk, bool large)
        {
            switch (kind)
            {
                case HypothesisDataKind.Binary:
                    return large
                        ? Recommend("OneProportionZTest", new[] { "BinomialExactTest" },
                            "A yes/no outcome in a large sample tests a proportion; the normal approximation is adequate.",
                            new[]
                            {
                                "Independent trials.", "At least 5 expected successes and failures under the null."
                            },
                            new[] { "ProportionConfidenceInterval" })
                        : Recommend("BinomialExactTest", new[] { "OneProportionZTest" },
                            "A yes/no outcome in a small or unknown-size sample is best tested exactly with the binomial distribution.",
                            new[] { "Independent trials." }, new[] { "ProportionConfidenceInterval" });
                case HypothesisDataKind.Categorical:
                    return Recommend("ChiSquareGoodnessOfFitTest", new[] { "BinomialExactTest" },
                        "Category counts are compared with expected proportions by a chi-square goodness-of-fit test.",
                        new[] { "Independent observations.", "Expected counts of about 5 or more per category." },
                        Array.Empty<string>());
                case HypothesisDataKind.Ordinal:
                    return Recommend("WilcoxonSignedRankTest", new[] { "SignTestOneSample" },
                        "Ordinal data support ranking but not averaging, so a rank test of the median is appropriate; the sign test is the most assumption-free fallback.",
                        new[] { "A roughly symmetric distribution of differences for the Wilcoxon test." },
                        new[] { "MedianConfidenceInterval" });
                default:
                    return parametricOk
                        ? Recommend("OneSampleTTest", new[] { "OneSampleZTest", "WilcoxonSignedRankTest" },
                            "A continuous, roughly normal (or large) sample is compared with a value using the one-sample t test.",
                            new[]
                            {
                                "Independent observations.", "Approximately normal data unless the sample is large."
                            },
                            new[] { "ShapiroWilkNormalityTest", "MeanConfidenceInterval" })
                        : Recommend("WilcoxonSignedRankTest", new[] { "SignTestOneSample", "OneSampleTTest" },
                            "Small or non-normal continuous data are safer with a rank test of the median than with the t test.",
                            new[] { "A roughly symmetric distribution." },
                            new[] { "MedianConfidenceInterval", "ShapiroWilkNormalityTest" });
            }
        }

        private static HypothesisTestRecommendation TwoIndependent(HypothesisDataKind kind, bool parametricOk,
            bool small, bool? variancesEqual)
        {
            switch (kind)
            {
                case HypothesisDataKind.Binary:
                    return small
                        ? Recommend("FisherExactTest", new[] { "TwoProportionZTest", "ChiSquareIndependenceTest" },
                            "Two small groups with a yes/no outcome are tested exactly on the 2x2 table.",
                            new[] { "Independent groups." }, Array.Empty<string>())
                        : Recommend("TwoProportionZTest", new[] { "ChiSquareIndependenceTest", "FisherExactTest" },
                            "Comparing the rate of a yes/no outcome in two independent groups uses the two-proportion z test.",
                            new[]
                            {
                                "Independent groups.", "At least 5 expected successes and failures in each group."
                            },
                            new[] { "ProportionConfidenceInterval" });
                case HypothesisDataKind.Categorical:
                    return Recommend("ChiSquareIndependenceTest", new[] { "FisherExactTest" },
                        "Comparing category distributions between two groups is a test of independence on the group by category table.",
                        new[] { "Independent groups.", "Expected counts of about 5 or more per cell." },
                        Array.Empty<string>());
                case HypothesisDataKind.Ordinal:
                    return Recommend("MannWhitneyUTest", new[] { "KolmogorovSmirnovTwoSampleTest" },
                        "Ordinal data are compared between two independent groups with the rank-based Mann-Whitney test.",
                        new[] { "Independent groups." }, Array.Empty<string>());
                default:
                    return parametricOk
                        ? Recommend("TwoSampleTTest",
                            new[] { "MannWhitneyUTest", "TwoSampleZTest", "KolmogorovSmirnovTwoSampleTest" },
                            variancesEqual == true
                                ? "Continuous, roughly normal groups with similar variances are compared with the two-sample t test; Welch's version (the default) is still safe."
                                : "Continuous, roughly normal groups are compared with Welch's two-sample t test, which does not need equal variances.",
                            new[] { "Independent groups.", "Approximately normal data unless the groups are large." },
                            new[] { "LeveneTest", "EffectSizeTwoSamples", "ShapiroWilkNormalityTest" })
                        : Recommend("MannWhitneyUTest", new[] { "TwoSampleTTest", "KolmogorovSmirnovTwoSampleTest" },
                            "Small or non-normal continuous groups are safer with the rank-based Mann-Whitney test than with the t test.",
                            new[] { "Independent groups." },
                            new[] { "EffectSizeTwoSamples", "ShapiroWilkNormalityTest" });
            }
        }

        private static HypothesisTestRecommendation Paired(HypothesisDataKind kind, bool parametricOk)
        {
            switch (kind)
            {
                case HypothesisDataKind.Binary:
                    return Recommend("McNemarTest", new[] { "BinomialExactTest" },
                        "Paired yes/no outcomes on the same cases are compared with McNemar's test on the discordant pairs.",
                        new[] { "The same cases measured twice." }, Array.Empty<string>());
                case HypothesisDataKind.Categorical:
                    return Recommend("BowkerSymmetryTest",
                        new[] { "StuartMaxwellHomogeneityTest", "BhapkarHomogeneityTest" },
                        "Paired categorical ratings are checked for a directional shift with Bowker's symmetry test or, for the overall distribution, marginal homogeneity tests.",
                        new[] { "The same cases classified twice into the same categories." },
                        new[] { "CohensKappaTest" });
                case HypothesisDataKind.Ordinal:
                    return Recommend("WilcoxonSignedRankPairedTest", new[] { "SignTestPaired" },
                        "Paired ordinal data use the Wilcoxon signed-rank test; the sign test needs even fewer assumptions.",
                        new[] { "The same cases measured twice." }, Array.Empty<string>());
                default:
                    return parametricOk
                        ? Recommend("PairedTTest", new[] { "WilcoxonSignedRankPairedTest", "SignTestPaired" },
                            "Continuous paired measurements whose differences are roughly normal are compared with the paired t test.",
                            new[]
                            {
                                "The same cases measured twice.",
                                "Differences approximately normal unless there are many pairs."
                            },
                            new[] { "ShapiroWilkNormalityTest", "EffectSizeTwoSamples" })
                        : Recommend("WilcoxonSignedRankPairedTest", new[] { "SignTestPaired", "PairedTTest" },
                            "Paired measurements with non-normal or few differences are safer with the Wilcoxon signed-rank test.",
                            new[]
                            {
                                "The same cases measured twice.", "A roughly symmetric distribution of differences."
                            },
                            new[] { "ShapiroWilkNormalityTest" });
            }
        }

        private static HypothesisTestRecommendation Multiple(HypothesisDataKind kind, bool parametricOk,
            bool? variancesEqual, int groups)
        {
            if (kind == HypothesisDataKind.Binary || kind == HypothesisDataKind.Categorical)
            {
                return Recommend("ChiSquareIndependenceTest", new[] { "ChiSquareGoodnessOfFitTest" },
                    $"Comparing a categorical or yes/no outcome across {groups} groups is a chi-square test of independence on the group by outcome table.",
                    new[] { "Independent groups.", "Expected counts of about 5 or more per cell." },
                    Array.Empty<string>());
            }

            if (kind == HypothesisDataKind.Continuous && parametricOk)
            {
                return Recommend("OneWayAnova", new[] { "KruskalWallisTest", "OneWayAnovaFromLabels" },
                    variancesEqual == false
                        ? $"Continuous roughly normal data in {groups} groups suit one-way ANOVA, but unequal variances were reported, so check LeveneTest and consider KruskalWallisTest."
                        : $"Continuous roughly normal data in {groups} groups are compared with one-way ANOVA.",
                    new[] { "Independent groups.", "Approximately normal groups with similar variances." },
                    new[] { "LeveneTest", "ShapiroWilkNormalityTest", "AdjustPValuesHolm" });
            }

            return Recommend("KruskalWallisTest", new[] { "OneWayAnova" },
                $"Ordinal, small or non-normal data in {groups} groups are compared with the rank-based Kruskal-Wallis test.",
                new[] { "Independent groups.", "At least about 5 values per group for the chi-square approximation." },
                new[] { "AdjustPValuesHolm" });
        }

        private static HypothesisTestRecommendation Association(int? smallestGroupSize)
        {
            var small = smallestGroupSize is < 30;

            return small
                ? Recommend("FisherExactTest", new[] { "ChiSquareIndependenceTest" },
                    "With a small sample and a 2x2 table the exact test is safer; for larger tables use ChiSquareIndependenceTest with merged sparse categories.",
                    new[] { "Independent observations, a 2x2 table for FisherExactTest." },
                    Array.Empty<string>())
                : Recommend("ChiSquareIndependenceTest", new[] { "FisherExactTest" },
                    "Association between two categorical variables is tested with the chi-square test of independence, which also reports Cramer's V.",
                    new[] { "Independent observations.", "Expected counts of about 5 or more per cell." },
                    Array.Empty<string>());
        }

        private static HypothesisTestRecommendation Variances(bool normal, int groups)
        {
            if (groups == 2 && normal)
            {
                return Recommend("FTestTwoVariances", new[] { "LeveneTest", "BartlettTest" },
                    "For two roughly normal groups the F test compares variances directly.",
                    new[]
                    {
                        "Independent groups.", "Approximately normal data; the F test is sensitive to non-normality."
                    },
                    new[] { "ShapiroWilkNormalityTest" });
            }

            return normal
                ? Recommend("BartlettTest", new[] { "LeveneTest" },
                    $"For {groups} roughly normal groups Bartlett's test is the most powerful check of equal variances.",
                    new[] { "Independent groups.", "Approximately normal data." }, new[] { "ShapiroWilkNormalityTest" })
                : Recommend("LeveneTest", new[] { "BartlettTest", "FTestTwoVariances" },
                    "Levene's test with the median (Brown-Forsythe) is robust to non-normal data when comparing variances.",
                    new[] { "Independent groups." }, Array.Empty<string>());
        }
    }
}