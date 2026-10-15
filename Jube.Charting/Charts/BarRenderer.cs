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
using Jube.Charting.Scales;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Ticks;
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    internal static class BarRenderer
    {
        public static string Render(BarChartSpec spec, string id)
        {
            var series = spec.Series;
            ChartLimits.Series(series.Count);
            ChartLimits.Categories(spec.Categories.Count);
            var n = spec.Categories.Count;
            ChartLimits.Points((long)n * Math.Max(series.Count, 1));

            foreach (var s in series)
            {
                if (s.Values.Count != n)
                {
                    throw new ChartSpecificationException("Series '" + XmlText.Clean(s.Name, 60) +
                                                          "' must have one value per category.");
                }

                if (s.Errors is not null && s.Errors.Count != n)
                {
                    throw new ChartSpecificationException("Error values must have one value per category.");
                }
            }

            var horizontal = spec.Orientation == BarOrientation.Horizontal;
            var percent = spec.Grouping == BarGrouping.Percent && series.Count > 1;
            var stacked = spec.Grouping != BarGrouping.Grouped && series.Count > 1;
            var names = series.Select(s => s.Name).ToArray();
            var colours = Guard.Colours(series.Select(s => s.Color));
            var legend = names.Select((nm, i) => new LegendEntry(nm, i)).ToList();

            var extent = new Extent();
            extent.Add(0);
            var lows = new double[series.Count][];
            var highs = new double[series.Count][];

            for (var si = 0; si < series.Count; si++)
            {
                lows[si] = new double[n];
                highs[si] = new double[n];
            }

            for (var i = 0; i < n; i++)
            {
                var pos = 0.0;
                var neg = 0.0;
                var absTotal = percent ? series.Sum(s => double.IsFinite(s.Values[i]) ? Math.Abs(s.Values[i]) : 0) : 0;

                for (var si = 0; si < series.Count; si++)
                {
                    var v = series[si].Values[i];

                    if (!double.IsFinite(v))
                    {
                        lows[si][i] = double.NaN;
                        highs[si][i] = double.NaN;
                        continue;
                    }

                    if (series[si].From is { } from)
                    {
                        var f = double.IsFinite(from[i]) ? from[i] : 0;
                        lows[si][i] = Math.Min(f, v);
                        highs[si][i] = Math.Max(f, v);
                        extent.Add(f);
                        extent.Add(v);
                        continue;
                    }

                    if (percent)
                    {
                        v = absTotal > 0 ? Math.Abs(v) / absTotal * 100 : 0;
                    }

                    if (stacked)
                    {
                        if (v >= 0)
                        {
                            lows[si][i] = pos;
                            pos += v;
                            highs[si][i] = pos;
                        }
                        else
                        {
                            highs[si][i] = neg;
                            neg += v;
                            lows[si][i] = neg;
                        }
                    }
                    else
                    {
                        lows[si][i] = Math.Min(0, v);
                        highs[si][i] = Math.Max(0, v);
                        var err = series[si].Errors is { } e && double.IsFinite(e[i]) ? Math.Abs(e[i]) : 0;
                        extent.Add(v + err);
                        extent.Add(v - err);
                    }
                }

                extent.Add(pos);
                extent.Add(neg);
            }

            foreach (var line in spec.Lines)
            {
                extent.Add(line.Value);
            }

            foreach (var band in spec.Bands)
            {
                extent.Add(band.From);
                extent.Add(band.To);
            }

            if (percent)
            {
                extent = new Extent();
                extent.Add(0);
                extent.Add(100);
            }

            var summary = Guard.Summary(
                percent ? "100% stacked bar chart" : stacked ? "Stacked bar chart" : "Bar chart",
                names,
                n + " categories, " + (horizontal ? "horizontal" : "vertical") + " bars. Values range from " +
                NumberFormat.Value(extent.Min) + " to " + NumberFormat.Value(extent.Max) + ".");

            if (series.Count == 0)
            {
                summary = "Bar chart with no series.";
            }

            var canvas = new ChartCanvas(spec.Options, id, summary, legend, series.Count, colours);
            canvas.LimitInteractive((long)n * Math.Max(series.Count, 1));
            var valueLabels = percent && spec.ValueFormat is null ? v => NumberFormat.Value(v) + "%" : spec.ValueFormat;
            (double Min, double Max)? fixedDomain = percent ? (0, 100) : null;

            if (horizontal)
            {
                RenderHorizontal(canvas, spec, series, extent, lows, highs, stacked, n, valueLabels, fixedDomain,
                    percent);
            }
            else
            {
                RenderVertical(canvas, spec, series, extent, lows, highs, stacked, n, valueLabels, fixedDomain,
                    percent);
            }

            return canvas.End();
        }

        private static void RenderVertical(ChartCanvas canvas, BarChartSpec spec, IReadOnlyList<ValueSeries> series,
            Extent extent, double[][] lows, double[][] highs, bool stacked, int n, Func<double, string>? format,
            (double Min, double Max)? fixedDomain, bool percent)
        {
            var plot = canvas.Frame(30, 20);
            IScale yScale;
            IReadOnlyList<Tick> yTicks;

            for (var pass = 0; pass < 2; pass++)
            {
                var target = (int)Math.Clamp(plot.Height / 42, 3, 10);

                (yScale, var values) = AxisFactory.Numeric(extent.Min, extent.Max, plot.Bottom, plot.Top,
                    AxisKind.Linear,
                    target);

                yTicks = AxisFactory.ToTicks(yScale, values, format);
                plot = canvas.Frame(TextMetrics.MaxWidth(yTicks.Select(t => t.Label), 11), 20);
            }

            var target2 = (int)Math.Clamp(plot.Height / 42, 3, 10);

            (yScale, var vs) = AxisFactory.Numeric(extent.Min, extent.Max, plot.Bottom, plot.Top, AxisKind.Linear,
                target2,
                fixedDomain);

            yTicks = AxisFactory.ToTicks(yScale, vs, format);

            canvas.Begin();
            canvas.YAxis(yTicks);
            var band = new BandScale(Math.Max(n, 1), plot.Left, plot.Right, n > 1 ? 0.25 : 0.5);
            var zero = yScale.Map(0);
            canvas.Baseline(zero);

            canvas.XAxis(CategoryLayout.CategoryTicks(spec.Categories, band));
            DrawGuides(canvas, spec, yScale, false);
            DrawBars(canvas, spec, series, lows, highs, stacked, n, false, band, yScale, format, percent);

            if (series.Count == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();
        }

        private static void RenderHorizontal(ChartCanvas canvas, BarChartSpec spec, IReadOnlyList<ValueSeries> series,
            Extent extent, double[][] lows, double[][] highs, bool stacked, int n, Func<double, string>? format,
            (double Min, double Max)? fixedDomain, bool percent)
        {
            var maxLabel = canvas.Width * 0.32;
            var labelWidth = Math.Min(TextMetrics.MaxWidth(spec.Categories, 11), maxLabel);
            var plot = canvas.Frame(labelWidth, 20);
            var target = (int)Math.Clamp(plot.Width / 80, 3, 10);

            var (xScale, values) = AxisFactory.Numeric(extent.Min, extent.Max, plot.Left, plot.Right, AxisKind.Linear,
                target, fixedDomain);

            var xTicks = AxisFactory.ToTicks(xScale, values, format);

            canvas.Begin();

            foreach (var t in xTicks)
            {
                canvas.W.Empty("line", ("class", "gl"), ("x1", Num.F(t.Position)), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(t.Position)), ("y2", Num.F(plot.Bottom)));
            }

            canvas.XAxis(xTicks);
            var band = new BandScale(Math.Max(n, 1), plot.Top, plot.Bottom, n > 1 ? 0.25 : 0.5);
            canvas.VerticalBaseline(xScale.Map(0));

            for (var i = 0; i < n; i++)
            {
                canvas.Text(plot.Left - 8, band.Center(i), TextMetrics.Truncate(spec.Categories[i], labelWidth, 11),
                    "tk end mid");
            }

            DrawGuides(canvas, spec, xScale, true);
            DrawBars(canvas, spec, series, lows, highs, stacked, n, true, band, xScale, format, percent);

            if (series.Count == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();
        }

        private static void DrawBars(ChartCanvas canvas, BarChartSpec spec, IReadOnlyList<ValueSeries> series,
            double[][] lows, double[][] highs, bool stacked, int n, bool horizontal, BandScale band, IScale value,
            Func<double, string>? format, bool percent)
        {
            var count = series.Count;
            const double gap = 2;
            var sub = stacked ? band.Bandwidth : (band.Bandwidth - (count - 1) * gap) / Math.Max(count, 1);
            sub = Math.Max(sub, 1);

            for (var si = 0; si < count; si++)
            {
                canvas.OpenSeries(si);

                for (var i = 0; i < n; i++)
                {
                    var lo = lows[si][i];
                    var hi = highs[si][i];

                    if (!double.IsFinite(lo) || !double.IsFinite(hi))
                    {
                        continue;
                    }

                    var v = series[si].Values[i];
                    var isRange = series[si].From is not null;
                    var offset = stacked ? 0 : si * (sub + gap);
                    var start = band.Start(i) + offset;
                    var a = value.Map(lo);
                    var b = value.Map(hi);
                    var length = Math.Abs(b - a);
                    var roundEnd = !stacked && !isRange ? Math.Min(4, Math.Min(sub / 2, length)) : 0;

                    var rows = new List<TipRow>
                    {
                        new(TextMetrics.Truncate(spec.Categories[i], 300, 12, true), true),
                        new(series[si].Name + ": " + (format is null ? NumberFormat.Value(v) : format(v)), false, si)
                    };

                    if (series[si].Errors is { } errs && double.IsFinite(errs[i]))
                    {
                        rows.Add(new TipRow("Error: " + NumberFormat.Value(Math.Abs(errs[i]))));
                    }

                    if (stacked)
                    {
                        var total = series.Sum(s => double.IsFinite(s.Values[i]) ? s.Values[i] : 0);
                        rows.Add(new TipRow("Total: " + NumberFormat.Value(total)));
                    }

                    if (series[si].From is { } fromValues)
                    {
                        rows[1] = new TipRow(
                            series[si].Name + ": " + NumberFormat.Value(fromValues[i]) + " to " + NumberFormat.Value(v),
                            false, si);
                    }

                    if (percent)
                    {
                        rows.Add(new TipRow("Share: " + NumberFormat.Value(Math.Round(hi - lo, 1)) + "%"));
                        rows[1] = new TipRow(series[si].Name + ": " + NumberFormat.Value(v), false, si);
                    }

                    var pointColour = series[si].PointColors is { } pc && i < pc.Count
                        ? ColourCheck.Normalise(pc[i])
                        : null;

                    canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)), null,
                        pointColour is null ? null : "--sc:" + pointColour);

                    var path = BarPath(horizontal, start, sub, Math.Min(a, b), Math.Max(a, b), v >= 0 || percent,
                        roundEnd);

                    canvas.W.Empty("path", ("class", "bar m"), ("d", path));

                    if (series[si].Errors is { } e2 && double.IsFinite(e2[i]) && e2[i] != 0)
                    {
                        DrawError(canvas, horizontal, start + sub / 2, value.Map(v - Math.Abs(e2[i])),
                            value.Map(v + Math.Abs(e2[i])));
                    }

                    var anchor = value.Map(v >= 0 ? hi : lo);

                    if (horizontal)
                    {
                        canvas.Tooltip(anchor, start + sub / 2, rows, true, 8);
                    }
                    else
                    {
                        canvas.Tooltip(start + sub / 2, anchor, rows, false, 8);
                    }

                    canvas.CloseMark();

                    if (spec.ShowValues && stacked && si == count - 1 && !horizontal && !percent)
                    {
                        var total = series.Sum(x => double.IsFinite(x.Values[i]) ? x.Values[i] : 0);

                        canvas.Text(start + sub / 2, Math.Min(a, b) - 6,
                            format is null ? NumberFormat.Value(total) : format(total), "lbl ctr");
                    }
                    else if (spec.ShowValues && !stacked && canvas.Interactive)
                    {
                        var label = format is null ? NumberFormat.Value(v) : format(v);

                        if (horizontal)
                        {
                            canvas.Text(v >= 0 ? anchor + 6 : value.Map(0) + 6, start + sub / 2, label, "lbl mid");
                        }
                        else if (TextMetrics.Width(label, 11) <= Math.Max(sub + gap * 2, 26))
                        {
                            canvas.Text(start + sub / 2, anchor + (v >= 0 ? -6 : 14), label, "lbl ctr");
                        }
                    }
                }

                canvas.W.Close();
            }
        }

        private static void DrawError(ChartCanvas canvas, bool horizontal, double center, double from, double to)
        {
            if (!double.IsFinite(from) || !double.IsFinite(to))
            {
                return;
            }

            var d = new PathBuilder();

            if (horizontal)
            {
                d.Move(from, center).Line(to, center).Move(from, center - 4).Line(from, center + 4).Move(to, center - 4)
                    .Line(to, center + 4);
            }
            else
            {
                d.Move(center, from).Line(center, to).Move(center - 4, from).Line(center + 4, from).Move(center - 4, to)
                    .Line(center + 4, to);
            }

            canvas.W.Empty("path", ("class", "eb"), ("d", d.ToString()));
        }

        private static string BarPath(bool horizontal, double start, double size, double lo, double hi, bool positive,
            double r)
        {
            var p = new PathBuilder();

            if (!horizontal)
            {
                var x0 = start;
                var x1 = start + size;
                var top = lo;
                var bottom = hi;

                if (r <= 0)
                {
                    return p.Move(x0, top).Line(x1, top).Line(x1, bottom).Line(x0, bottom).Close().ToString();
                }

                if (positive)
                {
                    p.Move(x0, bottom).Line(x0, top + r).Arc(r, false, true, x0 + r, top).Line(x1 - r, top)
                        .Arc(r, false, true, x1, top + r).Line(x1, bottom).Close();
                }
                else
                {
                    p.Move(x0, top).Line(x1, top).Line(x1, bottom - r).Arc(r, false, true, x1 - r, bottom)
                        .Line(x0 + r, bottom).Arc(r, false, true, x0, bottom - r).Close();
                }

                return p.ToString();
            }

            var y0 = start;
            var y1 = start + size;

            if (r <= 0)
            {
                return p.Move(lo, y0).Line(hi, y0).Line(hi, y1).Line(lo, y1).Close().ToString();
            }

            if (positive)
            {
                p.Move(lo, y0).Line(hi - r, y0).Arc(r, false, true, hi, y0 + r).Line(hi, y1 - r)
                    .Arc(r, false, true, hi - r, y1).Line(lo, y1).Close();
            }
            else
            {
                p.Move(hi, y0).Line(lo + r, y0).Arc(r, false, false, lo, y0 + r).Line(lo, y1 - r)
                    .Arc(r, false, false, lo + r, y1).Line(hi, y1).Close();
            }

            return p.ToString();
        }

        private static void DrawGuides(ChartCanvas canvas, BarChartSpec spec, IScale value, bool horizontal)
        {
            var plot = canvas.Plot;

            foreach (var band in spec.Bands)
            {
                var a = value.Map(band.From);
                var b = value.Map(band.To);

                if (!double.IsFinite(a) || !double.IsFinite(b))
                {
                    continue;
                }

                var lo = Math.Min(a, b);
                var size = Math.Abs(b - a);

                if (horizontal)
                {
                    canvas.W.Empty("rect", ("class", "band"), ("x", Num.F(lo)), ("y", Num.F(plot.Top)),
                        ("width", Num.F(size)),
                        ("height", Num.F(plot.Height)));
                }
                else
                {
                    canvas.W.Empty("rect", ("class", "band"), ("x", Num.F(plot.Left)), ("y", Num.F(lo)),
                        ("width", Num.F(plot.Width)), ("height", Num.F(size)));
                }

                if (!string.IsNullOrEmpty(band.Label))
                {
                    canvas.Text(horizontal ? lo + 4 : plot.Left + 6, horizontal ? plot.Top + 13 : lo + 13,
                        TextMetrics.Truncate(band.Label, horizontal ? size : plot.Width, 11), "tk");
                }
            }

            foreach (var line in spec.Lines)
            {
                var p = value.Map(line.Value);

                if (!double.IsFinite(p))
                {
                    continue;
                }

                canvas.W.Open("line", ("class", "ref"), ("x1", Num.F(horizontal ? p : plot.Left)),
                    ("y1", Num.F(horizontal ? plot.Top : p)), ("x2", Num.F(horizontal ? p : plot.Right)),
                    ("y2", Num.F(horizontal ? plot.Bottom : p)));

                canvas.W.Element("title", (line.Label ?? "Reference") + ": " + NumberFormat.Value(line.Value));
                canvas.W.Close();

                if (!string.IsNullOrEmpty(line.Label))
                {
                    canvas.Text(horizontal ? p + 4 : plot.Right - 4, horizontal ? plot.Top + 11 : p - 4,
                        TextMetrics.Truncate(line.Label, 160, 11), horizontal ? "tk" : "tk end");
                }
            }
        }
    }
}