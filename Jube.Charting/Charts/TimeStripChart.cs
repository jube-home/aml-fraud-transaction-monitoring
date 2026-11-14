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
using System.Globalization;
using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Core.Models;
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Jube.Charting.Scales;
using Jube.Charting.Svg;

namespace Jube.Charting.Charts
{
    public sealed class TimeStripChart(TimeStripChartSpec spec) : IStyleModeChart
    {
        public TimeStripChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new TimeStripChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        public static string Format(DateTimeOffset when, TimeSpan span)
        {
            var format = span < TimeSpan.FromDays(2) ? "HH:mm"
                : span < TimeSpan.FromDays(300) ? "MMM d" : "yyyy-MM-dd";

            return when.UtcDateTime.ToString(format, CultureInfo.InvariantCulture);
        }

        private string Draw(string id)
        {
            var s = Spec;
            var bins = s.Bins;
            ChartLimits.Points(bins.Count);

            foreach (var bin in bins)
            {
                if (bin.To <= bin.From || bin.Count < 0)
                {
                    throw new ChartSpecificationException(
                        "Time strip bins need an end after the start and a non-negative count.");
                }
            }

            var total = bins.Sum(b => (long)b.Count);

            var summary = bins.Count == 0
                ? "Time strip with no events."
                : "Time strip of " + total.ToString(CultureInfo.InvariantCulture) + " " + s.Name.ToLowerInvariant() +
                  " in " + bins.Count + " bins from " + Stamp(bins.Min(b => b.From)) + " to " +
                  Stamp(bins.Max(b => b.To)) + ".";

            var options = s.Options with { Legend = LegendPosition.None };
            var canvas = new ChartCanvas(options, id, summary, [], 1, []);
            canvas.LimitInteractive(bins.Count);
            var top = canvas.Frame(0, 0).Top;
            var plot = new Rect(ChartCanvas.Pad, top, canvas.Width - ChartCanvas.Pad, Math.Max(top + 8, canvas.Height - 20));
            canvas.SetPlot(plot);
            canvas.Begin();

            if (bins.Count == 0)
            {
                canvas.Baseline(plot.Bottom);

                return canvas.End();
            }

            var start = bins.Min(b => b.From);
            var end = bins.Max(b => b.To);
            var span = end - start;
            var scale = new LinearScale(0, Math.Max(span.TotalSeconds, 1), plot.Left, plot.Right);
            var max = Math.Max(bins.Max(b => b.Count), 1);
            canvas.OpenSeries(0);

            for (var i = 0; i < bins.Count; i++)
            {
                var bin = bins[i];
                var x0 = scale.Map((bin.From - start).TotalSeconds);
                var x1 = scale.Map((bin.To - start).TotalSeconds);
                var height = bin.Count == 0 ? 0 : Math.Max(1.5, plot.Height * bin.Count / max);

                var rows = new List<TipRow>
                {
                    new(Stamp(bin.From) + " to " + Stamp(bin.To), true),
                    new(s.Name + ": " + bin.Count.ToString(CultureInfo.InvariantCulture), false, 0)
                };

                canvas.OpenMark(0, string.Join(", ", rows.Select(r => r.Text)), null, null,
                [
                    ("data-bin", i.ToString(CultureInfo.InvariantCulture)), ("data-from", Unix(bin.From)),
                    ("data-to", Unix(bin.To))
                ]);

                canvas.W.Empty("rect", ("class", "bar m"), ("x", Num.F(x0 + 0.5)),
                    ("y", Num.F(plot.Bottom - height)), ("width", Num.F(Math.Max(x1 - x0 - 1, 0.5))),
                    ("height", Num.F(height)));

                canvas.Tooltip((x0 + x1) / 2, plot.Bottom - height, rows);
                canvas.CloseMark();
            }

            canvas.W.Close();
            canvas.Baseline(plot.Bottom);
            var middle = start + TimeSpan.FromTicks(span.Ticks / 2);
            var ty = plot.Bottom + 14;
            canvas.Text(plot.Left, ty, Format(start, span), "tk");
            canvas.Text(plot.CenterX, ty, Format(middle, span), "tk ctr");
            canvas.Text(plot.Right, ty, Format(end, span), "tk end");

            return canvas.End();
        }

        private static string Stamp(DateTimeOffset when)
        {
            return when.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        private static string Unix(DateTimeOffset when)
        {
            return when.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        }
    }
}
