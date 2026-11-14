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
namespace Jube.Data.Query.GetCaseNarrativeCandidatesQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dto.Options;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetCaseNarrativeCandidatesQuery(DbContext dbContext)
    {
        public Task<List<GetCaseNarrativeCandidatesQueryDto>> ExecuteAsync(int tenantRegistryId, int skip, int take,
            CancellationToken token = default)
        {
            var narrative = (int)CaseNoteKind.Narrative;

            return (from c in dbContext.Case
                from w in dbContext.CaseWorkflow.InnerJoin(w => w.Guid == c.CaseWorkflowGuid)
                from m in dbContext.EntityAnalysisModel.InnerJoin(m => m.Id == w.EntityAnalysisModelId)
                where (c.ClosedStatusId == 0 || c.ClosedStatusId == 1)
                      && c.CaseKey != null && c.CaseKeyValue != null
                      && w.EnableNarrative == 1 && (w.Deleted == 0 || w.Deleted == null)
                      && (m.Deleted == 0 || m.Deleted == null) && m.TenantRegistryId == tenantRegistryId
                orderby dbContext.CaseNote.Where(n => n.CaseId == c.Id && n.KindId == narrative)
                    .Max(n => n.CreatedDate) ?? System.DateTime.MinValue, c.Id
                select new GetCaseNarrativeCandidatesQueryDto
                {
                    CaseId = c.Id,
                    CaseKey = c.CaseKey,
                    CaseKeyValue = c.CaseKeyValue,
                    CaseWorkflowGuid = c.CaseWorkflowGuid,
                    CaseWorkflowStatusGuid = c.CaseWorkflowStatusGuid,
                    EntityAnalysisModelId = m.Id,
                    TenantRegistryId = m.TenantRegistryId.GetValueOrDefault()
                }).Skip(skip).Take(take).ToListAsync(token);
        }
    }
}
