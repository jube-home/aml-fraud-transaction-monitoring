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
    public sealed class RidgelineChart(RidgelineChartSpec spec) : IStyleModeChart
    {
        public RidgelineChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new RidgelineChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var groups = s.Groups;

            if (!(s.Overlap is >= 0.5 and <= 6))
            {
                throw new ChartSpecificationException("Ridgeline overlap must be between 0.5 and 6.");
            }

            ChartLimits.Categories(groups.Count);
            var prepared = DistributionPrep.Prepare(groups, s.Bandwidth, s.BandwidthAdjust, 3);
            var colours = Guard.Colours(groups.Select(g => g.Color));
            var format = s.ValueFormat ?? NumberFormat.Value;
            var names = groups.Select(g => g.Name).ToArray();

            var summary = Guard.Summary("Ridgeline chart", names, string.Join(" ", prepared.Select((p, i) =>
                TextMetrics.Truncate(names[i], 80, 12) + ": " + p.Summary.Count + " values.").Take(6)));

            var canvas = new ChartCanvas(s.Options with { Legend = LegendPosition.None }, id, summary, [], groups.Count,
                colours);

            var extent = new Extent();

            foreach (var (sm, e) in prepared)
            {
                extent.Add(sm.Min);
                extent.Add(sm.Max);

                foreach (var g in e.Grid)
                {
                    extent.Add(g);
                }
            }

            canvas.Family = StyleFamily.Ridgeline;
            var labelWidth = Math.Min(TextMetrics.MaxWidth(names, 11), canvas.Width * 0.3);
            var plot = canvas.Frame(labelWidth, 20);
            var lo = extent.Any ? extent.Min : 0;
            var hi = extent.Any ? extent.Max : 1;

            var (xs, values) = AxisFactory.Numeric(lo, hi, plot.Left, plot.Right, AxisKind.Linear,
                (int)Math.Clamp(plot.Width / 80, 3, 12));

            var ticks = AxisFactory.ToTicks(xs, values, s.ValueFormat);
            var n = groups.Count;
            var step = n <= 1 ? plot.Height / (1 + s.Overlap - 1) : plot.Height / (n - 1 + s.Overlap);
            var peak = step * s.Overlap;

            if (n == 1)
            {
                peak = plot.Height * 0.9;
            }

            var globalMax = prepared.Select(p => p.Estimate.Density.Count > 0 ? p.Estimate.Density.Max() : 0)
                .DefaultIfEmpty(0).Max();

            canvas.LimitInteractive(n);
            canvas.Begin();
            canvas.XAxis(ticks, true);
            canvas.Baseline(plot.Bottom);

            for (var i = 0; i < n; i++)
            {
                var baseline = plot.Top + (n == 1 ? plot.Height : peak + i * step);
                baseline = Math.Min(baseline, plot.Bottom);

                canvas.W.Empty("line", ("class", "rb"), ("x1", Num.F(plot.Left)), ("y1", Num.F(baseline)),
                    ("x2", Num.F(plot.Right)), ("y2", Num.F(baseline)));

                canvas.Text(plot.Left - 8, baseline - 3,
                    TextMetrics.Truncate(names[i], plot.Left - ChartCanvas.Pad - 6, 11), "tk end");

                var (sm, e) = prepared[i];
                var rows = DistributionPrep.Rows(names[i], i, sm, e, format);
                canvas.OpenMark(i, string.Join(", ", rows.Select(r => r.Text)));

                if (e.Density.Count > 0)
                {
                    var dmax = s.Scale == DensityScale.Area ? globalMax : e.Density.Max();
                    var path = new PathBuilder().Move(xs.Map(e.Grid[0]), baseline);
                    var top = baseline;
                    var topX = xs.Map(e.Grid[0]);

                    for (var k = 0; k < e.Grid.Count; k++)
                    {
                        var y = baseline - peak * e.Density[k] / dmax;
                        path.Line(xs.Map(e.Grid[k]), y);

                        if (y < top)
                        {
                            top = y;
                            topX = xs.Map(e.Grid[k]);
                        }
                    }

                    path.Line(xs.Map(e.Grid[^1]), baseline);
                    canvas.W.Empty("path", ("class", "ri"), ("d", path.Close().ToString()));
                    canvas.Tooltip(topX, top, rows, true, 8);
                }

                canvas.CloseMark();
            }

            if (n == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}