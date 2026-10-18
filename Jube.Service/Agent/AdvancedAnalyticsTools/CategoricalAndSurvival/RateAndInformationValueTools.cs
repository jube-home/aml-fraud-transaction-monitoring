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
    public class RateAndInformationValueTools
    {
        private const int MaximumBins = 100;
        private const int ExactBinomialLimit = 400000;

        [AdvancedAnalyticsTool("CalculateInformationValueByCategory")]
        [Description(
            "Scores how well a categorical variable separates events from non-events using weight of evidence and information value, the standard credit-risk and fraud-scorecard screen. " +
            "For each category it returns the counts, the event rate, the share of all events and of all non-events, and WeightOfEvidence, the natural log of the event share over the non-event share (positive means riskier than average, negative safer). " +
            "TotalInformationValue is the sum of (event share minus non-event share) times weight of evidence; rule of thumb: below 0.02 not predictive, 0.02 to 0.1 weak, 0.1 to 0.3 medium, 0.3 to 0.5 strong, above 0.5 suspiciously strong. " +
            "If any category has no events or no non-events, smoothing is added to every cell so the logs stay finite. Rows with a blank category or a NaN outcome are skipped and counted.")]
        public InformationValueResult CalculateInformationValueByCategory(
            [Description("Category of each case; blank or missing entries are skipped.")]
            string[] categories,
            [Description("Outcome for each case in the same order: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description(
                "Amount added to every category's event and non-event count when any category has a zero cell. Default 0.5; use 0 to disable, which leaves the weight of evidence of a zero cell as NaN.")]
            double smoothing = 0.5)
        {
            CategoricalGuards.Required(categories, nameof(categories));
            CategoricalGuards.BinaryLabels(labels, nameof(labels));
            CategoricalGuards.SameLength(categories.Length, labels.Length, nameof(categories), nameof(labels));

            if (double.IsNaN(smoothing) || smoothing < 0d)
            {
                throw new ArgumentException("smoothing must be zero or a positive number.", nameof(smoothing));
            }

            var groups = new SortedDictionary<string, (int Events, int NonEvents)>(StringComparer.Ordinal);
            var skipped = 0;

            for (var i = 0; i < categories.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(categories[i]))
                {
                    skipped++;
                    continue;
                }

                groups.TryGetValue(categories[i], out var tally);

                groups[categories[i]] =
                    labels[i] == 1 ? (tally.Events + 1, tally.NonEvents) : (tally.Events, tally.NonEvents + 1);
            }

            if (groups.Count < 2)
            {
                throw new ArgumentException(
                    "At least two distinct categories are needed to measure separation; the variable is constant after removing blanks.",
                    nameof(categories));
            }

            var keys = groups.Keys.ToArray();
            var events = keys.Select(k => groups[k].Events).ToArray();
            var nonEvents = keys.Select(k => groups[k].NonEvents).ToArray();
            var nan = keys.Select(_ => double.NaN).ToArray();

            return BinningMath.InformationValue(keys, nan, nan, events, nonEvents, smoothing, skipped);
        }

        [AdvancedAnalyticsTool("CalculateInformationValueByBins")]
        [Description(
            "Bins a numeric variable and scores how well the bins separate events from non-events using weight of evidence and information value. " +
            "Bins are equal-frequency (quantile, default) or equal-width; values that fall into no bin because they are NaN form their own Missing bin so missingness itself can be informative. " +
            "Each bin reports its bounds (NaN for an open end or the Missing bin; the Label always shows the range), counts, event rate, weight of evidence (natural log of event share over non-event share, positive means riskier) and IV contribution; TotalInformationValue below 0.02 is not predictive, up to 0.1 weak, 0.3 medium, 0.5 strong, above suspicious. " +
            "WeightOfEvidenceIsMonotonic tells whether risk rises or falls steadily across the bins, which scorecards usually require.")]
        public InformationValueResult CalculateInformationValueByBins(
            [Description("Numeric value of the variable for each case; NaN marks a missing value.")]
            double[] values,
            [Description("Outcome for each case in the same order: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description(
                "Requested number of bins, from 2 to 100. Default 10; fewer are used when ties collapse bins.")]
            int bins = 10,
            [Description(
                "EqualFrequency (default) gives similar counts per bin; EqualWidth gives bins of equal value range.")]
            CategoricalBinningMethod method = CategoricalBinningMethod.EqualFrequency,
            [Description(
                "Amount added to every bin's counts when any bin has a zero cell. Default 0.5; 0 disables smoothing.")]
            double smoothing = 0.5)
        {
            CategoricalGuards.Required(values, nameof(values));
            CategoricalGuards.BinaryLabels(labels, nameof(labels));
            CategoricalGuards.SameLength(values.Length, labels.Length, nameof(values), nameof(labels));

            if (bins < 2 || bins > MaximumBins)
            {
                throw new ArgumentException($"bins must be between 2 and {MaximumBins}.", nameof(bins));
            }

            if (double.IsNaN(smoothing) || smoothing < 0d)
            {
                throw new ArgumentException("smoothing must be zero or a positive number.", nameof(smoothing));
            }

            var finite = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray();

            if (finite.Length == 0)
            {
                throw new ArgumentException("There are no finite values to bin.", nameof(values));
            }

            var cuts = BinningMath.CutPoints(finite, bins, method);
            var binCount = cuts.Length + 1;
            var events = new int[binCount + 1];
            var nonEvents = new int[binCount + 1];

            for (var i = 0; i < values.Length; i++)
            {
                var slot = double.IsNaN(values[i]) || double.IsInfinity(values[i])
                    ? binCount
                    : BinningMath.Assign(values[i], cuts);

                if (labels[i] == 1)
                {
                    events[slot]++;
                }
                else
                {
                    nonEvents[slot]++;
                }
            }

            var labelList = new List<string>();
            var lower = new List<double>();
            var upper = new List<double>();
            var eventList = new List<int>();
            var nonEventList = new List<int>();

            for (var b = 0; b < binCount; b++)
            {
                labelList.Add(BinningMath.Label(b, cuts));
                lower.Add(b == 0 ? double.NaN : cuts[b - 1]);
                upper.Add(b == cuts.Length ? double.NaN : cuts[b]);
                eventList.Add(events[b]);
                nonEventList.Add(nonEvents[b]);
            }

            if (events[binCount] + nonEvents[binCount] > 0)
            {
                labelList.Add("Missing");
                lower.Add(double.NaN);
                upper.Add(double.NaN);
                eventList.Add(events[binCount]);
                nonEventList.Add(nonEvents[binCount]);
            }

            return BinningMath.InformationValue(labelList.ToArray(), lower.ToArray(), upper.ToArray(),
                eventList.ToArray(), nonEventList.ToArray(), smoothing, 0);
        }

        [AdvancedAnalyticsTool("CalculatePopulationStabilityIndexFromCounts")]
        [Description(
            "Computes the Population Stability Index between a baseline (expected) distribution and a current (actual) distribution from counts per bin or category. " +
            "PSI is the sum over bins of (actual share minus expected share) times the log of their ratio: below 0.1 means stable, 0.1 to 0.25 a moderate shift worth investigating, above 0.25 a significant shift. " +
            "Empty bins are floored at a tiny share so the logarithm is finite. Each bin's contribution shows where the shift is. Use it to monitor score, feature or segment-mix drift.")]
        public PopulationStabilityResult CalculatePopulationStabilityIndexFromCounts(
            [Description("Baseline counts per bin or category; not negative, at least one above zero.")]
            double[] baselineCounts,
            [Description("Current counts per bin or category, same order and length as baselineCounts.")]
            double[] currentCounts,
            [Description("Optional names of the bins in order; leave empty to label them Bin1, Bin2 and so on.")]
            string[]? labels = null,
            [Description("Smallest share used for an empty bin. Default 0.0001; must be above 0 and below 0.5.")]
            double shareFloor = 0.0001)
        {
            CategoricalGuards.Required(baselineCounts, nameof(baselineCounts));
            CategoricalGuards.Required(currentCounts, nameof(currentCounts));

            CategoricalGuards.SameLength(baselineCounts.Length, currentCounts.Length, nameof(baselineCounts),
                nameof(currentCounts));

            if (baselineCounts.Length < 2)
            {
                throw new ArgumentException("At least two bins are needed.", nameof(baselineCounts));
            }

            foreach (var v in baselineCounts.Concat(currentCounts))
            {
                CategoricalGuards.NonNegativeCount(v, nameof(baselineCounts));
            }

            CheckFloor(shareFloor);

            return BinningMath.Stability(
                CategoricalGuards.Names(labels, baselineCounts.Length, "Bin", nameof(labels)), baselineCounts,
                currentCounts, shareFloor);
        }

        [AdvancedAnalyticsTool("CalculatePopulationStabilityIndexForNumericValues")]
        [Description(
            "Computes the Population Stability Index of a numeric variable or model score between a baseline sample and a current sample. " +
            "Bin edges are learned from the baseline only (equal-frequency by default), current values below or above the baseline range fall in the first or last bin, and NaN values form a Missing bin. " +
            "PSI below 0.1 is stable, 0.1 to 0.25 a moderate shift, above 0.25 a significant shift; per-bin shares and contributions show where the distribution moved.")]
        public PopulationStabilityResult CalculatePopulationStabilityIndexForNumericValues(
            [Description("Baseline (expected, training-time) values; NaN marks a missing value.")]
            double[] baselineValues,
            [Description("Current (actual, recent) values; NaN marks a missing value.")]
            double[] currentValues,
            [Description("Requested number of bins from 2 to 100. Default 10.")]
            int bins = 10,
            [Description("EqualFrequency (default) or EqualWidth bins, defined on the baseline.")]
            CategoricalBinningMethod method = CategoricalBinningMethod.EqualFrequency,
            [Description("Smallest share used for an empty bin. Default 0.0001; must be above 0 and below 0.5.")]
            double shareFloor = 0.0001)
        {
            CategoricalGuards.Required(baselineValues, nameof(baselineValues));
            CategoricalGuards.Required(currentValues, nameof(currentValues));

            if (bins < 2 || bins > MaximumBins)
            {
                throw new ArgumentException($"bins must be between 2 and {MaximumBins}.", nameof(bins));
            }

            CheckFloor(shareFloor);
            var finite = baselineValues.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray();

            if (finite.Length == 0)
            {
                throw new ArgumentException("The baseline has no finite values to define bins.",
                    nameof(baselineValues));
            }

            var cuts = BinningMath.CutPoints(finite, bins, method);
            var binCount = cuts.Length + 1;
            var baseline = new double[binCount + 1];
            var current = new double[binCount + 1];
            Tally(baselineValues, cuts, baseline, binCount);
            Tally(currentValues, cuts, current, binCount);
            var labels = Enumerable.Range(0, binCount).Select(b => BinningMath.Label(b, cuts)).ToList();
            var baselineList = baseline.Take(binCount).ToList();
            var currentList = current.Take(binCount).ToList();

            if (baseline[binCount] + current[binCount] > 0)
            {
                labels.Add("Missing");
                baselineList.Add(baseline[binCount]);
                currentList.Add(current[binCount]);
            }

            return BinningMath.Stability(labels.ToArray(), baselineList.ToArray(), currentList.ToArray(), shareFloor);
        }

        [AdvancedAnalyticsTool("CalculatePopulationStabilityIndexForCategories")]
        [Description(
            "Computes the Population Stability Index of a categorical variable between a baseline sample and a current sample of labels. " +
            "Categories seen in either sample form the bins, so a category that appears or vanishes shows up as a large contribution. " +
            "PSI below 0.1 is stable, 0.1 to 0.25 a moderate shift, above 0.25 a significant shift. Blank labels are skipped.")]
        public PopulationStabilityResult CalculatePopulationStabilityIndexForCategories(
            [Description("Baseline category label for each case; blank entries are skipped.")]
            string[] baselineCategories,
            [Description("Current category label for each case; blank entries are skipped.")]
            string[] currentCategories,
            [Description("Smallest share used for an empty category. Default 0.0001; must be above 0 and below 0.5.")]
            double shareFloor = 0.0001)
        {
            CategoricalGuards.Required(baselineCategories, nameof(baselineCategories));
            CategoricalGuards.Required(currentCategories, nameof(currentCategories));
            CheckFloor(shareFloor);

            var baselineTally = baselineCategories.Where(c => !string.IsNullOrWhiteSpace(c)).GroupBy(c => c)
                .ToDictionary(g => g.Key, g => (double)g.Count());
            var currentTally = currentCategories.Where(c => !string.IsNullOrWhiteSpace(c)).GroupBy(c => c)
                .ToDictionary(g => g.Key, g => (double)g.Count());

            var keys = baselineTally.Keys.Union(currentTally.Keys).OrderBy(k => k, StringComparer.Ordinal).ToArray();

            if (keys.Length < 1)
            {
                throw new ArgumentException("There are no non-blank categories in either sample.",
                    nameof(baselineCategories));
            }

            return BinningMath.Stability(keys, keys.Select(k => baselineTally.GetValueOrDefault(k)).ToArray(),
                keys.Select(k => currentTally.GetValueOrDefault(k)).ToArray(), shareFloor);
        }

        [AdvancedAnalyticsTool("TabulateEventRateBySegment")]
        [Description(
            "Tabulates a binary event rate (alert rate, fraud rate, default rate) for every segment of a categorical variable, ranked from highest to lowest rate. " +
            "Each segment reports its count, events, event rate with a confidence interval, share of rows and of events, lift against the overall rate (2 means twice the average), and a z test of the segment against all other rows. " +
            "The homogeneity chi-square tests whether the rate differs across segments at all. SmallSample is true when the segment is below your minimum count and its interval should be trusted little. Blank segments are skipped and counted.")]
        public SegmentEventRateResult TabulateEventRateBySegment(
            [Description(
                "Segment name for each case, for example channel, country or product; blank entries are skipped.")]
            string[] segments,
            [Description("Event flag for each case in the same order: 1 for the event, 0 otherwise.")]
            int[] events,
            [Description("Confidence level for the rate intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Interval method: Wilson (default), ClopperPearson, AgrestiCoull, Jeffreys or Wald.")]
            ProportionIntervalMethod method = ProportionIntervalMethod.Wilson,
            [Description("Segments with fewer cases than this are flagged SmallSample. Default 30.")]
            int minimumCount = 30)
        {
            CategoricalGuards.Required(segments, nameof(segments));
            CategoricalGuards.BinaryLabels(events, nameof(events));
            CategoricalGuards.SameLength(segments.Length, events.Length, nameof(segments), nameof(events));
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var tally = new Dictionary<string, (int Count, int Events)>(StringComparer.Ordinal);
            var skipped = 0;

            for (var i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    skipped++;
                    continue;
                }

                tally.TryGetValue(segments[i], out var t);
                tally[segments[i]] = (t.Count + 1, t.Events + events[i]);
            }

            if (tally.Count == 0)
            {
                throw new ArgumentException("There are no cases with a non-blank segment.", nameof(segments));
            }

            var total = tally.Values.Sum(t => t.Count);
            var totalEvents = tally.Values.Sum(t => t.Events);
            var overall = totalEvents / (double)total;
            var overallInterval = CategoricalIntervals.Proportion(totalEvents, total, confidenceLevel, method);
            var rows = new List<SegmentEventRateRow>();

            foreach (var pair in tally)
            {
                var interval = CategoricalIntervals.Proportion(pair.Value.Events, pair.Value.Count, confidenceLevel,
                    method);

                var restCount = total - pair.Value.Count;
                var restEvents = totalEvents - pair.Value.Events;
                var z = double.NaN;
                var p = double.NaN;

                if (restCount > 0)
                {
                    var pooled = totalEvents / (double)total;
                    var se = Math.Sqrt(pooled * (1d - pooled) * (1d / pair.Value.Count + 1d / restCount));

                    if (se > 0d)
                    {
                        z = (interval.Estimate - restEvents / (double)restCount) / se;
                        p = 2d * CategoricalLibrary.NormalSurvival(Math.Abs(z));
                    }
                }

                rows.Add(new SegmentEventRateRow
                {
                    Segment = pair.Key,
                    Count = pair.Value.Count,
                    Events = pair.Value.Events,
                    EventRate = interval.Estimate,
                    Lower = interval.Lower,
                    Upper = interval.Upper,
                    ShareOfRows = pair.Value.Count / (double)total,
                    ShareOfEvents = totalEvents > 0 ? pair.Value.Events / (double)totalEvents : double.NaN,
                    LiftVersusOverall = overall > 0d ? interval.Estimate / overall : double.NaN,
                    ZVersusRest = z,
                    PValueVersusRest = p,
                    SmallSample = pair.Value.Count < minimumCount
                });
            }

            var ordered = rows.OrderByDescending(r => r.EventRate).ThenBy(r => r.Segment, StringComparer.Ordinal)
                .ToArray();

            var chi = double.NaN;
            var df = 0;
            var pValue = double.NaN;

            if (tally.Count >= 2 && totalEvents > 0 && totalEvents < total)
            {
                var table = ordered.Select(r => new[] { (double)r.Events, r.Count - r.Events }).ToArray();

                var analysis = ContingencyMath.Analyse(table, ordered.Select(r => r.Segment).ToArray(),
                    ["Event", "NonEvent"]);

                chi = analysis.ChiSquare;
                df = analysis.DegreesOfFreedom;
                pValue = analysis.PValue;
            }

            return new SegmentEventRateResult
            {
                Segments = ordered,
                TotalRows = total,
                TotalEvents = totalEvents,
                OverallEventRate = overall,
                OverallLower = overallInterval.Lower,
                OverallUpper = overallInterval.Upper,
                HomogeneityChiSquare = chi,
                HomogeneityDegreesOfFreedom = df,
                HomogeneityPValue = pValue,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("TabulateAlertHitRatesBySegment")]
        [Description(
            "Summarises how an alerting rule or model performs in each segment from one alert flag and one confirmed-event flag per case. " +
            "For every segment and overall it returns the alert rate (alerts over cases), the hit rate (confirmed events among alerts, also called precision), the detection rate (confirmed events that were alerted, also called recall), false positives per true hit, the number of missed events, and Wilson intervals for the alert and hit rates. " +
            "Use it to find segments where alerts are mostly noise (low hit rate) or where events slip through (low detection rate). Blank segments are skipped and counted.")]
        public AlertHitRateResult TabulateAlertHitRatesBySegment(
            [Description("Segment name for each case, for example channel or country; blank entries are skipped.")]
            string[] segments,
            [Description("Alert flag for each case: 1 if the rule or model raised an alert, 0 otherwise.")]
            int[] alerted,
            [Description(
                "Ground truth for each case in the same order: 1 if the case was truly an event (for example confirmed fraud), 0 otherwise.")]
            int[] actual,
            [Description("Confidence level for the Wilson intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Required(segments, nameof(segments));
            CategoricalGuards.BinaryLabels(alerted, nameof(alerted));
            CategoricalGuards.BinaryLabels(actual, nameof(actual));
            CategoricalGuards.SameLength(segments.Length, alerted.Length, nameof(segments), nameof(alerted));
            CategoricalGuards.SameLength(segments.Length, actual.Length, nameof(segments), nameof(actual));
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var tally = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
            var skipped = 0;

            for (var i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    skipped++;
                    continue;
                }

                if (!tally.TryGetValue(segments[i], out var t))
                {
                    t = new int[4];
                    tally[segments[i]] = t;
                }

                t[0]++;
                t[1] += alerted[i];
                t[2] += alerted[i] == 1 && actual[i] == 1 ? 1 : 0;
                t[3] += actual[i];
            }

            if (tally.Count == 0)
            {
                throw new ArgumentException("There are no cases with a non-blank segment.", nameof(segments));
            }

            var rows = tally.Select(p => AlertRow(p.Key, p.Value, confidenceLevel)).ToArray();
            var overallTally = new int[4];

            foreach (var t in tally.Values)
            {
                for (var k = 0; k < 4; k++)
                {
                    overallTally[k] += t[k];
                }
            }

            return new AlertHitRateResult
            {
                Segments = rows,
                Overall = AlertRow("Overall", overallTally, confidenceLevel),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculatePoissonRateConfidenceInterval")]
        [Description(
            "Computes an exact (Garwood) confidence interval for a Poisson event rate such as alerts per thousand accounts or cases per day, from an event count and the exposure it was observed over. " +
            "The rate is events divided by exposure in whatever unit the exposure uses (person-days, transactions, thousand accounts); the interval is in the same unit. " +
            "Zero events still give a finite upper limit, which is the rate you cannot rule out. Use it for rare-event rates where a normal approximation is unreliable.")]
        public CategoricalEstimate CalculatePoissonRateConfidenceInterval(
            [Description("Observed number of events, a whole number of at least 0.")]
            double events,
            [Description(
                "Exposure the events were observed over, for example person-time or number of accounts; must be above 0.")]
            double exposure,
            [Description("Confidence level, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CheckWholeCount(events, nameof(events));
            CategoricalGuards.PositiveFinite(exposure, nameof(exposure));

            return CategoricalIntervals.PoissonRate(events, exposure, confidenceLevel);
        }

        [AdvancedAnalyticsTool("ComparePoissonRates")]
        [Description(
            "Compares two Poisson event rates, for example the alert rate before and after a rule change, from each period's event count and exposure. " +
            "Returns both rates, the rate ratio (above 1 means group 1 is higher) with an exact confidence interval, the rate difference with a normal-approximation interval, and a two-sided p-value from the exact conditional binomial test (a normal approximation is used and reported for very large counts). " +
            "A rate-ratio interval that includes 1, or a large p-value, means the difference could be chance. When group 2 has no events the rate ratio and its upper limit are NaN because the ratio is unbounded; when group 1 has no events the lower limit is 0.")]
        public PoissonRateComparisonResult ComparePoissonRates(
            [Description("Events observed in group 1, a whole number of at least 0.")]
            double events1,
            [Description("Exposure of group 1, for example person-time or number of accounts; must be above 0.")]
            double exposure1,
            [Description("Events observed in group 2, a whole number of at least 0.")]
            double events2,
            [Description("Exposure of group 2 in the same unit as exposure1; must be above 0.")]
            double exposure2,
            [Description("Confidence level, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CheckWholeCount(events1, nameof(events1));
            CheckWholeCount(events2, nameof(events2));
            CategoricalGuards.PositiveFinite(exposure1, nameof(exposure1));
            CategoricalGuards.PositiveFinite(exposure2, nameof(exposure2));
            var n = events1 + events2;
            var rate1 = events1 / exposure1;
            var rate2 = events2 / exposure2;
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var differenceSe = Math.Sqrt(events1 / (exposure1 * exposure1) + events2 / (exposure2 * exposure2));
            var lower = double.NaN;
            var upper = double.NaN;
            var p = double.NaN;
            var method = "no events in either group";

            if (n > 0d)
            {
                var proportion = exposure1 / (exposure1 + exposure2);

                var interval = CategoricalIntervals.Proportion(events1, n, confidenceLevel,
                    ProportionIntervalMethod.ClopperPearson);

                var scale = exposure2 / exposure1;
                lower = interval.Lower <= 0d ? 0d : interval.Lower / (1d - interval.Lower) * scale;
                upper = interval.Upper >= 1d ? double.NaN : interval.Upper / (1d - interval.Upper) * scale;

                if (n <= ExactBinomialLimit)
                {
                    var masses = CategoricalLibrary.BinomialMassFunction((int)n, proportion);
                    var observed = masses[(int)events1];
                    var threshold = observed * (1d + 1e-7);
                    p = 0d;

                    foreach (var mass in masses)
                    {
                        if (mass <= threshold)
                        {
                            p += mass;
                        }
                    }

                    p = Math.Min(1d, p);
                    method = "exact conditional binomial";
                }
                else
                {
                    var sd = Math.Sqrt(n * proportion * (1d - proportion));
                    p = 2d * CategoricalLibrary.NormalSurvival(Math.Abs(events1 - n * proportion) / sd);
                    method = "normal approximation to the conditional binomial";
                }
            }

            return new PoissonRateComparisonResult
            {
                Rate1 = rate1,
                Rate2 = rate2,
                RateRatio = rate2 > 0d ? rate1 / rate2 : double.NaN,
                RateRatioLower = lower,
                RateRatioUpper = upper,
                RateDifference = rate1 - rate2,
                RateDifferenceLower = rate1 - rate2 - z * differenceSe,
                RateDifferenceUpper = rate1 - rate2 + z * differenceSe,
                PValue = p,
                PValueMethod = method
            };
        }

        [AdvancedAnalyticsTool("TestPoissonDispersion")]
        [Description(
            "Tests whether a series of event counts (for example alerts per day) behaves like a Poisson process, using the index of dispersion: the sum of squared deviations over the mean is chi-square distributed with n minus 1 degrees of freedom under the Poisson hypothesis. " +
            "VarianceToMeanRatio near 1 is Poisson; well above 1 (small PValueOverdispersion) means bursty or clustered counts, which a plain Poisson model or rate interval will understate; below 1 means unusually regular counts. " +
            "A series with mean zero or fewer than two observations cannot be tested and gives NaN.")]
        public PoissonDispersionResult TestPoissonDispersion(
            [Description("Event count for each period, each a whole number of at least 0; at least two periods.")]
            double[] counts)
        {
            CategoricalGuards.Required(counts, nameof(counts));

            if (counts.Length < 2)
            {
                throw new ArgumentException("At least two count observations are needed to test dispersion.",
                    nameof(counts));
            }

            foreach (var c in counts)
            {
                CheckWholeCount(c, nameof(counts));
            }

            var n = counts.Length;
            var mean = counts.Average();
            var sumSquares = counts.Sum(c => (c - mean) * (c - mean));
            var variance = sumSquares / (n - 1);

            if (mean <= 0d)
            {
                return new PoissonDispersionResult
                {
                    Observations = n,
                    Mean = mean,
                    Variance = variance,
                    VarianceToMeanRatio = double.NaN,
                    ChiSquare = double.NaN,
                    DegreesOfFreedom = n - 1,
                    PValueOverdispersion = double.NaN,
                    PValueUnderdispersion = double.NaN,
                    PValueTwoSided = double.NaN,
                    Interpretation = "all counts are zero; dispersion cannot be assessed"
                };
            }

            var chi = sumSquares / mean;
            var upperTail = CategoricalLibrary.ChiSquareSurvival(chi, n - 1);
            var lowerTail = 1d - upperTail;

            return new PoissonDispersionResult
            {
                Observations = n,
                Mean = mean,
                Variance = variance,
                VarianceToMeanRatio = variance / mean,
                ChiSquare = chi,
                DegreesOfFreedom = n - 1,
                PValueOverdispersion = upperTail,
                PValueUnderdispersion = lowerTail,
                PValueTwoSided = Math.Min(1d, 2d * Math.Min(upperTail, lowerTail)),
                Interpretation = upperTail < 0.05
                    ? "overdispersed: counts vary more than a Poisson process would allow"
                    : lowerTail < 0.05
                        ? "underdispersed: counts are more regular than a Poisson process"
                        : "consistent with a Poisson process"
            };
        }

        private static AlertHitRateRow AlertRow(string segment, int[] t, double level)
        {
            var alertInterval = CategoricalIntervals.Proportion(t[1], t[0], level, ProportionIntervalMethod.Wilson);
            var hitInterval = CategoricalIntervals.Proportion(t[2], t[1], level, ProportionIntervalMethod.Wilson);

            return new AlertHitRateRow
            {
                Segment = segment,
                Cases = t[0],
                Alerts = t[1],
                AlertRate = alertInterval.Estimate,
                AlertRateLower = alertInterval.Lower,
                AlertRateUpper = alertInterval.Upper,
                TrueHits = t[2],
                HitRate = hitInterval.Estimate,
                HitRateLower = hitInterval.Lower,
                HitRateUpper = hitInterval.Upper,
                ActualEvents = t[3],
                DetectionRate = t[3] > 0 ? t[2] / (double)t[3] : double.NaN,
                FalsePositivesPerTrueHit = t[2] > 0 ? (t[1] - t[2]) / (double)t[2] : double.NaN,
                MissedEvents = t[3] - t[2]
            };
        }

        private static void Tally(double[] values, double[] cuts, double[] destination, int missingSlot)
        {
            foreach (var value in values)
            {
                var slot = double.IsNaN(value) || double.IsInfinity(value)
                    ? missingSlot
                    : BinningMath.Assign(value, cuts);

                destination[slot]++;
            }
        }

        private static void CheckFloor(double floor)
        {
            if (double.IsNaN(floor) || floor <= 0d || floor >= 0.5)
            {
                throw new ArgumentException("shareFloor must be above 0 and below 0.5.", nameof(floor));
            }
        }

        private static void CheckWholeCount(double value, string parameterName)
        {
            CategoricalGuards.NonNegativeCount(value, parameterName);

            if (Math.Abs(value - Math.Round(value)) > 1e-9 || value > int.MaxValue / 2.0)
            {
                throw new ArgumentException($"{parameterName} must be a whole number of events that is not too large.",
                    parameterName);
            }
        }
    }
}