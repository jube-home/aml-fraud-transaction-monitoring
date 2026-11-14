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

    public class CaseChecklistItemRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public CaseChecklistItemRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseChecklistItemRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        private IQueryable<CaseChecklistItem> Visible()
        {
            return from i in dbContext.CaseChecklistItem
                join c in dbContext.Case on i.CaseId equals c.Id
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                where (w.Deleted == 0 || w.Deleted == null) && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                select i;
        }

        private Task<bool> CaseInTenantAsync(int caseId, CancellationToken token)
        {
            return (from c in dbContext.Case
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                where c.Id == caseId && (w.Deleted == 0 || w.Deleted == null)
                      && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                select c.Id).AnyAsync(token);
        }

        public Task<List<CaseChecklistItem>> ListForCaseAsync(int caseId, CancellationToken token = default)
        {
            return Visible().Where(w => w.CaseId == caseId).OrderBy(o => o.Priority).ThenBy(o => o.ItemName)
                .ThenBy(o => o.Id).ToListAsync(token);
        }

        public Task<CaseChecklistItem> GetAsync(int caseId, long id, CancellationToken token = default)
        {
            return Visible().Where(w => w.CaseId == caseId && w.Id == id).FirstOrDefaultAsync(token);
        }

        public Task<List<CaseChecklistItem>> ListOpenPastDueAsync(int caseId, DateTime now,
            CancellationToken token = default)
        {
            return Visible().Where(w => w.CaseId == caseId && w.StateId == 1 && w.SlaBreachedDate == null
                                        && w.DueDate != null && w.DueDate < now)
                .OrderBy(o => o.DueDate).ThenBy(o => o.Id).ToListAsync(token);
        }

        public async Task<bool> TryRecordSlaBreachAsync(long id, int caseId, DateTime now,
            CancellationToken token = default)
        {
            if (!await CaseInTenantAsync(caseId, token).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The case is not in the tenant of the repository.");
            }

            var updated = await dbContext.CaseChecklistItem
                .Where(w => w.Id == id && w.CaseId == caseId && w.SlaBreachedDate == null)
                .Set(s => s.SlaBreachedDate, now)
                .UpdateAsync(token).ConfigureAwait(false);

            return updated == 1;
        }

        public async Task<CaseChecklistItem> InsertAsync(CaseChecklistItem model, CancellationToken token = default)
        {
            if (!await CaseInTenantAsync(model.CaseId, token).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The case is not in the tenant of the repository.");
            }

            model.CreatedDate = model.CreatedDate == default ? DateTime.UtcNow : model.CreatedDate;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task UpdateAsync(CaseChecklistItem model, CancellationToken token = default)
        {
            if (!await CaseInTenantAsync(model.CaseId, token).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The case is not in the tenant of the repository.");
            }

            await dbContext.CaseChecklistItem.Where(w => w.Id == model.Id && w.CaseId == model.CaseId)
                .Set(s => s.StateId, model.StateId)
                .Set(s => s.ShownDate, model.ShownDate)
                .Set(s => s.CompletedDate, model.CompletedDate)
                .Set(s => s.CompletedUser, model.CompletedUser)
                .Set(s => s.LastEvaluatedDate, model.LastEvaluatedDate)
                .Set(s => s.UpdatedDate, model.UpdatedDate)
                .UpdateAsync(token).ConfigureAwait(false);
        }
    }
}
