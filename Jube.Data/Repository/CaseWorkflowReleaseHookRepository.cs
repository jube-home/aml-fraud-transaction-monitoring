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
    using Concurrency;
    using Context;
    using LinqToDB;
    using Poco;

    public class CaseWorkflowReleaseHookRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowReleaseHookRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowReleaseHookRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        private sealed class Visibility
        {
            public CaseWorkflowReleaseHook Row { get; init; }
            public int WorkflowId { get; init; }
        }

        private IQueryable<Visibility> Visible()
        {
            return from v in dbContext.CaseWorkflowReleaseHook
                join w in dbContext.CaseWorkflow on v.CaseWorkflowGuid equals w.Guid
                where w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                      && (w.Deleted == 0 || w.Deleted == null)
                      && (v.Deleted == 0 || v.Deleted == null)
                select new Visibility { Row = v, WorkflowId = w.Id };
        }

        private static List<CaseWorkflowReleaseHook> Stamp(IEnumerable<Visibility> rows)
        {
            return rows.Select(r =>
            {
                r.Row.CaseWorkflowId = r.WorkflowId;

                return r.Row;
            }).ToList();
        }

        private IQueryable<CaseWorkflow> VisibleWorkflows()
        {
            return dbContext.CaseWorkflow.Where(w =>
                w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null));
        }

        public Task<bool> ExistsCaseWorkflowAsync(Guid caseWorkflowGuid, CancellationToken token = default)
        {
            return VisibleWorkflows().AnyAsync(w => w.Guid == caseWorkflowGuid, token);
        }

        public Task<CaseWorkflow> GetCaseWorkflowByIdAsync(int caseWorkflowId, CancellationToken token = default)
        {
            return VisibleWorkflows().FirstOrDefaultAsync(w => w.Id == caseWorkflowId, token);
        }

        public async Task<IEnumerable<CaseWorkflowReleaseHook>> GetAllDescAsync(CancellationToken token = default)
        {
            return Stamp(await Visible().OrderBy(o => o.Row.Id).ToListAsync(token));
        }

        public async Task<List<CaseWorkflowReleaseHook>> GetByCaseWorkflowIdAsync(int caseWorkflowId,
            CancellationToken token = default)
        {
            return Stamp(await Visible().Where(w => w.WorkflowId == caseWorkflowId).OrderBy(o => o.Row.Id)
                .ToListAsync(token));
        }

        public async Task<List<CaseWorkflowReleaseHook>> GetActiveByCaseWorkflowGuidAsync(Guid caseWorkflowGuid,
            CancellationToken token = default)
        {
            return Stamp(await Visible().Where(w => w.Row.CaseWorkflowGuid == caseWorkflowGuid && w.Row.Active == 1)
                .OrderBy(o => o.Row.Id).ToListAsync(token));
        }

        public async Task<CaseWorkflowReleaseHook> GetByIdAsync(int id, CancellationToken token = default)
        {
            return Stamp(await Visible().Where(w => w.Row.Id == id).ToListAsync(token)).FirstOrDefault();
        }

        public async Task<CaseWorkflowReleaseHook> GetByNameAsync(string name, int caseWorkflowId,
            CancellationToken token = default)
        {
            return Stamp(await Visible()
                .Where(w => w.WorkflowId == caseWorkflowId && w.Row.Name.ToLower() == name.ToLower())
                .ToListAsync(token)).FirstOrDefault();
        }

        public async Task<CaseWorkflowReleaseHook> InsertAsync(CaseWorkflowReleaseHook model,
            CancellationToken token = default)
        {
            var workflow = await VisibleWorkflows().FirstOrDefaultAsync(w => w.Guid == model.CaseWorkflowGuid, token)
                .ConfigureAwait(false);

            if (workflow == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            model.CaseWorkflowId = workflow.Id;

            return model;
        }

        public async Task<CaseWorkflowReleaseHook> UpdateAsync(CaseWorkflowReleaseHook model,
            CancellationToken token = default)
        {
            var existing = await GetByIdAsync(model.Id, token).ConfigureAwait(false);

            if (existing == null || existing.Locked == 1)
            {
                throw new KeyNotFoundException();
            }

            OptimisticConcurrency.Verify(model.Version, existing.Version);

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CaseWorkflowGuid = existing.CaseWorkflowGuid;
            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedUser = userName;
            model.UpdatedDate = DateTime.UtcNow;
            model.ImportId = existing.ImportId;
            model.InheritedId = existing.InheritedId;

            await dbContext.UpdateAsync(model, token: token);

            await dbContext.InsertAsync(new CaseWorkflowReleaseHookVersion
            {
                CaseWorkflowReleaseHookId = existing.Id,
                Guid = existing.Guid,
                CaseWorkflowGuid = existing.CaseWorkflowGuid,
                Name = existing.Name,
                Active = existing.Active,
                Locked = existing.Locked,
                SourceField = existing.SourceField,
                SourceValue = existing.SourceValue,
                ReleaseUrl = existing.ReleaseUrl,
                RefuseUrl = existing.RefuseUrl,
                HttpEndpointTypeId = existing.HttpEndpointTypeId,
                HeaderName = existing.HeaderName,
                CredentialVariable = existing.CredentialVariable,
                Body = existing.Body,
                CreatedDate = existing.CreatedDate,
                CreatedUser = existing.CreatedUser,
                UpdatedDate = existing.UpdatedDate,
                UpdatedUser = existing.UpdatedUser,
                Deleted = existing.Deleted,
                DeletedDate = existing.DeletedDate,
                DeletedUser = existing.DeletedUser,
                Version = existing.Version,
                InheritedId = existing.InheritedId,
                ImportId = existing.ImportId
            }, token: token);

            model.CaseWorkflowId = existing.CaseWorkflowId;

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var existing = await GetByIdAsync(id, token).ConfigureAwait(false);

            if (existing == null || existing.Locked == 1)
            {
                throw new KeyNotFoundException();
            }

            await dbContext.CaseWorkflowReleaseHook
                .Where(d => d.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.CaseWorkflowReleaseHook
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (w.Deleted == 0 || w.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
