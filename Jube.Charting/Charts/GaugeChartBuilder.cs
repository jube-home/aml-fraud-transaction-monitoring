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
    public sealed class GaugeChartBuilder : ChartBuilder<GaugeChartBuilder>
    {
        private readonly List<GaugeRange> ranges = [];
        private Func<double, string>? format;
        private string? label;
        private double max = 100;
        private double min;
        private double? target;
        private double value;

        public GaugeChartBuilder()
        {
            Options = new ChartOptions { Width = 320, Height = 260 };
        }

        public GaugeChartBuilder Value(double amount, string? caption = null)
        {
            value = amount;
            label = caption ?? label;

            return this;
        }

        public GaugeChartBuilder Range(double minimum, double maximum)
        {
            min = minimum;
            max = maximum;

            return this;
        }

        public GaugeChartBuilder Target(double targetValue)
        {
            target = targetValue;

            return this;
        }

        public GaugeChartBuilder Band(double from, double to, string? rangeLabel = null)
        {
            ranges.Add(new GaugeRange(from, to, rangeLabel));

            return this;
        }

        public GaugeChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public GaugeChart Build()
        {
            return new GaugeChart(new GaugeChartSpec
            {
                Options = Options,
                Value = value,
                Min = min,
                Max = max,
                Target = target,
                Label = label,
                Ranges = [.. ranges],
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}