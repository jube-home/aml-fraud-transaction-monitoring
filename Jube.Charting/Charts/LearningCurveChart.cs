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
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class LearningCurveChart(LearningCurveChartSpec spec) : IStyleModeChart
    {
        public LearningCurveChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new LearningCurveChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var n = Math.Max(s.Train.Count, s.Validation.Count);

            if (s.Iterations is not null && s.Iterations.Count != n)
            {
                throw new ChartSpecificationException(
                    "Iterations must have one value per training or validation value.");
            }

            double X(int i)
            {
                return s.Iterations is null ? i + 1 : s.Iterations[i];
            }

            var series = new List<PointSeries>();

            if (s.Train.Count > 0)
            {
                series.Add(new PointSeries("Train", [.. s.Train.Select((v, i) => new DataPoint(X(i), v))]));
            }

            if (s.Validation.Count > 0)
            {
                series.Add(new PointSeries("Validation",
                    [.. s.Validation.Select((v, i) => new DataPoint(X(i), v))]));
            }

            var highlights = new List<PointHighlight>();

            if (s is { MarkBest: true, Validation.Count: > 0 })
            {
                var best = -1;

                for (var i = 0; i < s.Validation.Count; i++)
                {
                    var v = s.Validation[i];

                    if (!double.IsFinite(v))
                    {
                        continue;
                    }

                    if (best < 0 || (s.LowerIsBetter ? v < s.Validation[best] : v > s.Validation[best]))
                    {
                        best = i;
                    }
                }

                if (best >= 0)
                {
                    highlights.Add(new PointHighlight(series.Count - 1, X(best), s.Validation[best],
                        "Best validation " + s.Metric + "\n" + NumberFormat.Value(s.Validation[best]) +
                        " at iteration " +
                        NumberFormat.Value(X(best))));
                }
            }

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Iteration",
                YAxisTitle = s.Options.YAxisTitle ?? s.Metric
            };

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "Learning curve",
                Mode = XyMode.Line,
                Series = series,
                Crosshair = true,
                Markers = MarkerMode.Off,
                Highlights = highlights,
                SummaryDetail = n + " iterations."
            }, id));
        }
    }
}