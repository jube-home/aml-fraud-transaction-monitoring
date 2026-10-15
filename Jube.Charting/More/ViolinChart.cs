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
    public sealed class ViolinChart(ViolinChartSpec spec) : IStyleModeChart
    {
        public ViolinChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ViolinChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var groups = s.Groups;
            ChartLimits.Categories(groups.Count);
            var prepared = DistributionPrep.Prepare(groups, s.Bandwidth, s.BandwidthAdjust, 2);
            var colours = Guard.Colours(groups.Select(g => g.Color));
            var format = s.ValueFormat ?? NumberFormat.Value;
            var names = groups.Select(g => g.Name).ToArray();

            var summary = Guard.Summary("Violin chart", names, string.Join(" ", prepared.Select((p, i) =>
                TextMetrics.Truncate(names[i], 80, 12) + ": " + p.Summary.Count + " values, median " +
                (p.Summary.Count > 0 ? format(p.Summary.Median) : "n/a") + ".").Take(6)));

            var options = s.Options with { Legend = LegendPosition.None };
            var canvas = new ChartCanvas(options, id, summary, [], groups.Count, colours);
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

            canvas.Family = StyleFamily.Violin;
            var n = Math.Max(groups.Count, 1);
            var lo = extent.Any ? extent.Min : 0;
            var hi = extent.Any ? extent.Max : 1;

            Rect plot;
            IScale value;
            BandScale band;
            IReadOnlyList<Tick> valueTicks;

            if (!s.Horizontal)
            {
                var layout = CategoryLayout.Vertical(canvas, extent, groups.Count, s.ValueFormat);
                plot = layout.Plot;
                value = layout.Value;
                band = layout.Band;
                valueTicks = layout.ValueTicks;
            }
            else
            {
                var labelWidth = Math.Min(TextMetrics.MaxWidth(names, 11), canvas.Width * 0.3);
                plot = canvas.Frame(labelWidth, 20);
                band = new BandScale(n, plot.Top, plot.Bottom, groups.Count > 1 ? 0.3 : 0.5);

                var (scale, values) = AxisFactory.Numeric(lo, hi, plot.Left, plot.Right, AxisKind.Linear,
                    (int)Math.Clamp(plot.Width / 80, 3, 12));

                value = scale;
                valueTicks = AxisFactory.ToTicks(scale, values, s.ValueFormat);
            }

            var globalMax = prepared.Select(p => p.Estimate.Density.Count > 0 ? p.Estimate.Density.Max() : 0)
                .DefaultIfEmpty(0).Max();

            canvas.LimitInteractive(groups.Count);
            canvas.Begin();

            if (!s.Horizontal)
            {
                canvas.YAxis(valueTicks);
                canvas.Baseline(plot.Bottom);
                canvas.XAxis(CategoryLayout.CategoryTicks(names, band));
            }
            else
            {
                canvas.XAxis(valueTicks, true);
                canvas.VerticalBaseline(plot.Left);

                for (var i = 0; i < groups.Count; i++)
                {
                    canvas.Text(plot.Left - 8, band.Center(i),
                        TextMetrics.Truncate(names[i], plot.Left - ChartCanvas.Pad - 6, 11),
                        "tk end mid");
                }
            }

            (double X, double Y) P(int i, double v, double offset)
            {
                var c = band.Center(i);
                var pv = value.Map(v);

                return s.Horizontal ? (pv, c + offset) : (c + offset, pv);
            }

            var half = band.Bandwidth / 2;

            for (var i = 0; i < groups.Count; i++)
            {
                var (sm, e) = prepared[i];
                var rows = DistributionPrep.Rows(names[i], i, sm, e, format);
                canvas.OpenMark(i, string.Join(", ", rows.Select(r => r.Text)));

                if (sm.Count == 0)
                {
                    canvas.CloseMark();
                    continue;
                }

                if (e.Density.Count > 0)
                {
                    var dmax = s.Scale == DensityScale.Area ? globalMax : e.Density.Max();
                    var path = new PathBuilder();

                    for (var k = 0; k < e.Grid.Count; k++)
                    {
                        var (x, y) = P(i, e.Grid[k], half * e.Density[k] / dmax);
                        path.Line(x, y);
                    }

                    for (var k = e.Grid.Count - 1; k >= 0; k--)
                    {
                        var (x, y) = P(i, e.Grid[k], -half * e.Density[k] / dmax);
                        path.Line(x, y);
                    }

                    canvas.W.Empty("path", ("class", "vi"), ("d", path.Close().ToString()));
                }

                if (s.ShowPoints)
                {
                    var sample = MoreKit.Thin(DensityMath.Finite(groups[i].Values), 200);

                    for (var k = 0; k < sample.Count; k++)
                    {
                        var jitter = (k * 0.6180339887 % 1 - 0.5) * half * 1.2;
                        var (x, y) = P(i, sample[k], jitter);
                        canvas.W.Empty("circle", ("class", "vpt"), ("cx", Num.F(x)), ("cy", Num.F(y)), ("r", "2"));
                    }
                }

                if (s.ShowBox)
                {
                    var (bx0, by0) = P(i, sm.Q1, -3);
                    var (bx1, by1) = P(i, sm.Q3, 3);
                    var (wx0, wy0) = P(i, sm.LowerWhisker, 0);
                    var (wx1, wy1) = P(i, sm.UpperWhisker, 0);

                    canvas.W.Empty("path", ("class", "vwk"),
                        ("d", new PathBuilder().Move(wx0, wy0).Line(wx1, wy1).ToString()));

                    canvas.W.Empty("rect", ("class", "vbx"), ("x", Num.F(Math.Min(bx0, bx1))),
                        ("y", Num.F(Math.Min(by0, by1))),
                        ("width", Num.F(Math.Max(Math.Abs(bx1 - bx0), s.Horizontal ? 1 : 0))),
                        ("height", Num.F(Math.Max(Math.Abs(by1 - by0), s.Horizontal ? 0 : 1))), ("rx", "2"));

                    var (mx, my) = P(i, sm.Median, 0);
                    canvas.W.Empty("circle", ("class", "vmd"), ("cx", Num.F(mx)), ("cy", Num.F(my)), ("r", "3.5"));
                }

                var (ax, ay) = P(i, sm.Median, 0);
                canvas.Tooltip(ax, ay, rows, true, half + 6);
                canvas.CloseMark();
            }

            if (groups.Count == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}