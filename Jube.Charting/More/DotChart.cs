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
    public sealed class DotChart(DotChartSpec spec) : IStyleModeChart
    {
        public DotChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new DotChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            ChartLimits.Series(s.Series.Count);
            ChartLimits.Categories(s.Categories.Count);
            ChartLimits.Points(s.Series.Sum(x => (long)x.Values.Count));

            foreach (var series in s.Series)
            {
                if (series.Values.Count != s.Categories.Count)
                {
                    throw new ChartSpecificationException("Every series needs one value per category.");
                }
            }

            if (s.SortSeries < 0 || s.SortSeries >= Math.Max(s.Series.Count, 1))
            {
                throw new ChartSpecificationException("The sort series index is out of range.");
            }

            if (s is { Style: DotStyle.Dumbbell, Series.Count: < 2 })
            {
                throw new ChartSpecificationException("A dumbbell chart needs at least two series.");
            }

            var format = s.ValueFormat ?? NumberFormat.Value;
            var order = Enumerable.Range(0, s.Categories.Count).ToArray();

            if (s.Sort != DotSort.None && s.Series.Count > 0)
            {
                var key = s.Series[s.SortSeries].Values;

                var sorted = order.OrderBy(i => double.IsFinite(key[i]) ? 0 : 1)
                    .ThenBy(i => s.Sort == DotSort.Ascending ? key[i] : -key[i]).ThenBy(i => i);

                order = [.. sorted];
            }

            var cats = order.Select(i => s.Categories[i]).ToArray();
            var colours = Guard.Colours(s.Series.Select(x => x.Color));
            var names = s.Series.Select(x => x.Name).ToArray();
            var legend = s.Series.Count > 1 ? s.Series.Select((x, i) => new LegendEntry(x.Name, i)).ToList() : [];
            var extent = new Extent();

            foreach (var series in s.Series)
            {
                foreach (var v in series.Values)
                {
                    extent.Add(v);
                }
            }

            if (s.Style == DotStyle.Lollipop)
            {
                extent.Add(s.Baseline);
            }

            var kind = s.Style switch
            {
                DotStyle.Lollipop => "Lollipop chart", DotStyle.Dumbbell => "Dumbbell chart", _ => "Dot plot"
            };

            var summary = Guard.Summary(kind, names, "Across " + cats.Length + " categories" +
                                                     (extent.Any
                                                         ? ", values from " + format(extent.Min) + " to " +
                                                           format(extent.Max) + "."
                                                         : "."));

            var canvas = new ChartCanvas(MoreKit.AutoLegend(s.Options), id, summary, legend, s.Series.Count, colours)
            {
                Family = StyleFamily.Dot
            };

            canvas.LimitInteractive(cats.Length);

            var n = Math.Max(cats.Length, 1);
            Rect plot;
            IScale value;
            BandScale band;
            IReadOnlyList<Tick> ticks;
            var lo = extent.Any ? extent.Min : 0;
            var hi = extent.Any ? extent.Max : 1;

            if (s.Horizontal)
            {
                var labelWidth = Math.Min(TextMetrics.MaxWidth(cats, 11), canvas.Width * 0.3);
                plot = canvas.Frame(labelWidth, 20);
                band = new BandScale(n, plot.Top, plot.Bottom, cats.Length > 1 ? 0.25 : 0.5);

                (value, var values) = AxisFactory.Numeric(lo, hi, plot.Left, plot.Right, AxisKind.Linear,
                    (int)Math.Clamp(plot.Width / 80, 3, 12));

                ticks = AxisFactory.ToTicks(value, values, s.ValueFormat);
            }
            else
            {
                var layout = CategoryLayout.Vertical(canvas, extent, cats.Length, s.ValueFormat, 0.25);
                plot = layout.Plot;
                value = layout.Value;
                band = layout.Band;
                ticks = layout.ValueTicks;
            }

            canvas.Begin();

            if (s.Horizontal)
            {
                canvas.XAxis(ticks, true);
                canvas.VerticalBaseline(plot.Left);

                for (var i = 0; i < cats.Length; i++)
                {
                    canvas.Text(plot.Left - 8, band.Center(i),
                        TextMetrics.Truncate(cats[i], plot.Left - ChartCanvas.Pad - 6, 11), "tk end mid");
                }
            }
            else
            {
                canvas.YAxis(ticks);
                canvas.Baseline(plot.Bottom);
                canvas.XAxis(CategoryLayout.CategoryTicks(cats, band));
            }

            (double X, double Y) P(int row, double v)
            {
                var c = band.Center(row);
                var pv = value.Map(v);

                return s.Horizontal ? (pv, c) : (c, pv);
            }

            for (var row = 0; row < cats.Length; row++)
            {
                var src = order[row];
                var rows = new List<TipRow> { new(cats[row], true) };

                for (var k = 0; k < s.Series.Count; k++)
                {
                    rows.Add(new TipRow(s.Series[k].Name + ": " + format(s.Series[k].Values[src]), false, k));
                }

                if (s is { Style: DotStyle.Dumbbell, Series.Count: 2 } && double.IsFinite(s.Series[0].Values[src]) &&
                    double.IsFinite(s.Series[1].Values[src]))
                {
                    var d = s.Series[1].Values[src] - s.Series[0].Values[src];

                    rows.Add(new TipRow("Change: " + (d >= 0 ? "+" : string.Empty) + format(d) +
                                        (s.Series[0].Values[src] != 0
                                            ? " (" + (d >= 0 ? "+" : string.Empty) +
                                              NumberFormat.Percent(d / Math.Abs(s.Series[0].Values[src])) + ")"
                                            : string.Empty)));
                }

                canvas.OpenMark(-1, string.Join(", ", rows.Select(r => r.Text)));
                var bandStart = band.Start(row) - band.Step * band.Padding / 2;

                if (s.Horizontal)
                {
                    canvas.W.Empty("rect", ("class", "rowhl"), ("x", Num.F(plot.Left)), ("y", Num.F(bandStart)),
                        ("width", Num.F(plot.Width)), ("height", Num.F(band.Step)));
                }
                else
                {
                    canvas.W.Empty("rect", ("class", "rowhl"), ("x", Num.F(bandStart)), ("y", Num.F(plot.Top)),
                        ("width", Num.F(band.Step)), ("height", Num.F(plot.Height)));
                }

                var present = Enumerable.Range(0, s.Series.Count).Where(k => double.IsFinite(s.Series[k].Values[src]))
                    .ToArray();

                if (present.Length > 0)
                {
                    if (s.Style == DotStyle.Dot)
                    {
                        var a = P(row, lo);
                        var b = P(row, hi);

                        canvas.W.Empty("path", ("class", "rg"),
                            ("d", new PathBuilder().Move(a.X, a.Y).Line(b.X, b.Y).ToString()));
                    }
                    else if (s.Style == DotStyle.Dumbbell)
                    {
                        var min = present.Min(k => s.Series[k].Values[src]);
                        var max = present.Max(k => s.Series[k].Values[src]);
                        var a = P(row, min);
                        var b = P(row, max);

                        canvas.W.Empty("path", ("class", "dbc"),
                            ("d", new PathBuilder().Move(a.X, a.Y).Line(b.X, b.Y).ToString()));
                    }
                    else
                    {
                        foreach (var k in present)
                        {
                            var a = P(row, Math.Clamp(s.Baseline, value.DomainMin, value.DomainMax));
                            var b = P(row, s.Series[k].Values[src]);

                            canvas.W.Empty("path", ("class", "stem s" + k % ChartLimits.MaxSeries),
                                ("d", new PathBuilder().Move(a.X, a.Y).Line(b.X, b.Y).ToString()));
                        }
                    }

                    foreach (var k in present)
                    {
                        var p = P(row, s.Series[k].Values[src]);

                        canvas.W.Empty("circle", ("class", "pt sr s" + k % ChartLimits.MaxSeries), ("cx", Num.F(p.X)),
                            ("cy", Num.F(p.Y)), ("r", "5.5"));
                    }

                    if (s is { ShowValues: true, Horizontal: true })
                    {
                        var max = present.Max(k => s.Series[k].Values[src]);
                        var min = present.Min(k => s.Series[k].Values[src]);
                        var pmax = P(row, max);

                        canvas.W.Element("text", format(max), ("class", "dl"), ("x", Num.F(pmax.X + 9)),
                            ("y", Num.F(pmax.Y + 4)));

                        if (present.Length > 1 && Math.Abs(min - max) > 0.0001)
                        {
                            var pmin = P(row, min);

                            canvas.W.Element("text", format(min), ("class", "dl end"), ("x", Num.F(pmin.X - 9)),
                                ("y", Num.F(pmin.Y + 4)));
                        }
                    }
                    else if (s.ShowValues)
                    {
                        var max = present.Max(k => s.Series[k].Values[src]);
                        var pmax = P(row, max);

                        canvas.W.Element("text", format(max), ("class", "dl ctr"), ("x", Num.F(pmax.X)),
                            ("y", Num.F(pmax.Y - 10)));
                    }

                    var anchor = P(row, present.Max(k => s.Series[k].Values[src]));
                    canvas.Tooltip(anchor.X, anchor.Y, rows, s.Horizontal, 12);
                }

                canvas.CloseMark();
            }

            if (cats.Length == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}