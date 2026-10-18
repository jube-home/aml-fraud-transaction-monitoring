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
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class PolarChart(PolarChartSpec spec) : IStyleModeChart
    {
        public PolarChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new PolarChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            ChartLimits.Series(s.Series.Count);
            ChartLimits.Points(s.Series.Sum(x => (long)x.Points.Count));
            var format = s.ValueFormat ?? NumberFormat.Value;
            var extent = new Extent();
            extent.Add(0);

            foreach (var p in s.Series.SelectMany(x => x.Points))
            {
                if (double.IsFinite(p.X))
                {
                    extent.Add(p.Y);
                }
            }

            var ringValues = ExtendedTicks.Generate(0, extent.Max > 0 ? extent.Max : 1, 4, true).Where(v => v >= 0)
                .ToList();

            var hi = ringValues[^1];
            var spokes = Math.Clamp(s.Spokes, 4, 36);
            var names = s.Series.Select(x => x.Name).ToArray();
            var colours = Guard.Colours(s.Series.Select(x => x.Color));
            var legend = names.Select((nm, i) => new LegendEntry(nm, i)).ToList();
            var summary = Guard.Summary("Polar chart", names, "Radius up to " + format(hi) + ".");
            var canvas = new ChartCanvas(s.Options, id, summary, legend, s.Series.Count, colours);
            canvas.LimitInteractive(s.Series.Sum(x => (long)x.Points.Count));
            canvas.FramePlain(8);
            var plot = canvas.Plot;
            var radius = Math.Max(Math.Min(plot.Width / 2 - 30, plot.Height / 2 - 24), 30);
            var cx = plot.CenterX;
            var cy = plot.CenterY;
            canvas.Begin();

            double Angle(int i)
            {
                return -Math.PI / 2 + 2 * Math.PI * i / spokes;
            }

            RadialGrid.Draw(canvas, cx, cy, radius, spokes, Angle,
                [.. ringValues.Select(v => (v / hi * radius, format(v)))],
                [.. Enumerable.Range(0, spokes).Select(i => NumberFormat.Value(360.0 * i / spokes) + "°")], 0);

            for (var si = 0; si < s.Series.Count; si++)
            {
                var series = s.Series[si];

                var pts = series.Points.Select(p => double.IsFinite(p.X) && double.IsFinite(p.Y)
                    ? (X: cx + Math.Sin(p.X * Math.PI / 180) * p.Y / hi * radius,
                        Y: cy - Math.Cos(p.X * Math.PI / 180) * p.Y / hi * radius)
                    : (X: double.NaN, Y: double.NaN)).ToArray();

                canvas.OpenSeries(si);

                if (s.Mode != PolarMode.Scatter)
                {
                    var path = new PathBuilder();

                    foreach (var p in pts)
                    {
                        path.Line(p.X, p.Y);
                    }

                    if (s.Mode == PolarMode.Area && pts.All(p => double.IsFinite(p.X)) && pts.Length >= 3)
                    {
                        var area = new PathBuilder();

                        foreach (var p in pts)
                        {
                            area.Line(p.X, p.Y);
                        }

                        canvas.W.Empty("path", ("class", "ar"), ("d", area.Close().ToString()));
                    }

                    if (!path.IsEmpty)
                    {
                        canvas.W.Open("path", ("class", "ln"), ("d", path.ToString()));
                        canvas.W.Element("title", series.Name);
                        canvas.W.Close();
                    }
                }

                for (var i = 0; i < pts.Length; i++)
                {
                    if (!double.IsFinite(pts[i].X))
                    {
                        continue;
                    }

                    var rows = new List<TipRow>();

                    if (!string.IsNullOrEmpty(series.Points[i].Label))
                    {
                        rows.Add(new TipRow(series.Points[i].Label!, true));
                    }

                    rows.Add(new TipRow(series.Name, true, si));
                    rows.Add(new TipRow("Angle: " + NumberFormat.Value(series.Points[i].X) + "°"));
                    rows.Add(new TipRow("Radius: " + format(series.Points[i].Y)));
                    canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(pts[i].X)), ("cy", Num.F(pts[i].Y)),
                        ("r", "9"));

                    canvas.W.Empty("circle", ("class", "pt"), ("cx", Num.F(pts[i].X)), ("cy", Num.F(pts[i].Y)),
                        ("r", s.Mode == PolarMode.Scatter ? "4.5" : "3.5"));

                    canvas.Tooltip(pts[i].X, pts[i].Y - 4, rows);
                    canvas.CloseMark();
                }

                canvas.W.Close();
            }

            return canvas.End();
        }
    }
}