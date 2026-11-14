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
    public static class DwellCalculator
    {
        public static List<StatusDwell> ByStatus(IReadOnlyCollection<StayFact> stays, DateTime now,
            IReadOnlyDictionary<Guid, int?> maxDwellMinutes)
        {
            double Minutes(StayFact s) => Math.Max(0, ((s.ExitedDate ?? now) - s.EnteredDate).TotalMinutes);

            var total = stays.Sum(Minutes);

            return stays.GroupBy(s => s.CaseWorkflowStatusGuid).Select(g =>
            {
                var completed = g.Where(s => s.ExitedDate.HasValue).Select(Minutes).ToList();
                var current = g.Where(s => !s.ExitedDate.HasValue).Select(Minutes).ToList();
                var sum = g.Sum(Minutes);
                var max = maxDwellMinutes.TryGetValue(g.Key, out var m) ? m : null;

                return new StatusDwell(g.Key, g.Count(), current.Count, DistributionStats.Of(completed),
                    DistributionStats.Of(current), sum, total > 0 ? sum / total : 0, max,
                    max is > 0 ? g.Count(s => Minutes(s) > max.Value) : 0);
            }).OrderByDescending(d => d.TotalMinutes).ToList();
        }
    }
}
