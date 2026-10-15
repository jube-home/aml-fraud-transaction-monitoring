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
using Jube.Charting.Ticks;

namespace Jube.Charting.Performance
{
    public sealed class LiftChart(LiftChartSpec spec) : IStyleModeChart
    {
        public LiftChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new LiftChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            ChartLimits.Categories(s.Deciles.Count);

            var options = s.Options with
            {
                XAxisTitle = s.Options.XAxisTitle ?? "Score decile, highest scores first",
                YAxisTitle = s.Options.YAxisTitle ?? "Lift over the base rate",
                Legend = s.Options.Legend == LegendPosition.Auto
                    ? s.ShowCumulative ? LegendPosition.Bottom : LegendPosition.None
                    : s.Options.Legend
            };

            var series = new List<ValueSeries>
            {
                new("Lift in decile", [.. s.Deciles.Select(d => Round(d.DecileLift))])
            };

            if (s.ShowCumulative)
            {
                series.Add(new ValueSeries("Cumulative lift",
                    [.. s.Deciles.Select(d => Round(d.CumulativeLift))]));
            }

            var spec = new BarChartSpec
            {
                Options = options,
                Categories = [.. s.Deciles.Select(d => d.Decile.ToString(CultureInfo.InvariantCulture))],
                Series = series,
                ShowValues = s is { ShowValues: true, Deciles.Count: <= 10 },
                ValueFormat = v => NumberFormat.Value(v),
                Lines = [new ValueLine(1, "No lift (base rate)")]
            };

            return ChartRun.Execute(options, id => BarRenderer.Render(spec, id));
        }

        private static double Round(double value)
        {
            return double.IsFinite(value) ? Math.Round(value, 3) : 0;
        }
    }
}