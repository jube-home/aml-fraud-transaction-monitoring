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
    internal static class HeatRenderer
    {
        public static string Render(HeatModel m, string id)
        {
            var nr = m.Rows.Count;
            var nc = m.Cols.Count;
            ChartLimits.Cells((long)nr * nc);
            ChartLimits.Categories(Math.Max(nr, nc));
            var summary = (m.Kind + " with " + nr + " rows and " + nc + " columns. " + m.Detail).Trim();
            var canvas = new ChartCanvas(m.Options, id, summary, [], 0, [], true);
            canvas.LimitInteractive((long)nr * nc);

            var rowWidth = Math.Min(TextMetrics.MaxWidth(m.Rows, 11), canvas.Width * 0.25);
            var colWidthMax = TextMetrics.MaxWidth(m.Cols, 11);
            var legendReserve = 76.0;
            var plot = canvas.Frame(rowWidth, 20, legendReserve);
            var cw = plot.Width / Math.Max(nc, 1);
            var rotate = colWidthMax > cw - 4;
            var bottom = rotate ? Math.Min(colWidthMax, 100) * 0.64 + 14 : 20;
            plot = canvas.Frame(rowWidth, bottom, legendReserve);
            cw = plot.Width / Math.Max(nc, 1);
            var ch = plot.Height / Math.Max(nr, 1);
            canvas.Begin();

            var (lo, hi) = m.Domain;
            var bins = m.Diverging ? Palette.Light.Diverging.Count : Palette.Light.Sequential.Count;
            var prefix = m.Diverging ? "d" : "q";
            var textPrefix = m.Diverging ? "dt" : "qt";

            int Bin(double v)
            {
                var t = (v - lo) / (hi - lo);

                return Math.Clamp((int)Math.Floor(t * bins), 0, bins - 1);
            }

            for (var r = 0; r < nr; r++)
            {
                canvas.Text(plot.Left - 8, plot.Top + (r + 0.5) * ch, TextMetrics.Truncate(m.Rows[r], rowWidth, 11),
                    "tk end mid");
            }

            for (var c = 0; c < nc; c++)
            {
                var x = plot.Left + (c + 0.5) * cw;

                if (rotate)
                {
                    var label = TextMetrics.Truncate(m.Cols[c], 100, 11);

                    canvas.W.Element("text", label, ("class", "tk end"), ("x", Num.F(x + 4)),
                        ("y", Num.F(plot.Bottom + 12)),
                        ("transform", "rotate(-40 " + Num.F(x + 4) + " " + Num.F(plot.Bottom + 12) + ")"));
                }
                else
                {
                    canvas.Text(x, plot.Bottom + 16, TextMetrics.Truncate(m.Cols[c], cw - 4, 11), "tk ctr");
                }
            }

            for (var r = 0; r < nr; r++)
            {
                for (var c = 0; c < nc; c++)
                {
                    var v = m.Values[r][c];
                    var x = plot.Left + c * cw;
                    var y = plot.Top + r * ch;
                    var rows = m.Tip(r, c);
                    canvas.OpenMark(-1, string.Join(", ", rows.Select(t => t.Text)));

                    if (!double.IsFinite(v))
                    {
                        canvas.W.Empty("rect", ("class", "cell m na"), ("x", Num.F(x)), ("y", Num.F(y)),
                            ("width", Num.F(cw)), ("height", Num.F(ch)));
                    }
                    else
                    {
                        var bin = Bin(v);

                        canvas.W.Empty("rect", ("class", "cell m " + prefix + bin), ("x", Num.F(x)), ("y", Num.F(y)),
                            ("width", Num.F(cw)), ("height", Num.F(ch)));

                        var lines = m.CellText(r, c);

                        if (lines.Length > 0 && cw >= 30 && ch >= 15 * lines.Length + 4)
                        {
                            var widest = lines.Max(l => TextMetrics.Width(l, 11));

                            if (widest <= cw - 4)
                            {
                                for (var k = 0; k < lines.Length; k++)
                                {
                                    var ty = y + ch / 2 + (k - (lines.Length - 1) / 2.0) * 14;

                                    canvas.W.Element("text", lines[k], ("class", "lbl ctr mid " + textPrefix + bin),
                                        ("x", Num.F(x + cw / 2)), ("y", Num.F(ty)));
                                }
                            }
                        }
                    }

                    canvas.Tooltip(x + cw / 2, y + ch / 2, rows);
                    canvas.CloseMark();
                }
            }

            DrawLegendBar(canvas, m, plot, bins, prefix, lo, hi);
            canvas.AxisTitles(bottom);

            if (nr == 0 || nc == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            return canvas.End();
        }

        private static void DrawLegendBar(ChartCanvas canvas, HeatModel m, Rect plot, int bins, string prefix,
            double lo,
            double hi)
        {
            var x = plot.Right + 16;
            var height = Math.Min(plot.Height, 220);
            var top = plot.Top;
            var block = height / bins;

            for (var i = 0; i < bins; i++)
            {
                canvas.W.Empty("rect", ("class", prefix + (bins - 1 - i)), ("x", Num.F(x)),
                    ("y", Num.F(top + i * block)),
                    ("width", "12"), ("height", Num.F(block + 0.5)));
            }

            if (m.LegendTitle is not null)
            {
                canvas.Text(x, top - 14, TextMetrics.Truncate(m.LegendTitle, 70, 11), "tk");
            }

            var ticks = ExtendedTicks.Generate(lo, hi, Math.Clamp((int)(height / 40), 2, 6))
                .Where(v => v >= lo - 1e-9 && v <= hi + 1e-9).ToArray();

            var lastY = double.NegativeInfinity;

            foreach (var v in ticks)
            {
                var y = top + (1 - (v - lo) / (hi - lo)) * height;

                if (Math.Abs(y - lastY) < 14)
                {
                    continue;
                }

                lastY = y;

                canvas.W.Empty("line", ("class", "ax"), ("x1", Num.F(x + 12)), ("y1", Num.F(y)), ("x2", Num.F(x + 16)),
                    ("y2", Num.F(y)));

                canvas.W.Element("text", TextMetrics.Truncate(m.LegendFormat(v), 56, 11), ("class", "tk mid"),
                    ("x", Num.F(x + 20)), ("y", Num.F(y)));
            }
        }
    }
}