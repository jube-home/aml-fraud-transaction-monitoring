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
using Jube.Charting.Core;
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Jube.Charting.More.Models;
using Jube.Charting.Svg;
using Jube.Charting.Theme;
using Jube.Charting.Theme.Models;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class CalendarChart(CalendarChartSpec spec) : IStyleModeChart
    {
        public const int MaxYears = 12;

        public CalendarChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new CalendarChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            ChartLimits.Points(s.Days.Count);
            var format = s.ValueFormat ?? NumberFormat.Value;
            var groups = new SortedDictionary<DateTime, List<double>>();

            foreach (var d in s.Days)
            {
                if (!double.IsFinite(d.Value))
                {
                    continue;
                }

                var key = d.Date.Date;

                if (!groups.TryGetValue(key, out var list))
                {
                    list = [];
                    groups[key] = list;
                }

                list.Add(d.Value);
            }

            var days = groups.ToDictionary(g => g.Key, g => s.Aggregate switch
            {
                CalendarAggregate.Mean => g.Value.Average(),
                CalendarAggregate.Max => g.Value.Max(),
                _ => g.Value.Sum()
            });

            var years = days.Keys.Select(d => d.Year).Distinct().OrderBy(y => y).ToList();

            if (years.Count > MaxYears)
            {
                throw new ChartLimitException("calendar year count", years.Count, MaxYears);
            }

            var lowData = days.Count > 0 ? days.Values.Min() : 0;
            var highData = days.Count > 0 ? days.Values.Max() : 1;
            var lo = s.Min ?? (lowData >= 0 ? 0 : lowData);
            var hi = s.Max ?? highData;

            if (!(hi > lo))
            {
                hi = lo + 1;
            }

            var bins = Palette.Light.Sequential.Count;

            int Bin(double v)
            {
                return Math.Clamp((int)Math.Floor((v - lo) / (hi - lo) * bins), 0, bins - 1);
            }

            var summary = "Calendar heatmap of " + days.Count + " days" + (days.Count > 0
                ? " from " + days.Keys.First().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " to " +
                  days.Keys.Last().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ", values from " +
                  format(lowData) + " to " + format(highData) + "."
                : ".");

            var probe = new ChartCanvas(s.Options with { Legend = LegendPosition.None }, id, summary, [], 0, [], true);
            var frame = probe.Frame(0, 0);
            var leftLabels = 32.0;
            var pitch = Math.Clamp((s.Options.Width - 2 * ChartCanvas.Pad - leftLabels) / 54.0, 6, 24);
            var stripHeight = 20 + 7 * pitch + 14;
            var legendHeight = 30.0;
            var strips = Math.Max(years.Count, 1);
            var height = Math.Min(4000, frame.Top + strips * stripHeight + legendHeight + ChartCanvas.Pad);
            var options = s.Options with { Legend = LegendPosition.None, Height = height };
            var canvas = new ChartCanvas(options, id, summary, [], 0, [], true);
            canvas.LimitInteractive(days.Count);
            canvas.Family = StyleFamily.Calendar;
            var plot = canvas.Frame(0, 0);
            canvas.Begin();

            var weekStart = s.WeekStartsOnSunday ? DayOfWeek.Sunday : DayOfWeek.Monday;

            int Row(DateTime d)
            {
                return ((int)d.DayOfWeek - (int)weekStart + 7) % 7;
            }

            var x0 = plot.Left + leftLabels;

            for (var yi = 0; yi < years.Count; yi++)
            {
                var year = years[yi];
                var top = plot.Top + yi * stripHeight;
                var jan1 = new DateTime(year, 1, 1);
                var offset = Row(jan1);

                canvas.W.Element("text", year.ToString(CultureInfo.InvariantCulture), ("class", "cyr"),
                    ("x", Num.F(plot.Left)),
                    ("y", Num.F(top + 11)));

                var lastMonthX = double.NegativeInfinity;

                for (var month = 1; month <= 12; month++)
                {
                    var first = new DateTime(year, month, 1);
                    var col = (first.DayOfYear - 1 + offset) / 7;
                    var mx = x0 + col * pitch;

                    if (mx < lastMonthX + 26 || mx < plot.Left + 44)
                    {
                        continue;
                    }

                    lastMonthX = mx;

                    canvas.W.Element("text", first.ToString("MMM", CultureInfo.InvariantCulture), ("class", "cmo"),
                        ("x", Num.F(mx)), ("y", Num.F(top + 11)));
                }

                var cellsTop = top + 20;

                foreach (var row in new[] { 0, 2, 4 })
                {
                    var day = weekStart + row;

                    canvas.W.Element("text", ((DayOfWeek)((int)day % 7)).ToString()[..3], ("class", "cmo"),
                        ("x", Num.F(plot.Left)),
                        ("y", Num.F(cellsTop + (row + 0.5) * pitch + 3.5)));
                }

                var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;

                for (var d = 0; d < daysInYear; d++)
                {
                    var date = jan1.AddDays(d);
                    var col = (d + offset) / 7;
                    var x = x0 + col * pitch;
                    var y = cellsTop + Row(date) * pitch;

                    if (days.TryGetValue(date, out var value))
                    {
                        var bin = Bin(value);
                        var label = date.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture);
                        var rows = new List<TipRow> { new(label, true), new(s.ValueName + ": " + format(value)) };
                        canvas.OpenMark(-1, string.Join(", ", rows.Select(r => r.Text)));

                        canvas.W.Empty("rect", ("class", "cell m q" + bin), ("x", Num.F(x)), ("y", Num.F(y)),
                            ("width", Num.F(pitch)), ("height", Num.F(pitch)), ("rx", "3"));

                        canvas.Tooltip(x + pitch / 2, y, rows);
                        canvas.CloseMark();
                    }
                    else
                    {
                        canvas.W.Empty("rect", ("class", "cell na"), ("x", Num.F(x)), ("y", Num.F(y)),
                            ("width", Num.F(pitch)),
                            ("height", Num.F(pitch)), ("rx", "3"));
                    }
                }
            }

            var ly = plot.Top + strips * stripHeight + 4;
            var swatch = 14.0;
            var barWidth = bins * (swatch + 2);
            var lx = plot.Right - barWidth - 60;
            canvas.W.Element("text", format(lo), ("class", "cmo end"), ("x", Num.F(lx - 6)), ("y", Num.F(ly + 11)));

            for (var b = 0; b < bins; b++)
            {
                canvas.W.Empty("rect", ("class", "cell q" + b), ("x", Num.F(lx + b * (swatch + 2))), ("y", Num.F(ly)),
                    ("width", Num.F(swatch)), ("height", Num.F(swatch)), ("rx", "3"));
            }

            canvas.W.Element("text", format(hi), ("class", "cmo"), ("x", Num.F(lx + barWidth + 4)),
                ("y", Num.F(ly + 11)));

            if (days.Count == 0)
            {
                canvas.Text(plot.CenterX, plot.Top + 40, "No data", "tk ctr");
            }

            return canvas.End();
        }
    }
}