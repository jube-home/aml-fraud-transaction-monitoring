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
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Charting.Charts
{
    public sealed class ScatterChart(ScatterChartSpec spec) : IStyleModeChart
    {
        public ScatterChartSpec Spec { get; } = spec;
        public bool IsBubble => Spec.Series.Any(s => s.Points.Any(p => double.IsFinite(p.Size)));

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ScatterChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var bubble = IsBubble;
            var references = s.References.Concat(s.TrendLines ? Trends(s.Series) : []).ToArray();

            return ChartRun.Execute(s.Options, id => XyRenderer.Render(new XyModel
            {
                Options = s.Options,
                Kind = bubble ? "Bubble chart" : "Scatter chart",
                Mode = bubble ? XyMode.Bubble : XyMode.Scatter,
                Series = s.Series,
                DataLabels = s.DataLabels,
                Bands = s.Bands,
                Annotations = s.Annotations,
                XFormat = s.XFormat,
                YFormat = s.YFormat,
                Y2Format = s.Y2Format,
                XAxis = s.XAxis,
                YScale = s.YScale,
                References = references,
                XDomain = s.XDomain,
                YDomain = s.YDomain
            }, id));
        }

        private static IEnumerable<ReferenceLine> Trends(IReadOnlyList<PointSeries> series)
        {
            for (var i = 0; i < series.Count; i++)
            {
                var pts = series[i].Points.Where(p => double.IsFinite(p.X) && double.IsFinite(p.Y)).ToArray();

                if (pts.Length < 2)
                {
                    continue;
                }

                var mx = pts.Average(p => p.X);
                var my = pts.Average(p => p.Y);
                var sxx = pts.Sum(p => (p.X - mx) * (p.X - mx));
                var syy = pts.Sum(p => (p.Y - my) * (p.Y - my));
                var sxy = pts.Sum(p => (p.X - mx) * (p.Y - my));

                if (sxx <= 0)
                {
                    continue;
                }

                var slope = sxy / sxx;
                var intercept = my - slope * mx;
                var r2 = syy > 0 ? sxy * sxy / (sxx * syy) : 1;
                var x0 = pts.Min(p => p.X);
                var x1 = pts.Max(p => p.X);

                yield return new ReferenceLine(x0, intercept + slope * x0, x1, intercept + slope * x1,
                    series[i].Name + " trend: y = " + NumberFormat.Value(slope) + " x + " +
                    NumberFormat.Value(intercept) + " (R squared " + NumberFormat.Value(Math.Round(r2, 3)) +
                    ")", i);
            }
        }
    }
}