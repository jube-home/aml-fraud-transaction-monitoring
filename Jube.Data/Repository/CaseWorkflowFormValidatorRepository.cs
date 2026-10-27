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
    using Poco;
    using Jube.Data.Concurrency;

    public class CaseWorkflowFormValidatorRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowFormValidatorRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowFormValidatorRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        private sealed class Visibility
        {
            public CaseWorkflowFormValidator Row { get; init; }
            public int WorkflowId { get; init; }
            public Guid FormGuid { get; init; }
        }

        private IQueryable<Visibility> Visible()
        {
            return from v in dbContext.CaseWorkflowFormValidator
                join f in dbContext.CaseWorkflowForm on v.CaseWorkflowFormGuid equals f.Guid
                where f.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                      && (f.Deleted == 0 || f.Deleted == null)
                      && (v.Deleted == 0 || v.Deleted == null)
                select new Visibility { Row = v, WorkflowId = f.CaseWorkflowId.Value, FormGuid = f.Guid };
        }

        private static List<CaseWorkflowFormValidator> Stamp(IEnumerable<Visibility> rows)
        {
            return rows.Select(r =>
            {
                r.Row.CaseWorkflowId = r.WorkflowId;

                return r.Row;
            }).ToList();
        }

        private IQueryable<CaseWorkflowForm> VisibleForms()
        {
            return dbContext.CaseWorkflowForm.Where(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null));
        }

        public Task<bool> ExistsCaseWorkflowFormAsync(Guid caseWorkflowFormGuid, CancellationToken token = default)
        {
            return VisibleForms().AnyAsync(w => w.Guid == caseWorkflowFormGuid, token);
        }

        public Task<CaseWorkflowForm> GetFormAsync(Guid caseWorkflowFormGuid, CancellationToken token = default)
        {
            return VisibleForms().FirstOrDefaultAsync(w => w.Guid == caseWorkflowFormGuid, token);
        }

        public Task<Guid?> GetModelGuidOfFormAsync(Guid caseWorkflowFormGuid, CancellationToken token = default)
        {
            return VisibleForms().Where(w => w.Guid == caseWorkflowFormGuid)
                .Select(w => (Guid?)w.CaseWorkflow.EntityAnalysisModel.Guid).FirstOrDefaultAsync(token);
        }

        public async Task<IEnumerable<CaseWorkflowFormValidator>> GetAllDescAsync(CancellationToken token = default)
        {
            return Stamp(await Visible().OrderBy(o => o.Row.Id).ToListAsync(token));
        }

        public async Task<List<CaseWorkflowFormValidator>> GetByCaseWorkflowIdAsync(int caseWorkflowId,
            CancellationToken token = default)
        {
            return Stamp(await Visible().Where(w => w.WorkflowId == caseWorkflowId).OrderBy(o => o.Row.Id)
                .ToListAsync(token));
        }

        public async Task<List<CaseWorkflowFormValidator>> GetActiveByCaseWorkflowFormGuidAsync(Guid caseWorkflowFormGuid,
            CancellationToken token = default)
        {
            return Stamp(await Visible().Where(w => w.FormGuid == caseWorkflowFormGuid && w.Row.Active == 1)
                .OrderBy(o => o.Row.Id).ToListAsync(token));
        }

        public async Task<CaseWorkflowFormValidator> GetByIdAsync(int id, CancellationToken token = default)
        {
            return Stamp(await Visible().Where(w => w.Row.Id == id).ToListAsync(token)).FirstOrDefault();
        }

        public async Task<CaseWorkflowFormValidator> InsertAsync(CaseWorkflowFormValidator model,
            CancellationToken token = default)
        {
            var form = await GetFormAsync(model.CaseWorkflowFormGuid, token).ConfigureAwait(false);

            if (form == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            model.CaseWorkflowId = form.CaseWorkflowId.GetValueOrDefault();

            return model;
        }

        public async Task<CaseWorkflowFormValidator> UpdateAsync(CaseWorkflowFormValidator model,
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
            model.CaseWorkflowFormGuid = existing.CaseWorkflowFormGuid;
            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedUser = userName;
            model.UpdatedDate = DateTime.UtcNow;
            model.ImportId = existing.ImportId;

            await dbContext.UpdateAsync(model, token: token);

            await dbContext.InsertAsync(new CaseWorkflowFormValidatorVersion
            {
                CaseWorkflowFormValidatorId = existing.Id,
                Guid = existing.Guid,
                CaseWorkflowFormGuid = existing.CaseWorkflowFormGuid,
                Name = existing.Name,
                FieldName = existing.FieldName,
                KindId = existing.KindId,
                Argument1 = existing.Argument1,
                Argument2 = existing.Argument2,
                Sql = existing.Sql,
                Message = existing.Message,
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

            await dbContext.CaseWorkflowFormValidator
                .Where(d => d.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.CaseWorkflowFormValidator
                .Where(w => w.CaseWorkflowForm.CaseWorkflow.EntityAnalysisModel.TenantRegistryId ==
                            tenantRegistryIdOutsideOfInstance
                            && (w.Deleted == 0 || w.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
