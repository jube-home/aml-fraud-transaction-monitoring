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
    using Jube.Data.Concurrency;

    public class CaseWorkflowChecklistItemRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowChecklistItemRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowChecklistItemRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        private sealed class Visibility
        {
            public CaseWorkflowChecklistItem Row { get; init; }
            public int WorkflowId { get; init; }
        }

        private IQueryable<Visibility> Visible()
        {
            return from i in dbContext.CaseWorkflowChecklistItem
                join c in dbContext.CaseWorkflowChecklist on i.CaseWorkflowChecklistGuid equals c.Guid
                join s in dbContext.CaseWorkflowStatus on c.CaseWorkflowStatusGuid equals s.Guid
                where s.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                      && (s.Deleted == 0 || s.Deleted == null)
                      && (c.Deleted == 0 || c.Deleted == null)
                      && (i.Deleted == 0 || i.Deleted == null)
                select new Visibility { Row = i, WorkflowId = s.CaseWorkflowId.Value };
        }

        private static IOrderedQueryable<Visibility> Ordered(IQueryable<Visibility> source)
        {
            return source.OrderBy(o => o.Row.Priority ?? 0).ThenBy(o => o.Row.Name).ThenBy(o => o.Row.Id);
        }

        private static List<CaseWorkflowChecklistItem> Stamp(IEnumerable<Visibility> rows)
        {
            return rows.Select(r =>
            {
                r.Row.CaseWorkflowId = r.WorkflowId;

                return r.Row;
            }).ToList();
        }

        private IQueryable<CaseWorkflowChecklist> VisibleChecklists()
        {
            return from c in dbContext.CaseWorkflowChecklist
                join s in dbContext.CaseWorkflowStatus on c.CaseWorkflowStatusGuid equals s.Guid
                where s.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                      && (s.Deleted == 0 || s.Deleted == null)
                      && (c.Deleted == 0 || c.Deleted == null)
                select c;
        }

        public Task<bool> ExistsCaseWorkflowChecklistAsync(Guid caseWorkflowChecklistGuid,
            CancellationToken token = default)
        {
            return VisibleChecklists().AnyAsync(w => w.Guid == caseWorkflowChecklistGuid, token);
        }

        public async Task<int?> GetCaseWorkflowIdOfChecklistAsync(Guid caseWorkflowChecklistGuid,
            CancellationToken token = default)
        {
            return await (from c in VisibleChecklists()
                join s in dbContext.CaseWorkflowStatus on c.CaseWorkflowStatusGuid equals s.Guid
                where c.Guid == caseWorkflowChecklistGuid && (s.Deleted == 0 || s.Deleted == null)
                select s.CaseWorkflowId).FirstOrDefaultAsync(token);
        }

        public async Task<IEnumerable<CaseWorkflowChecklistItem>> GetAllAsync(CancellationToken token = default)
        {
            return Stamp(await Ordered(Visible()).ToListAsync(token));
        }

        public async Task<IEnumerable<CaseWorkflowChecklistItem>> GetAllDescAsync(CancellationToken token = default)
        {
            return Stamp(await Visible().OrderBy(o => o.Row.Id).ToListAsync(token));
        }

        public async Task<List<CaseWorkflowChecklistItem>> GetByCaseWorkflowIdAsync(int caseWorkflowId,
            CancellationToken token = default)
        {
            return Stamp(await Ordered(Visible().Where(w => w.WorkflowId == caseWorkflowId)).ToListAsync(token));
        }

        public async Task<List<CaseWorkflowChecklistItem>> GetByCaseWorkflowChecklistGuidAsync(
            Guid caseWorkflowChecklistGuid, CancellationToken token = default)
        {
            return Stamp(await Ordered(Visible().Where(w => w.Row.CaseWorkflowChecklistGuid ==
                                                            caseWorkflowChecklistGuid)).ToListAsync(token));
        }

        public async Task<List<CaseWorkflowChecklistItem>> GetActiveByCaseWorkflowChecklistGuidAsync(
            Guid caseWorkflowChecklistGuid, CancellationToken token = default)
        {
            return Stamp(await Ordered(Visible().Where(w =>
                    w.Row.CaseWorkflowChecklistGuid == caseWorkflowChecklistGuid && w.Row.Active == 1))
                .ToListAsync(token));
        }

        public async Task<CaseWorkflowChecklistItem> GetByIdAsync(int id, CancellationToken token = default)
        {
            return Stamp(await Visible().Where(w => w.Row.Id == id).ToListAsync(token)).FirstOrDefault();
        }

        public async Task<CaseWorkflowChecklistItem> InsertAsync(CaseWorkflowChecklistItem model,
            CancellationToken token = default)
        {
            var caseWorkflowId = await GetCaseWorkflowIdOfChecklistAsync(model.CaseWorkflowChecklistGuid, token)
                .ConfigureAwait(false);

            if (caseWorkflowId == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            model.CaseWorkflowId = caseWorkflowId.GetValueOrDefault();

            return model;
        }

        public async Task<CaseWorkflowChecklistItem> UpdateAsync(CaseWorkflowChecklistItem model,
            CancellationToken token = default)
        {
            var existing = await GetByIdAsync(model.Id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            OptimisticConcurrency.Verify(model.Version, existing.Version);

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CaseWorkflowChecklistGuid = existing.CaseWorkflowChecklistGuid;
            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedUser = userName;
            model.UpdatedDate = DateTime.UtcNow;
            model.ImportId = existing.ImportId;

            await dbContext.UpdateAsync(model, token: token);

            await dbContext.InsertAsync(new CaseWorkflowChecklistItemVersion
            {
                CaseWorkflowChecklistItemId = existing.Id,
                Guid = existing.Guid,
                CaseWorkflowChecklistGuid = existing.CaseWorkflowChecklistGuid,
                Name = existing.Name,
                Priority = existing.Priority,
                EnableShow = existing.EnableShow,
                ShowSql = existing.ShowSql,
                EnableValidation = existing.EnableValidation,
                ValidateSql = existing.ValidateSql,
                ValidateMessage = existing.ValidateMessage,
                SlaMinutes = existing.SlaMinutes,
                Active = existing.Active,
                CreatedDate = existing.CreatedDate,
                CreatedUser = existing.CreatedUser,
                UpdatedDate = existing.UpdatedDate,
                UpdatedUser = existing.UpdatedUser,
                Version = existing.Version
            }, token: token);

            model.CaseWorkflowId = existing.CaseWorkflowId;

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var existing = await GetByIdAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            await dbContext.CaseWorkflowChecklistItem
                .Where(d => d.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.CaseWorkflowChecklistItem
                .Where(w => w.CaseWorkflowChecklist.CaseWorkflowStatus.CaseWorkflow.EntityAnalysisModel
                                .TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (w.Deleted == 0 || w.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
