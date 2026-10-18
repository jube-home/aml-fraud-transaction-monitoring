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
using Jube.Charting.Exceptions;
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public sealed class CalendarChartBuilder : ChartBuilder<CalendarChartBuilder>
    {
        private readonly List<CalendarValue> days = [];
        private CalendarAggregate aggregate;
        private Func<double, string>? format;
        private double? max;
        private double? min;
        private string name = "Value";
        private bool sunday;

        public CalendarChartBuilder Day(DateTime date, double value)
        {
            days.Add(new CalendarValue(date, value));

            return this;
        }

        public CalendarChartBuilder Days(IEnumerable<DateTime> dates, IEnumerable<double> values)
        {
            var d = dates.ToArray();
            var v = values.ToArray();

            if (d.Length != v.Length)
            {
                throw new ChartSpecificationException("Dates and values must have the same length.");
            }

            for (var i = 0; i < d.Length; i++)
            {
                days.Add(new CalendarValue(d[i], v[i]));
            }

            return this;
        }

        public CalendarChartBuilder Aggregate(CalendarAggregate aggregateMode)
        {
            aggregate = aggregateMode;

            return this;
        }

        public CalendarChartBuilder WeekStartsOnSunday(bool startOnSunday = true)
        {
            sunday = startOnSunday;

            return this;
        }

        public CalendarChartBuilder Range(double minimum, double maximum)
        {
            min = minimum;
            max = maximum;

            return this;
        }

        public CalendarChartBuilder ValueName(string valueName)
        {
            name = valueName;

            return this;
        }

        public CalendarChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public CalendarChart Build()
        {
            return new CalendarChart(new CalendarChartSpec
            {
                Options = Options,
                Days = [.. days],
                Aggregate = aggregate,
                WeekStartsOnSunday = sunday,
                Min = min,
                Max = max,
                ValueFormat = format,
                ValueName = name
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}