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
    public sealed class TreemapChart(TreemapChartSpec spec) : IStyleModeChart
    {
        public TreemapChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new TreemapChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private const double HeaderHeight = 18;

        private static double Total(TreemapNode node)
        {
            if (node.Children is { Count: > 0 })
            {
                return node.Children.Sum(c => Math.Max(Total(c), 0));
            }

            return double.IsFinite(node.Value) && node.Value > 0 ? node.Value : 0;
        }

        private string Draw(string id)
        {
            var s = Spec;
            var format = s.ValueFormat ?? NumberFormat.Value;
            var top = s.Nodes.Where(n => Total(n) > 0).OrderByDescending(Total).ToList();
            ChartLimits.Categories(top.Count);
            long count = 0;

            foreach (var n in top)
            {
                count += 1 + (n.Children?.Count ?? 0);
            }

            ChartLimits.Points(count);
            var grand = top.Sum(Total);
            var grouped = top.Any(n => n.Children is { Count: > 0 });

            var legend = grouped
                ? top.Select((n, i) => new LegendEntry(n.Label, i)).ToList()
                : [];

            var summary = "Treemap of " + top.Count + (grouped ? " groups" : " items") + " totalling " + format(grand) +
                          ". Largest: " +
                          string.Join(", ",
                              top.Take(5).Select(n => TextMetrics.Truncate(n.Label, 80, 12) + " " + format(Total(n)))) +
                          ".";

            var canvas = new ChartCanvas(
                s.Options with
                {
                    Legend = grouped && top.Count is > 1 and <= 8 ? s.Options.Legend : LegendPosition.None
                },
                id, summary, legend, top.Count, [], true);
            canvas.LimitInteractive(count);
            canvas.FramePlain(8);
            var plot = canvas.Plot;
            canvas.Begin();

            if (grand <= 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");

                return canvas.End();
            }

            var rects = Squarify([.. top.Select(Total)], plot.Left, plot.Top, plot.Width, plot.Height);

            var maxLeaf = top.SelectMany(n => n.Children is { Count: > 0 } ? n.Children : [n]).Select(Total)
                .DefaultIfEmpty(1).Max();

            for (var g = 0; g < top.Count; g++)
            {
                var node = top[g];
                var r = rects[g];

                if (node.Children is { Count: > 0 })
                {
                    canvas.OpenSeries(g % 8);
                    var kids = node.Children.Where(c => Total(c) > 0).OrderByDescending(Total).ToList();
                    var header = r is { Height: > HeaderHeight * 2, Width: > 50 } ? HeaderHeight : 0;

                    canvas.W.Empty("rect", ("class", "tgroup"), ("x", Num.F(r.X)), ("y", Num.F(r.Y)),
                        ("width", Num.F(r.Width)), ("height", Num.F(r.Height)));

                    if (header > 0)
                    {
                        canvas.Text(r.X + 6, r.Y + 13, TextMetrics.Truncate(node.Label, r.Width - 12, 11, true), "lbl");
                    }

                    var inner = Squarify([.. kids.Select(Total)], r.X, r.Y + header, r.Width, r.Height - header);

                    for (var k = 0; k < kids.Count; k++)
                    {
                        var rows = new List<TipRow>
                        {
                            new(node.Label + " / " + kids[k].Label, true, g % 8),
                            new("Value: " + format(Total(kids[k]))),
                            new("Of group: " + NumberFormat.Percent(Total(kids[k]) / Total(node))),
                            new("Of total: " + NumberFormat.Percent(Total(kids[k]) / grand))
                        };

                        DrawLeaf(canvas, inner[k], kids[k].Label, rows, "cell bar m", "on", format(Total(kids[k])));
                    }

                    canvas.W.Close();
                }
                else
                {
                    var bin = (int)Math.Clamp(Math.Floor(Total(node) / maxLeaf * 8.999), 0, 8);

                    var rows = new List<TipRow>
                    {
                        new(node.Label, true),
                        new("Value: " + format(Total(node))),
                        new("Of total: " + NumberFormat.Percent(Total(node) / grand))
                    };

                    DrawLeaf(canvas, r, node.Label, rows, "cell m q" + bin, "qt" + bin, format(Total(node)));
                }
            }

            return canvas.End();
        }

        private static void DrawLeaf(ChartCanvas canvas, (double X, double Y, double Width, double Height) r,
            string label,
            List<TipRow> rows, string cls, string textCls, string value)
        {
            canvas.OpenMark(-1, string.Join(", ", rows.Select(t => t.Text)));

            canvas.W.Empty("rect", ("class", cls), ("x", Num.F(r.X)), ("y", Num.F(r.Y)),
                ("width", Num.F(Math.Max(r.Width, 0))),
                ("height", Num.F(Math.Max(r.Height, 0))));

            if (r is { Width: > 44, Height: > 22 })
            {
                canvas.W.Element("text", TextMetrics.Truncate(label, r.Width - 10, 11, true),
                    ("class", "lbl " + textCls),
                    ("x", Num.F(r.X + 6)), ("y", Num.F(r.Y + 15)));

                if (r.Height > 38)
                {
                    canvas.W.Element("text", TextMetrics.Truncate(value, r.Width - 10, 11), ("class", "lbl " + textCls),
                        ("x", Num.F(r.X + 6)), ("y", Num.F(r.Y + 29)));
                }
            }

            canvas.Tooltip(r.X + r.Width / 2, r.Y + r.Height / 2, rows);
            canvas.CloseMark();
        }

        internal static List<(double X, double Y, double Width, double Height)> Squarify(IReadOnlyList<double> values,
            double x, double y, double w, double h)
        {
            var result = new (double X, double Y, double Width, double Height)[values.Count];
            var total = values.Sum();

            if (total <= 0 || w <= 0 || h <= 0)
            {
                return [.. result];
            }

            var areas = values.Select(v => v / total * w * h).ToArray();
            var i = 0;

            while (i < areas.Length)
            {
                var side = Math.Min(w, h);
                var row = new List<int> { i };
                var sum = areas[i];
                var worst = Worst(areas, row, sum, side);
                var j = i + 1;

                while (j < areas.Length)
                {
                    var candidateSum = sum + areas[j];
                    row.Add(j);
                    var next = Worst(areas, row, candidateSum, side);

                    if (next > worst)
                    {
                        row.RemoveAt(row.Count - 1);
                        break;
                    }

                    sum = candidateSum;
                    worst = next;
                    j++;
                }

                var thickness = sum / side;
                var offset = 0.0;

                foreach (var k in row)
                {
                    var length = areas[k] / thickness;
                    result[k] = w >= h ? (x, y + offset, thickness, length) : (x + offset, y, length, thickness);
                    offset += length;
                }

                if (w >= h)
                {
                    x += thickness;
                    w -= thickness;
                }
                else
                {
                    y += thickness;
                    h -= thickness;
                }

                i = j;
            }

            return [.. result];
        }

        private static double Worst(double[] areas, List<int> row, double sum, double side)
        {
            var max = row.Max(k => areas[k]);
            var min = row.Min(k => areas[k]);
            var s2 = side * side;

            return Math.Max(s2 * max / (sum * sum), sum * sum / (s2 * min));
        }
    }
}