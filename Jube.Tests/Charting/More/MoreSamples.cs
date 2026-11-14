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

using System;
using Jube.Charting.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Jube.Charting;
using Jube.Charting.Interfaces;
using Jube.Charting.More;
using Jube.Charting.More.Models;

namespace Jube.Test.Charting.More
{
    public static class MoreSamples
    {
        public static readonly double[] MercaptopurineTimes =
            [6, 6, 6, 6, 7, 9, 10, 10, 11, 13, 16, 17, 19, 20, 22, 23, 25, 32, 32, 34, 35];

        public static readonly bool[] MercaptopurineEvents =
        [
            true, true, true, false, true, false, true, false, false, true, true, false, false, false, true, true,
            false, false, false, false, false
        ];

        public static readonly double[] PlaceboTimes =
            [1, 1, 2, 2, 3, 4, 4, 5, 5, 8, 8, 8, 8, 11, 11, 12, 12, 15, 17, 22, 23];

        private static readonly string[] countries =
            ["United Kingdom", "Germany", "France", "Spain", "Italy", "Netherlands", "Poland", "Sweden"];

        public static IReadOnlyDictionary<string, ISvgChart> All()
        {
            return new SortedDictionary<string, ISvgChart>(StringComparer.Ordinal)
            {
                ["survival"] = Survival(),
                ["timeseries-forecast"] = Forecast(),
                ["timeseries-range"] = RangeSelected(),
                ["step-chart"] = Steps(),
                ["error-bars"] = ErrorBars(),
                ["regression"] = Regression(),
                ["parallel"] = Parallel(),
                ["facets"] = Facets(),
                ["scatter-matrix"] = ScatterMatrix(),
                ["density"] = Density(),
                ["ecdf"] = Ecdf(),
                ["violin"] = Violin(false),
                ["violin-horizontal"] = Violin(true),
                ["ridgeline"] = Ridgeline(),
                ["dot-plot"] = Dots(DotStyle.Dot),
                ["lollipop"] = Dots(DotStyle.Lollipop),
                ["dumbbell"] = Dots(DotStyle.Dumbbell),
                ["slope"] = Slope(),
                ["bump"] = Bump(),
                ["correlation-clustered"] = CorrelationClustered(),
                ["adjacency"] = Adjacency(),
                ["calendar"] = Calendar(),
                ["sankey"] = Sankey(),
                ["stream"] = Stream(false),
                ["stream-percent"] = Stream(true),
                ["legend-many"] = LegendMany()
            };
        }

        public static ISvgChart Survival()
        {
            return Chart.Survival().Title("Time to remission relapse")
                .Subtitle("Kaplan-Meier with 95% confidence bands")
                .XAxisTitle("Weeks").YAxisTitle("Remission")
                .Group("6-MP", MercaptopurineTimes, MercaptopurineEvents)
                .Group("Placebo", PlaceboTimes, PlaceboTimes.Select(_ => true))
                .Build();
        }

        private static DateTime Day(int i)
        {
            return new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i);
        }

        private static double Volume(int i)
        {
            return 1000 + 6 * i + 120 * Math.Sin(i * 2 * Math.PI / 7) + 40 * Math.Sin(i * 0.9);
        }

        public static ISvgChart Forecast()
        {
            const int history = 50;
            const int total = 70;
            var days = Enumerable.Range(0, total).Select(Day).ToArray();
            var actual = Enumerable.Range(0, total).Select(i => i < history ? Volume(i) : double.NaN).ToArray();

            var forecast = Enumerable.Range(0, total).Select(i =>
                i >= history - 1 ? 1000 + 6 * i + 120 * Math.Sin(i * 2 * Math.PI / 7) : double.NaN).ToArray();

            double Half(int i, double k)
            {
                return i < history - 1 ? double.NaN : k * (8 + 3.2 * (i - history + 1));
            }

            var lo80 = Enumerable.Range(0, total).Select(i => forecast[i] - Half(i, 1.28)).ToArray();
            var hi80 = Enumerable.Range(0, total).Select(i => forecast[i] + Half(i, 1.28)).ToArray();
            var lo95 = Enumerable.Range(0, total).Select(i => forecast[i] - Half(i, 1.96)).ToArray();
            var hi95 = Enumerable.Range(0, total).Select(i => forecast[i] + Half(i, 1.96)).ToArray();

            return Chart.TimeSeries().Title("Daily transaction volume")
                .Subtitle("Actual versus forecast with prediction intervals")
                .YAxisTitle("Transactions")
                .Series("Actual", days, actual)
                .Forecast("Forecast", days, forecast, Day(history - 1)).Interval("95% interval", lo95, hi95)
                .Interval("80% interval", lo80, hi80)
                .Event(Day(12), "Campaign launch").ChangePoint(Day(33), "Rule change")
                .Region(Day(20), Day(24), "Outage")
                .Build();
        }

        public static ISvgChart RangeSelected()
        {
            var days = Enumerable.Range(0, 180).Select(Day).ToArray();
            var values = Enumerable.Range(0, 180).Select(i => Volume(i) + 30 * Math.Sin(i / 11.0)).ToArray();

            return Chart.TimeSeries().Title("Volume with range selector").YAxisTitle("Transactions")
                .Series("Volume", days, values)
                .RangeSelector(Day(60), Day(110))
                .Build();
        }

        public static ISvgChart Steps()
        {
            return Chart.StepChart().Title("Card limit by tier").XAxisTitle("Month").YAxisTitle("Limit")
                .Series("Standard", [0.0, 3, 6, 9, 12], [1000.0, 1500, 1500, 2000, 2000])
                .Series("Premium", [0.0, 2, 6, 10, 12], [2000.0, 3500, 3500, 5000, 5000])
                .FromZero()
                .Build();
        }

        public static ISvgChart ErrorBars()
        {
            var xs = new[] { 1.0, 2, 3, 4, 5, 6, 7, 8 };
            var ys = new[] { 2.1, 3.9, 6.2, 7.8, 10.1, 12.4, 13.8, 16.3 };
            var errors = new[] { 0.6, 0.5, 0.9, 0.4, 0.8, 1.1, 0.7, 1.2 };
            var ys2 = new[] { 1.0, 2.2, 3.1, 3.9, 5.2, 5.8, 7.1, 7.7 };

            return Chart.ErrorBars().Title("Measured response").XAxisTitle("Dose").YAxisTitle("Response")
                .Points("Treated", xs, ys).YErrors(errors).XErrors([.. xs.Select(_ => 0.2)])
                .Series("Control", xs, ys2).WithPoints()
                .Interval("95% band", ys2.Select(v => v - 0.9), ys2.Select(v => v + 0.9))
                .Build();
        }

        public static ISvgChart Regression()
        {
            var xs = new[] { 10.0, 8, 13, 9, 11, 14, 6, 4, 12, 7, 5 };
            var ys = new[] { 8.04, 6.95, 7.58, 8.81, 8.33, 9.96, 7.24, 4.26, 10.84, 4.82, 5.68 };

            return Chart.Regression().Title("Linear fit").XAxisTitle("x").YAxisTitle("y").Data("Sample", xs, ys)
                .Extend(0.15)
                .Build();
        }

        public static ISvgChart Parallel()
        {
            const int rows = 90;

            var segment = Enumerable.Range(0, rows).Select(i => new[] { "Retail", "Corporate", "Private" }[i % 3])
                .ToArray();

            double Base(int i)
            {
                return new[] { 1.0, 3.0, 6.0 }[i % 3];
            }

            var amount = Enumerable.Range(0, rows)
                .Select(i => Base(i) * (100 + 40 * Math.Sin(i * 1.7) + 20 * Math.Cos(i * 0.6))).ToArray();

            var velocity = Enumerable.Range(0, rows).Select(i => 2 + Base(i) + 1.5 * Math.Sin(i * 0.9)).ToArray();

            var distance = Enumerable.Range(0, rows).Select(i => 50 + 30 * Math.Abs(Math.Sin(i * 0.37)) * Base(i))
                .ToArray();

            var hour = Enumerable.Range(0, rows).Select(i => (double)(i * 7 % 24)).ToArray();

            var channel = Enumerable.Range(0, rows)
                .Select(i => new[] { "Card", "Transfer", "Cash", "Online" }[(i * 5 + i / 3) % 4]).ToArray();

            var risk = Enumerable.Range(0, rows)
                .Select(i => Math.Min(1, Math.Max(0, 0.15 * Base(i) + 0.3 * Math.Abs(Math.Sin(i * 0.5))))).ToArray();

            return Chart.ParallelCoordinates().Title("Transaction profile").Subtitle("Each line is one customer")
                .Dimension("Amount", amount).Dimension("Velocity", velocity).Dimension("Distance km", distance)
                .Dimension("Hour", hour, 0, 24).CategoricalDimension("Channel", channel).Dimension("Risk", risk)
                .Groups(segment)
                .Build();
        }

        public static ISvgChart Facets()
        {
            var xs = Enumerable.Range(0, 24).Select(i => (double)i).ToArray();

            var builder = Chart.Facets().Title("Hourly volume by channel").XAxisTitle("Hour of day")
                .YAxisTitle("Transactions")
                .Size(720, 520).Columns(2).SharedScales();

            var channels = new[] { "Card", "Transfer", "Cash", "Online" };

            for (var c = 0; c < channels.Length; c++)
            {
                var k = c + 1;

                builder.LinePanel(channels[c], xs,
                [
                    .. xs.Select(x => 50 * k + 30 * k * Math.Sin((x - 3 * k) / 24 * 2 * Math.PI) + 5 * Math.Sin(x * k))
                ]);
            }

            return builder.Build();
        }

        public static ISvgChart ScatterMatrix()
        {
            const int rows = 90;

            double Wave(int i, double f)
            {
                return Math.Sin(i * f) + 0.5 * Math.Cos(i * f * 2.3);
            }

            var group = Enumerable.Range(0, rows).Select(i => new[] { "Retail", "Corporate", "Private" }[i % 3])
                .ToArray();

            double Base(int i)
            {
                return new[] { 1.0, 2.2, 3.5 }[i % 3];
            }

            var amount = Enumerable.Range(0, rows).Select(i => 100 * Base(i) + 25 * Wave(i, 0.7)).ToArray();
            var velocity = Enumerable.Range(0, rows).Select(i => 2 * Base(i) + Wave(i, 1.3)).ToArray();
            var distance = Enumerable.Range(0, rows).Select(i => 80 - 12 * Base(i) + 20 * Wave(i, 0.4)).ToArray();

            var risk = Enumerable.Range(0, rows).Select(i => 0.1 * Base(i) + 0.05 * Wave(i, 2.1) + 0.001 * amount[i])
                .ToArray();

            return Chart.ScatterMatrix().Title("Feature relationships").Size(720, 660)
                .Variable("Amount", amount).Variable("Velocity", velocity).Variable("Distance", distance)
                .Variable("Risk", risk)
                .Groups(group)
                .Build();
        }

        public static double[] Normal(int n, double mean, double deviation, int seed = 1)
        {
            var values = Enumerable.Range(0, n).Select(i => mean + deviation * ChartStats.NormalQuantile((i + 0.5) / n))
                .ToArray();

            return [.. values.OrderBy(v => (int)((v * 7919 + seed * 104729) * 1000) % 977)];
        }

        public static double[] Bimodal(int n, double a, double b, double deviation)
        {
            return [.. Normal(n / 2, a, deviation, 2), .. Normal(n / 2, b, deviation, 3)];
        }

        public static ISvgChart Density()
        {
            return Chart.Density().Title("Transaction amount distribution")
                .Subtitle("Gaussian kernel, Silverman bandwidth")
                .XAxisTitle("Amount").YAxisTitle("Density")
                .Values("Retail", Normal(400, 60, 20)).Values("Corporate", Bimodal(400, 120, 260, 30))
                .Build();
        }

        public static ISvgChart Ecdf()
        {
            return Chart.Ecdf().Title("Empirical distribution").XAxisTitle("Amount").YAxisTitle("Cumulative share")
                .Values("Retail", Normal(120, 60, 20)).Values("Corporate", Bimodal(120, 120, 260, 30))
                .ConfidenceBand()
                .Build();
        }

        public static ISvgChart Violin(bool horizontal)
        {
            return Chart.Violin().Title("Amount by segment").YAxisTitle(horizontal ? null : "Amount")
                .XAxisTitle(horizontal ? "Amount" : null)
                .Horizontal(horizontal)
                .Values("Retail", Normal(200, 60, 20)).Values("Corporate", Bimodal(200, 120, 260, 30))
                .Values("Private", Normal(200, 300, 60)).Points()
                .Build();
        }

        public static ISvgChart Ridgeline()
        {
            return Chart.Ridgeline().Title("Amount by month").XAxisTitle("Amount")
                .Values("January", Normal(150, 80, 20)).Values("February", Normal(150, 100, 25))
                .Values("March", Bimodal(150, 90, 160, 20)).Values("April", Normal(150, 150, 30))
                .Values("May", Normal(150, 170, 22))
                .Build();
        }

        public static ISvgChart Dots(DotStyle style)
        {
            var before = new[] { 42.0, 31, 55, 23, 61, 18, 37, 29 };
            var after = new[] { 35.0, 34, 41, 27, 44, 25, 30, 22 };

            var builder = style switch
            {
                DotStyle.Lollipop => Chart.Lollipop(),
                DotStyle.Dumbbell => Chart.Dumbbell(),
                _ => Chart.DotPlot()
            };

            builder.Title(style == DotStyle.Dumbbell ? "Alert rate before and after tuning" : "Alert rate by country")
                .XAxisTitle("Alerts per 1,000")
                .Categories(countries);

            if (style == DotStyle.Lollipop)
            {
                builder.Series("Before", before).ShowValues();
            }
            else
            {
                builder.Series("Before", before).Series("After", after);

                if (style == DotStyle.Dumbbell)
                {
                    builder.ShowValues();
                }
            }

            return builder.Sorted(DotSort.Descending).Build();
        }

        public static ISvgChart Slope()
        {
            return Chart.Slope().Title("Fraud loss by channel").Subtitle("Millions")
                .Periods(["2024", "2025"])
                .Series("Card present", [4.2, 3.1]).Series("Card not present", [7.9, 9.4])
                .Series("Transfer", [5.5, 6.8])
                .Series("Cash", [2.2, 1.4]).Series("Cheque", [1.9, 1.3]).Series("Mobile", [3.0, 5.2])
                .Build();
        }

        public static ISvgChart Bump()
        {
            return Chart.Bump().Title("Alert volume rank by country").Periods(["Q1", "Q2", "Q3", "Q4", "Q1 next"])
                .Series("United Kingdom", [90.0, 88, 95, 92, 99]).Series("Germany", [80.0, 91, 90, 97, 96])
                .Series("France", [70.0, 72, 85, 94, 90]).Series("Spain", [60.0, 65, 62, 66, 80])
                .Series("Italy", [85.0, 79, 70, 60, 55]).Series("Poland", [40.0, 45, 60, 75, 65])
                .Build();
        }

        public static ISvgChart CorrelationClustered()
        {
            const int n = 60;

            double[] Wave(double f, double phase)
            {
                return [.. Enumerable.Range(0, n).Select(i => Math.Sin(i * f + phase))];
            }

            var a = Wave(0.31, 0);
            var b = Wave(0.7, 1);

            double[] Mix(double[] x, double[] y, double w)
            {
                return [.. x.Select((v, i) => v * w + y[i] * (1 - w) + 0.15 * Math.Sin(i * 3.1 + w))];
            }

            return Chart.CorrelationMatrix().Title("Feature correlation").Subtitle("Ordered by hierarchical clustering")
                .Variable("Amount", Mix(a, b, 0.95)).Variable("Distance", Mix(b, a, 0.9))
                .Variable("Velocity", Mix(a, b, 0.85))
                .Variable("Hour", Mix(b, a, 0.98)).Variable("Balance", Mix(a, b, 0.9)).Variable("Age", Mix(b, a, 0.8))
                .Size(600, 520)
                .Build();
        }

        public static ISvgChart Adjacency()
        {
            var builder = Chart.Adjacency().Title("Account transfer network").Order(MatrixOrder.Cluster)
                .Nodes(["A", "B", "C", "D", "E", "F", "G", "H"]).Size(520, 480);

            foreach (var (from, to, w) in new[]
                     {
                         ("A", "C", 5.0), ("A", "E", 4), ("C", "E", 6), ("A", "G", 3), ("G", "E", 4), ("B", "D", 5),
                         ("B", "F", 4), ("D", "F", 6), ("D", "H", 3), ("B", "H", 2), ("C", "D", 1)
                     })
            {
                builder.Edge(from, to, w);
            }

            return builder.Build();
        }

        public static ISvgChart Calendar()
        {
            var start = new DateTime(2025, 1, 1);

            var builder = Chart.CalendarHeatmap().Title("Daily transaction volume").ValueName("Transactions")
                .Size(720, 300);

            for (var i = 0; i < 540; i++)
            {
                var date = start.AddDays(i);

                if (i % 23 == 5)
                {
                    continue;
                }

                var weekday = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.45 : 1.0;
                var season = 1 + 0.5 * Math.Sin(i / 58.0);
                var spike = i % 97 == 40 ? 1.7 : 1.0;
                builder.Day(date, Math.Round(2000 * weekday * season * spike + 150 * Math.Sin(i * 1.3)));
            }

            return builder.Build();
        }

        public static ISvgChart Sankey(bool inline = false)
        {
            var builder = Chart.Sankey();

            if (inline)
            {
                builder.StyleMode(ChartStyleMode.Inline);
            }

            return builder.Title("Alert disposition").Subtitle("Alerts flowing from source to outcome")
                .Size(720, 420)
                .Link("Card", "Rules", 420).Link("Transfer", "Rules", 300).Link("Cash", "Rules", 120)
                .Link("Card", "Model", 260).Link("Transfer", "Model", 180)
                .Link("Rules", "Reviewed", 600).Link("Rules", "Auto closed", 240)
                .Link("Model", "Reviewed", 300).Link("Model", "Auto closed", 140)
                .Link("Reviewed", "Escalated", 210).Link("Reviewed", "Cleared", 690)
                .Link("Escalated", "SAR filed", 120).Link("Escalated", "No action", 90)
                .Build();
        }

        public static ISvgChart Stream(bool percent)
        {
            var xs = Enumerable.Range(0, 48).Select(i => (double)i).ToArray();
            var names = new[] { "Card present", "Card not present", "Transfer", "Cash", "Cheque", "Mobile" };
            var builder = percent ? Chart.PercentStream() : Chart.Stream();
            builder.Title(percent ? "Channel share over time" : "Channel volume over time").XAxisTitle("Week").X(xs);

            for (var k = 0; k < names.Length; k++)
            {
                var center = 6 + k * 8;

                builder.Series(names[k],
                    [.. xs.Select(x => 20 + 80 * Math.Exp(-Math.Pow((x - center) / 9, 2)) + 6 * Math.Sin(x / 3 + k))]);
            }

            return builder.Build();
        }

        public static ISvgChart LegendMany()
        {
            var builder = Chart.Line().Title("Many series with a capped legend").LegendMaxRows(2);

            for (var k = 0; k < 24; k++)
            {
                builder.Series("Series number " + k,
                    [.. Enumerable.Range(0, 12).Select(i => k + Math.Sin(i / 2.0 + k))]);
            }

            return builder.Build();
        }
    }
}