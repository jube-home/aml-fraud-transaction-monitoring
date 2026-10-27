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


namespace Jube.CaseRisk.Journal
{
    public sealed record Burst(double WindowHours, int Max, DateTime At, double Median, double Ratio);

    public sealed record VelocityReport(IReadOnlyList<IReadOnlyList<int>> HourByWeekday, IReadOnlyList<Burst> Bursts);

    public static class VelocityCalculator
    {
        public static readonly double[] WindowHours = [1, 24, 168];

        public static VelocityReport Compute(IReadOnlyList<JournalEvent> events)
        {
            var times = events.Select(e => DateTime.SpecifyKind(e.At, DateTimeKind.Utc)).OrderBy(at => at).ToArray();
            var grid = Enumerable.Range(0, 7).Select(_ => new int[24]).ToArray();

            foreach (var at in times)
            {
                grid[((int)at.DayOfWeek + 6) % 7][at.Hour]++;
            }

            var bursts = new List<Burst>();

            if (times.Length > 0)
            {
                foreach (var hours in WindowHours)
                {
                    bursts.Add(Slide(times, hours));
                }
            }

            return new VelocityReport(grid.Select(row => (IReadOnlyList<int>)row.ToList()).ToList(), bursts);
        }

        private static Burst Slide(DateTime[] times, double hours)
        {
            var window = TimeSpan.FromHours(hours);
            var counts = new int[times.Length];
            var end = 0;

            for (var start = 0; start < times.Length; start++)
            {
                end = Math.Max(end, start);

                while (end < times.Length && times[end] - times[start] < window)
                {
                    end++;
                }

                counts[start] = end - start;
            }

            var maximum = 0;
            var at = times[0];

            for (var index = 0; index < counts.Length; index++)
            {
                if (counts[index] > maximum)
                {
                    maximum = counts[index];
                    at = times[index];
                }
            }

            var median = RobustStatistics.Quantile(RobustStatistics.Sorted(counts.Select(count => (double)count)), 0.5);

            return new Burst(hours, maximum, at, median, median > 0 ? maximum / median : 0);
        }
    }
}
