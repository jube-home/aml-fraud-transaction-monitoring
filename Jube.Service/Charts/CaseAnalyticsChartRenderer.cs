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
using Jube.Service.Query.CaseAnalytics;

namespace Jube.Service.Charts
{
    public static class CaseAnalyticsChartRenderer
    {
        private static readonly string[] Weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        public static string Render(CaseAnalyticsChart kind, CaseAnalyticsDto report, bool dark)
        {
            ArgumentNullException.ThrowIfNull(report);

            var theme = dark ? ThemeMode.Dark : ThemeMode.Light;

            return kind switch
            {
                CaseAnalyticsChart.Flow => Flow(report, theme),
                CaseAnalyticsChart.FirstView => Histogram("Time to first look", "Cases by how long they waited",
                    report.CycleTimes.FirstViewHistogram, theme),
                CaseAnalyticsChart.Resolution => Histogram("Time to resolution", "Closed cases by how long they took",
                    report.CycleTimes.ResolutionHistogram, theme),
                CaseAnalyticsChart.Aging => Aging(report, theme),
                CaseAnalyticsChart.StatusDwell => StatusDwell(report, theme),
                CaseAnalyticsChart.RuleEffort => RuleEffort(report, theme),
                CaseAnalyticsChart.RulePrecision => RulePrecision(report, theme),
                CaseAnalyticsChart.Gains => Gains(report, theme),
                CaseAnalyticsChart.Calibration => Calibration(report, theme),
                CaseAnalyticsChart.Arrivals => Arrivals(report, theme),
                CaseAnalyticsChart.FirstViewByHour => FirstViewByHour(report, theme),
                CaseAnalyticsChart.ServiceLevelWeekly => ServiceLevelWeekly(report, theme),
                CaseAnalyticsChart.Handoffs => Handoffs(report, theme),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "That chart is not available.")
            };
        }

        public static string RiskMatrix(CaseRiskMatrixDto matrix, bool dark)
        {
            ArgumentNullException.ThrowIfNull(matrix);

            if (matrix.Total == 0)
            {
                throw new ArgumentException("There are no cases to chart.");
            }

            string[] impacts = ["Minimal", "Low", "Moderate", "High", "Severe"];
            string[] likelihoods = ["Very likely", "Likely", "Possible", "Unlikely"];

            var values = new double[likelihoods.Length, impacts.Length];

            foreach (var cell in matrix.Cells.Where(c => c is { LikelihoodBand: >= 1 and <= 4, Impact: >= 1 and <= 5 }))
            {
                values[4 - cell.LikelihoodBand, cell.Impact - 1] = cell.Cases;
            }

            var unassessed = matrix.Cells.Where(c => c.LikelihoodBand == 0).Sum(c => c.Cases);

            return Chart.Heatmap()
                .Title("Risk matrix")
                .Subtitle(unassessed > 0
                    ? $"{matrix.Total - unassessed} assessed cases; {unassessed} not yet assessed"
                    : $"{matrix.Total} cases")
                .XAxisTitle("Impact")
                .YAxisTitle("Likelihood")
                .Size(560, 340)
                .Theme(dark ? ThemeMode.Dark : ThemeMode.Light)
                .Rows(likelihoods)
                .Columns(impacts)
                .Values(values)
                .ShowValues()
                .Decimals(0)
                .LegendTitle("Cases")
                .Render();
        }

        private static void Require(bool any, string what)
        {
            if (!any)
            {
                throw new ArgumentException("There is no " + what + " to chart.");
            }
        }

        private static string Flow(CaseAnalyticsDto report, ThemeMode theme)
        {
            Require(report.Flow.Count > 0, "flow");

            return Chart.Combo()
                .Title("Opened, closed and backlog")
                .Subtitle("Cases opened and closed each day, and the cases still open at the end of it")
                .Size(760, 340)
                .Theme(theme)
                .Categories(report.Flow.Select(d => d.Date.ToString("d MMM")))
                .Columns("Opened", report.Flow.Select(d => (double)d.Opened))
                .Columns("Closed", report.Flow.Select(d => (double)d.Closed))
                .Line("Backlog", report.Flow.Select(d => (double)d.Backlog))
                .Render();
        }

        private static string Histogram(string title, string subtitle, List<HistogramBucket> buckets, ThemeMode theme)
        {
            Require(buckets.Sum(b => b.Count) > 0, "timing");

            return Chart.Column()
                .Title(title)
                .Subtitle(subtitle)
                .Size(680, 320)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .YAxisTitle("Cases")
                .Categories(buckets.Select(b => b.Label))
                .Series("Cases", buckets.Select(b => (double)b.Count))
                .ShowValues()
                .Render();
        }

        private static string Aging(CaseAnalyticsDto report, ThemeMode theme)
        {
            Require(report.Aging.Open > 0, "open case");

            string[] lanes = ["Live money", "Mandatory", "Deadline near", "Standard"];

            var chart = Chart.Column()
                .Title("Open cases by age")
                .Subtitle("How long the open cases have waited, by urgency lane")
                .Size(720, 340)
                .Theme(theme)
                .Stacked()
                .YAxisTitle("Cases")
                .Categories(report.Aging.Buckets.Select(b => b.Label));

            for (var lane = 1; lane <= 4; lane++)
            {
                var current = lane;

                chart.Series(lanes[lane - 1], report.Aging.Buckets.Select(b =>
                    b.ByLane.TryGetValue(current, out var count) ? (double)count : 0));
            }

            return chart.Render();
        }

        private static string StatusDwell(CaseAnalyticsDto report, ThemeMode theme)
        {
            var rows = report.StatusDwell.Where(d => d.Completed.Count + d.Current.Count > 0).Take(15).ToList();
            Require(rows.Count > 0, "status stay");

            string Name(Guid guid) => report.StatusNames.GetValueOrDefault(guid, "Unknown");

            return Chart.Bar()
                .Title("Time in each status")
                .Subtitle("Median and slowest tenth of completed stays, in hours")
                .Size(720, 120 + rows.Count * 36)
                .Theme(theme)
                .Horizontal()
                .YAxisTitle("Hours")
                .ValueFormat(v => v.ToString("0.#"))
                .Categories(rows.Select(r => Name(r.CaseWorkflowStatusGuid)))
                .Series("Median", rows.Select(r => r.Completed.P50 / 60))
                .Series("Slowest tenth", rows.Select(r => r.Completed.P90 / 60))
                .Render();
        }

        private static string RuleEffort(CaseAnalyticsDto report, ThemeMode theme)
        {
            var rows = report.Rules.Where(r => r.WastedMinutes > 0).Take(12).ToList();
            Require(rows.Count > 0, "false positive effort");

            return Chart.Bar()
                .Title("Analyst time lost to false positives")
                .Subtitle("Hours spent on cases the rule raised that turned out not to be real")
                .Size(720, 120 + rows.Count * 36)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .YAxisTitle("Hours")
                .ValueFormat(v => v.ToString("0.#"))
                .Categories(rows.Select(r => r.Name))
                .Series("Hours", rows.Select(r => r.WastedMinutes / 60))
                .Render();
        }

        private static string RulePrecision(CaseAnalyticsDto report, ThemeMode theme)
        {
            var rows = report.Rules.Where(r => r.Precision.HasValue).Take(12).ToList();
            Require(rows.Count > 0, "decided rule");

            return Chart.Bar()
                .Title("Rule precision")
                .Subtitle("Share of decided cases the rule raised that were confirmed")
                .Size(720, 120 + rows.Count * 36)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .YAxisTitle("Percent")
                .ValueFormat(v => v.ToString("0") + "%")
                .Categories(rows.Select(r => r.Name))
                .Series("Precision", rows.Select(r => r.Precision!.Value * 100))
                .Render();
        }

        private static string Gains(CaseAnalyticsDto report, ThemeMode theme)
        {
            var ranking = report.Ranking;
            Require(ranking.Outcomes > 0 && ranking.PriorityGains.Count > 0, "decided case");

            double[] Xs(List<GainsPoint> points) => points.Select(p => p.Worked * 100).ToArray();
            double[] Ys(List<GainsPoint> points) => points.Select(p => p.Found * 100).ToArray();

            var chart = Chart.Line()
                .Title("Confirmed cases found as cases are worked")
                .Subtitle(ranking.Sufficient
                    ? $"{ranking.Outcomes} decided cases"
                    : $"Only {ranking.Outcomes} decided cases, too few to rely on")
                .XAxisTitle("Cases worked (%)")
                .YAxisTitle("Confirmed found (%)")
                .Size(640, 400)
                .Theme(theme)
                .XDomain(0, 100)
                .YDomain(0, 100)
                .Series("Priority order", Xs(ranking.PriorityGains), Ys(ranking.PriorityGains));

            if (ranking.LikelihoodGains.Count > 0)
            {
                chart.Series("Likelihood order", Xs(ranking.LikelihoodGains), Ys(ranking.LikelihoodGains));
            }

            chart.Series("Oldest first", Xs(ranking.OldestFirstGains), Ys(ranking.OldestFirstGains));
            chart.Reference(0, 0, 100, 100, "Random");

            return chart.Render();
        }

        private static string Calibration(CaseAnalyticsDto report, ThemeMode theme)
        {
            var bins = report.Ranking.Calibration;
            Require(bins.Count > 0, "likelihood");

            return Chart.Calibration()
                .Title("Predicted likelihood against what happened")
                .Subtitle(report.Ranking.Sufficient
                    ? $"{report.Ranking.Outcomes} decided cases"
                    : $"Only {report.Ranking.Outcomes} decided cases, too few to rely on")
                .Size(560, 460)
                .Theme(theme)
                .Curve("Likelihood", bins.Select(b => b.Predicted), bins.Select(b => b.Observed),
                    bins.Select(b => (double)b.Cases), null, report.Ranking.ExpectedCalibrationError)
                .Render();
        }

        private static string Arrivals(CaseAnalyticsDto report, ThemeMode theme)
        {
            Require(report.Rhythm.ArrivalsByWeekdayAndHour.Sum(d => d.Sum()) > 0, "arrival");

            return Chart.Heatmap()
                .Title("When cases arrive")
                .Subtitle("Cases created by weekday and hour (UTC)")
                .XAxisTitle("Hour")
                .YAxisTitle("Weekday")
                .Size(820, 320)
                .Theme(theme)
                .Rows(Weekdays)
                .Columns(Enumerable.Range(0, 24).Select(h => h.ToString("00")))
                .Values(report.Rhythm.ArrivalsByWeekdayAndHour.Select(d => d.Select(c => (double)c)))
                .Decimals(0)
                .LegendTitle("Cases")
                .Render();
        }

        private static string FirstViewByHour(CaseAnalyticsDto report, ThemeMode theme)
        {
            Require(report.Rhythm.MedianFirstViewMinutesByHour.Any(m => m.HasValue), "first look");

            return Chart.Column()
                .Title("Median time to first look by hour of arrival")
                .Subtitle("Minutes a case waited before anyone looked at it, by the hour it arrived (UTC)")
                .Size(760, 320)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .YAxisTitle("Minutes")
                .Categories(Enumerable.Range(0, 24).Select(h => h.ToString("00")))
                .Series("Median minutes", report.Rhythm.MedianFirstViewMinutesByHour.Select(m => m ?? 0))
                .Render();
        }

        private static string ServiceLevelWeekly(CaseAnalyticsDto report, ThemeMode theme)
        {
            var weeks = report.ServiceLevels.Weekly;
            Require(weeks.Count > 0, "service level result");

            var starts = weeks.Select(w => w.WeekStart).Distinct().OrderBy(w => w).ToList();

            var chart = Chart.Line()
                .Title("Service level attainment by week")
                .Subtitle("Share of cases created that week which met each target")
                .YAxisTitle("Met (%)")
                .Size(720, 340)
                .Theme(theme)
                .YDomain(0, 100)
                .Categories(starts.Select(s => s.ToString("d MMM")));

            foreach (var dimension in weeks.Select(w => w.Dimension).Distinct().OrderBy(d => d))
            {
                chart.Series(dimension switch
                {
                    ServiceLevelDimension.FirstView => "First look",
                    ServiceLevelDimension.Ownership => "Ownership",
                    ServiceLevelDimension.Resolution => "Resolution",
                    _ => "Time in status"
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

        private static string Handoffs(CaseAnalyticsDto report, ThemeMode theme)
        {
            Require(report.Handoffs.Distribution.Sum(d => d.Cases) > 0, "case");

            return Chart.Column()
                .Title("How often cases change hands")
                .Subtitle($"{report.Handoffs.ReopenedCases} cases were reopened after closing")
                .Size(560, 300)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .XAxisTitle("Changes of owner")
                .YAxisTitle("Cases")
                .Categories(report.Handoffs.Distribution.Select(d => d.Label))
                .Series("Cases", report.Handoffs.Distribution.Select(d => (double)d.Cases))
                .ShowValues()
                .Render();
        }
    }
}
