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
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    internal static class PieRenderer
    {
        public static string Render(PieChartSpec spec, string id)
        {
            ChartLimits.Categories(spec.Slices.Count);
            var slices = spec.Slices.Where(s => double.IsFinite(s.Value) && s.Value > 0).ToList();
            var max = Math.Clamp(spec.MaxSlices, 2, 8);

            if (slices.Count > max)
            {
                var ordered = slices.OrderByDescending(s => s.Value).ToList();
                var keep = ordered.Take(max - 1).ToHashSet();
                var other = ordered.Where(s => !keep.Contains(s)).Sum(s => s.Value);
                slices =
                [
                    .. slices.Where(keep.Contains),
                    new PieSlice("Other", other)
                ];
            }

            var total = slices.Sum(s => s.Value);

            var options = spec.Options.Legend == LegendPosition.Auto && slices.Count >= 2
                ? spec.Options with { Legend = LegendPosition.Right }
                : spec.Options;

            var names = slices.Select(s => s.Label).ToArray();
            var colours = Guard.Colours(slices.Select(s => s.Color));
            var legend = names.Select((n, i) => new LegendEntry(n, i)).ToList();
            var format = spec.ValueFormat ?? NumberFormat.Value;

            var summary = Guard.Summary(spec.InnerRadiusRatio > 0 ? "Donut chart" : "Pie chart", names,
                total > 0 ? "Total " + format(total) + "." : "There is no data to plot.");

            var canvas = new ChartCanvas(options, id, summary, legend, slices.Count, colours);
            canvas.LimitInteractive(slices.Count);
            canvas.FramePlain();
            var plot = canvas.Plot;
            canvas.Begin();

            var cx = plot.CenterX;
            var cy = plot.CenterY;
            var radius = Math.Max(Math.Min(plot.Width, plot.Height) / 2 - 8, 20);
            var inner = radius * spec.InnerRadiusRatio;

            if (total <= 0)
            {
                canvas.Text(cx, cy, "No data", "tk ctr");

                return canvas.End();
            }

            var angle = -Math.PI / 2;

            for (var i = 0; i < slices.Count; i++)
            {
                var s = slices[i];
                var sweep = s.Value / total * 2 * Math.PI;
                var a0 = angle;
                var a1 = angle + sweep;
                angle = a1;
                var mid = (a0 + a1) / 2;
                var share = s.Value / total;

                var rows = new List<TipRow>
                {
                    new(s.Label, true, i),
                    new(format(s.Value) + " (" + NumberFormat.Percent(share) + ")")
                };

                canvas.OpenMark(i, string.Join(", ", rows.Select(r => r.Text)), "sr");
                var explode = 6.0;

                if (canvas.External)
                {
                    var direction = Math.Clamp(
                        (int)Math.Floor((Math.IEEERemainder(mid, 2 * Math.PI) + Math.PI) / (2 * Math.PI) *
                                        StyleCaps.PieDirections),
                        0, StyleCaps.PieDirections - 1);

                    canvas.W.Empty("path", ("class", "sl m px" + direction),
                        ("d", SlicePath(cx, cy, radius, inner, a0, a1)));
                }
                else
                {
                    canvas.W.Empty("path", ("class", "sl m"), ("d", SlicePath(cx, cy, radius, inner, a0, a1)),
                        ("style",
                            "--dx:" + Num.F(Math.Cos(mid) * explode) + "px;--dy:" + Num.F(Math.Sin(mid) * explode) +
                            "px"));
                }

                if (share >= 0.06)
                {
                    var lr = inner > 0 ? (radius + inner) / 2 : radius * 0.66;
                    var text = NumberFormat.Percent(share, share < 0.1 ? 1 : 0);

                    canvas.W.Element("text", text, ("class", "lbl ctr mid on"), ("x", Num.F(cx + Math.Cos(mid) * lr)),
                        ("y", Num.F(cy + Math.Sin(mid) * lr)));
                }

                canvas.Tooltip(cx + Math.Cos(mid) * radius * 0.8, cy + Math.Sin(mid) * radius * 0.8, rows);
                canvas.CloseMark();
            }

            if (inner > 0)
            {
                var label = spec.CenterLabel ?? format(total);

                canvas.W.Element("text", TextMetrics.Truncate(label, inner * 1.6, 16, true),
                    ("class", "ttl ctr mid"), ("x", Num.F(cx)), ("y", Num.F(cy - 6)));

                canvas.W.Element("text", spec.CenterLabel is null ? "Total" : null, ("class", "tk ctr mid"),
                    ("x", Num.F(cx)), ("y", Num.F(cy + 12)));
            }

            return canvas.End();
        }

        private static string SlicePath(double cx, double cy, double r, double inner, double a0, double a1)
        {
            var full = a1 - a0 >= 2 * Math.PI - 1e-6;
            var p = new PathBuilder();

            if (full)
            {
                p.Move(cx, cy - r).Arc(r, true, true, cx, cy + r).Arc(r, true, true, cx, cy - r).Close();

                if (inner > 0)
                {
                    p.Move(cx, cy - inner).Arc(inner, true, false, cx, cy + inner)
                        .Arc(inner, true, false, cx, cy - inner)
                        .Close();
                }

                return p.ToString();
            }

            var large = a1 - a0 > Math.PI;
            var x0 = cx + Math.Cos(a0) * r;
            var y0 = cy + Math.Sin(a0) * r;
            var x1 = cx + Math.Cos(a1) * r;
            var y1 = cy + Math.Sin(a1) * r;
            p.Move(x0, y0).Arc(r, large, true, x1, y1);

            if (inner > 0)
            {
                p.Line(cx + Math.Cos(a1) * inner, cy + Math.Sin(a1) * inner)
                    .Arc(inner, large, false, cx + Math.Cos(a0) * inner, cy + Math.Sin(a0) * inner);
            }
            else
            {
                p.Line(cx, cy);
            }

            return p.Close().ToString();
        }
    }
}