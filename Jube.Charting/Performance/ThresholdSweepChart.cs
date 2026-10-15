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
    public sealed class ThresholdSweepChart(ThresholdSweepChartSpec spec) : IStyleModeChart
    {
        public ThresholdSweepChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ThresholdSweepChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var points = s.Sweep?.Points ?? [];

            var series = new List<PointSeries>
            {
                Line("Precision", points, p => p.Precision),
                Line("Recall", points, p => p.Recall),
                Line("F1", points, p => p.F1)
            };

            if (s.ShowMcc)
            {
                series.Add(Line("MCC", points, p => p.Mcc));
            }

            var references = new List<ReferenceLine>();
            var highlights = new List<PointHighlight>();
            var annotations = new List<Annotation>();
            var summary = new List<string>();

            if (points.Length > 0)
            {
                if (s.CurrentThreshold is { } current && double.IsFinite(current))
                {
                    references.Add(new ReferenceLine(current, 0, current, 1,
                        "Current threshold " + NumberFormat.Value(current)));

                    annotations.Add(new Annotation(current, 0.03, "Current " + NumberFormat.Value(current)));
                    summary.Add("Current threshold " + NumberFormat.Value(current) + ".");
                }

                if (s.MinimumRiskThreshold is { } risk && double.IsFinite(risk))
                {
                    references.Add(new ReferenceLine(risk, 0, risk, 1,
                        "Minimum-risk threshold " + NumberFormat.Value(Math.Round(risk, 3)), 5));

                    annotations.Add(new Annotation(risk, 0.03,
                        "Minimum risk " + NumberFormat.Value(Math.Round(risk, 3))));

                    summary.Add("Minimum-risk threshold " + NumberFormat.Value(Math.Round(risk, 3)) + ".");
                }

                var best = s.Sweep!.BestF1Index;

                if (best >= 0 && best < points.Length)
                {
                    var b = points[best];

                    highlights.Add(new PointHighlight(2, b.Threshold, b.F1,
                        "Best F1 " + NumberFormat.Value(Math.Round(b.F1, 3)) + " at threshold " +
                        NumberFormat.Value(Math.Round(b.Threshold, 3)) + "\nPrecision " +
                        NumberFormat.Value(Math.Round(double.IsFinite(b.Precision) ? b.Precision : 0, 3)) +
                        "\nRecall " +
                        NumberFormat.Value(Math.Round(b.Recall, 3))));

                    summary.Add("Best F1 " + NumberFormat.Value(Math.Round(b.F1, 3)) + " at threshold " +
                                NumberFormat.Value(Math.Round(b.Threshold, 3)) + ".");
                }
            }

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Decision threshold",
                YAxisTitle = s.Options.YAxisTitle ?? "Measure",
                Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.Bottom : s.Options.Legend
            };

            var lowest = s.ShowMcc && points.Any(p => p.Mcc < 0) ? Math.Floor(points.Min(p => p.Mcc) * 10) / 10 : 0;

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "Threshold sweep chart",
                Mode = XyMode.Line,
                Series = series,
                Markers = MarkerMode.Off,
                References = references,
                Highlights = highlights,
                Annotations = annotations,
                YDomain = (lowest, 1),
                SummaryDetail = string.Join(" ", summary),
                XFormat = NumberFormat.Value,
                YFormat = NumberFormat.Value
            }, id));
        }

        private static PointSeries Line(string name, SweepPoint[] points, Func<SweepPoint, double> pick)
        {
            var data = new DataPoint[points.Length];

            for (var i = 0; i < points.Length; i++)
            {
                var y = pick(points[i]);
                data[i] = new DataPoint(points[i].Threshold, y);
            }

            return new PointSeries(name, data);
        }
    }
}