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

using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Interfaces;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Ticks;
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class RadarChart(RadarChartSpec spec) : IStyleModeChart
    {
        public RadarChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new RadarChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var n = s.Axes.Count;
            ChartLimits.Series(s.Series.Count);
            ChartLimits.Categories(n);
            ChartLimits.Points((long)n * s.Series.Count);

            foreach (var series in s.Series)
            {
                if (series.Values.Count != n)
                {
                    throw new ChartSpecificationException("Every radar series needs one value per axis.");
                }
            }

            var format = s.ValueFormat ?? NumberFormat.Value;
            var extent = new Extent();
            extent.Add(0);

            foreach (var v in s.Series.SelectMany(x => x.Values))
            {
                extent.Add(v);
            }

            var lo = s.Min ?? Math.Min(extent.Min, 0);
            var hi = s.Max ?? (extent.Any ? extent.Max : 1);

            if (hi <= lo)
            {
                hi = lo + 1;
            }

            var ringValues = ExtendedTicks.Generate(lo, hi, 4, true).ToList();
            lo = ringValues[0];
            hi = ringValues[^1];
            var names = s.Series.Select(x => x.Name).ToArray();
            var colours = Guard.Colours(s.Series.Select(x => x.Color));
            var legend = names.Select((nm, i) => new LegendEntry(nm, i)).ToList();

            var summary = Guard.Summary("Radar chart", names, n + " axes: " +
                                                              string.Join(", ",
                                                                  s.Axes.Take(8)
                                                                      .Select(a => TextMetrics.Truncate(a, 80, 12))) +
                                                              ".");

            var canvas = new ChartCanvas(s.Options, id, summary, legend, s.Series.Count, colours);
            canvas.LimitInteractive((long)n * s.Series.Count);
            canvas.FramePlain(8);
            var plot = canvas.Plot;
            var labelRoom = 70.0;
            var radius = Math.Max(Math.Min(plot.Width / 2 - labelRoom, plot.Height / 2 - 24), 30);
            var cx = plot.CenterX;
            var cy = plot.CenterY;
            canvas.Begin();

            double Angle(int i)
            {
                return -Math.PI / 2 + 2 * Math.PI * i / Math.Max(n, 1);
            }

            double Radius(double v)
            {
                return (v - lo) / (hi - lo) * radius;
            }

            RadialGrid.Draw(canvas, cx, cy, radius, n, Angle, [.. ringValues.Select(v => (Radius(v), format(v)))],
                [.. s.Axes], 0);

            for (var si = 0; si < s.Series.Count; si++)
            {
                var series = s.Series[si];

                var pts = series.Values.Select((v, i) => double.IsFinite(v)
                    ? (X: cx + Math.Cos(Angle(i)) * Radius(v), Y: cy + Math.Sin(Angle(i)) * Radius(v))
                    : (X: double.NaN, Y: double.NaN)).ToArray();

                canvas.OpenSeries(si);
                var complete = pts.All(p => double.IsFinite(p.X)) && n >= 3;
                var path = new PathBuilder();

                for (var i = 0; i < n; i++)
                {
                    path.Line(pts[i].X, pts[i].Y);
                }

                if (complete)
                {
                    path.Close();
                    canvas.W.Empty("path", ("class", s.Filled ? "ar" : "ar off"), ("d", path.ToString()));
                }

                if (!path.IsEmpty)
                {
                    canvas.W.Open("path", ("class", "ln"), ("d", path.ToString()));
                    canvas.W.Element("title", series.Name);
                    canvas.W.Close();
                }

                for (var i = 0; i < n; i++)
                {
                    if (!double.IsFinite(pts[i].X))
                    {
                        continue;
                    }

                    var rows = new List<TipRow>
                    {
                        new(s.Axes[i], true),
                        new(series.Name + ": " + format(series.Values[i]), false, si)
                    };

                    canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(pts[i].X)), ("cy", Num.F(pts[i].Y)),
                        ("r", "9"));
                    canvas.W.Empty("circle", ("class", "pt"), ("cx", Num.F(pts[i].X)), ("cy", Num.F(pts[i].Y)),
                        ("r", "3.5"));

                    canvas.Tooltip(pts[i].X, pts[i].Y - 4, rows);
                    canvas.CloseMark();
                }

                canvas.W.Close();
            }

            if (n < 3)
            {
                canvas.Text(cx, cy, "A radar chart needs at least three axes", "tk ctr");
            }

            return canvas.End();
        }
    }
}