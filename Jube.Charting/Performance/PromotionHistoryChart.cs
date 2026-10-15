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
using Jube.Charting.Charts;
using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Jube.Charting.Performance.Models;
using Jube.Charting.Scales;
using Jube.Charting.Ticks;

namespace Jube.Charting.Performance
{
    public sealed class PromotionHistoryChart(PromotionHistoryChartSpec spec) : IStyleModeChart
    {
        public PromotionHistoryChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new PromotionHistoryChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            ChartLimits.Points(s.Promotions.Count * 3L);
            var ordered = s.Promotions.Where(p => double.IsFinite(p.Score)).OrderBy(p => p.Date).ToList();
            var score = new List<DataPoint>();
            var best = new List<DataPoint>();
            var complexity = new List<DataPoint>();
            var highlights = new List<PointHighlight>();
            var bestSoFar = double.NegativeInfinity;
            var hasComplexity = s.ShowComplexity && ordered.Any(p => double.IsFinite(p.Complexity));

            for (var i = 0; i < ordered.Count; i++)
            {
                var p = ordered[i];
                var x = TimeScale.ToSeconds(p.Date);
                bestSoFar = Math.Max(bestSoFar, p.Score);

                var label = "Promotion " + (i + 1).ToString(CultureInfo.InvariantCulture) +
                            (double.IsFinite(p.Trials)
                                ? " after " + NumberFormat.Value(p.Trials) +
                                  (Math.Abs(p.Trials - 1) < 0.0001 ? " trial" : " trials")
                                : string.Empty) +
                            (p.Active ? ", active" : string.Empty);

                score.Add(new DataPoint(x, p.Score, label));
                best.Add(new DataPoint(x, bestSoFar));

                if (hasComplexity && double.IsFinite(p.Complexity))
                {
                    complexity.Add(new DataPoint(x, p.Complexity,
                        "Promotion " + (i + 1).ToString(CultureInfo.InvariantCulture)));
                }

                if (p.Active)
                {
                    var rows = label + "\nScore " + NumberFormat.Value(Math.Round(p.Score, 3));

                    if (double.IsFinite(p.Complexity))
                    {
                        rows += "\nTopology complexity " + NumberFormat.Value(p.Complexity);
                    }

                    if (double.IsFinite(p.TrialsToDate))
                    {
                        rows += "\nTrials to date " + NumberFormat.Value(p.TrialsToDate);
                    }

                    highlights.Add(new PointHighlight(0, x, p.Score, rows));
                }
            }

            var series = new List<PointSeries> { new("Score at promotion", score) };
            var tipNames = new List<string?> { "Score" };

            if (s.ShowBestSoFar)
            {
                series.Add(new PointSeries("Best score so far", best));
                tipNames.Add("Best score");
            }

            if (hasComplexity)
            {
                series.Add(new PointSeries("Topology complexity", complexity, null, true));
                tipNames.Add("Topology complexity");
            }

            var trials = ordered.Where(p => double.IsFinite(p.TrialsToDate)).Select(p => p.TrialsToDate)
                .DefaultIfEmpty(0)
                .Max();

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Promotion date (UTC)",
                YAxisTitle = s.Options.YAxisTitle ?? "Score",
                SecondaryYAxisTitle = hasComplexity ? s.Options.SecondaryYAxisTitle ?? "Topology complexity" : null,
                Subtitle = s.Options.Subtitle ?? (ordered.Count == 0
                    ? null
                    : ordered.Count.ToString(CultureInfo.InvariantCulture) +
                      (ordered.Count == 1 ? " promotion" : " promotions") +
                      ", best score " + NumberFormat.Value(Math.Round(bestSoFar, 3)) +
                      (trials > 0 ? ", " + NumberFormat.Value(trials) + " trials explored" : string.Empty)),
                Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.Bottom : s.Options.Legend
            };

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "Promotion history chart",
                Mode = XyMode.Line,
                XAxis = XAxisKind.Time,
                Series = series,
                Markers = MarkerMode.On,
                Step = true,
                Highlights = highlights,
                TipYNames = tipNames,
                SummaryDetail = ordered.Count == 0 ? string.Empty : options.Subtitle + ".",
                YFormat = v => NumberFormat.Value(v),
                Y2Format = v => NumberFormat.Value(v)
            }, id));
        }
    }
}