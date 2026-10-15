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
using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Interfaces;
using Jube.Charting.Scales;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class GanttChart(GanttChartSpec spec) : IStyleModeChart
    {
        public GanttChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new GanttChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var tasks = s.Tasks;
            ChartLimits.Categories(tasks.Count);
            var groups = tasks.Select(t => t.Group).Where(g => g is not null).Distinct().Cast<string>().ToList();
            var extent = new Extent();

            foreach (var t in tasks)
            {
                extent.Add(TimeScale.ToSeconds(t.Start));
                extent.Add(TimeScale.ToSeconds(t.End));
            }

            if (s.Marker is { } m)
            {
                extent.Add(TimeScale.ToSeconds(m));
            }

            var legend = groups.Count > 1 ? groups.Select((g, i) => new LegendEntry(g, i)).ToList() : [];

            var summary = "Gantt chart with " + tasks.Count + " tasks" + (extent.Any
                ? " between " + TimeScale.FromSeconds(extent.Min).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                  " and " +
                  TimeScale.FromSeconds(extent.Max).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "."
                : ".");

            var canvas = new ChartCanvas(s.Options, id, summary, legend, Math.Max(groups.Count, 1), []);
            canvas.LimitInteractive(tasks.Count);
            var labelWidth = Math.Min(TextMetrics.MaxWidth(tasks.Select(t => t.Label), 11), canvas.Width * 0.3);
            var plot = canvas.Frame(labelWidth, 20);
            var lo = extent.Any ? extent.Min : 0;
            var hi = extent.Any ? extent.Max : 1;

            if (hi <= lo)
            {
                hi = lo + 86400;
            }

            var scale = new LinearScale(lo, hi, plot.Left, plot.Right);

            var tickData = TimeTicks.Generate(TimeScale.FromSeconds(lo), TimeScale.FromSeconds(hi),
                (int)Math.Clamp(plot.Width / 90, 2, 12));

            var ticks = tickData.Select(t => new Tick(t.Seconds, scale.Map(t.Seconds), t.Label)).ToArray();
            canvas.Begin();

            foreach (var t in ticks)
            {
                canvas.W.Empty("line", ("class", "gl"), ("x1", Num.F(t.Position)), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(t.Position)), ("y2", Num.F(plot.Bottom)));
            }

            canvas.XAxis(ticks);
            var band = new BandScale(Math.Max(tasks.Count, 1), plot.Top, plot.Bottom, 0.3);
            var height = Math.Min(band.Bandwidth, 26);

            for (var i = 0; i < tasks.Count; i++)
            {
                canvas.Text(plot.Left - 8, band.Center(i), TextMetrics.Truncate(tasks[i].Label, labelWidth, 11),
                    "tk end mid");
            }

            for (var g = 0; g < Math.Max(groups.Count, 1); g++)
            {
                canvas.OpenSeries(g);

                for (var i = 0; i < tasks.Count; i++)
                {
                    var t = tasks[i];
                    var gi = t.Group is null ? 0 : groups.IndexOf(t.Group);

                    if (gi != g)
                    {
                        continue;
                    }

                    var x0 = scale.Map(TimeScale.ToSeconds(t.Start));
                    var x1 = scale.Map(TimeScale.ToSeconds(t.End));
                    var cy = band.Center(i);
                    var span = t.End - t.Start;

                    var rows = new List<TipRow>
                    {
                        new(t.Label, true, g),
                        new("Start: " + t.Start.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
                        new("End: " + t.End.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
                        new("Duration: " + NumberFormat.Value(Math.Round(span.TotalDays, 2)) + " days")
                    };

                    if (double.IsFinite(t.Progress))
                    {
                        rows.Add(new TipRow("Progress: " + NumberFormat.Percent(Math.Clamp(t.Progress, 0, 1), 0)));
                    }

                    canvas.OpenMark(g, string.Join(", ", rows.Select(r => r.Text)));

                    if (span <= TimeSpan.Zero)
                    {
                        var d = new PathBuilder().Move(x0, cy - 9).Line(x0 + 9, cy).Line(x0, cy + 9).Line(x0 - 9, cy)
                            .Close();

                        canvas.W.Empty("path", ("class", "bar m"), ("d", d.ToString()));
                    }
                    else
                    {
                        var w = Math.Max(x1 - x0, 2);

                        canvas.W.Empty("rect",
                            ("class", "bar m" + (double.IsFinite(t.Progress) ? " part" : string.Empty)),
                            ("x", Num.F(x0)),
                            ("y", Num.F(cy - height / 2)), ("width", Num.F(w)), ("height", Num.F(height)), ("rx", "3"));

                        if (double.IsFinite(t.Progress))
                        {
                            canvas.W.Empty("rect", ("class", "bar m"), ("x", Num.F(x0)), ("y", Num.F(cy - height / 2)),
                                ("width", Num.F(w * Math.Clamp(t.Progress, 0, 1))), ("height", Num.F(height)),
                                ("rx", "3"));
                        }
                    }

                    canvas.Tooltip((x0 + x1) / 2, cy - height / 2, rows, false, 6);
                    canvas.CloseMark();
                }

                canvas.W.Close();
            }

            if (s.Marker is { } marker)
            {
                var mx = scale.Map(TimeScale.ToSeconds(marker));

                canvas.W.Open("line", ("class", "ref"), ("x1", Num.F(mx)), ("y1", Num.F(plot.Top)), ("x2", Num.F(mx)),
                    ("y2", Num.F(plot.Bottom)));

                canvas.W.Element("title", s.MarkerLabel ?? marker.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                canvas.W.Close();

                if (!string.IsNullOrEmpty(s.MarkerLabel))
                {
                    canvas.Text(mx + 4, plot.Top + 11, TextMetrics.Truncate(s.MarkerLabel, 120, 11), "tk");
                }
            }

            if (tasks.Count == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}