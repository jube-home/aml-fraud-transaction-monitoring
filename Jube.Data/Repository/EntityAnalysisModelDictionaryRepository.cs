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

using Jube.Data.Query.GetApprovedEntityQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jube.Data.Repository
{
    public class EntityAnalysisModelDictionaryRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelDictionaryRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelDictionaryRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelDictionaryRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<bool> ParentModelVisibleByGuidAsync(Guid entityAnalysisModelGuid, CancellationToken token = default)
        {
            return EntityAnalysisModelParentGuard.IsVisibleByGuidAsync(dbContext, tenantRegistryId,
                entityAnalysisModelGuid, token);
        }

        public Task<EntityAnalysisModelDictionary> GetByNameEntityAnalysisModelGuidAsync(string name,
            Guid entityAnalysisModelGuid, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelDictionary
                .FirstOrDefaultAsync(f =>
                    f.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && f.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionary>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelDictionary
                .Where(w =>
                    w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                .ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionary>> GetByEntityAnalysisModelIdOrderByIdAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelDictionary
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == w.EntityAnalysisModel.Guid
                    && w.EntityAnalysisModel.Id == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionary>> GetByEntityAnalysisModelIdOrderByNameAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelDictionary
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == w.EntityAnalysisModel.Guid
                    && w.EntityAnalysisModel.Id == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Name).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionary>> GetByEntityAnalysisModelGuidAsync(
            Guid entityAnalysisModelGuid, CancellationToken token = default, bool includeDeleted = false)
        {
            return await dbContext.EntityAnalysisModelDictionary
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == w.EntityAnalysisModel.Guid
                    && w.EntityAnalysisModel.Guid == entityAnalysisModelGuid
                    && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null)
                    && (includeDeleted || w.Deleted == 0 || w.Deleted == null))
                .ToListAsync(token);
        }

        public Task<EntityAnalysisModelDictionary> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelDictionary.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id, token);
        }

        public async Task<EntityAnalysisModelDictionary> InsertAsync(EntityAnalysisModelDictionary model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelDictionary> UpdateAsync(EntityAnalysisModelDictionary model,
            CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelDictionary
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            if (!await ParentModelVisibleByGuidAsync(model.EntityAnalysisModelGuid, token).ConfigureAwait(false))
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<EntityAnalysisModelDictionary, EntityAnalysisModelDictionaryVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelDictionaryVersion>(existing);
            audit.EntityAnalysisModelDictionaryId = existing.Id;

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

        private async Task DeleteVersionedAsync(int id, CancellationToken token)
        {
            var existing = await dbContext.EntityAnalysisModelDictionary
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
                cfg => { cfg.CreateMap<EntityAnalysisModelDictionary, EntityAnalysisModelDictionaryVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelDictionaryVersion>(existing);
            audit.EntityAnalysisModelDictionaryId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            var records = await dbContext.EntityAnalysisModelDictionary
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

            var deletedState = mapper.Map<EntityAnalysisModelDictionaryVersion>(existing);
            deletedState.EntityAnalysisModelDictionaryId = existing.Id;
            deletedState.Deleted = Convert.ToByte(1);
            deletedState.DeletedDate = DateTime.UtcNow;
            deletedState.DeletedUser = userName;
            deletedState.Version = (existing.Version ?? 1) + 1;

            await dbContext.InsertAsync(deletedState, token: token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelDictionary
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionary>> GetApprovedByEntityAnalysisModelIdOrderByIdAsync(
            int entityAnalysisModelId, int approvalsRequired = 1,
            CancellationToken token = default)
        {
            var current = await dbContext.EntityAnalysisModelDictionary
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == w.EntityAnalysisModel.Guid
                    && w.EntityAnalysisModel.Id == entityAnalysisModelId)
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            return await new
                    GetApprovedEntityQuery<EntityAnalysisModelDictionary, EntityAnalysisModelDictionaryVersion>(
                        dbContext,
                        EntityApprovalKind.EntityAnalysisModelDictionary,
                        nameof(EntityAnalysisModelDictionaryVersion.EntityAnalysisModelDictionaryId), tenantRegistryId)
                .ExecuteAsync(current, approvalsRequired, token).ConfigureAwait(false);
        }
    }
}