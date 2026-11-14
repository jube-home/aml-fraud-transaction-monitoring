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

using Jube.Dto.Options;
using Jube.Service.Query.CaseChecklist.Models;

namespace Jube.Service.Query.CaseChecklist
{
    public static class CaseChecklistReconcilePlan
    {
        public static List<ChecklistPlanOperation> Compute(Guid currentStatusGuid,
            IReadOnlyList<ChecklistPlanDefinition> definitions, IReadOnlyList<ChecklistPlanExisting> existing,
            DateTime now)
        {
            var operations = new List<ChecklistPlanOperation>();
            var byItem = existing.ToDictionary(e => e.ItemGuid);
            var defined = new HashSet<Guid>();

            foreach (var definition in definitions)
            {
                if (!defined.Add(definition.ItemGuid))
                {
                    continue;
                }

                if (!byItem.TryGetValue(definition.ItemGuid, out var row))
                {
                    if (definition.Show == ChecklistShowOutcome.Shown)
                    {
                        operations.Add(new ChecklistPlanOperation(ChecklistPlanOperationKind.Insert,
                            definition.ItemGuid, null, definition, definition.ItemName, now,
                            definition.SlaMinutes.HasValue ? now.AddMinutes(definition.SlaMinutes.Value) : null));
                    }

                    continue;
                }

                switch (row.State)
                {
                    case CaseChecklistItemState.Shown when definition.Show == ChecklistShowOutcome.NotShown:
                        operations.Add(new ChecklistPlanOperation(ChecklistPlanOperationKind.Hide, row.ItemGuid,
                            row.Id, definition, row.ItemName, null, null));
                        break;
                    case CaseChecklistItemState.Hidden when definition.Show == ChecklistShowOutcome.Shown:
                        operations.Add(new ChecklistPlanOperation(ChecklistPlanOperationKind.Show, row.ItemGuid,
                            row.Id, definition, row.ItemName, now, null));
                        break;
                }
            }

            foreach (var row in existing)
            {
                if (row.StatusGuid == currentStatusGuid
                    && row.State == CaseChecklistItemState.Shown
                    && !defined.Contains(row.ItemGuid))
                {
                    operations.Add(new ChecklistPlanOperation(ChecklistPlanOperationKind.Hide, row.ItemGuid,
                        row.Id, null, row.ItemName, null, null));
                }
            }

            return operations;
        }
    }
}
