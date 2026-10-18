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
using Jube.Charting.Scales;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class StreamChartBuilder : ChartBuilder<StreamChartBuilder>
    {
        private readonly List<ValueSeries> series = [];
        private XAxisKind axis = XAxisKind.Linear;
        private Func<double, string>? format;
        private StreamOffset offset = StreamOffset.Wiggle;
        private StreamOrder order = StreamOrder.InsideOut;
        private bool smooth = true;
        private IReadOnlyList<double> x = [];
        private Func<double, string>? xFormat;

        public StreamChartBuilder X(IEnumerable<double> xValues)
        {
            x = [.. xValues];

            return this;
        }

        public StreamChartBuilder Times(IEnumerable<DateTime> times)
        {
            x = [.. times.Select(TimeScale.ToSeconds)];
            axis = XAxisKind.Time;

            return this;
        }

        public StreamChartBuilder Series(string name, IEnumerable<double> values, string? color = null)
        {
            series.Add(new ValueSeries(name, [.. values], color));

            return this;
        }

        public StreamChartBuilder Offset(StreamOffset offsetMode)
        {
            offset = offsetMode;

            return this;
        }

        public StreamChartBuilder Order(StreamOrder orderMode)
        {
            order = orderMode;

            return this;
        }

        public StreamChartBuilder Smooth(bool enabled = true)
        {
            smooth = enabled;

            return this;
        }

        public StreamChartBuilder XFormat(Func<double, string> formatter)
        {
            xFormat = formatter;

            return this;
        }

        public StreamChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public StreamChart Build()
        {
            return new StreamChart(new StreamChartSpec
            {
                Options = Options,
                X = x,
                Series = [.. series],
                XAxis = axis,
                Offset = offset,
                Order = order,
                Smooth = smooth,
                XFormat = xFormat,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}