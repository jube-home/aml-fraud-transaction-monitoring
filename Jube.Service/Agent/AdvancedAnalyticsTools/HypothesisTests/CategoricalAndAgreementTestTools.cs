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
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Testing;
using Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models;
using static Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.HypothesisTestSupport;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests
{
    [AdvancedAnalyticsToolGroup("HypothesisTests")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class CategoricalAndAgreementTestTools
    {
        [AdvancedAnalyticsTool("ChiSquareGoodnessOfFitTest")]
        [Description(
            "Chi-square goodness-of-fit test: do observed category counts match expected proportions, for example whether transactions are spread evenly over weekdays or a first-digit distribution follows a law. " +
            "Statistic is the sum of (observed - expected)^2 / expected over the categories; DegreesOfFreedom is categories - 1 minus any parameters estimated from the data. EffectSize is Cohen's w, sqrt(chi-square / n) (about 0.1 small, 0.3 medium, 0.5 large). " +
            "Leave expectedProportions empty for an even split. The approximation needs an expected count of about 5 or more per category, and a warning lists any that fall short.")]
        public HypothesisTestResult ChiSquareGoodnessOfFitTest(
            [Description(
                "Observed count in each category, at least 2 categories, non-negative, at least one above zero.")]
            double[] observedCounts,
            [Description(
                "Expected share (or expected count) of each category in the same order; scaled to sum to the observed total. Leave empty for equal shares.")]
            double[]? expectedProportions = null,
            [Description(
                "Number of distribution parameters estimated from the same data, which reduces the degrees of freedom. Default 0.")]
            int estimatedParameters = 0,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(observedCounts, nameof(observedCounts), 2);
            RequireAlpha(alpha);

            if (observedCounts.Any(c => c < 0))
            {
                throw new ArgumentException("Counts must not be negative.", nameof(observedCounts));
            }

            var total = observedCounts.Sum();

            if (total <= 0)
            {
                throw new ArgumentException("Every observed count is zero.", nameof(observedCounts));
            }

            double[] expected;

            if (expectedProportions == null || expectedProportions.Length == 0)
            {
                expected = observedCounts.Select(_ => total / observedCounts.Length).ToArray();
            }
            else
            {
                if (expectedProportions.Length != observedCounts.Length)
                {
                    throw new ArgumentException(
                        "Give one expected proportion for every observed count, in the same order.",
                        nameof(expectedProportions));
                }

                if (expectedProportions.Any(p => double.IsNaN(p) || double.IsInfinity(p) || p <= 0))
                {
                    throw new ArgumentException("Expected proportions must be finite and greater than zero.",
                        nameof(expectedProportions));
                }

                var scale = total / expectedProportions.Sum();
                expected = expectedProportions.Select(p => p * scale).ToArray();
            }

            if (estimatedParameters < 0 || estimatedParameters > observedCounts.Length - 2)
            {
                throw new ArgumentException(
                    "The number of estimated parameters must be between 0 and categories - 2 so that at least 1 degree of freedom remains.",
                    nameof(estimatedParameters));
            }

            var degrees = observedCounts.Length - 1 - estimatedParameters;
            var test = Guard(() => new ChiSquareTest(expected, observedCounts, degrees), nameof(observedCounts));
            var effect = Math.Sqrt(test.Statistic / total);

            var warnings = new List<string>();

            if (expected.Any(e => e < 5))
            {
                warnings.Add(
                    "At least one expected count is below 5, so the chi-square approximation is unreliable; merge sparse categories.");
            }

            var result = Build("Chi-square goodness-of-fit test", "chi-square", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, "the observed counts follow the expected proportions",
                "the observed counts do not follow the expected proportions", degrees,
                detail: $"Cohen's w = {Format(effect)} ({Magnitude(effect)}).", warnings: warnings);

            result.EffectSize = effect;
            result.EffectSizeName = "Cohen's w";
            result.SampleSize1 = (int)Math.Round(total);

            return result;
        }

        [AdvancedAnalyticsTool("ChiSquareIndependenceTest")]
        [Description(
            "Chi-square test of independence for a contingency table of counts: are the row variable and the column variable associated, for example fraud flag by channel. " +
            "Each row is one category of the first variable and each column one category of the second; cells are counts. Statistic is chi-square, DegreesOfFreedom is (rows-1)*(columns-1). " +
            "EffectSize is Cramer's V (0 no association to 1 perfect; for a 2x2 table it equals phi). Yates' continuity correction can be applied to a 2x2 table only. " +
            "Warnings list expected counts below 5, in which case FisherExactTest (2x2) or merged categories are safer.")]
        public HypothesisTestResult ChiSquareIndependenceTest(
            [Description(
                "Table of counts: one array per row, all rows the same length, at least 2 rows and 2 columns, non-negative.")]
            int[][] table,
            [Description("Apply Yates' continuity correction. Only allowed for a 2x2 table. Default false.")]
            bool yatesCorrection = false,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table, nameof(table), 2, 2);
            RequireAlpha(alpha);

            var rows = table.Length;
            var columns = table[0].Length;
            var total = table.Sum(r => r.Sum(c => (double)c));
            var rowTotals = table.Select(r => r.Sum(c => (double)c)).ToArray();
            var columnTotals = Enumerable.Range(0, columns).Select(j => table.Sum(r => (double)r[j])).ToArray();

            if (rowTotals.Any(t => t == 0) || columnTotals.Any(t => t == 0))
            {
                throw new ArgumentException(
                    "A row or column of the table sums to zero, so its expected count is zero. Remove the empty category.",
                    nameof(table));
            }

            if (yatesCorrection && (rows != 2 || columns != 2))
            {
                throw new ArgumentException("Yates' correction is only defined for a 2x2 table.",
                    nameof(yatesCorrection));
            }

            ChiSquareTest test;
            var expected = new double[rows * columns];
            var observed = new double[rows * columns];
            var minExpected = double.MaxValue;

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    expected[i * columns + j] = rowTotals[i] * columnTotals[j] / total;
                    observed[i * columns + j] = table[i][j];
                    minExpected = Math.Min(minExpected, expected[i * columns + j]);
                }
            }

            var degrees = (rows - 1) * (columns - 1);

            if (rows == 2 && columns == 2)
            {
                test = new ChiSquareTest(new ConfusionMatrix(ToRectangular(table)), yatesCorrection);
            }
            else
            {
                test = new ChiSquareTest(expected, observed, degrees);
            }

            var v = Math.Sqrt(test.Statistic / (total * Math.Min(rows - 1, columns - 1)));

            var warnings = new List<string>();

            if (expected.Any(e => e < 5))
            {
                warnings.Add(
                    $"The smallest expected count is {Format(minExpected)}, below 5: the chi-square approximation is unreliable, use FisherExactTest for 2x2 or merge categories.");
            }

            var result = Build(
                yatesCorrection ? "Chi-square test of independence (Yates)" : "Chi-square test of independence",
                "chi-square", test.Statistic, test.PValue, HypothesisTail.Greater, alpha,
                "the row and column variables are independent", "the row and column variables are associated",
                degrees, detail: $"Cramer's V = {Format(v)} ({Magnitude(v)} association).", warnings: warnings);

            result.EffectSize = v;
            result.EffectSizeName = "Cramer's V";
            result.SampleSize1 = (int)Math.Round(total);

            return result;
        }

        [AdvancedAnalyticsTool("FisherExactTest")]
        [Description(
            "Fisher's exact test for a 2x2 table of counts [[a, b], [c, d]] (rows are the two groups, columns are outcome yes then no), valid for any sample size including tiny or lopsided tables where the chi-square test fails. " +
            "Statistic is the top-left count a, EstimatedValue is the sample odds ratio (a*d)/(b*c) (empty when a zero cell makes it infinite). " +
            "Greater tests whether a is larger than independence predicts (odds ratio above 1); Less whether it is smaller. The two-sided p-value adds every table no more likely than the observed one.")]
        public HypothesisTestResult FisherExactTest(
            [Description("A 2x2 table of non-negative counts: [[a, b], [c, d]].")]
            int[][] table,
            [Description("TwoSided, Greater (top-left cell above expectation, odds ratio above 1) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table, nameof(table), 2, 2);
            RequireAlpha(alpha);
            Require2X2(table, nameof(table));

            var test = Guard(() => new FisherExactTest(new ConfusionMatrix(ToRectangular(table)), ToOneSample(tail)),
                nameof(table));

            var a = (double)table[0][0];
            var b = (double)table[0][1];
            var c = (double)table[1][0];
            var d = (double)table[1][1];
            double? odds = b * c > 0 ? a * d / (b * c) : null;

            var warnings = new List<string>();

            if (odds == null)
            {
                warnings.Add("A cell is zero so the sample odds ratio is undefined or infinite.");
            }

            var result = Build("Fisher's exact test", "a", test.Statistic, test.PValue, tail, alpha,
                "the two groups have the same odds of the outcome",
                TailPhrase(tail, "the odds of the outcome in group 1", "differ from those in group 2",
                    "are higher than in group 2", "are lower than in group 2", string.Empty),
                detail: odds.HasValue ? $"The sample odds ratio is {Format(odds.Value)}." : string.Empty,
                warnings: warnings);

            result.EstimatedValue = odds;
            result.NullValue = 1;
            result.SampleSize1 = (int)(a + b + c + d);

            return result;
        }

        [AdvancedAnalyticsTool("McNemarTest")]
        [Description(
            "McNemar's test for paired binary outcomes in a 2x2 table [[both yes, first yes only], [second yes only, both no]], for example the same cases judged by two rules or the same customers before and after a change. " +
            "It uses only the discordant cells b (first yes only) and c (second yes only): Statistic is (b - c)^2 / (b + c) (with Yates' correction (|b - c| - 0.5)^2 / (b + c) if requested) with 1 degree of freedom. " +
            "A small p-value means one rule says yes more often than the other. Fewer than about 25 discordant pairs makes the chi-square approximation poor, and a warning says so; with no discordant pairs the test is undefined.")]
        public HypothesisTestResult McNemarTest(
            [Description("A 2x2 table of paired counts: [[both yes, first yes only], [second yes only, both no]].")]
            int[][] table,
            [Description("Apply the continuity correction. Default false.")]
            bool yatesCorrection = false,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table, nameof(table), 2, 2);
            RequireAlpha(alpha);
            Require2X2(table, nameof(table));

            var b = table[0][1];
            var c = table[1][0];

            if (b + c == 0)
            {
                throw new ArgumentException(
                    "There are no discordant pairs (both off-diagonal cells are zero), so McNemar's statistic is undefined.",
                    nameof(table));
            }

            var test = Guard(() => new McNemarTest(new ConfusionMatrix(ToRectangular(table)), yatesCorrection),
                nameof(table));

            var warnings = new List<string>();

            if (b + c < 25)
            {
                warnings.Add(
                    "Fewer than 25 discordant pairs: use BinomialExactTest on b out of b + c with probability 0.5 for an exact result.");
            }

            var result = Build("McNemar test", "chi-square", test.Statistic, test.PValue, HypothesisTail.Greater,
                alpha, "the two paired outcomes have the same rate of yes",
                "the two paired outcomes have different rates of yes", 1,
                detail: $"{b} cases were yes only on the first measurement and {c} only on the second.",
                warnings: warnings);

            result.SampleSize1 = table.Sum(r => r.Sum());

            return result;
        }

        [AdvancedAnalyticsTool("BowkerSymmetryTest")]
        [Description(
            "Bowker's test of symmetry for a square table of paired categorical ratings (the multi-category generalisation of McNemar's test): is the probability of moving from category i to j the same as from j to i. " +
            "Rows are the first rating and columns the second rating of the same cases, over the same categories. Statistic is chi-square with k(k-1)/2 degrees of freedom. " +
            "A pair of off-diagonal cells that are both zero makes the statistic undefined for that pair and is rejected with a message naming the categories.")]
        public HypothesisTestResult BowkerSymmetryTest(
            [Description(
                "Square table of non-negative counts, at least 2x2: rows first rating, columns second rating.")]
            int[][] table,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table, nameof(table), 2, 2, true);
            RequireAlpha(alpha);

            for (var i = 0; i < table.Length; i++)
            {
                for (var j = 0; j < i; j++)
                {
                    if (table[i][j] + table[j][i] == 0)
                    {
                        throw new ArgumentException(
                            $"Categories {j} and {i} never change into each other in either direction, which makes Bowker's statistic undefined for that pair. Merge or remove one of those categories and call again.",
                            nameof(table));
                    }
                }
            }

            var test = Guard(() => new BowkerTest(new GeneralConfusionMatrix(ToRectangular(table))), nameof(table));

            var result = Build("Bowker symmetry test", "chi-square", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, "the table is symmetric about its diagonal",
                "the table is not symmetric: shifts between categories are more common in one direction",
                test.DegreesOfFreedom);

            result.SampleSize1 = table.Sum(r => r.Sum());

            return result;
        }

        [AdvancedAnalyticsTool("StuartMaxwellHomogeneityTest")]
        [Description(
            "Stuart-Maxwell test of marginal homogeneity for a square table of paired categorical ratings: do the two ratings (rows versus columns) have the same overall distribution over the categories. " +
            "It is the multi-category generalisation of McNemar's test focused on the margins rather than symmetry. Statistic is chi-square with k-1 degrees of freedom and needs at least 3 categories. " +
            "The table must not have a zero row and matching zero column together, or the covariance matrix is singular.")]
        public HypothesisTestResult StuartMaxwellHomogeneityTest(
            [Description(
                "Square table of non-negative counts, at least 3x3: rows first rating, columns second rating.")]
            int[][] table,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table, nameof(table), 3, 3, true);
            RequireAlpha(alpha);

            var test = Guard(() => new StuartMaxwellTest(new GeneralConfusionMatrix(ToRectangular(table))),
                nameof(table));

            var result = Build("Stuart-Maxwell marginal homogeneity test", "chi-square", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, "both ratings have the same distribution over the categories",
                "the two ratings distribute cases differently over the categories", test.DegreesOfFreedom);

            result.SampleSize1 = table.Sum(r => r.Sum());

            return result;
        }

        [AdvancedAnalyticsTool("BhapkarHomogeneityTest")]
        [Description(
            "Bhapkar test of marginal homogeneity for a square table of paired categorical ratings, an alternative to StuartMaxwellHomogeneityTest that is asymptotically more powerful. " +
            "It asks whether rows and columns have the same overall category distribution. Statistic is chi-square with k-1 degrees of freedom and needs at least 3 categories.")]
        public HypothesisTestResult BhapkarHomogeneityTest(
            [Description(
                "Square table of non-negative counts, at least 3x3: rows first rating, columns second rating.")]
            int[][] table,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table, nameof(table), 3, 3, true);
            RequireAlpha(alpha);

            var test = Guard(() => new BhapkarTest(new GeneralConfusionMatrix(ToRectangular(table))), nameof(table));

            var result = Build("Bhapkar marginal homogeneity test", "chi-square", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, "both ratings have the same distribution over the categories",
                "the two ratings distribute cases differently over the categories", test.DegreesOfFreedom);

            result.SampleSize1 = table.Sum(r => r.Sum());

            return result;
        }

        [AdvancedAnalyticsTool("CohensKappaTest")]
        [Description(
            "Measures and tests agreement between two raters (or two rules) classifying the same cases into the same categories, corrected for the agreement expected by chance. " +
            "Rows are the first rater's category and columns the second rater's, so the diagonal holds agreements. EstimatedValue is Cohen's kappa (1 perfect, 0 no better than chance, negative worse than chance); the confidence bounds are for kappa at 1-alpha. " +
            "With hypothesizedKappa 0 the p-value uses the standard error under no agreement; with another value the general standard error is used. Interpretation names the Landis and Koch band (slight, fair, moderate, substantial, almost perfect).")]
        public HypothesisTestResult CohensKappaTest(
            [Description("Square table of non-negative counts, at least 2x2: rows first rater, columns second rater.")]
            int[][] table,
            [Description("The kappa stated by the null hypothesis. Default 0 (agreement no better than chance).")]
            double hypothesizedKappa = 0,
            [Description(
                "Greater (default) tests for agreement above the hypothesised value, TwoSided any difference, Less below.")]
            HypothesisTail tail = HypothesisTail.Greater,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table, nameof(table), 2, 2, true);
            RequireAlpha(alpha);

            if (double.IsNaN(hypothesizedKappa) || hypothesizedKappa < -1 || hypothesizedKappa > 1)
            {
                throw new ArgumentException("Kappa lies between -1 and 1.", nameof(hypothesizedKappa));
            }

            var matrix = new GeneralConfusionMatrix(ToRectangular(table));
            var kappa = matrix.Kappa;

            if (double.IsNaN(kappa))
            {
                throw new ArgumentException(
                    "Kappa is undefined because the raters used only one category, so chance agreement is already 1.",
                    nameof(table));
            }

            var test = Guard(() => new KappaTest(matrix, hypothesizedKappa, ToOneSample(tail)), nameof(table));

            var critical = NormalDistribution.Standard
                .InverseDistributionFunction(1 - alpha / 2);

            var lower = kappa - critical * matrix.StandardError;
            var upper = kappa + critical * matrix.StandardError;

            var result = Build("Cohen's kappa test", "z", test.Statistic, test.PValue, tail, alpha,
                $"kappa equals {Format(hypothesizedKappa)}",
                TailPhrase(tail, "kappa", "differs from", "is greater than", "is less than",
                    Format(hypothesizedKappa)),
                detail:
                $"Kappa is {Format(kappa)}, {KappaBand(kappa)} agreement; the {Format(1 - alpha)} interval is {Format(lower)} to {Format(upper)}.");

            result.EstimatedValue = kappa;
            result.NullValue = hypothesizedKappa;
            result.EffectSize = kappa;
            result.EffectSizeName = "Cohen's kappa";
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = lower;
            result.ConfidenceUpper = upper;
            result.SampleSize1 = table.Sum(r => r.Sum());

            return result;
        }

        [AdvancedAnalyticsTool("WeightedKappaTest")]
        [Description(
            "Weighted kappa agreement test for ordered categories, where near misses count as partial agreement: use it for ratings on an ordinal scale such as risk bands 1 to 5. " +
            "Linear weights give credit that falls in proportion to the distance between categories, Quadratic weights penalise large disagreements more heavily and match the intraclass correlation. " +
            "Rows are the first rater and columns the second, in category order. EstimatedValue is weighted kappa (1 perfect, 0 chance level).")]
        public HypothesisTestResult WeightedKappaTest(
            [Description(
                "Square table of non-negative counts, at least 2x2, categories in their natural order: rows first rater, columns second rater.")]
            int[][] table,
            [Description("Linear or Quadratic agreement weights. Default Quadratic.")]
            HypothesisKappaWeighting weighting = HypothesisKappaWeighting.Quadratic,
            [Description("The weighted kappa stated by the null hypothesis. Default 0.")]
            double hypothesizedKappa = 0,
            [Description(
                "Greater (default) tests for agreement above the hypothesised value, TwoSided any difference, Less below.")]
            HypothesisTail tail = HypothesisTail.Greater,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table, nameof(table), 2, 2, true);
            RequireAlpha(alpha);

            if (double.IsNaN(hypothesizedKappa) || hypothesizedKappa < -1 || hypothesizedKappa > 1)
            {
                throw new ArgumentException("Kappa lies between -1 and 1.", nameof(hypothesizedKappa));
            }

            var k = table.Length;
            var weights = new double[k, k];

            for (var i = 0; i < k; i++)
            {
                for (var j = 0; j < k; j++)
                {
                    var distance = Math.Abs(i - j) / (double)(k - 1);

                    weights[i, j] = weighting == HypothesisKappaWeighting.Linear
                        ? 1 - distance
                        : 1 - distance * distance;
                }
            }

            var matrix = new WeightedConfusionMatrix(ToRectangular(table), weights);
            var kappa = matrix.WeightedKappa;

            if (double.IsNaN(kappa))
            {
                throw new ArgumentException("Weighted kappa is undefined because the raters used only one category.",
                    nameof(table));
            }

            var test = Guard(() => new KappaTest(matrix, hypothesizedKappa, ToOneSample(tail)), nameof(table));

            var critical = NormalDistribution.Standard
                .InverseDistributionFunction(1 - alpha / 2);

            var lower = kappa - critical * matrix.WeightedStandardError;
            var upper = kappa + critical * matrix.WeightedStandardError;

            var result = Build($"Weighted kappa test ({weighting.ToString().ToLowerInvariant()} weights)", "z",
                test.Statistic, test.PValue, tail, alpha, $"weighted kappa equals {Format(hypothesizedKappa)}",
                TailPhrase(tail, "weighted kappa", "differs from", "is greater than", "is less than",
                    Format(hypothesizedKappa)),
                detail:
                $"Weighted kappa is {Format(kappa)}, {KappaBand(kappa)} agreement; the {Format(1 - alpha)} interval is {Format(lower)} to {Format(upper)}.");

            result.EstimatedValue = kappa;
            result.NullValue = hypothesizedKappa;
            result.EffectSize = kappa;
            result.EffectSizeName = "Weighted kappa";
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = lower;
            result.ConfidenceUpper = upper;
            result.SampleSize1 = table.Sum(r => r.Sum());

            return result;
        }

        [AdvancedAnalyticsTool("KappaComparisonTest")]
        [Description(
            "Tests whether the agreement (Cohen's kappa) achieved in two independent tables is different, for example two pairs of analysts or two time periods. " +
            "Each table is a square table of counts with rows the first rater and columns the second. Statistic is the z score for the absolute kappa difference; the test is always two-sided. EstimatedValue is kappa1 - kappa2.")]
        public HypothesisTestResult KappaComparisonTest(
            [Description("First square table of non-negative counts, at least 2x2.")]
            int[][] table1,
            [Description("Second square table of non-negative counts, at least 2x2.")]
            int[][] table2,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireTable(table1, nameof(table1), 2, 2, true);
            RequireTable(table2, nameof(table2), 2, 2, true);
            RequireAlpha(alpha);

            var matrix1 = new GeneralConfusionMatrix(ToRectangular(table1));
            var matrix2 = new GeneralConfusionMatrix(ToRectangular(table2));

            if (double.IsNaN(matrix1.Kappa) || double.IsNaN(matrix2.Kappa))
            {
                throw new ArgumentException("Kappa is undefined for a table where a rater used only one category.",
                    nameof(table1));
            }

            var test = Guard(() => new TwoMatrixKappaTest(matrix1, matrix2), nameof(table1));

            var result = Build("Kappa comparison test", "z", test.Statistic, test.PValue, HypothesisTail.TwoSided,
                alpha, "both tables have the same kappa", "the two tables have different kappa",
                detail:
                $"Kappa is {Format(matrix1.Kappa)} in the first table and {Format(matrix2.Kappa)} in the second.");

            result.EstimatedValue = matrix1.Kappa - matrix2.Kappa;
            result.NullValue = 0;
            result.SampleSize1 = table1.Sum(r => r.Sum());
            result.SampleSize2 = table2.Sum(r => r.Sum());

            return result;
        }

        [AdvancedAnalyticsTool("KappaHomogeneityTest")]
        [Description(
            "Tests whether several independent tables share the same kappa (homogeneity of agreement), for example one table per site. " +
            "Statistic is a chi-square with tables-1 degrees of freedom that weighs each table's kappa by the inverse of its variance; EstimatedValue is the variance-weighted overall kappa. " +
            "A small p-value means agreement differs between tables, so do not pool them. Needs at least 2 tables, all square and of any (possibly different) size.")]
        public HypothesisTestResult KappaHomogeneityTest(
            [Description("Array of square tables of counts, at least 2 tables, each at least 2x2.")]
            int[][][] tables,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            var matrices = ToKappaMatrices(tables, nameof(tables), 2);
            RequireAlpha(alpha);

            var test = Guard(() => new AverageKappaTest(matrices), nameof(tables));

            var result = Build("Kappa homogeneity test", "chi-square", test.Statistic, test.PValue,
                HypothesisTail.Greater, alpha, "all tables have the same kappa", "kappa differs between the tables",
                test.DegreesOfFreedom,
                detail: $"The variance-weighted overall kappa is {Format(test.OverallKappa)}.");

            result.EstimatedValue = test.OverallKappa;
            result.SampleSize1 = tables.Sum(t => t.Sum(r => r.Sum()));

            return result;
        }

        [AdvancedAnalyticsTool("KappaGroupDifferenceTest")]
        [Description(
            "Compares the average kappa of two groups of tables, for example several sites using one process versus several sites using another, with a two-sample t test on the kappas of the tables in each group. " +
            "Each group needs at least 2 square tables. Statistic is t on the difference of the mean kappas (equal variances assumed unless assumeEqualVariances is false); EstimatedValue is the mean kappa of group 1 minus that of group 2. Greater means group 1 agrees more.")]
        public HypothesisTestResult KappaGroupDifferenceTest(
            [Description("Array of square tables of counts for the first group, at least 2 tables.")]
            int[][][] tables1,
            [Description("Array of square tables of counts for the second group, at least 2 tables.")]
            int[][][] tables2,
            [Description("Whether the kappa variance is assumed equal in the two groups. Default true.")]
            bool assumeEqualVariances = true,
            [Description("TwoSided, Greater (group 1 agrees more) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            var matrices1 = ToKappaMatrices(tables1, nameof(tables1), 2);
            var matrices2 = ToKappaMatrices(tables2, nameof(tables2), 2);
            RequireAlpha(alpha);

            var test = Guard(
                () => new TwoAverageKappaTest(matrices1, matrices2, 0, assumeEqualVariances, ToTwoSample(tail)),
                nameof(tables1));

            var difference = matrices1.Average(m => m.Kappa) - matrices2.Average(m => m.Kappa);

            var result = Build("Two-group average kappa test", "t", test.Statistic, test.PValue, tail, alpha,
                "both groups have the same average kappa",
                TailPhrase(tail, "the average kappa of group 1", "differs from that of group 2",
                    "is higher than that of group 2", "is lower than that of group 2", string.Empty),
                test.DegreesOfFreedom);

            result.EstimatedValue = difference;
            result.NullValue = 0;
            result.SampleSize1 = matrices1.Length;
            result.SampleSize2 = matrices2.Length;

            return result;
        }

        private static GeneralConfusionMatrix[] ToKappaMatrices(int[][][]? tables, string parameterName, int minimum)
        {
            if (tables == null || tables.Length < minimum)
            {
                throw new ArgumentException(
                    $"At least {minimum} tables are needed, each a square table of counts.", parameterName);
            }

            var matrices = new GeneralConfusionMatrix[tables.Length];

            for (var i = 0; i < tables.Length; i++)
            {
                RequireTable(tables[i], parameterName, 2, 2, true);
                matrices[i] = new GeneralConfusionMatrix(ToRectangular(tables[i]));

                if (double.IsNaN(matrices[i].Kappa) || double.IsNaN(matrices[i].Variance))
                {
                    throw new ArgumentException(
                        $"Kappa or its variance is undefined for table {i}: a rater used a single category or the table is degenerate.",
                        parameterName);
                }
            }

            return matrices;
        }

        [AdvancedAnalyticsTool("AucSignificanceTest")]
        [Description(
            "Tests whether a scoring model's area under the ROC curve (AUC) differs from a stated value, by default 0.5 which is no better than chance, using DeLong's variance. " +
            "Provide one score per case (higher means more likely positive) and the true label (1 positive, 0 negative). EstimatedValue is the exact AUC (probability that a random positive scores above a random negative, ties count half); the confidence bounds are the normal interval at 1-alpha clipped to 0 to 1. " +
            "Needs at least 2 positives and 2 negatives. An AUC below 0.5 is reported as it is: it means the score is inversely related to the label.")]
        public HypothesisTestResult AucSignificanceTest(
            [Description("Model score for every case, no NaN. Higher means more likely positive.")]
            double[] scores,
            [Description("True label for every case in the same order: 1 for positive, 0 for negative.")]
            int[] labels,
            [Description("The AUC stated by the null hypothesis, between 0 and 1. Default 0.5.")]
            double hypothesizedAuc = 0.5,
            [Description("TwoSided, Greater (AUC above the hypothesised value) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(scores, nameof(scores), 4);
            RequireAlpha(alpha);
            RequireProbability(hypothesizedAuc, nameof(hypothesizedAuc), true);
            RequireBinaryLabels(labels, scores.Length, nameof(labels));

            var curve = new ReceiverOperatingCharacteristic(labels, scores);
            var auc = curve.PositiveAccuracies.Mean();

            var standardError = Math.Sqrt(ReceiverOperatingCurveTest.DeLongVariance(curve.PositiveAccuracies,
                curve.NegativeAccuracies));

            if (standardError == 0)
            {
                throw new ArgumentException(
                    "The DeLong standard error is zero, which happens when the scores separate the classes perfectly or are all identical; the test is undefined.",
                    nameof(scores));
            }

            var test = Guard(() => new ZTest(auc, standardError,
                hypothesizedAuc,
                ToOneSample(tail)), nameof(scores));

            var critical = NormalDistribution.Standard
                .InverseDistributionFunction(1 - alpha / 2);

            var lower = Math.Max(0, auc - critical * standardError);
            var upper = Math.Min(1, auc + critical * standardError);

            var result = Build("AUC significance test (DeLong)", "z", test.Statistic, test.PValue, tail, alpha,
                $"the AUC equals {Format(hypothesizedAuc)}",
                TailPhrase(tail, "the AUC", "differs from", "is greater than", "is less than",
                    Format(hypothesizedAuc)),
                detail:
                $"AUC is {Format(auc)} (standard error {Format(standardError)}); the {Format(1 - alpha)} interval is {Format(lower)} to {Format(upper)}.");

            result.EstimatedValue = auc;
            result.NullValue = hypothesizedAuc;
            result.ConfidenceLevel = 1 - alpha;
            result.ConfidenceLower = lower;
            result.ConfidenceUpper = upper;
            result.SampleSize1 = curve.Observations;

            return result;
        }

        [AdvancedAnalyticsTool("AucComparisonTest")]
        [Description(
            "Compares the areas under the ROC curve of two scoring models evaluated on the same cases, using DeLong's method for correlated curves. " +
            "Provide both models' scores in the same case order plus the true labels (1 positive, 0 negative). EstimatedValue is AUC of model 1 minus AUC of model 2; Greater means model 1 ranks better. " +
            "It must be run on identical cases; for models tested on different cases use the difference of independent AUCs with care. Needs at least 2 positives and 2 negatives.")]
        public HypothesisTestResult AucComparisonTest(
            [Description("Scores of the first model for every case, no NaN. Higher means more likely positive.")]
            double[] scores1,
            [Description("Scores of the second model for the same cases in the same order, no NaN.")]
            double[] scores2,
            [Description("True label for every case: 1 for positive, 0 for negative.")]
            int[] labels,
            [Description("TwoSided, Greater (model 1 has the higher AUC) or Less.")]
            HypothesisTail tail = HypothesisTail.TwoSided,
            [Description("Significance level between 0 and 1. Default 0.05.")]
            double alpha = 0.05)
        {
            RequireValues(scores1, nameof(scores1), 4);
            RequireValues(scores2, nameof(scores2), 4);
            RequireSameLength(scores1, nameof(scores1), scores2, nameof(scores2));
            RequireAlpha(alpha);
            RequireBinaryLabels(labels, scores1.Length, nameof(labels));

            var curve1 = new ReceiverOperatingCharacteristic(labels, scores1);
            var curve2 = new ReceiverOperatingCharacteristic(labels, scores2);

            var auc1 = curve1.PositiveAccuracies.Mean();
            var auc2 = curve2.PositiveAccuracies.Mean();

            var covarianceNegative = curve1.NegativeAccuracies.Covariance(curve2.NegativeAccuracies);
            var covariancePositive = curve1.PositiveAccuracies.Covariance(curve2.PositiveAccuracies);

            var covariance = covarianceNegative / curve1.NegativeAccuracies.Length +
                             covariancePositive / curve1.PositiveAccuracies.Length;

            var variance1 = ReceiverOperatingCurveTest.DeLongVariance(curve1.PositiveAccuracies,
                curve1.NegativeAccuracies);
            var variance2 = ReceiverOperatingCurveTest.DeLongVariance(curve2.PositiveAccuracies,
                curve2.NegativeAccuracies);

            var variance = variance1 + variance2 - 2 * covariance;

            if (!(variance > 0))
            {
                throw new ArgumentException(
                    "The variance of the AUC difference is zero or negative, which happens when both models rank the cases identically or perfectly; the test is undefined.",
                    nameof(scores1));
            }

            var test = Guard(() => new ZTest(auc1 - auc2, Math.Sqrt(variance),
                hypothesizedMean: 0,
                ToOneSample(tail)), nameof(scores1));

            var result = Build("AUC comparison test (DeLong)", "z", test.Statistic, test.PValue, tail, alpha,
                "both models have the same AUC",
                TailPhrase(tail, "model 1", "has a different AUC from model 2", "has a higher AUC than model 2",
                    "has a lower AUC than model 2", string.Empty),
                detail: $"AUC is {Format(auc1)} for model 1 and {Format(auc2)} for model 2.");

            result.EstimatedValue = auc1 - auc2;
            result.NullValue = 0;
            result.SampleSize1 = scores1.Length;

            return result;
        }

        private static void RequireBinaryLabels(int[]? labels, int expectedLength, string parameterName)
        {
            if (labels == null || labels.Length != expectedLength)
            {
                throw new ArgumentException("Give exactly one label for every score.", parameterName);
            }

            if (labels.Any(l => l != 0 && l != 1))
            {
                throw new ArgumentException("Labels must be 1 (positive) or 0 (negative).", parameterName);
            }

            if (labels.Count(l => l == 1) < 2 || labels.Count(l => l == 0) < 2)
            {
                throw new ArgumentException("At least 2 positive and 2 negative cases are needed.", parameterName);
            }
        }

        private static void Require2X2(int[][] table, string parameterName)
        {
            if (table.Length != 2 || table[0].Length != 2)
            {
                throw new ArgumentException("This test needs exactly a 2x2 table: [[a, b], [c, d]].", parameterName);
            }
        }

        private static string KappaBand(double kappa)
        {
            if (kappa < 0)
            {
                return "poor (worse than chance)";
            }

            if (kappa <= 0.2)
            {
                return "slight";
            }

            if (kappa <= 0.4)
            {
                return "fair";
            }

            if (kappa <= 0.6)
            {
                return "moderate";
            }

            return kappa <= 0.8 ? "substantial" : "almost perfect";
        }
    }
}