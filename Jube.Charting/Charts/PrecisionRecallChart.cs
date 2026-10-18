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
using Jube.Charting.Interfaces;
using Jube.Charting.Ticks;
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class PrecisionRecallChart(PrecisionRecallChartSpec spec) : IStyleModeChart
    {
        private const int HoverPoints = 16;

        public PrecisionRecallChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new PrecisionRecallChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            ChartLimits.Series(s.Curves.Count);
            var series = new List<PointSeries>();
            var legend = new List<string>();
            var highlights = new List<PointHighlight>();
            var refs = new List<ReferenceLine>();

            for (var ci = 0; ci < s.Curves.Count; ci++)
            {
                var c = s.Curves[ci];

                if (c.Recall.Count != c.Precision.Count)
                {
                    throw new ChartSpecificationException(
                        "A precision-recall curve needs equally many recall and precision values.");
                }

                if (c.Thresholds is not null && c.Thresholds.Count != c.Recall.Count)
                {
                    throw new ChartSpecificationException("Thresholds must have one value per curve point.");
                }

                var points = new DataPoint[c.Recall.Count];

                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = new DataPoint(c.Recall[i], c.Precision[i]);
                }

                series.Add(new PointSeries(c.Name, points, c.Color));

                legend.Add(c.AveragePrecision is { } ap
                    ? c.Name + " (AP " + ap.ToString("0.000", CultureInfo.InvariantCulture) + ")"
                    : c.Name);

                if (c.Prevalence is { } prev && double.IsFinite(prev) && refs.Count == 0)
                {
                    refs.Add(new ReferenceLine(0, prev, 1, prev,
                        "No skill (prevalence " + NumberFormat.Value(prev) + ")"));
                }

                if (s.ShowThresholdPoints && c.Thresholds is not null && points.Length > 2)
                {
                    var step = Math.Max(1, (points.Length - 1) / HoverPoints);

                    for (var i = step; i < points.Length - 1; i += step)
                    {
                        var t = c.Thresholds[i];

                        highlights.Add(new PointHighlight(ci, points[i].X, points[i].Y,
                            "Threshold " + (double.IsInfinity(t) ? "none" : NumberFormat.Value(t)) + "\nRecall " +
                            NumberFormat.Value(points[i].X) + "\nPrecision " + NumberFormat.Value(points[i].Y), false));
                    }
                }
            }

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Recall",
                YAxisTitle = s.Options.YAxisTitle ?? "Precision",
                Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.Top : s.Options.Legend
            };

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "Precision-recall curve",
                Mode = XyMode.Line,
                Series = series,
                LegendOverride = legend,
                Markers = MarkerMode.Off,
                References = refs,
                Highlights = highlights,
                XDomain = (0, 1),
                YDomain = (0, 1),
                SummaryDetail = string.Join(" ", legend.Select(l => l + "."))
            }, id));
        }
    }
}