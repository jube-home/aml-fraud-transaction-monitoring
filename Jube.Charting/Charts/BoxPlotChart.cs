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
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class BoxPlotChart(BoxPlotChartSpec spec) : IStyleModeChart
    {
        public BoxPlotChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new BoxPlotChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private const int MaxOutliersDrawn = 200;

        private string Draw(string id)
        {
            var s = Spec;
            var boxes = s.Boxes;
            ChartLimits.Categories(boxes.Count);
            ChartLimits.Points(boxes.Sum(b => 5L + (b.Outliers?.Count ?? 0)));
            var extent = new Extent();

            foreach (var b in boxes)
            {
                extent.Add(b.Min);
                extent.Add(b.Max);

                foreach (var o in (b.Outliers ?? []).Take(MaxOutliersDrawn))
                {
                    extent.Add(o);
                }
            }

            var colours = Guard.Colours([boxes.Count > 0 ? boxes[0].Color : null]);

            var summary = "Box plot of " + boxes.Count + " groups: " +
                          string.Join("; ", boxes.Take(6).Select(b =>
                              TextMetrics.Truncate(b.Label, 120, 12) + " median " +
                              NumberFormat.Value(b.Median))) + ".";

            var canvas = new ChartCanvas(s.Options, id, summary, [], 1, colours);
            canvas.LimitInteractive(boxes.Count);
            var layout = CategoryLayout.Vertical(canvas, extent, boxes.Count, s.ValueFormat);
            canvas.Begin();
            layout.DrawAxes(canvas, [.. boxes.Select(b => b.Label)], false);
            var y = layout.Value;
            canvas.OpenSeries(0);

            for (var i = 0; i < boxes.Count; i++)
            {
                var b = boxes[i];
                var x0 = layout.Band.Start(i);
                var w = layout.Band.Bandwidth;
                var cx = x0 + w / 2;
                var pts = new[] { b.Min, b.Q1, b.Median, b.Q3, b.Max };

                if (pts.Any(v => !double.IsFinite(v)))
                {
                    continue;
                }

                var rows = new List<TipRow>
                {
                    new(b.Label, true),
                    new("Maximum: " + NumberFormat.Value(b.Max)),
                    new("Upper quartile: " + NumberFormat.Value(b.Q3)),
                    new("Median: " + NumberFormat.Value(b.Median)),
                    new("Lower quartile: " + NumberFormat.Value(b.Q1)),
                    new("Minimum: " + NumberFormat.Value(b.Min))
                };

                if (b.Outliers is { Count: > 0 })
                {
                    rows.Add(new TipRow("Outliers: " + b.Outliers.Count));
                }

                if (b.Count is { } count)
                {
                    rows.Add(new TipRow("Observations: " + count));
                }

                var style = ColourCheck.Normalise(b.Color) is { } c ? "--sc:" + c : null;
                canvas.OpenMark(0, string.Join(", ", rows.Select(r => r.Text)), null, style);

                canvas.W.Empty("rect", ("class", "box m"), ("x", Num.F(x0)), ("y", Num.F(y.Map(b.Q3))),
                    ("width", Num.F(w)), ("height", Num.F(Math.Max(y.Map(b.Q1) - y.Map(b.Q3), 1))), ("rx", "2"));

                var whisk = new PathBuilder().Move(cx, y.Map(b.Max)).Line(cx, y.Map(b.Q3)).Move(cx, y.Map(b.Q1))
                    .Line(cx, y.Map(b.Min)).Move(cx - w / 4, y.Map(b.Max)).Line(cx + w / 4, y.Map(b.Max))
                    .Move(cx - w / 4, y.Map(b.Min)).Line(cx + w / 4, y.Map(b.Min));

                canvas.W.Empty("path", ("class", "wh"), ("d", whisk.ToString()));

                canvas.W.Empty("line", ("class", "med"), ("x1", Num.F(x0)), ("y1", Num.F(y.Map(b.Median))),
                    ("x2", Num.F(x0 + w)), ("y2", Num.F(y.Map(b.Median))));

                foreach (var o in (b.Outliers ?? []).Take(MaxOutliersDrawn).Where(double.IsFinite))
                {
                    canvas.W.Empty("circle", ("class", "out"), ("cx", Num.F(cx)), ("cy", Num.F(y.Map(o))), ("r", "3"));
                }

                canvas.Tooltip(cx, y.Map(b.Max), rows, false, 8);
                canvas.CloseMark();
            }

            canvas.W.Close();

            if (boxes.Count == 0)
            {
                canvas.Text(layout.Plot.CenterX, layout.Plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}