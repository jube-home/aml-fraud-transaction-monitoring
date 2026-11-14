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
using Jube.Data.Poco;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSummary;
using Jube.Service.Query.CaseSearch;

namespace Jube.Service.Query.CaseSummary
{
    internal static class CaseChecklistSummaryBuilder
    {
        public static CaseChecklistSummaryDto Build(IEnumerable<CaseChecklistItem> rows, Guid currentStatusGuid,
            DateTime now)
        {
            var visible = rows.Where(w => w.CaseWorkflowStatusGuid == currentStatusGuid
                                          && w.StateId != (byte)CaseChecklistItemState.Hidden).ToList();

            var open = visible.Where(w => w.StateId == (byte)CaseChecklistItemState.Shown).ToList();
            var done = visible.Count(w => w.StateId == (byte)CaseChecklistItemState.Done);

            var next = open.Where(w => w.DueDate.HasValue).Select(w => w.DueDate!.Value).OrderBy(o => o)
                .Cast<DateTime?>().FirstOrDefault();

            return new CaseChecklistSummaryDto
            {
                Required = open.Count + done,
                Done = done,
                Open = open.Count,
                Breached = open.Count(w => w.SlaBreachedDate.HasValue || (w.DueDate.HasValue && now > w.DueDate.Value)),
                NextDueDate = CaseSearchMapper.Utc(next),
                Complete = open.Count == 0
            };
        }
    }
}
