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
    public static class FlowCalculator
    {
        public static List<FlowDay> Daily(IReadOnlyCollection<CaseFact> facts, IReadOnlyCollection<StayFact> stays,
            DateTime fromDate, DateTime toDate)
        {
            var days = new List<FlowDay>();

            for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
            {
                var start = day;
                var end = day.AddDays(1);

                var opened = facts.Count(f => f.CreatedDate >= start && f.CreatedDate < end);
                var closed = facts.Count(f => f.ClosedDate.HasValue && f.ClosedDate.Value >= start
                                                                     && f.ClosedDate.Value < end);
                var backlog = facts.Count(f => f.CreatedDate < end && (!f.ClosedDate.HasValue || f.ClosedDate.Value >= end));

                var byStatus = stays
                    .Where(s => s.EnteredDate < end && (!s.ExitedDate.HasValue || s.ExitedDate.Value >= end))
                    .GroupBy(s => s.CaseWorkflowStatusGuid)
                    .ToDictionary(g => g.Key, g => g.Select(s => s.CaseId).Distinct().Count());

                days.Add(new FlowDay(day, opened, closed, backlog, byStatus));
            }

            return days;
        }
    }
}
