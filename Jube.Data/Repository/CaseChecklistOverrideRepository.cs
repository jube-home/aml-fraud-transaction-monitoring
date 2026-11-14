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

    public class CaseChecklistOverrideRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public CaseChecklistOverrideRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        private IQueryable<CaseChecklistOverride> Visible()
        {
            return from o in dbContext.CaseChecklistOverride
                join c in dbContext.Case on o.CaseId equals c.Id
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                where (w.Deleted == 0 || w.Deleted == null) && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                select o;
        }

        public Task<CaseChecklistOverride> GetOpenAsync(int caseId, Guid caseWorkflowStatusGuid,
            CancellationToken token = default)
        {
            return Visible().Where(w => w.CaseId == caseId && w.CaseWorkflowStatusGuid == caseWorkflowStatusGuid
                                                          && w.ConsumedDate == null)
                .FirstOrDefaultAsync(token);
        }

        public Task<List<CaseChecklistOverride>> ListForCaseAsync(int caseId, CancellationToken token = default)
        {
            return Visible().Where(w => w.CaseId == caseId).OrderBy(o => o.GrantedDate).ThenBy(o => o.Id)
                .ToListAsync(token);
        }

        public async Task<CaseChecklistOverride> InsertAsync(CaseChecklistOverride model,
            CancellationToken token = default)
        {
            var inTenant = await (from c in dbContext.Case
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                where c.Id == model.CaseId && (w.Deleted == 0 || w.Deleted == null)
                      && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                select c.Id).AnyAsync(token).ConfigureAwait(false);

            if (!inTenant)
            {
                throw new InvalidOperationException("The case is not in the tenant of the repository.");
            }

            model.GrantedDate = model.GrantedDate == default ? DateTime.UtcNow : model.GrantedDate;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task<bool> ConsumeAsync(long id, string consumedUser, DateTime consumedDate,
            CancellationToken token = default)
        {
            var changed = await Visible().Where(w => w.Id == id && w.ConsumedDate == null)
                .Set(s => s.ConsumedDate, consumedDate).Set(s => s.ConsumedUser, consumedUser)
                .UpdateAsync(token).ConfigureAwait(false);

            return changed > 0;
        }
    }
}
