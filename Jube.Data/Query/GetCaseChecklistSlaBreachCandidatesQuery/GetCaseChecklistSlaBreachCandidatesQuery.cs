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

namespace Jube.Data.Query.GetCaseChecklistSlaBreachCandidatesQuery
{
    public class GetCaseChecklistSlaBreachCandidatesQuery(DbContext dbContext)
    {
        private const int ClosedStatusId = 3;

        public Task<List<GetCaseChecklistSlaBreachCandidatesQueryDto>> ExecuteAsync(DateTime now, int batchSize,
            CancellationToken token = default)
        {
            return (from i in dbContext.CaseChecklistItem
                join c in dbContext.Case on i.CaseId equals c.Id
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                where i.StateId == 1
                      && (w.Deleted == 0 || w.Deleted == null)
                      && i.SlaBreachedDate == null
                      && i.DueDate != null
                      && i.DueDate < now
                      && c.ClosedStatusId != ClosedStatusId
                orderby i.DueDate, i.Id
                select new GetCaseChecklistSlaBreachCandidatesQueryDto
                {
                    ItemId = i.Id,
                    CaseId = i.CaseId,
                    TenantRegistryId = w.EntityAnalysisModel.TenantRegistryId ?? 0,
                    DueDate = i.DueDate.Value
                }).Take(batchSize).ToListAsync(token);
        }
    }
}
