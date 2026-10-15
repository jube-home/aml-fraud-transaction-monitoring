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

using Jube.Charting.Core;
using Jube.Charting.More.Models;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class SlopeChartBuilder : ChartBuilder<SlopeChartBuilder>
    {
        private readonly List<ValueSeries> series = [];
        private Func<double, string>? format;
        private bool lowestIsBest;
        private SlopeMode mode;
        private IReadOnlyList<string> periods = [];
        private bool smooth;

        public SlopeChartBuilder Mode(SlopeMode slopeMode)
        {
            mode = slopeMode;

            return this;
        }

        public SlopeChartBuilder Periods(IEnumerable<string> periodLabels)
        {
            periods = [.. periodLabels];

            return this;
        }

        public SlopeChartBuilder Series(string name, IEnumerable<double> values, string? color = null)
        {
            series.Add(new ValueSeries(name, [.. values], color));

            return this;
        }

        public SlopeChartBuilder Smooth(bool enabled = true)
        {
            smooth = enabled;

            return this;
        }

        public SlopeChartBuilder LowestIsBest(bool lowest = true)
        {
            lowestIsBest = lowest;

            return this;
        }

        public SlopeChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public SlopeChart Build()
        {
            return new SlopeChart(new SlopeChartSpec
            {
                Options = Options,
                Periods = periods,
                Series = [.. series],
                Mode = mode,
                Smooth = smooth,
                LowestIsBest = lowestIsBest,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}