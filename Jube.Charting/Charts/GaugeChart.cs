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
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class GaugeChart(GaugeChartSpec spec) : IStyleModeChart
    {
        public GaugeChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new GaugeChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private const double StartAngle = 135;
        private const double Sweep = 270;

        private static (double X, double Y) Point(double cx, double cy, double r, double degrees)
        {
            var a = degrees * Math.PI / 180;

            return (cx + Math.Cos(a) * r, cy + Math.Sin(a) * r);
        }

        private static string Arc(double cx, double cy, double outer, double inner, double from, double to)
        {
            var (ox0, oy0) = Point(cx, cy, outer, from);
            var (ox1, oy1) = Point(cx, cy, outer, to);
            var (ix1, iy1) = Point(cx, cy, inner, to);
            var (ix0, iy0) = Point(cx, cy, inner, from);
            var large = to - from > 180;

            return new PathBuilder().Move(ox0, oy0).Arc(outer, large, true, ox1, oy1).Line(ix1, iy1)
                .Arc(inner, large, false, ix0, iy0).Close().ToString();
        }

        private string Draw(string id)
        {
            var s = Spec;
            var format = s.ValueFormat ?? NumberFormat.Value;
            var min = s.Min;
            var max = s.Max > s.Min ? s.Max : s.Min + 1;
            var value = double.IsFinite(s.Value) ? s.Value : min;
            var t = Math.Clamp((value - min) / (max - min), 0, 1);

            var summary = "Gauge showing " + format(value) + " on a scale from " + format(min) + " to " + format(max) +
                          "." +
                          (s.Target is { } tg ? " Target " + format(tg) + "." : string.Empty);

            var canvas = new ChartCanvas(s.Options with { Legend = LegendPosition.None }, id, summary, [], 1, [], true);
            canvas.LimitInteractive(1);
            canvas.FramePlain(8);
            var plot = canvas.Plot;
            var radius = Math.Max(Math.Min(plot.Width / 2, plot.Height / 2 + 14) - 6, 40);
            var cx = plot.CenterX;
            var cy = plot.CenterY + radius * 0.12;
            var thickness = Math.Max(radius * 0.16, 10);
            var inner = radius - thickness;
            canvas.Begin();

            var rows = new List<TipRow> { new(s.Label ?? "Value", true, 0), new("Value: " + format(value)) };

            if (s.Target is { } target)
            {
                rows.Add(new TipRow("Target: " + format(target)));
            }

            if (s.Ranges.Count > 0)
            {
                var current = s.Ranges.FirstOrDefault(r => value >= r.From && value <= r.To);

                if (current?.Label is { } lbl)
                {
                    rows.Add(new TipRow("Range: " + lbl));
                }

                var ordered = s.Ranges.OrderBy(r => r.From).ToArray();

                for (var i = 0; i < ordered.Length; i++)
                {
                    var a = Math.Clamp((ordered[i].From - min) / (max - min), 0, 1);
                    var b = Math.Clamp((ordered[i].To - min) / (max - min), 0, 1);

                    if (b <= a)
                    {
                        continue;
                    }

                    var bin = ordered.Length == 1 ? 3 : 1 + (int)Math.Round(i * 6.0 / (ordered.Length - 1));

                    canvas.W.Open("path", ("class", "cell q" + bin),
                        ("d", Arc(cx, cy, radius, inner, StartAngle + a * Sweep, StartAngle + b * Sweep)));

                    canvas.W.Element("title",
                        (ordered[i].Label ?? "Range") + ": " + format(ordered[i].From) + " to " +
                        format(ordered[i].To));

                    canvas.W.Close();
                }
            }
            else
            {
                canvas.W.Empty("path", ("class", "gtrack"),
                    ("d", Arc(cx, cy, radius, inner, StartAngle, StartAngle + Sweep)));
            }

            canvas.OpenSeries(0);
            canvas.OpenMark(0, string.Join(", ", rows.Select(r => r.Text)));

            if (s.Ranges.Count == 0)
            {
                if (t > 0)
                {
                    canvas.W.Empty("path", ("class", "bar m"),
                        ("d", Arc(cx, cy, radius, inner, StartAngle, StartAngle + t * Sweep)));
                }
            }
            else
            {
                var (nx, ny) = Point(cx, cy, radius - 2, StartAngle + t * Sweep);
                var (bx0, by0) = Point(cx, cy, 6, StartAngle + t * Sweep + 90);
                var (bx1, by1) = Point(cx, cy, 6, StartAngle + t * Sweep - 90);

                canvas.W.Empty("path", ("class", "needle m"),
                    ("d", new PathBuilder().Move(nx, ny).Line(bx0, by0).Line(bx1, by1).Close().ToString()));

                canvas.W.Empty("circle", ("class", "hub"), ("cx", Num.F(cx)), ("cy", Num.F(cy)), ("r", "7"));
            }

            canvas.Tooltip(cx, cy - radius * 0.55, rows);
            canvas.CloseMark();
            canvas.W.Close();

            if (s.Target is { } tv && double.IsFinite(tv))
            {
                var tt = Math.Clamp((tv - min) / (max - min), 0, 1);
                var (tx0, ty0) = Point(cx, cy, radius + 5, StartAngle + tt * Sweep);
                var (tx1, ty1) = Point(cx, cy, inner - 3, StartAngle + tt * Sweep);

                canvas.W.Open("line", ("class", "tgt"), ("x1", Num.F(tx0)), ("y1", Num.F(ty0)), ("x2", Num.F(tx1)),
                    ("y2", Num.F(ty1)));

                canvas.W.Element("title", "Target: " + format(tv));
                canvas.W.Close();
            }

            var (lx, ly) = Point(cx, cy, (radius + inner) / 2, StartAngle);
            var (rx, ry) = Point(cx, cy, (radius + inner) / 2, StartAngle + Sweep);
            canvas.Text(lx, ly + 16, format(min), "tk ctr");
            canvas.Text(rx, ry + 16, format(max), "tk ctr");

            canvas.W.Element("text", TextMetrics.Truncate(format(value), inner * 1.6, 28, true), ("class", "big ctr"),
                ("x", Num.F(cx)), ("y", Num.F(cy + (s.Ranges.Count > 0 ? radius * 0.5 : 8))));

            if (!string.IsNullOrWhiteSpace(s.Label))
            {
                canvas.W.Element("text", TextMetrics.Truncate(s.Label, inner * 1.8, 12), ("class", "tk ctr"),
                    ("x", Num.F(cx)), ("y", Num.F(cy + (s.Ranges.Count > 0 ? radius * 0.5 + 18 : 28))));
            }

            return canvas.End();
        }
    }
}