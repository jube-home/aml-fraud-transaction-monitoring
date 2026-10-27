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

    public class CaseSearchFilterRepository(DbContext dbContext, string userName, int tenantRegistryId)
    {
        private IQueryable<Guid> CallerRole => dbContext.UserRegistry
            .Where(w => w.Name == userName && (w.Deleted == 0 || w.Deleted == null))
            .Select(s => s.RoleRegistryGuid);

        private IQueryable<CaseSearchFilter> InScope()
        {
            var callerRole = CallerRole;

            return dbContext.CaseSearchFilter.Where(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (w.CaseWorkflow.Deleted == 0 || w.CaseWorkflow.Deleted == null)
                && (w.CaseWorkflow.EntityAnalysisModel.Deleted == 0
                    || w.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                && dbContext.CaseWorkflowRole.Any(r => r.CaseWorkflowGuid == w.CaseWorkflow.Guid
                                                       && (r.Deleted == 0 || r.Deleted == null)
                                                       && callerRole.Contains(r.RoleRegistryGuid)));
        }

        private IQueryable<CaseSearchFilter> Visible()
        {
            var callerRole = CallerRole;

            return InScope().Where(w => w.CreatedUser == userName
                                        || (w.SharedRoleRegistryGuid != null
                                            && callerRole.Contains(w.SharedRoleRegistryGuid.Value)));
        }

        public Task<List<CaseSearchFilter>> ListVisibleAsync(Guid? caseWorkflowGuid,
            CancellationToken token = default)
        {
            var query = Visible();

            if (caseWorkflowGuid.HasValue)
            {
                var guid = caseWorkflowGuid.Value;
                query = query.Where(w => w.CaseWorkflow.Guid == guid);
            }

            return query.LoadWith(w => w.CaseWorkflow).OrderBy(o => o.Name).ToListAsync(token);
        }

        public Task<CaseSearchFilter> GetVisibleByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return AsyncExtensions.FirstOrDefaultAsync(Visible().LoadWith(w => w.CaseWorkflow),
                w => w.Guid == guid, token);
        }

        public Task<CaseSearchFilter> GetOwnedByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return AsyncExtensions.FirstOrDefaultAsync(InScope().LoadWith(w => w.CaseWorkflow),
                w => w.Guid == guid && w.CreatedUser == userName, token);
        }

        public Task<CaseSearchFilter> GetOwnedByNameAsync(string name, int caseWorkflowId,
            CancellationToken token = default)
        {
            var lowered = name.ToLower();

            return InScope().FirstOrDefaultAsync(w => w.CreatedUser == userName
                                                      && w.CaseWorkflowId == caseWorkflowId
                                                      && w.Name.ToLower() == lowered, token);
        }

        public async Task<int?> GetAccessibleCaseWorkflowIdAsync(Guid caseWorkflowGuid,
            CancellationToken token = default)
        {
            var callerRole = CallerRole;

            var id = await dbContext.CaseWorkflow
                .Where(w => w.Guid == caseWorkflowGuid
                            && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null)
                            && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null)
                            && dbContext.CaseWorkflowRole.Any(r => r.CaseWorkflowGuid == w.Guid
                                                                   && (r.Deleted == 0 || r.Deleted == null)
                                                                   && callerRole.Contains(r.RoleRegistryGuid)))
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(token);

            return id;
        }

        public async Task<List<(Guid Guid, string Name, string ModelName)>> ListAccessibleWorkflowsAsync(
            CancellationToken token = default)
        {
            var callerRole = CallerRole;

            var rows = await dbContext.CaseWorkflow
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.Active == 1
                            && (w.Deleted == 0 || w.Deleted == null)
                            && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null)
                            && dbContext.CaseWorkflowRole.Any(r => r.CaseWorkflowGuid == w.Guid
                                                                   && (r.Deleted == 0 || r.Deleted == null)
                                                                   && callerRole.Contains(r.RoleRegistryGuid)))
                .OrderBy(o => o.EntityAnalysisModel.Name).ThenBy(o => o.Name)
                .Select(s => new { s.Guid, s.Name, ModelName = s.EntityAnalysisModel.Name })
                .ToListAsync(token);

            return rows.Select(r => (r.Guid, r.Name, r.ModelName)).ToList();
        }

        public Task<bool> RoleExistsAsync(Guid roleRegistryGuid, CancellationToken token = default)
        {
            return dbContext.RoleRegistry.AnyAsync(w => w.Guid == roleRegistryGuid
                                                        && w.TenantRegistryId == tenantRegistryId
                                                        && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<CaseSearchFilter> InsertAsync(CaseSearchFilter model, CancellationToken token = default)
        {
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;

            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<CaseSearchFilter> UpdateAsync(CaseSearchFilter model, CancellationToken token = default)
        {
            var existing = await GetOwnedByGuidAsync(model.Guid, token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            OptimisticConcurrency.Verify(model.Version, existing.Version);

            existing.Name = model.Name;
            existing.FilterJson = model.FilterJson;
            existing.SelectJson = model.SelectJson;
            existing.SharedRoleRegistryGuid = model.SharedRoleRegistryGuid;
            existing.UpdatedUser = userName;
            existing.UpdatedDate = DateTime.UtcNow;
            existing.Version += 1;

            await dbContext.UpdateAsync(existing, token: token);

            return existing;
        }

        public async Task<int> DeleteAsync(Guid guid, CancellationToken token = default)
        {
            var existing = await GetOwnedByGuidAsync(guid, token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            await dbContext.CaseSearchFilter.Where(w => w.Id == existing.Id).DeleteAsync(token);

            return existing.Id;
        }
    }
}
