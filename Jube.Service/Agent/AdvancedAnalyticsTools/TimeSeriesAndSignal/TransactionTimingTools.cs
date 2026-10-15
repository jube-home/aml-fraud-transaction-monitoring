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
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    public enum CalendarCycle
    {
        HourOfDay,
        DayOfWeek,
        DayOfMonth,
        MonthOfYear
    }

    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class TransactionTimingTools
    {
        private const long SecondsToTicks = 10_000_000L;

        private static DateTime FromEpoch(double epochSeconds, string name, int index)
        {
            if (epochSeconds < -62135596800.0 || epochSeconds > 253402300799.0)
            {
                throw new ArgumentException(
                    $"{name} position {index} ({epochSeconds}) is outside the supported date range (years 1 to 9999). Timestamps must be epoch seconds, not milliseconds.",
                    name);
            }

            var ticks = DateTime.UnixEpoch.Ticks + (long)Math.Round(epochSeconds * SecondsToTicks);

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static double EventsPerHour(int events, double span)
        {
            return span > 0 ? (events - 1) * 3600.0 / span : double.NaN;
        }

        [AdvancedAnalyticsTool("ParseTimestampsToEpochSeconds")]
        [Description(
            "Converts ISO 8601 timestamp strings (for example 2021-01-01T00:00:00Z, 2021-01-01T10:30:00+01:00 or 2021-01-01 10:30:00) into epoch seconds, the number of seconds since 1970-01-01 00:00:00 UTC, which is the time format every other timing and velocity tool expects. Strings with an offset or Z are converted to UTC; strings with no offset are taken to already be UTC. " +
            "The result has one number per input, fractional seconds included. If any string cannot be read, the call fails and names the position so it can be corrected.")]
        public double[] ParseTimestampsToEpochSeconds(
            [Description("The ISO 8601 date-time strings, one per event, in any order.")]
            string[] isoTimestamps)
        {
            if (isoTimestamps == null || isoTimestamps.Length == 0)
            {
                throw new ArgumentException("isoTimestamps must contain at least one ISO 8601 timestamp string.",
                    nameof(isoTimestamps));
            }

            var result = new double[isoTimestamps.Length];

            for (var i = 0; i < isoTimestamps.Length; i++)
            {
                if (!DateTimeOffset.TryParse(isoTimestamps[i], CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out var parsed))
                {
                    throw new ArgumentException(
                        $"isoTimestamps position {i} ('{isoTimestamps[i]}') is not a valid ISO 8601 date-time. Use a form such as 2021-01-31T14:05:09Z.",
                        nameof(isoTimestamps));
                }

                result[i] = (parsed.UtcTicks - DateTime.UnixEpoch.Ticks) / (double)SecondsToTicks;
            }

            return result;
        }

        [AdvancedAnalyticsTool("FormatEpochSecondsAsIso")]
        [Description(
            "Converts epoch seconds (seconds since 1970-01-01 00:00:00 UTC) into ISO 8601 UTC strings such as 2021-01-01T00:00:00Z, so results from the timing tools can be read as dates. Fractional seconds are kept to 100 nanoseconds. " +
            "Values outside years 1 to 9999 are rejected with the position; a value in the trillions probably means milliseconds were supplied instead of seconds.")]
        public string[] FormatEpochSecondsAsIso(
            [Description("Epoch seconds, one per timestamp.")]
            double[] epochSeconds)
        {
            var v = TimeSeriesGuard.Finite(epochSeconds, nameof(epochSeconds), 1);

            return v.Select((s, i) =>
                FromEpoch(s, nameof(epochSeconds), i)
                    .ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture)).ToArray();
        }

        [AdvancedAnalyticsTool("TimestampCalendarFeatures")]
        [Description(
            "Breaks epoch-second timestamps into calendar features in a chosen time zone offset: hour, ISO day of week (1 Monday to 7 Sunday), day of month, month, year, day of year, a weekend flag and the second of the day. Use it to build features such as night-time or weekend activity for fraud and AML rules. " +
            "Every output array has one entry per timestamp in the same order. utcOffsetMinutes shifts UTC to local time (60 for UTC+1, -300 for UTC-5); daylight-saving rules are not applied, so pass the offset that applies to the data.")]
        public CalendarFeaturesResult TimestampCalendarFeatures(
            [Description("Event times as epoch seconds (seconds since 1970-01-01 UTC), any order.")]
            double[] epochSeconds,
            [Description("Local offset from UTC in minutes, between -1080 and 1080. Default 0 (UTC).")]
            int utcOffsetMinutes = 0)
        {
            var v = TimeSeriesGuard.Finite(epochSeconds, nameof(epochSeconds), 1);

            if (utcOffsetMinutes < -1080 || utcOffsetMinutes > 1080)
            {
                throw new ArgumentException(
                    $"utcOffsetMinutes must be between -1080 and 1080 but was {utcOffsetMinutes}.",
                    nameof(utcOffsetMinutes));
            }

            var local = v.Select((s, i) => FromEpoch(s, nameof(epochSeconds), i).AddMinutes(utcOffsetMinutes))
                .ToArray();

            return new CalendarFeaturesResult(
                local.Select(d => d.Hour).ToArray(),
                local.Select(d => d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek).ToArray(),
                local.Select(d => d.Day).ToArray(),
                local.Select(d => d.Month).ToArray(),
                local.Select(d => d.Year).ToArray(),
                local.Select(d => d.DayOfYear).ToArray(),
                local.Select(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                    .ToArray(),
                local.Select(d => d.TimeOfDay.TotalSeconds).ToArray());
        }

        [AdvancedAnalyticsTool("InterArrivalTimeStatistics")]
        [Description(
            "Statistics of the time gaps between successive events (inter-arrival times) for a customer, account or device, from epoch-second timestamps. Use it to characterise how regular or bursty activity is. " +
            "Gaps holds each successive gap in seconds (the events are sorted first, so the input order does not matter). CoefficientOfVariation is the standard deviation over the mean: about 1 for random (Poisson) arrivals, near 0 for machine-regular activity and well above 1 for bursty activity. Burstiness is (sd - mean)/(sd + mean), from -1 (perfectly regular) through 0 (random) to +1 (extremely bursty). " +
            "EventsPerHour is the average rate over the observed span. Needs at least 2 events (3 for the spread figures, which are NaN otherwise); duplicate timestamps give gaps of 0.")]
        public InterArrivalResult InterArrivalTimeStatistics(
            [Description("Event times as epoch seconds (seconds since 1970-01-01 UTC), any order, at least 2.")]
            double[] epochSeconds)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 2, false).OrderBy(x => x).ToArray();
            var gaps = new double[t.Length - 1];

            for (var i = 0; i < gaps.Length; i++)
            {
                gaps[i] = t[i + 1] - t[i];
            }

            var mean = TimeSeriesGuard.Mean(gaps);
            var sd = gaps.Length >= 2 ? TimeSeriesGuard.SampleStandardDeviation(gaps) : double.NaN;
            var span = t[^1] - t[0];

            return new InterArrivalResult(gaps, t.Length, mean, TimeSeriesGuard.Median(gaps), sd,
                mean > 0 ? sd / mean : double.NaN, gaps.Min(), gaps.Max(),
                mean + sd > 0 ? (sd - mean) / (sd + mean) : double.NaN, span, EventsPerHour(t.Length, span));
        }

        [AdvancedAnalyticsTool("DetectTimeGaps")]
        [Description(
            "Finds unusually long pauses between events (dormancy, outages, missing feed data, an account reactivating after silence). Give an absolute threshold in seconds, or leave it NaN to flag any gap longer than gapMultiple times the median gap. " +
            "Each returned gap gives the index of the event before and after the pause, their epoch seconds and the pause length in seconds. The events must already be in time order. Use the flagged gaps to split a stream into active periods or to explain a drop in counts.")]
        public TimeGapResult DetectTimeGaps(
            [Description("Event times as epoch seconds, in ascending order, at least 2.")]
            double[] epochSeconds,
            [Description("Smallest pause in seconds to report. NaN (default) uses gapMultiple times the median gap.")]
            double minGapSeconds = double.NaN,
            [Description("Multiplier of the median gap used when minGapSeconds is NaN. Default 5.")]
            double gapMultiple = 5)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 2);
            var gaps = new double[t.Length - 1];

            for (var i = 0; i < gaps.Length; i++)
            {
                gaps[i] = t[i + 1] - t[i];
            }

            var median = TimeSeriesGuard.Median(gaps);
            double threshold;

            if (double.IsNaN(minGapSeconds))
            {
                TimeSeriesGuard.Positive(gapMultiple, nameof(gapMultiple));
                threshold = gapMultiple * median;
            }
            else
            {
                threshold = TimeSeriesGuard.Positive(minGapSeconds, nameof(minGapSeconds));
            }

            var found = Enumerable.Range(0, gaps.Length)
                .Where(i => gaps[i] > threshold)
                .Select(i => new TimeGap(i, i + 1, t[i], t[i + 1], gaps[i]))
                .ToArray();

            return new TimeGapResult(found, threshold, median, t.Length);
        }

        [AdvancedAnalyticsTool("ResampleEventsToBuckets")]
        [Description(
            "Aggregates an event stream into fixed-length time buckets (per minute, hour, day) to produce an evenly spaced series that autocorrelation, seasonality, forecasting and spectral tools can use. Give epoch-second timestamps and, optionally, a value per event such as the amount. " +
            "Buckets are aligned to originEpochSeconds (default 0, so day buckets start at midnight UTC) and run continuously from the first to the last event; empty buckets are kept with a count of 0 (set includeEmptyBuckets false to drop them). Counts is the number of events; Sums, Means, Minimums and Maximums summarise the values (sum 0 and the rest NaN for an empty bucket; all NaN when no values are supplied). " +
            "BucketStartEpochSeconds is each bucket's start. The events need not be sorted. Refuses to build more than 200000 buckets; use a longer bucketSeconds in that case.")]
        public BucketAggregationResult ResampleEventsToBuckets(
            [Description("Event times as epoch seconds (seconds since 1970-01-01 UTC), any order.")]
            double[] epochSeconds,
            [Description(
                "Optional value per event (for example amount), same order and length as epochSeconds. Leave empty for counts only.")]
            double[]? values = null,
            [Description(
                "Bucket length in seconds, above 0: 60 per minute, 3600 per hour, 86400 per day. Default 3600.")]
            double bucketSeconds = 3600,
            [Description(
                "Epoch second at which a bucket boundary falls. Default 0. Use a local midnight to align day buckets to a time zone.")]
            double originEpochSeconds = 0,
            [Description("True (default) keeps buckets with no events so the series is evenly spaced.")]
            bool includeEmptyBuckets = true)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 1, false);
            TimeSeriesGuard.Positive(bucketSeconds, nameof(bucketSeconds));

            if (!double.IsFinite(originEpochSeconds))
            {
                throw new ArgumentException("originEpochSeconds must be a finite number.", nameof(originEpochSeconds));
            }

            var hasValues = values != null && values.Length > 0;

            if (hasValues)
            {
                TimeSeriesGuard.SameLength(t, values!, nameof(epochSeconds), nameof(values));
            }

            var indices = t.Select(x => (long)Math.Floor((x - originEpochSeconds) / bucketSeconds)).ToArray();
            var first = indices.Min();
            var last = indices.Max();

            if (last - first + 1 > 200000)
            {
                throw new ArgumentException(
                    $"The span would create {last - first + 1} buckets, above the limit of 200000. Use a longer bucketSeconds.",
                    nameof(bucketSeconds));
            }

            var count = (int)(last - first + 1);
            var counts = new int[count];
            var sums = new double[count];
            var minimums = Enumerable.Repeat(double.PositiveInfinity, count).ToArray();
            var maximums = Enumerable.Repeat(double.NegativeInfinity, count).ToArray();

            for (var i = 0; i < t.Length; i++)
            {
                var b = (int)(indices[i] - first);
                counts[b]++;

                if (hasValues)
                {
                    var value = values![i];
                    sums[b] += value;
                    minimums[b] = Math.Min(minimums[b], value);
                    maximums[b] = Math.Max(maximums[b], value);
                }
            }

            var keep = Enumerable.Range(0, count).Where(b => includeEmptyBuckets || counts[b] > 0).ToArray();

            return new BucketAggregationResult(
                keep.Select(b => originEpochSeconds + (first + b) * bucketSeconds).ToArray(),
                keep.Select(b => counts[b]).ToArray(),
                keep.Select(b => hasValues ? sums[b] : double.NaN).ToArray(),
                keep.Select(b => hasValues && counts[b] > 0 ? sums[b] / counts[b] : double.NaN).ToArray(),
                keep.Select(b => hasValues && counts[b] > 0 ? minimums[b] : double.NaN).ToArray(),
                keep.Select(b => hasValues && counts[b] > 0 ? maximums[b] : double.NaN).ToArray(),
                bucketSeconds,
                counts.Count(c => c == 0));
        }

        [AdvancedAnalyticsTool("TrailingWindowVelocity")]
        [Description(
            "Transaction velocity at every event: how many events (and how much value) occurred in the window of windowSeconds ending at that event. Use it to build features like 'transactions in the last hour' or 'amount in the last 24 hours' for each transaction and to spot rapid sequences. " +
            "The window is (t - windowSeconds, t], so an event exactly windowSeconds earlier is excluded. Counts[i] and Sums[i] include the current event unless includeCurrent is false; RatesPerHour is Counts scaled to per hour; SecondsSincePreviousEvent is the gap to the previous event (NaN for the first). Events must be in ascending time order; supply values in the same order to get Sums (otherwise Sums are 0).")]
        public TrailingVelocityResult TrailingWindowVelocity(
            [Description("Event times as epoch seconds, in ascending order.")]
            double[] epochSeconds,
            [Description("Window length in seconds, above 0, for example 3600 for one hour.")]
            double windowSeconds,
            [Description(
                "Optional value per event (for example amount), same order as epochSeconds. Leave empty for counts only.")]
            double[]? values = null,
            [Description(
                "True (default) counts the current event in its own window; false counts only earlier events.")]
            bool includeCurrent = true)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 1);
            TimeSeriesGuard.Positive(windowSeconds, nameof(windowSeconds));
            var hasValues = values != null && values.Length > 0;

            if (hasValues)
            {
                TimeSeriesGuard.Finite(values, nameof(values), 1);
                TimeSeriesGuard.SameLength(t, values!, nameof(epochSeconds), nameof(values));
            }

            var n = t.Length;
            var counts = new int[n];
            var sums = new double[n];
            var previous = new double[n];
            var lo = 0;
            var running = 0.0;
            var included = 0;

            for (var i = 0; i < n; i++)
            {
                if (hasValues)
                {
                    running += values![i];
                }

                included++;

                while (t[lo] <= t[i] - windowSeconds)
                {
                    if (hasValues)
                    {
                        running -= values![lo];
                    }

                    lo++;
                    included--;
                }

                var count = includeCurrent ? included : included - 1;
                counts[i] = count;

                if (hasValues)
                {
                    sums[i] = includeCurrent ? running : running - values![i];
                }

                previous[i] = i == 0 ? double.NaN : t[i] - t[i - 1];
            }

            return new TrailingVelocityResult(counts, sums, counts.Select(c => c * 3600.0 / windowSeconds).ToArray(),
                previous, windowSeconds);
        }

        [AdvancedAnalyticsTool("MultiWindowVelocityFeatures")]
        [Description(
            "Snapshot of activity at one moment over several look-back windows at once, for example the last 1 minute, 1 hour and 24 hours, as a ready-made feature vector. Windows are (asOf - window, asOf] and only events at or before asOf are counted, so it can be run at any point in the past without leaking later data. " +
            "Counts[i], Sums[i] and RatesPerHour[i] describe WindowSeconds[i]. SecondsSinceLastEvent is the time from the latest event at or before asOf to asOf (NaN if none) and TotalEventsUpToAsOf the whole history. Leave asOf NaN to use the last event's time. Events must be in ascending order; without values, Sums are 0.")]
        public VelocitySnapshotResult MultiWindowVelocityFeatures(
            [Description("Event times as epoch seconds, in ascending order.")]
            double[] epochSeconds,
            [Description("Look-back windows in seconds, each above 0, for example 60, 3600, 86400.")]
            double[] windowsSeconds,
            [Description("Optional value per event, same order as epochSeconds. Leave empty for counts only.")]
            double[]? values = null,
            [Description(
                "The moment to compute the snapshot at, as epoch seconds; NaN (default) uses the last event time.")]
            double asOfEpochSeconds = double.NaN)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 1);
            var w = TimeSeriesGuard.Finite(windowsSeconds, nameof(windowsSeconds), 1);

            if (w.Any(x => x <= 0))
            {
                throw new ArgumentException("Every window in windowsSeconds must be above 0.", nameof(windowsSeconds));
            }

            var hasValues = values != null && values.Length > 0;

            if (hasValues)
            {
                TimeSeriesGuard.Finite(values, nameof(values), 1);
                TimeSeriesGuard.SameLength(t, values!, nameof(epochSeconds), nameof(values));
            }

            var asOf = double.IsNaN(asOfEpochSeconds) ? t[^1] : asOfEpochSeconds;

            if (!double.IsFinite(asOf))
            {
                throw new ArgumentException("asOfEpochSeconds must be finite or NaN.", nameof(asOfEpochSeconds));
            }

            var upTo = Enumerable.Range(0, t.Length).Where(i => t[i] <= asOf).ToArray();
            var counts = new int[w.Length];
            var sums = new double[w.Length];

            for (var k = 0; k < w.Length; k++)
            {
                foreach (var i in upTo)
                {
                    if (t[i] > asOf - w[k])
                    {
                        counts[k]++;

                        if (hasValues)
                        {
                            sums[k] += values![i];
                        }
                    }
                }
            }

            return new VelocitySnapshotResult(asOf, w, counts, sums,
                counts.Select((c, k) => c * 3600.0 / w[k]).ToArray(),
                upTo.Length == 0 ? double.NaN : asOf - t[upTo[^1]], upTo.Length);
        }

        [AdvancedAnalyticsTool("WindowRateChangeTest")]
        [Description(
            "Tests whether the event rate in a recent window is unusually high or low compared with the preceding baseline window, assuming events arrive at a steady random (Poisson) rate. Use it to detect a sudden surge or collapse in activity for a customer or channel. " +
            "The recent window is (asOf - recentSeconds, asOf] and the baseline is the baselineSeconds before it. ExpectedRecentCount is what the baseline rate predicts for the recent window (the baseline count is floored at 0.5 so a silent baseline still gives a usable expectation); RateRatio is recent rate over baseline rate; PValueIncrease is the probability of seeing at least the recent count by chance and PValueDecrease of seeing at most that many. Small values indicate a real change. " +
            "Events must be in ascending order; leave asOf NaN to use the last event time.")]
        public RateChangeResult WindowRateChangeTest(
            [Description("Event times as epoch seconds, in ascending order.")]
            double[] epochSeconds,
            [Description("Length in seconds of the recent window, above 0.")]
            double recentSeconds,
            [Description(
                "Length in seconds of the baseline window before the recent one, above 0 and normally several times longer.")]
            double baselineSeconds,
            [Description(
                "The moment the recent window ends, as epoch seconds; NaN (default) uses the last event time.")]
            double asOfEpochSeconds = double.NaN)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 1);
            TimeSeriesGuard.Positive(recentSeconds, nameof(recentSeconds));
            TimeSeriesGuard.Positive(baselineSeconds, nameof(baselineSeconds));
            var asOf = double.IsNaN(asOfEpochSeconds) ? t[^1] : asOfEpochSeconds;
            var recent = t.Count(x => x <= asOf && x > asOf - recentSeconds);
            var baseline = t.Count(x => x <= asOf - recentSeconds && x > asOf - recentSeconds - baselineSeconds);
            var expected = Math.Max(baseline, 0.5) * recentSeconds / baselineSeconds;
            var poisson = new PoissonDistribution(expected);
            var increase = recent == 0 ? 1.0 : poisson.ComplementaryDistributionFunction(recent - 1);
            var decrease = poisson.DistributionFunction(recent);
            var recentRate = recent * 3600.0 / recentSeconds;
            var baselineRate = baseline * 3600.0 / baselineSeconds;
            var ratio = baseline == 0 ? recent == 0 ? double.NaN : double.PositiveInfinity : recentRate / baselineRate;

            var reading = increase < 0.01 ? "The recent rate is significantly higher than the baseline."
                : decrease < 0.01 ? "The recent rate is significantly lower than the baseline."
                : "No significant change in rate.";

            return new RateChangeResult(recent, baseline, recentRate, baselineRate, ratio, expected, increase, decrease,
                reading);
        }

        [AdvancedAnalyticsTool("EventBurstDetection")]
        [Description(
            "Finds bursts: short periods in which at least minEvents events fall inside a window of windowSeconds, such as rapid-fire card attempts or structured deposits. Every event that starts a window meeting the threshold is marked, and overlapping windows are merged into one burst. " +
            "Each burst reports its first and last event index, epoch seconds, EventCount (all events in the merged burst), MaximumWindowCount (the busiest single window) and PValue, the probability of that many events in one window if events arrived at the overall average rate (a small value means the burst is not explained by the normal rate; NaN when the overall span is 0). Events must be in ascending time order.")]
        public BurstDetectionResult EventBurstDetection(
            [Description("Event times as epoch seconds, in ascending order.")]
            double[] epochSeconds,
            [Description("Window length in seconds, above 0.")]
            double windowSeconds,
            [Description("Smallest number of events in one window that counts as a burst, at least 2.")]
            int minEvents)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 1);
            TimeSeriesGuard.Positive(windowSeconds, nameof(windowSeconds));
            TimeSeriesGuard.AtLeast(minEvents, 2, nameof(minEvents));
            var n = t.Length;
            var marked = new bool[n];
            var windowCount = new int[n];
            var j = 0;

            for (var i = 0; i < n; i++)
            {
                j = Math.Max(j, i);

                while (j + 1 < n && t[j + 1] <= t[i] + windowSeconds)
                {
                    j++;
                }

                windowCount[i] = j - i + 1;

                if (windowCount[i] >= minEvents)
                {
                    for (var k = i; k <= j; k++)
                    {
                        marked[k] = true;
                    }
                }
            }

            var span = t[^1] - t[0];
            var rate = span > 0 ? (n - 1) / span : double.NaN;
            var bursts = new List<EventBurst>();
            var idx = 0;

            while (idx < n)
            {
                if (!marked[idx])
                {
                    idx++;
                    continue;
                }

                var start = idx;

                while (idx + 1 < n && marked[idx + 1])
                {
                    idx++;
                }

                var max = 0;

                for (var i = start; i <= idx; i++)
                {
                    max = Math.Max(max, windowCount[i]);
                }

                var p = double.IsNaN(rate)
                    ? double.NaN
                    : new PoissonDistribution(Math.Max(rate * windowSeconds, 1e-12))
                        .ComplementaryDistributionFunction(max - 1);

                bursts.Add(new EventBurst(start, idx, t[start], t[idx], idx - start + 1, max, p));
                idx++;
            }

            return new BurstDetectionResult(bursts.ToArray(), rate, windowSeconds, minEvents);
        }

        [AdvancedAnalyticsTool("SessionizeEvents")]
        [Description(
            "Splits an event stream into sessions: a new session starts whenever the gap since the previous event exceeds timeoutSeconds. Use it to group transactions into visits or activity episodes and to count events per episode. " +
            "SessionIds[i] is the zero-based session of event i; Sessions lists each session's first and last event index, event count, start and end epoch seconds and duration. A gap equal to the timeout does not start a new session. Events must be in ascending time order.")]
        public SessionizeResult SessionizeEvents(
            [Description("Event times as epoch seconds, in ascending order.")]
            double[] epochSeconds,
            [Description("Inactivity gap in seconds that closes a session, above 0, for example 1800 for 30 minutes.")]
            double timeoutSeconds)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 1);
            TimeSeriesGuard.Positive(timeoutSeconds, nameof(timeoutSeconds));
            var ids = new int[t.Length];
            var sessions = new List<SessionSummary>();
            var start = 0;

            for (var i = 1; i <= t.Length; i++)
            {
                if (i == t.Length || t[i] - t[i - 1] > timeoutSeconds)
                {
                    var id = sessions.Count;

                    sessions.Add(new SessionSummary(id, start, i - 1, i - start, t[start], t[i - 1],
                        t[i - 1] - t[start]));

                    for (var k = start; k < i; k++)
                    {
                        ids[k] = id;
                    }

                    start = i;
                }
            }

            return new SessionizeResult(ids, sessions.ToArray(), timeoutSeconds);
        }

        [AdvancedAnalyticsTool("CalendarCycleProfile")]
        [Description(
            "Profiles when events happen within a calendar cycle: by hour of day, day of week (Monday first), day of month or month of year, in a chosen time zone offset. Use it to see whether activity is concentrated at unusual times, for example overnight or on weekends. " +
            "Labels, Counts and Shares give the distribution (one entry per hour, weekday, day or month); PeakLabel is the busiest; EntropyBits and NormalisedEntropy (0 to 1) measure how spread out activity is, where 1 means perfectly even; ChiSquareStatistic, DegreesOfFreedom and PValue test against a uniform spread (a small PValue means the timing is concentrated; day-of-month uniformity is only approximate because months differ in length). " +
            "Timestamps are epoch seconds; daylight-saving is not applied.")]
        public CalendarProfileResult CalendarCycleProfile(
            [Description("Event times as epoch seconds, any order.")]
            double[] epochSeconds,
            [Description("The cycle to profile: HourOfDay, DayOfWeek, DayOfMonth or MonthOfYear. Default HourOfDay.")]
            CalendarCycle cycle = CalendarCycle.HourOfDay,
            [Description("Local offset from UTC in minutes, between -1080 and 1080. Default 0.")]
            int utcOffsetMinutes = 0)
        {
            var t = TimeSeriesGuard.Finite(epochSeconds, nameof(epochSeconds), 1);

            if (utcOffsetMinutes < -1080 || utcOffsetMinutes > 1080)
            {
                throw new ArgumentException(
                    $"utcOffsetMinutes must be between -1080 and 1080 but was {utcOffsetMinutes}.",
                    nameof(utcOffsetMinutes));
            }

            string[] labels;

            switch (cycle)
            {
                case CalendarCycle.HourOfDay:
                    labels = Enumerable.Range(0, 24).Select(h => h.ToString("00", CultureInfo.InvariantCulture))
                        .ToArray();

                    break;
                case CalendarCycle.DayOfWeek:
                    labels = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                    break;
                case CalendarCycle.DayOfMonth:
                    labels = Enumerable.Range(1, 31).Select(d => d.ToString(CultureInfo.InvariantCulture)).ToArray();
                    break;
                default:
                    labels = new[]
                    {
                        "January", "February", "March", "April", "May", "June", "July", "August", "September",
                        "October",
                        "November", "December"
                    };

                    break;
            }

            var counts = new int[labels.Length];

            for (var i = 0; i < t.Length; i++)
            {
                var d = FromEpoch(t[i], nameof(epochSeconds), i).AddMinutes(utcOffsetMinutes);

                var bin = cycle switch
                {
                    CalendarCycle.HourOfDay => d.Hour,
                    CalendarCycle.DayOfWeek => d.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)d.DayOfWeek - 1,
                    CalendarCycle.DayOfMonth => d.Day - 1,
                    _ => d.Month - 1
                };

                counts[bin]++;
            }

            var total = (double)t.Length;
            var shares = counts.Select(c => c / total).ToArray();
            var entropy = -shares.Where(s => s > 0).Sum(s => s * Math.Log(s, 2));
            var expected = total / labels.Length;
            var chi = counts.Sum(c => (c - expected) * (c - expected) / expected);
            var df = labels.Length - 1;
            var peak = Array.IndexOf(counts, counts.Max());

            return new CalendarProfileResult(labels, counts, shares, labels[peak], entropy,
                entropy / Math.Log(labels.Length, 2),
                chi, df, TimeSeriesGuard.ChiSquareUpperTail(chi, df), t.Length);
        }

        [AdvancedAnalyticsTool("EventPeriodicityScore")]
        [Description(
            "Measures how machine-like the timing of an event stream is, to catch scheduled, automated or beaconing behaviour such as identical-interval payments. It looks at the gaps between events: MedianIntervalSeconds is the typical gap, CoefficientOfVariation is spread over mean (near 0 for clockwork regularity, about 1 for random arrivals, above 1 for bursty), and ShareWithinTolerance is the fraction of gaps within the tolerance fraction of the median (1 means every gap is essentially the same). " +
            "Reading summarises: highly regular (share at least 0.9 and variation below 0.2), somewhat regular, or irregular. Needs at least 3 events; timestamps are epoch seconds and are sorted first.")]
        public PeriodicityResult EventPeriodicityScore(
            [Description("Event times as epoch seconds, any order, at least 3.")]
            double[] epochSeconds,
            [Description(
                "Relative tolerance around the median gap, for example 0.1 for plus or minus 10 percent. Default 0.1.")]
            double toleranceFraction = 0.1)
        {
            var t = TimeSeriesGuard.Epoch(epochSeconds, nameof(epochSeconds), 3, false).OrderBy(x => x).ToArray();
            TimeSeriesGuard.Positive(toleranceFraction, nameof(toleranceFraction));
            var gaps = new double[t.Length - 1];

            for (var i = 0; i < gaps.Length; i++)
            {
                gaps[i] = t[i + 1] - t[i];
            }

            var median = TimeSeriesGuard.Median(gaps);
            var mean = TimeSeriesGuard.Mean(gaps);
            var cv = mean > 0 ? TimeSeriesGuard.SampleStandardDeviation(gaps) / mean : double.NaN;
            var share = gaps.Count(g => Math.Abs(g - median) <= toleranceFraction * median) / (double)gaps.Length;

            var reading = share >= 0.9 && cv < 0.2
                ? "Highly regular timing, consistent with scheduled or automated activity."
                : share >= 0.6
                    ? "Somewhat regular timing."
                    : "Irregular timing.";

            return new PeriodicityResult(t.Length, median, mean, cv, share, toleranceFraction, reading);
        }

        [AdvancedAnalyticsTool("CountDispersionIndexTest")]
        [Description(
            "Index of dispersion test for event counts per period (for example transactions per hour): compares the variance of the counts with their mean. For purely random (Poisson) arrivals the variance equals the mean, so DispersionIndex is near 1. " +
            "A value well above 1 means overdispersion (bursts or clustering; PValueOverdispersed is small); well below 1 means underdispersion (unnaturally regular counts; PValueUnderdispersed is small). ChiSquareStatistic is (n-1) x variance / mean with n-1 degrees of freedom. " +
            "Counts must be finite and not negative, with at least 3 periods and a mean above 0; produce them with ResampleEventsToBuckets.")]
        public DispersionTestResult CountDispersionIndexTest(
            [Description("Non-negative event counts per equal-length period, at least 3 periods.")]
            double[] counts)
        {
            var c = TimeSeriesGuard.Finite(counts, nameof(counts), 3);

            if (c.Any(x => x < 0))
            {
                throw new ArgumentException("counts must not contain negative numbers.", nameof(counts));
            }

            var mean = TimeSeriesGuard.Mean(c);

            if (mean <= 0)
            {
                throw new ArgumentException(
                    "counts are all zero, so the dispersion index is undefined. Supply periods with at least some events.",
                    nameof(counts));
            }

            var variance = TimeSeriesGuard.SampleVariance(c);
            var index = variance / mean;
            var df = c.Length - 1;
            var chi = df * index;
            var upper = TimeSeriesGuard.ChiSquareUpperTail(chi, df);
            var lower = 1 - upper;

            var reading = upper < 0.05 ? "Overdispersed: counts are burstier than random arrivals."
                : lower < 0.05 ? "Underdispersed: counts are more regular than random arrivals."
                : "Consistent with random (Poisson) arrivals.";

            return new DispersionTestResult(mean, variance, index, chi, df, upper, lower, c.Length, reading);
        }
    }
}