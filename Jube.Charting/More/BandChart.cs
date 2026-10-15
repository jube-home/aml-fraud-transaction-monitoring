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

using System.Text;
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
    public sealed class BandChart(BandChartSpec spec) : IStyleModeChart
    {
        public BandChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new BandChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private static List<double> Take(IReadOnlyList<double>? values, int[] order, int expected, string what)
        {
            if (values is null)
            {
                return null!;
            }

            if (values.Count != expected)
            {
                throw new ChartSpecificationException(what + " must have the same length as the x values.");
            }

            return [.. order.Select(i => values[i])];
        }

        private static PreparedBandSeries Prepare(BandSeries s)
        {
            if (s.X.Count != s.Y.Count)
            {
                throw new ChartSpecificationException("X and Y values must have the same length.");
            }

            var order = Enumerable.Range(0, s.X.Count).Where(i => double.IsFinite(s.X[i])).ToArray();
            Array.Sort(order, (a, b) => Math.Abs(s.X[a] - s.X[b]) > 0.0001 ? s.X[a].CompareTo(s.X[b]) : a.CompareTo(b));
            var bands = s.Bands ?? [];
            var count = s.X.Count;

            return new PreparedBandSeries
            {
                Name = s.Name,
                Style = s.Style,
                Dashed = s.Dashed,
                ForecastFrom = s.ForecastFrom,
                X = [.. order.Select(i => s.X[i])],
                Y = [.. order.Select(i => s.Y[i])],
                BandLabels = [.. bands.Select(b => b.Label)],
                BandHidden = [.. bands.Select(b => b.Hidden)],
                Lower = [.. bands.Select(b => Take(b.Lower, order, count, "Band bounds"))],
                Upper = [.. bands.Select(b => Take(b.Upper, order, count, "Band bounds"))],
                YErrLow = Take(s.YErrorLow, order, count, "Error values"),
                YErrHigh = Take(s.YErrorHigh, order, count, "Error values"),
                XErrLow = Take(s.XErrorLow, order, count, "Error values"),
                XErrHigh = Take(s.XErrorHigh, order, count, "Error values")
            };
        }

        private static double Lerp(double a, double b, double t)
        {
            return double.IsFinite(a) && double.IsFinite(b) ? a + (b - a) * t : double.NaN;
        }

        private static PreparedBandSeries Clip(PreparedBandSeries s, double from, double to)
        {
            var x = new List<double>();
            var y = new List<double>();
            var lo = s.Lower.Select(_ => new List<double>()).ToList();
            var hi = s.Upper.Select(_ => new List<double>()).ToList();
            List<double>? yl = s.YErrLow is null ? null : [];
            List<double>? yh = s.YErrHigh is null ? null : [];
            List<double>? xl = s.XErrLow is null ? null : [];
            List<double>? xh = s.XErrHigh is null ? null : [];

            void Add(double px, double py, IEnumerable<double> l, IEnumerable<double> h, double a, double b, double c,
                double d)
            {
                x.Add(px);
                y.Add(py);
                var li = l.ToArray();
                var hi2 = h.ToArray();

                for (var k = 0; k < lo.Count; k++)
                {
                    lo[k].Add(li[k]);
                    hi[k].Add(hi2[k]);
                }

                yl?.Add(a);
                yh?.Add(b);
                xl?.Add(c);
                xh?.Add(d);
            }

            for (var i = 0; i < s.X.Count; i++)
            {
                var index = i;
                var inside = s.X[i] >= from && s.X[i] <= to;

                if (s.LineLike && i > 0)
                {
                    var x0 = s.X[i - 1];
                    var x1 = s.X[i];

                    foreach (var edge in new[] { from, to })
                    {
                        if (x0 < edge && x1 > edge)
                        {
                            var t = (edge - x0) / (x1 - x0);
                            var step = s.Style == BandSeriesStyle.Step;

                            Add(edge, step ? s.Y[i - 1] : Lerp(s.Y[i - 1], s.Y[i], t),
                                s.Lower.Select(l => step ? l[index - 1] : Lerp(l[index - 1], l[index], t)),
                                s.Upper.Select(u => step ? u[index - 1] : Lerp(u[index - 1], u[index], t)),
                                double.NaN, double.NaN, double.NaN, double.NaN);
                        }
                    }
                }

                if (inside)
                {
                    Add(s.X[i], s.Y[i], s.Lower.Select(l => l[index]), s.Upper.Select(u => u[index]),
                        s.YErrLow?[i] ?? 0, s.YErrHigh?[i] ?? 0, s.XErrLow?[i] ?? 0, s.XErrHigh?[i] ?? 0);
                }
            }

            var order = Enumerable.Range(0, x.Count).ToArray();
            Array.Sort(order, (a, b) => Math.Abs(x[a] - x[b]) > 0.0001 ? x[a].CompareTo(x[b]) : a.CompareTo(b));

            List<double> Re(List<double>? l)
            {
                return l is null ? null! : [.. order.Select(i => l[i])];
            }

            return new PreparedBandSeries
            {
                Name = s.Name,
                Style = s.Style,
                Dashed = s.Dashed,
                ForecastFrom = s.ForecastFrom,
                X = Re(x),
                Y = Re(y),
                BandLabels = s.BandLabels,
                BandHidden = s.BandHidden,
                Lower = [.. lo.Select(Re)],
                Upper = [.. hi.Select(Re)],
                YErrLow = Re(yl),
                YErrHigh = Re(yh),
                XErrLow = Re(xl),
                XErrHigh = Re(xh)
            };
        }

        private string Draw(string id)
        {
            var s = Spec;
            ChartLimits.Series(s.Series.Count);
            ChartLimits.Points(s.Series.Sum(x => (long)x.X.Count));
            var full = s.Series.Select(Prepare).ToList();
            var colours = Guard.Colours(s.Series.Select(x => x.Color));

            foreach (var marker in s.Events)
            {
                if (!double.IsFinite(marker.X))
                {
                    throw new ChartSpecificationException("Event markers need a finite position.");
                }
            }

            if (s.Window is { } w && !(w.To > w.From))
            {
                throw new ChartSpecificationException("The range window must have From less than To.");
            }

            var window = s.RangeSelector ? s.Window : null;
            var series = window is null ? full : [.. full.Select(p => Clip(p, window.From, window.To))];

            var xe = new Extent();
            var ye = new Extent();

            foreach (var p in series)
            {
                for (var i = 0; i < p.X.Count; i++)
                {
                    var px = p.X[i];

                    if (s.XAxis == XAxisKind.Log && px <= 0)
                    {
                        continue;
                    }

                    xe.Add(px);
                    xe.Add(px - (p.XErrLow?[i] ?? 0));
                    xe.Add(px + (p.XErrHigh?[i] ?? 0));
                    ye.Add(p.Y[i]);
                    ye.Add(p.Y[i] - (p.YErrLow?[i] ?? 0));
                    ye.Add(p.Y[i] + (p.YErrHigh?[i] ?? 0));

                    for (var b = 0; b < p.Lower.Count; b++)
                    {
                        ye.Add(p.Lower[b][i]);
                        ye.Add(p.Upper[b][i]);
                    }
                }
            }

            var hasData = xe.Any && ye.Any;

            foreach (var r in s.References)
            {
                ye.Add(r.Y1);
                ye.Add(r.Y2);
            }

            foreach (var band in s.Regions.Where(b => b.Horizontal))
            {
                ye.Add(band.From);
                ye.Add(band.To);
            }

            if (window is null)
            {
                foreach (var band in s.Regions.Where(b => !b.Horizontal))
                {
                    xe.Add(band.From);
                    xe.Add(band.To);
                }

                foreach (var ev in s.Events)
                {
                    xe.Add(ev.X);
                }
            }

            if (s.YFromZero)
            {
                ye.Add(0);
            }

            var xDomain = window is null ? s.XDomain : (window.From, window.To);
            var yMin = ye.Any ? ye.Min : 0;
            var yMax = ye.Any ? ye.Max : 1;
            var xMin = xe.Any ? xe.Min : 0;
            var xMax = xe.Any ? xe.Max : 1;

            var names = series.Select(p => p.Name).ToArray();

            var legend = series.Select((p, i) => new LegendEntry(p.Name, i,
                p.LineLike ? LegendShape.Line : LegendShape.Square)).ToList();

            var summary = Guard.Summary(s.Kind, names, hasData
                ? "Y values range from " + NumberFormat.Value(yMin) + " to " + NumberFormat.Value(yMax) + "." +
                  (series.Any(p => p.Lower.Count > 0) ? " Interval bands are shown." : string.Empty) +
                  (s.Events.Count > 0 ? " " + s.Events.Count + " marked events." : string.Empty)
                : "There is no data to plot.");

            var canvas = new ChartCanvas(MoreKit.AutoLegend(s.Options), id, summary, legend, series.Count, colours);
            var selectorHeight = s.RangeSelector ? 46.0 : 0;
            var selectorExtra = s.RangeSelector ? selectorHeight + 34 : 0;

            canvas.Family = StyleFamily.Bands;

            var plot = canvas.Frame(30, 20 + selectorExtra);
            IScale ys;
            IReadOnlyList<Tick> yTicks;

            for (var pass = 0; pass < 2; pass++)
            {
                (ys, var values) = AxisFactory.Numeric(yMin, yMax, plot.Bottom, plot.Top, s.YScale,
                    (int)Math.Clamp(plot.Height / 42, 3, 10), s.YDomain);

                yTicks = AxisFactory.ToTicks(ys, values, s.YFormat);
                plot = canvas.Frame(TextMetrics.MaxWidth(yTicks.Select(t => t.Label), 11), 20 + selectorExtra);
            }

            var (xs, xTicks) = MoreAxes.X(s.XAxis, xMin, xMax, plot, xDomain, s.XFormat);

            (ys, var yValues) = AxisFactory.Numeric(yMin, yMax, plot.Bottom, plot.Top, s.YScale,
                (int)Math.Clamp(plot.Height / 42, 3, 10), s.YDomain);

            yTicks = AxisFactory.ToTicks(ys, yValues, s.YFormat);

            var pointMarks = series.Where(p => p.HasPoints).Sum(p => (long)p.X.Count);
            var slots = BuildSlots(series, 300);
            canvas.LimitInteractive(pointMarks + slots.Count + s.Events.Count);
            canvas.Begin();

            canvas.YAxis(yTicks);
            canvas.XAxis(xTicks, s.Series.Any(p => p.Style == BandSeriesStyle.Points));
            canvas.Baseline(plot.Bottom);
            DrawRegions(canvas, s, xs, ys, plot);
            DrawReferences(canvas, s, xs, ys);

            for (var si = 0; si < series.Count; si++)
            {
                DrawSeries(canvas, s, series[si], si, xs, ys, pointMarks);
            }

            DrawEvents(canvas, s, xs, plot);

            if (slots.Count > 0 && canvas.Interactive)
            {
                DrawCrosshair(canvas, s, series, slots, xs, ys, plot);
            }

            if (s.RangeSelector)
            {
                DrawSelector(canvas, s, full, plot, selectorHeight, window);
            }

            if (!hasData)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles(20 + selectorExtra);

            return canvas.End();
        }

        private static List<double> BuildSlots(List<PreparedBandSeries> series, int maximum)
        {
            var set = new SortedSet<double>();

            foreach (var p in series.Where(p => p.LineLike))
            {
                foreach (var x in p.X)
                {
                    set.Add(x);
                }
            }

            return [.. MoreKit.Thin([.. set], maximum).Distinct()];
        }

        private static string YText(BandChartSpec s, double y)
        {
            return s.YFormat is null ? NumberFormat.Value(y) : s.YFormat(y);
        }

        private static void DrawRegions(ChartCanvas canvas, BandChartSpec s, IScale xs, IScale ys, Rect plot)
        {
            foreach (var band in s.Regions)
            {
                var a = band.Horizontal ? ys.Map(band.From) : xs.Map(band.From);
                var b = band.Horizontal ? ys.Map(band.To) : xs.Map(band.To);

                if (!double.IsFinite(a) || !double.IsFinite(b))
                {
                    continue;
                }

                var lo = Math.Min(a, b);
                var hi = Math.Max(a, b);

                if (band.Horizontal)
                {
                    lo = Math.Max(lo, plot.Top);
                    hi = Math.Min(hi, plot.Bottom);

                    if (hi <= lo)
                    {
                        continue;
                    }

                    canvas.W.Open("rect", ("class", "band"), ("x", Num.F(plot.Left)), ("y", Num.F(lo)),
                        ("width", Num.F(plot.Width)), ("height", Num.F(hi - lo)));
                }
                else
                {
                    lo = Math.Max(lo, plot.Left);
                    hi = Math.Min(hi, plot.Right);

                    if (hi <= lo)
                    {
                        continue;
                    }

                    canvas.W.Open("rect", ("class", "band"), ("x", Num.F(lo)), ("y", Num.F(plot.Top)),
                        ("width", Num.F(hi - lo)), ("height", Num.F(plot.Height)));
                }

                if (band.Label is not null)
                {
                    canvas.W.Element("title", band.Label);
                }

                canvas.W.Close();

                if (band.Label is not null)
                {
                    var text = TextMetrics.Truncate(band.Label, hi - lo - 8, 11);

                    if (text.Length > 0)
                    {
                        canvas.W.Element("text", text, ("class", "evt"),
                            ("x", Num.F(band.Horizontal ? plot.Left + 6 : lo + 4)),
                            ("y", Num.F(band.Horizontal ? lo + 12 : plot.Bottom - 8)));
                    }
                }
            }
        }

        private static void DrawReferences(ChartCanvas canvas, BandChartSpec s, IScale xs, IScale ys)
        {
            foreach (var r in s.References)
            {
                var x1 = xs.Map(double.IsInfinity(r.X1) ? r.X1 < 0 ? xs.DomainMin : xs.DomainMax : r.X1);
                var x2 = xs.Map(double.IsInfinity(r.X2) ? r.X2 < 0 ? xs.DomainMin : xs.DomainMax : r.X2);
                var y1 = ys.Map(double.IsInfinity(r.Y1) ? r.Y1 < 0 ? ys.DomainMin : ys.DomainMax : r.Y1);
                var y2 = ys.Map(double.IsInfinity(r.Y2) ? r.Y2 < 0 ? ys.DomainMin : ys.DomainMax : r.Y2);

                if (!double.IsFinite(x1) || !double.IsFinite(x2) || !double.IsFinite(y1) || !double.IsFinite(y2))
                {
                    continue;
                }

                canvas.W.Open("line", ("class", "ref"), ("x1", Num.F(x1)), ("y1", Num.F(y1)), ("x2", Num.F(x2)),
                    ("y2", Num.F(y2)));

                if (r.Label is not null)
                {
                    canvas.W.Element("title", r.Label);
                }

                canvas.W.Close();
            }
        }

        private static void DrawSeries(ChartCanvas canvas, BandChartSpec s, PreparedBandSeries p, int si, IScale xs,
            IScale ys, long pointMarks)
        {
            var n = p.X.Count;
            var xp = new double[n];
            var yp = new double[n];

            for (var i = 0; i < n; i++)
            {
                xp[i] = xs.Map(p.X[i]);
                yp[i] = ys.Map(p.Y[i]);
            }

            canvas.OpenSeries(si);
            var step = p.Style == BandSeriesStyle.Step;

            if (p.LineLike)
            {
                for (var b = 0; b < p.Lower.Count; b++)
                {
                    var lo = p.Lower[b].Select(ys.Map).ToArray();
                    var hi = p.Upper[b].Select(ys.Map).ToArray();
                    var cls = p.Lower.Count > 1 && b < p.Lower.Count - 1 ? "cib o" : "cib";
                    var path = BandPath(xp, lo, hi, step);

                    if (path.Length > 0)
                    {
                        canvas.W.Open("path", ("class", cls), ("d", path));
                        canvas.W.Element("title", p.Name + " " + p.BandLabels[b]);
                        canvas.W.Close();
                    }
                }

                var kf = -1;

                if (p.ForecastFrom is { } ff)
                {
                    for (var i = 0; i < n; i++)
                    {
                        if (p.X[i] <= ff)
                        {
                            kf = i;
                        }
                    }
                }

                if (kf >= 0 && kf < n - 1)
                {
                    Line(canvas, xp, yp, 0, kf, step, p.Dashed, p.Name);
                    Line(canvas, xp, yp, kf, n - 1, step, true, p.Name);
                }
                else
                {
                    Line(canvas, xp, yp, 0, n - 1, step, p.Dashed, p.Name);
                }
            }

            if (p.YErrLow is not null || p.YErrHigh is not null || p.XErrLow is not null || p.XErrHigh is not null)
            {
                var bars = new PathBuilder();

                for (var i = 0; i < n; i++)
                {
                    if (!double.IsFinite(xp[i]) || !double.IsFinite(yp[i]))
                    {
                        continue;
                    }

                    var lo = ys.Map(p.Y[i] - (p.YErrLow?[i] ?? 0));
                    var hi = ys.Map(p.Y[i] + (p.YErrHigh?[i] ?? 0));

                    if (double.IsFinite(lo) && double.IsFinite(hi) && (p.YErrLow?[i] ?? 0) + (p.YErrHigh?[i] ?? 0) > 0)
                    {
                        bars.Move(xp[i], lo).Line(xp[i], hi).Move(xp[i] - 4, lo).Line(xp[i] + 4, lo).Move(xp[i] - 4, hi)
                            .Line(xp[i] + 4, hi);
                    }

                    var xl = xs.Map(p.X[i] - (p.XErrLow?[i] ?? 0));
                    var xh = xs.Map(p.X[i] + (p.XErrHigh?[i] ?? 0));

                    if (double.IsFinite(xl) && double.IsFinite(xh) && (p.XErrLow?[i] ?? 0) + (p.XErrHigh?[i] ?? 0) > 0)
                    {
                        bars.Move(xl, yp[i]).Line(xh, yp[i]).Move(xl, yp[i] - 4).Line(xl, yp[i] + 4).Move(xh, yp[i] - 4)
                            .Line(xh, yp[i] + 4);
                    }
                }

                if (!bars.IsEmpty)
                {
                    canvas.W.Empty("path", ("class", "ebs"), ("d", bars.ToString()));
                }
            }

            if (p.HasPoints)
            {
                var radius = pointMarks > 400 ? 2.5 : 4;

                for (var i = 0; i < n; i++)
                {
                    if (!double.IsFinite(xp[i]) || !double.IsFinite(yp[i]))
                    {
                        continue;
                    }

                    if (canvas.Interactive)
                    {
                        var rows = PointRows(s, p, si, i);
                        canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)));

                        canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(xp[i])), ("cy", Num.F(yp[i])),
                            ("r", Num.F(Math.Max(radius + 4, 9))));

                        canvas.W.Empty("circle", ("class", "pt"), ("cx", Num.F(xp[i])), ("cy", Num.F(yp[i])),
                            ("r", Num.F(radius)));

                        canvas.Tooltip(xp[i], yp[i] - radius, rows);
                        canvas.CloseMark();
                    }
                    else
                    {
                        canvas.W.Empty("circle", ("class", "pt"), ("cx", Num.F(xp[i])), ("cy", Num.F(yp[i])),
                            ("r", Num.F(radius)));
                    }
                }
            }

            canvas.W.Close();
        }

        private static List<TipRow> PointRows(BandChartSpec s, PreparedBandSeries p, int si, int i)
        {
            var rows = new List<TipRow>
            {
                new(p.Name, true, si),
                new((string.IsNullOrWhiteSpace(s.Options.XAxisTitle) ? "X" : s.Options.XAxisTitle) + ": " +
                    MoreAxes.XText(s.XAxis, p.X[i], s.XFormat)),
                new((string.IsNullOrWhiteSpace(s.Options.YAxisTitle) ? "Y" : s.Options.YAxisTitle) + ": " +
                    YText(s, p.Y[i]))
            };

            if (p.YErrLow is not null || p.YErrHigh is not null)
            {
                rows.Add(new TipRow("Error: -" + NumberFormat.Value(p.YErrLow?[i] ?? 0) + " / +" +
                                    NumberFormat.Value(p.YErrHigh?[i] ?? 0)));
            }

            if (p.XErrLow is not null || p.XErrHigh is not null)
            {
                rows.Add(new TipRow("X error: -" + NumberFormat.Value(p.XErrLow?[i] ?? 0) + " / +" +
                                    NumberFormat.Value(p.XErrHigh?[i] ?? 0)));
            }

            for (var b = 0; b < p.Lower.Count; b++)
            {
                if (!p.BandHidden[b] && double.IsFinite(p.Lower[b][i]) && double.IsFinite(p.Upper[b][i]))
                {
                    rows.Add(
                        new TipRow(p.BandLabels[b] + ": " + YText(s, p.Lower[b][i]) + " to " +
                                   YText(s, p.Upper[b][i])));
                }
            }

            return rows;
        }

        private static void Line(ChartCanvas canvas, double[] xp, double[] yp, int from, int to, bool step, bool dashed,
            string name)
        {
            var path = new PathBuilder();

            for (var i = from; i <= to; i++)
            {
                if (step && i > from && double.IsFinite(xp[i]) && double.IsFinite(yp[i]) && double.IsFinite(yp[i - 1]))
                {
                    path.Line(xp[i], yp[i - 1]);
                }

                path.Line(xp[i], yp[i]);
            }

            if (path.IsEmpty)
            {
                return;
            }

            canvas.W.Open("path", ("class", dashed ? "ln dash" : "ln"), ("d", path.ToString()));
            canvas.W.Element("title", name);
            canvas.W.Close();
            canvas.W.Empty("path", ("class", "hl"), ("d", path.ToString()));
        }

        private static string BandPath(double[] xp, double[] lo, double[] hi, bool step)
        {
            var text = new StringBuilder();
            var i = 0;

            while (i < xp.Length)
            {
                if (!Ok(xp[i], lo[i], hi[i]))
                {
                    i++;
                    continue;
                }

                var start = i;

                while (i < xp.Length && Ok(xp[i], lo[i], hi[i]))
                {
                    i++;
                }

                var end = i - 1;

                if (end == start)
                {
                    continue;
                }

                var path = new PathBuilder().Move(xp[start], hi[start]);

                for (var k = start + 1; k <= end; k++)
                {
                    if (step)
                    {
                        path.Line(xp[k], hi[k - 1]);
                    }

                    path.Line(xp[k], hi[k]);
                }

                for (var k = end; k >= start; k--)
                {
                    if (step && k < end)
                    {
                        path.Line(xp[k + 1], lo[k]);
                    }

                    path.Line(xp[k], lo[k]);
                }

                text.Append(path.Close());
            }

            return text.ToString();
        }

        private static bool Ok(double x, double a, double b)
        {
            return double.IsFinite(x) && double.IsFinite(a) && double.IsFinite(b);
        }

        private static void DrawEvents(ChartCanvas canvas, BandChartSpec s, IScale xs, Rect plot)
        {
            var placed = new List<(double X, string Label, bool Cp, string Text)>();

            foreach (var ev in s.Events)
            {
                var x = xs.Map(ev.X);

                if (!double.IsFinite(x) || x < plot.Left - 0.5 || x > plot.Right + 0.5)
                {
                    continue;
                }

                placed.Add((x, ev.Label, ev.ChangePoint, MoreAxes.XText(s.XAxis, ev.X, s.XFormat)));
            }

            placed.Sort((a, b) => a.X.CompareTo(b.X));

            for (var i = 0; i < placed.Count; i++)
            {
                var (x, label, cp, when) = placed[i];
                var rows = new List<TipRow> { new(label, true), new((cp ? "Change point: " : "Event: ") + when) };
                canvas.OpenMark(-1, string.Join(", ", rows.Select(r => r.Text)));

                canvas.W.Empty("rect", ("class", "hit"), ("x", Num.F(x - 6)), ("y", Num.F(plot.Top)), ("width", "12"),
                    ("height", Num.F(plot.Height)));

                canvas.W.Empty("line", ("class", cp ? "evl cp" : "evl"), ("x1", Num.F(x)), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(x)), ("y2", Num.F(plot.Bottom)));

                canvas.W.Empty("circle", ("class", "evd"), ("cx", Num.F(x)), ("cy", Num.F(plot.Top + 4)), ("r", "3.5"));
                var room = (i + 1 < placed.Count ? placed[i + 1].X : plot.Right) - x - 10;
                var text = TextMetrics.Truncate(label, Math.Min(room, 150), 11);

                if (text.Length > 0 && room > 20)
                {
                    canvas.W.Element("text", text, ("class", "evt"), ("x", Num.F(x + 6)),
                        ("y", Num.F(plot.Top + 14 + i % 2 * 12)));
                }

                canvas.Tooltip(x, plot.Top + 24, rows, true, 8);
                canvas.CloseMark();
            }
        }

        private static int Nearest(List<double> xs, double x, double tolerance)
        {
            var lo = 0;
            var hi = xs.Count - 1;

            while (lo < hi)
            {
                var mid = (lo + hi) / 2;

                if (xs[mid] < x)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            var best = -1;
            var bestDistance = tolerance;

            for (var k = Math.Max(lo - 1, 0); k <= Math.Min(lo, xs.Count - 1); k++)
            {
                var d = Math.Abs(xs[k] - x);

                if (d <= bestDistance)
                {
                    bestDistance = d;
                    best = k;
                }
            }

            return best;
        }

        private static void DrawCrosshair(ChartCanvas canvas, BandChartSpec s, List<PreparedBandSeries> series,
            List<double> slots, IScale xs, IScale ys, Rect plot)
        {
            var px = slots.Select(xs.Map).ToArray();
            var spacing = slots.Count > 1 ? (slots[^1] - slots[0]) / (slots.Count - 1) : 0;

            for (var k = 0; k < slots.Count; k++)
            {
                if (!double.IsFinite(px[k]))
                {
                    continue;
                }

                var left = k == 0 ? plot.Left : (px[k - 1] + px[k]) / 2;
                var right = k == slots.Count - 1 ? plot.Right : (px[k] + px[k + 1]) / 2;
                var rows = new List<TipRow> { new(MoreAxes.XText(s.XAxis, slots[k], s.XFormat), true) };
                var dots = new List<(int Series, double Y)>();
                var top = double.PositiveInfinity;

                for (var si = 0; si < series.Count; si++)
                {
                    var p = series[si];

                    if (!p.LineLike)
                    {
                        continue;
                    }

                    var i = Nearest(p.X, slots[k], spacing / 2 + 1e-9);

                    if (i < 0 || !double.IsFinite(p.Y[i]))
                    {
                        continue;
                    }

                    var forecast = p.ForecastFrom is { } ff && p.X[i] > ff;

                    rows.Add(new TipRow(
                        p.Name + (forecast && !p.Name.Contains("forecast", StringComparison.OrdinalIgnoreCase)
                            ? " (forecast)"
                            : string.Empty) + ": " + YText(s, p.Y[i]), false, si));

                    for (var b = 0; b < p.Lower.Count && rows.Count < 14; b++)
                    {
                        if (!p.BandHidden[b] && double.IsFinite(p.Lower[b][i]) && double.IsFinite(p.Upper[b][i]))
                        {
                            rows.Add(new TipRow("   " + p.BandLabels[b] + ": " + YText(s, p.Lower[b][i]) + " to " +
                                                YText(s, p.Upper[b][i])));
                        }
                    }

                    var y = ys.Map(p.Y[i]);

                    if (double.IsFinite(y))
                    {
                        dots.Add((si, y));
                        top = Math.Min(top, y);
                    }
                }

                if (rows.Count == 1)
                {
                    continue;
                }

                canvas.OpenMark(-1, string.Join(", ", rows.Select(r => r.Text)));

                canvas.W.Empty("rect", ("class", "hit"), ("x", Num.F(left)), ("y", Num.F(plot.Top)),
                    ("width", Num.F(Math.Max(right - left, 1))), ("height", Num.F(plot.Height)));

                canvas.W.Empty("line", ("class", "vl"), ("x1", Num.F(px[k])), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(px[k])),
                    ("y2", Num.F(plot.Bottom)));

                foreach (var (si, y) in dots)
                {
                    canvas.W.Empty("circle", ("class", "cx s" + si % ChartLimits.MaxSeries), ("cx", Num.F(px[k])),
                        ("cy", Num.F(y)), ("r", "4.5"));
                }

                canvas.Tooltip(px[k], double.IsFinite(top) ? top : plot.CenterY, rows, true, 12);
                canvas.CloseMark();
            }
        }

        private static void DrawSelector(ChartCanvas canvas, BandChartSpec s, List<PreparedBandSeries> full,
            Rect plot, double height, RangeWindow? window)
        {
            var top = plot.Bottom + 28;
            var bottom = top + height;
            var xe = new Extent();
            var ye = new Extent();

            foreach (var p in full)
            {
                foreach (var x in p.X)
                {
                    xe.Add(x);
                }

                foreach (var y in p.Y)
                {
                    ye.Add(y);
                }
            }

            if (!xe.Any || !ye.Any)
            {
                return;
            }

            var sx = new LinearScale(xe.Min, xe.Max, plot.Left, plot.Right);
            var sy = new LinearScale(ye.Min, ye.Max, bottom - 4, top + 4);

            canvas.W.Empty("rect", ("class", "rsb"), ("x", Num.F(plot.Left)), ("y", Num.F(top)),
                ("width", Num.F(plot.Width)),
                ("height", Num.F(height)), ("rx", "3"));

            for (var si = 0; si < full.Count; si++)
            {
                var p = full[si];

                if (!p.LineLike)
                {
                    continue;
                }

                var path = new PathBuilder();
                var thin = MoreKit.Thin([.. Enumerable.Range(0, p.X.Count)], 400);

                foreach (var i in thin)
                {
                    path.Line(sx.Map(p.X[i]), sy.Map(p.Y[i]));
                }

                if (!path.IsEmpty)
                {
                    canvas.W.Empty("path", ("class", "rsl s" + si % ChartLimits.MaxSeries), ("d", path.ToString()));
                }
            }

            var from = window?.From ?? xe.Min;
            var to = window?.To ?? xe.Max;
            var x0 = Math.Clamp(sx.Map(from), plot.Left, plot.Right);
            var x1 = Math.Clamp(sx.Map(to), plot.Left, plot.Right);

            if (window is not null)
            {
                if (x0 > plot.Left)
                {
                    canvas.W.Empty("rect", ("class", "rsd"), ("x", Num.F(plot.Left)), ("y", Num.F(top)),
                        ("width", Num.F(x0 - plot.Left)), ("height", Num.F(height)));
                }

                if (x1 < plot.Right)
                {
                    canvas.W.Empty("rect", ("class", "rsd"), ("x", Num.F(x1)), ("y", Num.F(top)),
                        ("width", Num.F(plot.Right - x1)), ("height", Num.F(height)));
                }
            }

            var label = "Selected: " + MoreAxes.XText(s.XAxis, from, s.XFormat) + " to " +
                        MoreAxes.XText(s.XAxis, to, s.XFormat);
            canvas.OpenMark(-1, label);

            canvas.W.Empty("rect", ("class", "rsw"), ("x", Num.F(x0)), ("y", Num.F(top)),
                ("width", Num.F(Math.Max(x1 - x0, 1))),
                ("height", Num.F(height)));

            canvas.W.Empty("rect", ("class", "rsh"), ("x", Num.F(x0 - 3)), ("y", Num.F(top + height / 2 - 10)),
                ("width", "6"),
                ("height", "20"), ("rx", "2"));
            canvas.W.Empty("rect", ("class", "rsh"), ("x", Num.F(x1 - 3)), ("y", Num.F(top + height / 2 - 10)),
                ("width", "6"),
                ("height", "20"), ("rx", "2"));

            canvas.Tooltip((x0 + x1) / 2, top, [new TipRow(label)], false, 6);
            canvas.CloseMark();
            canvas.Text(plot.Left, bottom + 13, MoreAxes.XText(s.XAxis, xe.Min, s.XFormat), "tk");
            canvas.Text(plot.Right, bottom + 13, MoreAxes.XText(s.XAxis, xe.Max, s.XFormat), "tk end");
        }
    }
}