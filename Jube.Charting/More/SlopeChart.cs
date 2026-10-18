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
using Jube.Charting.Theme.Models;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class SlopeChart(SlopeChartSpec spec) : IStyleModeChart
    {
        public SlopeChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new SlopeChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        public static double[][] Rank(IReadOnlyList<IReadOnlyList<double>> valuesBySeries, bool lowestIsBest)
        {
            var series = valuesBySeries.Count;
            var periods = series == 0 ? 0 : valuesBySeries.Max(v => v.Count);

            var ranks = Enumerable.Range(0, series).Select(_ => Enumerable.Repeat(double.NaN, periods).ToArray())
                .ToArray();

            for (var p = 0; p < periods; p++)
            {
                var present = Enumerable.Range(0, series)
                    .Where(k => p < valuesBySeries[k].Count && double.IsFinite(valuesBySeries[k][p])).ToArray();

                foreach (var k in present)
                {
                    var v = valuesBySeries[k][p];

                    ranks[k][p] =
                        1 + present.Count(o => lowestIsBest ? valuesBySeries[o][p] < v : valuesBySeries[o][p] > v);
                }
            }

            return ranks;
        }

        private string Draw(string id)
        {
            var s = Spec;
            var periods = s.Periods;
            var m = s.Series.Count;
            ChartLimits.Series(m);
            ChartLimits.Categories(periods.Count);
            ChartLimits.Points((long)m * periods.Count);

            if (periods.Count < 2 && m > 0)
            {
                throw new ChartSpecificationException("A slope or bump chart needs at least two periods.");
            }

            foreach (var series in s.Series)
            {
                if (series.Values.Count != periods.Count)
                {
                    throw new ChartSpecificationException("Every series needs one value per period.");
                }
            }

            var format = s.ValueFormat ?? NumberFormat.Value;
            var ranks = Rank([.. s.Series.Select(x => x.Values)], s.LowestIsBest);
            var ranked = s.Mode == SlopeMode.Ranks;
            var names = s.Series.Select(x => x.Name).ToArray();
            var colours = Guard.Colours(s.Series.Select(x => x.Color));
            var kind = ranked ? "Bump chart" : "Slope chart";

            var summary = Guard.Summary(kind, names, "Across " + periods.Count + " periods: " +
                                                     MoreKit.Names([.. periods], 4) + ".");

            var canvas = new ChartCanvas(
                s.Options with
                {
                    Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.None : s.Options.Legend
                },
                id, summary, [.. s.Series.Select((x, i) => new LegendEntry(x.Name, i, LegendShape.Line))], m,
                colours)
            {
                Family = StyleFamily.Slope
            };

            canvas.LimitInteractive((long)m * periods.Count);

            string LeftLabel(int k)
            {
                return ranked ? names[k] : names[k] + "  " + format(s.Series[k].Values[0]);
            }

            string RightLabel(int k)
            {
                return ranked ? names[k] : format(s.Series[k].Values[^1]) + "  " + names[k];
            }

            var leftWidth = m == 0
                ? 0
                : Math.Min(
                    TextMetrics.MaxWidth(
                        Enumerable.Range(0, m).Where(k => double.IsFinite(s.Series[k].Values[0])).Select(LeftLabel),
                        11),
                    canvas.Width * 0.24);

            var rightWidth = m == 0
                ? 0
                : Math.Min(
                    TextMetrics.MaxWidth(
                        Enumerable.Range(0, m).Where(k => double.IsFinite(s.Series[k].Values[^1])).Select(RightLabel),
                        11),
                    canvas.Width * 0.24);

            var labelOffset = ranked ? 16.0 : 9.0;
            var frame = canvas.Frame(leftWidth + labelOffset, 0, rightWidth + labelOffset + 4);
            var headerHeight = 24.0;
            var margin = ranked ? 12.0 : 6.0;
            var plot = new Rect(frame.Left, frame.Top + headerHeight, frame.Right, frame.Bottom - 4);
            canvas.SetPlot(plot);
            canvas.Begin();

            var count = periods.Count;

            var xs = Enumerable.Range(0, count)
                .Select(i => count == 1 ? plot.CenterX : plot.Left + plot.Width * i / (count - 1)).ToArray();

            IScale y;

            var maxRank = ranked && m > 0
                ? Math.Max(1, ranks.SelectMany(r => r).Where(double.IsFinite).DefaultIfEmpty(1).Max())
                : 1;

            if (ranked)
            {
                y = new LinearScale(1, maxRank <= 1 ? 2 : maxRank, plot.Top + margin, plot.Bottom - margin);
            }
            else
            {
                var e = new Extent();

                foreach (var series in s.Series)
                {
                    foreach (var v in series.Values)
                    {
                        e.Add(v);
                    }
                }

                var (lo, hi) = e.Any ? MoreKit.NiceDomain(e.Min, e.Max, 4) : (0.0, 1.0);
                y = new LinearScale(lo, hi, plot.Bottom - margin, plot.Top + margin);
            }

            for (var i = 0; i < count; i++)
            {
                canvas.W.Empty("line", ("class", "gl"), ("x1", Num.F(xs[i])), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(xs[i])),
                    ("y2", Num.F(plot.Bottom)));

                canvas.W.Element("text",
                    TextMetrics.Truncate(periods[i], Math.Max(plot.Width / Math.Max(count - 1, 1) - 8, 30), 12, true),
                    ("class", "ph ctr"), ("x", Num.F(xs[i])), ("y", Num.F(plot.Top - 8)));
            }

            double Y(int k, int p)
            {
                return y.Map(ranked ? ranks[k][p] : s.Series[k].Values[p]);
            }

            var radius = ranked ? 9.0 : 4.5;

            for (var k = 0; k < m; k++)
            {
                canvas.OpenSeries(k);
                var path = new PathBuilder();

                for (var p = 0; p < count; p++)
                {
                    var py = Y(k, p);

                    if (!double.IsFinite(py))
                    {
                        path.Gap();
                        continue;
                    }

                    if (s.Smooth && p > 0 && double.IsFinite(Y(k, p - 1)))
                    {
                        var midX = (xs[p - 1] + xs[p]) / 2;
                        path.Cubic(midX, Y(k, p - 1), midX, py, xs[p], py);
                    }
                    else
                    {
                        path.Line(xs[p], py);
                    }
                }

                if (!path.IsEmpty)
                {
                    canvas.W.Open("path", ("class", "ln"), ("d", path.ToString()));
                    canvas.W.Element("title", names[k]);
                    canvas.W.Close();
                    canvas.W.Empty("path", ("class", "hl"), ("d", path.ToString()));
                }

                for (var p = 0; p < count; p++)
                {
                    var py = Y(k, p);

                    if (!double.IsFinite(py))
                    {
                        continue;
                    }

                    var rows = new List<TipRow>
                    {
                        new(names[k], true, k),
                        new(periods[p] + ": " + format(s.Series[k].Values[p])),
                        new("Rank: " + NumberFormat.Value(ranks[k][p]) + " of " +
                            ranks.Count(r => double.IsFinite(r[p])))
                    };

                    if (p > 0 && double.IsFinite(s.Series[k].Values[p - 1]))
                    {
                        var d = s.Series[k].Values[p] - s.Series[k].Values[p - 1];
                        rows.Add(new TipRow("Change: " + (d >= 0 ? "+" : string.Empty) + format(d)));
                    }

                    canvas.OpenMark(k, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(xs[p])), ("cy", Num.F(py)),
                        ("r", Num.F(radius + 4)));

                    canvas.W.Empty("circle", ("class", "pt"), ("cx", Num.F(xs[p])), ("cy", Num.F(py)),
                        ("r", Num.F(radius)));

                    if (ranked)
                    {
                        canvas.W.Element("text", NumberFormat.Value(ranks[k][p]), ("class", "rk ctr mid on"),
                            ("x", Num.F(xs[p])),
                            ("y", Num.F(py)));
                    }

                    canvas.Tooltip(xs[p], py - radius, rows);
                    canvas.CloseMark();
                }

                canvas.W.Close();
            }

            Labels(canvas, s, Y, xs, plot, LeftLabel, RightLabel, true);
            Labels(canvas, s, Y, xs, plot, LeftLabel, RightLabel, false);

            if (m == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            return canvas.End();
        }

        private static void Labels(ChartCanvas canvas, SlopeChartSpec s, Func<int, int, double> y,
            double[] xs, Rect plot, Func<int, string> left, Func<int, string> right, bool isLeft)
        {
            var p = isLeft ? 0 : xs.Length - 1;

            var items = Enumerable.Range(0, s.Series.Count).Where(k => double.IsFinite(y(k, p)))
                .Select(k => (K: k, Y: y(k, p))).OrderBy(t => t.Y).ThenBy(t => t.K).ToList();

            var pos = items.Select(t => t.Y).ToArray();
            Spread(pos, 13, plot.Top + 6, plot.Bottom - 2);
            var offset = s.Mode == SlopeMode.Ranks ? 16.0 : 9.0;

            for (var i = 0; i < items.Count; i++)
            {
                var k = items[i].K;
                var text = isLeft ? left(k) : right(k);

                var maxWidth = isLeft
                    ? xs[0] - ChartCanvas.Pad - offset
                    : canvas.Width - ChartCanvas.Pad - xs[^1] - offset;

                canvas.W.Element("text", TextMetrics.Truncate(text, Math.Max(maxWidth, 20), 11),
                    ("class", isLeft ? "dl end mid" : "dl mid"),
                    ("x", Num.F(isLeft ? xs[0] - offset : xs[^1] + offset)),
                    ("y", Num.F(pos[i])));
            }
        }

        public static void Spread(double[] positions, double gap, double min, double max)
        {
            var n = positions.Length;

            for (var pass = 0; pass < 60; pass++)
            {
                var moved = false;

                for (var i = 1; i < n; i++)
                {
                    var d = positions[i] - positions[i - 1];

                    if (d < gap)
                    {
                        var push = (gap - d) / 2;
                        positions[i - 1] -= push;
                        positions[i] += push;
                        moved = true;
                    }
                }

                if (n > 0)
                {
                    positions[0] = Math.Max(positions[0], min);
                    positions[n - 1] = Math.Min(positions[n - 1], max);
                }

                if (!moved)
                {
                    break;
                }
            }
        }
    }
}