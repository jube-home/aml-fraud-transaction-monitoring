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
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    public enum SpikeMethod
    {
        RollingZScore,
        RollingRobustZScore,
        GlobalRobustZScore,
        InterquartileFence
    }

    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SeriesAnomalyTools
    {
        private static void Baseline(double[] v, double target, double sigma, int baselineCount, out double mean,
            out double sd)
        {
            var count = baselineCount > 0 ? Math.Min(baselineCount, v.Length) : Math.Min(30, Math.Max(2, v.Length / 2));
            var head = v.Take(count).ToArray();
            mean = double.IsNaN(target) ? TimeSeriesGuard.Mean(head) : target;
            sd = double.IsNaN(sigma) ? TimeSeriesGuard.SampleStandardDeviation(head) : sigma;

            if (!(sd > 0) || double.IsInfinity(sd))
            {
                throw new ArgumentException(
                    "The baseline standard deviation is 0 or undefined (the baseline is constant or too short), so deviations cannot be scaled. Supply standardDeviation explicitly or use a longer baseline.",
                    "standardDeviation");
            }
        }

        [AdvancedAnalyticsTool("CusumChangeDetection")]
        [Description(
            "Two-sided CUSUM (cumulative sum) control chart for detecting a sustained shift in the average level of a series, upward or downward, much earlier than a single-point threshold would. Use it to catch a step change in daily volume, average amount or score. " +
            "Each new value adds its deviation from the target minus a slack allowance (kSigma standard deviations) to a running sum that never goes below 0; an alarm is raised when a sum exceeds hSigma standard deviations, and the sums then restart. UpperSums detects rises and LowerSums falls (both are reported per position). AlarmIndices with AlarmDirections (Up or Down) and EstimatedChangeStarts (where the alarming sum last left 0, the likely start of the shift) describe the alarms. " +
            "The target and standard deviation come from the first baselineCount values unless supplied; supply them from known normal behaviour when possible. Defaults kSigma 0.5 and hSigma 5 detect a one-sigma shift within roughly 10 points. Requires finite values.")]
        public CusumResult CusumChangeDetection(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("The in-control mean. NaN (default) estimates it from the first baselineCount values.")]
            double targetMean = double.NaN,
            [Description("The in-control standard deviation. NaN (default) estimates it from the baseline.")]
            double standardDeviation = double.NaN,
            [Description(
                "Number of leading values treated as the normal baseline when the target or deviation is not supplied; 0 (default) uses the first half of the series up to 30 values.")]
            int baselineCount = 0,
            [Description(
                "Slack allowance in standard deviations; shifts smaller than about twice this are ignored. Default 0.5.")]
            double kSigma = 0.5,
            [Description("Decision interval in standard deviations at which an alarm is raised. Default 5.")]
            double hSigma = 5)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.AtLeast(baselineCount, 0, nameof(baselineCount));

            if (kSigma < 0 || double.IsNaN(kSigma))
            {
                throw new ArgumentException($"kSigma must be 0 or more but was {kSigma}.", nameof(kSigma));
            }

            TimeSeriesGuard.Positive(hSigma, nameof(hSigma));
            Baseline(v, targetMean, standardDeviation, baselineCount, out var mean, out var sd);
            var n = v.Length;
            var upper = new double[n];
            var lower = new double[n];
            var alarms = new List<int>();
            var directions = new List<string>();
            var starts = new List<int>();
            var sHigh = 0.0;
            var sLow = 0.0;
            var startHigh = 0;
            var startLow = 0;

            for (var t = 0; t < n; t++)
            {
                var z = (v[t] - mean) / sd;
                var nextHigh = Math.Max(0, sHigh + z - kSigma);
                var nextLow = Math.Max(0, sLow - z - kSigma);

                if (sHigh == 0 && nextHigh > 0)
                {
                    startHigh = t;
                }

                if (sLow == 0 && nextLow > 0)
                {
                    startLow = t;
                }

                sHigh = nextHigh;
                sLow = nextLow;
                upper[t] = sHigh;
                lower[t] = sLow;

                if (sHigh > hSigma)
                {
                    alarms.Add(t);
                    directions.Add("Up");
                    starts.Add(startHigh);
                    sHigh = 0;
                    sLow = 0;
                }
                else if (sLow > hSigma)
                {
                    alarms.Add(t);
                    directions.Add("Down");
                    starts.Add(startLow);
                    sHigh = 0;
                    sLow = 0;
                }
            }

            return new CusumResult(upper, lower, alarms.ToArray(), directions.ToArray(), starts.ToArray(), mean, sd,
                kSigma,
                hSigma);
        }

        [AdvancedAnalyticsTool("PageHinkleyDriftDetection")]
        [Description(
            "Page-Hinkley test for drift in the mean of a stream, suited to online monitoring where no baseline is available: it compares each new value with the running average of everything seen since the last alarm. Use it to detect gradual or abrupt upward or downward drift in a metric such as average transaction size. " +
            "IncreaseStatistic and DecreaseStatistic are the accumulated evidence for a rise and a fall (each is the gap between the cumulative deviation and its minimum or maximum so far); an alarm fires when either exceeds lambda, is recorded in AlarmIndices with AlarmDirections and the test restarts. delta is the tolerated magnitude of change (values smaller than it are ignored). " +
            "The thresholds are in the units of the series, so scale lambda to the size of shift you care about. Requires finite values.")]
        public PageHinkleyResult PageHinkleyDriftDetection(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Magnitude of change that is tolerated without alarm, 0 or more, in series units. Default 0.")]
            double delta = 0,
            [Description(
                "Alarm threshold in series units, above 0. Larger values give fewer, later alarms. Default 5.")]
            double lambda = 5)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);
            TimeSeriesGuard.Positive(lambda, nameof(lambda));

            if (delta < 0 || double.IsNaN(delta))
            {
                throw new ArgumentException($"delta must be 0 or more but was {delta}.", nameof(delta));
            }

            var n = v.Length;
            var increase = new double[n];
            var decrease = new double[n];
            var alarms = new List<int>();
            var directions = new List<string>();
            var count = 0;
            var mean = 0.0;
            var sumUp = 0.0;
            var minUp = 0.0;
            var sumDown = 0.0;
            var maxDown = 0.0;

            for (var t = 0; t < n; t++)
            {
                count++;
                mean += (v[t] - mean) / count;
                sumUp += v[t] - mean - delta;
                minUp = Math.Min(minUp, sumUp);
                sumDown += v[t] - mean + delta;
                maxDown = Math.Max(maxDown, sumDown);
                increase[t] = sumUp - minUp;
                decrease[t] = maxDown - sumDown;

                var up = increase[t] > lambda;
                var down = decrease[t] > lambda;

                if (up || down)
                {
                    alarms.Add(t);
                    directions.Add(up && (!down || increase[t] >= decrease[t]) ? "Up" : "Down");
                    count = 0;
                    mean = 0;
                    sumUp = 0;
                    minUp = 0;
                    sumDown = 0;
                    maxDown = 0;
                }
            }

            return new PageHinkleyResult(increase, decrease, alarms.ToArray(), directions.ToArray(), delta, lambda);
        }

        [AdvancedAnalyticsTool("EwmaControlChart")]
        [Description(
            "Exponentially weighted moving average (EWMA) control chart: smooths the series with weight lambda on the newest value and raises an alarm when the smoothed value leaves control limits of target plus or minus L standard errors. It detects small persistent shifts in the mean that a Shewhart chart misses, while still reacting to large ones. " +
            "Ewma is the smoothed statistic (starting from the target); UpperLimits and LowerLimits are the exact time-varying limits (they widen to a steady width over the first few points); AlarmIndices lists the positions outside the limits. The target and standard deviation are estimated from the first baselineCount values unless supplied. " +
            "Typical settings are lambda 0.05 to 0.25 and limitWidth 3. Requires finite values.")]
        public EwmaChartResult EwmaControlChart(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Weight on the newest value, above 0 and at most 1. Default 0.2.")]
            double lambda = 0.2,
            [Description("Half-width of the limits in standard errors of the EWMA. Default 3.")]
            double limitWidth = 3,
            [Description("The in-control mean. NaN (default) estimates it from the baseline.")]
            double targetMean = double.NaN,
            [Description(
                "The in-control standard deviation of single values. NaN (default) estimates it from the baseline.")]
            double standardDeviation = double.NaN,
            [Description(
                "Number of leading values used as the baseline when target or deviation is not supplied; 0 (default) uses the first half up to 30 values.")]
            int baselineCount = 0)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);

            if (!(lambda > 0 && lambda <= 1))
            {
                throw new ArgumentException($"lambda must be above 0 and at most 1 but was {lambda}.", nameof(lambda));
            }

            TimeSeriesGuard.Positive(limitWidth, nameof(limitWidth));
            TimeSeriesGuard.AtLeast(baselineCount, 0, nameof(baselineCount));
            Baseline(v, targetMean, standardDeviation, baselineCount, out var mean, out var sd);
            var n = v.Length;
            var ewma = new double[n];
            var upperLimits = new double[n];
            var lowerLimits = new double[n];
            var alarms = new List<int>();
            var z = mean;

            for (var t = 0; t < n; t++)
            {
                z = lambda * v[t] + (1 - lambda) * z;

                var width = limitWidth * sd *
                            Math.Sqrt(lambda / (2 - lambda) * (1 - Math.Pow(1 - lambda, 2 * (t + 1))));

                ewma[t] = z;
                upperLimits[t] = mean + width;
                lowerLimits[t] = mean - width;

                if (z > upperLimits[t] || z < lowerLimits[t])
                {
                    alarms.Add(t);
                }
            }

            return new EwmaChartResult(ewma, upperLimits, lowerLimits, alarms.ToArray(), mean, sd, lambda, limitWidth);
        }

        [AdvancedAnalyticsTool("ShewhartControlChartAlarms")]
        [Description(
            "Shewhart control chart with the standard Western Electric run rules, flagging individual points and patterns that are unlikely if the process is in control. ZScores are the values standardised by target and standard deviation. Rule 1 (BeyondThreeSigma): a point more than 3 sigma from the target. Rule 2 (TwoOfThreeBeyondTwoSigma): 2 of 3 consecutive points beyond 2 sigma on the same side. Rule 3 (FourOfFiveBeyondOneSigma): 4 of 5 consecutive points beyond 1 sigma on the same side. Rule 4 (EightInARowOneSide): 8 consecutive points on one side of the target. " +
            "Each rule array lists the positions at which the rule completes; AlarmIndices is their union in order. The target and standard deviation come from the first baselineCount values unless supplied. Requires finite values.")]
        public ShewhartChartResult ShewhartControlChartAlarms(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("The in-control mean. NaN (default) estimates it from the baseline.")]
            double targetMean = double.NaN,
            [Description("The in-control standard deviation. NaN (default) estimates it from the baseline.")]
            double standardDeviation = double.NaN,
            [Description(
                "Number of leading values used as the baseline when target or deviation is not supplied; 0 (default) uses the first half up to 30 values.")]
            int baselineCount = 0)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.AtLeast(baselineCount, 0, nameof(baselineCount));
            Baseline(v, targetMean, standardDeviation, baselineCount, out var mean, out var sd);
            var z = v.Select(x => (x - mean) / sd).ToArray();
            var n = z.Length;
            var rule1 = new List<int>();
            var rule2 = new List<int>();
            var rule3 = new List<int>();
            var rule4 = new List<int>();

            for (var t = 0; t < n; t++)
            {
                if (Math.Abs(z[t]) > 3)
                {
                    rule1.Add(t);
                }

                if (t >= 2 && Completes(z, t, 3, 2, 2))
                {
                    rule2.Add(t);
                }

                if (t >= 4 && Completes(z, t, 5, 4, 1))
                {
                    rule3.Add(t);
                }

                if (t >= 7)
                {
                    var side = Math.Sign(z[t]);

                    if (side != 0 && Enumerable.Range(t - 7, 8).All(k => Math.Sign(z[k]) == side))
                    {
                        rule4.Add(t);
                    }
                }
            }

            var all = rule1.Concat(rule2).Concat(rule3).Concat(rule4).Distinct().OrderBy(x => x).ToArray();

            return new ShewhartChartResult(z, rule1.ToArray(), rule2.ToArray(), rule3.ToArray(), rule4.ToArray(), all,
                mean,
                sd);
        }

        private static bool Completes(double[] z, int t, int span, int required, double sigmas)
        {
            if (Math.Abs(z[t]) <= sigmas)
            {
                return false;
            }

            var side = Math.Sign(z[t]);
            var hits = 0;

            for (var k = t - span + 1; k <= t; k++)
            {
                if (Math.Sign(z[k]) == side && Math.Abs(z[k]) > sigmas)
                {
                    hits++;
                }
            }

            return hits >= required;
        }

        [AdvancedAnalyticsTool("SeriesSpikeDetection")]
        [Description(
            "Flags spikes and dips in a series with a choice of method. RollingZScore scores each value against the mean and standard deviation of the preceding window; RollingRobustZScore uses the preceding window's median and 1.4826 x median absolute deviation, which outliers cannot distort; GlobalRobustZScore uses the whole series' median and deviation; InterquartileFence scores how many interquartile ranges a value lies beyond the first or third quartile (threshold 1.5 is Tukey's fence, 3 is far out). " +
            "Scores has one entry per value (NaN where the method has no baseline, such as the first window; positive for high values and negative for low ones), SpikeIndices lists positions whose absolute score exceeds the threshold and SpikeValues their values. A baseline with zero spread scores any different value as infinite. " +
            "Use a threshold of about 3 to 3.5 for the z-score methods. Requires finite values; the window is used only by the rolling methods.")]
        public SpikeDetectionResult SeriesSpikeDetection(
            [Description("The series in time order, finite.")]
            double[] values,
            [Description("Scoring method. Default RollingRobustZScore.")]
            SpikeMethod method = SpikeMethod.RollingRobustZScore,
            [Description("Preceding-window length for the rolling methods, at least 5. Default 30.")]
            int window = 30,
            [Description("Absolute score above which a value is a spike, above 0. Default 3.5.")]
            double threshold = 3.5)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.Positive(threshold, nameof(threshold));
            TimeSeriesGuard.AtLeast(window, 5, nameof(window));
            var n = v.Length;
            var scores = new double[n];

            switch (method)
            {
                case SpikeMethod.RollingZScore:
                    for (var t = 0; t < n; t++)
                    {
                        var baseline = Preceding(v, t, window);

                        scores[t] = baseline.Length < 3
                            ? double.NaN
                            : Scale(v[t] - TimeSeriesGuard.Mean(baseline),
                                TimeSeriesGuard.SampleStandardDeviation(baseline));
                    }

                    break;
                case SpikeMethod.RollingRobustZScore:
                    for (var t = 0; t < n; t++)
                    {
                        var baseline = Preceding(v, t, window);

                        scores[t] = baseline.Length < 3
                            ? double.NaN
                            : Scale(v[t] - TimeSeriesGuard.Median(baseline),
                                1.4826 * TimeSeriesGuard.MedianAbsoluteDeviation(baseline));
                    }

                    break;
                case SpikeMethod.GlobalRobustZScore:
                    var median = TimeSeriesGuard.Median(v);
                    var scale = 1.4826 * TimeSeriesGuard.MedianAbsoluteDeviation(v);

                    for (var t = 0; t < n; t++)
                    {
                        scores[t] = Scale(v[t] - median, scale);
                    }

                    break;
                default:
                    var q1 = v.Quantile(0.25);
                    var q3 = v.Quantile(0.75);
                    var iqr = q3 - q1;

                    for (var t = 0; t < n; t++)
                    {
                        scores[t] = v[t] > q3 ? Scale(v[t] - q3, iqr) : v[t] < q1 ? -Scale(q1 - v[t], iqr) : 0;
                    }

                    break;
            }

            var spikes = Enumerable.Range(0, n).Where(t => !double.IsNaN(scores[t]) && Math.Abs(scores[t]) > threshold)
                .ToArray();

            return new SpikeDetectionResult(scores, spikes, spikes.Select(t => v[t]).ToArray(), method.ToString(),
                threshold);
        }

        private static double[] Preceding(double[] v, int t, int window)
        {
            var start = Math.Max(0, t - window);
            var slice = new double[t - start];
            Array.Copy(v, start, slice, 0, slice.Length);

            return slice;
        }

        private static double Scale(double deviation, double spread)
        {
            return spread > 0 ? deviation / spread :
                deviation == 0 ? 0 :
                deviation > 0 ? double.PositiveInfinity : double.NegativeInfinity;
        }

        [AdvancedAnalyticsTool("LevelShiftScan")]
        [Description(
            "Scans a series for level shifts (a step up or down that persists) by comparing the mean of the window before each point with the mean of the window starting at it, using a Welch t-type score. Use it to locate where a metric moved to a new level, in contrast to a one-off spike. " +
            "Scores[t] is (mean after - mean before) divided by its standard error, defined for positions with a full window on both sides (NaN elsewhere; infinite if both windows are constant but different). ShiftIndices lists the strongest positions whose absolute score exceeds the threshold (no two closer than one window), with ShiftSizes (the change in mean) and ShiftScores. " +
            "ShiftIndices[i] is the first observation at the new level. Requires finite values and at least twice the window.")]
        public LevelShiftResult LevelShiftScan(
            [Description("The series in time order, finite, at least twice the window long.")]
            double[] values,
            [Description("Number of observations in each comparison window, at least 3. Default 10.")]
            int window = 10,
            [Description("Absolute score above which a shift is reported. Default 5.")]
            double threshold = 5)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 6);
            TimeSeriesGuard.AtLeast(window, 3, nameof(window));
            TimeSeriesGuard.Positive(threshold, nameof(threshold));

            if (v.Length < 2 * window)
            {
                throw new ArgumentException(
                    $"values has {v.Length} points but two windows of {window} need {2 * window}. Use a smaller window.",
                    nameof(window));
            }

            var n = v.Length;
            var scores = new double[n];
            Array.Fill(scores, double.NaN);
            var sizes = new double[n];

            for (var t = window; t <= n - window; t++)
            {
                var before = new double[window];
                var after = new double[window];
                Array.Copy(v, t - window, before, 0, window);
                Array.Copy(v, t, after, 0, window);
                var diff = TimeSeriesGuard.Mean(after) - TimeSeriesGuard.Mean(before);

                var se = Math.Sqrt(TimeSeriesGuard.SampleVariance(before) / window +
                                   TimeSeriesGuard.SampleVariance(after) / window);

                sizes[t] = diff;
                scores[t] = Scale(diff, se);
            }

            var candidates = Enumerable.Range(0, n)
                .Where(t => !double.IsNaN(scores[t]) && Math.Abs(scores[t]) > threshold)
                .OrderByDescending(t => Math.Abs(scores[t])).ThenBy(t => t).ToList();

            var chosen = new List<int>();

            foreach (var t in candidates)
            {
                if (chosen.All(c => Math.Abs(c - t) >= window))
                {
                    chosen.Add(t);
                }
            }

            chosen.Sort();

            return new LevelShiftResult(scores, chosen.ToArray(), chosen.Select(t => sizes[t]).ToArray(),
                chosen.Select(t => scores[t]).ToArray(), window);
        }

        [AdvancedAnalyticsTool("MeanShiftChangePoints")]
        [Description(
            "Finds change points where the average level of a series shifts, splitting it into segments of roughly constant mean, using binary segmentation on the sum of squared errors. Use it to segment a history into regimes (for example before and after a policy change or account takeover) without choosing a window. " +
            "The noise level is estimated robustly as the median absolute first difference divided by 0.6745 x sqrt(2); a split is kept only if it reduces the squared error by more than penaltyMultiplier x 2 x noise variance x ln(n), a BIC-style penalty, and both parts have at least minSegmentLength points. Splits are made greedily, largest gain first, up to maxChangePoints. " +
            "ChangePointIndices are the first observation of each new segment in time order with their Gains; Segments give each piece's start, end, size and mean. An empty change point list means no significant shift was found. Requires finite values.")]
        public SeriesMeanShiftResult MeanShiftChangePoints(
            [Description("The series in time order, finite, at least 2 x minSegmentLength long.")]
            double[] values,
            [Description("Most change points to return, at least 1. Default 5.")]
            int maxChangePoints = 5,
            [Description("Fewest observations allowed in a segment, at least 2. Default 5.")]
            int minSegmentLength = 5,
            [Description("Scales the penalty; above 1 is more conservative, below 1 more sensitive. Default 1.")]
            double penaltyMultiplier = 1)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.AtLeast(maxChangePoints, 1, nameof(maxChangePoints));
            TimeSeriesGuard.AtLeast(minSegmentLength, 2, nameof(minSegmentLength));
            TimeSeriesGuard.Positive(penaltyMultiplier, nameof(penaltyMultiplier));
            var n = v.Length;

            if (n < 2 * minSegmentLength)
            {
                throw new ArgumentException(
                    $"values has {n} points but two segments of {minSegmentLength} need {2 * minSegmentLength}. Use a smaller minSegmentLength.",
                    nameof(minSegmentLength));
            }

            var diffs = new double[n - 1];

            for (var i = 0; i < diffs.Length; i++)
            {
                diffs[i] = v[i + 1] - v[i];
            }

            var sigma = TimeSeriesGuard.Median(diffs.Select(Math.Abs).ToArray()) / (0.6745 * Math.Sqrt(2));
            sigma = Math.Max(sigma, Math.Max(1e-6 * TimeSeriesGuard.SampleStandardDeviation(v), 1e-12));
            var penalty = penaltyMultiplier * 2 * sigma * sigma * Math.Log(n);
            var prefix = new double[n + 1];
            var prefixSquares = new double[n + 1];

            for (var i = 0; i < n; i++)
            {
                prefix[i + 1] = prefix[i] + v[i];
                prefixSquares[i + 1] = prefixSquares[i] + v[i] * v[i];
            }

            (int Split, double Gain) Best(int start, int end)
            {
                var bestSplit = -1;
                var bestGain = 0.0;
                var total = end - start + 1;

                for (var s = start + minSegmentLength; s <= end - minSegmentLength + 1; s++)
                {
                    var n1 = s - start;
                    var n2 = end - s + 1;
                    var m1 = (prefix[s] - prefix[start]) / n1;
                    var m2 = (prefix[end + 1] - prefix[s]) / n2;
                    var gain = (double)n1 * n2 / total * (m1 - m2) * (m1 - m2);

                    if (gain > bestGain)
                    {
                        bestGain = gain;
                        bestSplit = s;
                    }
                }

                return (bestSplit, bestGain);
            }

            var segments = new List<(int Start, int End)> { (0, n - 1) };
            var points = new List<(int Index, double Gain)>();

            while (points.Count < maxChangePoints)
            {
                var candidate = -1;
                var candidateGain = 0.0;
                var candidateSplit = -1;

                for (var i = 0; i < segments.Count; i++)
                {
                    var (split, gain) = Best(segments[i].Start, segments[i].End);

                    if (split >= 0 && gain > candidateGain)
                    {
                        candidate = i;
                        candidateGain = gain;
                        candidateSplit = split;
                    }
                }

                if (candidate < 0 || candidateGain <= penalty)
                {
                    break;
                }

                var (a, b) = segments[candidate];
                segments[candidate] = (a, candidateSplit - 1);
                segments.Insert(candidate + 1, (candidateSplit, b));
                points.Add((candidateSplit, candidateGain));
            }

            var ordered = points.OrderBy(p => p.Index).ToArray();

            return new SeriesMeanShiftResult(
                ordered.Select(p => p.Index).ToArray(),
                ordered.Select(p => p.Gain).ToArray(),
                segments.Select(s => new MeanShiftSegment(s.Start, s.End, s.End - s.Start + 1,
                    (prefix[s.End + 1] - prefix[s.Start]) / (s.End - s.Start + 1))).ToArray(),
                sigma,
                penalty);
        }

        [AdvancedAnalyticsTool("DrawdownAnalysis")]
        [Description(
            "Drawdown analysis of a level series such as an account balance, cumulative profit or running total: how far the level falls below its previous high-water mark and for how long. Use it to measure the worst loss from a peak, or how long an account has been below its best level. " +
            "Drawdowns[t] is the level minus the highest level up to t (0 at a new high, otherwise negative). MaximumDrawdown is the largest fall in level units (positive) and MaximumDrawdownFraction that fall as a fraction of the peak it came from (NaN if the peak is 0 or negative); PeakIndex and TroughIndex locate it, and RecoveryIndex is the first later position back at the peak level (-1 if it never recovered). " +
            "LongestUnderwaterSteps is the longest run of positions below a high; CurrentUnderwaterSteps is the run in progress at the end. Requires finite values.")]
        public DrawdownResult DrawdownAnalysis(
            [Description("The level series in time order (balance or cumulative total), finite.")]
            double[] levels)
        {
            var v = TimeSeriesGuard.Finite(levels, nameof(levels), 2);
            var n = v.Length;
            var drawdowns = new double[n];
            var peak = v[0];
            var peakIndex = 0;
            var worst = 0.0;
            var worstPeakIndex = 0;
            var worstTrough = 0;
            var longest = 0;
            var run = 0;

            for (var t = 0; t < n; t++)
            {
                if (v[t] >= peak)
                {
                    peak = v[t];
                    peakIndex = t;
                }

                drawdowns[t] = v[t] - peak;
                run = drawdowns[t] < 0 ? run + 1 : 0;
                longest = Math.Max(longest, run);

                if (peak - v[t] > worst)
                {
                    worst = peak - v[t];
                    worstPeakIndex = peakIndex;
                    worstTrough = t;
                }
            }

            var peakLevel = v[worstPeakIndex];
            var recovery = -1;

            for (var t = worstTrough + 1; t < n; t++)
            {
                if (v[t] >= peakLevel)
                {
                    recovery = t;
                    break;
                }
            }

            if (worst == 0)
            {
                return new DrawdownResult(drawdowns, 0, double.NaN, -1, -1, -1, longest, run);
            }

            return new DrawdownResult(drawdowns, worst, peakLevel <= 0 ? double.NaN : worst / peakLevel,
                worstPeakIndex, worstTrough, recovery, longest, run);
        }
    }
}