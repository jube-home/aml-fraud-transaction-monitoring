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

using Jube.Charting.Core;
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Jube.Charting.More.Models;
using Jube.Charting.Scales;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Theme.Models;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class ScatterMatrixChart(ScatterMatrixSpec spec) : IStyleModeChart
    {
        public const int MaxVariables = 12;
        private const int LinkedRowLimit = 150;

        public ScatterMatrixSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ScatterMatrixChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var vars = s.Variables;
            var n = vars.Count;

            if (n > MaxVariables)
            {
                throw new ChartLimitException("scatter matrix variable count", n, MaxVariables);
            }

            if (n is > 0 and < 2)
            {
                throw new ChartSpecificationException("A scatterplot matrix needs at least two variables.");
            }

            if (s.Bins is < 2 or > 60)
            {
                throw new ChartSpecificationException("Histogram bins must be between 2 and 60.");
            }

            var rows = n == 0 ? 0 : vars[0].Values.Count;

            if (vars.Any(v => v.Values.Count != rows))
            {
                throw new ChartSpecificationException("Every variable must have the same number of values.");
            }

            if (s.Groups is not null && s.Groups.Count != rows)
            {
                throw new ChartSpecificationException("Group labels must have one entry per row.");
            }

            var scatterCells = s.Upper == MatrixUpper.Scatter ? n * (n - 1) : n * (n - 1) / 2;
            ChartLimits.Points((long)rows * scatterCells);
            ChartLimits.Points((long)rows * n);
            var groupNames = new List<string>();
            var groupOf = new int[rows];

            if (s.Groups is not null)
            {
                var index = new Dictionary<string, int>();

                for (var r = 0; r < rows; r++)
                {
                    var g = s.Groups[r];

                    if (!index.TryGetValue(g, out var gi))
                    {
                        gi = groupNames.Count;
                        index[g] = gi;
                        groupNames.Add(g);
                    }

                    groupOf[r] = gi;
                }
            }
            else if (rows > 0)
            {
                groupNames.Add("All rows");
            }

            ChartLimits.Series(groupNames.Count);
            var legend = groupNames.Count > 1 ? groupNames.Select((g, i) => new LegendEntry(g, i)).ToList() : [];

            var summary = "Scatterplot matrix of " + n + " variables over " + rows + " rows: " +
                          MoreKit.Names([.. vars.Select(v => v.Name)], 8) + ".";

            var options = s.Options.LegendMaxRows > 0 ? s.Options : s.Options with { LegendMaxRows = 2 };
            var canvas = new ChartCanvas(options, id, summary, legend, groupNames.Count, [], true);

            var domains = new (double Min, double Max)[n];
            var ticks = new IReadOnlyList<double>[n];

            for (var v = 0; v < n; v++)
            {
                var e = new Extent();

                foreach (var x in vars[v].Values)
                {
                    e.Add(x);
                }

                var lo = e.Any ? e.Min : 0;
                var hi = e.Any ? e.Max : 1;
                var t = ExtendedTicks.Generate(lo, hi, 3, true);
                domains[v] = (t[0], t[^1]);

                if (!(domains[v].Max > domains[v].Min))
                {
                    domains[v] = (domains[v].Min - 1, domains[v].Max + 1);
                }

                var domain = domains[v];
                ticks[v] = [.. t.Where(x => x >= domain.Min - 1e-9 && x <= domain.Max + 1e-9)];
            }

            var labelWidth = n == 0
                ? 0
                : TextMetrics.MaxWidth(Enumerable.Range(0, n).SelectMany(v => NumberFormat.Axis(ticks[v])), 10);

            canvas.Family = StyleFamily.ScatterMatrix;
            var css = new CssRules(canvas.K);
            var linked = rows <= LinkedRowLimit;

            if (linked && !canvas.External)
            {
                for (var r = 0; r < rows; r++)
                {
                    css.Raw(DiagramRules.MatrixRow(canvas.K, r));
                }
            }

            canvas.ExtraCss = css.ToString();
            var plot = canvas.Frame(labelWidth, 14);
            var cw = plot.Width / Math.Max(n, 1);
            var ch = plot.Height / Math.Max(n, 1);
            var marks = (long)rows * scatterCells + n * s.Bins + (long)n * (n - 1) / 2;
            canvas.LimitInteractive(marks);
            canvas.Begin();

            if (n == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");

                return canvas.End();
            }

            var pad = 5.0;

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    var x0 = plot.Left + j * cw;
                    var y0 = plot.Top + i * ch;

                    canvas.W.Empty("rect", ("class", "cf"), ("x", Num.F(x0)), ("y", Num.F(y0)), ("width", Num.F(cw)),
                        ("height", Num.F(ch)));

                    if (i == j)
                    {
                        Histogram(canvas, s, vars[i], domains[i], groupOf, groupNames.Count, x0, y0, cw, ch, pad);
                    }
                    else if (i > j || s.Upper == MatrixUpper.Scatter)
                    {
                        Scatter(canvas, vars, i, j, domains, groupOf, groupNames.Count, linked, x0, y0, cw, ch, pad);
                    }
                    else if (s.Upper == MatrixUpper.Correlation)
                    {
                        Correlation(canvas, vars, i, j, x0, y0, cw, ch);
                    }
                }
            }

            for (var j = 0; j < n; j++)
            {
                var scale = new LinearScale(domains[j].Min, domains[j].Max, plot.Left + j * cw + pad,
                    plot.Left + (j + 1) * cw - pad);

                var labels = NumberFormat.Axis(ticks[j]);
                var last = double.NegativeInfinity;

                for (var t = 0; t < ticks[j].Count; t++)
                {
                    var px = scale.Map(ticks[j][t]);
                    var half = TextMetrics.Width(labels[t], 10) / 2;

                    if (px - half < last + 4 || px - half < plot.Left + j * cw + 1 ||
                        px + half > plot.Left + (j + 1) * cw - 1)
                    {
                        continue;
                    }

                    last = px + half;

                    canvas.W.Element("text", labels[t], ("class", "mtk ctr"), ("x", Num.F(px)),
                        ("y", Num.F(plot.Bottom + 12)));
                }
            }

            for (var i = 1; i < n; i++)
            {
                var scale = new LinearScale(domains[i].Min, domains[i].Max, plot.Top + (i + 1) * ch - pad,
                    plot.Top + i * ch + pad);

                var labels = NumberFormat.Axis(ticks[i]);

                for (var t = 0; t < ticks[i].Count; t++)
                {
                    canvas.W.Element("text", labels[t], ("class", "mtk end mid"), ("x", Num.F(plot.Left - 6)),
                        ("y", Num.F(scale.Map(ticks[i][t]))));
                }
            }

            return canvas.End();
        }

        private static void Histogram(ChartCanvas canvas, ScatterMatrixSpec s, MatrixVariable v,
            (double Min, double Max) domain, int[] groupOf, int groups, double x0, double y0, double cw, double ch,
            double pad)
        {
            var bins = s.Bins;
            var counts = new int[bins, Math.Max(groups, 1)];
            var width = (domain.Max - domain.Min) / bins;

            for (var r = 0; r < v.Values.Count; r++)
            {
                if (!double.IsFinite(v.Values[r]))
                {
                    continue;
                }

                var b = Math.Clamp((int)Math.Floor((v.Values[r] - domain.Min) / width), 0, bins - 1);
                counts[b, groups > 0 ? groupOf[r] : 0]++;
            }

            var max = 1;

            for (var b = 0; b < bins; b++)
            {
                var total = 0;

                for (var g = 0; g < Math.Max(groups, 1); g++)
                {
                    total += counts[b, g];
                }

                max = Math.Max(max, total);
            }

            var bw = (cw - 2 * pad) / bins;
            var innerTop = y0 + 22;
            var innerBottom = y0 + ch - pad;
            var scaleY = new LinearScale(0, max, innerBottom, innerTop);

            canvas.W.Element("text", TextMetrics.Truncate(v.Name, cw - 12, 12, true), ("class", "mvn"),
                ("x", Num.F(x0 + 6)),
                ("y", Num.F(y0 + 15)));

            for (var b = 0; b < bins; b++)
            {
                var total = 0;

                var rowsText = new List<TipRow>
                {
                    new(
                        v.Name + ": " + NumberFormat.Value(domain.Min + b * width) + " to " +
                        NumberFormat.Value(domain.Min + (b + 1) * width), true)
                };

                var stack = 0;
                var rects = new List<(int Group, double Top, double Bottom)>();

                for (var g = 0; g < Math.Max(groups, 1); g++)
                {
                    var c = counts[b, g];

                    if (c == 0)
                    {
                        continue;
                    }

                    rects.Add((g, scaleY.Map(stack + c), scaleY.Map(stack)));
                    stack += c;
                    total += c;

                    if (groups > 1)
                    {
                        rowsText.Add(new TipRow("Group " + g + ": " + c, false, g));
                    }
                }

                if (total == 0)
                {
                    continue;
                }

                rowsText.Add(new TipRow("Count: " + total));
                canvas.OpenMark(-1, string.Join(", ", rowsText.Select(t => t.Text)));

                foreach (var (g, top, bottom) in rects)
                {
                    canvas.W.Empty("rect", ("class", "bar s" + g % ChartLimits.MaxSeries),
                        ("x", Num.F(x0 + pad + b * bw + 0.5)),
                        ("y", Num.F(top)), ("width", Num.F(Math.Max(bw - 1, 0.5))),
                        ("height", Num.F(Math.Max(bottom - top, 0.5))));
                }

                canvas.Tooltip(x0 + pad + (b + 0.5) * bw, scaleY.Map(total), rowsText);
                canvas.CloseMark();
            }
        }

        private static void Scatter(ChartCanvas canvas, IReadOnlyList<MatrixVariable> vars, int i, int j,
            (double Min, double Max)[] domains, int[] groupOf, int groups, bool linked, double x0, double y0, double cw,
            double ch, double pad)
        {
            var sx = new LinearScale(domains[j].Min, domains[j].Max, x0 + pad, x0 + cw - pad);
            var sy = new LinearScale(domains[i].Min, domains[i].Max, y0 + ch - pad, y0 + pad);
            var radius = cw < 90 ? 2.2 : 3;

            for (var r = 0; r < vars[i].Values.Count; r++)
            {
                var xv = vars[j].Values[r];
                var yv = vars[i].Values[r];

                if (!double.IsFinite(xv) || !double.IsFinite(yv))
                {
                    continue;
                }

                var px = sx.Map(xv);
                var py = sy.Map(yv);
                var g = groups > 0 ? groupOf[r] : 0;

                if (canvas.Interactive)
                {
                    var rows = new List<TipRow>
                    {
                        new("Row " + (r + 1) + (groups > 1 ? " (group " + g + ")" : string.Empty), true,
                            groups > 1 ? g : null),
                        new(vars[j].Name + ": " + NumberFormat.Value(xv)),
                        new(vars[i].Name + ": " + NumberFormat.Value(yv))
                    };

                    canvas.OpenMark(g, string.Join(", ", rows.Select(t => t.Text)),
                        linked && StyleCaps.Keep(canvas.External, r, StyleCaps.MatrixRows) ? "r" + r : null);

                    canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(px)), ("cy", Num.F(py)), ("r", "7"));

                    canvas.W.Empty("circle", ("class", "pt dot"), ("cx", Num.F(px)), ("cy", Num.F(py)),
                        ("r", Num.F(radius)));

                    canvas.Tooltip(px, py - radius, rows);
                    canvas.CloseMark();
                }
                else
                {
                    canvas.W.Open("g", ("class", "s" + g % ChartLimits.MaxSeries));

                    canvas.W.Empty("circle", ("class", "pt dot"), ("cx", Num.F(px)), ("cy", Num.F(py)),
                        ("r", Num.F(radius)));

                    canvas.W.Close();
                }
            }
        }

        private static void Correlation(ChartCanvas canvas, IReadOnlyList<MatrixVariable> vars, int i, int j, double x0,
            double y0, double cw, double ch)
        {
            var r = ChartStats.Pearson(vars[j].Values, vars[i].Values, out var count);
            var bins = Palette.Light.Diverging.Count;

            var rows = new List<TipRow>
            {
                new(vars[j].Name + " and " + vars[i].Name, true),
                new("Correlation: " + (double.IsFinite(r) ? NumberFormat.Value(Math.Round(r, 3)) : "n/a")),
                new("Pairs: " + count)
            };

            canvas.OpenMark(-1, string.Join(", ", rows.Select(t => t.Text)));

            if (double.IsFinite(r))
            {
                var bin = Math.Clamp((int)Math.Floor((r + 1) / 2 * bins), 0, bins - 1);

                canvas.W.Empty("rect", ("class", "cell m d" + bin), ("x", Num.F(x0 + 1)), ("y", Num.F(y0 + 1)),
                    ("width", Num.F(cw - 2)), ("height", Num.F(ch - 2)));

                var size = Math.Clamp(10 + 12 * Math.Abs(r), 10, 22);

                canvas.W.Element("text", NumberFormat.Value(Math.Round(r, 2)),
                    ("class",
                        "lbl ctr mid dt" + bin + (canvas.External ? " fs" + (int)Math.Round(size) : string.Empty)),
                    ("x", Num.F(x0 + cw / 2)), ("y", Num.F(y0 + ch / 2)),
                    ("style", canvas.External ? null : "font-size:" + Num.F(size) + "px"));
            }
            else
            {
                canvas.W.Empty("rect", ("class", "cell m na"), ("x", Num.F(x0 + 1)), ("y", Num.F(y0 + 1)),
                    ("width", Num.F(cw - 2)), ("height", Num.F(ch - 2)));
            }

            canvas.Tooltip(x0 + cw / 2, y0 + ch / 2, rows);
            canvas.CloseMark();
        }
    }
}