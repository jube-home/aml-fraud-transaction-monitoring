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
using Accord.Math.Optimization;
using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    public enum WindowAlignment
    {
        Trailing,
        Centered,
        Leading
    }

    public enum WindowStatistic
    {
        Mean,
        Sum,
        Minimum,
        Maximum,
        Median,
        StandardDeviation,
        Variance,
        Range,
        MedianAbsoluteDeviation,
        Skewness,
        Kurtosis,
        Count,
        Quantile,
        CoefficientOfVariation
    }

    public enum CumulativeOperation
    {
        Sum,
        Product,
        Maximum,
        Minimum,
        Mean
    }

    public enum DetrendMethod
    {
        Mean,
        Linear,
        Polynomial
    }

    public enum MissingFillMethod
    {
        LinearInterpolation,
        ForwardFill,
        BackwardFill,
        NearestValue,
        SeriesMean,
        SeriesMedian,
        Constant
    }

    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SeriesTransformTools
    {
        [AdvancedAnalyticsTool("DifferenceSeries")]
        [Description(
            "Differences a series: replaces each value with its change from the value 'lag' steps earlier, optionally repeated 'order' times. Use first differences (order 1) to remove a trend before autocorrelation or forecasting work, and a seasonal lag (for example 7 for daily data) to remove a repeating pattern. " +
            "The result is the same length as the input so it stays aligned in time; the first order x lag positions have no earlier value and are NaN. A NaN in the input makes the affected differences NaN. " +
            "Differencing a straight line once gives a constant; differencing twice gives zeros.")]
        public double[] DifferenceSeries(
            [Description("The series in time order. NaN is allowed and propagates.")]
            double[] values,
            [Description("How many times the differencing is applied. Default 1.")]
            int order = 1,
            [Description(
                "Step distance of each difference: 1 for change from the previous value, 7 for change from a week ago in daily data. Default 1.")]
            int lag = 1)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            TimeSeriesGuard.AtLeast(order, 1, nameof(order));
            TimeSeriesGuard.AtLeast(lag, 1, nameof(lag));
            var current = (double[])v.Clone();

            for (var pass = 0; pass < order; pass++)
            {
                var next = new double[current.Length];

                for (var i = 0; i < next.Length; i++)
                {
                    next[i] = i < lag ? double.NaN : current[i] - current[i - lag];
                }

                current = next;
            }

            return current;
        }

        [AdvancedAnalyticsTool("LagSeries")]
        [Description(
            "Shifts a series in time so that each position holds the value from 'lag' steps earlier (a lag) or, with a negative lag, from later steps (a lead). Use it to line up a series with its own past or future when building features or comparing periods. " +
            "The result is the same length as the input; positions with no source value are NaN. For lag 2 the output is NaN, NaN, x0, x1, x2 and so on.")]
        public double[] LagSeries(
            [Description("The series in time order. NaN is allowed and moves with the data.")]
            double[] values,
            [Description(
                "Steps to shift: positive for earlier values (lag), negative for later values (lead). Default 1.")]
            int lag = 1)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            var result = new double[v.Length];

            for (var i = 0; i < v.Length; i++)
            {
                var source = i - lag;
                result[i] = source >= 0 && source < v.Length ? v[source] : double.NaN;
            }

            return result;
        }

        [AdvancedAnalyticsTool("LaggedFeatureMatrix")]
        [Description(
            "Turns a series into a supervised learning table: each row holds the values at several earlier lags and the target is the value at the current time. Use it to feed an ordinary regression or classifier with autoregressive features, for example lags 1, 2 and 7 of daily volume. " +
            "Features[i][j] is the value at Lags[j] steps before the target of row i; Targets[i] is the series value at position RowIndices[i]. Rows start once the largest lag is available, so the table has length minus the largest lag rows. Requires finite values.")]
        public LaggedFeatureMatrixResult LaggedFeatureMatrix(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("The lags to use as features, each 1 or more, for example 1, 2, 7. Order is kept.")]
            int[] lags)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);

            if (lags == null || lags.Length == 0 || lags.Any(l => l < 1))
            {
                throw new ArgumentException(
                    "lags must list at least one lag and every lag must be 1 or more, for example [1, 2, 7].",
                    nameof(lags));
            }

            var max = lags.Max();

            if (v.Length <= max)
            {
                throw new ArgumentException(
                    $"values has {v.Length} points but the largest lag is {max}; supply more history or smaller lags.",
                    nameof(lags));
            }

            var rows = v.Length - max;
            var features = new double[rows][];
            var targets = new double[rows];
            var indices = new int[rows];

            for (var i = 0; i < rows; i++)
            {
                var t = i + max;
                indices[i] = t;
                targets[i] = v[t];
                features[i] = lags.Select(l => v[t - l]).ToArray();
            }

            return new LaggedFeatureMatrixResult(features, targets, indices, lags);
        }

        [AdvancedAnalyticsTool("CumulativeSeries")]
        [Description(
            "Running (cumulative) total, product, maximum, minimum or mean of a series: element i summarises positions 0 to i. Use the cumulative sum to turn changes into a level or to build a running balance, the cumulative maximum to track a high-water mark, and the cumulative mean for a running average. " +
            "The result is the same length as the input. A NaN in the input makes the running value NaN from that point onward for Sum, Product and Mean, while Maximum and Minimum ignore NaN.")]
        public double[] CumulativeSeries(
            [Description("The series in time order.")]
            double[] values,
            [Description("Sum, Product, Maximum, Minimum or Mean. Default Sum.")]
            CumulativeOperation operation = CumulativeOperation.Sum)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            var result = new double[v.Length];
            var accumulator = operation == CumulativeOperation.Product ? 1.0 : 0.0;
            var seen = false;
            var compensation = 0.0;

            for (var i = 0; i < v.Length; i++)
            {
                switch (operation)
                {
                    case CumulativeOperation.Sum:
                    case CumulativeOperation.Mean:
                        var y = v[i] - compensation;
                        var t = accumulator + y;
                        compensation = double.IsFinite(t) ? t - accumulator - y : 0;
                        accumulator = t;
                        result[i] = operation == CumulativeOperation.Sum ? accumulator : accumulator / (i + 1);
                        break;
                    case CumulativeOperation.Product:
                        accumulator *= v[i];
                        result[i] = accumulator;
                        break;
                    case CumulativeOperation.Maximum:
                        if (!double.IsNaN(v[i]) && (!seen || v[i] > accumulator))
                        {
                            accumulator = v[i];
                            seen = true;
                        }

                        result[i] = seen ? accumulator : double.NaN;
                        break;
                    default:
                        if (!double.IsNaN(v[i]) && (!seen || v[i] < accumulator))
                        {
                            accumulator = v[i];
                            seen = true;
                        }

                        result[i] = seen ? accumulator : double.NaN;
                        break;
                }
            }

            return result;
        }

        [AdvancedAnalyticsTool("PercentChangeSeries")]
        [Description(
            "Relative change of a series over 'periods' steps: value divided by the value that many steps earlier, minus 1 (0.10 means up 10 percent). Use it for growth rates of volumes or balances; for prices prefer LogReturnSeries. " +
            "The result is the same length as the input; the first 'periods' positions and any position whose earlier value is 0 or NaN are NaN.")]
        public double[] PercentChangeSeries(
            [Description("The series in time order. NaN is allowed and propagates.")]
            double[] values,
            [Description("Step distance of the comparison, default 1.")]
            int periods = 1)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            TimeSeriesGuard.AtLeast(periods, 1, nameof(periods));
            var result = new double[v.Length];

            for (var i = 0; i < v.Length; i++)
            {
                result[i] = i < periods || v[i - periods] == 0 ? double.NaN : v[i] / v[i - periods] - 1;
            }

            return result;
        }

        [AdvancedAnalyticsTool("LogReturnSeries")]
        [Description(
            "Logarithmic return of a strictly positive series over 'periods' steps: the natural log of value divided by the earlier value. Log returns add up over time and are approximately symmetric, which makes them the usual input for volatility and spike work. " +
            "The result is the same length as the input; the first 'periods' positions and any position where either value is 0 or negative or NaN are NaN.")]
        public double[] LogReturnSeries(
            [Description("A series of positive numbers in time order. NaN is allowed and propagates.")]
            double[] values,
            [Description("Step distance of the comparison, default 1.")]
            int periods = 1)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            TimeSeriesGuard.AtLeast(periods, 1, nameof(periods));
            var result = new double[v.Length];

            for (var i = 0; i < v.Length; i++)
            {
                result[i] = i < periods || !(v[i] > 0) || !(v[i - periods] > 0)
                    ? double.NaN
                    : Math.Log(v[i] / v[i - periods]);
            }

            return result;
        }

        [AdvancedAnalyticsTool("SimpleMovingAverageSeries")]
        [Description(
            "Simple moving average: each output is the plain average of the values in a sliding window, smoothing out short-term noise. Use a trailing window for something you could compute live, a centred window to see the underlying trend in hindsight. " +
            "The result is the same length as the input. By default positions where the window is not full (near the edges) are NaN; lower minPeriods to average partial windows. Missing (NaN) inputs are skipped and the position is NaN when fewer than minPeriods values remain.")]
        public double[] SimpleMovingAverageSeries(
            [Description("The series in time order. NaN is allowed and skipped.")]
            double[] values,
            [Description("Window length in observations, at least 1.")]
            int window,
            [Description(
                "Trailing (window ends at the current point), Centered or Leading (window starts at the current point). Default Trailing.")]
            WindowAlignment alignment = WindowAlignment.Trailing,
            [Description("Minimum number of finite values needed in the window; 0 (default) means the whole window.")]
            int minPeriods = 0)
        {
            return RollingCore(values, window, WindowStatistic.Mean, alignment, minPeriods, double.NaN);
        }

        [AdvancedAnalyticsTool("WeightedMovingAverageSeries")]
        [Description(
            "Weighted moving average: each output is the weighted average of the latest values, with the weights you supply applied from oldest to newest, so recent points can count more (for example weights 1, 2, 3 for a linear-weighted average of three points). " +
            "The weights are normalised by their sum. The result is the same length as the input and the first (number of weights minus 1) positions are NaN. Requires finite values and weights that sum to a non-zero number.")]
        public double[] WeightedMovingAverageSeries(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description(
                "The weights from the oldest value in the window to the newest; the window length is the number of weights.")]
            double[] weights)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 1);
            var w = TimeSeriesGuard.Finite(weights, nameof(weights), 1);

            if (Math.Abs(w.Sum()) < 1e-300)
            {
                throw new ArgumentException(
                    "weights sum to zero, so the average is undefined. Supply weights with a non-zero total.",
                    nameof(weights));
            }

            var total = w.Sum();
            var result = new double[v.Length];

            for (var i = 0; i < v.Length; i++)
            {
                if (i < w.Length - 1)
                {
                    result[i] = double.NaN;
                    continue;
                }

                var sum = 0.0;

                for (var j = 0; j < w.Length; j++)
                {
                    sum += w[j] * v[i - w.Length + 1 + j];
                }

                result[i] = sum / total;
            }

            return result;
        }

        [AdvancedAnalyticsTool("ExponentiallyWeightedSmoothingSeries")]
        [Description(
            "Exponentially weighted moving average: recent observations count most and older ones fade geometrically, so it reacts faster than a simple average without a hard window. Give either alpha (the weight on the newest value, between 0 and 1) or a span (roughly the number of periods it averages over, alpha = 2 / (span + 1)). " +
            "With adjust false (default) the recursion is average = alpha x value + (1 - alpha) x previous average, starting from the first value. With adjust true every output is a proper weighted average of all values so far, which removes the start-up bias. " +
            "The result is the same length as the input. Requires finite values and exactly one of alpha or span.")]
        public double[] ExponentiallyWeightedSmoothingSeries(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Smoothing weight on the newest value, above 0 and at most 1. Leave NaN when giving span.")]
            double alpha = double.NaN,
            [Description("Span in periods, at least 1, used when alpha is NaN.")]
            double span = double.NaN,
            [Description("False (default) uses the recursive form; true divides by the running sum of weights.")]
            bool adjust = false)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 1);
            var a = ResolveAlpha(alpha, span);
            var result = new double[v.Length];
            var numerator = 0.0;
            var denominator = 0.0;

            for (var i = 0; i < v.Length; i++)
            {
                if (adjust)
                {
                    numerator = numerator * (1 - a) + v[i];
                    denominator = denominator * (1 - a) + 1;
                    result[i] = numerator / denominator;
                }
                else
                {
                    result[i] = i == 0 ? v[0] : a * v[i] + (1 - a) * result[i - 1];
                }
            }

            return result;
        }

        [AdvancedAnalyticsTool("ExponentialMovingStatisticsSeries")]
        [Description(
            "Exponentially weighted mean and standard deviation of a series, updated recursively so recent behaviour dominates. Use it as an adaptive baseline: compare each new value with the running mean in units of the running standard deviation to spot departures that a fixed-window statistic reacts to too slowly. " +
            "Means[i] and StandardDeviations[i] describe the weighted history up to and including position i (the first standard deviation is 0). The variance recursion is v = (1 - alpha)(v + alpha x (value - previous mean) squared). Give alpha or span, exactly one. Requires finite values.")]
        public ExponentialStatisticsResult ExponentialMovingStatisticsSeries(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Smoothing weight on the newest value, above 0 and at most 1. Leave NaN when giving span.")]
            double alpha = double.NaN,
            [Description("Span in periods, at least 1, used when alpha is NaN.")]
            double span = double.NaN)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 1);
            var a = ResolveAlpha(alpha, span);
            var means = new double[v.Length];
            var deviations = new double[v.Length];
            var mean = v[0];
            var variance = 0.0;
            means[0] = mean;

            for (var i = 1; i < v.Length; i++)
            {
                var d = v[i] - mean;
                mean += a * d;
                variance = (1 - a) * (variance + a * d * d);
                means[i] = mean;
                deviations[i] = Math.Sqrt(variance);
            }

            return new ExponentialStatisticsResult(means, deviations, a);
        }

        [AdvancedAnalyticsTool("RollingStatisticSeries")]
        [Description(
            "Rolling (moving window) statistic of a series: Mean, Sum, Minimum, Maximum, Median, StandardDeviation (sample), Variance (sample), Range, MedianAbsoluteDeviation, Skewness, Kurtosis, Count of finite values, Quantile (using the quantile argument) or CoefficientOfVariation. Use it to build features such as the mean and volatility of the last 30 transactions, or to see how a metric's level and spread evolve. " +
            "The result is the same length as the input. Missing (NaN) or infinite values are ignored inside a window; a position is NaN when fewer than minPeriods finite values are in its window (default: the full window, so the start of a trailing window is NaN). Statistics that need more points than are present (for example standard deviation of 1 value) are NaN. Count ignores minPeriods and always reports the finite count.")]
        public double[] RollingStatisticSeries(
            [Description("The series in time order. NaN is allowed and ignored.")]
            double[] values,
            [Description("Window length in observations, at least 1.")]
            int window,
            [Description("The statistic to compute over each window. Default Mean.")]
            WindowStatistic statistic = WindowStatistic.Mean,
            [Description("Trailing (window ends at the current point), Centered or Leading. Default Trailing.")]
            WindowAlignment alignment = WindowAlignment.Trailing,
            [Description("Minimum finite values required in a window; 0 (default) means the whole window.")]
            int minPeriods = 0,
            [Description(
                "Probability between 0 and 1 used when statistic is Quantile, for example 0.9. Ignored otherwise.")]
            double quantile = 0.5)
        {
            return RollingCore(values, window, statistic, alignment, minPeriods, quantile);
        }

        [AdvancedAnalyticsTool("ExpandingStatisticSeries")]
        [Description(
            "Expanding (running from the start) statistic of a series: element i is the chosen statistic over positions 0 to i. Use it for a running mean, a running maximum, a running volatility or any statistic of everything seen so far, for example how a customer's average amount evolves as history accumulates. " +
            "Available statistics are the same as RollingStatisticSeries. Missing (NaN) values are ignored; a position is NaN until minPeriods finite values have been seen (default 1). Series longer than 20000 values are rejected for the order-based statistics (Median, MedianAbsoluteDeviation, Quantile, Skewness, Kurtosis).")]
        public double[] ExpandingStatisticSeries(
            [Description("The series in time order. NaN is allowed and ignored.")]
            double[] values,
            [Description("The statistic to compute over everything so far. Default Mean.")]
            WindowStatistic statistic = WindowStatistic.Mean,
            [Description("Minimum finite values required before a value is reported. Default 1.")]
            int minPeriods = 1,
            [Description("Probability between 0 and 1 used when statistic is Quantile. Ignored otherwise.")]
            double quantile = 0.5)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            TimeSeriesGuard.AtLeast(minPeriods, 1, nameof(minPeriods));

            if (statistic == WindowStatistic.Quantile)
            {
                TimeSeriesGuard.Unit(quantile, nameof(quantile), true, true);
            }

            var ordered = statistic is WindowStatistic.Median or WindowStatistic.MedianAbsoluteDeviation
                or WindowStatistic.Quantile or WindowStatistic.Skewness or WindowStatistic.Kurtosis;

            if (ordered && v.Length > 20000)
            {
                throw new ArgumentException(
                    "Order-based expanding statistics are limited to 20000 values. Use RollingStatisticSeries with a window instead.",
                    nameof(values));
            }

            var result = new double[v.Length];
            var buffer = new List<double>();
            var count = 0;
            var mean = 0.0;
            var m2 = 0.0;
            var sum = 0.0;
            var minimum = double.PositiveInfinity;
            var maximum = double.NegativeInfinity;

            for (var i = 0; i < v.Length; i++)
            {
                if (double.IsFinite(v[i]))
                {
                    count++;
                    var d = v[i] - mean;
                    mean += d / count;
                    m2 += d * (v[i] - mean);
                    sum += v[i];
                    minimum = Math.Min(minimum, v[i]);
                    maximum = Math.Max(maximum, v[i]);

                    if (ordered || statistic == WindowStatistic.CoefficientOfVariation)
                    {
                        buffer.Add(v[i]);
                    }
                }

                if (statistic == WindowStatistic.Count)
                {
                    result[i] = count;
                }
                else if (count < minPeriods)
                {
                    result[i] = double.NaN;
                }
                else
                {
                    switch (statistic)
                    {
                        case WindowStatistic.Mean:
                            result[i] = mean;
                            break;
                        case WindowStatistic.Sum:
                            result[i] = sum;
                            break;
                        case WindowStatistic.Minimum:
                            result[i] = minimum;
                            break;
                        case WindowStatistic.Maximum:
                            result[i] = maximum;
                            break;
                        case WindowStatistic.Range:
                            result[i] = maximum - minimum;
                            break;
                        case WindowStatistic.Variance:
                            result[i] = count < 2 ? double.NaN : m2 / (count - 1);
                            break;
                        case WindowStatistic.StandardDeviation:
                            result[i] = count < 2 ? double.NaN : Math.Sqrt(m2 / (count - 1));
                            break;
                        case WindowStatistic.CoefficientOfVariation:
                            result[i] = mean == 0 || count < 2
                                ? double.NaN
                                : Math.Sqrt(m2 / (count - 1)) / Math.Abs(mean);

                            break;
                        default:
                            result[i] = WindowValue(buffer, statistic, quantile);
                            break;
                    }
                }
            }

            return result;
        }

        [AdvancedAnalyticsTool("RollingCorrelationSeries")]
        [Description(
            "Rolling Pearson correlation between two aligned series over a trailing window, showing how their relationship strengthens, weakens or flips over time. Use it to watch whether two metrics that normally move together have decoupled. " +
            "Each output is the correlation of the last 'window' pairs ending at that position (1 = move together, -1 = opposite, 0 = unrelated). Pairs with a missing value are dropped; a position is NaN when fewer than 3 pairs remain, before the window is full, or when either side is constant in the window. Both series must be the same length.")]
        public double[] RollingCorrelationSeries(
            [Description("The first series in time order. NaN is allowed and drops that pair.")]
            double[] x,
            [Description("The second series, the same length and time grid as x.")]
            double[] y,
            [Description("Window length in observations, at least 3.")]
            int window)
        {
            var a = TimeSeriesGuard.AllowMissing(x, nameof(x), 3);
            var b = TimeSeriesGuard.AllowMissing(y, nameof(y), 3);
            TimeSeriesGuard.SameLength(a, b, nameof(x), nameof(y));
            TimeSeriesGuard.AtLeast(window, 3, nameof(window));
            var result = new double[a.Length];

            for (var i = 0; i < a.Length; i++)
            {
                result[i] = double.NaN;

                if (i < window - 1)
                {
                    continue;
                }

                var px = new List<double>();
                var py = new List<double>();

                for (var j = i - window + 1; j <= i; j++)
                {
                    if (double.IsFinite(a[j]) && double.IsFinite(b[j]))
                    {
                        px.Add(a[j]);
                        py.Add(b[j]);
                    }
                }

                if (px.Count < 3)
                {
                    continue;
                }

                var mx = TimeSeriesGuard.Mean(px);
                var my = TimeSeriesGuard.Mean(py);
                var sxy = 0.0;
                var sxx = 0.0;
                var syy = 0.0;

                for (var k = 0; k < px.Count; k++)
                {
                    sxy += (px[k] - mx) * (py[k] - my);
                    sxx += (px[k] - mx) * (px[k] - mx);
                    syy += (py[k] - my) * (py[k] - my);
                }

                result[i] = sxx <= 0 || syy <= 0 ? double.NaN : Math.Max(-1, Math.Min(1, sxy / Math.Sqrt(sxx * syy)));
            }

            return result;
        }

        [AdvancedAnalyticsTool("RollingZScoreSeries")]
        [Description(
            "Rolling z-score: how many standard deviations each value is from the mean of the preceding window. Use it to flag values that are unusual relative to their own recent history, for example a payment far above the customer's last 30. " +
            "By default the window contains the earlier points only (excluding the current one) so a spike does not dilute its own baseline; set includeCurrent true to include it. Output is the same length as the input and NaN until the window has minPeriods values, when the window's standard deviation is 0, or when the value itself is missing. Missing values in the window are ignored.")]
        public double[] RollingZScoreSeries(
            [Description("The series in time order. NaN is allowed and ignored.")]
            double[] values,
            [Description("Number of earlier observations in the baseline window, at least 2.")]
            int window,
            [Description("True includes the current value in its own baseline; default false.")]
            bool includeCurrent = false,
            [Description("Minimum finite values needed in the baseline; 0 (default) means the whole window.")]
            int minPeriods = 0)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            TimeSeriesGuard.AtLeast(window, 2, nameof(window));
            TimeSeriesGuard.AtLeast(minPeriods, 0, nameof(minPeriods));
            var need = Math.Max(2, minPeriods == 0 ? window : minPeriods);
            var result = new double[v.Length];
            var buffer = new List<double>();

            for (var i = 0; i < v.Length; i++)
            {
                result[i] = double.NaN;
                var end = includeCurrent ? i : i - 1;
                var start = end - window + 1;
                buffer.Clear();

                for (var j = Math.Max(0, start); j <= end; j++)
                {
                    if (double.IsFinite(v[j]))
                    {
                        buffer.Add(v[j]);
                    }
                }

                if (!double.IsFinite(v[i]) || buffer.Count < need)
                {
                    continue;
                }

                var sd = TimeSeriesGuard.SampleStandardDeviation(buffer);

                if (sd > 0)
                {
                    result[i] = (v[i] - TimeSeriesGuard.Mean(buffer)) / sd;
                }
            }

            return result;
        }

        [AdvancedAnalyticsTool("DetrendSeries")]
        [Description(
            "Removes a trend from a series so what is left is the fluctuation around it. Method Mean subtracts the average, Linear subtracts a fitted straight line and Polynomial subtracts a fitted polynomial of the given degree (2 to 6), fitted by least squares against the time index 0, 1, 2 and so on. Use it before autocorrelation, spectral or seasonality work when a trend would dominate. " +
            "Detrended is the series minus Trend; Intercept and SlopePerStep describe the straight-line fit (SlopePerStep is 0 for Mean and both are NaN for Polynomial); RSquared is the share of variance the trend explains. Requires finite values and more points than the fit has parameters.")]
        public DetrendResult DetrendSeries(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Mean, Linear or Polynomial. Default Linear.")]
            DetrendMethod method = DetrendMethod.Linear,
            [Description("Polynomial degree from 2 to 6, used only when method is Polynomial. Default 2.")]
            int degree = 2)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);
            var n = v.Length;
            var trend = new double[n];
            var intercept = double.NaN;
            var slope = double.NaN;

            if (method == DetrendMethod.Mean)
            {
                var mean = TimeSeriesGuard.Mean(v);
                Array.Fill(trend, mean);
                intercept = mean;
                slope = 0;
            }
            else if (method == DetrendMethod.Linear)
            {
                TimeSeriesGuard.LinearFit(v, out intercept, out slope);

                for (var i = 0; i < n; i++)
                {
                    trend[i] = intercept + slope * i;
                }
            }
            else
            {
                if (degree < 2 || degree > 6)
                {
                    throw new ArgumentException($"degree must be between 2 and 6 but was {degree}.", nameof(degree));
                }

                if (n <= degree + 1)
                {
                    throw new ArgumentException(
                        $"values has {n} points which is too few for a degree {degree} polynomial (need more than {degree + 1}).",
                        nameof(values));
                }

                var design = new double[n, degree + 1];

                for (var i = 0; i < n; i++)
                {
                    var t = n == 1 ? 0 : 2.0 * i / (n - 1) - 1;

                    for (var k = 0; k <= degree; k++)
                    {
                        design[i, k] = Math.Pow(t, k);
                    }
                }

                var coefficients = TimeSeriesGuard.LeastSquares(design, v);

                for (var i = 0; i < n; i++)
                {
                    var fitted = 0.0;

                    for (var k = 0; k <= degree; k++)
                    {
                        fitted += design[i, k] * coefficients[k];
                    }

                    trend[i] = fitted;
                }
            }

            var detrended = v.Select((x, i) => x - trend[i]).ToArray();
            var total = TimeSeriesGuard.SampleVariance(v) * (n - 1);
            var residual = detrended.Sum(d => d * d);
            var r2 = total <= 0 ? double.NaN : Math.Max(0, 1 - residual / total);

            return new DetrendResult(detrended, trend, intercept, slope, r2, method.ToString());
        }

        [AdvancedAnalyticsTool("FillMissingValuesInSeries")]
        [Description(
            "Fills gaps (NaN values) in an ordered series using time-aware rules: LinearInterpolation draws a straight line between the neighbouring known values, ForwardFill carries the last known value forward, BackwardFill takes the next known value, NearestValue copies the closest known point, SeriesMean and SeriesMedian use a constant summary and Constant uses the value you supply. " +
            "Use it to make a series finite before autocorrelation, spectral or forecasting tools, which reject missing values. Leading or trailing gaps that a rule cannot reach (for example forward fill at the start) take the nearest known value for interpolation and stay NaN for fills, which RemainingMissing counts. FilledIndices lists the positions that were filled. Infinite values are treated as missing.")]
        public FillMissingResult FillMissingValuesInSeries(
            [Description("The series in time order; NaN or infinite marks a gap.")]
            double[] values,
            [Description("How to fill. Default LinearInterpolation.")]
            MissingFillMethod method = MissingFillMethod.LinearInterpolation,
            [Description("The value used when method is Constant. Ignored otherwise. Default 0.")]
            double constant = 0)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            var known = Enumerable.Range(0, v.Length).Where(i => double.IsFinite(v[i])).ToArray();
            var result = v.Select(x => double.IsFinite(x) ? x : double.NaN).ToArray();
            var filled = new List<int>();

            if (known.Length == 0 && method != MissingFillMethod.Constant)
            {
                return new FillMissingResult(result, Array.Empty<int>(), v.Length, method.ToString());
            }

            var knownValues = known.Select(i => v[i]).ToArray();
            var mean = known.Length == 0 ? double.NaN : TimeSeriesGuard.Mean(knownValues);
            var median = known.Length == 0 ? double.NaN : TimeSeriesGuard.Median(knownValues);

            for (var i = 0; i < v.Length; i++)
            {
                if (double.IsFinite(v[i]))
                {
                    continue;
                }

                var previous = known.Where(k => k < i).Select(k => (int?)k).LastOrDefault();
                var next = known.Where(k => k > i).Select(k => (int?)k).FirstOrDefault();
                double value;

                switch (method)
                {
                    case MissingFillMethod.ForwardFill:
                        value = previous.HasValue ? v[previous.Value] : double.NaN;
                        break;
                    case MissingFillMethod.BackwardFill:
                        value = next.HasValue ? v[next.Value] : double.NaN;
                        break;
                    case MissingFillMethod.NearestValue:
                        value = !previous.HasValue ? v[next!.Value]
                            : !next.HasValue ? v[previous.Value]
                            : i - previous.Value <= next.Value - i ? v[previous.Value] : v[next.Value];

                        break;
                    case MissingFillMethod.SeriesMean:
                        value = mean;
                        break;
                    case MissingFillMethod.SeriesMedian:
                        value = median;
                        break;
                    case MissingFillMethod.Constant:
                        value = constant;
                        break;
                    default:
                        value = !previous.HasValue ? v[next!.Value]
                            : !next.HasValue ? v[previous.Value]
                            : v[previous.Value] + (v[next.Value] - v[previous.Value]) * (i - previous.Value) /
                            (next.Value - previous.Value);

                        break;
                }

                result[i] = value;

                if (!double.IsNaN(value))
                {
                    filled.Add(i);
                }
            }

            return new FillMissingResult(result, filled.ToArray(), result.Count(double.IsNaN), method.ToString());
        }

        [AdvancedAnalyticsTool("MedianFilterSeries")]
        [Description(
            "Median filter: replaces each value with the median of a small window centred on it, removing isolated spikes and dropouts while keeping genuine level shifts sharp (unlike a moving average, which smears them). Use it to clean a noisy series before looking at its shape. " +
            "The window must be odd; at the edges the first and last values are repeated to fill the window. The result is the same length as the input. Requires finite values.")]
        public double[] MedianFilterSeries(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Odd window length of 3 or more, for example 3 or 5.")]
            int window = 3)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 1);

            if (window < 3 || window % 2 == 0)
            {
                throw new ArgumentException($"window must be an odd number of at least 3 but was {window}.",
                    nameof(window));
            }

            var half = window / 2;
            var result = new double[v.Length];
            var buffer = new double[window];

            for (var i = 0; i < v.Length; i++)
            {
                for (var k = -half; k <= half; k++)
                {
                    buffer[k + half] = v[Math.Min(v.Length - 1, Math.Max(0, i + k))];
                }

                result[i] = TimeSeriesGuard.Median(buffer);
            }

            return result;
        }

        [AdvancedAnalyticsTool("HampelOutlierFilter")]
        [Description(
            "Hampel filter: flags and replaces spikes by comparing each value with the median of its neighbourhood, measured in robust standard deviations (1.4826 x median absolute deviation). Use it to clean a series of isolated bad points or to list unusual values without being fooled by the outliers themselves, as a mean and standard deviation would be. " +
            "The window spans halfWidth points on each side (edges use the available points). A value is an outlier when its distance from the local median exceeds nSigmas robust deviations; when the local deviation is 0 any difference from the median counts. Cleaned is the series with outliers replaced by the local median; OutlierIndices and OutlierScores (distance in robust deviations, infinite if the local deviation is 0) describe them; LocalMedians is the baseline. Requires finite values.")]
        public HampelFilterResult HampelOutlierFilter(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Points on each side of a value included in its neighbourhood, at least 1. Default 3.")]
            int halfWidth = 3,
            [Description("Threshold in robust standard deviations. Default 3.")]
            double nSigmas = 3)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.AtLeast(halfWidth, 1, nameof(halfWidth));
            TimeSeriesGuard.Positive(nSigmas, nameof(nSigmas));
            var n = v.Length;
            var cleaned = (double[])v.Clone();
            var medians = new double[n];
            var indices = new List<int>();
            var scores = new List<double>();

            for (var i = 0; i < n; i++)
            {
                var start = Math.Max(0, i - halfWidth);
                var end = Math.Min(n - 1, i + halfWidth);
                var window = new double[end - start + 1];
                Array.Copy(v, start, window, 0, window.Length);
                var median = TimeSeriesGuard.Median(window);
                var scale = 1.4826 * TimeSeriesGuard.MedianAbsoluteDeviation(window);
                var distance = Math.Abs(v[i] - median);
                medians[i] = median;

                if (distance > nSigmas * scale)
                {
                    indices.Add(i);
                    scores.Add(scale == 0 ? double.PositiveInfinity : distance / scale);
                    cleaned[i] = median;
                }
            }

            return new HampelFilterResult(cleaned, indices.ToArray(), scores.ToArray(), medians);
        }

        [AdvancedAnalyticsTool("SavitzkyGolayFilter")]
        [Description(
            "Savitzky-Golay filter: smooths a series (or estimates its slope or curvature) by fitting a low-order polynomial to a sliding window by least squares. Unlike a moving average it preserves peaks and sharp features, so it suits noisy measurements that still have real shape. " +
            "windowLength must be odd and greater than polynomialOrder; derivative 0 returns the smoothed series, 1 the first derivative (change per unit of samplingInterval) and 2 the second derivative. Near the two ends the polynomial fitted to the first or last full window is evaluated at the edge positions. A polynomial of degree up to polynomialOrder is reproduced exactly. Requires finite values and at least windowLength points.")]
        public double[] SavitzkyGolayFilter(
            [Description("The series in time order, evenly spaced, finite.")]
            double[] values,
            [Description("Odd number of points in each fitting window, at least 3. Default 5.")]
            int windowLength = 5,
            [Description("Degree of the fitted polynomial, at least 1 and below windowLength. Default 2.")]
            int polynomialOrder = 2,
            [Description(
                "0 for smoothing, 1 for the first derivative, 2 for the second; must not exceed polynomialOrder. Default 0.")]
            int derivative = 0,
            [Description("Time between samples, used to scale derivatives. Default 1.")]
            double samplingInterval = 1)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.Positive(samplingInterval, nameof(samplingInterval));

            if (windowLength < 3 || windowLength % 2 == 0)
            {
                throw new ArgumentException($"windowLength must be an odd number of at least 3 but was {windowLength}.",
                    nameof(windowLength));
            }

            if (polynomialOrder < 1 || polynomialOrder >= windowLength)
            {
                throw new ArgumentException(
                    $"polynomialOrder must be at least 1 and less than windowLength ({windowLength}) but was {polynomialOrder}.",
                    nameof(polynomialOrder));
            }

            if (derivative < 0 || derivative > polynomialOrder)
            {
                throw new ArgumentException(
                    $"derivative must be between 0 and polynomialOrder ({polynomialOrder}) but was {derivative}.",
                    nameof(derivative));
            }

            if (v.Length < windowLength)
            {
                throw new ArgumentException(
                    $"values has {v.Length} points but windowLength is {windowLength}; supply more data or a shorter window.",
                    nameof(windowLength));
            }

            var half = windowLength / 2;
            var design = new double[windowLength, polynomialOrder + 1];

            for (var r = 0; r < windowLength; r++)
            {
                for (var k = 0; k <= polynomialOrder; k++)
                {
                    design[r, k] = Math.Pow(r - half, k);
                }
            }

            var pseudo = design.PseudoInverse();
            var n = v.Length;
            var result = new double[n];

            for (var i = 0; i < n; i++)
            {
                var start = Math.Min(Math.Max(i - half, 0), n - windowLength);
                var offset = i - (start + half);
                var value = 0.0;

                for (var k = derivative; k <= polynomialOrder; k++)
                {
                    var coefficient = 0.0;

                    for (var r = 0; r < windowLength; r++)
                    {
                        coefficient += pseudo[k, r] * v[start + r];
                    }

                    var falling = 1.0;

                    for (var q = 0; q < derivative; q++)
                    {
                        falling *= k - q;
                    }

                    value += coefficient * falling * Math.Pow(offset, k - derivative);
                }

                result[i] = value / Math.Pow(samplingInterval, derivative);
            }

            return result;
        }

        [AdvancedAnalyticsTool("BoxCoxTransformSeries")]
        [Description(
            "Box-Cox power transform of a strictly positive series, used to stabilise variance and make skewed data (amounts, counts, durations) closer to symmetric before modelling. lambda 1 is a shift, 0.5 a square root, 0 the natural log and -1 a reciprocal. " +
            "Leave lambda as NaN to choose it automatically by maximising the Box-Cox profile log-likelihood between -3 and 3; the chosen value is reported in Lambda with LambdaEstimated true and the log-likelihood at that value. The transform is (x^lambda - 1) / lambda, or ln x when lambda is 0. All values must be above 0 and finite; add an offset first if some are 0.")]
        public BoxCoxResult BoxCoxTransformSeries(
            [Description("Strictly positive series values, finite.")]
            double[] values,
            [Description("The power to use, or NaN (default) to estimate it by maximum likelihood.")]
            double lambda = double.NaN)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);

            if (v.Any(x => x <= 0))
            {
                throw new ArgumentException(
                    "Every value must be above 0 for a Box-Cox transform. Add a constant offset so the minimum is above 0, or use a different transform.",
                    nameof(values));
            }

            var estimated = double.IsNaN(lambda);

            if (estimated)
            {
                var logSum = v.Sum(Math.Log);
                lambda = BrentSearch.Maximize(l => BoxCoxLogLikelihood(v, l, logSum), -3, 3, 1e-8, 200);
            }

            return new BoxCoxResult(v.Select(x => BoxCox(x, lambda)).ToArray(), lambda, estimated,
                BoxCoxLogLikelihood(v, lambda, v.Sum(Math.Log)));
        }

        [AdvancedAnalyticsTool("KalmanLocalLevelFilter")]
        [Description(
            "Kalman filter for a local level model: the series is a slowly wandering hidden level plus measurement noise. Use it to track the true underlying level of a noisy metric and to get one-step-ahead predictions with standardised surprises. The filter starts at the first observation and updates recursively with the given process variance (how far the level can drift per step) and measurement variance (observation noise). " +
            "FilteredLevels and FilteredVariances are the estimate and its uncertainty after each observation; OneStepPredictions[i] is the forecast made before seeing position i (NaN at 0); Innovations are the forecast errors and StandardisedInnovations divide them by their expected standard deviation, so values beyond about 3 in size are unusual; LogLikelihood sums the Gaussian log density of the innovations and SteadyStateGain is the limiting weight given to a new observation. " +
            "With processVariance 0 the level is the running mean. Requires finite values and positive measurementVariance.")]
        public KalmanLocalLevelResult KalmanLocalLevelFilter(
            [Description("The series in time order, evenly spaced, finite.")]
            double[] values,
            [Description("Variance of the level's random drift per step, 0 or more. Larger tracks changes faster.")]
            double processVariance,
            [Description("Variance of the observation noise around the level, above 0.")]
            double measurementVariance)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);
            TimeSeriesGuard.Positive(measurementVariance, nameof(measurementVariance));

            if (double.IsNaN(processVariance) || double.IsInfinity(processVariance) || processVariance < 0)
            {
                throw new ArgumentException($"processVariance must be 0 or more but was {processVariance}.",
                    nameof(processVariance));
            }

            var n = v.Length;
            var levels = new double[n];
            var variances = new double[n];
            var predictions = new double[n];
            var innovations = new double[n];
            var standardised = new double[n];
            var logLikelihood = 0.0;
            var level = v[0];
            var p = measurementVariance;
            levels[0] = level;
            variances[0] = p;
            predictions[0] = double.NaN;
            innovations[0] = double.NaN;
            standardised[0] = double.NaN;

            for (var t = 1; t < n; t++)
            {
                var predictedVariance = p + processVariance;
                var f = predictedVariance + measurementVariance;
                var gain = predictedVariance / f;
                var innovation = v[t] - level;
                predictions[t] = level;
                innovations[t] = innovation;
                standardised[t] = innovation / Math.Sqrt(f);
                logLikelihood += -0.5 * (Math.Log(2 * Math.PI * f) + innovation * innovation / f);
                level += gain * innovation;
                p = (1 - gain) * predictedVariance;
                levels[t] = level;
                variances[t] = p;
            }

            var q = processVariance / measurementVariance;
            var steady = (q + Math.Sqrt(q * q + 4 * q)) / 2;

            return new KalmanLocalLevelResult(levels, variances, predictions, innovations, standardised, logLikelihood,
                steady / (steady + 1));
        }

        [AdvancedAnalyticsTool("SeriesProfile")]
        [Description(
            "Quick diagnostic overview of a single series to decide what to do with it next: length, missing count, range, mean, spread, whether it is constant or monotonic, its linear trend slope, lag-1 autocorrelation, a Mann-Kendall trend p-value, counts of zeros and negatives, the longest run of identical values and a suggested seasonal period (0 if none is found). " +
            "Notes lists plain-language observations such as 'contains missing values' or 'strong trend'. Missing (NaN) values are counted and excluded from the other figures. Use it first on an unfamiliar series; then call the specific tool it points to.")]
        public SeriesProfileResult SeriesProfile(
            [Description("The series in time order. NaN is allowed and counted as missing.")]
            double[] values)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            var finite = v.Where(double.IsFinite).ToArray();
            var notes = new List<string>();
            var missing = v.Length - finite.Length;

            if (missing > 0)
            {
                notes.Add(
                    $"{missing} missing or infinite values were excluded; fill them before autocorrelation or spectral tools.");
            }

            if (finite.Length == 0)
            {
                return new SeriesProfileResult(v.Length, missing, double.NaN, double.NaN, double.NaN, double.NaN,
                    double.NaN,
                    double.NaN, false, false, false, double.NaN, double.NaN, double.NaN, 0, 0, 0, 0,
                    notes.Concat(new[] { "No finite values." }).ToArray());
            }

            var constant = TimeSeriesGuard.IsConstant(finite);
            var increasing = true;
            var decreasing = true;
            var longest = 1;
            var run = 1;

            for (var i = 1; i < finite.Length; i++)
            {
                increasing &= finite[i] >= finite[i - 1];
                decreasing &= finite[i] <= finite[i - 1];
                run = finite[i] == finite[i - 1] ? run + 1 : 1;
                longest = Math.Max(longest, run);
            }

            var slope = double.NaN;
            var lag1 = double.NaN;
            var mkP = double.NaN;
            var period = 0;

            if (finite.Length >= 3)
            {
                TimeSeriesGuard.LinearFit(finite, out _, out slope);

                if (!constant)
                {
                    lag1 = TimeSeriesGuard.Autocorrelations(finite, 1)[1];
                }

                if (finite.Length <= 4000)
                {
                    mkP = new SeriesDependenceTools().MannKendallTrendTest(finite).PValue;
                }
            }

            if (finite.Length >= 8 && !constant)
            {
                period = new SeriesDependenceTools().EstimateSeasonalPeriodByAutocorrelation(finite).Period;
            }

            if (constant)
            {
                notes.Add("The series is constant.");
            }

            if (!double.IsNaN(mkP) && mkP < 0.01)
            {
                notes.Add("A strong monotonic trend is present; consider DetrendSeries or DifferenceSeries.");
            }

            if (!double.IsNaN(lag1) && Math.Abs(lag1) > 0.5)
            {
                notes.Add("Strong lag-1 autocorrelation: successive values are not independent.");
            }

            if (period > 0)
            {
                notes.Add($"A repeating cycle of about {period} observations was detected.");
            }

            return new SeriesProfileResult(
                v.Length, missing, finite.Min(), finite.Max(), TimeSeriesGuard.Mean(finite),
                finite.Length > 1 ? TimeSeriesGuard.SampleStandardDeviation(finite) : double.NaN,
                finite[0], finite[^1], constant, increasing, decreasing, slope, lag1, mkP,
                finite.Count(x => x == 0), finite.Count(x => x < 0), longest, period, notes.ToArray());
        }

        private static (int Start, int End) WindowBounds(int index, int window, WindowAlignment alignment, int length)
        {
            int start;

            switch (alignment)
            {
                case WindowAlignment.Trailing:
                    start = index - window + 1;
                    break;
                case WindowAlignment.Centered:
                    start = index - (window - 1) / 2;
                    break;
                default:
                    start = index;
                    break;
            }

            return (Math.Max(0, start), Math.Min(length - 1, start + window - 1));
        }

        internal static double ResolveAlpha(double alpha, double span)
        {
            if (double.IsNaN(alpha) == double.IsNaN(span))
            {
                throw new ArgumentException("Give exactly one of alpha or span and leave the other NaN.",
                    double.IsNaN(alpha) ? nameof(alpha) : nameof(span));
            }

            if (!double.IsNaN(alpha))
            {
                if (!(alpha > 0 && alpha <= 1))
                {
                    throw new ArgumentException($"alpha must be above 0 and at most 1 but was {alpha}.", nameof(alpha));
                }

                return alpha;
            }

            if (!(span >= 1))
            {
                throw new ArgumentException($"span must be at least 1 but was {span}.", nameof(span));
            }

            return 2.0 / (span + 1);
        }

        private static double[] RollingCore(double[] values, int window, WindowStatistic statistic,
            WindowAlignment alignment, int minPeriods, double quantile)
        {
            var v = TimeSeriesGuard.AllowMissing(values, nameof(values), 1);
            TimeSeriesGuard.AtLeast(window, 1, nameof(window));
            TimeSeriesGuard.AtLeast(minPeriods, 0, nameof(minPeriods));

            if (statistic == WindowStatistic.Quantile)
            {
                TimeSeriesGuard.Unit(quantile, nameof(quantile), true, true);
            }

            if ((double)v.Length * Math.Min(window, v.Length) > 1e8)
            {
                throw new ArgumentException(
                    "values length times window is above 100 million operations. Use a smaller window or a shorter series.",
                    nameof(window));
            }

            var need = minPeriods == 0 ? window : minPeriods;
            var result = new double[v.Length];
            var buffer = new List<double>(Math.Min(window, v.Length));

            for (var i = 0; i < v.Length; i++)
            {
                var (start, end) = WindowBounds(i, window, alignment, v.Length);
                buffer.Clear();

                for (var j = start; j <= end; j++)
                {
                    if (double.IsFinite(v[j]))
                    {
                        buffer.Add(v[j]);
                    }
                }

                if (statistic == WindowStatistic.Count)
                {
                    result[i] = buffer.Count;
                    continue;
                }

                result[i] = buffer.Count < need || buffer.Count == 0
                    ? double.NaN
                    : WindowValue(buffer, statistic, quantile);
            }

            return result;
        }

        internal static double WindowValue(List<double> window, WindowStatistic statistic, double quantile)
        {
            switch (statistic)
            {
                case WindowStatistic.Mean:
                    return TimeSeriesGuard.Mean(window);
                case WindowStatistic.Sum:
                    return TimeSeriesGuard.Sum(window);
                case WindowStatistic.Minimum:
                    return window.Min();
                case WindowStatistic.Maximum:
                    return window.Max();
                case WindowStatistic.Median:
                    return TimeSeriesGuard.Median(window);
                case WindowStatistic.StandardDeviation:
                    return TimeSeriesGuard.SampleStandardDeviation(window);
                case WindowStatistic.Variance:
                    return TimeSeriesGuard.SampleVariance(window);
                case WindowStatistic.Range:
                    return window.Max() - window.Min();
                case WindowStatistic.MedianAbsoluteDeviation:
                    return TimeSeriesGuard.MedianAbsoluteDeviation(window);
                case WindowStatistic.Skewness:
                    return window.Count < 3 ? double.NaN : window.ToArray().Skewness();
                case WindowStatistic.Kurtosis:
                    return window.Count < 4 ? double.NaN : window.ToArray().Kurtosis();
                case WindowStatistic.Quantile:
                    return window.ToArray().Quantile(quantile);
                case WindowStatistic.CoefficientOfVariation:
                    var mean = TimeSeriesGuard.Mean(window);

                    return mean == 0 ? double.NaN : TimeSeriesGuard.SampleStandardDeviation(window) / Math.Abs(mean);
                default:
                    return double.NaN;
            }
        }

        private static double BoxCox(double x, double lambda)
        {
            return Math.Abs(lambda) < 1e-12 ? Math.Log(x) : (Math.Pow(x, lambda) - 1) / lambda;
        }

        private static double BoxCoxLogLikelihood(double[] v, double lambda, double logSum)
        {
            var y = v.Select(x => BoxCox(x, lambda)).ToArray();
            var mean = y.Average();
            var variance = y.Sum(t => (t - mean) * (t - mean)) / y.Length;

            return variance <= 0
                ? double.NegativeInfinity
                : -y.Length / 2.0 * Math.Log(variance) + (lambda - 1) * logSum;
        }
    }
}