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
using LinqToDB.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jube.Data.Repository
{
    public class EntityAnalysisModelRequestXpathRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelRequestXpathRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelRequestXpathRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public EntityAnalysisModelRequestXpathRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<EntityAnalysisModelRequestXpath> GetByNameEntityAnalysisModelIdAsync(string name,
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelRequestXpath
                .FirstOrDefaultAsync(f =>
                    f.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && f.EntityAnalysisModelId == entityAnalysisModelId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<EntityAnalysisModelRequestXpath>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelRequestXpath
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelRequestXpath>> GetByEntityAnalysisModelIdOrderByIdAsync(
            int entityAnalysisModelId, CancellationToken token = default, bool includeDeleted = false)
        {
            return await dbContext.EntityAnalysisModelRequestXpath
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && (includeDeleted || w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntityAnalysisModelRequestXpath>> GetByEntityAnalysisModelIdOrderByNameAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelRequestXpath
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Name).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntityAnalysisModelRequestXpath>>
            GetByEntityAnalysisModelIdOrderByNameCacheOnlyAsync(int entityAnalysisModelId,
                CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelRequestXpath
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && w.Cache == 1
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Name).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntityAnalysisModelRequestXpath>> GetByOverrideKeysAsync(
            CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelRequestXpath
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null)
                    && w.EnableOverride == 1 && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelRequestXpath>> GetByCasesWorkflowIdAsync(int casesWorkflowId,
            CancellationToken token = default)
        {
            var query =
                from x in dbContext.EntityAnalysisModelRequestXpath
                join m in dbContext.EntityAnalysisModel
                    on x.EntityAnalysisModelId equals m.Id
                join c in dbContext.CaseWorkflow on m.Id equals c.EntityAnalysisModelId
                where (m.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                      && c.Id == casesWorkflowId && (x.Deleted == 0 || x.Deleted == null)
                select x;

            return await query.ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelRequestXpath>> GetByEntityAnalysisModelIdByDataTypeAsync(
            int entityAnalysisModelId, CancellationToken token = default, params int[] dataTypeIds)
        {
            var dataTypeIdsList = dataTypeIds.ToList();

            return await dbContext.EntityAnalysisModelRequestXpath
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && dataTypeIdsList.Contains(w.DataTypeId.GetValueOrDefault())
                    && w.EntityAnalysisModelId == entityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync(token);
        }

        public Task<EntityAnalysisModelRequestXpath> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelRequestXpath.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id, token);
        }

        public Task<EntityAnalysisModelRequestXpath> InsertIncrementCacheIndexIdAsync(
            EntityAnalysisModelRequestXpath model, CancellationToken token = default)
        {
            return dbContext.InTransactionAsync(async () =>
            {
                await dbContext.ExecuteAsync("SELECT pg_advisory_xact_lock(@key)", token,
                    new DataParameter("key", 0x5850415400000000L + model.EntityAnalysisModelId));

                var cacheIndexId = await dbContext.EntityAnalysisModelRequestXpath
                    .Where(w => (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                                 !tenantRegistryId.HasValue)
                                && w.EntityAnalysisModel.Id == model.EntityAnalysisModelId
                    ).MaxAsync(m => m.CacheIndexId, token) ?? 0;

                model.CreatedUser = userName ?? model.CreatedUser;
                model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
                model.CreatedDate = DateTime.UtcNow;
                model.Version = 1;
                model.CacheIndexId = cacheIndexId + 1;

                model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
                return model;
            }, token);
        }

        public async Task<EntityAnalysisModelRequestXpath> InsertAsync(EntityAnalysisModelRequestXpath model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;

            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelRequestXpath> UpdateAsync(EntityAnalysisModelRequestXpath model,
            CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelRequestXpath
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.Id == model.Id
                    && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Guid = existing.Guid;
            model.CacheIndexId = existing.CacheIndexId;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<EntityAnalysisModelRequestXpath, EntityAnalysisModelRequestXpathVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelRequestXpathVersion>(existing);
            audit.EntityAnalysisModelRequestXpathId = existing.Id;

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
            var existing = await dbContext.EntityAnalysisModelRequestXpath
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
                cfg => { cfg.CreateMap<EntityAnalysisModelRequestXpath, EntityAnalysisModelRequestXpathVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelRequestXpathVersion>(existing);
            audit.EntityAnalysisModelRequestXpathId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            var records = await dbContext.EntityAnalysisModelRequestXpath
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

            var deletedState = mapper.Map<EntityAnalysisModelRequestXpathVersion>(existing);
            deletedState.EntityAnalysisModelRequestXpathId = existing.Id;
            deletedState.Deleted = Convert.ToByte(1);
            deletedState.DeletedDate = DateTime.UtcNow;
            deletedState.DeletedUser = userName;
            deletedState.Version = (existing.Version ?? 1) + 1;

            await dbContext.InsertAsync(deletedState, token: token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelRequestXpath
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelRequestXpath>>
            GetApprovedByEntityAnalysisModelIdOrderByIdAsync(
                int entityAnalysisModelId, int approvalsRequired = 1,
                CancellationToken token = default)
        {
            var current = await dbContext.EntityAnalysisModelRequestXpath
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            return await new
                    GetApprovedEntityQuery<EntityAnalysisModelRequestXpath, EntityAnalysisModelRequestXpathVersion>(
                        dbContext,
                        EntityApprovalKind.EntityAnalysisModelRequestXPath,
                        nameof(EntityAnalysisModelRequestXpathVersion.EntityAnalysisModelRequestXpathId),
                        tenantRegistryId)
                .ExecuteAsync(current, approvalsRequired, token).ConfigureAwait(false);
        }
    }
}