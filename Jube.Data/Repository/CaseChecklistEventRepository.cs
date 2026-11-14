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

    public class CaseChecklistEventRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public CaseChecklistEventRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseChecklistEventRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<List<CaseChecklistEvent>> ListForCaseAsync(int caseId, CancellationToken token = default)
        {
            return (from e in dbContext.CaseChecklistEvent
                join c in dbContext.Case on e.CaseId equals c.Id
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                where e.CaseId == caseId && (w.Deleted == 0 || w.Deleted == null)
                      && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                orderby e.CreatedDate, e.Id
                select e).ToListAsync(token);
        }

        public Task<List<CaseChecklistEvent>> ListRecentForCaseAsync(int caseId, int limit,
            CancellationToken token = default)
        {
            return (from e in dbContext.CaseChecklistEvent
                join c in dbContext.Case on e.CaseId equals c.Id
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                where e.CaseId == caseId && (w.Deleted == 0 || w.Deleted == null)
                      && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                orderby e.CreatedDate descending, e.Id descending
                select e).Take(limit).ToListAsync(token);
        }

        public async Task<CaseChecklistEvent> InsertAsync(CaseChecklistEvent model, CancellationToken token = default)
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

            model.CreatedDate = model.CreatedDate == default ? DateTime.UtcNow : model.CreatedDate;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public Task SetCaseEventIdAsync(long id, int caseEventId, CancellationToken token = default)
        {
            return dbContext.CaseChecklistEvent.Where(w => w.Id == id).Set(s => s.CaseEventId, caseEventId)
                .UpdateAsync(token);
        }
    }
}
