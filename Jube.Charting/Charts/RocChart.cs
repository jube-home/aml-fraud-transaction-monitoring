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
    public sealed class RocChart(RocChartSpec spec) : IStyleModeChart
    {
        private const int HoverPoints = 16;

        private static string ThresholdLabel(RocCurve c, int i)
        {
            var t = c.Thresholds![i];
            var thr = double.IsInfinity(t) ? "none" : NumberFormat.Value(t);

            return "Threshold " + thr + "\nFalse positive rate " + NumberFormat.Value(c.FalsePositiveRate[i]) +
                   "\nTrue positive rate " + NumberFormat.Value(c.TruePositiveRate[i]);
        }

        public RocChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new RocChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            ChartLimits.Series(s.Curves.Count);
            var series = new List<PointSeries>();
            var legend = new List<string>();
            var highlights = new List<PointHighlight>();

            for (var ci = 0; ci < s.Curves.Count; ci++)
            {
                var c = s.Curves[ci];

                if (c.FalsePositiveRate.Count != c.TruePositiveRate.Count)
                {
                    throw new ChartSpecificationException(
                        "A ROC curve needs equally many false and true positive rates.");
                }

                if (c.Thresholds is not null && c.Thresholds.Count != c.FalsePositiveRate.Count)
                {
                    throw new ChartSpecificationException("Thresholds must have one value per curve point.");
                }

                var points = new DataPoint[c.FalsePositiveRate.Count];

                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = new DataPoint(c.FalsePositiveRate[i], c.TruePositiveRate[i]);
                }

                series.Add(new PointSeries(c.Name, points, c.Color));
                var auc = c.Auc ?? CurveMath.Auc(c.FalsePositiveRate, c.TruePositiveRate);

                legend.Add(c.Name + " (AUC " + auc.ToString("0.000", CultureInfo.InvariantCulture) +
                           ")");

                if (c.Thresholds is null)
                {
                    continue;
                }

                if (s.ShowThresholdPoints && points.Length > 2)
                {
                    var step = Math.Max(1, (points.Length - 1) / HoverPoints);

                    for (var i = step; i < points.Length - 1; i += step)
                    {
                        highlights.Add(new PointHighlight(ci, points[i].X, points[i].Y, ThresholdLabel(c, i), false));
                    }
                }

                if (s is { ShowBestPoint: true, Curves.Count: <= 3 })
                {
                    var best = -1;
                    var bestJ = double.NegativeInfinity;

                    for (var i = 0; i < points.Length; i++)
                    {
                        var j = points[i].Y - points[i].X;

                        if (double.IsFinite(j) && j > bestJ && double.IsFinite(c.Thresholds[i]))
                        {
                            bestJ = j;
                            best = i;
                        }
                    }

                    if (best >= 0)
                    {
                        highlights.Add(new PointHighlight(ci, points[best].X, points[best].Y,
                            "Best cut-off (Youden J " + NumberFormat.Value(bestJ) + ")\n" + ThresholdLabel(c, best)));
                    }
                }
            }

            var refs = s.ShowChance ? new[] { new ReferenceLine(0, 0, 1, 1, "Chance") } : [];

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "False positive rate",
                YAxisTitle = s.Options.YAxisTitle ?? "True positive rate",
                Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.Top : s.Options.Legend
            };

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "ROC curve",
                Mode = XyMode.Line,
                Series = series,
                LegendOverride = legend,
                Markers = MarkerMode.Off,
                References = refs,
                Highlights = highlights,
                XDomain = (0, 1),
                YDomain = (0, 1),
                SummaryDetail = string.Join(" ", legend.Select(l => l + ".")),
                XFormat = NumberFormat.Value,
                YFormat = NumberFormat.Value
            }, id));
        }
    }
}