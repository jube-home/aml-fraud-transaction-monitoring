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

using System.Globalization;
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
    internal static class XyRenderer
    {
        public static string Render(XyModel m, string id)
        {
            var series = m.Series;
            ChartLimits.Series(series.Count);
            var total = series.Sum(s => (long)s.Points.Count);
            ChartLimits.Points(total);

            if (m.Categories is not null)
            {
                ChartLimits.Categories(m.Categories.Count);
            }

            var options = m.Options;
            var names = series.Select(s => s.Name).ToArray();
            var colours = Guard.Colours(series.Select(s => s.Color));
            var legendNames = m.LegendOverride ?? names;

            var legend = legendNames.Select((n, i) => new LegendEntry(n, i,
                m.Mode is XyMode.Line ? LegendShape.Line : LegendShape.Square)).ToList();

            var xExtent = new Extent();
            var yExtent = new Extent();
            var y2Extent = new Extent();
            var stackHigh = m.Stacked ? Stack(m) : null;

            for (var si = 0; si < series.Count; si++)
            {
                for (var pi = 0; pi < series[si].Points.Count; pi++)
                {
                    var p = series[si].Points[pi];

                    if (!double.IsFinite(p.X))
                    {
                        continue;
                    }

                    if (m.XAxis == XAxisKind.Log && p.X <= 0)
                    {
                        continue;
                    }

                    xExtent.Add(p.X);
                    var y = stackHigh is null ? p.Y : stackHigh[si][pi];

                    if (m.YScale == AxisKind.Log && y <= 0)
                    {
                        continue;
                    }

                    if (series[si].SecondaryAxis)
                    {
                        y2Extent.Add(y);
                    }
                    else
                    {
                        yExtent.Add(y);
                    }
                }
            }

            foreach (var r in m.References)
            {
                if (m.XDomain is null)
                {
                    xExtent.Add(r.X1);
                    xExtent.Add(r.X2);
                }

                if (m.YDomain is null)
                {
                    yExtent.Add(r.Y1);
                    yExtent.Add(r.Y2);
                }
            }

            var hasData = (yExtent.Any || y2Extent.Any) && xExtent.Any;

            if (m.Mode == XyMode.Area || m.YFromZero)
            {
                yExtent.Add(0);
            }

            foreach (var b in m.Bands.Where(b => b.Horizontal))
            {
                yExtent.Add(b.From);
                yExtent.Add(b.To);
            }

            foreach (var b in m.Bands.Where(b => !b.Horizontal))
            {
                xExtent.Add(b.From);
                xExtent.Add(b.To);
            }

            foreach (var a in m.Annotations)
            {
                xExtent.Add(a.X);
                yExtent.Add(a.Y);
            }

            var yMin = yExtent.Any ? yExtent.Min : 0;
            var yMax = yExtent.Any ? yExtent.Max : 1;
            var xMin = xExtent.Any ? xExtent.Min : 0;
            var xMax = xExtent.Any ? xExtent.Max : 1;
            var yDomain = m.PercentStack ? (0.0, 100.0) : m.YDomain;
            var yFormat = m.PercentStack ? v => NumberFormat.Value(v) + "%" : m.YFormat;

            var summary = Guard.Summary(m.Kind, names, hasData
                ? "Y values range from " + NumberFormat.Value(yMin) + " to " + NumberFormat.Value(yMax) + ". " +
                  m.SummaryDetail
                : "There is no data to plot.");

            var canvas = new ChartCanvas(options, id, summary.Trim(), legend, series.Count, colours);
            var showStrip = m.Strip is { Count: > 0 };
            var stripExtra = showStrip ? StripHeight + StripGap + 4 : 0;

            var yTarget = 5;
            IScale yScale;
            IReadOnlyList<double> yValues;
            IReadOnlyList<Tick> yTicks;
            IReadOnlyList<Tick> y2Ticks;
            var plot = canvas.Frame(30, 20 + stripExtra);

            for (var pass = 0; pass < 2; pass++)
            {
                yTarget = (int)Math.Clamp(plot.Height / 42, 3, 10);
                (yScale, yValues) = AxisFactory.Numeric(yMin, yMax, plot.Bottom, plot.Top, m.YScale, yTarget, yDomain);
                yTicks = AxisFactory.ToTicks(yScale, yValues, yFormat);
                var leftWidth = TextMetrics.MaxWidth(yTicks.Select(t => t.Label), 11);
                (_, y2Ticks) = Secondary(m, y2Extent, plot, yTarget);

                var rightReserve = y2Ticks.Count == 0
                    ? 0
                    : TextMetrics.MaxWidth(y2Ticks.Select(t => t.Label), 11) + 10 +
                      (string.IsNullOrWhiteSpace(options.SecondaryYAxisTitle) ? 0 : 18);

                plot = canvas.Frame(leftWidth, 20 + stripExtra, rightReserve);
            }

            var (xScale, xTicks) = BuildX(m, xMin, xMax, plot);
            (yScale, yValues) = AxisFactory.Numeric(yMin, yMax, plot.Bottom, plot.Top, m.YScale, yTarget, yDomain);
            yTicks = AxisFactory.ToTicks(yScale, yValues, yFormat);
            (var y2Scale, y2Ticks) = Secondary(m, y2Extent, plot, yTarget);

            var pointMarksAllowed = total <= ChartLimits.InteractiveMarksPerChart;
            var slotXs = m.Crosshair ? SlotXs(series) : [];
            canvas.LimitInteractive(m.Crosshair ? slotXs.Count : 0);
            canvas.Begin();

            canvas.YAxis(yTicks);
            DrawBands(canvas, m, xScale, yScale, plot);
            DrawSecondaryAxis(canvas, y2Ticks, plot, options);

            canvas.XAxis(showStrip ? [] : xTicks,
                m.XAxis != XAxisKind.Category && m.Mode is XyMode.Scatter or XyMode.Bubble);

            if (showStrip)
            {
                foreach (var t in xTicks)
                {
                    canvas.W.Empty("line", ("class", "gl"), ("x1", Num.F(t.Position)), ("y1", Num.F(plot.Top)),
                        ("x2", Num.F(t.Position)), ("y2", Num.F(plot.Bottom)));
                }
            }

            var baselineY = yScale.Map(Math.Clamp(0, yScale.DomainMin, yScale.DomainMax));
            canvas.Baseline(m.Mode == XyMode.Area && double.IsFinite(baselineY) ? baselineY : plot.Bottom);

            if (m.Mode is XyMode.Scatter or XyMode.Bubble && m.XAxis != XAxisKind.Category)
            {
                canvas.VerticalBaseline(plot.Left);
            }

            foreach (var r in m.References)
            {
                var x1 = xScale.Map(Resolve(r.X1, xScale));
                var y1 = yScale.Map(Resolve(r.Y1, yScale));
                var x2 = xScale.Map(Resolve(r.X2, xScale));
                var y2 = yScale.Map(Resolve(r.Y2, yScale));

                if (double.IsFinite(x1) && double.IsFinite(y1) && double.IsFinite(x2) && double.IsFinite(y2))
                {
                    canvas.W.Open("line", ("class", r.Series is { } rs ? "ref s" + rs % ChartLimits.MaxSeries : "ref"),
                        ("x1", Num.F(x1)), ("y1", Num.F(y1)), ("x2", Num.F(x2)),
                        ("y2", Num.F(y2)));

                    if (r.Label is not null)
                    {
                        canvas.W.Element("title", r.Label);
                    }

                    canvas.W.Close();
                }
            }

            var maxPer = series.Count == 0 ? 0 : series.Max(s => s.Points.Count);

            var showMarkers = m.Markers switch
            {
                MarkerMode.On => true,
                MarkerMode.Off => false,
                _ => m.Mode == XyMode.Line && maxPer <= 40
            };

            for (var si = 0; si < series.Count; si++)
            {
                DrawSeries(canvas, m, series, si, xScale,
                    series[si].SecondaryAxis && y2Scale is not null ? y2Scale : yScale,
                    stackHigh, showMarkers, total, pointMarksAllowed);
            }

            if (m.Crosshair)
            {
                DrawCrosshair(canvas, m, series, slotXs, xScale, yScale, plot);
            }

            DrawAnnotations(canvas, m, xScale, yScale);

            foreach (var h in m.Highlights)
            {
                var hx = xScale.Map(h.X);
                var hy = yScale.Map(h.Y);

                if (!double.IsFinite(hx) || !double.IsFinite(hy))
                {
                    continue;
                }

                var lines = h.Label.Split('\n');
                canvas.OpenMark(h.Series, string.Join(", ", lines));

                if (h.ShowAlways)
                {
                    canvas.W.Empty("circle", ("class", "mkr m s" + h.Series % ChartLimits.MaxSeries), ("cx", Num.F(hx)),
                        ("cy", Num.F(hy)), ("r", "6"));
                }
                else
                {
                    canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(hx)), ("cy", Num.F(hy)), ("r", "9"));

                    canvas.W.Empty("circle", ("class", "pt h s" + h.Series % ChartLimits.MaxSeries), ("cx", Num.F(hx)),
                        ("cy", Num.F(hy)), ("r", "4.5"));
                }

                canvas.Tooltip(hx, hy - 5, [.. lines.Select((l, i) => new TipRow(l, i == 0))]);
                canvas.CloseMark();
            }

            if (showStrip)
            {
                DrawStrip(canvas, m, xScale, plot);
            }

            if (!hasData)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles(showStrip ? 20 + stripExtra : 20);

            return canvas.End();
        }

        private const double StripHeight = 56;
        private const double StripGap = 14;

        private static (IScale, IReadOnlyList<Tick>) BuildX(XyModel m, double xMin, double xMax, Rect plot)
        {
            if (m.XAxis == XAxisKind.Category)
            {
                var n = Math.Max(m.Categories?.Count ?? 0, 1);
                var scale = new LinearScale(-0.5, n - 0.5, plot.Left, plot.Right);
                var ticks = new List<Tick>();

                for (var i = 0; i < n; i++)
                {
                    var label = m.Categories is not null && i < m.Categories.Count
                        ? m.Categories[i]
                        : (i + 1).ToString(CultureInfo.InvariantCulture);

                    ticks.Add(new Tick(i, scale.Map(i), label));
                }

                return (scale, ticks);
            }

            if (m.XAxis == XAxisKind.Time)
            {
                var scale = new LinearScale(xMin, xMax, plot.Left, plot.Right);
                var count = (int)Math.Clamp(plot.Width / 90, 2, 12);

                var ticks = TimeTicks.Generate(TimeScale.FromSeconds(scale.DomainMin),
                    TimeScale.FromSeconds(scale.DomainMax),
                    count);

                return (scale, [.. ticks.Select(t => new Tick(t.Seconds, scale.Map(t.Seconds), t.Label))]);
            }

            var target = (int)Math.Clamp(plot.Width / 80, 3, 12);

            var (xs, values) = AxisFactory.Numeric(xMin, xMax, plot.Left, plot.Right,
                m.XAxis == XAxisKind.Log ? AxisKind.Log : AxisKind.Linear, target, m.XDomain);

            return (xs, AxisFactory.ToTicks(xs, values, m.XFormat));
        }

        private static double[][] Stack(XyModel m)
        {
            var series = m.Series;
            var n = series.Count == 0 ? 0 : series[0].Points.Count;

            foreach (var s in series)
            {
                if (s.Points.Count != n)
                {
                    throw new ChartSpecificationException("Stacked series must have the same number of points.");
                }
            }

            var totals = new double[n];

            if (m.PercentStack)
            {
                foreach (var t in series)
                {
                    for (var pi = 0; pi < n; pi++)
                    {
                        totals[pi] += double.IsFinite(t.Points[pi].Y) ? Math.Max(t.Points[pi].Y, 0) : 0;
                    }
                }
            }

            var result = new double[series.Count][];
            var running = new double[n];

            for (var si = 0; si < series.Count; si++)
            {
                result[si] = new double[n];

                for (var pi = 0; pi < n; pi++)
                {
                    var y = series[si].Points[pi].Y;
                    var add = double.IsFinite(y) ? y : 0;
                    running[pi] += m.PercentStack ? totals[pi] > 0 ? Math.Max(add, 0) / totals[pi] * 100 : 0 : add;
                    result[si][pi] = running[pi];
                }
            }

            return result;
        }

        private static List<double> SlotXs(IReadOnlyList<PointSeries> series)
        {
            var set = new SortedSet<double>();

            foreach (var s in series)
            {
                foreach (var p in s.Points)
                {
                    if (double.IsFinite(p.X) && double.IsFinite(p.Y))
                    {
                        set.Add(p.X);
                    }
                }
            }

            return [.. set];
        }

        private static string XText(XyModel m, double x)
        {
            if (m is { XAxis: XAxisKind.Category, Categories: not null })
            {
                var i = (int)Math.Round(x);

                return i >= 0 && i < m.Categories.Count ? m.Categories[i] : NumberFormat.Value(x);
            }

            if (m.XAxis == XAxisKind.Time)
            {
                var t = TimeScale.FromSeconds(x);

                return t.TimeOfDay == TimeSpan.Zero
                    ? t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }

            return m.XFormat is null ? NumberFormat.Value(x) : m.XFormat(x);
        }

        private static string YText(XyModel m, double y)
        {
            return m.YFormat is null ? NumberFormat.Value(y) : m.YFormat(y);
        }

        private static void DrawSeries(ChartCanvas canvas, XyModel m, IReadOnlyList<PointSeries> series, int si,
            IScale xs, IScale ys, double[][]? stack, bool showMarkers, long total, bool pointMarksAllowed)
        {
            var s = series[si];
            var xp = new double[s.Points.Count];
            var yp = new double[s.Points.Count];
            var base0 = ys.Map(Math.Clamp(0, ys.DomainMin, ys.DomainMax));
            var lowerPx = new double[s.Points.Count];

            for (var i = 0; i < s.Points.Count; i++)
            {
                var p = s.Points[i];
                xp[i] = double.IsFinite(p.X) ? xs.Map(p.X) : double.NaN;
                var y = stack is null ? p.Y : stack[si][i];
                yp[i] = double.IsFinite(p.Y) ? ys.Map(y) : double.NaN;
                lowerPx[i] = stack is null || si == 0 ? base0 : ys.Map(stack[si - 1][i]);

                if (!double.IsFinite(lowerPx[i]))
                {
                    lowerPx[i] = base0;
                }
            }

            canvas.OpenSeries(si);

            if (m.Mode == XyMode.Area)
            {
                DrawAreaSegments(canvas, xp, yp, lowerPx, m.Smooth);
            }

            if (m.Mode is XyMode.Line or XyMode.Area || m.ConnectPoints)
            {
                var path = new PathBuilder();

                if (m is { Smooth: true, Step: false })
                {
                    AppendSmooth(path, xp, yp);
                }
                else
                {
                    for (var i = 0; i < xp.Length; i++)
                    {
                        if (m.Step && i > 0 && double.IsFinite(xp[i]) && double.IsFinite(yp[i]) &&
                            double.IsFinite(yp[i - 1]))
                        {
                            path.Line(xp[i], yp[i - 1]);
                        }

                        path.Line(xp[i], yp[i]);
                    }
                }

                if (!path.IsEmpty)
                {
                    canvas.W.Open("path", ("class", "ln"), ("d", path.ToString()));
                    canvas.W.Element("title", s.Name);
                    canvas.W.Close();
                }
            }

            var isolated = new HashSet<int>();

            for (var i = 0; i < xp.Length; i++)
            {
                var self = double.IsFinite(xp[i]) && double.IsFinite(yp[i]);
                var prev = i > 0 && double.IsFinite(xp[i - 1]) && double.IsFinite(yp[i - 1]);
                var next = i + 1 < xp.Length && double.IsFinite(xp[i + 1]) && double.IsFinite(yp[i + 1]);

                if (self && !prev && !next && m.Mode is XyMode.Line or XyMode.Area)
                {
                    isolated.Add(i);
                }
            }

            var sizeScale = m.Mode == XyMode.Bubble ? BubbleScale(series) : null;
            var scatterRadius = total > 2000 ? 2.5 : 4.5;
            var crosshair = m.Crosshair;

            for (var i = 0; i < xp.Length; i++)
            {
                if (!double.IsFinite(xp[i]) || !double.IsFinite(yp[i]))
                {
                    continue;
                }

                var p = s.Points[i];
                var isPoint = m.Mode is XyMode.Scatter or XyMode.Bubble;
                var drawDot = isPoint || showMarkers || isolated.Contains(i);

                var interactive = canvas.Interactive && pointMarksAllowed && !crosshair &&
                                  (isPoint || s.Points.Count <= ChartLimits.InteractivePointsPerSeries);

                var radius = m.Mode == XyMode.Bubble ? sizeScale!(p.Size) : isPoint ? scatterRadius : 3.5;
                var dotClass = m.Mode == XyMode.Bubble ? "bub" : "pt";

                if (total > 2000 && isPoint)
                {
                    dotClass += " sm";
                }

                if (interactive)
                {
                    var rows = PointRows(m, s, si, p, series.Count > 1);
                    canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(xp[i])), ("cy", Num.F(yp[i])),
                        ("r", Num.F(Math.Max(radius + 4, 9))));

                    canvas.W.Empty("circle", ("class", dotClass + (drawDot ? string.Empty : " h")),
                        ("cx", Num.F(xp[i])),
                        ("cy", Num.F(yp[i])), ("r", Num.F(radius)));

                    canvas.Tooltip(xp[i], yp[i] - radius, rows);
                    canvas.CloseMark();
                }
                else if (drawDot)
                {
                    canvas.W.Empty("circle", ("class", dotClass), ("cx", Num.F(xp[i])), ("cy", Num.F(yp[i])),
                        ("r", Num.F(radius)));
                }
            }

            if (m.DataLabels && s.Points.Count <= 40)
            {
                for (var i = 0; i < xp.Length; i++)
                {
                    if (double.IsFinite(xp[i]) && double.IsFinite(yp[i]))
                    {
                        canvas.W.Element("text", YText(m, s.Points[i].Y), ("class", "lbl ctr"), ("x", Num.F(xp[i])),
                            ("y", Num.F(yp[i] - 9)));
                    }
                }
            }

            canvas.W.Close();
        }

        private static List<TipRow> PointRows(XyModel m, PointSeries s, int si, DataPoint p, bool multi)
        {
            var rows = new List<TipRow>();

            if (!string.IsNullOrEmpty(p.Label))
            {
                rows.Add(new TipRow(p.Label, true));
            }

            if (multi || string.IsNullOrEmpty(p.Label))
            {
                rows.Add(new TipRow(s.Name, true, si));
            }

            var xName = string.IsNullOrWhiteSpace(m.Options.XAxisTitle) ? "X" : m.Options.XAxisTitle;

            var yName = m.TipYNames is { } names && si < names.Count && !string.IsNullOrWhiteSpace(names[si])
                ? names[si]!
                : string.IsNullOrWhiteSpace(m.Options.YAxisTitle)
                    ? "Y"
                    : m.Options.YAxisTitle;

            rows.Add(new TipRow(xName + ": " + XText(m, p.X)));
            rows.Add(new TipRow(yName + ": " + YText(m, p.Y)));

            if (m.Mode == XyMode.Bubble && double.IsFinite(p.Size))
            {
                rows.Add(new TipRow("Size: " + NumberFormat.Value(p.Size)));
            }

            return rows;
        }

        private static Func<double, double> BubbleScale(IReadOnlyList<PointSeries> series)
        {
            var e = new Extent();

            foreach (var s in series)
            {
                foreach (var p in s.Points)
                {
                    if (p.Size > 0)
                    {
                        e.Add(p.Size);
                    }
                }
            }

            var max = e.Any ? e.Max : 1;

            return size => !double.IsFinite(size) ? 5 : size <= 0 ? 3 : 3 + 19 * Math.Sqrt(size / max);
        }

        private static void DrawAreaSegments(ChartCanvas canvas, double[] xp, double[] yp, double[] lower, bool smooth)
        {
            var i = 0;

            while (i < xp.Length)
            {
                if (!double.IsFinite(xp[i]) || !double.IsFinite(yp[i]))
                {
                    i++;
                    continue;
                }

                var start = i;

                while (i < xp.Length && double.IsFinite(xp[i]) && double.IsFinite(yp[i]))
                {
                    i++;
                }

                var end = i - 1;
                var path = new PathBuilder();
                var count = end - start + 1;
                var top = new ArraySegment<double>(yp, start, count);
                var xs = new ArraySegment<double>(xp, start, count);
                var bottom = new double[count];
                var xr = new double[count];

                for (var k = 0; k < count; k++)
                {
                    bottom[k] = lower[end - k];
                    xr[k] = xp[end - k];
                }

                if (smooth)
                {
                    Spline.Append(path, xs, top, false);
                    Spline.Append(path, xr, bottom, true);
                }
                else
                {
                    path.Move(xp[start], yp[start]);

                    for (var k = start + 1; k <= end; k++)
                    {
                        path.Line(xp[k], yp[k]);
                    }

                    for (var k = end; k >= start; k--)
                    {
                        path.Line(xp[k], lower[k]);
                    }
                }

                path.Close();
                canvas.W.Empty("path", ("class", "ar"), ("d", path.ToString()));
            }
        }

        private static void DrawCrosshair(ChartCanvas canvas, XyModel m, IReadOnlyList<PointSeries> series,
            List<double> slots, IScale xs, IScale ys, Rect plot)
        {
            if (!canvas.Interactive || slots.Count == 0)
            {
                return;
            }

            var lookups = series.Select(s =>
            {
                var d = new Dictionary<double, DataPoint>();

                foreach (var p in s.Points)
                {
                    if (double.IsFinite(p.X) && double.IsFinite(p.Y))
                    {
                        d.TryAdd(p.X, p);
                    }
                }

                return d;
            }).ToArray();

            var px = slots.Select(xs.Map).ToArray();

            for (var k = 0; k < slots.Count; k++)
            {
                var left = k == 0 ? plot.Left : (px[k - 1] + px[k]) / 2;
                var right = k == slots.Count - 1 ? plot.Right : (px[k] + px[k + 1]) / 2;

                var header = m.XAxis is XAxisKind.Linear or XAxisKind.Log &&
                             !string.IsNullOrWhiteSpace(m.Options.XAxisTitle)
                    ? m.Options.XAxisTitle + " " + XText(m, slots[k])
                    : XText(m, slots[k]);

                var rows = new List<TipRow> { new(header, true) };
                var top = double.PositiveInfinity;
                var dots = new List<(int Series, double Y)>();

                for (var si = 0; si < series.Count; si++)
                {
                    if (!lookups[si].TryGetValue(slots[k], out var p))
                    {
                        continue;
                    }

                    rows.Add(new TipRow(series[si].Name + ": " + YText(m, p.Y), false, si));
                    var y = ys.Map(p.Y);

                    if (double.IsFinite(y))
                    {
                        dots.Add((si, y));
                        top = Math.Min(top, y);
                    }
                }

                canvas.OpenMark(-1, string.Join(", ", rows.Select(r => r.Text)));

                canvas.W.Empty("rect", ("class", "hit"), ("x", Num.F(left)), ("y", Num.F(plot.Top)),
                    ("width", Num.F(Math.Max(right - left, 1))), ("height", Num.F(plot.Height)));

                canvas.W.Empty("line", ("class", "vl"), ("x1", Num.F(px[k])), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(px[k])), ("y2", Num.F(plot.Bottom)));

                foreach (var (si, y) in dots)
                {
                    canvas.W.Empty("circle", ("class", "cx s" + si % ChartLimits.MaxSeries), ("cx", Num.F(px[k])),
                        ("cy", Num.F(y)), ("r", "4.5"));
                }

                canvas.Tooltip(px[k], double.IsFinite(top) ? top : plot.CenterY, rows, true, 12);
                canvas.CloseMark();
            }
        }

        private static void DrawStrip(ChartCanvas canvas, XyModel m, IScale xs, Rect plot)
        {
            var bars = m.Strip!;
            var top = plot.Bottom + StripGap;
            var bottom = top + StripHeight;
            var max = Math.Max(bars.Max(b => b.Count), 1);

            canvas.W.Empty("line", ("class", "ax"), ("x1", Num.F(plot.Left)), ("y1", Num.F(bottom)),
                ("x2", Num.F(plot.Right)), ("y2", Num.F(bottom)));

            canvas.Text(plot.Left - 8, top + 8, m.StripTitle ?? "Count", "tk end");
            var labelY = bottom + 16;
            var lastEnd = double.NegativeInfinity;

            foreach (var t in AxisFactory.ToTicks(xs, [
                         .. ExtendedTicks.Generate(xs.DomainMin, xs.DomainMax,
                                 (int)Math.Clamp(plot.Width / 80, 3, 12))
                             .Where(v => v >= xs.DomainMin - 1e-9 && v <= xs.DomainMax + 1e-9)
                     ], m.XFormat))
            {
                var half = TextMetrics.Width(t.Label, 11) / 2;

                if (t.Position - half < lastEnd + 6)
                {
                    continue;
                }

                lastEnd = t.Position + half;
                canvas.Text(t.Position, labelY, t.Label, "tk ctr");
            }

            for (var i = 0; i < bars.Count; i++)
            {
                var b = bars[i];
                var x0 = xs.Map(b.X0);
                var x1 = xs.Map(b.X1);

                if (!double.IsFinite(x0) || !double.IsFinite(x1))
                {
                    continue;
                }

                var h = b.Count / max * (StripHeight - 4);

                var label = "Bin " + NumberFormat.Value(b.X0) + " to " + NumberFormat.Value(b.X1) + ": " +
                            NumberFormat.Value(b.Count);
                canvas.OpenMark(0, label);

                canvas.W.Empty("rect", ("class", "bar m s0"), ("x", Num.F(x0 + 1)), ("y", Num.F(bottom - h)),
                    ("width", Num.F(Math.Max(x1 - x0 - 2, 1))), ("height", Num.F(h)), ("rx", "2"));

                canvas.Tooltip((x0 + x1) / 2, bottom - h,
                [
                    new TipRow("Bin " + NumberFormat.Value(b.X0) + " to " + NumberFormat.Value(b.X1), true),
                    new TipRow("Count: " + NumberFormat.Value(b.Count))
                ]);

                canvas.CloseMark();
            }
        }

        private static (IScale? Scale, IReadOnlyList<Tick> Ticks) Secondary(XyModel m, Extent extent, Rect plot,
            int target)
        {
            if (!extent.Any)
            {
                return (null, []);
            }

            var (scale, values) = AxisFactory.Numeric(extent.Min, extent.Max, plot.Bottom, plot.Top, m.YScale, target);

            return (scale, AxisFactory.ToTicks(scale, values, m.Y2Format));
        }

        private static void DrawSecondaryAxis(ChartCanvas canvas, IReadOnlyList<Tick> ticks, Rect plot,
            ChartOptions options)
        {
            foreach (var t in ticks.Where(t => double.IsFinite(t.Position)))
            {
                canvas.W.Element("text", t.Label, ("class", "tk mid"), ("x", Num.F(plot.Right + 8)),
                    ("y", Num.F(t.Position)));
            }

            if (ticks.Count > 0 && !string.IsNullOrWhiteSpace(options.SecondaryYAxisTitle))
            {
                var x = canvas.Width - ChartCanvas.Pad + 4;

                canvas.W.Element("text", TextMetrics.Truncate(options.SecondaryYAxisTitle, plot.Height, 12),
                    ("class", "at ctr"), ("x", "0"), ("y", "0"),
                    ("transform", "translate(" + Num.F(x) + " " + Num.F(plot.CenterY) + ") rotate(90)"));
            }
        }

        private static void DrawBands(ChartCanvas canvas, XyModel m, IScale xs, IScale ys, Rect plot)
        {
            foreach (var b in m.Bands)
            {
                var a = b.Horizontal ? ys.Map(b.From) : xs.Map(b.From);
                var c = b.Horizontal ? ys.Map(b.To) : xs.Map(b.To);

                if (!double.IsFinite(a) || !double.IsFinite(c))
                {
                    continue;
                }

                var lo = Math.Min(a, c);
                var size = Math.Abs(c - a);

                if (b.Horizontal)
                {
                    canvas.W.Empty("rect", ("class", "band"), ("x", Num.F(plot.Left)), ("y", Num.F(lo)),
                        ("width", Num.F(plot.Width)), ("height", Num.F(size)));
                }
                else
                {
                    canvas.W.Empty("rect", ("class", "band"), ("x", Num.F(lo)), ("y", Num.F(plot.Top)),
                        ("width", Num.F(size)), ("height", Num.F(plot.Height)));
                }

                if (!string.IsNullOrEmpty(b.Label))
                {
                    var label = TextMetrics.Truncate(b.Label, b.Horizontal ? plot.Width : size, 11);

                    canvas.Text(b.Horizontal ? plot.Left + 6 : lo + 4, b.Horizontal ? lo + 13 : plot.Top + 13, label,
                        "tk");
                }
            }
        }

        private static void DrawAnnotations(ChartCanvas canvas, XyModel m, IScale xs, IScale ys)
        {
            var plot = canvas.Plot;

            foreach (var a in m.Annotations)
            {
                var x = xs.Map(a.X);
                var y = ys.Map(a.Y);

                if (!double.IsFinite(x) || !double.IsFinite(y))
                {
                    continue;
                }

                canvas.W.Open("g", ("class", canvas.External ? null : "an"));
                canvas.W.Element("title", a.Text);
                canvas.W.Empty("circle", ("class", "and"), ("cx", Num.F(x)), ("cy", Num.F(y)), ("r", "3.5"));
                var text = TextMetrics.Truncate(a.Text, 180, 11);
                var width = TextMetrics.Width(text, 11);
                var right = x + 8 + width > plot.Right;

                canvas.W.Element("text", text, ("class", right ? "ant end" : "ant"),
                    ("x", Num.F(right ? x - 8 : x + 8)),
                    ("y", Num.F(Math.Max(y - 6, plot.Top + 10))));

                canvas.W.Close();
            }
        }

        private static void AppendSmooth(PathBuilder path, double[] xp, double[] yp)
        {
            var i = 0;

            while (i < xp.Length)
            {
                if (!double.IsFinite(xp[i]) || !double.IsFinite(yp[i]))
                {
                    i++;
                    continue;
                }

                var start = i;

                while (i < xp.Length && double.IsFinite(xp[i]) && double.IsFinite(yp[i]))
                {
                    i++;
                }

                Spline.Append(path, xp[start..i], yp[start..i], false);
            }
        }

        private static double Resolve(double value, IScale scale)
        {
            if (double.IsNegativeInfinity(value))
            {
                return scale.DomainMin;
            }

            return double.IsPositiveInfinity(value) ? scale.DomainMax : value;
        }
    }
}