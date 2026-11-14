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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Data.Query.GetCaseChecklistDefinitionsQuery
{
    public class GetCaseChecklistDefinitionsQuery(DbContext dbContext)
    {
        public async Task<List<GetCaseChecklistDefinitionsQueryDto>> ExecuteAsync(Guid caseWorkflowStatusGuid,
            CancellationToken token = default)
        {
            var rows = await (from c in dbContext.CaseWorkflowChecklist
                join i in dbContext.CaseWorkflowChecklistItem on c.Guid equals i.CaseWorkflowChecklistGuid
                where c.CaseWorkflowStatusGuid == caseWorkflowStatusGuid
                      && c.Active == 1
                      && (c.Deleted == 0 || c.Deleted == null)
                      && i.Active == 1
                      && (i.Deleted == 0 || i.Deleted == null)
                select new GetCaseChecklistDefinitionsQueryDto
                {
                    CaseWorkflowChecklistItemGuid = i.Guid,
                    CaseWorkflowChecklistGuid = c.Guid,
                    CaseWorkflowStatusGuid = c.CaseWorkflowStatusGuid,
                    ChecklistName = c.Name,
                    ChecklistPriority = c.Priority ?? 0,
                    ChecklistId = c.Id,
                    ItemName = i.Name,
                    ItemPriority = i.Priority ?? 0,
                    ItemId = i.Id,
                    EnableShow = i.EnableShow == 1,
                    ShowSql = i.ShowSql,
                    SlaMinutes = i.SlaMinutes
                }).ToListAsync(token).ConfigureAwait(false);

            return rows.OrderBy(o => o.ChecklistPriority).ThenBy(o => o.ChecklistName, StringComparer.Ordinal)
                .ThenBy(o => o.ChecklistId).ThenBy(o => o.ItemPriority)
                .ThenBy(o => o.ItemName, StringComparer.Ordinal).ThenBy(o => o.ItemId).ToList();
        }
    }
}
