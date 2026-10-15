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
using Accord.Math.Decompositions;
using Accord.Statistics;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    [AdvancedAnalyticsToolGroup("RelationshipsAndRegression")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class CorrelationTools
    {
        [AdvancedAnalyticsTool("PearsonCorrelation")]
        [Description(
            "Measures the strength of a straight-line relationship between two numeric variables with the Pearson product-moment correlation. " +
            "It returns r (from -1 for a perfect downhill line to +1 for a perfect uphill line, 0 for none), r squared (the share of variance in one explained linearly by the other), the t statistic with n-2 degrees of freedom, the two-sided p-value (a small p-value means a correlation this strong is unlikely if the true correlation were 0), and a Fisher z confidence interval for the true correlation. " +
            "Sensitive to outliers and blind to curved relationships; use SpearmanRankCorrelation or KendallTauCorrelation for monotone or outlier-prone data. A constant variable gives NaN, and fewer than 3 complete pairs is rejected.")]
        public CorrelationCoefficientResult PearsonCorrelation(
            [Description("The first variable, one number per observation. NaN marks a missing value.")]
            double[] x,
            [Description("The second variable, in the same observation order and of the same length as x.")]
            double[] y,
            [Description(
                "Confidence level for the interval on the correlation, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "True drops any pair where either value is NaN and reports how many were dropped; false rejects missing values. Default false.")]
            bool ignoreMissing = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            var (cx, cy, dropped) = CorrelationMath.PairwiseComplete(x, y, nameof(x), nameof(y), ignoreMissing);
            CorrelationMath.RequirePairs(cx.Length, 3, "a correlation with a p-value");
            var r = CorrelationMath.Pearson(cx, cy);

            var interval = CorrelationMath.FisherInterval(r, cx.Length > 3 ? 1 / Math.Sqrt(cx.Length - 3d) : double.NaN,
                confidenceLevel);

            return Build("Pearson", r, cx.Length, dropped, "t", CorrelationMath.TStatistic(r, cx.Length),
                cx.Length - 2d, CorrelationMath.PValueFromT(r, cx.Length), confidenceLevel, interval);
        }

        [AdvancedAnalyticsTool("SpearmanRankCorrelation")]
        [Description(
            "Measures the strength of a monotone (consistently rising or falling, not necessarily straight) relationship using Spearman's rho, which is the Pearson correlation of the ranks with tied values given their average rank. " +
            "It returns rho (-1 to +1), rho squared, the t statistic with n-2 degrees of freedom and the two-sided p-value (a t approximation, accurate for roughly n above 10), and a Fisher z confidence interval using the Bonett-Wright standard error sqrt((1+rho^2/2)/(n-3)). " +
            "Robust to outliers and to any monotone transformation of either variable. A constant variable gives NaN; fewer than 3 complete pairs is rejected.")]
        public CorrelationCoefficientResult SpearmanRankCorrelation(
            [Description("The first variable, one number per observation. NaN marks a missing value.")]
            double[] x,
            [Description("The second variable, in the same observation order and of the same length as x.")]
            double[] y,
            [Description("Confidence level for the interval on rho, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "True drops any pair where either value is NaN and reports how many were dropped; false rejects missing values. Default false.")]
            bool ignoreMissing = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            var (cx, cy, dropped) = CorrelationMath.PairwiseComplete(x, y, nameof(x), nameof(y), ignoreMissing);
            CorrelationMath.RequirePairs(cx.Length, 3, "a rank correlation with a p-value");
            var rho = CorrelationMath.Spearman(cx, cy);

            var interval = CorrelationMath.FisherInterval(rho, CorrelationMath.SpearmanStandardError(rho, cx.Length),
                confidenceLevel);

            return Build("Spearman", rho, cx.Length, dropped, "t", CorrelationMath.TStatistic(rho, cx.Length),
                cx.Length - 2d, CorrelationMath.PValueFromT(rho, cx.Length), confidenceLevel, interval);
        }

        [AdvancedAnalyticsTool("KendallTauCorrelation")]
        [Description(
            "Measures ordinal association with Kendall's tau, the surplus of concordant pairs (both variables move the same way) over discordant pairs (they move opposite ways), scaled to -1..+1. " +
            "All three variants are always returned: TauA ignores ties, TauB corrects for ties (the usual choice and the one tested), and TauC adjusts for rectangular tables with different numbers of distinct values. " +
            "The z statistic and two-sided p-value use the tie-corrected null variance of the concordance surplus; the interval is the approximate Fieller-Hartley-Pearson Fisher interval on the chosen variant. " +
            "Robust to outliers and preferred for small samples with many ties. Cost grows with the square of the row count, so at most 30000 pairs are accepted.")]
        public KendallTauResult KendallTauCorrelation(
            [Description("The first variable, one number per observation. NaN marks a missing value.")]
            double[] x,
            [Description("The second variable, in the same observation order and of the same length as x.")]
            double[] y,
            [Description(
                "Which tau is reported in the Tau field and used for the interval: TauB (default), TauA or TauC.")]
            KendallTauVariant variant = KendallTauVariant.TauB,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("True drops any pair where either value is NaN and reports how many; false rejects them.")]
            bool ignoreMissing = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));

            if (!Enum.IsDefined(typeof(KendallTauVariant), variant))
            {
                throw new ArgumentException("Unknown variant. Use TauB, TauA or TauC.", nameof(variant));
            }

            var (cx, cy, dropped) = CorrelationMath.PairwiseComplete(x, y, nameof(x), nameof(y), ignoreMissing);
            CorrelationMath.RequirePairs(cx.Length, 3, "Kendall tau");
            var counts = CorrelationMath.Kendall(cx, cy);
            var tauA = CorrelationMath.TauA(counts);
            var tauB = CorrelationMath.TauB(counts);
            var tauC = CorrelationMath.TauC(counts);

            var tau = variant switch
            {
                KendallTauVariant.TauA => tauA,
                KendallTauVariant.TauC => tauC,
                _ => tauB
            };

            var interval = cx.Length > 4
                ? CorrelationMath.FisherInterval(tau, Math.Sqrt(0.437 / (cx.Length - 4d)), confidenceLevel)
                : (double.NaN, double.NaN);

            return new KendallTauResult
            {
                Variant = variant.ToString(),
                Tau = tau,
                TauA = tauA,
                TauB = tauB,
                TauC = tauC,
                ConcordantPairs = counts.Concordant,
                DiscordantPairs = counts.Discordant,
                PairsTiedInXOnly = counts.TiedX,
                PairsTiedInYOnly = counts.TiedY,
                PairsTiedInBoth = counts.TiedBoth,
                PairCount = cx.Length,
                MissingPairsDropped = dropped,
                ZStatistic = CorrelationMath.KendallZ(counts),
                PValue = CorrelationMath.KendallP(counts),
                ConfidenceLevel = confidenceLevel,
                ConfidenceLower = interval.Item1,
                ConfidenceUpper = interval.Item2,
                Interpretation = CorrelationMath.Describe(tau, "Kendall " + variant)
            };
        }

        [AdvancedAnalyticsTool("PointBiserialCorrelation")]
        [Description(
            "Measures how strongly a numeric variable differs between two groups defined by a 0/1 flag (for example transaction amount for fraud=1 versus fraud=0), as the Pearson correlation between the flag and the numbers. " +
            "It returns the coefficient (positive when group 1 has the higher values), the t statistic, the two-sided p-value (identical to the pooled two-sample t-test p-value), a Fisher confidence interval, the two group means and sizes, and the pooled standard deviation. " +
            "Both groups must have at least one row and the flag must contain only 0 and 1. A constant numeric variable gives NaN.")]
        public PointBiserialResult PointBiserialCorrelation(
            [Description(
                "The group flag for each observation: 1 for the event or first group, 0 for the other. Only 0 and 1 are allowed.")]
            int[] group,
            [Description(
                "The numeric variable, one value per observation in the same order. NaN marks a missing value.")]
            double[] values,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "True drops observations whose numeric value is NaN and reports how many; false rejects them.")]
            bool ignoreMissing = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            var (flags, numbers, dropped) = SplitGroups(group, values, ignoreMissing);
            var g1 = numbers.Where((_, i) => flags[i] == 1).ToArray();
            var g0 = numbers.Where((_, i) => flags[i] == 0).ToArray();
            var n = numbers.Length;
            var r = CorrelationMath.Pearson(flags.Select(f => (double)f).ToArray(), numbers);

            var interval =
                CorrelationMath.FisherInterval(r, n > 3 ? 1 / Math.Sqrt(n - 3d) : double.NaN, confidenceLevel);

            var pooled = Math.Sqrt(
                (g1.Sum(v => Math.Pow(v - g1.Average(), 2)) + g0.Sum(v => Math.Pow(v - g0.Average(), 2))) /
                (n - 2d));

            return new PointBiserialResult
            {
                Method = "PointBiserial",
                Coefficient = r,
                TStatistic = CorrelationMath.TStatistic(r, n),
                DegreesOfFreedom = n - 2d,
                PValue = CorrelationMath.PValueFromT(r, n),
                ConfidenceLevel = confidenceLevel,
                ConfidenceLower = interval.Lower,
                ConfidenceUpper = interval.Upper,
                Group1Count = g1.Length,
                Group0Count = g0.Length,
                Group1Mean = g1.Average(),
                Group0Mean = g0.Average(),
                PooledStandardDeviation = pooled,
                MissingPairsDropped = dropped,
                Interpretation = CorrelationMath.Describe(r, "point-biserial")
            };
        }

        [AdvancedAnalyticsTool("BiserialCorrelation")]
        [Description(
            "Estimates the correlation between a numeric variable and a 0/1 flag that is really a dichotomised normal latent variable (for example pass/fail from an underlying score), using the biserial coefficient (M1-M0)/s * p*q/y where y is the standard normal density at the split. " +
            "It is always larger in magnitude than the point-biserial and can exceed 1 in absolute value when the normality assumption fails. " +
            "TStatistic holds a z statistic against zero using the null standard error sqrt(p*q)/(y*sqrt(n)), DegreesOfFreedom is NaN, and no confidence interval is produced. Use PointBiserialCorrelation when the flag is a genuine two-valued measurement.")]
        public PointBiserialResult BiserialCorrelation(
            [Description(
                "The group flag for each observation: 1 for the upper group, 0 for the lower. Only 0 and 1 are allowed.")]
            int[] group,
            [Description(
                "The numeric variable, one value per observation in the same order. NaN marks a missing value.")]
            double[] values,
            [Description(
                "True drops observations whose numeric value is NaN and reports how many; false rejects them.")]
            bool ignoreMissing = false)
        {
            var (flags, numbers, dropped) = SplitGroups(group, values, ignoreMissing);
            var g1 = numbers.Where((_, i) => flags[i] == 1).ToArray();
            var g0 = numbers.Where((_, i) => flags[i] == 0).ToArray();
            var n = numbers.Length;
            var p = g1.Length / (double)n;
            var q = 1 - p;

            var ordinate = NormalDistribution.Standard.ProbabilityDensityFunction(
                RegressionStats.NormalQuantile(q));

            var mean = numbers.Average();
            var sd = Math.Sqrt(numbers.Sum(v => (v - mean) * (v - mean)) / n);

            var pooled = Math.Sqrt(
                (g1.Sum(v => Math.Pow(v - g1.Average(), 2)) + g0.Sum(v => Math.Pow(v - g0.Average(), 2))) /
                (n - 2d));

            var r = sd > 0 ? (g1.Average() - g0.Average()) / sd * p * q / ordinate : double.NaN;
            var z = r * ordinate * Math.Sqrt(n) / Math.Sqrt(p * q);

            return new PointBiserialResult
            {
                Method = "Biserial",
                Coefficient = r,
                TStatistic = z,
                DegreesOfFreedom = double.NaN,
                PValue = RegressionStats.NormalTwoSided(z),
                ConfidenceLevel = double.NaN,
                ConfidenceLower = double.NaN,
                ConfidenceUpper = double.NaN,
                Group1Count = g1.Length,
                Group0Count = g0.Length,
                Group1Mean = g1.Average(),
                Group0Mean = g0.Average(),
                PooledStandardDeviation = pooled,
                MissingPairsDropped = dropped,
                Interpretation = CorrelationMath.Describe(r, "biserial")
            };
        }

        [AdvancedAnalyticsTool("CorrelationMatrix")]
        [Description(
            "Computes every pairwise correlation among several numeric columns in one call, with a matching matrix of two-sided p-values and pair counts, plus the strongest pairs ranked by absolute coefficient with Benjamini-Hochberg adjusted p-values. " +
            "Choose Pearson for linear relationships, Spearman for monotone ones (tie-corrected) or Kendall (tau-b, at most 5000 rows). Read Coefficients[i][j] as the correlation between Variables[i] and Variables[j]; the diagonal is 1. " +
            "A constant column gives NaN in its row and column. With pairwise deletion each pair uses all rows where both values exist (PairCounts shows how many); with listwise deletion only rows complete in every column are used, so the matrix is internally consistent.")]
        public CorrelationMatrixResult CorrelationMatrix(
            [Description(
                "Rows of observations: one array of numbers per case, all the same length, one entry per column. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "Optional name of each column, unique and in column order. Leave empty to use x1, x2 and so on.")]
            string[]? variableNames = null,
            [Description("Pearson (linear), Spearman (monotone rank) or Kendall (tau-b). Default Pearson.")]
            CorrelationMethod method = CorrelationMethod.Pearson,
            [Description(
                "True uses every row available for each pair; false keeps only rows complete in all columns. Default true.")]
            bool pairwiseDeletion = true,
            [Description("How many of the strongest pairs to list in StrongestPairs, from 0 to 1000. Default 20.")]
            int maximumPairsReported = 20)
        {
            var columns = RegressionInput.ColumnCount(inputs, nameof(inputs));

            if (columns < 2)
            {
                throw new ArgumentException("Supply at least two columns to correlate.", nameof(inputs));
            }

            if (columns > 2000)
            {
                throw new ArgumentException("At most 2000 columns are accepted; select the columns of interest first.",
                    nameof(inputs));
            }

            if (maximumPairsReported < 0 || maximumPairsReported > 1000)
            {
                throw new ArgumentException("maximumPairsReported must be between 0 and 1000.",
                    nameof(maximumPairsReported));
            }

            if (!Enum.IsDefined(typeof(CorrelationMethod), method))
            {
                throw new ArgumentException("Unknown method. Use Pearson, Spearman or Kendall.", nameof(method));
            }

            var names = RegressionInput.ResolveNames(variableNames, columns, nameof(variableNames));
            ValidateFinite(inputs, nameof(inputs));
            var rows = inputs;
            var dropped = 0;

            if (!pairwiseDeletion)
            {
                rows = inputs.Where(r => r.All(v => !double.IsNaN(v))).ToArray();
                dropped = inputs.Length - rows.Length;
            }

            if (rows.Length < 3)
            {
                throw new ArgumentException(
                    $"Only {rows.Length} usable row(s) remain but at least 3 are needed. Use pairwise deletion or fill the missing values.",
                    nameof(inputs));
            }

            if (method == CorrelationMethod.Kendall && rows.Length > 5000)
            {
                throw new ArgumentException(
                    "Kendall matrices are limited to 5000 rows because every pair of rows is compared for every pair of columns. Use Spearman or sample the rows.",
                    nameof(inputs));
            }

            var cols = RegressionInput.Columns(rows);
            var coefficients = new double[columns][];
            var pValues = new double[columns][];
            var counts = new int[columns][];

            for (var i = 0; i < columns; i++)
            {
                coefficients[i] = new double[columns];
                pValues[i] = new double[columns];
                counts[i] = new int[columns];
            }

            var pairs = new List<CorrelationPair>();

            for (var i = 0; i < columns; i++)
            {
                var complete = cols[i].Count(v => !double.IsNaN(v));

                coefficients[i][i] = complete > 1 && cols[i].Where(v => !double.IsNaN(v)).Distinct().Count() > 1
                    ? 1
                    : double.NaN;

                counts[i][i] = complete;

                for (var j = i + 1; j < columns; j++)
                {
                    var (x, y, _) = CorrelationMath.PairwiseComplete(cols[i], cols[j], "x", "y", true);
                    counts[i][j] = counts[j][i] = x.Length;

                    if (x.Length < 3)
                    {
                        coefficients[i][j] = coefficients[j][i] = double.NaN;
                        pValues[i][j] = pValues[j][i] = double.NaN;
                    }
                    else
                    {
                        var (r, p) = CorrelationMath.Coefficient(method, x, y);
                        coefficients[i][j] = coefficients[j][i] = r;
                        pValues[i][j] = pValues[j][i] = p;
                    }

                    pairs.Add(new CorrelationPair
                    {
                        VariableA = names[i],
                        VariableB = names[j],
                        Coefficient = coefficients[i][j],
                        PValue = pValues[i][j],
                        PairCount = x.Length
                    });
                }
            }

            var adjusted = RegressionStats.BenjaminiHochberg(pairs.Select(p => p.PValue).ToArray());

            var strongest = pairs
                .Select((p, i) => p with { AdjustedPValue = adjusted[i] })
                .OrderByDescending(p => double.IsNaN(p.Coefficient) ? -1 : Math.Abs(p.Coefficient))
                .Take(maximumPairsReported)
                .ToArray();

            return new CorrelationMatrixResult
            {
                Method = method.ToString(),
                Variables = names,
                Coefficients = coefficients,
                PValues = pValues,
                PairCounts = counts,
                RowsUsed = rows.Length,
                RowsDropped = dropped,
                StrongestPairs = strongest,
                TotalPairs = pairs.Count
            };
        }

        [AdvancedAnalyticsTool("CovarianceMatrix")]
        [Description(
            "Computes the sample covariance matrix of several numeric columns, together with column means, variances, standard deviations, the total variance (trace) and the generalised variance (determinant). " +
            "Covariance[i][j] is in the product of the units of columns i and j, so it is not comparable across pairs; use CorrelationMatrix for a unit-free view. " +
            "Rows containing NaN are dropped as a whole (listwise) when ignoreMissing is true so the matrix stays positive semi-definite, otherwise missing values are rejected. A determinant near 0 means the columns are nearly linearly dependent.")]
        public CovarianceMatrixResult CovarianceMatrix(
            [Description(
                "Rows of observations: one array of numbers per case, all the same length, one entry per column.")]
            double[][] inputs,
            [Description(
                "Optional name of each column, unique and in column order. Leave empty to use x1, x2 and so on.")]
            string[]? variableNames = null,
            [Description(
                "True divides by n-1 (sample covariance, default); false divides by n (population covariance).")]
            bool unbiased = true,
            [Description("True drops any row containing NaN and reports the count; false rejects missing values.")]
            bool ignoreMissing = false)
        {
            var columns = RegressionInput.ColumnCount(inputs, nameof(inputs));
            var names = RegressionInput.ResolveNames(variableNames, columns, nameof(variableNames));
            ValidateFinite(inputs, nameof(inputs));
            var rows = inputs;
            var dropped = 0;

            if (inputs.Any(r => r.Any(double.IsNaN)))
            {
                if (!ignoreMissing)
                {
                    throw new ArgumentException(
                        "inputs contains a missing (NaN) value. Set ignoreMissing to true to drop those rows, or fill the gaps first.",
                        nameof(inputs));
                }

                rows = inputs.Where(r => r.All(v => !double.IsNaN(v))).ToArray();
                dropped = inputs.Length - rows.Length;
            }

            if (rows.Length < 2)
            {
                throw new ArgumentException($"Only {rows.Length} usable row(s) remain; at least 2 are needed.",
                    nameof(inputs));
            }

            var covariance = rows.Covariance();

            if (!unbiased)
            {
                var factor = (rows.Length - 1d) / rows.Length;
                covariance = covariance.Select(row => row.Select(v => v * factor).ToArray()).ToArray();
            }

            var variances = Enumerable.Range(0, columns).Select(i => covariance[i][i]).ToArray();

            return new CovarianceMatrixResult
            {
                Variables = names,
                Covariance = covariance,
                Means = rows.Mean(0),
                Variances = variances,
                StandardDeviations = variances.Select(v => Math.Sqrt(Math.Max(0, v))).ToArray(),
                TotalVariance = variances.Sum(),
                GeneralisedVariance = covariance.ToMatrix().Determinant(false),
                RowsUsed = rows.Length,
                RowsDropped = dropped,
                Unbiased = unbiased
            };
        }

        [AdvancedAnalyticsTool("PartialCorrelation")]
        [Description(
            "Measures the relationship between x and y after removing the linear influence of one or more control variables from both (the correlation of the two sets of regression residuals). " +
            "Use it to ask whether x and y are still related once confounders are held constant; compare with ZeroOrderCorrelation, the raw correlation. " +
            "It returns the coefficient, t statistic with n-2-k degrees of freedom (k = number of controls), two-sided p-value and a Fisher confidence interval with standard error 1/sqrt(n-3-k). " +
            "Method Spearman ranks every variable first (a rank-based approximation); Kendall is not supported. Rows with NaN are dropped only when ignoreMissing is true.")]
        public PartialCorrelationResult PartialCorrelation(
            [Description("The first variable, one number per observation.")]
            double[] x,
            [Description("The second variable, same length and order as x.")]
            double[] y,
            [Description(
                "Control variables as rows: one array per observation holding the control values, same row count as x.")]
            double[][] controls,
            [Description("Pearson (default) or Spearman. Kendall is not supported for partial correlation.")]
            CorrelationMethod method = CorrelationMethod.Pearson,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "True drops rows with any NaN in x, y or the controls and reports the count; false rejects them.")]
            bool ignoreMissing = false)
        {
            var (cx, cy, z, dropped) = PrepareControlled(x, y, controls, method, ignoreMissing, confidenceLevel);
            var n = cx.Length;
            var k = z[0].Length;
            var rx = LinearFit.Fit(z, cx, null, ControlNames(k), true).Residuals;
            var ry = LinearFit.Fit(z, cy, null, ControlNames(k), true).Residuals;
            var r = CorrelationMath.Pearson(rx, ry);
            var df = n - 2d - k;
            var t = CorrelationMath.TStatistic(r, n - k);

            var interval = CorrelationMath.FisherInterval(r, n - 3 - k > 0 ? 1 / Math.Sqrt(n - 3d - k) : double.NaN,
                confidenceLevel);

            return new PartialCorrelationResult
            {
                Method = "Partial" + method,
                Coefficient = r,
                ZeroOrderCorrelation = CorrelationMath.Pearson(cx, cy),
                PairCount = n,
                ControlCount = k,
                DegreesOfFreedom = df,
                TStatistic = t,
                PValue = RegressionStats.TwoSidedTp(t, df),
                ConfidenceLevel = confidenceLevel,
                ConfidenceLower = interval.Lower,
                ConfidenceUpper = interval.Upper,
                RowsDropped = dropped,
                Interpretation = CorrelationMath.Describe(r, "partial")
            };
        }

        [AdvancedAnalyticsTool("SemiPartialCorrelation")]
        [Description(
            "Measures the unique contribution of x to y: the correlation between y and the part of x that is not explained by the control variables (the residual of x after regressing on the controls), which leaves y itself untouched. " +
            "Its square is the increase in R squared when x is added to a regression of y on the controls. The t statistic and p-value are those of x's coefficient in the regression of y on x and the controls, with n-k-2 degrees of freedom (k = number of controls). " +
            "No confidence interval is produced (fields are NaN). Compare with PartialCorrelation, which also removes the controls from y. Method Spearman ranks every variable first.")]
        public PartialCorrelationResult SemiPartialCorrelation(
            [Description("The predictor whose unique contribution is wanted, one number per observation.")]
            double[] x,
            [Description("The outcome variable, same length and order as x.")]
            double[] y,
            [Description(
                "Control variables as rows: one array per observation holding the control values, same row count as x.")]
            double[][] controls,
            [Description("Pearson (default) or Spearman. Kendall is not supported.")]
            CorrelationMethod method = CorrelationMethod.Pearson,
            [Description(
                "True drops rows with any NaN in x, y or the controls and reports the count; false rejects them.")]
            bool ignoreMissing = false)
        {
            var (cx, cy, z, dropped) = PrepareControlled(x, y, controls, method, ignoreMissing, 0.95);
            var n = cx.Length;
            var k = z[0].Length;
            var rx = LinearFit.Fit(z, cx, null, ControlNames(k), true).Residuals;
            var r = CorrelationMath.Pearson(rx, cy);
            var combined = cx.Select((v, i) => new[] { v }.Concat(z[i]).ToArray()).ToArray();
            var full = LinearFit.Fit(combined, cy, null, new[] { "x" }.Concat(ControlNames(k)).ToArray(), true);
            var t = full.Beta[1] / full.StandardError(1);
            var df = full.DfResidual;

            return new PartialCorrelationResult
            {
                Method = "SemiPartial" + method,
                Coefficient = r,
                ZeroOrderCorrelation = CorrelationMath.Pearson(cx, cy),
                PairCount = n,
                ControlCount = k,
                DegreesOfFreedom = df,
                TStatistic = t,
                PValue = RegressionStats.TwoSidedTp(t, df),
                ConfidenceLevel = double.NaN,
                ConfidenceLower = double.NaN,
                ConfidenceUpper = double.NaN,
                RowsDropped = dropped,
                Interpretation = CorrelationMath.Describe(r, "semi-partial")
            };
        }

        [AdvancedAnalyticsTool("PartialCorrelationMatrix")]
        [Description(
            "Computes the partial correlation of every pair of columns controlling for all the other columns at once, from the inverse of the correlation matrix. " +
            "Coefficients[i][j] is the direct association between Variables[i] and Variables[j] with the influence of every remaining variable removed, so a large raw correlation that collapses here was driven by the others. " +
            "PValues use n-p degrees of freedom (p = number of columns). Rows with NaN are dropped whole when ignoreMissing is true. Perfectly collinear columns make the matrix non-invertible and are rejected; find them with VarianceInflationFactors.")]
        public PartialCorrelationMatrixResult PartialCorrelationMatrix(
            [Description(
                "Rows of observations: one array of numbers per case, all the same length, at least 3 columns.")]
            double[][] inputs,
            [Description(
                "Optional name of each column, unique and in column order. Leave empty to use x1, x2 and so on.")]
            string[]? variableNames = null,
            [Description("True drops rows containing NaN and reports the count; false rejects missing values.")]
            bool ignoreMissing = false)
        {
            var columns = RegressionInput.ColumnCount(inputs, nameof(inputs));

            if (columns < 3)
            {
                throw new ArgumentException(
                    "Partial correlations need at least 3 columns (with 2 columns use PearsonCorrelation).",
                    nameof(inputs));
            }

            var names = RegressionInput.ResolveNames(variableNames, columns, nameof(variableNames));
            ValidateFinite(inputs, nameof(inputs));
            var rows = inputs;

            if (inputs.Any(r => r.Any(double.IsNaN)))
            {
                if (!ignoreMissing)
                {
                    throw new ArgumentException(
                        "inputs contains a missing (NaN) value. Set ignoreMissing to true to drop those rows, or fill the gaps first.",
                        nameof(inputs));
                }

                rows = inputs.Where(r => r.All(v => !double.IsNaN(v))).ToArray();
            }

            if (rows.Length <= columns + 1)
            {
                throw new ArgumentException(
                    $"Only {rows.Length} usable row(s) remain; more than {columns + 1} are needed for {columns} columns.",
                    nameof(inputs));
            }

            var correlation = rows.Correlation();
            var singular = new JaggedSingularValueDecomposition(correlation, false, false).Diagonal;

            if (singular.Any(double.IsNaN) || !(singular.Min() > singular.Max() * 1e-12))
            {
                throw new ArgumentException(
                    "The columns are perfectly collinear or one is constant, so the partial correlations are undefined. Remove the redundant column.",
                    nameof(inputs));
            }

            var precision = correlation.Inverse();
            var df = rows.Length - columns;
            var coefficients = new double[columns][];
            var pValues = new double[columns][];

            for (var i = 0; i < columns; i++)
            {
                coefficients[i] = new double[columns];
                pValues[i] = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    if (i == j)
                    {
                        coefficients[i][j] = 1;
                        pValues[i][j] = 0;
                        continue;
                    }

                    var r = -precision[i][j] / Math.Sqrt(precision[i][i] * precision[j][j]);
                    coefficients[i][j] = r;

                    pValues[i][j] =
                        RegressionStats.TwoSidedTp(CorrelationMath.TStatistic(r, rows.Length - columns + 2), df);
                }
            }

            return new PartialCorrelationMatrixResult
            {
                Variables = names,
                Coefficients = coefficients,
                PValues = pValues,
                DegreesOfFreedom = df,
                RowsUsed = rows.Length,
                RowsDropped = inputs.Length - rows.Length
            };
        }

        [AdvancedAnalyticsTool("MutualInformationEstimate")]
        [Description(
            "Estimates how much knowing one numeric variable reduces uncertainty about another, capturing any dependence including non-monotone and non-linear shapes that correlation misses. " +
            "Both variables are cut into bins (equal width or equal frequency; automatic bin count is floor(sqrt(n/5)) clamped to 2..20) and the plug-in estimate MI = H(X) + H(Y) - H(X,Y) is returned in nats and bits, plus a normalised score MI/sqrt(H(X)H(Y)) between 0 and 1. " +
            "GStatistic = 2n*MI (natural log) is referred to a chi-square with (bins-1)(bins-1) degrees of freedom for a p-value against independence; biasCorrect subtracts the Miller-Madow correction for the estimator's positive bias in small samples. " +
            "Zero means the binned variables look independent. Estimates depend on the binning and are biased upwards when bins are many relative to n.")]
        public MutualInformationResult MutualInformationEstimate(
            [Description("The first variable, one number per observation. NaN marks a missing value.")]
            double[] x,
            [Description("The second variable, same length and order as x.")]
            double[] y,
            [Description("Number of bins per variable, from 2 to 200; 0 chooses automatically. Default 0.")]
            int bins = 0,
            [Description(
                "EqualWidth (default) cuts the range into equal intervals; EqualFrequency puts about the same number of rows in each bin, which suits skewed data.")]
            MutualInformationBinning binning = MutualInformationBinning.EqualWidth,
            [Description(
                "True applies the Miller-Madow bias correction to the reported information (the G test always uses the raw value). Default false.")]
            bool biasCorrect = false,
            [Description("True drops pairs with a NaN and reports how many; false rejects missing values.")]
            bool ignoreMissing = false)
        {
            var (cx, cy, dropped) = CorrelationMath.PairwiseComplete(x, y, nameof(x), nameof(y), ignoreMissing);
            CorrelationMath.RequirePairs(cx.Length, 4, "a mutual information estimate");

            return MutualInformationCore.Estimate(cx, cy, bins, binning, biasCorrect, dropped);
        }

        [AdvancedAnalyticsTool("RankPredictorsByMutualInformation")]
        [Description(
            "Screens many numeric columns against one target by binned mutual information and ranks them, so non-linear and non-monotone predictors are not missed the way they are by correlation. " +
            "For each column it returns MI in bits, the normalised MI (0 to 1), the chi-square p-value of the G test and a Benjamini-Hochberg adjusted p-value across the columns, sorted strongest first with Rank 1 best. " +
            "Each column uses the rows where both it and the target exist. Binning follows MutualInformationEstimate; the target may be numeric or a coded 0/1 label.")]
        public PredictorInformationScore[] RankPredictorsByMutualInformation(
            [Description(
                "Rows of predictor values: one array of numbers per case, all the same length. NaN marks a missing value.")]
            double[][] inputs,
            [Description("The target value for each row, in the same order as the rows. NaN marks a missing value.")]
            double[] target,
            [Description(
                "Optional name of each predictor column, unique and in column order. Leave empty to use x1, x2 and so on.")]
            string[]? variableNames = null,
            [Description("Number of bins per variable, from 2 to 200; 0 chooses automatically. Default 0.")]
            int bins = 0,
            [Description("EqualWidth (default) or EqualFrequency binning of every variable.")]
            MutualInformationBinning binning = MutualInformationBinning.EqualWidth)
        {
            var columns = RegressionInput.ColumnCount(inputs, nameof(inputs));
            RegressionInput.RequireArray(target, nameof(target), 4);

            if (target.Length != inputs.Length)
            {
                throw new ArgumentException(
                    $"inputs has {inputs.Length} row(s) but target has {target.Length}. Supply one target per row.",
                    nameof(target));
            }

            var names = RegressionInput.ResolveNames(variableNames, columns, nameof(variableNames));
            var cols = RegressionInput.Columns(inputs);
            var scored = new List<(string Name, MutualInformationResult Result)>();

            for (var j = 0; j < columns; j++)
            {
                var (cx, cy, _) = CorrelationMath.PairwiseComplete(cols[j], target, "column", nameof(target), true);

                scored.Add(cx.Length < 4
                    ? (names[j], new MutualInformationResult
                    {
                        MutualInformationBits = double.NaN,
                        NormalisedMutualInformation = double.NaN,
                        PValue = double.NaN,
                        PairCount = cx.Length
                    })
                    : (names[j], MutualInformationCore.Estimate(cx, cy, bins, binning, false, 0)));
            }

            var adjusted = RegressionStats.BenjaminiHochberg(scored.Select(s => s.Result.PValue).ToArray());

            return scored
                .Select((s, i) => new PredictorInformationScore
                {
                    Variable = s.Name,
                    MutualInformationBits = s.Result.MutualInformationBits,
                    NormalisedMutualInformation = s.Result.NormalisedMutualInformation,
                    PValue = s.Result.PValue,
                    AdjustedPValue = adjusted[i],
                    PairCount = s.Result.PairCount
                })
                .OrderByDescending(s => double.IsNaN(s.MutualInformationBits) ? -1 : s.MutualInformationBits)
                .Select((s, i) => s with { Rank = i + 1 })
                .ToArray();
        }

        [AdvancedAnalyticsTool("RankPredictorsByCorrelation")]
        [Description(
            "Screens many numeric columns against one target by correlation and ranks them by absolute strength, returning each column's coefficient, two-sided p-value, Benjamini-Hochberg adjusted p-value (controls false discoveries across the many columns) and pair count. " +
            "Use Pearson for linear, Spearman for monotone or Kendall (at most 5000 rows) relationships. Each column uses the rows where both it and the target exist; columns with fewer than 3 such rows or no variation get NaN and rank last. Rank 1 is the strongest.")]
        public PredictorCorrelationScore[] RankPredictorsByCorrelation(
            [Description(
                "Rows of predictor values: one array of numbers per case, all the same length. NaN marks a missing value.")]
            double[][] inputs,
            [Description("The target value for each row, in the same order as the rows. NaN marks a missing value.")]
            double[] target,
            [Description(
                "Optional name of each predictor column, unique and in column order. Leave empty to use x1, x2 and so on.")]
            string[]? variableNames = null,
            [Description("Pearson (default), Spearman or Kendall.")]
            CorrelationMethod method = CorrelationMethod.Pearson)
        {
            var columns = RegressionInput.ColumnCount(inputs, nameof(inputs));
            RegressionInput.RequireArray(target, nameof(target), 3);

            if (target.Length != inputs.Length)
            {
                throw new ArgumentException(
                    $"inputs has {inputs.Length} row(s) but target has {target.Length}. Supply one target per row.",
                    nameof(target));
            }

            if (!Enum.IsDefined(typeof(CorrelationMethod), method))
            {
                throw new ArgumentException("Unknown method. Use Pearson, Spearman or Kendall.", nameof(method));
            }

            var names = RegressionInput.ResolveNames(variableNames, columns, nameof(variableNames));
            var cols = RegressionInput.Columns(inputs);
            var scores = new List<(string Name, double R, double P, int N)>();

            for (var j = 0; j < columns; j++)
            {
                var (cx, cy, _) = CorrelationMath.PairwiseComplete(cols[j], target, "column", nameof(target), true);

                if (cx.Length < 3)
                {
                    scores.Add((names[j], double.NaN, double.NaN, cx.Length));
                    continue;
                }

                if (method == CorrelationMethod.Kendall && cx.Length > 5000)
                {
                    throw new ArgumentException(
                        "Kendall is limited to 5000 rows here. Use Spearman or sample the rows.",
                        nameof(inputs));
                }

                var (r, p) = CorrelationMath.Coefficient(method, cx, cy);
                scores.Add((names[j], r, p, cx.Length));
            }

            var adjusted = RegressionStats.BenjaminiHochberg(scores.Select(s => s.P).ToArray());

            return scores
                .Select((s, i) => new PredictorCorrelationScore
                {
                    Variable = s.Name,
                    Coefficient = s.R,
                    PValue = s.P,
                    AdjustedPValue = adjusted[i],
                    PairCount = s.N
                })
                .OrderByDescending(s => double.IsNaN(s.Coefficient) ? -1 : Math.Abs(s.Coefficient))
                .Select((s, i) => s with { Rank = i + 1 })
                .ToArray();
        }

        [AdvancedAnalyticsTool("CorrelationRatio")]
        [Description(
            "Measures how strongly a categorical grouping explains a numeric variable, with the correlation ratio eta (the square root of between-group variance over total variance, 0 to 1) and eta squared (the share of variance explained by group membership). " +
            "Unlike Pearson it detects any pattern of group means, so it suits a categorical predictor against a numeric outcome. Also returned are the bias-lowered epsilon squared, the one-way ANOVA F statistic with its degrees of freedom and p-value, and each group's label, size and mean. " +
            "At least 2 groups and more rows than groups are required. A numeric variable with no variation gives NaN.")]
        public CorrelationRatioResult CorrelationRatio(
            [Description(
                "An integer code for the group of each observation, for example 0, 1, 2 for three categories.")]
            int[] groups,
            [Description(
                "The numeric variable, one value per observation in the same order. NaN marks a missing value.")]
            double[] values,
            [Description(
                "True drops observations whose numeric value is NaN and reports how many; false rejects them.")]
            bool ignoreMissing = false)
        {
            RegressionInput.RequireArray(values, nameof(values), 1);

            if (groups == null || groups.Length != values.Length)
            {
                throw new ArgumentException(
                    $"groups must have one code per value ({values.Length}); it has {groups?.Length ?? 0}.",
                    nameof(groups));
            }

            var kept = new List<int>();

            for (var i = 0; i < values.Length; i++)
            {
                if (double.IsInfinity(values[i]))
                {
                    throw new ArgumentException($"Value {i} is infinite. Remove or cap infinite values first.",
                        nameof(values));
                }

                if (double.IsNaN(values[i]))
                {
                    if (!ignoreMissing)
                    {
                        throw new ArgumentException(
                            $"Value {i} is missing (NaN). Set ignoreMissing to true to drop it, or fill the gap first.",
                            nameof(values));
                    }

                    continue;
                }

                kept.Add(i);
            }

            var labels = kept.Select(i => groups[i]).Distinct().OrderBy(g => g).ToArray();

            if (labels.Length < 2)
            {
                throw new ArgumentException("At least 2 distinct groups are needed. Check the group codes.",
                    nameof(groups));
            }

            var n = kept.Count;

            if (n <= labels.Length)
            {
                throw new ArgumentException("There must be more observations than groups.", nameof(values));
            }

            var grand = kept.Average(i => values[i]);
            var sizes = new int[labels.Length];
            var means = new double[labels.Length];
            var ssb = 0d;
            var ssw = 0d;

            for (var g = 0; g < labels.Length; g++)
            {
                var members = kept.Where(i => groups[i] == labels[g]).Select(i => values[i]).ToArray();
                sizes[g] = members.Length;
                means[g] = members.Average();
                ssb += members.Length * (means[g] - grand) * (means[g] - grand);
                ssw += members.Sum(v => (v - means[g]) * (v - means[g]));
            }

            var sst = ssb + ssw;
            var dfb = labels.Length - 1d;
            var dfw = n - (double)labels.Length;
            var eta2 = sst > 0 ? ssb / sst : double.NaN;
            var f = ssw > 0 ? ssb / dfb / (ssw / dfw) : double.NaN;
            var epsilon = sst > 0 ? (ssb - dfb * ssw / dfw) / sst : double.NaN;

            return new CorrelationRatioResult
            {
                Eta = Math.Sqrt(eta2),
                EtaSquared = eta2,
                AdjustedEtaSquared = epsilon,
                FStatistic = f,
                DegreesOfFreedomBetween = dfb,
                DegreesOfFreedomWithin = dfw,
                PValue = RegressionStats.FUpperTail(f, dfb, dfw),
                GroupCount = labels.Length,
                PairCount = n,
                MissingPairsDropped = values.Length - n,
                GroupMeans = means,
                GroupSizes = sizes,
                GroupLabels = labels
            };
        }

        [AdvancedAnalyticsTool("CompareIndependentCorrelations")]
        [Description(
            "Tests whether the correlation observed in one sample differs from the correlation in a separate, independent sample (for example the same two variables in two customer segments or two time periods), using the Fisher z transformation. " +
            "It returns each Fisher z, the z statistic for the difference z1-z2 over sqrt(1/(n1-3)+1/(n2-3)), the two-sided p-value (small means the correlations genuinely differ) and a confidence interval for the difference in Fisher z units. " +
            "Do not use it for correlations sharing a variable or measured on the same rows. Each sample size must be at least 4 and each correlation strictly between -1 and 1.")]
        public CorrelationComparisonResult CompareIndependentCorrelations(
            [Description("Correlation from the first sample, strictly between -1 and 1.")]
            double correlation1,
            [Description("Number of observations behind the first correlation, at least 4.")]
            int sampleSize1,
            [Description("Correlation from the second sample, strictly between -1 and 1.")]
            double correlation2,
            [Description("Number of observations behind the second correlation, at least 4.")]
            int sampleSize2,
            [Description(
                "Confidence level for the interval on the difference, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            RequireOpenCorrelation(correlation1, nameof(correlation1));
            RequireOpenCorrelation(correlation2, nameof(correlation2));

            if (sampleSize1 < 4 || sampleSize2 < 4)
            {
                throw new ArgumentException("Each sample size must be at least 4.", nameof(sampleSize1));
            }

            var z1 = Special.Atanh(correlation1);
            var z2 = Special.Atanh(correlation2);
            var se = Math.Sqrt(1d / (sampleSize1 - 3) + 1d / (sampleSize2 - 3));
            var z = (z1 - z2) / se;
            var quantile = RegressionStats.NormalQuantile(0.5 + confidenceLevel / 2);

            return new CorrelationComparisonResult
            {
                Correlation1 = correlation1,
                Correlation2 = correlation2,
                FisherZ1 = z1,
                FisherZ2 = z2,
                ZStatistic = z,
                PValue = RegressionStats.NormalTwoSided(z),
                DifferenceInFisherZ = z1 - z2,
                ConfidenceLevel = confidenceLevel,
                ConfidenceLower = z1 - z2 - quantile * se,
                ConfidenceUpper = z1 - z2 + quantile * se
            };
        }

        [AdvancedAnalyticsTool("CorrelationSummaryInterval")]
        [Description(
            "Turns a correlation you already know and its sample size into a significance test and confidence interval without needing the raw data. " +
            "It returns the t statistic and two-sided p-value against a true correlation of 0 (n-2 degrees of freedom), the Fisher z value and its standard error 1/sqrt(n-3), the confidence interval for the true correlation, and the smallest absolute correlation that would be significant at the complementary level (a quick check of what n can detect). " +
            "The correlation must be strictly between -1 and 1 and n at least 4.")]
        public CorrelationSummaryIntervalResult CorrelationSummaryInterval(
            [Description("The observed correlation coefficient, strictly between -1 and 1.")]
            double correlation,
            [Description("Number of paired observations behind the correlation, at least 4.")]
            int sampleSize,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            RequireOpenCorrelation(correlation, nameof(correlation));

            if (sampleSize < 4)
            {
                throw new ArgumentException("sampleSize must be at least 4.", nameof(sampleSize));
            }

            var se = 1 / Math.Sqrt(sampleSize - 3d);
            var interval = CorrelationMath.FisherInterval(correlation, se, confidenceLevel);
            var tCritical = RegressionStats.TQuantile(0.5 + confidenceLevel / 2, sampleSize - 2d);

            return new CorrelationSummaryIntervalResult
            {
                Correlation = correlation,
                SampleSize = sampleSize,
                TStatistic = CorrelationMath.TStatistic(correlation, sampleSize),
                DegreesOfFreedom = sampleSize - 2d,
                PValue = CorrelationMath.PValueFromT(correlation, sampleSize),
                FisherZ = Special.Atanh(correlation),
                StandardErrorOfZ = se,
                ConfidenceLevel = confidenceLevel,
                ConfidenceLower = interval.Lower,
                ConfidenceUpper = interval.Upper,
                SmallestSignificantCorrelation = tCritical / Math.Sqrt(tCritical * tCritical + sampleSize - 2d)
            };
        }

        private static CorrelationCoefficientResult Build(string method, double r, int n, int dropped,
            string statisticName,
            double statistic, double df, double p, double level, (double Lower, double Upper) interval)
        {
            return new CorrelationCoefficientResult
            {
                Method = method,
                Coefficient = r,
                CoefficientOfDetermination = r * r,
                PairCount = n,
                MissingPairsDropped = dropped,
                StatisticName = statisticName,
                Statistic = statistic,
                DegreesOfFreedom = df,
                PValue = p,
                ConfidenceLevel = level,
                ConfidenceLower = interval.Lower,
                ConfidenceUpper = interval.Upper,
                Interpretation = CorrelationMath.Describe(r, method)
            };
        }

        private static void RequireOpenCorrelation(double value, string name)
        {
            if (!(value > -1 && value < 1))
            {
                throw new ArgumentException($"{name} must be strictly between -1 and 1.", name);
            }
        }

        private static void ValidateFinite(double[][] inputs, string name)
        {
            for (var i = 0; i < inputs.Length; i++)
            {
                for (var j = 0; j < inputs[i].Length; j++)
                {
                    if (double.IsInfinity(inputs[i][j]))
                    {
                        throw new ArgumentException(
                            $"{name} row {i} column {j} is infinite. Remove or cap infinite values first.", name);
                    }
                }
            }
        }

        private static string[] ControlNames(int count)
        {
            return Enumerable.Range(1, count).Select(i => "c" + i).ToArray();
        }

        private static (int[] Flags, double[] Values, int Dropped) SplitGroups(int[]? group, double[]? values,
            bool ignoreMissing)
        {
            RegressionInput.RequireArray(values, nameof(values), 3);

            if (group == null || group.Length != values!.Length)
            {
                throw new ArgumentException(
                    $"group must have one flag per value ({values!.Length}); it has {group?.Length ?? 0}.",
                    nameof(group));
            }

            var flags = new List<int>();
            var numbers = new List<double>();

            for (var i = 0; i < values.Length; i++)
            {
                if (group[i] != 0 && group[i] != 1)
                {
                    throw new ArgumentException(
                        $"group[{i}] is {group[i]}; only 0 and 1 are allowed. Recode the flag first.", nameof(group));
                }

                if (double.IsInfinity(values[i]))
                {
                    throw new ArgumentException($"Value {i} is infinite. Remove or cap infinite values first.",
                        nameof(values));
                }

                if (double.IsNaN(values[i]))
                {
                    if (!ignoreMissing)
                    {
                        throw new ArgumentException(
                            $"Value {i} is missing (NaN). Set ignoreMissing to true to drop it, or fill the gap first.",
                            nameof(values));
                    }

                    continue;
                }

                flags.Add(group[i]);
                numbers.Add(values[i]);
            }

            if (flags.Count(f => f == 1) < 1 || flags.Count(f => f == 0) < 1 || numbers.Count < 3)
            {
                throw new ArgumentException(
                    "Both groups need at least one usable observation and there must be at least 3 observations in total.",
                    nameof(group));
            }

            return (flags.ToArray(), numbers.ToArray(), values.Length - numbers.Count);
        }

        private static (double[] X, double[] Y, double[][] Z, int Dropped) PrepareControlled(double[]? x, double[]? y,
            double[][]? controls, CorrelationMethod method, bool ignoreMissing, double level)
        {
            RegressionInput.RequireLevel(level, nameof(level));
            RegressionInput.RequireSameLength(x, y, nameof(x), nameof(y));

            if (method == CorrelationMethod.Kendall || !Enum.IsDefined(typeof(CorrelationMethod), method))
            {
                throw new ArgumentException("Use Pearson or Spearman; Kendall is not supported here.", nameof(method));
            }

            var k = RegressionInput.ColumnCount(controls, nameof(controls));

            if (controls!.Length != x!.Length)
            {
                throw new ArgumentException(
                    $"controls has {controls.Length} row(s) but x has {x.Length}. Supply one row of control values per observation.",
                    nameof(controls));
            }

            var keep = new List<int>();

            for (var i = 0; i < x.Length; i++)
            {
                var missing = double.IsNaN(x[i]) || double.IsNaN(y![i]) || controls[i].Any(double.IsNaN);

                if (double.IsInfinity(x[i]) || double.IsInfinity(y![i]) || controls[i].Any(double.IsInfinity))
                {
                    throw new ArgumentException($"Row {i} contains an infinite value. Remove or cap it first.",
                        nameof(x));
                }

                if (missing && !ignoreMissing)
                {
                    throw new ArgumentException(
                        $"Row {i} has a missing (NaN) value. Set ignoreMissing to true to drop incomplete rows, or fill the gap first.",
                        nameof(x));
                }

                if (!missing)
                {
                    keep.Add(i);
                }
            }

            if (keep.Count < k + 4)
            {
                throw new ArgumentException(
                    $"Only {keep.Count} complete row(s) remain but {k + 4} are needed for {k} control(s).", nameof(x));
            }

            var cx = keep.Select(i => x[i]).ToArray();
            var cy = keep.Select(i => y![i]).ToArray();
            var z = keep.Select(i => (double[])controls[i].Clone()).ToArray();

            if (method == CorrelationMethod.Spearman)
            {
                cx = cx.Rank();
                cy = cy.Rank();
                z = RegressionInput.FromColumns(RegressionInput.Columns(z).Select(c => c.Rank()).ToArray());
            }

            return (cx, cy, z, x.Length - keep.Count);
        }
    }
}