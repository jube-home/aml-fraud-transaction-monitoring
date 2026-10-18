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
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Jube.Charting.Performance.Models;
using Jube.Charting.Ticks;

namespace Jube.Charting.Performance
{
    public sealed class GainsChart(GainsChartSpec spec) : IStyleModeChart
    {
        public GainsChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new GainsChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            ChartLimits.Series(s.Curves.Count);
            var series = new List<PointSeries>();
            var highlights = new List<PointHighlight>();
            var references = new List<ReferenceLine>();
            var summary = new List<string>();
            var annotations = new List<Annotation>();
            var maxAbsoluteY = 0d;

            for (var ci = 0; ci < s.Curves.Count; ci++)
            {
                var c = s.Curves[ci];

                if (c.Population.Count != c.Captured.Count)
                {
                    throw new ChartSpecificationException(
                        "A gains curve needs equally many population and captured values.");
                }

                if (c.Thresholds is not null && c.Thresholds.Count != c.Population.Count)
                {
                    throw new ChartSpecificationException("Thresholds must have one value per gains point.");
                }

                var points = new DataPoint[c.Population.Count];

                for (var i = 0; i < points.Length; i++)
                {
                    var label = c.Thresholds is null || !double.IsFinite(c.Thresholds[i])
                        ? null
                        : "Score cut-off " + NumberFormat.Value(c.Thresholds[i]);

                    var y = s.Absolute ? c.Captured[i] * (c.TotalPositiveValue ?? 0) : c.Captured[i];
                    maxAbsoluteY = Math.Max(maxAbsoluteY, y);
                    points[i] = new DataPoint(c.Population[i], y, label);
                }

                series.Add(new PointSeries(c.Name, points, c.Color));

                if (s.Absolute || !s.ShowDecileMarks || points.Length < 2)
                {
                    continue;
                }

                for (var d = 1; d <= 9; d++)
                {
                    var x = d / 10.0;
                    var y = Interpolate(c.Population, c.Captured, x);

                    if (!double.IsFinite(y))
                    {
                        continue;
                    }

                    var lift = y / x;

                    highlights.Add(new PointHighlight(ci, x, y,
                        "Top " + NumberFormat.Percent(x, 0) + " of scores\nCaptures " + NumberFormat.Percent(y) +
                        " of positives\nLift " + NumberFormat.Value(Math.Round(lift, 2)), false));
                }

                var at20 = Interpolate(c.Population, c.Captured, 0.2);

                if (double.IsFinite(at20))
                {
                    summary.Add(c.Name + " captures " + NumberFormat.Percent(at20, 0) +
                                " of positives in the top 20%.");
                }

                if (s.ShowPerfect && c.Prevalence is { } prevalence and > 0 and < 1)
                {
                    references.Add(new ReferenceLine(0, 0, prevalence, 1, "Perfect model", ci));
                    references.Add(new ReferenceLine(prevalence, 1, 1, 1, "Perfect model", ci));
                    annotations.Add(new Annotation(prevalence, 1, "Perfect model"));
                }
            }

            if (s is { Absolute: false, ShowRandom: true })
            {
                references.Add(new ReferenceLine(0, 0, 1, 1, "Random ranking"));
                annotations.Add(new Annotation(0.6, 0.6, "Random ranking"));
            }

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Share of population, highest score first",
                YAxisTitle = s.Options.YAxisTitle ??
                             (s.Absolute ? "Cumulative value captured" : "Share of positives captured"),
                Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.Bottom : s.Options.Legend
            };

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "Cumulative gains chart",
                Mode = XyMode.Line,
                Series = series,
                Markers = MarkerMode.Off,
                References = references,
                Highlights = highlights,
                Annotations = annotations,
                XDomain = (0, 1),
                YDomain = s.Absolute ? (0, maxAbsoluteY <= 0 ? 1 : maxAbsoluteY) : (0, 1),
                SummaryDetail = string.Join(" ", summary),
                XFormat = v => NumberFormat.Percent(v, 0),
                YFormat = s.Absolute ? v => NumberFormat.Value(v) : v => NumberFormat.Percent(v, 0)
            }, id));
        }

        internal static double Interpolate(IReadOnlyList<double> xs, IReadOnlyList<double> ys, double x)
        {
            if (xs.Count == 0 || x < xs[0] || x > xs[^1])
            {
                return double.NaN;
            }

            for (var i = 1; i < xs.Count; i++)
            {
                if (x <= xs[i])
                {
                    var span = xs[i] - xs[i - 1];

                    return span <= 0 ? ys[i] : ys[i - 1] + (ys[i] - ys[i - 1]) * (x - xs[i - 1]) / span;
                }
            }

            return ys[^1];
        }
    }
}