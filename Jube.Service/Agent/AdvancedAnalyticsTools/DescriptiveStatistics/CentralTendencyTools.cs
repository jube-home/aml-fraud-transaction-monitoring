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
    public class CentralTendencyTools
    {
        private const string IgnoreMissingText =
            "True drops missing (NaN) values before computing and reports how many were dropped; false (default) rejects input containing NaN.";

        [AdvancedAnalyticsTool("Mean")]
        [Description(
            "Arithmetic mean (average) of a list of numbers: the sum divided by the count. Use it as the default measure of the typical level of a roughly symmetric variable; prefer Median when there are extreme outliers. " +
            "Returns Value (the mean), Count (numbers used) and MissingIgnored (NaN values dropped). Infinite values make the result infinite or NaN.")]
        public DescriptiveScalar Mean(
            [Description("The numbers to average, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.Mean(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("WeightedMean")]
        [Description(
            "Weighted arithmetic mean: each value counts in proportion to its weight (for example an amount, a volume or a frequency). Use it when observations are not equally important or when values are already aggregated with counts. " +
            "Weights must be finite and not negative and the same length as values; a value whose weight is 0 contributes nothing. Returns the mean in Value.")]
        public DescriptiveScalar WeightedMean(
            [Description("The numbers to average.")]
            double[] values,
            [Description("One non-negative weight per value, in the same order. They need not sum to 1.")]
            double[] weights,
            [Description("True drops missing (NaN) values together with their weights; false (default) rejects NaN.")]
            bool ignoreMissing = false)
        {
            DescriptiveInput.PairClean(values, weights, "weights", ignoreMissing, out var v, out var w,
                out var missing);

            if (w.Sum() <= 0)
            {
                throw new ArgumentException(
                    "weights sum to zero, so a weighted mean is undefined. Give at least one positive weight.",
                    "weights");
            }

            return DescriptiveInput.Scalar(v.WeightedMean(w), v.Length, missing);
        }

        [AdvancedAnalyticsTool("TrimmedMean")]
        [Description(
            "Mean after discarding the lowest and highest share of the sorted values (a truncated mean). It is resistant to outliers while using more data than the median; a trim of 0.1 removes the bottom 10 percent and top 10 percent. " +
            "trimFraction is a share of each tail, between 0 and 0.5 exclusive; the number trimmed per tail is rounded down. Value is the trimmed mean.")]
        public DescriptiveScalar TrimmedMean(
            [Description("The numbers to average.")]
            double[] values,
            [Description("Share to discard from EACH tail, from 0 up to but not including 0.5. Default 0.1.")]
            double trimFraction = 0.1,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (double.IsNaN(trimFraction) || trimFraction < 0 || trimFraction >= 0.5)
            {
                throw new ArgumentException(
                    "trimFraction must be from 0 up to but not including 0.5 (a share of each tail).", "trimFraction");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.TruncatedMean(trimFraction), v.Length, missing);
        }

        [AdvancedAnalyticsTool("WinsorisedMean")]
        [Description(
            "Mean after replacing the lowest and highest share of values with the nearest retained value, rather than deleting them. Like a trimmed mean it limits outlier influence but keeps the sample size. " +
            "winsorFraction is the share replaced in each tail (0 to under 0.5); the count per tail is rounded down. Value is the winsorised mean.")]
        public DescriptiveScalar WinsorisedMean(
            [Description("The numbers to average.")]
            double[] values,
            [Description("Share replaced in EACH tail, from 0 up to but not including 0.5. Default 0.05.")]
            double winsorFraction = 0.05,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (double.IsNaN(winsorFraction) || winsorFraction < 0 || winsorFraction >= 0.5)
            {
                throw new ArgumentException(
                    "winsorFraction must be from 0 up to but not including 0.5 (a share of each tail).",
                    "winsorFraction");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var sorted = DescriptiveInput.Sorted(v);
            var k = (int)Math.Floor(sorted.Length * winsorFraction);

            for (var i = 0; i < k; i++)
            {
                sorted[i] = sorted[k];
                sorted[sorted.Length - 1 - i] = sorted[sorted.Length - 1 - k];
            }

            return DescriptiveInput.Scalar(sorted.Mean(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("GeometricMean")]
        [Description(
            "Geometric mean: the n-th root of the product, computed through logarithms. Use it for growth factors, ratios and multiplicative quantities such as index relatives. " +
            "All values must be strictly positive; a zero or negative value is rejected because the result would be zero or undefined. Value is the geometric mean, never larger than the arithmetic mean.")]
        public DescriptiveScalar GeometricMean(
            [Description("Strictly positive numbers, for example growth factors such as 1.05.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            RequirePositive(v, "geometric");

            return DescriptiveInput.Scalar(v.GeometricMean(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("HarmonicMean")]
        [Description(
            "Harmonic mean: the count divided by the sum of reciprocals. It is the right average for rates and ratios over a fixed quantity, such as speeds over equal distances or price-to-earnings ratios. " +
            "All values must be strictly positive. Value is the harmonic mean, never larger than the geometric mean.")]
        public DescriptiveScalar HarmonicMean(
            [Description("Strictly positive numbers such as rates or ratios.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            RequirePositive(v, "harmonic");
            var reciprocals = new double[v.Length];

            for (var i = 0; i < v.Length; i++)
            {
                reciprocals[i] = 1.0 / v[i];
            }

            return DescriptiveInput.Scalar(1.0 / reciprocals.Mean(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("QuadraticMean")]
        [Description(
            "Quadratic mean, also called the root mean square (RMS): the square root of the mean of the squares. Use it as the typical magnitude of values that can be positive or negative, such as errors, residuals or signal amplitude. " +
            "Negative values are allowed. Value is always at least the absolute value of the arithmetic mean.")]
        public DescriptiveScalar QuadraticMean(
            [Description("The numbers, of either sign.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var squares = new double[v.Length];

            for (var i = 0; i < v.Length; i++)
            {
                squares[i] = v[i] * v[i];
            }

            return DescriptiveInput.Scalar(Math.Sqrt(squares.Mean()), v.Length, missing);
        }

        [AdvancedAnalyticsTool("PowerMean")]
        [Description(
            "Generalised power mean of order p: the p-th root of the mean of the values raised to p. Order 1 is the arithmetic mean, 2 the quadratic mean, -1 the harmonic mean and 0 the geometric mean; larger orders emphasise large values. " +
            "Values must be strictly positive unless p is a positive even integer. Value is the power mean.")]
        public DescriptiveScalar PowerMean(
            [Description("The numbers; strictly positive unless the order is a positive even integer.")]
            double[] values,
            [Description("The order p of the mean; any finite number. 0 gives the geometric mean.")]
            double order,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (double.IsNaN(order) || double.IsInfinity(order))
            {
                throw new ArgumentException("order must be a finite number such as 1, 2, 0 or -1.", "order");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var evenPositive = order > 0 && order == Math.Floor(order) && Math.Abs(order) % 2 == 0;

            if (!evenPositive)
            {
                RequirePositive(v, "power");
            }

            double result;

            if (order == 0)
            {
                result = v.GeometricMean();
            }
            else
            {
                var powered = new double[v.Length];

                for (var i = 0; i < v.Length; i++)
                {
                    powered[i] = Math.Pow(v[i], order);
                }

                result = Math.Pow(powered.Mean(), 1.0 / order);
            }

            return DescriptiveInput.Scalar(result, v.Length, missing);
        }

        [AdvancedAnalyticsTool("ContraharmonicMean")]
        [Description(
            "Contraharmonic (Lehmer) mean: the sum of squares over the sum of values, or with an order the sum of x^order over the sum of x^(order-1). It weights large values by themselves, so it is at least the arithmetic mean; it gives the mean value seen by a randomly chosen unit of the total, for instance the average size of the group a random person belongs to. " +
            "The sum of the values must not be zero. Value is the mean.")]
        public DescriptiveScalar ContraharmonicMean(
            [Description("The numbers, normally positive sizes or amounts.")]
            double[] values,
            [Description("Order of the Lehmer mean, 2 or more (default 2, the contraharmonic mean).")]
            int order = 2,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (order < 2 || order > 20)
            {
                throw new ArgumentException("order must be between 2 and 20; 2 gives the classic contraharmonic mean.",
                    "order");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            if (v.Sum() == 0)
            {
                throw new ArgumentException("The values sum to zero, so the contraharmonic mean is undefined.",
                    "values");
            }

            return DescriptiveInput.Scalar(Measures.ContraHarmonicMean(v, order - 1), v.Length, missing);
        }

        [AdvancedAnalyticsTool("ExponentialWeightedMean")]
        [Description(
            "Exponentially weighted mean of an ordered series in which the most recent observation (the last element) has the greatest weight and older observations decay geometrically. Use it for a recency-weighted level of a time-ordered series. " +
            "alpha is the smoothing factor between 0 and 1: near 1 follows the last values, near 0 approaches the plain mean. Value is the weighted mean.")]
        public DescriptiveScalar ExponentialWeightedMean(
            [Description("The series in time order, oldest first, newest last.")]
            double[] values,
            [Description("Smoothing factor greater than 0 and at most 1. Default 0.3.")]
            double alpha = 0.3,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (double.IsNaN(alpha) || alpha <= 0 || alpha > 1)
            {
                throw new ArgumentException("alpha must be greater than 0 and at most 1.", "alpha");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.ExponentialWeightedMean(alpha), v.Length, missing);
        }

        [AdvancedAnalyticsTool("GrandMean")]
        [Description(
            "Combines the means of several groups into the overall mean, weighting each group mean by its sample size. Use it when you only have per-group means and counts rather than the raw data. " +
            "Both arrays must have the same length and every sample size must be at least 1. Value is the overall mean and Count is the total number of observations.")]
        public DescriptiveScalar GrandMean(
            [Description("The mean of each group.")]
            double[] groupMeans,
            [Description("The number of observations in each group, in the same order.")]
            int[] sampleSizes)
        {
            if (groupMeans == null || groupMeans.Length == 0)
            {
                throw new ArgumentException("groupMeans is empty. Supply one mean per group.", "groupMeans");
            }

            if (sampleSizes == null || sampleSizes.Length != groupMeans.Length)
            {
                throw new ArgumentException("sampleSizes must have one entry per group mean.", "sampleSizes");
            }

            if (sampleSizes.Any(s => s < 1) || groupMeans.Any(double.IsNaN))
            {
                throw new ArgumentException("Every sample size must be at least 1 and no group mean may be NaN.",
                    "sampleSizes");
            }

            return DescriptiveInput.Scalar(Measures.GrandMean(groupMeans, sampleSizes), sampleSizes.Sum(), 0);
        }

        [AdvancedAnalyticsTool("Median")]
        [Description(
            "Median: the middle value when the numbers are sorted, or the interpolated 50th percentile for an even count. It is the robust measure of the typical value and is unaffected by extreme outliers. " +
            "Value is the median; half the values are at or below it.")]
        public DescriptiveScalar Median(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(
                v.Median(false, DescriptiveInput.Map(DescriptiveQuantileInterpolation.Linear)), v.Length,
                missing);
        }

        [AdvancedAnalyticsTool("Trimean")]
        [Description(
            "Tukey's trimean: (lower quartile + 2 x median + upper quartile) / 4. A robust location estimate that blends the median with the quartiles and copes with skewed data better than the median alone. " +
            "Value is the trimean; quartiles use linear interpolation.")]
        public DescriptiveScalar Trimean(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            var q = v.Quantiles(new[] { 0.25, 0.5, 0.75 }, false,
                DescriptiveInput.Map(DescriptiveQuantileInterpolation.Linear));

            return DescriptiveInput.Scalar((q[0] + 2 * q[1] + q[2]) / 4.0, v.Length, missing);
        }

        [AdvancedAnalyticsTool("Midhinge")]
        [Description(
            "Midhinge: the average of the lower and upper quartiles, a robust measure of the centre of the middle half of the data. Compare it with the median: a large gap signals asymmetry in the middle of the distribution. " +
            "Value is the midhinge; quartiles use linear interpolation.")]
        public DescriptiveScalar Midhinge(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            var q = v.Quantiles(new[] { 0.25, 0.75 }, false,
                DescriptiveInput.Map(DescriptiveQuantileInterpolation.Linear));

            return DescriptiveInput.Scalar((q[0] + q[1]) / 2.0, v.Length, missing);
        }

        [AdvancedAnalyticsTool("MinimumMaximumRange")]
        [Description(
            "Smallest value, largest value, their difference (the range) and their midpoint (the midrange) in one call. Use it as the first look at the extent of a variable or to detect impossible values. " +
            "The range is very sensitive to outliers; use InterquartileRange for a robust spread.")]
        public DescriptiveRangeSummary MinimumMaximumRange(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var min = v.Min();
            var max = v.Max();

            return new DescriptiveRangeSummary
            {
                Minimum = min, Maximum = max, Range = max - min, Midrange = (min + max) / 2.0, Count = v.Length,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("Minimum")]
        [Description(
            "The smallest value in a list of numbers. Value is the minimum; Count is how many numbers were used and MissingIgnored how many NaN were dropped.")]
        public DescriptiveScalar Minimum(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.Min(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("Maximum")]
        [Description(
            "The largest value in a list of numbers. Value is the maximum; Count is how many numbers were used and MissingIgnored how many NaN were dropped.")]
        public DescriptiveScalar Maximum(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.Max(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("ArgumentOfMinimum")]
        [Description(
            "Finds where the smallest value sits: returns its zero-based Index in the original array (the first occurrence when tied) and the Value. Use it to locate the record behind an extreme low. " +
            "With ignoreMissing the index still refers to the original array including the missing entries.")]
        public DescriptiveLocatedValue ArgumentOfMinimum(
            [Description("The numbers, in original order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            return Locate(values, ignoreMissing, false);
        }

        [AdvancedAnalyticsTool("ArgumentOfMaximum")]
        [Description(
            "Finds where the largest value sits: returns its zero-based Index in the original array (the first occurrence when tied) and the Value. Use it to locate the record behind an extreme high. " +
            "With ignoreMissing the index still refers to the original array including the missing entries.")]
        public DescriptiveLocatedValue ArgumentOfMaximum(
            [Description("The numbers, in original order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            return Locate(values, ignoreMissing, true);
        }

        [AdvancedAnalyticsTool("Sum")]
        [Description(
            "Total of a list of numbers. Use it for aggregate amounts such as total volume or value. " +
            "Value is the sum; an overflow beyond about 1.8e308 returns infinity.")]
        public DescriptiveScalar Sum(
            [Description("The numbers to add.")] double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.Sum(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("Product")]
        [Description(
            "Product of a list of numbers, for example compounding growth factors. " +
            "Value is the product; very long lists can overflow to infinity or underflow to 0, in which case use GeometricMean or work on logarithms instead.")]
        public DescriptiveScalar Product(
            [Description("The numbers to multiply.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.Product(), v.Length, missing);
        }

        [AdvancedAnalyticsTool("CountValues")]
        [Description(
            "Counts how many entries are present, missing (NaN), infinite, zero, positive, negative and distinct in a list of numbers. Use it before analysis to understand data quality without any statistics that would fail on gaps. " +
            "MissingFraction is missing divided by total. Distinct counts unique non-missing values.")]
        public DescriptiveCountProfile CountValues(
            [Description("The numbers, which may include NaN.")]
            double[] values)
        {
            if (values == null)
            {
                throw new ArgumentException("values is required. Supply an array of numbers.", "values");
            }

            var missing = values.Count(double.IsNaN);
            var valid = values.Where(x => !double.IsNaN(x)).ToArray();

            return new DescriptiveCountProfile
            {
                Total = values.Length,
                Valid = valid.Length,
                Missing = missing,
                Infinite = valid.Count(double.IsInfinity),
                Distinct = valid.Distinct().Count(),
                Zeros = valid.Count(x => x == 0),
                Positives = valid.Count(x => x > 0),
                Negatives = valid.Count(x => x < 0),
                MissingFraction = values.Length == 0 ? double.NaN : (double)missing / values.Length
            };
        }

        [AdvancedAnalyticsTool("ModeOfNumbers")]
        [Description(
            "Most frequent value(s) in a list of numbers. Every value tied for the highest frequency is returned in Modes (ascending), with the shared Frequency and IsUnique false when there is more than one mode. " +
            "Use it for discrete data such as counts; for continuous data where all values differ every value is a mode with Frequency 1, so bin the data first.")]
        public DescriptiveNumericModeResult ModeOfNumbers(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            v.Mode(out var best);
            var groups = v.GroupBy(x => x).Where(g => g.Count() == best).Select(g => g.Key).OrderBy(x => x).ToArray();

            return new DescriptiveNumericModeResult
            {
                Modes = groups, Frequency = best, IsUnique = groups.Length == 1, Count = v.Length,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("ModeOfCategories")]
        [Description(
            "Most frequent label(s) in a list of text categories. Every label tied for the highest count is returned in Modes (ordinal order), with the shared Frequency. " +
            "Labels are compared exactly and case sensitively; an empty string counts as a label while null is treated as missing.")]
        public DescriptiveCategoryModeResult ModeOfCategories(
            [Description("The category labels, one per observation.")]
            string[] values,
            [Description("True skips null labels and reports them; false (default) rejects null.")]
            bool ignoreMissing = false)
        {
            var v = DescriptiveInput.CleanLabels(values, "values", ignoreMissing, out var missing);

            var counts = v.GroupBy(x => x, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

            var best = counts.Values.Max();

            var modes = counts.Where(p => p.Value == best).Select(p => p.Key).OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            return new DescriptiveCategoryModeResult
            {
                Modes = modes, Frequency = best, IsUnique = modes.Length == 1, Count = v.Length,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("WeightedMode")]
        [Description(
            "Value with the largest total weight, where each observation carries a weight (for example an amount or a repeat count). Ties are returned together in Modes with the shared total weight in Frequency. " +
            "Weights must be finite and not negative; Frequency here is the summed weight rounded to the nearest whole number, so read Count for the number of observations.")]
        public DescriptiveNumericModeResult WeightedMode(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("One non-negative weight per value, in the same order.")]
            double[] weights,
            [Description("True drops missing (NaN) values together with their weights; false (default) rejects NaN.")]
            bool ignoreMissing = false)
        {
            DescriptiveInput.PairClean(values, weights, "weights", ignoreMissing, out var v, out var w,
                out var missing);

            var totals = new Dictionary<double, double>();

            for (var i = 0; i < v.Length; i++)
            {
                totals[v[i]] = totals.TryGetValue(v[i], out var t) ? t + w[i] : w[i];
            }

            var best = totals.Values.Max();
            var modes = totals.Where(p => p.Value == best).Select(p => p.Key).OrderBy(x => x).ToArray();

            return new DescriptiveNumericModeResult
            {
                Modes = modes, Frequency = (int)Math.Min(int.MaxValue, Math.Round(best)), IsUnique = modes.Length == 1,
                Count = v.Length, MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("CircularMean")]
        [Description(
            "Mean direction of cyclical data such as hour of day, day of week, month or compass bearing, where 23:00 and 01:00 are close. An ordinary mean of hours 23 and 1 gives 12, the circular mean gives 0. " +
            "Choose the unit of the data; Value is the mean direction in the same unit within 0 up to the cycle length. It is NaN when the directions cancel out (resultant length near 0), so check CircularSummaryStatistics.")]
        public DescriptiveScalar CircularMean(
            [Description("The cyclical values in the chosen unit, for example hours 0-24 or degrees 0-360.")]
            double[] values,
            [Description("The unit of the values: Radians, Degrees, Hours24, Days7 or Months12. Default Hours24.")]
            DescriptiveAngleUnit unit = DescriptiveAngleUnit.Hours24,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(Circular.Mean(v, CycleLength(unit)), v.Length, missing);
        }

        [AdvancedAnalyticsTool("CircularSummaryStatistics")]
        [Description(
            "Descriptive statistics for cyclical data (hour of day, day of week, bearing): mean direction, median, mean resultant length (0 to 1, near 1 means values cluster at one time, near 0 means spread evenly around the cycle), circular variance (1 minus resultant), circular standard deviation, angular deviation, concentration, skewness and kurtosis. " +
            "Use it to test whether activity concentrates at particular times. Directions are returned in the chosen unit; the spread measures are in radians. Fields are NaN when undefined.")]
        public DescriptiveCircularSummary CircularSummaryStatistics(
            [Description("The cyclical values in the chosen unit.")]
            double[] values,
            [Description("The unit of the values: Radians, Degrees, Hours24, Days7 or Months12. Default Hours24.")]
            DescriptiveAngleUnit unit = DescriptiveAngleUnit.Hours24,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var length = CycleLength(unit);
            var radians = v.ToRadians(length);

            return new DescriptiveCircularSummary
            {
                MeanDirection = Circular.Mean(v, length),
                Median = Circular.Median(v, length),
                ResultantLength = Circular.Resultant(radians),
                CircularVariance = Circular.Variance(radians),
                CircularStandardDeviation = Circular.StandardDeviation(radians),
                AngularDeviation = Circular.AngularDeviation(radians),
                Concentration = Circular.Concentration(radians),
                Skewness = Circular.Skewness(radians),
                Kurtosis = Circular.Kurtosis(radians),
                Count = v.Length,
                MissingIgnored = missing
            };
        }

        private static double CycleLength(DescriptiveAngleUnit unit)
        {
            return unit switch
            {
                DescriptiveAngleUnit.Radians => 2 * Math.PI,
                DescriptiveAngleUnit.Degrees => 360.0,
                DescriptiveAngleUnit.Hours24 => 24.0,
                DescriptiveAngleUnit.Days7 => 7.0,
                DescriptiveAngleUnit.Months12 => 12.0,
                _ => throw new ArgumentException("Unknown unit. Use Radians, Degrees, Hours24, Days7 or Months12.",
                    "unit")
            };
        }

        private static void RequirePositive(double[] values, string kind)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (!(values[i] > 0) || double.IsInfinity(values[i]))
                {
                    throw new ArgumentException(
                        $"The {kind} mean needs strictly positive finite values; index {i} is {values[i].ToString(CultureInfo.InvariantCulture)}. Filter or shift the data first.",
                        "values");
                }
            }
        }

        private static DescriptiveLocatedValue Locate(double[] values, bool ignoreMissing, bool largest)
        {
            if (values == null)
            {
                throw new ArgumentException("values is required. Supply an array of numbers.", "values");
            }

            var bestIndex = -1;
            var missing = 0;

            for (var i = 0; i < values.Length; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    if (!ignoreMissing)
                    {
                        throw new ArgumentException(
                            $"values contains a missing (NaN) value at index {i}. Set ignoreMissing to true to skip it.",
                            "values");
                    }

                    missing++;
                    continue;
                }

                if (bestIndex < 0 || (largest ? values[i] > values[bestIndex] : values[i] < values[bestIndex]))
                {
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                throw new ArgumentException("values has no usable values. Supply at least one non-missing number.",
                    "values");
            }

            return new DescriptiveLocatedValue
            {
                Value = values[bestIndex], Index = bestIndex, Count = values.Length - missing, MissingIgnored = missing
            };
        }
    }
}