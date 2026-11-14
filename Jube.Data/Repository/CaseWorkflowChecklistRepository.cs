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

    public class CaseWorkflowChecklistRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowChecklistRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowChecklistRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        private sealed class Visibility
        {
            public CaseWorkflowChecklist Row { get; init; }
            public int WorkflowId { get; init; }
            public int StatusId { get; init; }
        }

        private IQueryable<Visibility> Visible()
        {
            return from c in dbContext.CaseWorkflowChecklist
                join s in dbContext.CaseWorkflowStatus on c.CaseWorkflowStatusGuid equals s.Guid
                where s.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                      && (s.Deleted == 0 || s.Deleted == null)
                      && (c.Deleted == 0 || c.Deleted == null)
                select new Visibility { Row = c, WorkflowId = s.CaseWorkflowId.Value, StatusId = s.Id };
        }

        private static IOrderedQueryable<Visibility> Ordered(IQueryable<Visibility> source)
        {
            return source.OrderBy(o => o.Row.Priority ?? 0).ThenBy(o => o.Row.Name).ThenBy(o => o.Row.Id);
        }

        private static List<CaseWorkflowChecklist> Stamp(IEnumerable<Visibility> rows)
        {
            return rows.Select(r =>
            {
                r.Row.CaseWorkflowId = r.WorkflowId;
                r.Row.CaseWorkflowStatusId = r.StatusId;

                return r.Row;
            }).ToList();
        }

        private IQueryable<CaseWorkflowStatus> VisibleStatuses()
        {
            return dbContext.CaseWorkflowStatus.Where(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null));
        }

        public Task<bool> ExistsCaseWorkflowStatusAsync(Guid caseWorkflowStatusGuid, CancellationToken token = default)
        {
            return VisibleStatuses().AnyAsync(w => w.Guid == caseWorkflowStatusGuid, token);
        }

        public Task<CaseWorkflowStatus> GetStatusAsync(Guid caseWorkflowStatusGuid, CancellationToken token = default)
        {
            return VisibleStatuses().FirstOrDefaultAsync(w => w.Guid == caseWorkflowStatusGuid, token);
        }

        public Task<CaseWorkflowStatus> GetStatusByIdAsync(int caseWorkflowStatusId, CancellationToken token = default)
        {
            return VisibleStatuses().FirstOrDefaultAsync(w => w.Id == caseWorkflowStatusId, token);
        }

        public async Task<IEnumerable<CaseWorkflowChecklist>> GetAllAsync(CancellationToken token = default)
        {
            return Stamp(await Ordered(Visible()).ToListAsync(token));
        }

        public async Task<List<CaseWorkflowChecklist>> GetByCaseWorkflowStatusIdAsync(int caseWorkflowStatusId,
            CancellationToken token = default)
        {
            return Stamp(await Ordered(Visible().Where(w => w.StatusId == caseWorkflowStatusId))
                .ToListAsync(token));
        }

        public async Task<List<CaseWorkflowChecklist>> GetActiveByCaseWorkflowStatusGuidAsync(
            Guid caseWorkflowStatusGuid, CancellationToken token = default)
        {
            return Stamp(await Ordered(Visible().Where(w =>
                    w.Row.CaseWorkflowStatusGuid == caseWorkflowStatusGuid && w.Row.Active == 1))
                .ToListAsync(token));
        }

        public async Task<CaseWorkflowChecklist> GetByIdAsync(int id, CancellationToken token = default)
        {
            return Stamp(await Visible().Where(w => w.Row.Id == id).ToListAsync(token)).FirstOrDefault();
        }

        public async Task<CaseWorkflowChecklist> InsertAsync(CaseWorkflowChecklist model,
            CancellationToken token = default)
        {
            var status = await GetStatusAsync(model.CaseWorkflowStatusGuid, token).ConfigureAwait(false);

            if (status == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Priority ??= 0;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            model.CaseWorkflowId = status.CaseWorkflowId.GetValueOrDefault();
            model.CaseWorkflowStatusId = status.Id;

            return model;
        }

        public async Task<CaseWorkflowChecklist> UpdateAsync(CaseWorkflowChecklist model,
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
            model.CaseWorkflowStatusGuid = existing.CaseWorkflowStatusGuid;
            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedUser = userName;
            model.UpdatedDate = DateTime.UtcNow;
            model.Priority ??= 0;
            model.ImportId = existing.ImportId;

            await dbContext.UpdateAsync(model, token: token);

            await dbContext.InsertAsync(new CaseWorkflowChecklistVersion
            {
                CaseWorkflowChecklistId = existing.Id,
                Guid = existing.Guid,
                CaseWorkflowStatusGuid = existing.CaseWorkflowStatusGuid,
                Name = existing.Name,
                Priority = existing.Priority,
                Active = existing.Active,
                CreatedDate = existing.CreatedDate,
                CreatedUser = existing.CreatedUser,
                UpdatedDate = existing.UpdatedDate,
                UpdatedUser = existing.UpdatedUser,
                Version = existing.Version
            }, token: token);

            model.CaseWorkflowId = existing.CaseWorkflowId;
            model.CaseWorkflowStatusId = existing.CaseWorkflowStatusId;

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var existing = await GetByIdAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            await dbContext.CaseWorkflowChecklist
                .Where(d => d.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.CaseWorkflowChecklist
                .Where(w => w.CaseWorkflowStatus.CaseWorkflow.EntityAnalysisModel.TenantRegistryId ==
                            tenantRegistryIdOutsideOfInstance
                            && (w.Deleted == 0 || w.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
