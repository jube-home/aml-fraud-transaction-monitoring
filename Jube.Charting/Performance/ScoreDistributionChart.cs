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

using Jube.Charting.Charts;
using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Jube.Charting.Performance.Models;
using Jube.Charting.Ticks;

namespace Jube.Charting.Performance
{
    public sealed class ScoreDistributionChart(ScoreDistributionChartSpec spec) : IStyleModeChart
    {
        public ScoreDistributionChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ScoreDistributionChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var d = s.Distribution ?? new DistributionResult([], [], [], 0, 0);
            var bins = d.PositiveCounts.Length;

            var series = new List<PointSeries>
            {
                Steps(s.NegativeName, d.Edges, d.NegativeCounts, d.Negatives),
                Steps(s.PositiveName, d.Edges, d.PositiveCounts, d.Positives)
            };

            var references = new List<ReferenceLine>();

            if (s.Threshold is { } t && double.IsFinite(t) && bins > 0)
            {
                references.Add(new ReferenceLine(t, 0, t, 1, "Decision threshold " + NumberFormat.Value(t), 2));
            }

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Predicted probability",
                YAxisTitle = s.Options.YAxisTitle ?? "Share of class",
                Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.Bottom : s.Options.Legend
            };

            var peak = bins == 0
                ? 1
                : Math.Max(Peak(d.NegativeCounts, d.Negatives), Peak(d.PositiveCounts, d.Positives));

            var top = peak <= 0 ? 1 : Math.Min(1, Math.Ceiling(peak * 1.08 * 20) / 20);
            var refsScaled = references.Select(r => r with { Y2 = top }).ToList();

            var annotations = s.Threshold is { } at && double.IsFinite(at) && bins > 0
                ? new List<Annotation> { new(at, top, "Threshold " + NumberFormat.Value(at)) }
                : new List<Annotation>();

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "Class-conditional score distribution",
                Mode = XyMode.Area,
                Series = series,
                Markers = MarkerMode.Off,
                References = refsScaled,
                Annotations = annotations,
                XDomain = bins > 0 ? (d.Edges[0], d.Edges[^1]) : null,
                YDomain = (0, top),
                SummaryDetail = bins > 0
                    ? d.Positives + " " + s.PositiveName + " and " + d.Negatives + " " + s.NegativeName +
                      " observations, each class shown as its own share of the bins."
                    : string.Empty,
                XFormat = v => NumberFormat.Value(v),
                YFormat = v => NumberFormat.Percent(v, 0)
            }, id));
        }

        private static double Peak(double[] counts, double total)
        {
            return total > 0 ? counts.Max() / total : 0;
        }

        private PointSeries Steps(string name, double[] edges, double[] counts, double total)
        {
            var points = new List<DataPoint>();

            for (var i = 0; i < counts.Length; i++)
            {
                var share = total > 0 ? counts[i] / total : 0;

                var label = NumberFormat.Value(Math.Round(edges[i], 3)) + " to " +
                            NumberFormat.Value(Math.Round(edges[i + 1], 3)) + "\n" + name + ": " +
                            NumberFormat.Percent(share) + " of class (" + NumberFormat.Value(counts[i]) + ")";

                points.Add(new DataPoint(edges[i], share, label));
                points.Add(new DataPoint(edges[i + 1], share, label));
            }

            return new PointSeries(name, points);
        }
    }
}