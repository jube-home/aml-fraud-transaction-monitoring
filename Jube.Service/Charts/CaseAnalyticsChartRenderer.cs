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

using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.CaseRisk;
using Jube.CaseRisk.Analytics;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSearch;
using Jube.Resources;
using Jube.Service.Query.CaseAnalytics;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public static class CaseAnalyticsChartRenderer
    {
        private static string[] Weekdays(IStringLocalizer? strings) =>
        [
            T(strings, CaseAnalyticsChartResources.WeekdayMon, "Mon"),
            T(strings, CaseAnalyticsChartResources.WeekdayTue, "Tue"),
            T(strings, CaseAnalyticsChartResources.WeekdayWed, "Wed"),
            T(strings, CaseAnalyticsChartResources.WeekdayThu, "Thu"),
            T(strings, CaseAnalyticsChartResources.WeekdayFri, "Fri"),
            T(strings, CaseAnalyticsChartResources.WeekdaySat, "Sat"),
            T(strings, CaseAnalyticsChartResources.WeekdaySun, "Sun")
        ];

        private static string T(IStringLocalizer? strings, string key, string english, params object[] arguments) =>
            ChartText.Of(strings, key, english, arguments);

        public static string Render(CaseAnalyticsChart kind, CaseAnalyticsDto report, bool dark,
            IStringLocalizer? strings = null)
        {
            ArgumentNullException.ThrowIfNull(report);

            var theme = dark ? ThemeMode.Dark : ThemeMode.Light;

            return kind switch
            {
                CaseAnalyticsChart.Flow => Flow(report, theme, strings),
                CaseAnalyticsChart.FirstView => Histogram(T(strings, CaseAnalyticsChartResources.FirstViewTitle, "Time to first look"),
                    T(strings, CaseAnalyticsChartResources.FirstViewSubtitle, "Cases by how long they waited"),
                    report.CycleTimes.FirstViewHistogram, theme, strings),
                CaseAnalyticsChart.Resolution => Histogram(T(strings, CaseAnalyticsChartResources.ResolutionTitle, "Time to resolution"),
                    T(strings, CaseAnalyticsChartResources.ResolutionSubtitle, "Closed cases by how long they took"),
                    report.CycleTimes.ResolutionHistogram, theme, strings),
                CaseAnalyticsChart.Aging => Aging(report, theme, strings),
                CaseAnalyticsChart.StatusDwell => StatusDwell(report, theme, strings),
                CaseAnalyticsChart.RuleEffort => RuleEffort(report, theme, strings),
                CaseAnalyticsChart.RulePrecision => RulePrecision(report, theme, strings),
                CaseAnalyticsChart.Gains => Gains(report, theme, strings),
                CaseAnalyticsChart.Calibration => Calibration(report, theme, strings),
                CaseAnalyticsChart.Arrivals => Arrivals(report, theme, strings),
                CaseAnalyticsChart.FirstViewByHour => FirstViewByHour(report, theme, strings),
                CaseAnalyticsChart.ServiceLevelWeekly => ServiceLevelWeekly(report, theme, strings),
                CaseAnalyticsChart.Handoffs => Handoffs(report, theme, strings),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "That chart is not available.")
            };
        }

        public static string RiskMatrix(CaseRiskMatrixDto matrix, bool dark, IStringLocalizer? strings = null)
        {
            ArgumentNullException.ThrowIfNull(matrix);

            if (matrix.Total == 0)
            {
                throw new ArgumentException("There are no cases to chart.");
            }

            string[] impacts =
            [
                T(strings, CaseAnalyticsChartResources.ImpactMinimal, "Minimal"), T(strings, CaseAnalyticsChartResources.ImpactLow, "Low"), T(strings, CaseAnalyticsChartResources.ImpactModerate, "Moderate"),
                T(strings, CaseAnalyticsChartResources.ImpactHigh, "High"), T(strings, CaseAnalyticsChartResources.ImpactSevere, "Severe")
            ];
            string[] likelihoods =
            [
                T(strings, CaseAnalyticsChartResources.LikelihoodVeryLikely, "Very likely"), T(strings, CaseAnalyticsChartResources.LikelihoodLikely, "Likely"),
                T(strings, CaseAnalyticsChartResources.LikelihoodPossible, "Possible"), T(strings, CaseAnalyticsChartResources.LikelihoodUnlikely, "Unlikely")
            ];

            var values = new double[likelihoods.Length, impacts.Length];

            foreach (var cell in matrix.Cells.Where(c => c is { LikelihoodBand: >= 1 and <= 4, Impact: >= 1 and <= 5 }))
            {
                values[4 - cell.LikelihoodBand, cell.Impact - 1] = cell.Cases;
            }

            var unassessed = matrix.Cells.Where(c => c.LikelihoodBand == 0).Sum(c => c.Cases);

            return Chart.Heatmap()
                .Title(T(strings, CaseAnalyticsChartResources.RiskMatrixTitle, "Risk matrix"))
                .Subtitle(unassessed > 0
                    ? T(strings, CaseAnalyticsChartResources.RiskMatrixAssessed, "{0} assessed cases; {1} not yet assessed", matrix.Total - unassessed, unassessed)
                    : T(strings, CaseAnalyticsChartResources.RiskMatrixCases, "{0} cases", matrix.Total))
                .XAxisTitle(T(strings, CaseAnalyticsChartResources.Impact, "Impact"))
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Likelihood, "Likelihood"))
                .Size(560, 340)
                .Theme(dark ? ThemeMode.Dark : ThemeMode.Light)
                .Rows(likelihoods)
                .Columns(impacts)
                .Values(values)
                .ShowValues()
                .Decimals(0)
                .LegendTitle(T(strings, CaseAnalyticsChartResources.Cases, "Cases"))
                .Render();
        }

        private static void Require(bool any, string what)
        {
            if (!any)
            {
                throw new ArgumentException("There is no " + what + " to chart.");
            }
        }

        private static string Flow(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            Require(report.Flow.Count > 0, "flow");

            return Chart.Combo()
                .Title(T(strings, CaseAnalyticsChartResources.FlowTitle, "Opened, closed and backlog"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.FlowSubtitle, "Cases opened and closed each day, and the cases still open at the end of it"))
                .Size(760, 340)
                .Theme(theme)
                .Categories(report.Flow.Select(d => d.Date.ToString("d MMM")))
                .Columns(T(strings, CaseAnalyticsChartResources.Opened, "Opened"), report.Flow.Select(d => (double)d.Opened))
                .Columns(T(strings, CaseAnalyticsChartResources.Closed, "Closed"), report.Flow.Select(d => (double)d.Closed))
                .Line(T(strings, CaseAnalyticsChartResources.Backlog, "Backlog"), report.Flow.Select(d => (double)d.Backlog))
                .Render();
        }

        private static string Histogram(string title, string subtitle, List<HistogramBucket> buckets, ThemeMode theme,
            IStringLocalizer? strings)
        {
            Require(buckets.Sum(b => b.Count) > 0, "timing");

            return Chart.Column()
                .Title(title)
                .Subtitle(subtitle)
                .Size(680, 320)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Cases, "Cases"))
                .Categories(buckets.Select(b => b.Label))
                .Series(T(strings, CaseAnalyticsChartResources.Cases, "Cases"), buckets.Select(b => (double)b.Count))
                .ShowValues()
                .Render();
        }

        private static string Aging(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            Require(report.Aging.Open > 0, "open case");

            string[] lanes =
            [
                T(strings, CaseAnalyticsChartResources.LaneLiveMoney, "Live money"), T(strings, CaseAnalyticsChartResources.LaneMandatory, "Mandatory"),
                T(strings, CaseAnalyticsChartResources.LaneDeadlineNear, "Deadline near"), T(strings, CaseAnalyticsChartResources.LaneStandard, "Standard")
            ];

            var chart = Chart.Column()
                .Title(T(strings, CaseAnalyticsChartResources.AgingTitle, "Open cases by age"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.AgingSubtitle, "How long the open cases have waited, by urgency lane"))
                .Size(720, 340)
                .Theme(theme)
                .Stacked()
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Cases, "Cases"))
                .Categories(report.Aging.Buckets.Select(b => b.Label));

            for (var lane = 1; lane <= 4; lane++)
            {
                var current = lane;

                chart.Series(lanes[lane - 1], report.Aging.Buckets.Select(b =>
                    b.ByLane.TryGetValue(current, out var count) ? (double)count : 0));
            }

            return chart.Render();
        }

        private static string StatusDwell(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            var rows = report.StatusDwell.Where(d => d.Completed.Count + d.Current.Count > 0).Take(15).ToList();
            Require(rows.Count > 0, "status stay");

            string Name(Guid guid) => report.StatusNames.GetValueOrDefault(guid, T(strings, CaseAnalyticsChartResources.Unknown, "Unknown"));

            return Chart.Bar()
                .Title(T(strings, CaseAnalyticsChartResources.StatusDwellTitle, "Time in each status"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.StatusDwellSubtitle, "Median and slowest tenth of completed stays, in hours"))
                .Size(720, 120 + rows.Count * 36)
                .Theme(theme)
                .Horizontal()
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Hours, "Hours"))
                .ValueFormat(v => v.ToString("0.#"))
                .Categories(rows.Select(r => Name(r.CaseWorkflowStatusGuid)))
                .Series(T(strings, CaseAnalyticsChartResources.Median, "Median"), rows.Select(r => r.Completed.P50 / 60))
                .Series(T(strings, CaseAnalyticsChartResources.SlowestTenth, "Slowest tenth"), rows.Select(r => r.Completed.P90 / 60))
                .Render();
        }

        private static string RuleEffort(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            var rows = report.Rules.Where(r => r.WastedMinutes > 0).Take(12).ToList();
            Require(rows.Count > 0, "false positive effort");

            return Chart.Bar()
                .Title(T(strings, CaseAnalyticsChartResources.RuleEffortTitle, "Analyst time lost to false positives"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.RuleEffortSubtitle, "Hours spent on cases the rule raised that turned out not to be real"))
                .Size(720, 120 + rows.Count * 36)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Hours, "Hours"))
                .ValueFormat(v => v.ToString("0.#"))
                .Categories(rows.Select(r => r.Name))
                .Series(T(strings, CaseAnalyticsChartResources.Hours, "Hours"), rows.Select(r => r.WastedMinutes / 60))
                .Render();
        }

        private static string RulePrecision(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            var rows = report.Rules.Where(r => r.Precision.HasValue).Take(12).ToList();
            Require(rows.Count > 0, "decided rule");

            return Chart.Bar()
                .Title(T(strings, CaseAnalyticsChartResources.RulePrecisionTitle, "Rule precision"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.RulePrecisionSubtitle, "Share of decided cases the rule raised that were confirmed"))
                .Size(720, 120 + rows.Count * 36)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Percent, "Percent"))
                .ValueFormat(v => v.ToString("0") + "%")
                .Categories(rows.Select(r => r.Name))
                .Series(T(strings, CaseAnalyticsChartResources.Precision, "Precision"), rows.Select(r => r.Precision!.Value * 100))
                .Render();
        }

        private static string Gains(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            var ranking = report.Ranking;
            Require(ranking.Outcomes > 0 && ranking.PriorityGains.Count > 0, "decided case");

            double[] Xs(List<GainsPoint> points) => points.Select(p => p.Worked * 100).ToArray();
            double[] Ys(List<GainsPoint> points) => points.Select(p => p.Found * 100).ToArray();

            var chart = Chart.Line()
                .Title(T(strings, CaseAnalyticsChartResources.GainsTitle, "Confirmed cases found as cases are worked"))
                .Subtitle(ranking.Sufficient
                    ? T(strings, CaseAnalyticsChartResources.DecidedCases, "{0} decided cases", ranking.Outcomes)
                    : T(strings, CaseAnalyticsChartResources.FewDecidedCases, "Only {0} decided cases, too few to rely on", ranking.Outcomes))
                .XAxisTitle(T(strings, CaseAnalyticsChartResources.CasesWorkedPercent, "Cases worked (%)"))
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.ConfirmedFoundPercent, "Confirmed found (%)"))
                .Size(640, 400)
                .Theme(theme)
                .XDomain(0, 100)
                .YDomain(0, 100)
                .Series(T(strings, CaseAnalyticsChartResources.PriorityOrder, "Priority order"), Xs(ranking.PriorityGains), Ys(ranking.PriorityGains));

            if (ranking.LikelihoodGains.Count > 0)
            {
                chart.Series(T(strings, CaseAnalyticsChartResources.LikelihoodOrder, "Likelihood order"), Xs(ranking.LikelihoodGains), Ys(ranking.LikelihoodGains));
            }

            chart.Series(T(strings, CaseAnalyticsChartResources.OldestFirst, "Oldest first"), Xs(ranking.OldestFirstGains), Ys(ranking.OldestFirstGains));
            chart.Reference(0, 0, 100, 100, T(strings, CaseAnalyticsChartResources.Random, "Random"));

            return chart.Render();
        }

        private static string Calibration(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            var bins = report.Ranking.Calibration;
            Require(bins.Count > 0, "likelihood");

            return Chart.Calibration()
                .Title(T(strings, CaseAnalyticsChartResources.CalibrationTitle, "Predicted likelihood against what happened"))
                .Subtitle(report.Ranking.Sufficient
                    ? T(strings, CaseAnalyticsChartResources.DecidedCases, "{0} decided cases", report.Ranking.Outcomes)
                    : T(strings, CaseAnalyticsChartResources.FewDecidedCases, "Only {0} decided cases, too few to rely on", report.Ranking.Outcomes))
                .Size(560, 460)
                .Theme(theme)
                .Curve(T(strings, CaseAnalyticsChartResources.Likelihood, "Likelihood"), bins.Select(b => b.Predicted), bins.Select(b => b.Observed),
                    bins.Select(b => (double)b.Cases), null, report.Ranking.ExpectedCalibrationError)
                .Render();
        }

        private static string Arrivals(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            Require(report.Rhythm.ArrivalsByWeekdayAndHour.Sum(d => d.Sum()) > 0, "arrival");

            return Chart.Heatmap()
                .Title(T(strings, CaseAnalyticsChartResources.ArrivalsTitle, "When cases arrive"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.ArrivalsSubtitle, "Cases created by weekday and hour (UTC)"))
                .XAxisTitle(T(strings, CaseAnalyticsChartResources.Hour, "Hour"))
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Weekday, "Weekday"))
                .Size(820, 320)
                .Theme(theme)
                .Rows(Weekdays(strings))
                .Columns(Enumerable.Range(0, 24).Select(h => h.ToString("00")))
                .Values(report.Rhythm.ArrivalsByWeekdayAndHour.Select(d => d.Select(c => (double)c)))
                .Decimals(0)
                .LegendTitle(T(strings, CaseAnalyticsChartResources.Cases, "Cases"))
                .Render();
        }

        private static string FirstViewByHour(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            Require(report.Rhythm.MedianFirstViewMinutesByHour.Any(m => m.HasValue), "first look");

            return Chart.Column()
                .Title(T(strings, CaseAnalyticsChartResources.FirstViewByHourTitle, "Median time to first look by hour of arrival"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.FirstViewByHourSubtitle, "Minutes a case waited before anyone looked at it, by the hour it arrived (UTC)"))
                .Size(760, 320)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Minutes, "Minutes"))
                .Categories(Enumerable.Range(0, 24).Select(h => h.ToString("00")))
                .Series(T(strings, CaseAnalyticsChartResources.MedianMinutes, "Median minutes"), report.Rhythm.MedianFirstViewMinutesByHour.Select(m => m ?? 0))
                .Render();
        }

        private static string ServiceLevelWeekly(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            var weeks = report.ServiceLevels.Weekly;
            Require(weeks.Count > 0, "service level result");

            var starts = weeks.Select(w => w.WeekStart).Distinct().OrderBy(w => w).ToList();

            var chart = Chart.Line()
                .Title(T(strings, CaseAnalyticsChartResources.ServiceLevelTitle, "Service level attainment by week"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.ServiceLevelSubtitle, "Share of cases created that week which met each target"))
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.MetPercent, "Met (%)"))
                .Size(720, 340)
                .Theme(theme)
                .YDomain(0, 100)
                .Categories(starts.Select(s => s.ToString("d MMM")));

            foreach (var dimension in weeks.Select(w => w.Dimension).Distinct().OrderBy(d => d))
            {
                chart.Series(dimension switch
                {
                    ServiceLevelDimension.FirstView => T(strings, CaseAnalyticsChartResources.ServiceLevelFirstView, "First look"),
                    ServiceLevelDimension.Ownership => T(strings, CaseAnalyticsChartResources.ServiceLevelOwnership, "Ownership"),
                    ServiceLevelDimension.Resolution => T(strings, CaseAnalyticsChartResources.ServiceLevelResolution, "Resolution"),
                    _ => T(strings, CaseAnalyticsChartResources.ServiceLevelTimeInStatus, "Time in status")
                }, starts.Select(start =>
                {
                    var week = weeks.FirstOrDefault(w => w.WeekStart == start && w.Dimension == dimension);

                    return week == null || week.Met + week.Breached == 0
                        ? 0
                        : 100.0 * week.Met / (week.Met + week.Breached);
                }));
            }

            return chart.Render();
        }

        private static string Handoffs(CaseAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            Require(report.Handoffs.Distribution.Sum(d => d.Cases) > 0, "case");

            return Chart.Column()
                .Title(T(strings, CaseAnalyticsChartResources.HandoffsTitle, "How often cases change hands"))
                .Subtitle(T(strings, CaseAnalyticsChartResources.HandoffsSubtitle, "{0} cases were reopened after closing", report.Handoffs.ReopenedCases))
                .Size(560, 300)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .XAxisTitle(T(strings, CaseAnalyticsChartResources.ChangesOfOwner, "Changes of owner"))
                .YAxisTitle(T(strings, CaseAnalyticsChartResources.Cases, "Cases"))
                .Categories(report.Handoffs.Distribution.Select(d => d.Label))
                .Series(T(strings, CaseAnalyticsChartResources.Cases, "Cases"), report.Handoffs.Distribution.Select(d => (double)d.Cases))
                .ShowValues()
                .Render();
        }
    }
}
