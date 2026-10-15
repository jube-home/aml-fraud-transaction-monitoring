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
    public class QuantileRankAndFrequencyTools
    {
        private const string IgnoreMissingText =
            "True drops missing (NaN) values before computing and reports how many were dropped; false (default) rejects input containing NaN.";

        private const string InterpolationText =
            "Quantile interpolation rule. Linear (default) matches common spreadsheets; Weibull is the alternative used by some statistics packages.";

        private const string SeriesMissingText =
            "Missing (NaN) entries stay NaN in the output at the same position and are skipped by the calculation.";

        private const long MaximumWindowWork = 200_000_000;

        [AdvancedAnalyticsTool("Quantile")]
        [Description(
            "The value below which a given share of the data falls, for example probability 0.9 gives the 90th percentile. Use it for thresholds, service levels and tail behaviour. " +
            "Probability is between 0 and 1. Value is the quantile, interpolated between neighbouring sorted values following the chosen rule.")]
        public DescriptiveScalar Quantile(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("Cumulative probability between 0 and 1, e.g. 0.95 for the 95th percentile.")]
            double probability,
            [Description(InterpolationText)] DescriptiveQuantileInterpolation interpolation =
                DescriptiveQuantileInterpolation.Linear,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            DescriptiveInput.RequireProbability(probability, "probability");
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(v.Quantile(probability, false, DescriptiveInput.Map(interpolation)),
                v.Length, missing);
        }

        [AdvancedAnalyticsTool("Quantiles")]
        [Description(
            "Several quantiles of the same data in one call, for example probabilities 0.05, 0.5 and 0.95. Use it to describe the shape of a distribution or to derive thresholds. " +
            "Each probability is between 0 and 1. Values contains one quantile per requested probability, in the same order.")]
        public DescriptiveQuantileTable Quantiles(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("Cumulative probabilities between 0 and 1, in any order.")]
            double[] probabilities,
            [Description(InterpolationText)] DescriptiveQuantileInterpolation interpolation =
                DescriptiveQuantileInterpolation.Linear,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (probabilities == null || probabilities.Length == 0)
            {
                throw new ArgumentException(
                    "probabilities is empty. Supply values between 0 and 1 such as 0.25, 0.5, 0.75.", "probabilities");
            }

            foreach (var p in probabilities)
            {
                DescriptiveInput.RequireProbability(p, "probabilities");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return new DescriptiveQuantileTable
            {
                Probabilities = probabilities,
                Values = v.Quantiles(probabilities, false, DescriptiveInput.Map(interpolation)),
                Count = v.Length,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("Percentiles")]
        [Description(
            "Percentiles of the data, where each level is between 0 and 100 (the 50th percentile is the median). Same as Quantiles but with percent levels, which is convenient for reports. " +
            "The returned Probabilities are the levels divided by 100 and Values the percentile values in the same order.")]
        public DescriptiveQuantileTable Percentiles(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("Percent levels between 0 and 100, for example 1, 5, 50, 95, 99.")]
            double[] percentLevels,
            [Description(InterpolationText)] DescriptiveQuantileInterpolation interpolation =
                DescriptiveQuantileInterpolation.Linear,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (percentLevels == null || percentLevels.Length == 0 ||
                percentLevels.Any(p => double.IsNaN(p) || p < 0 || p > 100))
            {
                throw new ArgumentException("percentLevels must be a non-empty list of numbers between 0 and 100.",
                    "percentLevels");
            }

            return Quantiles(values, percentLevels.Select(p => p / 100.0).ToArray(), interpolation, ignoreMissing);
        }

        [AdvancedAnalyticsTool("EqualProbabilityCutPoints")]
        [Description(
            "The cut points that divide the data into equal-sized groups: 4 parts gives quartiles, 5 quintiles, 10 deciles, 20 vigintiles, 100 percentiles. Use it to bin a variable into equal-frequency bands. " +
            "Returns parts minus 1 interior cut points in Values, with the matching probabilities in Probabilities (1/parts, 2/parts, ...).")]
        public DescriptiveQuantileTable EqualProbabilityCutPoints(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("How many equal groups, from 2 to 1000 (4 quartiles, 10 deciles, 100 percentiles).")]
            int parts,
            [Description(InterpolationText)] DescriptiveQuantileInterpolation interpolation =
                DescriptiveQuantileInterpolation.Linear,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (parts < 2 || parts > 1000)
            {
                throw new ArgumentException(
                    "parts must be between 2 and 1000, for example 4 for quartiles or 10 for deciles.", "parts");
            }

            var probabilities = Enumerable.Range(1, parts - 1).Select(i => (double)i / parts).ToArray();

            return Quantiles(values, probabilities, interpolation, ignoreMissing);
        }

        [AdvancedAnalyticsTool("InterpercentileRange")]
        [Description(
            "Distance between two percentiles, such as the 10th to 90th percentile range (a spread measure that ignores the most extreme tails) or 5th to 95th. It is a flexible, outlier-resistant spread. " +
            "Levels are percent values between 0 and 100 with lower below upper. Value is upper percentile minus lower percentile.")]
        public DescriptiveScalar InterpercentileRange(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("Lower percent level, 0 to 100, e.g. 10.")]
            double lowerPercent,
            [Description("Upper percent level, 0 to 100 and above the lower level, e.g. 90.")]
            double upperPercent,
            [Description(InterpolationText)] DescriptiveQuantileInterpolation interpolation =
                DescriptiveQuantileInterpolation.Linear,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (double.IsNaN(lowerPercent) || double.IsNaN(upperPercent) || lowerPercent < 0 || upperPercent > 100 ||
                lowerPercent >= upperPercent)
            {
                throw new ArgumentException(
                    "lowerPercent and upperPercent must satisfy 0 <= lowerPercent < upperPercent <= 100.",
                    "upperPercent");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            var q = v.Quantiles(new[] { lowerPercent / 100.0, upperPercent / 100.0 }, false,
                DescriptiveInput.Map(interpolation));

            return DescriptiveInput.Scalar(q[1] - q[0], v.Length, missing);
        }

        [AdvancedAnalyticsTool("FiveNumberSummary")]
        [Description(
            "Tukey five-number summary: minimum, lower quartile, median, upper quartile and maximum, plus the interquartile range and the standard box-plot fences (inner = 1.5 x IQR beyond the quartiles, outer = 3 x IQR). " +
            "Values beyond the inner fences are mild outliers and beyond the outer fences extreme outliers. Quartiles use the chosen interpolation.")]
        public DescriptiveFiveNumberSummaryResult FiveNumberSummary(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description(InterpolationText)] DescriptiveQuantileInterpolation interpolation =
                DescriptiveQuantileInterpolation.Linear,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var q = v.Quantiles(new[] { 0.25, 0.5, 0.75 }, false, DescriptiveInput.Map(interpolation));
            var iqr = q[2] - q[0];

            return new DescriptiveFiveNumberSummaryResult
            {
                Minimum = v.Min(),
                LowerQuartile = q[0],
                Median = q[1],
                UpperQuartile = q[2],
                Maximum = v.Max(),
                InterquartileRange = iqr,
                LowerInnerFence = q[0] - 1.5 * iqr,
                UpperInnerFence = q[2] + 1.5 * iqr,
                LowerOuterFence = q[0] - 3 * iqr,
                UpperOuterFence = q[2] + 3 * iqr,
                Count = v.Length,
                MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("PercentileRankOfValue")]
        [Description(
            "Where a single score sits within the data, as a percentile rank from 0 to 100: the percent of observations below it. Use it to say a transaction is larger than 97 percent of the customer's history. " +
            "Weak counts values at or below the score, Strict counts only values below, Mean averages both (the usual choice with ties). The score need not appear in the data.")]
        public DescriptiveScalar PercentileRankOfValue(
            [Description("The reference data, in any order.")]
            double[] values,
            [Description("The score whose position is wanted.")]
            double score,
            [Description("Weak, Strict or Mean handling of values equal to the score. Default Mean.")]
            DescriptivePercentileRankKind kind = DescriptivePercentileRankKind.Mean,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (double.IsNaN(score))
            {
                throw new ArgumentException("score must be a number, not NaN.", "score");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return DescriptiveInput.Scalar(RankOf(v, score, kind), v.Length, missing);
        }

        [AdvancedAnalyticsTool("PercentileRanksOfValues")]
        [Description(
            "The percentile rank (0 to 100) of every value within its own data, in the original order. Use it to convert a skewed variable into a uniform 0 to 100 scale or to score each observation relative to the rest. " +
            "Ties share a rank as set by the chosen kind. " + SeriesMissingText)]
        public DescriptiveSeriesResult PercentileRanksOfValues(
            [Description("The numbers; NaN stay NaN and are excluded from the reference data.")]
            double[] values,
            [Description("Weak, Strict or Mean handling of ties. Default Mean.")]
            DescriptivePercentileRankKind kind = DescriptivePercentileRankKind.Mean)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply an array of numbers.", "values");
            }

            var reference = DescriptiveInput.NonMissing(values);

            if (reference.Length == 0)
            {
                throw new ArgumentException("values has no usable values. Supply at least one non-missing number.",
                    "values");
            }

            var sorted = DescriptiveInput.Sorted(reference);
            var output = values.Select(x => double.IsNaN(x) ? double.NaN : RankOfSorted(sorted, x, kind)).ToArray();

            return new DescriptiveSeriesResult
                { Values = output, Count = reference.Length, MissingCount = values.Length - reference.Length };
        }

        [AdvancedAnalyticsTool("RanksOfValues")]
        [Description(
            "Rank of each value (1 is the smallest unless descending), in the original order, with a choice of tie handling: Average gives tied values the mean of their ranks (the standard for rank-based statistics), Minimum the lowest, Maximum the highest, Dense no gaps between ranks, Ordinal a distinct rank by position. " +
            "Returns HasTies and TiedGroups so you know whether ties occurred. NaN entries stay NaN and are not ranked.")]
        public DescriptiveRankResult RanksOfValues(
            [Description("The numbers to rank; NaN stay NaN.")]
            double[] values,
            [Description("Tie handling: Average (default), Minimum, Maximum, Dense or Ordinal.")]
            DescriptiveRankTieMethod ties = DescriptiveRankTieMethod.Average,
            [Description("True ranks the largest value 1; false (default) ranks the smallest value 1.")]
            bool descending = false)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply an array of numbers.", "values");
            }

            var positions = new List<int>();

            for (var i = 0; i < values.Length; i++)
            {
                if (!double.IsNaN(values[i]))
                {
                    positions.Add(i);
                }
            }

            var output = Enumerable.Repeat(double.NaN, values.Length).ToArray();
            var keys = positions.Select(p => descending ? -values[p] : values[p]).ToArray();
            var hasTies = false;

            if (keys.Length > 0)
            {
                keys.Rank(out hasTies);
                var order = Enumerable.Range(0, keys.Length).OrderBy(i => keys[i]).ThenBy(i => i).ToArray();
                var ranks = new double[keys.Length];
                var dense = 0;
                var start = 0;

                while (start < order.Length)
                {
                    var end = start;

                    while (end + 1 < order.Length && keys[order[end + 1]] == keys[order[start]])
                    {
                        end++;
                    }

                    dense++;

                    for (var k = start; k <= end; k++)
                    {
                        ranks[order[k]] = ties switch
                        {
                            DescriptiveRankTieMethod.Average => (start + end) / 2.0 + 1,
                            DescriptiveRankTieMethod.Minimum => start + 1,
                            DescriptiveRankTieMethod.Maximum => end + 1,
                            DescriptiveRankTieMethod.Dense => dense,
                            DescriptiveRankTieMethod.Ordinal => k + 1,
                            _ => throw new ArgumentException(
                                "Unknown tie method. Use Average, Minimum, Maximum, Dense or Ordinal.", "ties")
                        };
                    }

                    start = end + 1;
                }

                for (var i = 0; i < positions.Count; i++)
                {
                    output[positions[i]] = ranks[i];
                }
            }

            var tiedGroups = keys.GroupBy(x => x).Count(g => g.Count() > 1);

            return new DescriptiveRankResult
            {
                Ranks = output, HasTies = hasTies, TiedGroups = tiedGroups, Count = keys.Length,
                MissingCount = values.Length - keys.Length
            };
        }

        [AdvancedAnalyticsTool("ThresholdShare")]
        [Description(
            "Counts and proportions of observations above, below and equal to a threshold, for example the share of transactions over a reporting limit. " +
            "Proportions are relative to the number of non-missing values. Use it for exceedance rates and simple rule-hit-rate estimates.")]
        public DescriptiveThresholdShare ThresholdShare(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("The cut-off value to compare against.")]
            double threshold,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            if (double.IsNaN(threshold))
            {
                throw new ArgumentException("threshold must be a number, not NaN.", "threshold");
            }

            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);
            var above = v.Count(x => x > threshold);
            var below = v.Count(x => x < threshold);
            var equal = v.Length - above - below;

            return new DescriptiveThresholdShare
            {
                Threshold = threshold, Above = above, Below = below, Equal = equal, Count = v.Length,
                ProportionAbove = (double)above / v.Length, ProportionBelow = (double)below / v.Length,
                ProportionEqual = (double)equal / v.Length, MissingIgnored = missing
            };
        }

        [AdvancedAnalyticsTool("CumulativeSum")]
        [Description(
            "Running total of a series: element i is the sum of elements 0 to i. Use it for cumulative amounts over time or to build Lorenz-type curves. " +
            SeriesMissingText)]
        public DescriptiveSeriesResult CumulativeSum(
            [Description("The series in order.")] double[] values)
        {
            return Accumulate(values, x => x.CumulativeSum());
        }

        [AdvancedAnalyticsTool("CumulativeProduct")]
        [Description(
            "Running product of a series: element i is the product of elements 0 to i, for compounding growth factors (1.02, 0.99, ...) into a cumulative index. " +
            "Long series can overflow to infinity or underflow to 0. " + SeriesMissingText)]
        public DescriptiveSeriesResult CumulativeProduct(
            [Description("The series in order, typically growth factors.")]
            double[] values)
        {
            return Accumulate(values, x =>
            {
                var result = new double[x.Length];
                var running = 1.0;

                for (var i = 0; i < x.Length; i++)
                {
                    running *= x[i];
                    result[i] = running;
                }

                return result;
            });
        }

        [AdvancedAnalyticsTool("CumulativeMaximum")]
        [Description(
            "Running maximum of a series: element i is the largest of elements 0 to i (a high-water mark). Use it for peak tracking and drawdown analysis. " +
            SeriesMissingText)]
        public DescriptiveSeriesResult CumulativeMaximum(
            [Description("The series in order.")] double[] values)
        {
            return Accumulate(values, x => RunningExtreme(x, true));
        }

        [AdvancedAnalyticsTool("CumulativeMinimum")]
        [Description(
            "Running minimum of a series: element i is the smallest of elements 0 to i (a low-water mark). " +
            SeriesMissingText)]
        public DescriptiveSeriesResult CumulativeMinimum(
            [Description("The series in order.")] double[] values)
        {
            return Accumulate(values, x => RunningExtreme(x, false));
        }

        [AdvancedAnalyticsTool("RollingStatistic")]
        [Description(
            "A statistic computed over a trailing window of the last N observations at each position: rolling mean, sum, median, minimum, maximum, standard deviation, variance, range, skewness or count. Use it for moving averages and moving volatility of a time-ordered series. " +
            "Element i uses observations i-window+1 to i; NaN inside a window are skipped, and the output is NaN until at least minPeriods valid values are available (default the full window) or when the statistic is undefined for that few values (standard deviation and variance need 2, skewness 3).")]
        public DescriptiveSeriesResult RollingStatistic(
            [Description("The series in time order.")]
            double[] values,
            [Description("Window length in observations, at least 1.")]
            int window,
            [Description("Which statistic to compute over each window.")]
            DescriptiveWindowStatistic statistic = DescriptiveWindowStatistic.Mean,
            [Description(
                "Minimum valid values needed in a window to produce a result; 0 or less means the full window length.")]
            int minPeriods = 0)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply a series of numbers.", "values");
            }

            if (window < 1 || window > values.Length)
            {
                throw new ArgumentException($"window must be between 1 and the series length ({values.Length}).",
                    "window");
            }

            if ((long)values.Length * window > MaximumWindowWork)
            {
                throw new ArgumentException(
                    "The series and window are too large to compute in one call; use a shorter window or a sample.",
                    "window");
            }

            var required = minPeriods <= 0 ? window : Math.Min(minPeriods, window);
            var output = new double[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                var buffer = new List<double>(window);

                for (var j = Math.Max(0, i - window + 1); j <= i; j++)
                {
                    if (!double.IsNaN(values[j]))
                    {
                        buffer.Add(values[j]);
                    }
                }

                output[i] = buffer.Count >= required ? Evaluate(buffer.ToArray(), statistic) : double.NaN;
            }

            return new DescriptiveSeriesResult
                { Values = output, Count = values.Length, MissingCount = values.Count(double.IsNaN) };
        }

        [AdvancedAnalyticsTool("ExpandingStatistic")]
        [Description(
            "A statistic computed over everything seen so far at each position (an expanding window): running mean, sum, median, minimum, maximum, standard deviation, variance, range, skewness or count. " +
            "Use it to see how an estimate settles as data accumulates. NaN are skipped; the output is NaN until enough values exist for the statistic. Limited to 20000 observations; use CumulativeSum for longer series.")]
        public DescriptiveSeriesResult ExpandingStatistic(
            [Description("The series in time order.")]
            double[] values,
            [Description("Which statistic to compute over the growing window.")]
            DescriptiveWindowStatistic statistic = DescriptiveWindowStatistic.Mean,
            [Description("Minimum valid values before a result is produced; default 1.")]
            int minPeriods = 1)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply a series of numbers.", "values");
            }

            if (values.Length > 20000)
            {
                throw new ArgumentException(
                    "ExpandingStatistic accepts at most 20000 observations; use CumulativeSum, or RollingStatistic with a window.",
                    "values");
            }

            var buffer = new List<double>(values.Length);
            var output = new double[values.Length];
            var required = Math.Max(1, minPeriods);

            for (var i = 0; i < values.Length; i++)
            {
                if (!double.IsNaN(values[i]))
                {
                    buffer.Add(values[i]);
                }

                output[i] = buffer.Count >= required ? Evaluate(buffer.ToArray(), statistic) : double.NaN;
            }

            return new DescriptiveSeriesResult
                { Values = output, Count = values.Length, MissingCount = values.Count(double.IsNaN) };
        }

        [AdvancedAnalyticsTool("ExponentialMovingAverageSeries")]
        [Description(
            "Exponential moving average of a time-ordered series, one value per position: each output is alpha times the current value plus (1 - alpha) times the previous output, starting from the first value. Larger alpha reacts faster; use it as a smooth recency-weighted baseline. " +
            "NaN entries are carried through as NaN and do not update the average.")]
        public DescriptiveSeriesResult ExponentialMovingAverageSeries(
            [Description("The series in time order.")]
            double[] values,
            [Description("Smoothing factor greater than 0 and at most 1. Default 0.3.")]
            double alpha = 0.3)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply a series of numbers.", "values");
            }

            if (double.IsNaN(alpha) || alpha <= 0 || alpha > 1)
            {
                throw new ArgumentException("alpha must be greater than 0 and at most 1.", "alpha");
            }

            var output = new double[values.Length];
            var current = double.NaN;

            for (var i = 0; i < values.Length; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    output[i] = double.NaN;
                    continue;
                }

                current = double.IsNaN(current) ? values[i] : alpha * values[i] + (1 - alpha) * current;
                output[i] = current;
            }

            return new DescriptiveSeriesResult
                { Values = output, Count = values.Length, MissingCount = values.Count(double.IsNaN) };
        }

        [AdvancedAnalyticsTool("FrequencyTableOfCategories")]
        [Description(
            "Value counts for text categories: for each distinct label its count, proportion, percent and cumulative share. Use it to see the mix of merchant categories, countries, channels or statuses and to spot dominant or rare values. " +
            "Labels are compared exactly (case sensitive). Order by count (default), by label, or by first appearance; maxRows limits the rows returned and RowsOmitted reports how many were left out. Null labels count as missing.")]
        public DescriptiveFrequencyTableResult FrequencyTableOfCategories(
            [Description("The category label for each observation.")]
            string[] values,
            [Description("Row order: ByCountDescending (default), ByValueAscending or ByFirstAppearance.")]
            DescriptiveFrequencyOrder order = DescriptiveFrequencyOrder.ByCountDescending,
            [Description("Maximum rows to return, at least 1. Default 50.")]
            int maxRows = 50,
            [Description("True skips null labels and reports them; false (default) rejects null.")]
            bool ignoreMissing = false)
        {
            var v = DescriptiveInput.CleanLabels(values, "values", ignoreMissing, out var missing);

            return BuildTable(v.Select((label, index) => (label, index)).ToArray(), order, maxRows, missing);
        }

        [AdvancedAnalyticsTool("FrequencyTableOfIntegers")]
        [Description(
            "Value counts for integer codes or discrete counts (day of week, number of items, status codes): each distinct value with its count, proportion, percent and cumulative share. " +
            "Order by count (default), by value ascending, or by first appearance; maxRows limits the rows and RowsOmitted reports the rest.")]
        public DescriptiveFrequencyTableResult FrequencyTableOfIntegers(
            [Description("The integer value for each observation.")]
            int[] values,
            [Description("Row order: ByCountDescending (default), ByValueAscending or ByFirstAppearance.")]
            DescriptiveFrequencyOrder order = DescriptiveFrequencyOrder.ByCountDescending,
            [Description("Maximum rows to return, at least 1. Default 50.")]
            int maxRows = 50)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply an array of integers.", "values");
            }

            return BuildTable(values.Select((x, index) => (x.ToString(CultureInfo.InvariantCulture), index)).ToArray(),
                order, maxRows, 0,
                order == DescriptiveFrequencyOrder.ByValueAscending
                    ? (a, b) => int.Parse(a, CultureInfo.InvariantCulture)
                        .CompareTo(int.Parse(b, CultureInfo.InvariantCulture))
                    : null);
        }

        [AdvancedAnalyticsTool("FrequencyTableOfNumbers")]
        [Description(
            "Value counts for exact numeric values, such as repeated amounts (many payments of exactly 9.99 or 1000.00): each distinct value with its count, proportion, percent and cumulative share. " +
            "It is the quickest way to find round-number or repeated-amount structuring. Values are matched exactly, so for continuous data use a histogram instead. NaN are missing.")]
        public DescriptiveFrequencyTableResult FrequencyTableOfNumbers(
            [Description("The numbers, in any order.")]
            double[] values,
            [Description("Row order: ByCountDescending (default), ByValueAscending or ByFirstAppearance.")]
            DescriptiveFrequencyOrder order = DescriptiveFrequencyOrder.ByCountDescending,
            [Description("Maximum rows to return, at least 1. Default 50.")]
            int maxRows = 50,
            [Description(IgnoreMissingText)] bool ignoreMissing = false)
        {
            var v = DescriptiveInput.Clean(values, "values", ignoreMissing, 1, out var missing);

            return BuildTable(v.Select((x, index) => (x.ToString("R", CultureInfo.InvariantCulture), index)).ToArray(),
                order, maxRows, missing,
                order == DescriptiveFrequencyOrder.ByValueAscending
                    ? (a, b) => double.Parse(a, CultureInfo.InvariantCulture)
                        .CompareTo(double.Parse(b, CultureInfo.InvariantCulture))
                    : null);
        }

        [AdvancedAnalyticsTool("RareCategories")]
        [Description(
            "Lists the labels that occur at most a given number of times (default once): singletons and rare categories that often indicate typos, new counterparties or unusual behaviour. " +
            "Rows are ordered by count ascending then label; Total is the number of observations, Distinct the number of distinct labels overall, and RowsOmitted the rare labels beyond maxRows.")]
        public DescriptiveFrequencyTableResult RareCategories(
            [Description("The category label for each observation.")]
            string[] values,
            [Description("A label is rare when it occurs at most this many times, at least 1. Default 1.")]
            int maxCount = 1,
            [Description("Maximum rows to return, at least 1. Default 50.")]
            int maxRows = 50,
            [Description("True skips null labels and reports them; false (default) rejects null.")]
            bool ignoreMissing = false)
        {
            if (maxCount < 1 || maxRows < 1)
            {
                throw new ArgumentException("maxCount and maxRows must both be at least 1.",
                    maxCount < 1 ? "maxCount" : "maxRows");
            }

            var v = DescriptiveInput.CleanLabels(values, "values", ignoreMissing, out var missing);
            var groups = v.GroupBy(x => x, StringComparer.Ordinal).Select(g => (g.Key, Count: g.Count())).ToArray();

            var rare = groups.Where(g => g.Count <= maxCount).OrderBy(g => g.Count)
                .ThenBy(g => g.Key, StringComparer.Ordinal).ToArray();

            var shown = rare.Take(maxRows).ToArray();
            var cumulative = 0;

            var rows = shown.Select(g =>
            {
                cumulative += g.Count;

                return new DescriptiveFrequencyRow
                {
                    Value = g.Key, Count = g.Count, Proportion = (double)g.Count / v.Length,
                    Percent = 100.0 * g.Count / v.Length, CumulativeCount = cumulative,
                    CumulativeProportion = (double)cumulative / v.Length
                };
            }).ToArray();

            return new DescriptiveFrequencyTableResult
            {
                Rows = rows, Total = v.Length, Distinct = groups.Length, MissingCount = missing,
                RowsOmitted = rare.Length - shown.Length
            };
        }

        private static DescriptiveFrequencyTableResult BuildTable((string label, int index)[] items,
            DescriptiveFrequencyOrder order,
            int maxRows, int missing, Comparison<string>? valueComparison = null)
        {
            if (maxRows < 1)
            {
                throw new ArgumentException("maxRows must be at least 1.", "maxRows");
            }

            var groups = items.GroupBy(x => x.label, StringComparer.Ordinal)
                .Select(g => (g.Key, Count: g.Count(), First: g.Min(x => x.index))).ToList();

            switch (order)
            {
                case DescriptiveFrequencyOrder.ByCountDescending:
                    groups = groups.OrderByDescending(g => g.Count).ThenBy(g => g.First).ToList();
                    break;
                case DescriptiveFrequencyOrder.ByValueAscending:
                    groups.Sort((a, b) => valueComparison != null
                        ? valueComparison(a.Key, b.Key)
                        : string.CompareOrdinal(a.Key, b.Key));

                    break;
                case DescriptiveFrequencyOrder.ByFirstAppearance:
                    groups = groups.OrderBy(g => g.First).ToList();
                    break;
                default:
                    throw new ArgumentException(
                        "Unknown order. Use ByCountDescending, ByValueAscending or ByFirstAppearance.", "order");
            }

            var total = items.Length;
            var running = 0;
            var rows = new List<DescriptiveFrequencyRow>();

            foreach (var g in groups)
            {
                running += g.Count;

                if (rows.Count < maxRows)
                {
                    rows.Add(new DescriptiveFrequencyRow
                    {
                        Value = g.Key, Count = g.Count, Proportion = (double)g.Count / total,
                        Percent = 100.0 * g.Count / total,
                        CumulativeCount = running, CumulativeProportion = (double)running / total
                    });
                }
            }

            return new DescriptiveFrequencyTableResult
            {
                Rows = rows.ToArray(), Total = total, Distinct = groups.Count, MissingCount = missing,
                RowsOmitted = groups.Count - rows.Count
            };
        }

        private static double RankOf(double[] values, double score, DescriptivePercentileRankKind kind)
        {
            return RankOfSorted(DescriptiveInput.Sorted(values), score, kind);
        }

        private static double RankOfSorted(double[] sorted, double score, DescriptivePercentileRankKind kind)
        {
            var below = 0;
            var atOrBelow = 0;

            foreach (var x in sorted)
            {
                if (x < score)
                {
                    below++;
                }

                if (x <= score)
                {
                    atOrBelow++;
                }
            }

            var n = (double)sorted.Length;

            return kind switch
            {
                DescriptivePercentileRankKind.Weak => 100.0 * atOrBelow / n,
                DescriptivePercentileRankKind.Strict => 100.0 * below / n,
                DescriptivePercentileRankKind.Mean => 50.0 * (below + atOrBelow) / n,
                _ => throw new ArgumentException("Unknown kind. Use Weak, Strict or Mean.", "kind")
            };
        }

        private static DescriptiveSeriesResult Accumulate(double[] values, Func<double[], double[]> accumulate)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("values is empty. Supply a series of numbers.", "values");
            }

            var positions = new List<int>();

            for (var i = 0; i < values.Length; i++)
            {
                if (!double.IsNaN(values[i]))
                {
                    positions.Add(i);
                }
            }

            var output = Enumerable.Repeat(double.NaN, values.Length).ToArray();

            if (positions.Count > 0)
            {
                var result = accumulate(positions.Select(p => values[p]).ToArray());

                for (var i = 0; i < positions.Count; i++)
                {
                    output[positions[i]] = result[i];
                }
            }

            return new DescriptiveSeriesResult
                { Values = output, Count = positions.Count, MissingCount = values.Length - positions.Count };
        }

        private static double[] RunningExtreme(double[] values, bool maximum)
        {
            var result = new double[values.Length];
            var best = values[0];

            for (var i = 0; i < values.Length; i++)
            {
                best = maximum ? Math.Max(best, values[i]) : Math.Min(best, values[i]);
                result[i] = best;
            }

            return result;
        }

        private static double Evaluate(double[] window, DescriptiveWindowStatistic statistic)
        {
            return statistic switch
            {
                DescriptiveWindowStatistic.Count => window.Length,
                DescriptiveWindowStatistic.Mean => window.Length >= 1 ? window.Mean() : double.NaN,
                DescriptiveWindowStatistic.Sum => window.Length >= 1 ? window.Sum() : double.NaN,
                DescriptiveWindowStatistic.Median => window.Length >= 1 ? window.Median() : double.NaN,
                DescriptiveWindowStatistic.Minimum => window.Length >= 1 ? window.Min() : double.NaN,
                DescriptiveWindowStatistic.Maximum => window.Length >= 1 ? window.Max() : double.NaN,
                DescriptiveWindowStatistic.Range => window.Length >= 1 ? window.Max() - window.Min() : double.NaN,
                DescriptiveWindowStatistic.StandardDeviation => window.Length >= 2
                    ? window.StandardDeviation()
                    : double.NaN,
                DescriptiveWindowStatistic.Variance => window.Length >= 2 ? window.Variance(true) : double.NaN,
                DescriptiveWindowStatistic.Skewness => window.Length >= 3 ? window.Skewness() : double.NaN,
                _ => throw new ArgumentException("Unknown statistic.", "statistic")
            };
        }
    }
}