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
    public static class ChecklistSlaCalculator
    {
        public static ChecklistSlaReport Compute(IReadOnlyCollection<ChecklistItemFact> facts, DateTime now)
        {
            var items = facts.GroupBy(f => f.ItemGuid).Select(g => Item(g.ToList(), now))
                .OrderByDescending(i => i.Breached).ThenByDescending(i => i.CompletedLate)
                .ThenBy(i => i.ChecklistName, StringComparer.Ordinal).ThenBy(i => i.ItemName, StringComparer.Ordinal)
                .ToList();

            var within = items.Sum(i => i.CompletedWithinSla);
            var late = items.Sum(i => i.CompletedLate);

            return new ChecklistSlaReport(items, items.Sum(i => i.Shown), items.Sum(i => i.Completed), within, late,
                items.Sum(i => i.Breached), Attainment(within, late));
        }

        private static ChecklistSlaItem Item(List<ChecklistItemFact> group, DateTime now)
        {
            var latest = group.OrderByDescending(o => o.FirstShownDate).First();

            var completed = group.Where(f => f.Done && f.CompletedDate.HasValue).ToList();
            var timed = completed.Where(f => f.DueDate.HasValue).ToList();
            var within = timed.Count(f => f.CompletedDate!.Value <= f.DueDate!.Value);
            var late = timed.Count - within;

            var minutes = completed.Select(f => Math.Max(0, (f.CompletedDate!.Value - f.FirstShownDate).TotalMinutes))
                .ToList();
            var distribution = DistributionStats.Of(minutes);

            var open = group.Where(f => !f.Done && !f.Hidden).ToList();

            var breached = open.Count(f => f.SlaBreachedDate.HasValue || (f.DueDate.HasValue && now > f.DueDate.Value));

            var target = latest.DueDate.HasValue
                ? (int?)Math.Round((latest.DueDate.Value - latest.FirstShownDate).TotalMinutes)
                : null;

            return new ChecklistSlaItem(latest.ItemGuid, latest.ChecklistGuid, latest.ChecklistName, latest.ItemName,
                target, group.Count, group.Count(f => f.Hidden), completed.Count, within, late, open.Count, breached,
                distribution.Count > 0 ? distribution.P50 : null, distribution.Count > 0 ? distribution.P90 : null,
                Attainment(within, late));
        }

        private static double? Attainment(int within, int late) =>
            within + late > 0 ? (double)within / (within + late) : null;
    }
}
