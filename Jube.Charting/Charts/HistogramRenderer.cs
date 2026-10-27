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
    internal static class HistogramRenderer
    {
        public static string Render(HistogramChartSpec spec, string id)
        {
            var series = spec.Series;
            ChartLimits.Series(series.Count);
            long bins = 0;

            foreach (var s in series)
            {
                if (s.Edges.Count != s.Counts.Count + 1 || s.Counts.Count < 1)
                {
                    throw new ChartSpecificationException("A histogram series needs one more edge than counts.");
                }

                for (var i = 1; i < s.Edges.Count; i++)
                {
                    if (!(s.Edges[i] > s.Edges[i - 1]) || !double.IsFinite(s.Edges[i]) ||
                        !double.IsFinite(s.Edges[i - 1]))
                    {
                        throw new ChartSpecificationException(
                            "Histogram edges must be finite and strictly increasing.");
                    }
                }

                bins += s.Counts.Count;
            }

            ChartLimits.Points(bins);
            var names = series.Select(s => s.Name).ToArray();
            var colours = Guard.Colours(series.Select(s => s.Color));
            var legend = names.Select((n, i) => new LegendEntry(n, i)).ToList();
            var totals = series.Select(s => s.Counts.Where(double.IsFinite).Sum()).ToArray();

            double Value(int si, int i)
            {
                var c = series[si].Counts[i];

                if (!double.IsFinite(c) || c < 0)
                {
                    return double.NaN;
                }

                if (spec.Density)
                {
                    var width = series[si].Edges[i + 1] - series[si].Edges[i];

                    return totals[si] > 0 && width > 0 ? c / (totals[si] * width) : 0;
                }

                return spec.Normalize ? totals[si] > 0 ? c / totals[si] : 0 : c;
            }

            var xe = new Extent();
            var ye = new Extent();
            ye.Add(0);

            for (var si = 0; si < series.Count; si++)
            {
                foreach (var e in series[si].Edges)
                {
                    xe.Add(e);
                }

                for (var i = 0; i < series[si].Counts.Count; i++)
                {
                    ye.Add(Value(si, i));
                }
            }

            var summary = Guard.Summary("Histogram", names, xe.Any
                ? "Values from " + NumberFormat.Value(xe.Min) + " to " + NumberFormat.Value(xe.Max) + " in " +
                  (series.Count == 0 ? 0 : series[0].Counts.Count) + " bins."
                : "There is no data to plot.");

            var canvas = new ChartCanvas(spec.Options, id, summary, legend, series.Count, colours);
            canvas.LimitInteractive(bins);

            Func<double, string> yFormat =
                spec is { Normalize: true, Density: false } ? v => NumberFormat.Percent(v, 0) : NumberFormat.Value;

            var plot = canvas.Frame(30, 20);
            IScale yScale;

            for (var pass = 0; pass < 2; pass++)
            {
                (yScale, var values) = AxisFactory.Numeric(0, ye.Any ? ye.Max : 1, plot.Bottom, plot.Top,
                    AxisKind.Linear, (int)Math.Clamp(plot.Height / 42, 3, 10));

                var ticks = AxisFactory.ToTicks(yScale, values,
                    spec is { Normalize: true, Density: false } ? yFormat : null);
                plot = canvas.Frame(TextMetrics.MaxWidth(ticks.Select(t => t.Label), 11), 20);
            }

            var (ys, yvals) = AxisFactory.Numeric(0, ye.Any ? ye.Max : 1, plot.Bottom, plot.Top, AxisKind.Linear,
                (int)Math.Clamp(plot.Height / 42, 3, 10));

            yScale = ys;
            var yTicks = AxisFactory.ToTicks(yScale, yvals,
                spec is { Normalize: true, Density: false } ? yFormat : null);
            var xMin = xe.Any ? xe.Min : 0;
            var xMax = xe.Any ? xe.Max : 1;

            var (xScale, xvals) = AxisFactory.Numeric(xMin, xMax, plot.Left, plot.Right, AxisKind.Linear,
                (int)Math.Clamp(plot.Width / 80, 3, 12), (xMin, xMax));

            var xTicks = AxisFactory.ToTicks(xScale, xvals, null);

            canvas.Begin();
            canvas.YAxis(yTicks);
            canvas.XAxis(xTicks);
            canvas.Baseline(plot.Bottom);
            var baseline = yScale.Map(0);

            for (var ri = 0; ri < spec.References.Count; ri++)
            {
                var reference = spec.References[ri];
                var rx = xScale.Map(reference.X);

                if (!double.IsFinite(rx) || reference.X < xMin || reference.X > xMax)
                {
                    continue;
                }

                canvas.W.Open("line", ("class", "ref"), ("x1", Num.F(rx)), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(rx)), ("y2", Num.F(plot.Bottom)));
                canvas.W.Element("title", reference.Label + ": " + NumberFormat.Value(reference.X));
                canvas.W.Close();
                canvas.Text(rx, plot.Top + 10 + ri * 12, reference.Label, "tk ctr");
            }

            for (var si = 0; si < series.Count; si++)
            {
                canvas.OpenSeries(si);
                var s = series[si];

                for (var i = 0; i < s.Counts.Count; i++)
                {
                    var v = Value(si, i);

                    if (!double.IsFinite(v))
                    {
                        continue;
                    }

                    var x0 = xScale.Map(s.Edges[i]);
                    var x1 = xScale.Map(s.Edges[i + 1]);
                    var top = yScale.Map(v);

                    var rows = new List<TipRow>
                    {
                        new(NumberFormat.Value(s.Edges[i]) + " to " + NumberFormat.Value(s.Edges[i + 1]), true),
                        new(s.Name + ": " + NumberFormat.Value(s.Counts[i]), false, si)
                    };

                    if (spec.Density)
                    {
                        rows.Add(new TipRow("Density: " + NumberFormat.Value(v)));
                    }

                    if (spec.Normalize || totals[si] > 0)
                    {
                        rows.Add(new TipRow(NumberFormat.Percent(totals[si] > 0 ? s.Counts[i] / totals[si] : 0) +
                                            " of series"));
                    }

                    canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("rect", ("class", "ov m"), ("x", Num.F(x0 + 0.5)),
                        ("y", Num.F(Math.Min(top, baseline))),
                        ("width", Num.F(Math.Max(x1 - x0 - 1, 0.5))), ("height", Num.F(Math.Abs(baseline - top))));

                    canvas.Tooltip((x0 + x1) / 2, top, rows);
                    canvas.CloseMark();
                }

                canvas.W.Close();
            }

            if (!xe.Any)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}