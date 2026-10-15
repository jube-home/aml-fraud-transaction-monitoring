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
using Jube.Charting.Scales;

namespace Jube.Charting.Ticks
{
    public static class TimeTicks
    {
        private static readonly double[] secondSteps =
        [
            1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600, 7200, 10800, 21600, 43200, 86400, 172800, 604800
        ];

        private static readonly int[] monthSteps = [1, 3, 6];
        private static readonly int[] yearSteps = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000];

        public static IReadOnlyList<(double Seconds, string Label)> Generate(DateTime min, DateTime max, int target = 6)
        {
            var lo = TimeScale.ToSeconds(min);
            var hi = TimeScale.ToSeconds(max);

            if (lo > hi)
            {
                (lo, hi) = (hi, lo);
            }

            target = Math.Clamp(target, 2, 20);
            var span = Math.Max(hi - lo, 1);
            var ideal = span / target;

            var result = new List<(double, string)>();
            var bestSecond = secondSteps.OrderBy(s => Math.Abs(Math.Log(s / ideal))).First();
            var monthSeconds = 2_629_800.0;
            var yearSeconds = 31_557_600.0;

            var candidates = new List<(double Seconds, int Kind, int Step)>
            {
                (bestSecond, 0, (int)bestSecond)
            };

            foreach (var m in monthSteps)
            {
                candidates.Add((m * monthSeconds, 1, m));
            }

            foreach (var y in yearSteps)
            {
                candidates.Add((y * yearSeconds, 2, y));
            }

            var choice = candidates.OrderBy(c => Math.Abs(Math.Log(c.Seconds / ideal))).First();

            if (choice.Kind == 0)
            {
                var step = choice.Step;
                var format = step < 60 ? "HH:mm:ss" : step < 86400 ? "HH:mm" : "MMM d";
                var start = Math.Ceiling(lo / step) * step;

                for (var t = start; t <= hi + 1e-6 && result.Count < 200; t += step)
                {
                    result.Add((t, TimeScale.FromSeconds(t).ToString(format, CultureInfo.InvariantCulture)));
                }
            }
            else if (choice.Kind == 1)
            {
                var current = TimeScale.FromSeconds(lo);
                var monthIndex = current.Year * 12 + current.Month - 1;
                monthIndex = (int)Math.Ceiling(monthIndex / (double)choice.Step) * choice.Step;

                for (var i = 0; i < 200; i++, monthIndex += choice.Step)
                {
                    var date = new DateTime(monthIndex / 12, monthIndex % 12 + 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var seconds = TimeScale.ToSeconds(date);

                    if (seconds < lo)
                    {
                        continue;
                    }

                    if (seconds > hi)
                    {
                        break;
                    }

                    result.Add((seconds, date.ToString("MMM yyyy", CultureInfo.InvariantCulture)));
                }
            }
            else
            {
                var year = (int)Math.Ceiling(TimeScale.FromSeconds(lo).Year / (double)choice.Step) * choice.Step;

                for (var i = 0; i < 200 && year < 9999; i++, year += choice.Step)
                {
                    var date = new DateTime(Math.Max(year, 1), 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var seconds = TimeScale.ToSeconds(date);

                    if (seconds < lo)
                    {
                        continue;
                    }

                    if (seconds > hi)
                    {
                        break;
                    }

                    result.Add((seconds, date.ToString("yyyy", CultureInfo.InvariantCulture)));
                }
            }

            if (result.Count == 0)
            {
                result.Add((lo, TimeScale.FromSeconds(lo).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
            }

            return result;
        }
    }
}