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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dto.Options;
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;
    using Query.GetCaseNarrativeCandidatesQuery;

    public class CaseAgentRunRepository(DbContext dbContext)
    {
        public Task<List<CaseAgentRun>> ListForCaseAsync(int caseId, int take, CancellationToken token = default)
        {
            return dbContext.CaseAgentRun.Where(w => w.CaseId == caseId)
                .OrderByDescending(o => o.Id).Take(take).ToListAsync(token);
        }

        public Task<CaseAgentRun> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.CaseAgentRun.FirstOrDefaultAsync(w => w.Guid == guid, token);
        }

        public Task<string> LatestFactsHashAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseAgentRun.Where(w => w.CaseId == caseId)
                .OrderByDescending(o => o.Id).Select(s => s.FactsHash).FirstOrDefaultAsync(token);
        }

        public Task<List<GetCaseNarrativeCandidatesQueryDto>> ListCandidatesAsync(int tenantRegistryId,
            IReadOnlyCollection<Guid> workflowGuids, int take, CancellationToken token = default)
        {
            return (from c in dbContext.Case
                from w in dbContext.CaseWorkflow.InnerJoin(w => w.Guid == c.CaseWorkflowGuid)
                from m in dbContext.EntityAnalysisModel.InnerJoin(m => m.Id == w.EntityAnalysisModelId)
                where (c.ClosedStatusId == 0 || c.ClosedStatusId == 1)
                      && c.CaseKey != null && c.CaseKeyValue != null
                      && workflowGuids.Contains(w.Guid) && (w.Deleted == 0 || w.Deleted == null)
                      && (m.Deleted == 0 || m.Deleted == null) && m.TenantRegistryId == tenantRegistryId
                orderby dbContext.CaseAgentRun.Where(r => r.CaseId == c.Id).Max(r => (DateTime?)r.CreatedDate)
                        ?? DateTime.MinValue, c.Id
                select new GetCaseNarrativeCandidatesQueryDto
                {
                    CaseId = c.Id,
                    CaseKey = c.CaseKey,
                    CaseKeyValue = c.CaseKeyValue,
                    CaseWorkflowGuid = c.CaseWorkflowGuid,
                    CaseWorkflowStatusGuid = c.CaseWorkflowStatusGuid,
                    EntityAnalysisModelId = m.Id,
                    TenantRegistryId = m.TenantRegistryId.GetValueOrDefault()
                }).Take(take).ToListAsync(token);
        }

        public async Task InsertSupersedingAsync(CaseAgentRun run, CancellationToken token = default)
        {
            var pending = (short)CaseAgentRunState.Pending;
            var superseded = (short)CaseAgentRunState.Superseded;

            await dbContext.CaseAgentRun.Where(w => w.CaseId == run.CaseId && w.StateId == pending)
                .Set(s => s.StateId, superseded).UpdateAsync(token).ConfigureAwait(false);

            await dbContext.InsertAsync(run, token: token).ConfigureAwait(false);
        }

        public Task<int> DecideAsync(Guid guid, short state, string reason, string user,
            CancellationToken token = default)
        {
            var pending = (short)CaseAgentRunState.Pending;

            return dbContext.CaseAgentRun.Where(w => w.Guid == guid && w.StateId == pending)
                .Set(s => s.StateId, state).Set(s => s.OverrideReason, reason).Set(s => s.DecidedUser, user)
                .Set(s => s.DecidedDate, DateTime.UtcNow).UpdateAsync(token);
        }
    }
}
