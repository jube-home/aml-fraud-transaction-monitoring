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
    public sealed class CalibrationChart(CalibrationChartSpec spec) : IStyleModeChart
    {
        public CalibrationChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new CalibrationChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            ChartLimits.Series(s.Curves.Count);
            var series = new List<PointSeries>();
            var legend = new List<string>();

            foreach (var c in s.Curves)
            {
                if (c.MeanPredicted.Count != c.FractionPositive.Count)
                {
                    throw new ChartSpecificationException(
                        "A calibration curve needs equally many predicted and observed values.");
                }

                if (c.Counts is not null && c.Counts.Count != c.MeanPredicted.Count)
                {
                    throw new ChartSpecificationException("Counts must have one value per calibration bin.");
                }

                var points = new DataPoint[c.MeanPredicted.Count];

                for (var i = 0; i < points.Length; i++)
                {
                    var label = c.Counts is null ? null : "Observations: " + NumberFormat.Value(c.Counts[i]);
                    points[i] = new DataPoint(c.MeanPredicted[i], c.FractionPositive[i], label);
                }

                series.Add(new PointSeries(c.Name, points, c.Color));

                legend.Add(c.ExpectedCalibrationError is { } ece
                    ? c.Name + " (ECE " + ece.ToString("0.000", CultureInfo.InvariantCulture) + ")"
                    : c.Name);
            }

            IReadOnlyList<StripBar>? strip = null;
            var first = s.Curves.FirstOrDefault();

            if (s.ShowCounts && first?.Counts is { Count: > 0 } counts)
            {
                var edges = first.BinEdges is { } be && be.Count == counts.Count + 1
                    ? be
                    : [.. Enumerable.Range(0, counts.Count + 1).Select(i => (double)i / counts.Count)];

                strip = [.. counts.Select((c, i) => new StripBar(edges[i], edges[i + 1], c))];
            }

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Mean predicted probability",
                YAxisTitle = s.Options.YAxisTitle ?? "Observed fraction of positives",
                Legend = s.Options.Legend == LegendPosition.Auto ? LegendPosition.Top : s.Options.Legend
            };

            return ChartRun.Execute(options, id => XyRenderer.Render(new XyModel
            {
                Options = options,
                Kind = "Calibration curve",
                Mode = XyMode.Line,
                Series = series,
                LegendOverride = legend,
                Markers = MarkerMode.On,
                References = [new ReferenceLine(0, 0, 1, 1, "Perfectly calibrated")],
                XDomain = (0, 1),
                YDomain = (0, 1),
                Strip = strip,
                StripTitle = "Count",
                SummaryDetail = string.Join(" ", legend.Select(l => l + "."))
            }, id));
        }
    }
}