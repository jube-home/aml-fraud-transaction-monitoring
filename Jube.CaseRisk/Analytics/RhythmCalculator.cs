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

namespace Jube.CaseRisk.Analytics
{
    public static class RhythmCalculator
    {
        public static RhythmReport Compute(IReadOnlyCollection<CaseFact> facts)
        {
            var grid = Enumerable.Range(0, 7).Select(_ => new int[24]).ToArray();
            var closures = new int[24];

            foreach (var fact in facts)
            {
                grid[((int)fact.CreatedDate.DayOfWeek + 6) % 7][fact.CreatedDate.Hour]++;

                if (fact.ClosedDate.HasValue)
                {
                    closures[fact.ClosedDate.Value.Hour]++;
                }
            }

            var median = new double?[24];

            for (var hour = 0; hour < 24; hour++)
            {
                var waits = facts.Where(f => f.CreatedDate.Hour == hour && f.FirstViewedDate.HasValue)
                    .Select(f => Math.Max(0, (f.FirstViewedDate!.Value - f.CreatedDate).TotalMinutes)).ToList();

                median[hour] = waits.Count > 0 ? DistributionStats.Of(waits).P50 : null;
            }

            var busiest = (Weekday: 0, Hour: 0, Count: -1);

            for (var d = 0; d < 7; d++)
            {
                for (var h = 0; h < 24; h++)
                {
                    if (grid[d][h] > busiest.Count)
                    {
                        busiest = (d, h, grid[d][h]);
                    }
                }
            }

            return new RhythmReport(grid, median, closures, busiest.Weekday, busiest.Hour);
        }
    }
}
