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
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class WaterfallChart(WaterfallChartSpec spec) : IStyleModeChart
    {
        public WaterfallChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new WaterfallChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var steps = s.Steps;
            ChartLimits.Categories(steps.Count);
            var format = s.ValueFormat ?? NumberFormat.Value;
            var starts = new double[steps.Count];
            var ends = new double[steps.Count];
            var running = 0.0;
            var extent = new Extent();
            extent.Add(0);

            for (var i = 0; i < steps.Count; i++)
            {
                var v = steps[i].Value is { } sv && double.IsFinite(sv) ? sv : 0;

                if (steps[i].IsTotal)
                {
                    starts[i] = 0;
                    ends[i] = steps[i].Value is { } tv && double.IsFinite(tv) ? tv : running;
                    running = ends[i];
                }
                else
                {
                    starts[i] = running;
                    running += v;
                    ends[i] = running;
                }

                extent.Add(starts[i]);
                extent.Add(ends[i]);
            }

            if (s.ShowValues && extent.Any)
            {
                extent.Add(extent.Max + (extent.Max - extent.Min) * 0.08);
            }

            var summary = "Waterfall chart with " + steps.Count + " steps ending at " + format(running) + ".";

            var canvas = new ChartCanvas(s.Options, id, summary,
                [new LegendEntry("Increase", 0), new LegendEntry("Decrease", 1), new LegendEntry("Total", 2)], 3,
                [null, null, "#7d7c76"]);
            canvas.LimitInteractive(steps.Count);
            var layout = CategoryLayout.Vertical(canvas, extent, steps.Count, s.ValueFormat);
            canvas.Begin();
            layout.DrawAxes(canvas, [.. steps.Select(t => t.Label)]);
            var y = layout.Value;

            for (var group = 0; group < 3; group++)
            {
                canvas.OpenSeries(group);

                for (var i = 0; i < steps.Count; i++)
                {
                    var kind = steps[i].IsTotal ? 2 : ends[i] >= starts[i] ? 0 : 1;

                    if (kind != group)
                    {
                        continue;
                    }

                    var x0 = layout.Band.Start(i);
                    var w = layout.Band.Bandwidth;
                    var a = y.Map(starts[i]);
                    var b = y.Map(ends[i]);
                    var change = ends[i] - starts[i];

                    var rows = new List<TipRow>
                    {
                        new(steps[i].Label, true, kind),
                        new((steps[i].IsTotal ? "Total: " : "Change: ") +
                            (steps[i].IsTotal ? format(ends[i]) : (change >= 0 ? "+" : "") + format(change))),
                        new("Running total: " + format(ends[i]))
                    };

                    canvas.OpenMark(kind, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("rect", ("class", "bar m"), ("x", Num.F(x0)), ("y", Num.F(Math.Min(a, b))),
                        ("width", Num.F(w)), ("height", Num.F(Math.Max(Math.Abs(a - b), 1))), ("rx", "2"));

                    canvas.Tooltip(x0 + w / 2, Math.Min(a, b), rows, false, 8);
                    canvas.CloseMark();

                    if (s.ShowValues && canvas.Interactive)
                    {
                        var text = steps[i].IsTotal ? format(ends[i]) : (change >= 0 ? "+" : "") + format(change);
                        canvas.Text(x0 + w / 2, Math.Min(a, b) - 5, text, "lbl ctr");
                    }
                }

                canvas.W.Close();
            }

            for (var i = 0; i + 1 < steps.Count; i++)
            {
                var yy = y.Map(ends[i]);

                canvas.W.Empty("line", ("class", "conn"), ("x1", Num.F(layout.Band.Start(i) + layout.Band.Bandwidth)),
                    ("y1", Num.F(yy)), ("x2", Num.F(layout.Band.Start(i + 1))), ("y2", Num.F(yy)));
            }

            if (steps.Count == 0)
            {
                canvas.Text(layout.Plot.CenterX, layout.Plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}