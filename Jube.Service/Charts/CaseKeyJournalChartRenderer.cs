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


using System.Globalization;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Dto.Options;
using Jube.Service.Query.CaseKeyJournal;

namespace Jube.Service.Charts
{
    public static class CaseKeyJournalChartRenderer
    {
        private static readonly string[] Weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        public static string Render(CaseKeyJournalChart kind, CaseKeyJournalAnalyticsDto report, string? field,
            bool dark)
        {
            ArgumentNullException.ThrowIfNull(report);

            var theme = dark ? ThemeMode.Dark : ThemeMode.Light;

            return kind switch
            {
                CaseKeyJournalChart.Progression => Progression(report, field, theme),
                CaseKeyJournalChart.Distribution => Distribution(report, field, theme),
                CaseKeyJournalChart.Velocity => Velocity(report, theme),
                CaseKeyJournalChart.Gaps => Gaps(report, theme),
                CaseKeyJournalChart.Counterparties => Counterparties(report, field, theme),
                CaseKeyJournalChart.Correlation => Correlation(report, theme),
                CaseKeyJournalChart.Drift => Drift(report, theme),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "That chart is not available.")
            };
        }

        private static void Require(bool any, string what)
        {
            if (!any)
            {
                throw new ArgumentException("There is no " + what + " to chart.");
            }
        }

        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static string Progression(CaseKeyJournalAnalyticsDto report, string? field, ThemeMode theme)
        {
            var series = report.Progression?.Series.FirstOrDefault(s => s.Field == field);

            Require(series is { Points.Count: > 0 }, "progression");

            var format = report.Progression!.Bucket == "hour" ? "d MMM HH:mm" : "d MMM";

            return Chart.Combo()
                .Title(series!.Field + " over time")
                .Subtitle("Median per " + report.Progression.Bucket + ", latest " + Number(series.Latest))
                .Size(760, 300)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Categories(series.Points.Select(p => p.Start.ToString(format, CultureInfo.InvariantCulture)))
                .Line(series.Field, series.Points.Select(p => p.Median))
                .Render();
        }

        private static string Distribution(CaseKeyJournalAnalyticsDto report, string? field, ThemeMode theme)
        {
            var distribution = report.Distributions?.FirstOrDefault(d => d.Field == field);

            Require(distribution is { Count: > 0 }, "distribution");

            return Chart.Column()
                .Title(distribution!.Field + " distribution")
                .Subtitle("The latest value, " + Number(distribution.Latest) + ", is at the " +
                          (int)Math.Round(distribution.LatestPercentile * 100) + "th percentile of " +
                          distribution.Count + " transactions")
                .Size(720, 320)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .YAxisTitle("Transactions")
                .Categories(distribution.Bins.Select(b => Number(b.From)))
                .Series("Transactions", distribution.Bins.Select(b => (double)b.Count))
                .Render();
        }

        private static string Velocity(CaseKeyJournalAnalyticsDto report, ThemeMode theme)
        {
            Require(report.Velocity is not null && report.Velocity.HourByWeekday.Sum(day => day.Sum()) > 0,
                "transaction");

            return Chart.Heatmap()
                .Title("When transactions happen")
                .Subtitle("By weekday and hour (UTC)")
                .XAxisTitle("Hour")
                .YAxisTitle("Weekday")
                .Size(820, 320)
                .Theme(theme)
                .Rows(Weekdays)
                .Columns(Enumerable.Range(0, 24).Select(h => h.ToString("00", CultureInfo.InvariantCulture)))
                .Values(report.Velocity!.HourByWeekday.Select(day => day.Select(count => (double)count)))
                .Decimals(0)
                .LegendTitle("Transactions")
                .Render();
        }

        private static string Gaps(CaseKeyJournalAnalyticsDto report, ThemeMode theme)
        {
            Require(report.Gaps is not null && report.Gaps.Bins.Sum(b => b.Count) > 0, "gap");

            return Chart.Column()
                .Title("Time between transactions")
                .Subtitle("How long the key was quiet before each transaction")
                .Size(720, 320)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .YAxisTitle("Transactions")
                .Categories(report.Gaps!.Bins.Select(b => b.Label))
                .Series("Transactions", report.Gaps.Bins.Select(b => (double)b.Count))
                .ShowValues()
                .Render();
        }

        private static string Counterparties(CaseKeyJournalAnalyticsDto report, string? field, ThemeMode theme)
        {
            var profile = report.Counterparties?.FirstOrDefault(p => p.Field == field)
                          ?? report.Counterparties?.FirstOrDefault();

            Require(profile is { Top.Count: > 0 }, "counterparty");

            return Chart.Bar()
                .Title(profile!.Field + " counterparties")
                .Subtitle(profile.Distinct + " distinct, " + profile.NewCount + " new in the last 30 days")
                .Size(720, 120 + profile.Top.Count * 36)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .YAxisTitle("Transactions")
                .Categories(profile.Top.Select(entry => entry.Value))
                .Series("Transactions", profile.Top.Select(entry => (double)entry.Count))
                .Render();
        }

        private static string Correlation(CaseKeyJournalAnalyticsDto report, ThemeMode theme)
        {
            var correlation = report.Correlation;

            Require(correlation is { Fields.Count: > 1 }, "correlated field");

            return Chart.Heatmap()
                .Title("How the measures move together")
                .Subtitle("Pearson correlation, from -1 to 1")
                .Size(640, 460)
                .Theme(theme)
                .Rows(correlation!.Fields)
                .Columns(correlation.Fields)
                .Values(correlation.Matrix.Select(row => row.Select(cell => cell ?? 0)))
                .ShowValues()
                .Decimals(2)
                .LegendTitle("r")
                .Render();
        }

        private static string Drift(CaseKeyJournalAnalyticsDto report, ThemeMode theme)
        {
            var fields = report.Drift?.Fields.Take(15).ToList() ?? [];

            Require(fields.Count > 0, "drift");

            return Chart.Bar()
                .Title("Drift between earlier and recent history")
                .Subtitle("Distance between the two halves; above the critical value has drifted")
                .Size(720, 120 + fields.Count * 36)
                .Theme(theme)
                .Horizontal()
                .YAxisTitle("Distance")
                .ValueFormat(v => v.ToString("0.00", CultureInfo.InvariantCulture))
                .Categories(fields.Select(item => item.Field))
                .Series("Distance", fields.Select(item => item.Statistic))
                .Series("Critical", fields.Select(item => item.Critical))
                .Render();
        }
    }
}
