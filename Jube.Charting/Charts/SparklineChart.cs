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
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class SparklineChart(SparklineChartSpec spec) : IStyleModeChart
    {
        public SparklineChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new SparklineChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var v = s.Values;
            ChartLimits.Points(v.Count);
            var name = s.Name ?? "Values";
            var extent = new Extent();

            foreach (var x in v)
            {
                extent.Add(x);
            }

            if (s.Kind is SparklineKind.Column or SparklineKind.Area or SparklineKind.WinLoss)
            {
                extent.Add(0);
            }

            var summary = "Sparkline of " + v.Count + " values" + (extent.Any
                ? " from " + NumberFormat.Value(extent.Min) + " to " + NumberFormat.Value(extent.Max) +
                  ", last value " +
                  NumberFormat.Value(v.Count > 0 ? v[^1] : double.NaN) + "."
                : ".");

            var options = s.Options with { Legend = LegendPosition.None, Title = s.Options.Title };
            var canvas = new ChartCanvas(options, id, summary, [], 1, []);
            canvas.LimitInteractive(v.Count);
            var plot = new Rect(4, 4, canvas.Width - 4, canvas.Height - 4);
            canvas.SetPlot(plot);
            canvas.Begin();

            if (!extent.Any)
            {
                return canvas.End();
            }

            var lo = extent.Min;
            var hi = extent.Max;
            var ys = new LinearScale(lo, hi, plot.Bottom, plot.Top);
            var n = v.Count;
            var step = n > 1 ? plot.Width / (n - 1) : 0;

            double X(int i)
            {
                return n > 1 ? plot.Left + i * step : plot.CenterX;
            }

            canvas.OpenSeries(0);

            if (s.Kind is SparklineKind.Column or SparklineKind.WinLoss)
            {
                var band = new BandScale(n, plot.Left, plot.Right);
                var zero = ys.Map(0);

                for (var i = 0; i < n; i++)
                {
                    if (!double.IsFinite(v[i]))
                    {
                        continue;
                    }

                    var label = (s.Labels is { } l && i < l.Count ? l[i] + ": " : string.Empty) +
                                NumberFormat.Value(v[i]);

                    canvas.OpenMark(v[i] < 0 && s.Kind == SparklineKind.WinLoss ? 1 : 0, label);

                    var y = s.Kind == SparklineKind.WinLoss
                        ? plot.Top + plot.Height / 2 - Math.Sign(v[i]) * plot.Height * 0.4
                        : ys.Map(v[i]);

                    var y0 = s.Kind == SparklineKind.WinLoss ? plot.Top + plot.Height / 2 : zero;

                    canvas.W.Empty("rect", ("class", "bar m"), ("x", Num.F(band.Start(i))),
                        ("y", Num.F(Math.Min(y, y0))),
                        ("width", Num.F(Math.Max(band.Bandwidth, 1))),
                        ("height", Num.F(Math.Max(Math.Abs(y - y0), 1))));

                    canvas.CloseMark();
                }
            }
            else
            {
                var path = new PathBuilder();

                for (var i = 0; i < n; i++)
                {
                    path.Line(X(i), double.IsFinite(v[i]) ? ys.Map(v[i]) : double.NaN);
                }

                if (s.Kind == SparklineKind.Area)
                {
                    var i = 0;
                    var zero = ys.Map(Math.Clamp(0, lo, hi));

                    while (i < n)
                    {
                        if (!double.IsFinite(v[i]))
                        {
                            i++;
                            continue;
                        }

                        var start = i;
                        var area = new PathBuilder().Move(X(start), zero);

                        while (i < n && double.IsFinite(v[i]))
                        {
                            area.Line(X(i), ys.Map(v[i]));
                            i++;
                        }

                        canvas.W.Empty("path", ("class", "ar"), ("d", area.Line(X(i - 1), zero).Close().ToString()));
                    }
                }

                if (!path.IsEmpty)
                {
                    canvas.W.Open("path", ("class", "ln"), ("d", path.ToString()));
                    canvas.W.Element("title", name);
                    canvas.W.Close();
                }

                var marks = new HashSet<int>();

                if (s.MarkLast)
                {
                    for (var i = n - 1; i >= 0; i--)
                    {
                        if (double.IsFinite(v[i]))
                        {
                            marks.Add(i);
                            break;
                        }
                    }
                }

                if (s.MarkExtremes)
                {
                    for (var i = 0; i < n; i++)
                    {
                        if (double.IsFinite(v[i]) && (Math.Abs(v[i] - lo) < 0.0001 || Math.Abs(v[i] - hi) < 0.0001))
                        {
                            marks.Add(i);
                        }
                    }
                }

                for (var i = 0; i < n; i++)
                {
                    if (!double.IsFinite(v[i]))
                    {
                        continue;
                    }

                    var label = (s.Labels is { } l && i < l.Count ? l[i] + ": " : string.Empty) +
                                NumberFormat.Value(v[i]);
                    canvas.OpenMark(0, label);

                    canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(X(i))), ("cy", Num.F(ys.Map(v[i]))),
                        ("r", Num.F(Math.Max(step / 2, 4))));

                    canvas.W.Empty("circle", ("class", marks.Contains(i) ? "pt" : "pt h"), ("cx", Num.F(X(i))),
                        ("cy", Num.F(ys.Map(v[i]))), ("r", "3"));

                    canvas.CloseMark();
                }
            }

            canvas.W.Close();

            return canvas.End();
        }
    }
}