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
using Jube.Charting.Exceptions;

namespace Jube.Charting.Charts
{
    public sealed class TimeStripChartBuilder : ChartBuilder<TimeStripChartBuilder>
    {
        private readonly List<TimeStripBin> bins = [];
        private readonly List<DateTimeOffset> stamps = [];
        private int binCount = 24;
        private string name = "Events";

        public TimeStripChartBuilder()
        {
            Options = new ChartOptions
                { Width = 640, Height = 100, TransparentBackground = true, Legend = LegendPosition.None };
        }

        public TimeStripChartBuilder Bins(IEnumerable<(DateTimeOffset At, int Count)> counted, TimeSpan? width = null)
        {
            var list = counted.OrderBy(b => b.At).ToList();

            for (var i = 0; i < list.Count; i++)
            {
                var span = width ?? (i + 1 < list.Count ? list[i + 1].At - list[i].At
                    : i > 0 ? list[i].At - list[i - 1].At : TimeSpan.FromHours(1));

                bins.Add(new TimeStripBin(list[i].At, list[i].At + span, list[i].Count));
            }

            return this;
        }

        public TimeStripChartBuilder Times(IEnumerable<DateTimeOffset> timestamps, int count = 24)
        {
            stamps.AddRange(timestamps);
            binCount = count;

            return this;
        }

        public TimeStripChartBuilder Name(string seriesName)
        {
            name = seriesName;

            return this;
        }

        public TimeStripChart Build()
        {
            var all = new List<TimeStripBin>(bins);

            if (stamps.Count > 0)
            {
                all.AddRange(Binned(stamps, binCount));
            }

            return new TimeStripChart(new TimeStripChartSpec { Options = Options, Bins = [.. all], Name = name });
        }

        public override string Render()
        {
            return Build().Render();
        }

        public static IReadOnlyList<TimeStripBin> Binned(IReadOnlyList<DateTimeOffset> timestamps, int count)
        {
            if (count is < 1 or > 2000)
            {
                throw new ChartSpecificationException("A time strip needs between 1 and 2000 bins.");
            }

            ChartLimits.Points(timestamps.Count);
            var min = timestamps.Min();
            var max = timestamps.Max();
            var span = max - min;

            if (span <= TimeSpan.Zero)
            {
                span = TimeSpan.FromHours(1);
            }

            var width = TimeSpan.FromTicks(Math.Max(1, (long)Math.Ceiling(span.Ticks / (double)count)));
            var counts = new int[count];

            foreach (var stamp in timestamps)
            {
                var index = (int)Math.Min(count - 1, (stamp - min).Ticks / width.Ticks);
                counts[index]++;
            }

            return [.. Enumerable.Range(0, count).Select(i => new TimeStripBin(min + width * i, min + width * (i + 1), counts[i]))];
        }
    }
}
