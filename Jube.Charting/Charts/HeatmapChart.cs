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
    public sealed class HeatmapChart(HeatmapChartSpec spec) : IStyleModeChart
    {
        public HeatmapChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new HeatmapChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, id => HeatRenderer.Render(Model(), id));
        }

        private HeatModel Model()
        {
            var s = Spec;
            var nr = s.RowLabels.Count;
            var nc = s.ColumnLabels.Count;

            if (s.Values.Count != nr || s.Values.Any(r => r.Count != nc))
            {
                throw new ChartSpecificationException(
                    "Heatmap values must be a matrix with one row per row label and one column per column label.");
            }

            var values = s.Values.Select(r => r.ToArray()).ToArray();
            var e = new Extent();

            foreach (var row in values)
            {
                foreach (var v in row)
                {
                    e.Add(v);
                }
            }

            double lo;
            double hi;

            if (s.Diverging)
            {
                var m = e.Any ? Math.Max(Math.Abs(e.Min - s.Center), Math.Abs(e.Max - s.Center)) : 1;

                if (s is { Min: { } a, Max: { } b })
                {
                    m = Math.Max(Math.Abs(a - s.Center), Math.Abs(b - s.Center));
                }

                if (m == 0)
                {
                    m = 1;
                }

                lo = s.Center - m;
                hi = s.Center + m;
            }
            else
            {
                lo = s.Min ?? (e.Any ? e.Min : 0);
                hi = s.Max ?? (e.Any ? e.Max : 1);

                if (hi <= lo)
                {
                    hi = lo + 1;
                }
            }

            var decimals = Math.Clamp(s.Decimals, 0, 6);

            string Fmt(double v)
            {
                return double.IsFinite(v)
                    ? Math.Round(v, decimals).ToString("0." + new string('#', decimals),
                        CultureInfo.InvariantCulture)
                    : "n/a";
            }

            return new HeatModel
            {
                Options = s.Options,
                Kind = s.Diverging ? "Diverging heatmap" : "Heatmap",
                Rows = s.RowLabels,
                Cols = s.ColumnLabels,
                Values = values,
                Diverging = s.Diverging,
                Domain = (lo, hi),
                LegendTitle = s.LegendTitle,
                Detail = e.Any
                    ? "Values range from " + NumberFormat.Value(e.Min) + " to " + NumberFormat.Value(e.Max) + "."
                    : "There is no data to plot.",
                CellText = (r, c) => s.ShowValues && double.IsFinite(values[r][c]) ? [Fmt(values[r][c])] : [],
                Tip = (r, c) =>
                [
                    new TipRow(s.RowLabels[r] + " / " + s.ColumnLabels[c], true),
                    new TipRow("Value: " + Fmt(values[r][c]))
                ],
                LegendFormat = v => Fmt(v)
            };
        }
    }
}