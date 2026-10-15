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
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    [AdvancedAnalyticsToolGroup("CategoricalAndSurvival")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SurvivalEstimationTools
    {
        [AdvancedAnalyticsTool("EstimateKaplanMeierSurvival")]
        [Description(
            "Estimates the survival curve, the probability of still being event-free at each time, with the Kaplan-Meier product-limit method, allowing for censored cases (those still event-free when observation stopped). " +
            "Each row gives the time, the number at risk just before it, the events and censorings at it, the survival probability, its Greenwood standard error and a pointwise confidence band (log-log by default, which stays inside 0 to 1). " +
            "Also returned are the median survival time (first time the curve drops to 0.5 or below; NaN when the curve never gets that low, meaning more than half are still event-free) with its confidence limits, and the lower and upper quartile times. " +
            "Use it for time-to-event data such as time to chargeback, time to default or time to alert closure. Time must be measured from a common start and be at least 0; events are 1 for the event observed and 0 for censored. Rows with a NaN time are dropped.")]
        public KaplanMeierResult EstimateKaplanMeierSurvival(
            [Description(
                "Time from the common start to the event or to censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description(
                "Event indicator per case in the same order: 1 if the event was observed at that time, 0 if the case was censored.")]
            int[] events,
            [Description("Confidence level for the band, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Band transformation: LogLog (default, recommended), Log or Plain.")]
            SurvivalIntervalMethod intervalMethod = SurvivalIntervalMethod.LogLog,
            [Description(
                "True to list a row at every time including censoring-only times; false (default) lists event times only.")]
            bool includeCensoringTimes = false,
            [Description("Maximum rows to return, thinned evenly keeping the first and last; 0 (default) returns all.")]
            int maxRows = 0)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (maxRows < 0)
            {
                throw new ArgumentException("maxRows cannot be negative; use 0 for all rows.", nameof(maxRows));
            }

            var steps = SurvivalMath.Steps(t, e);
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);

            var rows = steps.Where(s => includeCensoringTimes || s.Events > 0).Select(s =>
            {
                var band = SurvivalMath.Interval(s.Survival, s.GreenwoodSum, z, intervalMethod);

                return new KaplanMeierRow
                {
                    Time = s.Time,
                    AtRisk = s.AtRisk,
                    Events = s.Events,
                    Censored = s.Censored,
                    Survival = s.Survival,
                    StandardError = s.Survival * Math.Sqrt(s.GreenwoodSum),
                    Lower = band.Lower,
                    Upper = band.Upper,
                    CumulativeHazard = s.CumulativeHazard
                };
            }).ToArray();

            return new KaplanMeierResult
            {
                Rows = Thin(rows, maxRows),
                Subjects = t.Length,
                Events = e.Sum(),
                Censored = t.Length - e.Sum(),
                MaximumObservedTime = t.Max(),
                LowerQuartileTime = SurvivalMath.Quantile(steps, 0.75),
                MedianSurvivalTime = SurvivalMath.Quantile(steps, 0.5),
                MedianLower = SurvivalMath.BoundQuantile(steps, 0.5, z, intervalMethod, true),
                MedianUpper = SurvivalMath.BoundQuantile(steps, 0.5, z, intervalMethod, false),
                UpperQuartileTime = SurvivalMath.Quantile(steps, 0.25),
                ConfidenceMethod = intervalMethod.ToString(),
                ConfidenceLevel = confidenceLevel,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("EstimateKaplanMeierByGroup")]
        [Description(
            "Runs a Kaplan-Meier analysis separately for each group (for example customer segment, channel or treatment arm) and compares the groups with a log-rank test. " +
            "For every group it returns the numbers of subjects, events and censored cases, the median survival time with confidence limits, the quartile times, the longest observation and, optionally, the survival probability with a confidence band at chosen evaluation times. " +
            "The overall log-rank chi-square and p-value test whether the survival curves differ at all between the groups (a small p-value means they do); with one group the test is NaN. Median NaN means fewer than half of that group had the event. " +
            "Rows with a NaN time or a blank group are dropped.")]
        public GroupedKaplanMeierResult EstimateKaplanMeierByGroup(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description("Group name for each case in the same order; blank names are dropped.")]
            string[] groups,
            [Description(
                "Optional times at which to report each group's survival probability with its band; leave empty to omit.")]
            double[]? evaluationTimes = null,
            [Description("Confidence level for the bands and medians, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Band transformation: LogLog (default), Log or Plain.")]
            SurvivalIntervalMethod intervalMethod = SurvivalIntervalMethod.LogLog)
        {
            SurvivalGuards.CleanGrouped(times, events, groups, out var t, out var e, out var g, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var evaluation = CheckEvaluationTimes(evaluationTimes, false);
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var names = g.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var summaries = new List<GroupSurvivalSummary>();

            foreach (var name in names)
            {
                var members = Enumerable.Range(0, t.Length).Where(i => g[i] == name).ToArray();
                var gt = members.Select(i => t[i]).ToArray();
                var ge = members.Select(i => e[i]).ToArray();
                var steps = SurvivalMath.Steps(gt, ge);

                summaries.Add(new GroupSurvivalSummary
                {
                    Group = name,
                    Subjects = gt.Length,
                    Events = ge.Sum(),
                    Censored = gt.Length - ge.Sum(),
                    LowerQuartileTime = SurvivalMath.Quantile(steps, 0.75),
                    MedianSurvivalTime = SurvivalMath.Quantile(steps, 0.5),
                    MedianLower = SurvivalMath.BoundQuantile(steps, 0.5, z, intervalMethod, true),
                    MedianUpper = SurvivalMath.BoundQuantile(steps, 0.5, z, intervalMethod, false),
                    UpperQuartileTime = SurvivalMath.Quantile(steps, 0.25),
                    MaximumObservedTime = gt.Max(),
                    SurvivalAtEvaluationTimes = evaluation.Select(time => PointAt(steps, gt, time, z, intervalMethod))
                        .ToArray()
                });
            }

            var chi = double.NaN;
            var df = 0;
            var p = double.NaN;

            if (names.Length >= 2)
            {
                var test = SurvivalMath.LogRank(t, e, g, LogRankWeighting.LogRank, 0d, 0d, confidenceLevel, skipped);
                chi = test.ChiSquare;
                df = test.DegreesOfFreedom;
                p = test.PValue;
            }

            return new GroupedKaplanMeierResult
            {
                Groups = summaries.ToArray(),
                LogRankChiSquare = chi,
                LogRankDegreesOfFreedom = df,
                LogRankPValue = p,
                ConfidenceMethod = intervalMethod.ToString(),
                ConfidenceLevel = confidenceLevel,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("EstimateNelsonAalenCumulativeHazard")]
        [Description(
            "Estimates the cumulative hazard function H(t), the accumulated risk of the event up to time t, with the Nelson-Aalen estimator, allowing for censoring. " +
            "Each row gives the time, number at risk, events, the cumulative hazard, its standard error, a log-scale confidence band and exp(minus H) as an approximation of survival. " +
            "The slope of the curve is the hazard rate: a straight line means a constant hazard, a steepening curve a rising risk. Prefer it to Kaplan-Meier when you want to look at how risk changes over time. " +
            "Events are 1 for observed and 0 for censored; NaN time rows are dropped.")]
        public NelsonAalenResult EstimateNelsonAalenCumulativeHazard(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description("Confidence level for the band, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "True to list a row at every time including censoring-only times; false (default) lists event times only.")]
            bool includeCensoringTimes = false,
            [Description("Maximum rows to return, thinned evenly keeping the first and last; 0 (default) returns all.")]
            int maxRows = 0)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (maxRows < 0)
            {
                throw new ArgumentException("maxRows cannot be negative; use 0 for all rows.", nameof(maxRows));
            }

            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);

            var rows = SurvivalMath.Steps(t, e).Where(s => includeCensoringTimes || s.Events > 0).Select(s =>
            {
                var se = Math.Sqrt(s.HazardVariance);
                var factor = s.CumulativeHazard > 0d ? Math.Exp(z * se / s.CumulativeHazard) : 1d;

                return new NelsonAalenRow
                {
                    Time = s.Time,
                    AtRisk = s.AtRisk,
                    Events = s.Events,
                    CumulativeHazard = s.CumulativeHazard,
                    StandardError = se,
                    Lower = s.CumulativeHazard / factor,
                    Upper = s.CumulativeHazard * factor,
                    Survival = Math.Exp(-s.CumulativeHazard)
                };
            }).ToArray();

            return new NelsonAalenResult
            {
                Rows = Thin(rows, maxRows),
                Subjects = t.Length,
                Events = e.Sum(),
                ConfidenceLevel = confidenceLevel,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("EvaluateSurvivalAtTimes")]
        [Description(
            "Reads the Kaplan-Meier survival curve at specific times of interest, for example the share of accounts still free of chargebacks after 30, 90 and 180 days. " +
            "For each requested time it returns the number still at risk, the survival probability with its standard error and confidence band, the Nelson-Aalen cumulative hazard and the cumulative probability of the event (one minus survival). " +
            "Before the first event survival is 1. Times beyond the longest observation return the last estimate, which is an extrapolation you should not trust when the last cases were censored, so compare with MaximumObservedTime. " +
            "Events are 1 for observed and 0 for censored; NaN time rows are dropped.")]
        public SurvivalAtTimesResult EvaluateSurvivalAtTimes(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description("The times at which to read the curve, each zero or more, for example 30, 90, 180.")]
            double[] evaluationTimes,
            [Description("Confidence level for the band, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Band transformation: LogLog (default), Log or Plain.")]
            SurvivalIntervalMethod intervalMethod = SurvivalIntervalMethod.LogLog)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var evaluation = CheckEvaluationTimes(evaluationTimes, true);
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var steps = SurvivalMath.Steps(t, e);

            return new SurvivalAtTimesResult
            {
                Points = evaluation.Select(time => PointAt(steps, t, time, z, intervalMethod)).ToArray(),
                ConfidenceMethod = intervalMethod.ToString(),
                ConfidenceLevel = confidenceLevel,
                Subjects = t.Length,
                Events = e.Sum(),
                MaximumObservedTime = t.Max(),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculateSurvivalTimeQuantiles")]
        [Description(
            "Finds the times by which chosen proportions of cases have experienced the event, from the Kaplan-Meier curve, with Brookmeyer-Crowley confidence limits. " +
            "Give proportions such as 0.25, 0.5 and 0.9: the result for 0.5 is the median survival time, and for 0.9 the time by which 90 percent have had the event. " +
            "A NaN time means the curve never drops far enough within the observation period, so that quantile cannot be estimated; a NaN limit means the confidence band never crosses the level. " +
            "Events are 1 for observed and 0 for censored; NaN time rows are dropped.")]
        public SurvivalQuantilesResult CalculateSurvivalTimeQuantiles(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description(
                "Proportions of cases with the event, each strictly between 0 and 1, for example 0.25, 0.5, 0.75.")]
            double[] proportions,
            [Description("Confidence level for the limits, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Band transformation used for the limits: LogLog (default), Log or Plain.")]
            SurvivalIntervalMethod intervalMethod = SurvivalIntervalMethod.LogLog)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CategoricalGuards.Required(proportions, nameof(proportions));

            if (proportions.Length == 0 || proportions.Any(p => double.IsNaN(p) || p <= 0d || p >= 1d))
            {
                throw new ArgumentException(
                    "Give at least one proportion, each strictly between 0 and 1, for example 0.5 for the median.",
                    nameof(proportions));
            }

            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var steps = SurvivalMath.Steps(t, e);

            return new SurvivalQuantilesResult
            {
                Quantiles = proportions.Select(p => new SurvivalQuantileRow
                {
                    ProportionWithEvent = p,
                    SurvivalLevel = 1d - p,
                    Time = SurvivalMath.Quantile(steps, 1d - p),
                    Lower = SurvivalMath.BoundQuantile(steps, 1d - p, z, intervalMethod, true),
                    Upper = SurvivalMath.BoundQuantile(steps, 1d - p, z, intervalMethod, false)
                }).ToArray(),
                ConfidenceMethod = intervalMethod.ToString(),
                ConfidenceLevel = confidenceLevel,
                Subjects = t.Length,
                Events = e.Sum(),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculateRestrictedMeanSurvivalTime")]
        [Description(
            "Computes the restricted mean survival time (RMST): the average event-free time over the first horizon time units, equal to the area under the Kaplan-Meier curve from 0 to the horizon. " +
            "Unlike the median it is always defined and easy to explain, for example 'on average an account stays clean for 160 of its first 180 days'. It returns the RMST with standard error and confidence interval, and LifetimeLost, the average time lost to the event within the horizon (horizon minus RMST). " +
            "If the horizon exceeds the longest observation the curve is assumed flat beyond it, and HorizonBeyondLastObservation says so; choose a horizon within the follow-up. Events are 1 for observed and 0 for censored; NaN time rows are dropped.")]
        public RestrictedMeanResult CalculateRestrictedMeanSurvivalTime(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description("Horizon time up to which the mean is taken, above 0, in the same unit as times.")]
            double horizon,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CategoricalGuards.PositiveFinite(horizon, nameof(horizon));
            var (area, variance) = SurvivalMath.RestrictedMean(SurvivalMath.Steps(t, e), horizon);
            var se = Math.Sqrt(variance);
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);

            return new RestrictedMeanResult
            {
                Horizon = horizon,
                RestrictedMeanSurvivalTime = area,
                StandardError = se,
                Lower = Math.Max(0d, area - z * se),
                Upper = Math.Min(horizon, area + z * se),
                LifetimeLost = horizon - area,
                HorizonBeyondLastObservation = horizon > t.Max(),
                MaximumObservedTime = t.Max(),
                Subjects = t.Length,
                Events = e.Sum(),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CompareRestrictedMeanSurvivalTime")]
        [Description(
            "Compares two groups by their restricted mean survival time up to a horizon, returning each group's RMST, the difference (first group minus second) with standard error, confidence interval, z statistic and two-sided p-value, and the ratio. " +
            "It needs no proportional-hazards assumption, so it is a good complement to the log-rank test and Cox model when the curves cross. A positive difference means the first group (alphabetically first name) stays event-free longer on average. " +
            "Exactly two distinct groups are required. Rows with a NaN time or a blank group are dropped; events are 1 for observed and 0 for censored.")]
        public RestrictedMeanComparisonResult CompareRestrictedMeanSurvivalTime(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description("Group name for each case; exactly two distinct names must occur.")]
            string[] groups,
            [Description("Horizon time up to which the mean is taken, above 0, in the same unit as times.")]
            double horizon,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            SurvivalGuards.CleanGrouped(times, events, groups, out var t, out var e, out var g, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CategoricalGuards.PositiveFinite(horizon, nameof(horizon));
            var names = g.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray();

            if (names.Length != 2)
            {
                throw new ArgumentException(
                    $"Exactly two distinct groups are required but {names.Length} were found.", nameof(groups));
            }

            var results = names.Select(name =>
            {
                var members = Enumerable.Range(0, t.Length).Where(i => g[i] == name).ToArray();
                var gt = members.Select(i => t[i]).ToArray();
                var ge = members.Select(i => e[i]).ToArray();

                return (Rmst: SurvivalMath.RestrictedMean(SurvivalMath.Steps(gt, ge), horizon), Max: gt.Max());
            }).ToArray();

            var difference = results[0].Rmst.Area - results[1].Rmst.Area;
            var se = Math.Sqrt(results[0].Rmst.Variance + results[1].Rmst.Variance);
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var statistic = se > 0d ? difference / se : double.NaN;

            return new RestrictedMeanComparisonResult
            {
                FirstGroup = names[0],
                SecondGroup = names[1],
                Horizon = horizon,
                RestrictedMeanFirst = results[0].Rmst.Area,
                RestrictedMeanSecond = results[1].Rmst.Area,
                Difference = difference,
                DifferenceStandardError = se,
                DifferenceLower = difference - z * se,
                DifferenceUpper = difference + z * se,
                Ratio = results[1].Rmst.Area > 0d ? results[0].Rmst.Area / results[1].Rmst.Area : double.NaN,
                ZStatistic = statistic,
                PValue = double.IsNaN(statistic)
                    ? double.NaN
                    : 2d * CategoricalLibrary.NormalSurvival(Math.Abs(statistic)),
                HorizonBeyondLastObservation = results.Any(r => horizon > r.Max),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("EstimateMedianFollowUpTime")]
        [Description(
            "Estimates how long a cohort has typically been followed, using the reverse Kaplan-Meier method: the roles of events and censoring are swapped so that the median time of the reverse curve is the median follow-up that would have been observed had nobody had the event. " +
            "It is the accepted way to report follow-up when events end observation early, since a simple median of observation times is biased downward. The result also gives that simple median and the longest observation for comparison, with confidence limits on the reverse-Kaplan-Meier median. " +
            "NaN means the reverse curve never falls to 0.5, which happens when few cases were censored. Events are 1 for observed and 0 for censored; NaN time rows are dropped.")]
        public FollowUpResult EstimateMedianFollowUpTime(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description("Confidence level for the limits, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var reversed = e.Select(v => 1 - v).ToArray();
            var steps = SurvivalMath.Steps(t, reversed);
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var sorted = t.OrderBy(v => v).ToArray();

            return new FollowUpResult
            {
                MedianFollowUpReverseKaplanMeier = SurvivalMath.Quantile(steps, 0.5),
                Lower = SurvivalMath.BoundQuantile(steps, 0.5, z, SurvivalIntervalMethod.LogLog, true),
                Upper = SurvivalMath.BoundQuantile(steps, 0.5, z, SurvivalIntervalMethod.LogLog, false),
                MedianObservationTime = CategoricalNumerics.Median(sorted),
                MaximumObservationTime = sorted[^1],
                Subjects = t.Length,
                Events = e.Sum(),
                Censored = t.Length - e.Sum(),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("TestLogRankSurvivalDifference")]
        [Description(
            "Tests whether two or more groups have the same survival curve, using the log-rank test or one of its weighted relatives. " +
            "It compares at every event time the observed number of events in each group with the number expected if the groups shared one hazard. Weighting chooses sensitivity: LogRank (default) is best against a constant hazard ratio, GehanBreslow and TaroneWare emphasise early differences, PetoPeto emphasises early-to-middle differences, and FlemingHarrington with p and q lets you target early (p high) or late (q high) differences. " +
            "Returns per-group observed and expected events, the chi-square with groups minus 1 degrees of freedom and its p-value (small means the curves differ). For two groups and the LogRank weighting it also gives a one-step hazard ratio estimate of the first group versus the second (alphabetical order) with confidence limits. " +
            "Rows with a NaN time or a blank group are dropped; events are 1 for observed and 0 for censored.")]
        public LogRankTestResult TestLogRankSurvivalDifference(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description("Group name for each case in the same order; at least two distinct names must occur.")]
            string[] groups,
            [Description(
                "Weighting scheme: LogRank (default), GehanBreslow, TaroneWare, PetoPeto or FlemingHarrington.")]
            LogRankWeighting weighting = LogRankWeighting.LogRank,
            [Description(
                "Fleming-Harrington p exponent on prior survival, zero or more; used only with FlemingHarrington. Default 0.")]
            double flemingHarringtonP = 0d,
            [Description(
                "Fleming-Harrington q exponent on prior failure probability, zero or more; used only with FlemingHarrington. Default 1.")]
            double flemingHarringtonQ = 1d,
            [Description("Confidence level for the hazard ratio limits, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            SurvivalGuards.CleanGrouped(times, events, groups, out var t, out var e, out var g, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (double.IsNaN(flemingHarringtonP) || double.IsNaN(flemingHarringtonQ) || flemingHarringtonP < 0d ||
                flemingHarringtonQ < 0d)
            {
                throw new ArgumentException("The Fleming-Harrington exponents must be zero or more.",
                    nameof(flemingHarringtonP));
            }

            return SurvivalMath.LogRank(t, e, g, weighting, flemingHarringtonP, flemingHarringtonQ, confidenceLevel,
                skipped);
        }

        private static double[] CheckEvaluationTimes(double[]? evaluationTimes, bool required)
        {
            if (evaluationTimes == null || evaluationTimes.Length == 0)
            {
                if (required)
                {
                    throw new ArgumentException("Give at least one evaluation time.", nameof(evaluationTimes));
                }

                return [];
            }

            foreach (var time in evaluationTimes)
            {
                if (double.IsNaN(time) || double.IsInfinity(time) || time < 0d)
                {
                    throw new ArgumentException("Every evaluation time must be a finite number, zero or more.",
                        nameof(evaluationTimes));
                }
            }

            return evaluationTimes;
        }

        private static SurvivalAtTimePoint PointAt(List<SurvivalStep> steps, double[] times, double time, double z,
            SurvivalIntervalMethod method)
        {
            var step = SurvivalMath.StepAt(steps, time);
            var atRisk = times.Count(v => v >= time);

            if (step == null)
            {
                return new SurvivalAtTimePoint
                {
                    Time = time,
                    AtRisk = atRisk,
                    Survival = 1d,
                    StandardError = 0d,
                    Lower = 1d,
                    Upper = 1d,
                    CumulativeHazard = 0d,
                    CumulativeProbabilityOfEvent = 0d
                };
            }

            var band = SurvivalMath.Interval(step.Survival, step.GreenwoodSum, z, method);

            return new SurvivalAtTimePoint
            {
                Time = time,
                AtRisk = atRisk,
                Survival = step.Survival,
                StandardError = step.Survival * Math.Sqrt(step.GreenwoodSum),
                Lower = band.Lower,
                Upper = band.Upper,
                CumulativeHazard = step.CumulativeHazard,
                CumulativeProbabilityOfEvent = 1d - step.Survival
            };
        }

        private static T[] Thin<T>(T[] rows, int maxRows)
        {
            if (maxRows <= 0 || rows.Length <= maxRows)
            {
                return rows;
            }

            if (maxRows == 1)
            {
                return [rows[0]];
            }

            var picked = new SortedSet<int>();

            for (var k = 0; k < maxRows; k++)
            {
                picked.Add((int)Math.Round(k * (rows.Length - 1d) / (maxRows - 1d)));
            }

            return picked.Select(i => rows[i]).ToArray();
        }
    }
}