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
    public sealed class ConfusionMatrixChart(ConfusionMatrixChartSpec spec) : IStyleModeChart
    {
        public ConfusionMatrixChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ConfusionMatrixChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var n = s.Labels.Count;

            if (s.Counts.Count != n || s.Counts.Any(r => r.Count != n))
            {
                throw new ChartSpecificationException(
                    "A confusion matrix must be square with one row and column per label.");
            }

            var counts = s.Counts.Select(r => r.Select(v => double.IsFinite(v) && v > 0 ? v : 0).ToArray()).ToArray();
            var rowSum = counts.Select(r => r.Sum()).ToArray();
            var colSum = Enumerable.Range(0, n).Select(c => counts.Sum(r => r[c])).ToArray();
            var total = rowSum.Sum();

            var rates = Enumerable.Range(0, n).Select(r => Enumerable.Range(0, n)
                .Select(c => rowSum[r] > 0 ? counts[r][c] / rowSum[r] : double.NaN).ToArray()).ToArray();

            var rateMode = s.Mode == ConfusionMode.Rates;
            var colour = rateMode ? rates : [.. counts.Select(r => r.ToArray())];
            var max = counts.Length == 0 ? 1 : Math.Max(counts.Max(r => r.Length == 0 ? 0 : r.Max()), 1);

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Predicted",
                YAxisTitle = s.Options.YAxisTitle ?? "Actual"
            };

            var correct = Enumerable.Range(0, n).Sum(i => counts[i][i]);
            var accuracy = total > 0 ? correct / total : 0;

            return ChartRun.Execute(options, id => HeatRenderer.Render(new HeatModel
            {
                Options = options,
                Kind = "Confusion matrix",
                Rows = s.Labels,
                Cols = s.Labels,
                Values = colour,
                Domain = rateMode ? (0, 1) : (0, max),
                Detail = "Accuracy " + NumberFormat.Percent(accuracy) + " over " + NumberFormat.Value(total) +
                         " observations. Rows are actual classes and columns are predicted classes.",
                LegendTitle = rateMode ? "Row rate" : "Count",
                LegendFormat = v => rateMode ? NumberFormat.Percent(v, 0) : NumberFormat.Value(v),
                CellText = (r, c) => rateMode
                    ? [NumberFormat.Percent(rates[r][c]), NumberFormat.Value(counts[r][c])]
                    : [NumberFormat.Value(counts[r][c]), NumberFormat.Percent(rates[r][c])],
                Tip = (r, c) =>
                [
                    new TipRow("Actual " + s.Labels[r] + ", predicted " + s.Labels[c], true),
                    new TipRow("Count: " + NumberFormat.Value(counts[r][c])),
                    new TipRow("Of actual " + s.Labels[r] + ": " + NumberFormat.Percent(rates[r][c])),
                    new TipRow("Of predicted " + s.Labels[c] + ": " +
                               NumberFormat.Percent(colSum[c] > 0 ? counts[r][c] / colSum[c] : double.NaN)),
                    new TipRow("Of all: " + NumberFormat.Percent(total > 0 ? counts[r][c] / total : double.NaN, 2))
                ]
            }, id));
        }
    }
}