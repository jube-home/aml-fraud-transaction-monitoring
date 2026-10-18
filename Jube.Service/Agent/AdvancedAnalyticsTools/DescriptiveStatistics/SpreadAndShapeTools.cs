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
using System.Globalization;
using Accord.Math;
using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics
{
    [AdvancedAnalyticsToolGroup("DescriptiveStatistics")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SpreadAndShapeTools
    {
        private const string IgnoreMissingText =
            "True drops missing (NaN) values before computing and reports how many were dropped; false (default) rejects input containing NaN.";

        private const string UnbiasedText =
            "True (default) uses the sample (n-1) estimator; false uses the population (n) divisor.";

        private const double NormalConsistencyConstant = 1.482602218505602;

        [AdvancedAnalyticsTool("Variance")]
        [Description(
            "Variance: the average squared distance from the mean, in squared units of the data. It is the basic measure of spread; take its square root (StandardDeviation) to return to the original units. " +
            "The sample estimator (default) needs at least 2 values; the population estimator needs at least 1. A constant series has variance 0.")]
        public DescriptiveScalar Variance(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(UnbiasedText)] bool unbiased = true,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, unbiased ? 2 : 1, out var missing);

            return DescriptiveInput.Scalar(v.Variance(unbiased), v.Length, missing);
        }

        [AdvancedAnalyticsTool("StandardDeviation")]
        [Description(
            "Standard deviation: the typical distance of values from their mean, in the same units as the data. For roughly normal data about 68 percent of values fall within one standard deviation of the mean. " +
            "The sample estimator (default) needs at least 2 values; the population estimator needs at least 1. Sensitive to outliers; see MedianAbsoluteDeviation for a robust alternative.")]
        public DescriptiveScalar StandardDeviation(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(UnbiasedText)] bool unbiased = true,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, unbiased ? 2 : 1, out var missing);

            return DescriptiveInput.Scalar(v.StandardDeviation(unbiased), v.Length, missing);
        }

        [AdvancedAnalyticsTool("StandardErrorOfMean")]
        [Description(
            "Standard error of the mean: the sample standard deviation divided by the square root of the sample size. It measures how precisely the sample mean estimates the true mean and shrinks as more data is collected. " +
            "Needs at least 2 values. Roughly, the true mean lies within two standard errors of the sample mean with 95 percent confidence for reasonably large samples.")]
        public DescriptiveScalar StandardErrorOfMean(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 2, out var missing);

            return DescriptiveInput.Scalar(Measures.StandardError(v), v.Length, missing);
        }

        [AdvancedAnalyticsTool("CoefficientOfVariation")]
        [Description(
            "Coefficient of variation: standard deviation divided by the absolute mean, a unit-free measure of relative spread that lets you compare variability across variables on different scales. " +
            "Value is a ratio (0.25 means the spread is 25 percent of the level). It is NaN or infinite when the mean is 0 and is only meaningful for ratio-scale data that is not centred on zero.")]
        public DescriptiveScalar CoefficientOfVariation(
            [Description("The numbers, normally all positive.")]
            double[] values,
            [Description(UnbiasedText)] bool unbiased = true,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, unbiased ? 2 : 1, out var missing);
            var mean = v.Mean();

            return DescriptiveInput.Scalar(v.StandardDeviation(mean, unbiased) / Math.Abs(mean), v.Length,
                missing);
        }

        [AdvancedAnalyticsTool("GeometricStandardDeviation")]
        [Description(
            "Geometric standard deviation: the exponential of the standard deviation of the logarithms, a multiplicative spread factor for positive, right-skewed data such as amounts. A value of 1 means no spread; 2 means typical values lie within a factor of 2 above or below the geometric mean. " +
            "All values must be strictly positive and at least 2 are needed. Value is a factor of at least 1.")]
        public DescriptiveScalar GeometricStandardDeviation(
            [Description("Strictly positive numbers such as amounts.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 2, out var missing);
            var logs = new double[v.Length];

            for (var i = 0; i < v.Length; i++)
            {
                if (!(v[i] > 0) || double.IsInfinity(v[i]))
                {
                    throw new ArgumentException(
                        $"values[{i}] is {v[i].ToString(CultureInfo.InvariantCulture)}; the geometric standard deviation needs strictly positive finite values.",
                        "values");
                }

                logs[i] = Math.Log(v[i]);
            }

            return DescriptiveInput.Scalar(Math.Exp(logs.StandardDeviation()), v.Length, missing);
        }

        [AdvancedAnalyticsTool("VarianceToMeanRatio")]
        [Description(
            "Index of dispersion: sample variance divided by the mean. For counts, a value near 1 is consistent with a Poisson process, above 1 means over-dispersed (bursty or clustered) and below 1 under-dispersed (regular). " +
            "Use it on non-negative counts per interval. It is undefined (NaN or infinite) when the mean is 0.")]
        public DescriptiveScalar VarianceToMeanRatio(
            [Description("Non-negative counts or rates, one per interval.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 2, out var missing);

            return DescriptiveInput.Scalar(v.Variance(true) / v.Mean(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("MeanAbsoluteDeviation")]
        [Description(
            "Mean absolute deviation: the average absolute distance of values from their mean (or from their median when aroundMedian is true). It is a spread measure in the data's own units that is less sensitive to outliers than the standard deviation. " +
            "Value is the average absolute distance.")]
        public DescriptiveScalar MeanAbsoluteDeviation(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("True measures distances from the median, false (default) from the mean.")]
            bool aroundMedian = false,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var centre = aroundMedian ? v.Median() : v.Mean();
            var deviations = new double[v.Length];

            for (var i = 0; i < v.Length; i++)
            {
                deviations[i] = Math.Abs(v[i] - centre);
            }

            return DescriptiveInput.Scalar(deviations.Mean(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("MedianAbsoluteDeviation")]
        [Description(
            "Median absolute deviation (MAD): the median of the absolute distances from the median. It is a robust spread measure that up to half the data can be outliers without wrecking. " +
            "With scaleToNormal true (default) the value is multiplied by 1.4826 so it estimates the standard deviation for normal data; false returns the raw MAD. Value 0 means more than half the values are identical.")]
        public DescriptiveScalar MedianAbsoluteDeviation(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(
                "True (default) multiplies by 1.4826 to make it comparable with a standard deviation; false returns the raw MAD.")]
            bool scaleToNormal = true,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var mad = Mad(v);

            return DescriptiveInput.Scalar(scaleToNormal ? mad * NormalConsistencyConstant : mad, v.Length, missing);
        }

        [AdvancedAnalyticsTool("InterquartileRange")]
        [Description(
            "Interquartile range (IQR): the upper quartile minus the lower quartile, the spread of the middle half of the data. It is robust to outliers and is the basis of the box plot and the 1.5 x IQR outlier fence. " +
            "Quartiles use the chosen interpolation (Linear is the spreadsheet default). Value is the IQR; Count and MissingIgnored describe the input.")]
        public DescriptiveScalar InterquartileRange(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(
                "Quantile interpolation rule. Linear (default) matches common spreadsheets and statistics packages.")]
            DescriptiveQuantileInterpolation interpolation = DescriptiveQuantileInterpolation.Linear,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var q = v.Quantiles(new[] { 0.25, 0.75 }, false, DescriptiveInput.Map(interpolation));

            return DescriptiveInput.Scalar(q[1] - q[0], v.Length, missing);
        }

        [AdvancedAnalyticsTool("QuartileCoefficientOfDispersion")]
        [Description(
            "Quartile coefficient of dispersion: (Q3 - Q1) / (Q3 + Q1), a robust unit-free measure of relative spread analogous to the coefficient of variation. " +
            "Use it to compare spread across skewed variables that are positive. Undefined (NaN) when the quartiles sum to 0.")]
        public DescriptiveScalar QuartileCoefficientOfDispersion(
            [Description("The numbers, normally positive.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            var q = v.Quantiles(new[] { 0.25, 0.75 }, false,
                DescriptiveInput.Map(DescriptiveQuantileInterpolation.Linear));

            return DescriptiveInput.Scalar((q[1] - q[0]) / (q[1] + q[0]), v.Length, missing);
        }

        [AdvancedAnalyticsTool("SumOfSquaredDeviations")]
        [Description(
            "Total sum of squared deviations from the mean (the sum of squares, SS): the building block of variance and of analysis of variance. Divide by n-1 for the sample variance. " +
            "Value is in squared units of the data.")]
        public DescriptiveScalar SumOfSquaredDeviations(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var mean = v.Mean();
            var total = 0.0;

            foreach (var x in v)
            {
                total += (x - mean) * (x - mean);
            }

            return DescriptiveInput.Scalar(total, v.Length, missing);
        }

        [AdvancedAnalyticsTool("WeightedVariance")]
        [Description(
            "Variance of values where each observation has a weight. Choose Frequency when weights are repeat counts (the sample estimator divides by total weight minus 1) or Reliability when weights are importance or probability weights (uses the reliability-weights correction). " +
            "Weights must be non-negative and finite. The unbiased estimator needs total weight above 1; Value is the variance.")]
        public DescriptiveScalar WeightedVariance(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("One non-negative weight per value, in the same order.")]
            double[] weights,
            [Description("Frequency for repeat counts, Reliability for importance weights. Default Reliability.")]
            DescriptiveWeightKind weightKind = DescriptiveWeightKind.Reliability,
            [Description(UnbiasedText)] bool unbiased = true,
            [Description("True drops missing (NaN) values together with their weights; false (default) rejects NaN.")]
            bool ignoreMissing = false)
        {
            DescriptiveInput.PairClean(values, weights, "weights", ignoreMissing, out var v, out var w,
                out var missing);

            RequirePositiveWeight(w);

            return DescriptiveInput.Scalar(v.WeightedVariance(w, unbiased, MapWeight(weightKind)), v.Length,
                missing);
        }

        [AdvancedAnalyticsTool("WeightedStandardDeviation")]
        [Description(
            "Standard deviation of values where each observation has a weight; the square root of the weighted variance. Choose Frequency for repeat counts or Reliability for importance weights. " +
            "Weights must be non-negative and finite with a positive total. Value is in the data's units.")]
        public DescriptiveScalar WeightedStandardDeviation(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("One non-negative weight per value, in the same order.")]
            double[] weights,
            [Description("Frequency for repeat counts, Reliability for importance weights. Default Reliability.")]
            DescriptiveWeightKind weightKind = DescriptiveWeightKind.Reliability,
            [Description(UnbiasedText)] bool unbiased = true,
            [Description("True drops missing (NaN) values together with their weights; false (default) rejects NaN.")]
            bool ignoreMissing = false)
        {
            DescriptiveInput.PairClean(values, weights, "weights", ignoreMissing, out var v, out var w,
                out var missing);

            RequirePositiveWeight(w);

            return DescriptiveInput.Scalar(v.WeightedStandardDeviation(w, unbiased, MapWeight(weightKind)),
                v.Length, missing);
        }

        [AdvancedAnalyticsTool("ExponentialWeightedVariance")]
        [Description(
            "Exponentially weighted variance of a time-ordered series, where recent observations count more (the last element has the greatest weight). Use it for a recency-weighted volatility estimate. " +
            "alpha between 0 and 1 controls the decay: larger values forget the past faster. Value is the variance; a series shorter than 2 values is rejected.")]
        public DescriptiveScalar ExponentialWeightedVariance(
            [Description("The series in time order, oldest first, newest last.")]
            double[] values,
            [Description("Smoothing factor greater than 0 and at most 1. Default 0.3.")]
            double alpha = 0.3,
            [Description(UnbiasedText)] bool unbiased = false,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (double.IsNaN(alpha) || alpha <= 0 || alpha > 1)
            {
                throw new ArgumentException("alpha must be greater than 0 and at most 1.", "alpha");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 2, out var missing);

            return DescriptiveInput.Scalar(v.ExponentialWeightedVariance(alpha, unbiased), v.Length, missing);
        }

        [AdvancedAnalyticsTool("PooledVariance")]
        [Description(
            "Pooled variance across several groups: the group variances averaged with weights equal to their degrees of freedom. It is the common-variance estimate used in equal-variance t-tests and ANOVA. " +
            "Each group needs at least 2 values (unbiased) and NaN values are dropped per group when ignoreMissing is true. Value is the pooled variance and Count the total observations.")]
        public DescriptiveScalar PooledVariance(
            [Description("One array of numbers per group; groups can differ in length.")]
            double[][] groups,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var cleaned = CleanGroups(groups, ignoreMissing, out var missing);

            return DescriptiveInput.Scalar(Measures.PooledVariance(true, cleaned), cleaned.Sum(g => g.Length), missing);
        }

        [AdvancedAnalyticsTool("PooledStandardDeviation")]
        [Description(
            "Pooled standard deviation across several groups: the square root of the pooled variance, the common within-group spread used for effect sizes such as Cohen's d. " +
            "Each group needs at least 2 values and NaN values are dropped per group when ignoreMissing is true.")]
        public DescriptiveScalar PooledStandardDeviation(
            [Description("One array of numbers per group; groups can differ in length.")]
            double[][] groups,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var cleaned = CleanGroups(groups, ignoreMissing, out var missing);

            return DescriptiveInput.Scalar(Measures.PooledStandardDeviation(true, cleaned), cleaned.Sum(g => g.Length),
                missing);
        }

        [AdvancedAnalyticsTool("PooledVarianceFromSummaries")]
        [Description(
            "Pooled variance from group summaries when the raw data is unavailable: supply each group's size and sample variance. Weights are the degrees of freedom (size minus 1). " +
            "Every size must be at least 2 for the unbiased estimator and variances must not be negative.")]
        public DescriptiveScalar PooledVarianceFromSummaries(
            [Description("Number of observations in each group.")]
            int[] sizes,
            [Description("Sample variance of each group, in the same order.")]
            double[] variances)
        {
            if (sizes == null || variances == null || sizes.Length == 0 || sizes.Length != variances.Length)
            {
                throw new ArgumentException("sizes and variances must be non-empty and have one entry per group.",
                    "sizes");
            }

            if (sizes.Any(s => s < 2) || variances.Any(x => double.IsNaN(x) || x < 0))
            {
                throw new ArgumentException("Every size must be at least 2 and every variance a non-negative number.",
                    "variances");
            }

            return DescriptiveInput.Scalar(Measures.PooledVariance(sizes, variances), sizes.Sum(), 0);
        }

        [AdvancedAnalyticsTool("Skewness")]
        [Description(
            "Skewness: the asymmetry of the distribution. Positive means a long right tail (a few very large values, typical of amounts), negative a long left tail, about 0 symmetric. " +
            "Rules of thumb: beyond +/-1 is strongly skewed. The unbiased (default) form is the adjusted Fisher-Pearson G1 and needs 3 values; the population form is the moment coefficient. Constant data gives NaN.")]
        public DescriptiveScalar Skewness(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(UnbiasedText)] bool unbiased = true,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, unbiased ? 3 : 1, out var missing);

            return DescriptiveInput.Scalar(v.Skewness(unbiased), v.Length, missing);
        }

        [AdvancedAnalyticsTool("ExcessKurtosis")]
        [Description(
            "Excess kurtosis: tail heaviness relative to the normal distribution, which scores 0. Positive means heavy tails and more extreme values than normal (typical of financial amounts), negative means light tails. " +
            "The unbiased (default) form needs 4 values; the population form is the moment coefficient minus 3. Constant data gives NaN.")]
        public DescriptiveScalar ExcessKurtosis(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(UnbiasedText)] bool unbiased = true,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, unbiased ? 4 : 1, out var missing);

            return DescriptiveInput.Scalar(v.Kurtosis(unbiased), v.Length, missing);
        }

        [AdvancedAnalyticsTool("Kurtosis")]
        [Description(
            "Pearson kurtosis: the fourth standardised moment (population form), where the normal distribution scores 3. It equals excess kurtosis plus 3; values above 3 mean heavier tails than normal. " +
            "Needs at least 1 value; constant data gives NaN. Use ExcessKurtosis for the sample-corrected form centred on 0.")]
        public DescriptiveScalar Kurtosis(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.Kurtosis(false) + 3.0, v.Length, missing);
        }

        [AdvancedAnalyticsTool("ShapeStatistics")]
        [Description(
            "Skewness, excess kurtosis and Pearson kurtosis together, for judging how far a variable departs from a normal bell curve. " +
            "Skewness near 0 and excess kurtosis near 0 are consistent with normality; large positive values indicate a right tail and heavy tails. Needs 4 values for the unbiased forms, 1 for the population forms.")]
        public DescriptiveShapeSummary ShapeStatistics(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(UnbiasedText)] bool unbiased = true,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, unbiased ? 4 : 1, out var missing);

            return new DescriptiveShapeSummary
            {
                Skewness = v.Skewness(unbiased),
                ExcessKurtosis = v.Kurtosis(unbiased),
                Kurtosis = v.Kurtosis(false) + 3.0,
                UnbiasedEstimators = unbiased,
                Count = v.Length,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("RawMoment")]
        [Description(
            "Raw (non-central) moment of a given order: the mean of the values raised to that power. Order 1 is the mean, order 2 the mean square. " +
            "Order must be between 1 and 12. Value is the moment; large values raised to high powers can overflow to infinity.")]
        public DescriptiveScalar RawMoment(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("Order of the moment, 1 to 12.")]
            int order,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            RequireOrder(order);
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(MomentAbout(v, 0, order), v.Length, missing);
        }

        [AdvancedAnalyticsTool("CentralMoment")]
        [Description(
            "Central moment of a given order: the mean of the deviations from the mean raised to that power. Order 2 is the population variance, order 3 measures asymmetry and order 4 tail weight. Order 1 is 0 by definition. " +
            "Order must be between 1 and 12. Uses the population divisor n.")]
        public DescriptiveScalar CentralMoment(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("Order of the moment, 1 to 12.")]
            int order,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            RequireOrder(order);
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(MomentAbout(v, v.Mean(), order), v.Length, missing);
        }

        [AdvancedAnalyticsTool("StandardisedMoment")]
        [Description(
            "Standardised moment of a given order: the central moment divided by the population standard deviation raised to that order, a scale-free shape number. Order 3 is skewness and order 4 is Pearson kurtosis. " +
            "Order must be between 3 and 12; constant data gives NaN.")]
        public DescriptiveScalar StandardisedMoment(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("Order of the moment, 3 to 12.")]
            int order,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (order < 3 || order > 12)
            {
                throw new ArgumentException("order must be between 3 and 12; orders 1 and 2 are the mean and variance.",
                    "order");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var mean = v.Mean();
            var sd = Math.Sqrt(MomentAbout(v, mean, 2));

            return DescriptiveInput.Scalar(MomentAbout(v, mean, order) / Math.Pow(sd, order), v.Length, missing);
        }

        [AdvancedAnalyticsTool("EntropyOfCounts")]
        [Description(
            "Shannon entropy of a distribution given as counts or probabilities: how unpredictable the category is. 0 means one category holds everything; the maximum is the log of the number of categories when all are equally likely. " +
            "Zero counts are ignored. Returns Entropy in the chosen unit, MaximumEntropy, NormalisedEntropy (Entropy over Maximum, 0 to 1, evenness) and Perplexity (the effective number of equally likely categories).")]
        public DescriptiveEntropyResult EntropyOfCounts(
            [Description("Non-negative counts or probabilities, one per category; they need not sum to 1.")]
            double[] counts,
            [Description("Unit of the logarithm: Bits (base 2, default), Nats (natural) or Hartleys (base 10).")]
            DescriptiveEntropyUnit unit = DescriptiveEntropyUnit.Bits)
        {
            if (counts == null || counts.Length == 0)
            {
                throw new ArgumentException("counts is empty. Supply one count per category.", "counts");
            }

            if (counts.Any(c => double.IsNaN(c) || double.IsInfinity(c) || c < 0))
            {
                throw new ArgumentException("counts must be finite and not negative.", "counts");
            }

            if (counts.Sum() <= 0)
            {
                throw new ArgumentException(
                    "counts sum to zero, so there is no distribution. Give at least one positive count.", "counts");
            }

            return EntropyResult(counts, unit, 0);
        }

        [AdvancedAnalyticsTool("EntropyOfCategories")]
        [Description(
            "Shannon entropy of a list of category labels (for example merchant categories or countries), computed from their observed frequencies. Low entropy means activity concentrates on a few categories; high entropy means it is spread evenly. " +
            "Returns Entropy in the chosen unit, MaximumEntropy, NormalisedEntropy (0 to 1), Perplexity and the number of Categories. Null labels are treated as missing.")]
        public DescriptiveEntropyResult EntropyOfCategories(
            [Description("The category label for each observation.")]
            string[] values,
            [Description("Unit of the logarithm: Bits (base 2, default), Nats (natural) or Hartleys (base 10).")]
            DescriptiveEntropyUnit unit = DescriptiveEntropyUnit.Bits,
            [Description("True skips null labels and reports them; false (default) rejects null.")]
            bool ignoreMissing = false)
        {
            var v = DescriptiveInput.CleanLabels(values, "values", ignoreMissing, out var missing);
            var counts = v.GroupBy(x => x, StringComparer.Ordinal).Select(g => (double)g.Count()).ToArray();

            return EntropyResult(counts, unit, missing);
        }

        [AdvancedAnalyticsTool("InequalityMeasures")]
        [Description(
            "Concentration and inequality of non-negative amounts (spend, balances, transaction counts per account): Gini coefficient (0 perfectly equal, near 1 one holder has everything), Theil index, Herfindahl-Hirschman index of shares (1/n for equal shares, 1 for total concentration), coefficient of variation and a Lorenz curve of cumulative shares. " +
            "Use it to measure how concentrated value is among accounts or counterparties. Values must not be negative and must not all be zero. The Lorenz curve is returned at up to 101 evenly spaced points.")]
        public DescriptiveInequalityResult InequalityMeasures(
            [Description("Non-negative amounts, one per holder or account.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Finite(values, "values", ignoreMissing, 1, out var missing);

            if (v.Any(x => x < 0))
            {
                throw new ArgumentException(
                    "values must not be negative for inequality measures. Use absolute amounts or filter first.",
                    "values");
            }

            var total = v.Sum();

            if (total <= 0)
            {
                throw new ArgumentException(
                    "values sum to zero, so shares are undefined. Supply at least one positive amount.", "values");
            }

            var sorted = DescriptiveInput.Sorted(v);
            var n = sorted.Length;
            var weighted = 0.0;

            for (var i = 0; i < n; i++)
            {
                weighted += (2.0 * (i + 1) - n - 1) * sorted[i];
            }

            var mean = total / n;
            var theil = 0.0;
            var herfindahl = 0.0;

            foreach (var x in sorted)
            {
                var share = x / total;
                herfindahl += share * share;

                if (x > 0)
                {
                    theil += x / mean * Math.Log(x / mean);
                }
            }

            var points = Math.Min(n, 100);
            var lorenz = new List<DescriptiveLorenzPoint> { new() { PopulationShare = 0, ValueShare = 0 } };
            var running = 0.0;
            var next = 1;

            for (var i = 0; i < n; i++)
            {
                running += sorted[i];

                if (i + 1 == (int)Math.Ceiling((double)next * n / points))
                {
                    lorenz.Add(new DescriptiveLorenzPoint
                        { PopulationShare = (double)(i + 1) / n, ValueShare = running / total });

                    next++;
                }
            }

            return new DescriptiveInequalityResult
            {
                Gini = weighted / (n * total),
                Theil = theil / n,
                HerfindahlIndex = herfindahl,
                CoefficientOfVariation = n > 1 ? v.StandardDeviation(mean, false) / mean : double.NaN,
                Lorenz = lorenz.ToArray(),
                Count = n,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("GiniCoefficient")]
        [Description(
            "Gini coefficient of non-negative amounts: 0 means every holder has the same amount, values near 1 mean one holder has almost everything. Use it as a single number for how concentrated spend, balances or activity is. " +
            "Values must not be negative and must not all be zero. Value is the population Gini (no small-sample correction).")]
        public DescriptiveScalar GiniCoefficient(
            [Description("Non-negative amounts, one per holder or account.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var result = InequalityMeasures(values, ignoreMissing);

            return DescriptiveInput.Scalar(result.Gini, result.Count, result.MissingIgnored);
        }

        private static DescriptiveEntropyResult EntropyResult(double[] counts, DescriptiveEntropyUnit unit, int missing)
        {
            var positive = counts.Where(c => c > 0).ToArray();
            var total = positive.Sum();
            var probabilities = positive.Select(c => c / total).ToArray();

            var divisor = unit switch
            {
                DescriptiveEntropyUnit.Bits => Math.Log(2),
                DescriptiveEntropyUnit.Nats => 1.0,
                DescriptiveEntropyUnit.Hartleys => Math.Log(10),
                _ => throw new ArgumentException("Unknown unit. Use Bits, Nats or Hartleys.", "unit")
            };

            var nats = Math.Abs(probabilities.Entropy());
            var maximum = Math.Log(counts.Length);

            return new DescriptiveEntropyResult
            {
                Entropy = nats / divisor,
                Unit = unit.ToString(),
                MaximumEntropy = maximum / divisor,
                NormalisedEntropy = counts.Length > 1 ? nats / maximum : double.NaN,
                Perplexity = Math.Exp(nats),
                Categories = positive.Length,
                Total = total,
                MissingIgnored = missing
            };
        }

        internal static double Mad(double[] values)
        {
            var median = values.Median();
            var deviations = new double[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                deviations[i] = Math.Abs(values[i] - median);
            }

            return deviations.Median();
        }

        private static double MomentAbout(double[] values, double centre, int order)
        {
            var total = 0.0;

            foreach (var x in values)
            {
                total += Math.Pow(x - centre, order);
            }

            return total / values.Length;
        }

        private static void RequireOrder(int order)
        {
            if (order < 1 || order > 12)
            {
                throw new ArgumentException("order must be between 1 and 12.", "order");
            }
        }

        private static WeightType MapWeight(DescriptiveWeightKind weightKind)
        {
            return weightKind switch
            {
                DescriptiveWeightKind.Frequency => WeightType.Repetition,
                DescriptiveWeightKind.Reliability => WeightType.Fraction,
                _ => throw new ArgumentException("Unknown weight kind. Use Frequency or Reliability.",
                    nameof(weightKind))
            };
        }

        private static void RequirePositiveWeight(double[] weights)
        {
            if (weights.Sum() <= 0)
            {
                throw new ArgumentException("weights sum to zero. Give at least one positive weight.", "weights");
            }
        }

        private static double[][] CleanGroups(double[][] groups, bool ignoreMissing, out int missing)
        {
            if (groups == null || groups.Length < 2)
            {
                throw new ArgumentException("groups needs at least two groups, each an array of numbers.", "groups");
            }

            var cleaned = new double[groups.Length][];
            missing = 0;

            for (var i = 0; i < groups.Length; i++)
            {
                cleaned[i] = DescriptiveInput.Clean(groups[i], $"groups[{i}]", ignoreMissing, 2, out var m);
                missing += m;
            }

            return cleaned;
        }
    }
}