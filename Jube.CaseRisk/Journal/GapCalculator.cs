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
    public sealed record GapBin(string Label, double FromMinutes, double? ToMinutes, int Count);

    public sealed record Dormancy(double Hours, DateTime From, DateTime To, int EventsInNextDay);

    public sealed record GapReport(IReadOnlyList<GapBin> Bins, double MedianHours, Dormancy? Longest);

    public static class GapCalculator
    {
        public static readonly double[] EdgesMinutes = [1, 5, 15, 60, 360, 1440, 4320, 10080, 43200];

        public static GapReport Compute(IReadOnlyList<JournalEvent> events)
        {
            var times = events.Select(e => DateTime.SpecifyKind(e.At, DateTimeKind.Utc)).OrderBy(at => at).ToArray();
            var gaps = new List<double>();

            for (var index = 1; index < times.Length; index++)
            {
                gaps.Add((times[index] - times[index - 1]).TotalMinutes);
            }

            var counts = new int[EdgesMinutes.Length + 1];

            foreach (var gap in gaps)
            {
                var slot = 0;

                while (slot < EdgesMinutes.Length && gap >= EdgesMinutes[slot])
                {
                    slot++;
                }

                counts[slot]++;
            }

            var bins = Enumerable.Range(0, counts.Length).Select(slot => new GapBin(Label(slot),
                slot == 0 ? 0 : EdgesMinutes[slot - 1], slot == EdgesMinutes.Length ? null : EdgesMinutes[slot],
                counts[slot])).ToList();

            Dormancy? longest = null;

            if (gaps.Count > 0)
            {
                var index = gaps.IndexOf(gaps.Max()) + 1;
                var from = times[index - 1];
                var to = times[index];

                longest = new Dormancy(gaps.Max() / 60, from, to, times.Count(at => at >= to && at < to.AddDays(1)));
            }

            return new GapReport(bins, gaps.Count == 0
                ? 0
                : RobustStatistics.Quantile(RobustStatistics.Sorted(gaps), 0.5) / 60, longest);
        }

        public static string Label(int slot)
        {
            return slot == 0 ? "< " + Text(EdgesMinutes[0])
                : slot == EdgesMinutes.Length ? Text(EdgesMinutes[^1]) + "+"
                : Text(EdgesMinutes[slot - 1]) + " – " + Text(EdgesMinutes[slot]);
        }

        private static string Text(double minutes) => minutes switch
        {
            < 60 => (int)minutes + " min",
            < 1440 => Math.Round(minutes / 60, 1).ToString("0.#") + " h",
            _ => Math.Round(minutes / 1440, 1).ToString("0.#") + " d"
        };
    }
}
