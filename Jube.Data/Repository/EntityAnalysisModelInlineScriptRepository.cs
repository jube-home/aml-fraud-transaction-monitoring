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

using Jube.Data.Concurrency;
using Jube.Data.Query.GetApprovedEntityQuery;
using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Jube.Data.Context;
using Jube.Data.Extensions;
using Jube.Data.Poco;
using LinqToDB;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jube.Data.Repository
{
    public class EntityAnalysisModelInlineScriptRepository :
        VersionedApprovalRepository<EntityAnalysisModelInlineScript, EntityAnalysisModelInlineScriptVersion>
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelInlineScriptRepository(DbContext dbContext, string userName) : this(dbContext,
            dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId).FirstOrDefault(),
            userName)
        {
        }

        public EntityAnalysisModelInlineScriptRepository(DbContext dbContext, int tenantRegistryId) : this(dbContext,
            tenantRegistryId, null)
        {
        }

        public EntityAnalysisModelInlineScriptRepository(DbContext dbContext) : this(dbContext, null, null)
        {
        }

        private EntityAnalysisModelInlineScriptRepository(DbContext dbContext, int? tenantRegistryId,
            string userName) : base(dbContext, EntityApprovalKind.EntityAnalysisModelInlineScript,
            nameof(EntityAnalysisModelInlineScriptVersion.EntityAnalysisModelInlineScriptId), tenantRegistryId,
            userName)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
            this.userName = userName;
        }

        protected override Task<EntityAnalysisModelInlineScript> LoadForMutationAsync(int id,
            CancellationToken token)
        {
            return GetByIdAsync(id, token);
        }

        public Task<EntityAnalysisModelInlineScript> GetByNameEntityAnalysisModelIdAsync(string name,
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelInlineScript
                .FirstOrDefaultAsync(f =>
                    f.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && f.EntityAnalysisModelId == entityAnalysisModelId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<EntityAnalysisModelInlineScript>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelInlineScript
                .Where(w => (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelInlineScript>> GetByEntityAnalysisModelIdOrderByIdAsync(
            int entityAnalysisModelId, CancellationToken token = default, bool includeDeleted = false)
        {
            return await dbContext.EntityAnalysisModelInlineScript
                .Where(w => (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && w.EntityAnalysisModelId == entityAnalysisModelId &&
                            (includeDeleted || w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntityAnalysisModelInlineScript>> GetByEntityAnalysisModelIdOrderByNameAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelInlineScript
                .Where(w => (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && w.EntityAnalysisModelId == entityAnalysisModelId &&
                            (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Name).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisModelInlineScript> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelInlineScript.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id, token);
        }

        public Task<EntityAnalysisModelInlineScript> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelInlineScript.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Guid == guid && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<EntityAnalysisModelInlineScript> InsertAsync(EntityAnalysisModelInlineScript model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<EntityAnalysisModelInlineScript> UpdateAsync(EntityAnalysisModelInlineScript model,
            CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelInlineScript
                .FirstOrDefaultAsync(w => (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                                           !tenantRegistryId.HasValue)
                                          && w.Id == model.Id
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            OptimisticConcurrency.Verify(model.Version, existing.Version);

            model.Version = existing.Version + 1;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<EntityAnalysisModelInlineScript, EntityAnalysisModelInlineScriptVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelInlineScriptVersion>(existing);
            audit.EntityAnalysisModelInlineScriptId = existing.Id;

            if (existing.Deleted != 1)
            {
                await dbContext.InsertAsync(audit, token: token);
            }

            return model;
        }

        public Task DeleteAsync(int id, CancellationToken token = default)
        {
            return dbContext.InTransactionAsync(() => DeleteVersionedAsync(id, token), token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelInlineScript
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        private async Task DeleteVersionedAsync(int id, CancellationToken token)
        {
            var existing = await dbContext.EntityAnalysisModelInlineScript
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.Id == id
                    && (w.Locked == 0 || w.Locked == null)
                    && (w.Deleted == 0 || w.Deleted == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var mapper = new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<EntityAnalysisModelInlineScript, EntityAnalysisModelInlineScriptVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelInlineScriptVersion>(existing);
            audit.EntityAnalysisModelInlineScriptId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            var records = await dbContext.EntityAnalysisModelInlineScript
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Locked == 0 || d.Locked == null)
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .Set(s => s.Version, (existing.Version ?? 1) + 1)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }

            var deletedState = mapper.Map<EntityAnalysisModelInlineScriptVersion>(existing);
            deletedState.EntityAnalysisModelInlineScriptId = existing.Id;
            deletedState.Deleted = Convert.ToByte(1);
            deletedState.DeletedDate = DateTime.UtcNow;
            deletedState.DeletedUser = userName;
            deletedState.Version = (existing.Version ?? 1) + 1;

            await dbContext.InsertAsync(deletedState, token: token);
        }

        public async Task<IEnumerable<EntityAnalysisModelInlineScript>>
            GetApprovedByEntityAnalysisModelIdOrderByIdAsync(
                int entityAnalysisModelId, int approvalsRequired = 1,
                CancellationToken token = default)
        {
            var current = await dbContext.EntityAnalysisModelInlineScript
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            return await new
                    GetApprovedEntityQuery<EntityAnalysisModelInlineScript, EntityAnalysisModelInlineScriptVersion>(
                        dbContext,
                        EntityApprovalKind.EntityAnalysisModelInlineScript,
                        nameof(EntityAnalysisModelInlineScriptVersion.EntityAnalysisModelInlineScriptId),
                        tenantRegistryId)
                .ExecuteAsync(current, approvalsRequired, token).ConfigureAwait(false);
        }
    }
}