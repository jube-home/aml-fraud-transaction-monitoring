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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal static class SurvivalGuards
    {
        public static void Clean(double[] times, int[] events, out double[] t, out int[] e, out int skipped)
        {
            CategoricalGuards.Required(times, nameof(times));
            CategoricalGuards.BinaryLabels(events, nameof(events));
            CategoricalGuards.SameLength(times.Length, events.Length, nameof(times), nameof(events));
            var keep = new List<int>();

            for (var i = 0; i < times.Length; i++)
            {
                if (double.IsNaN(times[i]))
                {
                    continue;
                }

                if (double.IsInfinity(times[i]) || times[i] < 0d)
                {
                    throw new ArgumentException(
                        $"times must be finite and not negative (time measured from entry); entry {i} is {times[i]}.",
                        nameof(times));
                }

                keep.Add(i);
            }

            if (keep.Count == 0)
            {
                throw new ArgumentException("There are no usable rows: the input is empty or every time is NaN.",
                    nameof(times));
            }

            skipped = times.Length - keep.Count;
            t = keep.Select(i => times[i]).ToArray();
            e = keep.Select(i => events[i]).ToArray();
        }

        public static void CleanGrouped(double[] times, int[] events, string[] groups, out double[] t, out int[] e,
            out string[] g, out int skipped)
        {
            CategoricalGuards.Required(groups, nameof(groups));
            CategoricalGuards.Required(times, nameof(times));
            CategoricalGuards.SameLength(times.Length, groups.Length, nameof(times), nameof(groups));
            CategoricalGuards.BinaryLabels(events, nameof(events));
            CategoricalGuards.SameLength(times.Length, events.Length, nameof(times), nameof(events));
            var keep = new List<int>();

            for (var i = 0; i < times.Length; i++)
            {
                if (double.IsNaN(times[i]) || string.IsNullOrWhiteSpace(groups[i]))
                {
                    continue;
                }

                if (double.IsInfinity(times[i]) || times[i] < 0d)
                {
                    throw new ArgumentException(
                        $"times must be finite and not negative (time measured from entry); entry {i} is {times[i]}.",
                        nameof(times));
                }

                keep.Add(i);
            }

            if (keep.Count == 0)
            {
                throw new ArgumentException(
                    "There are no usable rows: every time is NaN or every group label is blank.", nameof(times));
            }

            skipped = times.Length - keep.Count;
            t = keep.Select(i => times[i]).ToArray();
            e = keep.Select(i => events[i]).ToArray();
            g = keep.Select(i => groups[i]).ToArray();
        }
    }
}