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
    public sealed class KsChart(KsChartSpec spec) : IStyleModeChart
    {
        public KsChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new KsChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var r = s.Result ?? new KsResult([], [], [], double.NaN, double.NaN, double.NaN, double.NaN);
            var pos = new DataPoint[r.Scores.Length];
            var neg = new DataPoint[r.Scores.Length];

            for (var i = 0; i < r.Scores.Length; i++)
            {
                pos[i] = new DataPoint(r.Scores[i], r.PositiveCdf[i],
                    "Score " + NumberFormat.Value(r.Scores[i]) + "\n" + s.PositiveName + " at or below: " +
                    NumberFormat.Percent(r.PositiveCdf[i]) + "\n" + s.NegativeName + " at or below: " +
                    NumberFormat.Percent(r.NegativeCdf[i]));

                neg[i] = new DataPoint(r.Scores[i], r.NegativeCdf[i]);
            }

            var series = new List<PointSeries>
            {
                new(s.NegativeName, neg),
                new(s.PositiveName, pos)
            };

            var references = new List<ReferenceLine>();
            var annotations = new List<Annotation>();
            var hasKs = double.IsFinite(r.Statistic) && r.Scores.Length > 0;

            if (hasKs)
            {
                references.Add(new ReferenceLine(r.AtScore, r.PositiveAtMax, r.AtScore, r.NegativeAtMax,
                    "Maximum separation " + NumberFormat.Value(Math.Round(r.Statistic, 3))));

                var midY = (r.PositiveAtMax + r.NegativeAtMax) / 2;

                annotations.Add(new Annotation(r.AtScore, midY,
                    "KS " + NumberFormat.Value(Math.Round(r.Statistic, 3)) + " at score " +
                    NumberFormat.Value(Math.Round(r.AtScore, 3))));
            }

            if (s.Threshold is { } t && double.IsFinite(t))
            {
                references.Add(new ReferenceLine(t, 0, t, 1, "Decision threshold " + NumberFormat.Value(t), 2));
                annotations.Add(new Annotation(t, 0.04, "Threshold " + NumberFormat.Value(t)));
            }

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Score",
                YAxisTitle = s.Options.YAxisTitle ?? "Cumulative share of class",
                Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.Bottom : s.Options.Legend
            };

            var detail = hasKs
                ? "Kolmogorov-Smirnov statistic " + NumberFormat.Value(Math.Round(r.Statistic, 3)) + " at score " +
                  NumberFormat.Value(Math.Round(r.AtScore, 3)) + "."
                : string.Empty;

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "Kolmogorov-Smirnov chart",
                Mode = XyMode.Line,
                Series = series,
                Markers = MarkerMode.Off,
                References = references,
                Annotations = annotations,
                XDomain = s.XDomain,
                YDomain = (0, 1),
                SummaryDetail = detail,
                XFormat = v => NumberFormat.Value(v),
                YFormat = v => NumberFormat.Percent(v, 0)
            }, id));
        }
    }
}