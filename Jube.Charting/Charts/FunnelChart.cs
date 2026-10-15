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
    public sealed class FunnelChart(FunnelChartSpec spec) : IStyleModeChart
    {
        public FunnelChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new FunnelChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var stages = s.Stages.Where(t => double.IsFinite(t.Value) && t.Value >= 0).ToList();
            ChartLimits.Categories(stages.Count);
            var format = s.ValueFormat ?? NumberFormat.Value;
            var first = stages.Count > 0 ? stages[0].Value : 0;

            var summary = "Funnel chart with " + stages.Count + " stages" + (stages.Count > 0
                ? " from " + TextMetrics.Truncate(stages[0].Label, 100, 12) + " (" + format(first) + ") to " +
                  TextMetrics.Truncate(stages[^1].Label, 100, 12) + " (" + format(stages[^1].Value) + ")."
                : ".");

            var canvas = new ChartCanvas(s.Options with { Legend = LegendPosition.None }, id, summary, [], 0, [], true);
            canvas.LimitInteractive(stages.Count);
            canvas.FramePlain(8);
            var plot = canvas.Plot;
            canvas.Begin();

            if (stages.Count == 0 || first <= 0)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");

                return canvas.End();
            }

            var n = stages.Count;
            var gap = 4.0;
            var height = (plot.Height - gap * (n - 1)) / n;
            var maxWidth = Math.Min(plot.Width, 720);
            var cx = plot.CenterX;

            for (var i = 0; i < n; i++)
            {
                var top = plot.Top + i * (height + gap);
                var w0 = maxWidth * stages[i].Value / first;
                var w1 = i + 1 < n ? maxWidth * stages[i + 1].Value / first : w0 * 0.85;
                w0 = Math.Max(w0, 2);
                w1 = Math.Max(w1, 2);
                var bin = n == 1 ? 4 : 1 + (int)Math.Round(i * 6.0 / (n - 1));
                var ofFirst = stages[i].Value / first;
                var ofPrevious = i > 0 && stages[i - 1].Value > 0 ? stages[i].Value / stages[i - 1].Value : double.NaN;

                var rows = new List<TipRow>
                {
                    new(stages[i].Label, true),
                    new("Value: " + format(stages[i].Value)),
                    new("Of first stage: " + NumberFormat.Percent(ofFirst))
                };

                if (i > 0)
                {
                    rows.Add(new TipRow("Of previous stage: " + NumberFormat.Percent(ofPrevious)));
                }

                canvas.OpenMark(-1, string.Join(", ", rows.Select(r => r.Text)));

                var p = new PathBuilder().Move(cx - w0 / 2, top).Line(cx + w0 / 2, top).Line(cx + w1 / 2, top + height)
                    .Line(cx - w1 / 2, top + height).Close();

                canvas.W.Empty("path", ("class", "cell m q" + bin), ("d", p.ToString()));
                var narrow = Math.Min(w0, w1);
                var label = TextMetrics.Truncate(stages[i].Label, narrow - 12, 12, true);

                if (height >= 30 && label.Length > 0)
                {
                    canvas.W.Element("text", label, ("class", "lbl ctr mid qt" + bin), ("x", Num.F(cx)),
                        ("y", Num.F(top + height / 2 - 7)));

                    canvas.W.Element("text",
                        TextMetrics.Truncate(format(stages[i].Value) + " (" + NumberFormat.Percent(ofFirst, 0) + ")",
                            narrow - 12, 11),
                        ("class", "lbl ctr mid qt" + bin), ("x", Num.F(cx)), ("y", Num.F(top + height / 2 + 8)));
                }
                else if (height >= 16 && label.Length > 0)
                {
                    canvas.W.Element("text", label, ("class", "lbl ctr mid qt" + bin), ("x", Num.F(cx)),
                        ("y", Num.F(top + height / 2)));
                }

                canvas.Tooltip(cx, top + height / 2, rows);
                canvas.CloseMark();
            }

            return canvas.End();
        }
    }
}