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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models
{
    public sealed record CalendarFeaturesResult(
        int[] Hour,
        int[] DayOfWeekIso,
        int[] DayOfMonth,
        int[] Month,
        int[] Year,
        int[] DayOfYear,
        bool[] IsWeekend,
        double[] SecondOfDay);

    public sealed record InterArrivalResult(
        double[] Gaps,
        int Events,
        double MeanGapSeconds,
        double MedianGapSeconds,
        double StandardDeviationSeconds,
        double CoefficientOfVariation,
        double MinimumGapSeconds,
        double MaximumGapSeconds,
        double Burstiness,
        double SpanSeconds,
        double EventsPerHour);

    public sealed record TimeGap(
        int StartIndex,
        int EndIndex,
        double StartEpochSeconds,
        double EndEpochSeconds,
        double DurationSeconds);

    public sealed record TimeGapResult(
        TimeGap[] Gaps,
        double ThresholdSeconds,
        double MedianGapSeconds,
        int Events);

    public sealed record BucketAggregationResult(
        double[] BucketStartEpochSeconds,
        int[] Counts,
        double[] Sums,
        double[] Means,
        double[] Minimums,
        double[] Maximums,
        double BucketSeconds,
        int EmptyBuckets);

    public sealed record TrailingVelocityResult(
        int[] Counts,
        double[] Sums,
        double[] RatesPerHour,
        double[] SecondsSincePreviousEvent,
        double WindowSeconds);

    public sealed record VelocitySnapshotResult(
        double AsOfEpochSeconds,
        double[] WindowSeconds,
        int[] Counts,
        double[] Sums,
        double[] RatesPerHour,
        double SecondsSinceLastEvent,
        int TotalEventsUpToAsOf);

    public sealed record RateChangeResult(
        int RecentCount,
        int BaselineCount,
        double RecentRatePerHour,
        double BaselineRatePerHour,
        double RateRatio,
        double ExpectedRecentCount,
        double PValueIncrease,
        double PValueDecrease,
        string Reading);

    public sealed record EventBurst(
        int StartIndex,
        int EndIndex,
        double StartEpochSeconds,
        double EndEpochSeconds,
        int EventCount,
        int MaximumWindowCount,
        double PValue);

    public sealed record BurstDetectionResult(
        EventBurst[] Bursts,
        double BaselineRatePerSecond,
        double WindowSeconds,
        int MinimumEvents);

    public sealed record SessionSummary(
        int SessionId,
        int StartIndex,
        int EndIndex,
        int EventCount,
        double StartEpochSeconds,
        double EndEpochSeconds,
        double DurationSeconds);

    public sealed record SessionizeResult(
        int[] SessionIds,
        SessionSummary[] Sessions,
        double TimeoutSeconds);

    public sealed record CalendarProfileResult(
        string[] Labels,
        int[] Counts,
        double[] Shares,
        string PeakLabel,
        double EntropyBits,
        double NormalisedEntropy,
        double ChiSquareStatistic,
        int DegreesOfFreedom,
        double PValue,
        int Events);

    public sealed record PeriodicityResult(
        int Events,
        double MedianIntervalSeconds,
        double MeanIntervalSeconds,
        double CoefficientOfVariation,
        double ShareWithinTolerance,
        double ToleranceFraction,
        string Reading);

    public sealed record DispersionTestResult(
        double Mean,
        double Variance,
        double DispersionIndex,
        double ChiSquareStatistic,
        int DegreesOfFreedom,
        double PValueOverdispersed,
        double PValueUnderdispersed,
        int Observations,
        string Reading);
}