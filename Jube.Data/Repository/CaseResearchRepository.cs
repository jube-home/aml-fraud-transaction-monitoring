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
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;

    public class CaseResearchRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public CaseResearchRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public Task<List<CaseResearch>> GetByCaseKeyValueAsync(string key, string value,
            CancellationToken token = default)
        {
            return dbContext.CaseResearch.Where(w
                => tenantRegistryId.HasValue
                   && w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                   && (w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                       w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                   && (w.Case.CaseWorkflow.Deleted == 0 || w.Case.CaseWorkflow.Deleted == null)
                   && (w.Case.CaseWorkflowStatus.Deleted == 0 || w.Case.CaseWorkflowStatus.Deleted == null)
                   && w.CaseKey == key && w.CaseKeyValue == value
                   && dbContext.CaseWorkflowRole
                       .Where(r => r.CaseWorkflowGuid == w.Case.CaseWorkflow.Guid
                                   && (r.Deleted == 0 || r.Deleted == null))
                       .Any(r => dbContext.RoleRegistry
                           .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                           .Any(rr => dbContext.UserRegistry
                               .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                   && dbContext.CaseWorkflowStatusRole
                       .Where(r => r.CaseWorkflowStatusGuid == w.Case.CaseWorkflowStatus.Guid
                                   && (r.Deleted == 0 || r.Deleted == null))
                       .Any(r => dbContext.RoleRegistry
                           .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                           .Any(rr => dbContext.UserRegistry
                               .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
            ).OrderByDescending(o => o.Id).ToListAsync(token);
        }

        public Task<CaseResearch> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.CaseResearch.FirstOrDefaultAsync(w => tenantRegistryId.HasValue
                                                          && w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId ==
                                                          tenantRegistryId && w.Id == id, token);
        }

        public Task<List<CaseResearchCitation>> GetCitationsAsync(IReadOnlyCollection<int> researchIds,
            CancellationToken token = default)
        {
            return dbContext.CaseResearchCitation.Where(w => researchIds.Contains(w.CaseResearchId))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<CaseResearch> InsertAsync(CaseResearch model, IReadOnlyCollection<CaseResearchCitation> citations,
            CancellationToken token = default)
        {
            await using var transaction = await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            foreach (var citation in citations)
            {
                citation.CaseResearchId = model.Id;
                citation.CreatedUser = userName;
                citation.CreatedDate = model.CreatedDate;
                citation.Id = await dbContext.InsertWithInt32IdentityAsync(citation, token: token).ConfigureAwait(false);
            }

            await transaction.CommitAsync(token).ConfigureAwait(false);

            return model;
        }
    }
}
