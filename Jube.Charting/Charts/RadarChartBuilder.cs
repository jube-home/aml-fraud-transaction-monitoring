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
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class RadarChartBuilder : ChartBuilder<RadarChartBuilder>
    {
        private readonly List<ValueSeries> series = [];
        private string[] axes = [];
        private bool filled = true;
        private Func<double, string>? format;
        private double? max;
        private double? min;

        public RadarChartBuilder Axes(IEnumerable<string> axisNames)
        {
            axes = [.. axisNames];

            return this;
        }

        public RadarChartBuilder Series(string name, IEnumerable<double> values, string? color = null)
        {
            series.Add(new ValueSeries(name, [.. values], color));

            return this;
        }

        public RadarChartBuilder Filled(bool enabled = true)
        {
            filled = enabled;

            return this;
        }

        public RadarChartBuilder Range(double minimum, double maximum)
        {
            min = minimum;
            max = maximum;

            return this;
        }

        public RadarChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public RadarChart Build()
        {
            return new RadarChart(new RadarChartSpec
            {
                Options = Options,
                Axes = axes,
                Series = [.. series],
                Filled = filled,
                Min = min,
                Max = max,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}