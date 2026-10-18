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
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class BulletChart(BulletChartSpec spec) : IStyleModeChart
    {
        public BulletChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new BulletChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var items = s.Items;
            ChartLimits.Categories(items.Count);
            var format = s.ValueFormat ?? NumberFormat.Value;
            var extent = new Extent();
            extent.Add(0);

            foreach (var item in items)
            {
                extent.Add(item.Value);
                extent.Add(item.Target ?? double.NaN);

                foreach (var t in item.Thresholds ?? [])
                {
                    extent.Add(t);
                }
            }

            var summary = (s.Thick ? "Linear gauge" : "Bullet chart") + " with " + items.Count + " measures: " +
                          string.Join("; ", items.Take(6).Select(i =>
                              TextMetrics.Truncate(i.Label, 100, 12) + " " + format(i.Value) +
                              (i.Target is { } tg ? " against target " + format(tg) : string.Empty))) + ".";

            var canvas = new ChartCanvas(s.Options with { Legend = LegendPosition.None }, id, summary, [], 1, [], true);
            canvas.LimitInteractive(items.Count);
            var labelWidth = Math.Min(TextMetrics.MaxWidth(items.Select(i => i.Label), 12), canvas.Width * 0.3);
            var plot = canvas.Frame(labelWidth, 20);

            var (scale, values) = AxisFactory.Numeric(extent.Any ? extent.Min : 0, extent.Any ? extent.Max : 1,
                plot.Left,
                plot.Right, AxisKind.Linear, (int)Math.Clamp(plot.Width / 80, 3, 10));

            var ticks = AxisFactory.ToTicks(scale, values, s.ValueFormat);
            canvas.Begin();

            foreach (var t in ticks)
            {
                canvas.W.Empty("line", ("class", "gl"), ("x1", Num.F(t.Position)), ("y1", Num.F(plot.Top)),
                    ("x2", Num.F(t.Position)), ("y2", Num.F(plot.Bottom)));
            }

            canvas.XAxis(ticks);
            var band = new BandScale(Math.Max(items.Count, 1), plot.Top, plot.Bottom, 0.35);
            var height = Math.Min(band.Bandwidth, s.Thick ? 40 : 30);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var cy = band.Center(i);
                canvas.Text(plot.Left - 8, cy, TextMetrics.Truncate(item.Label, labelWidth, 12), "tk end mid");
                var top = cy - height / 2;
                var edges = new List<double> { scale.DomainMin };

                edges.AddRange((item.Thresholds ?? []).Where(double.IsFinite).OrderBy(v => v)
                    .Where(v => v > scale.DomainMin && v < scale.DomainMax));

                edges.Add(scale.DomainMax);

                for (var b = 0; b + 1 < edges.Count; b++)
                {
                    var cls = "rg" + Math.Min(edges.Count - 2 - b, 2);

                    if (edges.Count == 2)
                    {
                        cls = "rg0";
                    }

                    var x0 = scale.Map(edges[b]);
                    var x1 = scale.Map(edges[b + 1]);

                    canvas.W.Empty("rect", ("class", cls), ("x", Num.F(x0)), ("y", Num.F(top)),
                        ("width", Num.F(x1 - x0)),
                        ("height", Num.F(height)));
                }

                var rows = new List<TipRow> { new(item.Label, true, 0), new("Value: " + format(item.Value)) };

                if (item.Target is { } target)
                {
                    rows.Add(new TipRow("Target: " + format(target)));
                }

                canvas.OpenSeries(0);
                canvas.OpenMark(0, string.Join(", ", rows.Select(r => r.Text)));
                var v0 = scale.Map(0);
                var v1 = scale.Map(item.Value);
                var barHeight = height / (s.Thick ? 1.6 : 3);

                canvas.W.Empty("rect", ("class", "bar m"), ("x", Num.F(Math.Min(v0, v1))),
                    ("y", Num.F(cy - barHeight / 2)),
                    ("width", Num.F(Math.Max(Math.Abs(v1 - v0), 1))), ("height", Num.F(barHeight)), ("rx", "2"));

                if (item.Target is { } tv && double.IsFinite(tv))
                {
                    var tx = scale.Map(tv);

                    canvas.W.Empty("line", ("class", "tgt"), ("x1", Num.F(tx)), ("y1", Num.F(cy - height * 0.4)),
                        ("x2", Num.F(tx)), ("y2", Num.F(cy + height * 0.4)));
                }

                canvas.Tooltip(v1, top, rows);
                canvas.CloseMark();
                canvas.W.Close();
            }

            if (items.Count == 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}