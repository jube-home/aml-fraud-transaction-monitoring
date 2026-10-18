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
    public sealed class CandlestickChart(CandlestickChartSpec spec) : IStyleModeChart
    {
        public CandlestickChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new CandlestickChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var candles = s.Candles;
            ChartLimits.Categories(candles.Count);
            var extent = new Extent();

            foreach (var c in candles)
            {
                extent.Add(c.High);
                extent.Add(c.Low);
            }

            var ohlc = s.Style == CandleStyle.Ohlc;
            var summary = (ohlc ? "OHLC chart" : "Candlestick chart") + " with " + candles.Count + " periods.";

            var canvas = new ChartCanvas(s.Options with { Legend = LegendPosition.Top }, id, summary,
                [new LegendEntry("Rising", 0), new LegendEntry("Falling", 1)], 2, []);
            canvas.LimitInteractive(candles.Count);
            var layout = CategoryLayout.Vertical(canvas, extent, candles.Count, s.ValueFormat, 0.35);
            canvas.Begin();
            layout.DrawAxes(canvas, [.. candles.Select(c => c.Label)], false);
            var y = layout.Value;

            for (var group = 0; group < 2; group++)
            {
                canvas.OpenSeries(group);

                for (var i = 0; i < candles.Count; i++)
                {
                    var c = candles[i];
                    var rising = c.Close >= c.Open;

                    if (rising == (group == 1) ||
                        new[] { c.Open, c.High, c.Low, c.Close }.Any(v => !double.IsFinite(v)))
                    {
                        continue;
                    }

                    var x0 = layout.Band.Start(i);
                    var w = layout.Band.Bandwidth;
                    var cx = x0 + w / 2;
                    var change = c.Open != 0 ? (c.Close - c.Open) / Math.Abs(c.Open) : double.NaN;

                    var rows = new List<TipRow>
                    {
                        new(c.Label, true, group),
                        new("Open: " + NumberFormat.Value(c.Open)),
                        new("High: " + NumberFormat.Value(c.High)),
                        new("Low: " + NumberFormat.Value(c.Low)),
                        new("Close: " + NumberFormat.Value(c.Close)),
                        new("Change: " + NumberFormat.Percent(change, 2))
                    };

                    canvas.OpenMark(group, string.Join(", ", rows.Select(r => r.Text)));

                    canvas.W.Empty("rect", ("class", "hit"), ("x", Num.F(x0)), ("y", Num.F(layout.Plot.Top)),
                        ("width", Num.F(w)), ("height", Num.F(layout.Plot.Height)));

                    if (ohlc)
                    {
                        var p = new PathBuilder().Move(cx, y.Map(c.High)).Line(cx, y.Map(c.Low))
                            .Move(x0 + w * 0.05, y.Map(c.Open)).Line(cx, y.Map(c.Open))
                            .Move(cx, y.Map(c.Close)).Line(x0 + w * 0.95, y.Map(c.Close));

                        canvas.W.Empty("path", ("class", "wk m"), ("d", p.ToString()));
                    }
                    else
                    {
                        var wick = new PathBuilder().Move(cx, y.Map(c.High)).Line(cx, y.Map(c.Low));
                        canvas.W.Empty("path", ("class", "wk"), ("d", wick.ToString()));
                        var top = y.Map(Math.Max(c.Open, c.Close));
                        var height = Math.Max(Math.Abs(y.Map(c.Open) - y.Map(c.Close)), 1.5);

                        canvas.W.Empty("rect", ("class", rising ? "cd hollow m" : "cd m"), ("x", Num.F(x0)),
                            ("y", Num.F(top)),
                            ("width", Num.F(w)), ("height", Num.F(height)), ("rx", "1"));
                    }

                    canvas.Tooltip(cx, y.Map(c.High), rows, false, 8);
                    canvas.CloseMark();
                }

                canvas.W.Close();
            }

            if (candles.Count == 0)
            {
                canvas.Text(layout.Plot.CenterX, layout.Plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles();

            return canvas.End();
        }
    }
}