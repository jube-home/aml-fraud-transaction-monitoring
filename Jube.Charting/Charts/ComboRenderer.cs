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
    internal static class ComboRenderer
    {
        public static string Render(ComboChartSpec spec, string id)
        {
            var series = spec.Series;
            var n = spec.Categories.Count;
            ChartLimits.Series(series.Count);
            ChartLimits.Categories(n);
            ChartLimits.Points((long)n * series.Count);

            foreach (var s in series)
            {
                if (s.Values.Count != n)
                {
                    throw new ChartSpecificationException("Every combination series needs one value per category.");
                }
            }

            var names = series.Select(s => s.Name).ToArray();
            var colours = Guard.Colours(series.Select(s => s.Color));

            var legend = series.Select((s, i) => new LegendEntry(s.Name, i,
                s.Kind == ComboKind.Line ? LegendShape.Line : LegendShape.Square)).ToList();

            var primary = new Extent();
            var secondary = new Extent();

            foreach (var s in series)
            {
                foreach (var v in s.Values)
                {
                    if (s.SecondaryAxis)
                    {
                        secondary.Add(v);
                    }
                    else
                    {
                        primary.Add(v);
                    }
                }

                if (s.Kind != ComboKind.Line && s.SecondaryAxis)
                {
                    secondary.Add(0);
                }
                else if (s.Kind != ComboKind.Line)
                {
                    primary.Add(0);
                }
            }

            var hasSecondary = series.Any(s => s.SecondaryAxis);

            var summary = Guard.Summary("Combination chart", names, n + " categories. " +
                                                                    string.Join(" ", series.Select(s => s.Name +
                                                                        " is drawn as " +
                                                                        s.Kind.ToString().ToLowerInvariant() + "s" +
                                                                        (s.SecondaryAxis
                                                                            ? " on the secondary axis."
                                                                            : "."))));

            var canvas = new ChartCanvas(spec.Options, id, summary, legend, series.Count, colours);
            canvas.LimitInteractive((long)n * series.Count);

            var plot = canvas.Frame(30, 20);
            IScale ys;
            IScale? y2 = null;
            IReadOnlyList<Tick> ticks;
            IReadOnlyList<Tick> ticks2 = [];
            var titleReserve = string.IsNullOrWhiteSpace(spec.Options.SecondaryYAxisTitle) ? 0 : 18;

            for (var pass = 0; pass < 3; pass++)
            {
                var target = (int)Math.Clamp(plot.Height / 42, 3, 10);

                (ys, var v1) = AxisFactory.Numeric(primary.Any ? primary.Min : 0, primary.Any ? primary.Max : 1,
                    plot.Bottom,
                    plot.Top, AxisKind.Linear, target);

                ticks = AxisFactory.ToTicks(ys, v1, spec.ValueFormat);
                var reserve = 0.0;

                if (hasSecondary)
                {
                    (y2, var v2) = AxisFactory.Numeric(secondary.Any ? secondary.Min : 0,
                        secondary.Any ? secondary.Max : 1,
                        plot.Bottom, plot.Top, AxisKind.Linear, target);

                    ticks2 = AxisFactory.ToTicks(y2, v2, spec.SecondaryFormat);
                    reserve = TextMetrics.MaxWidth(ticks2.Select(t => t.Label), 11) + 10 + titleReserve;
                }

                plot = canvas.Frame(TextMetrics.MaxWidth(ticks.Select(t => t.Label), 11), 20, reserve);
            }

            var t3 = (int)Math.Clamp(plot.Height / 42, 3, 10);

            (ys, var w1) = AxisFactory.Numeric(primary.Any ? primary.Min : 0, primary.Any ? primary.Max : 1,
                plot.Bottom,
                plot.Top, AxisKind.Linear, t3);

            ticks = AxisFactory.ToTicks(ys, w1, spec.ValueFormat);

            if (hasSecondary)
            {
                (y2, var w2) = AxisFactory.Numeric(secondary.Any ? secondary.Min : 0, secondary.Any ? secondary.Max : 1,
                    plot.Bottom, plot.Top, AxisKind.Linear, t3);

                ticks2 = AxisFactory.ToTicks(y2, w2, spec.SecondaryFormat);
            }

            canvas.Begin();
            canvas.YAxis(ticks);

            foreach (var t in ticks2.Where(t => double.IsFinite(t.Position)))
            {
                canvas.W.Element("text", t.Label, ("class", "tk mid"), ("x", Num.F(plot.Right + 8)),
                    ("y", Num.F(t.Position)));
            }

            if (hasSecondary && titleReserve > 0)
            {
                canvas.W.Element("text", TextMetrics.Truncate(spec.Options.SecondaryYAxisTitle, plot.Height, 12),
                    ("class", "at ctr"), ("x", "0"), ("y", "0"),
                    ("transform",
                        "translate(" + Num.F(canvas.Width - ChartCanvas.Pad + 4) + " " + Num.F(plot.CenterY) +
                        ") rotate(90)"));
            }

            var band = new BandScale(Math.Max(n, 1), plot.Left, plot.Right, n > 1 ? 0.25 : 0.5);
            canvas.Baseline(ys.Map(Math.Clamp(0, ys.DomainMin, ys.DomainMax)));
            canvas.XAxis(CategoryLayout.CategoryTicks(spec.Categories, band));

            var columns = series.Select((s, i) => (s, i)).Where(t => t.s.Kind == ComboKind.Column).ToList();
            var sub = (band.Bandwidth - (columns.Count - 1) * 2.0) / Math.Max(columns.Count, 1);

            for (var c = 0; c < columns.Count; c++)
            {
                var (s, si) = columns[c];
                var scale = s.SecondaryAxis && y2 is not null ? y2 : ys;
                canvas.OpenSeries(si);

                for (var i = 0; i < n; i++)
                {
                    var v = s.Values[i];

                    if (!double.IsFinite(v))
                    {
                        continue;
                    }

                    var x = band.Start(i) + c * (sub + 2);
                    var a = scale.Map(0);
                    var b = scale.Map(v);
                    var top = Math.Min(a, b);
                    var height = Math.Abs(a - b);

                    var rows = new List<TipRow>
                    {
                        new(spec.Categories[i], true),
                        new(s.Name + ": " + NumberFormat.Value(v), false, si)
                    };

                    canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("rect", ("class", "bar m"), ("x", Num.F(x)), ("y", Num.F(top)),
                        ("width", Num.F(Math.Max(sub, 1))), ("height", Num.F(height)), ("rx", "2"));

                    canvas.Tooltip(x + sub / 2, b, rows, false, 8);
                    canvas.CloseMark();
                }

                canvas.W.Close();
            }

            foreach (var kind in new[] { ComboKind.Area, ComboKind.Line })
            {
                for (var si = 0; si < series.Count; si++)
                {
                    var s = series[si];

                    if (s.Kind != kind)
                    {
                        continue;
                    }

                    var scale = s.SecondaryAxis && y2 is not null ? y2 : ys;
                    var xp = Enumerable.Range(0, n).Select(band.Center).ToArray();
                    var yp = s.Values.Select(v => double.IsFinite(v) ? scale.Map(v) : double.NaN).ToArray();
                    canvas.OpenSeries(si);

                    DrawLine(canvas, s, xp, yp, scale.Map(Math.Clamp(0, scale.DomainMin, scale.DomainMax)),
                        spec.Smooth);

                    for (var i = 0; i < n; i++)
                    {
                        if (!double.IsFinite(yp[i]))
                        {
                            continue;
                        }

                        var rows = new List<TipRow>
                        {
                            new(spec.Categories[i], true),
                            new(s.Name + ": " + NumberFormat.Value(s.Values[i]), false, si)
                        };

                        canvas.OpenMark(si, string.Join(", ", rows.Select(r => r.Text)));

                        canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(xp[i])), ("cy", Num.F(yp[i])),
                            ("r", "9"));

                        canvas.W.Empty("circle", ("class", n <= 40 ? "pt" : "pt h"), ("cx", Num.F(xp[i])),
                            ("cy", Num.F(yp[i])),
                            ("r", "3.5"));

                        canvas.Tooltip(xp[i], yp[i] - 4, rows);
                        canvas.CloseMark();
                    }

                    canvas.W.Close();
                }
            }

            canvas.AxisTitles();

            return canvas.End();
        }

        private static void DrawLine(ChartCanvas canvas, ComboSeries s, double[] xp, double[] yp, double baseline,
            bool smooth)
        {
            if (s.Kind == ComboKind.Area)
            {
                var i = 0;

                while (i < xp.Length)
                {
                    if (!double.IsFinite(yp[i]))
                    {
                        i++;
                        continue;
                    }

                    var start = i;

                    while (i < xp.Length && double.IsFinite(yp[i]))
                    {
                        i++;
                    }

                    var path = new PathBuilder().Move(xp[start], baseline).Line(xp[start], yp[start]);

                    for (var k = start + 1; k < i; k++)
                    {
                        path.Line(xp[k], yp[k]);
                    }

                    path.Line(xp[i - 1], baseline).Close();
                    canvas.W.Empty("path", ("class", "ar"), ("d", path.ToString()));
                }
            }

            var line = new PathBuilder();

            if (smooth)
            {
                var i = 0;

                while (i < xp.Length)
                {
                    if (!double.IsFinite(yp[i]))
                    {
                        i++;
                        continue;
                    }

                    var start = i;

                    while (i < xp.Length && double.IsFinite(yp[i]))
                    {
                        i++;
                    }

                    Spline.Append(line, xp[start..i], yp[start..i], false);
                }
            }
            else
            {
                for (var k = 0; k < xp.Length; k++)
                {
                    line.Line(xp[k], yp[k]);
                }
            }

            if (!line.IsEmpty)
            {
                canvas.W.Open("path", ("class", "ln"), ("d", line.ToString()));
                canvas.W.Element("title", s.Name);
                canvas.W.Close();
            }
        }
    }
}