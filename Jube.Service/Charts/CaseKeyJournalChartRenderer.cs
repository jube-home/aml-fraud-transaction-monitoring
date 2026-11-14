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
using Jube.Resources;
using Jube.Service.Query.CaseKeyJournal;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public static class CaseKeyJournalChartRenderer
    {
        private static string[] Weekdays(IStringLocalizer? strings) =>
        [
            T(strings, CaseKeyJournalChartResources.WeekdayMon, "Mon"),
            T(strings, CaseKeyJournalChartResources.WeekdayTue, "Tue"),
            T(strings, CaseKeyJournalChartResources.WeekdayWed, "Wed"),
            T(strings, CaseKeyJournalChartResources.WeekdayThu, "Thu"),
            T(strings, CaseKeyJournalChartResources.WeekdayFri, "Fri"),
            T(strings, CaseKeyJournalChartResources.WeekdaySat, "Sat"),
            T(strings, CaseKeyJournalChartResources.WeekdaySun, "Sun")
        ];

        private static string T(IStringLocalizer? strings, string key, string english, params object[] arguments) =>
            ChartText.Of(strings, key, english, arguments);

        private static string Bucket(IStringLocalizer? strings, string bucket) => bucket switch
        {
            "hour" => T(strings, CaseKeyJournalChartResources.BucketHour, bucket),
            "day" => T(strings, CaseKeyJournalChartResources.BucketDay, bucket),
            "week" => T(strings, CaseKeyJournalChartResources.BucketWeek, bucket),
            "month" => T(strings, CaseKeyJournalChartResources.BucketMonth, bucket),
            _ => bucket
        };

        public static string Render(CaseKeyJournalChart kind, CaseKeyJournalAnalyticsDto report, string? field,
            bool dark, IStringLocalizer? strings = null)
        {
            ArgumentNullException.ThrowIfNull(report);

            var theme = dark ? ThemeMode.Dark : ThemeMode.Light;

            return kind switch
            {
                CaseKeyJournalChart.Progression => Progression(report, field, theme, strings),
                CaseKeyJournalChart.Distribution => Distribution(report, field, theme, strings),
                CaseKeyJournalChart.Velocity => Velocity(report, theme, strings),
                CaseKeyJournalChart.Gaps => Gaps(report, theme, strings),
                CaseKeyJournalChart.Counterparties => Counterparties(report, field, theme, strings),
                CaseKeyJournalChart.Correlation => Correlation(report, theme, strings),
                CaseKeyJournalChart.Drift => Drift(report, theme, strings),
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

        private static string Progression(CaseKeyJournalAnalyticsDto report, string? field, ThemeMode theme, IStringLocalizer? strings)
        {
            var series = report.Progression?.Series.FirstOrDefault(s => s.Field == field);

            Require(series is { Points.Count: > 0 }, "progression");

            var format = report.Progression!.Bucket == "hour" ? "d MMM HH:mm" : "d MMM";

            return Chart.Combo()
                .Title(T(strings, CaseKeyJournalChartResources.ProgressionTitle, "{0} over time", series!.Field))
                .Subtitle(T(strings, CaseKeyJournalChartResources.ProgressionSubtitle, "Median per {0}, latest {1}",
                    Bucket(strings, report.Progression.Bucket), Number(series.Latest)))
                .Size(760, 300)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Categories(series.Points.Select(p => p.Start.ToString(format, CultureInfo.InvariantCulture)))
                .Line(series.Field, series.Points.Select(p => p.Median))
                .Render();
        }

        private static string Distribution(CaseKeyJournalAnalyticsDto report, string? field, ThemeMode theme, IStringLocalizer? strings)
        {
            var distribution = report.Distributions?.FirstOrDefault(d => d.Field == field);

            Require(distribution is { Count: > 0 }, "distribution");

            return Chart.Column()
                .Title(T(strings, CaseKeyJournalChartResources.DistributionTitle, "{0} distribution", distribution!.Field))
                .Subtitle(T(strings, CaseKeyJournalChartResources.DistributionSubtitle,
                    "The latest value, {0}, is at the {1}th percentile of {2} transactions",
                    Number(distribution.Latest), (int)Math.Round(distribution.LatestPercentile * 100),
                    distribution.Count))
                .Size(720, 320)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .YAxisTitle(T(strings, CaseKeyJournalChartResources.Transactions, "Transactions"))
                .Categories(distribution.Bins.Select(b => Number(b.From)))
                .Series(T(strings, CaseKeyJournalChartResources.Transactions, "Transactions"), distribution.Bins.Select(b => (double)b.Count))
                .Render();
        }

        private static string Velocity(CaseKeyJournalAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            Require(report.Velocity is not null && report.Velocity.HourByWeekday.Sum(day => day.Sum()) > 0,
                "transaction");

            return Chart.Heatmap()
                .Title(T(strings, CaseKeyJournalChartResources.VelocityTitle, "When transactions happen"))
                .Subtitle(T(strings, CaseKeyJournalChartResources.VelocitySubtitle, "By weekday and hour (UTC)"))
                .XAxisTitle(T(strings, CaseKeyJournalChartResources.Hour, "Hour"))
                .YAxisTitle(T(strings, CaseKeyJournalChartResources.Weekday, "Weekday"))
                .Size(820, 320)
                .Theme(theme)
                .Rows(Weekdays(strings))
                .Columns(Enumerable.Range(0, 24).Select(h => h.ToString("00", CultureInfo.InvariantCulture)))
                .Values(report.Velocity!.HourByWeekday.Select(day => day.Select(count => (double)count)))
                .Decimals(0)
                .LegendTitle(T(strings, CaseKeyJournalChartResources.Transactions, "Transactions"))
                .Render();
        }

        private static string Gaps(CaseKeyJournalAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            Require(report.Gaps is not null && report.Gaps.Bins.Sum(b => b.Count) > 0, "gap");

            return Chart.Column()
                .Title(T(strings, CaseKeyJournalChartResources.GapsTitle, "Time between transactions"))
                .Subtitle(T(strings, CaseKeyJournalChartResources.GapsSubtitle, "How long the key was quiet before each transaction"))
                .Size(720, 320)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .YAxisTitle(T(strings, CaseKeyJournalChartResources.Transactions, "Transactions"))
                .Categories(report.Gaps!.Bins.Select(b => b.Label))
                .Series(T(strings, CaseKeyJournalChartResources.Transactions, "Transactions"), report.Gaps.Bins.Select(b => (double)b.Count))
                .ShowValues()
                .Render();
        }

        private static string Counterparties(CaseKeyJournalAnalyticsDto report, string? field, ThemeMode theme, IStringLocalizer? strings)
        {
            var profile = report.Counterparties?.FirstOrDefault(p => p.Field == field)
                          ?? report.Counterparties?.FirstOrDefault();

            Require(profile is { Top.Count: > 0 }, "counterparty");

            return Chart.Bar()
                .Title(T(strings, CaseKeyJournalChartResources.CounterpartiesTitle, "{0} counterparties", profile!.Field))
                .Subtitle(T(strings, CaseKeyJournalChartResources.CounterpartiesSubtitle, "{0} distinct, {1} new in the last 30 days",
                    profile.Distinct, profile.NewCount))
                .Size(720, 120 + profile.Top.Count * 36)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .YAxisTitle(T(strings, CaseKeyJournalChartResources.Transactions, "Transactions"))
                .Categories(profile.Top.Select(entry => entry.Value))
                .Series(T(strings, CaseKeyJournalChartResources.Transactions, "Transactions"), profile.Top.Select(entry => (double)entry.Count))
                .Render();
        }

        private static string Correlation(CaseKeyJournalAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            var correlation = report.Correlation;

            Require(correlation is { Fields.Count: > 1 }, "correlated field");

            return Chart.Heatmap()
                .Title(T(strings, CaseKeyJournalChartResources.CorrelationTitle, "How the measures move together"))
                .Subtitle(T(strings, CaseKeyJournalChartResources.CorrelationSubtitle, "Pearson correlation, from -1 to 1"))
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

        private static string Drift(CaseKeyJournalAnalyticsDto report, ThemeMode theme, IStringLocalizer? strings)
        {
            var fields = report.Drift?.Fields.Take(15).ToList() ?? [];

            Require(fields.Count > 0, "drift");

            return Chart.Bar()
                .Title(T(strings, CaseKeyJournalChartResources.DriftTitle, "Drift between earlier and recent history"))
                .Subtitle(T(strings, CaseKeyJournalChartResources.DriftSubtitle,
                    "Distance between the two halves; above the critical value has drifted"))
                .Size(720, 120 + fields.Count * 36)
                .Theme(theme)
                .Horizontal()
                .YAxisTitle(T(strings, CaseKeyJournalChartResources.Distance, "Distance"))
                .ValueFormat(v => v.ToString("0.00", CultureInfo.InvariantCulture))
                .Categories(fields.Select(item => item.Field))
                .Series(T(strings, CaseKeyJournalChartResources.Distance, "Distance"), fields.Select(item => item.Statistic))
                .Series(T(strings, CaseKeyJournalChartResources.Critical, "Critical"), fields.Select(item => item.Critical))
                .Render();
        }
    }
}
