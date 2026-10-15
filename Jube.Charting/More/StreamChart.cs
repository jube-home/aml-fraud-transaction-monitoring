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
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme.Models;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class StreamChart(StreamChartSpec spec) : IStyleModeChart
    {
        public StreamChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new StreamChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        public static int[] StackOrder(IReadOnlyList<IReadOnlyList<double>> values, StreamOrder order)
        {
            var n = values.Count;
            var indices = Enumerable.Range(0, n).ToArray();

            if (order == StreamOrder.None)
            {
                return indices;
            }

            var sums = values.Select(v => v.Sum()).ToArray();

            if (order == StreamOrder.Descending)
            {
                return [.. indices.OrderByDescending(i => sums[i]).ThenBy(i => i)];
            }

            var peaks = values.Select(v =>
            {
                var best = 0;

                for (var j = 1; j < v.Count; j++)
                {
                    if (v[j] > v[best])
                    {
                        best = j;
                    }
                }

                return best;
            }).ToArray();

            var byPeak = indices.OrderBy(i => peaks[i]).ThenBy(i => i).ToArray();
            double top = 0, bottom = 0;
            var tops = new List<int>();
            var bottoms = new List<int>();

            foreach (var i in byPeak)
            {
                if (top < bottom)
                {
                    top += sums[i];
                    tops.Add(i);
                }
                else
                {
                    bottom += sums[i];
                    bottoms.Add(i);
                }
            }

            bottoms.Reverse();

            return [.. bottoms, .. tops];
        }

        public static double[] Baseline(double[][] stackedValues, StreamOffset offset)
        {
            var n = stackedValues.Length;
            var m = n == 0 ? 0 : stackedValues[0].Length;
            var baseline = new double[m];

            if (offset is StreamOffset.Zero or StreamOffset.Expand || n == 0)
            {
                return baseline;
            }

            if (offset == StreamOffset.Silhouette)
            {
                for (var j = 0; j < m; j++)
                {
                    baseline[j] = -stackedValues.Sum(v => v[j]) / 2;
                }

                return baseline;
            }

            var y = 0.0;

            for (var j = 1; j < m; j++)
            {
                double s1 = 0, s2 = 0;

                for (var i = 0; i < n; i++)
                {
                    var now = stackedValues[i][j];
                    var before = stackedValues[i][j - 1];
                    var s3 = (now - before) / 2;

                    for (var k = 0; k < i; k++)
                    {
                        s3 += stackedValues[k][j] - stackedValues[k][j - 1];
                    }

                    s1 += now;
                    s2 += s3 * now;
                }

                baseline[j - 1] = y;

                if (s1 != 0)
                {
                    y -= s2 / s1;
                }
            }

            if (m > 0)
            {
                baseline[m - 1] = y;
            }

            return baseline;
        }

        private string Draw(string id)
        {
            var s = Spec;
            var m = s.X.Count;
            ChartLimits.Series(s.Series.Count);
            ChartLimits.Points((long)m * s.Series.Count);

            foreach (var series in s.Series)
            {
                if (series.Values.Count != m)
                {
                    throw new ChartSpecificationException("Every series needs one value per x position.");
                }

                if (series.Values.Any(v => double.IsFinite(v) && v < 0))
                {
                    throw new ChartSpecificationException("Stream values must not be negative.");
                }
            }

            if (s.X.Any(x => !double.IsFinite(x)))
            {
                throw new ChartSpecificationException("X positions must be finite.");
            }

            for (var j = 1; j < m; j++)
            {
                if (!(s.X[j] > s.X[j - 1]))
                {
                    throw new ChartSpecificationException("X positions must be strictly increasing.");
                }
            }

            var format = s.ValueFormat ?? NumberFormat.Value;

            var clean = s.Series
                .Select(x => (IReadOnlyList<double>)[.. x.Values.Select(v => double.IsFinite(v) ? v : 0)])
                .ToArray();

            var order = StackOrder(clean, s.Order);
            var stacked = order.Select(i => clean[i].ToArray()).ToArray();
            var totals = Enumerable.Range(0, m).Select(j => stacked.Sum(v => v[j])).ToArray();

            if (s.Offset == StreamOffset.Expand)
            {
                for (var j = 0; j < m; j++)
                {
                    if (totals[j] > 0)
                    {
                        foreach (var v in stacked)
                        {
                            v[j] /= totals[j];
                        }
                    }
                }
            }

            var baseline = Baseline(stacked, s.Offset);
            var lower = new double[stacked.Length][];
            var upper = new double[stacked.Length][];
            var running = (double[])baseline.Clone();

            for (var i = 0; i < stacked.Length; i++)
            {
                lower[i] = (double[])running.Clone();

                for (var j = 0; j < m; j++)
                {
                    running[j] += stacked[i][j];
                }

                upper[i] = (double[])running.Clone();
            }

            var e = new Extent();

            for (var j = 0; j < m; j++)
            {
                e.Add(baseline[j]);
                e.Add(running[j]);
            }

            var names = s.Series.Select(x => x.Name).ToArray();
            var colours = Guard.Colours(s.Series.Select(x => x.Color));
            var legend = s.Series.Select((x, i) => new LegendEntry(x.Name, i)).ToList();

            var summary = Guard.Summary("Stream chart", names, m > 0
                ? "Across " + m + " positions, " + s.Offset.ToString().ToLowerInvariant() + " baseline."
                : "There is no data to plot.");

            var canvas = new ChartCanvas(MoreKit.AutoLegend(s.Options), id, summary, legend, s.Series.Count, colours)
            {
                Family = StyleFamily.Stream
            };

            var showAxis = s.Offset is StreamOffset.Zero or StreamOffset.Expand;
            var plot = canvas.Frame(showAxis ? 30 : 40, 20);
            var lo = e.Any ? e.Min : 0;
            var hi = e.Any ? e.Max : 1;
            IScale ys;
            IReadOnlyList<Tick> yTicks = [];

            if (showAxis)
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    (ys, var values) = AxisFactory.Numeric(s.Offset == StreamOffset.Expand ? 0 : lo,
                        s.Offset == StreamOffset.Expand ? 1 : hi,
                        plot.Bottom, plot.Top, AxisKind.Linear, (int)Math.Clamp(plot.Height / 42, 3, 10),
                        s.Offset == StreamOffset.Expand ? (0, 1) : null);

                    yTicks = AxisFactory.ToTicks(ys, values,
                        s.Offset == StreamOffset.Expand ? v => NumberFormat.Percent(v, 0) : s.ValueFormat);

                    plot = canvas.Frame(TextMetrics.MaxWidth(yTicks.Select(t => t.Label), 11), 20);
                }
            }

            (ys, var yv) = AxisFactory.Numeric(s.Offset == StreamOffset.Expand ? 0 : lo,
                s.Offset == StreamOffset.Expand ? 1 : hi,
                plot.Bottom, plot.Top, AxisKind.Linear, (int)Math.Clamp(plot.Height / 42, 3, 10),
                s.Offset == StreamOffset.Expand ? (0, 1) : null);

            if (showAxis)
            {
                yTicks = AxisFactory.ToTicks(ys, yv,
                    s.Offset == StreamOffset.Expand ? v => NumberFormat.Percent(v, 0) : s.ValueFormat);
            }

            var xMin = m > 0 ? s.X[0] : 0;
            var xMax = m > 0 ? s.X[^1] : 1;
            var (xs, xTicks) = MoreAxes.X(s.XAxis, xMin, xMax, plot, (xMin, xMax), s.XFormat);

            var slotsPer = Math.Max(1,
                Math.Min(m, Math.Min(200, ChartLimits.InteractiveMarksPerChart / Math.Max(s.Series.Count, 1))));

            var slotIndex = MoreKit.Thin([.. Enumerable.Range(0, m)], slotsPer).Distinct().ToArray();
            canvas.LimitInteractive((long)slotIndex.Length * s.Series.Count);
            canvas.Begin();

            if (showAxis)
            {
                canvas.YAxis(yTicks);
            }

            canvas.XAxis(xTicks, true);
            canvas.Baseline(plot.Bottom);
            var px = s.X.Select(xs.Map).ToArray();

            for (var rank = 0; rank < order.Length; rank++)
            {
                var si = order[rank];
                var top = upper[rank].Select(ys.Map).ToArray();
                var bottom = lower[rank].Select(ys.Map).ToArray();
                canvas.OpenSeries(si);
                var path = new PathBuilder();

                if (m > 0)
                {
                    if (s.Smooth)
                    {
                        Spline.Append(path, px, top, false);
                        Spline.Append(path, [.. px.Reverse()], [.. bottom.Reverse()], true);
                    }
                    else
                    {
                        path.Move(px[0], top[0]);

                        for (var j = 1; j < m; j++)
                        {
                            path.Line(px[j], top[j]);
                        }

                        for (var j = m - 1; j >= 0; j--)
                        {
                            path.Line(px[j], bottom[j]);
                        }
                    }
                }

                if (!path.IsEmpty)
                {
                    canvas.W.Open("path", ("class", "st"), ("d", path.Close().ToString()));
                    canvas.W.Element("title", names[si]);
                    canvas.W.Close();
                }

                if (canvas.Interactive)
                {
                    for (var k = 0; k < slotIndex.Length; k++)
                    {
                        var j = slotIndex[k];
                        var left = k == 0 ? px[j] : (px[slotIndex[k - 1]] + px[j]) / 2;
                        var right = k == slotIndex.Length - 1 ? px[j] : (px[j] + px[slotIndex[k + 1]]) / 2;

                        if (slotIndex.Length == 1)
                        {
                            left = px[j] - 6;
                            right = px[j] + 6;
                        }

                        var y0 = Math.Min(top[j], bottom[j]);
                        var y1 = Math.Max(top[j], bottom[j]);
                        var raw = clean[si][j];

                        var rows = new List<TipRow>
                        {
                            new(names[si], true, si),
                            new(MoreAxes.XText(s.XAxis, s.X[j], s.XFormat)),
                            new("Value: " + format(raw)),
                            new("Share: " + NumberFormat.Percent(totals[j] > 0 ? raw / totals[j] : 0) + " of " +
                                format(totals[j]))
                        };

                        canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)));

                        canvas.W.Empty("rect", ("class", "shl"), ("x", Num.F(left)), ("y", Num.F(y0)),
                            ("width", Num.F(Math.Max(right - left, 1))), ("height", Num.F(Math.Max(y1 - y0, 1))));

                        canvas.Tooltip(px[j], (y0 + y1) / 2, rows, true);
                        canvas.CloseMark();
                    }
                }

                canvas.W.Close();
            }

            if (!showAxis && e.Any && hi > lo)
            {
                var unit = ExtendedTicks.Generate(0, hi - lo, 3).Where(v => v > 0).DefaultIfEmpty(hi - lo).First();
                var centre = plot.CenterY;
                var half = Math.Abs(ys.Map(unit) - ys.Map(0)) / 2;
                var bx = plot.Left - 6;

                canvas.W.Empty("path", ("class", "sbar"),
                    ("d", new PathBuilder().Move(bx, centre - half).Line(bx, centre + half).ToString()));

                canvas.W.Element("text", format(unit), ("class", "tk end mid"), ("x", Num.F(bx - 5)),
                    ("y", Num.F(centre)));
            }

            if (m == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}