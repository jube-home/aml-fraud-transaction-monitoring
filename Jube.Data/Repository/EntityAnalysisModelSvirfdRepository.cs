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
    using AutoMapper;
    using Concurrency;
    using Context;
    using Extensions;
    using LinqToDB;
    using LinqToDB.Async;
    using Microsoft.Extensions.Logging.Abstractions;
    using Models;
    using Poco;
    using Query.GetApprovedEntityQuery;

    public class EntityAnalysisModelSvirfdRepository :
        VersionedApprovalRepository<EntityAnalysisModelSvirfd, EntityAnalysisModelSvirfdVersion>
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelSvirfdRepository(DbContext dbContext, string userName) : this(dbContext,
            dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId).FirstOrDefault(),
            userName)
        {
        }

        public EntityAnalysisModelSvirfdRepository(DbContext dbContext, int tenantRegistryId) : this(dbContext,
            tenantRegistryId, null)
        {
        }

        private EntityAnalysisModelSvirfdRepository(DbContext dbContext, int? tenantRegistryId,
            string userName) : base(dbContext, EntityApprovalKind.EntityAnalysisModelSvirfd,
            nameof(EntityAnalysisModelSvirfdVersion.EntityAnalysisModelSvirfdId), tenantRegistryId,
            userName)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
            this.userName = userName;
        }

        protected override Task<EntityAnalysisModelSvirfd> LoadForMutationAsync(int id,
            CancellationToken token)
        {
            return GetByIdAsync(id, token);
        }

        public Task<EntityAnalysisModelSvirfd> GetByNameEntityAnalysisModelIdAsync(string name,
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSvirfd.FirstOrDefaultAsync(f =>
                (f.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
                && f.EntityAnalysisModelId == entityAnalysisModelId
                && (f.Deleted == 0 || f.Deleted == null)
                && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<EntityAnalysisModelSvirfd>> GetOrderByIdAsync(
            CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelSvirfd
                .Where(w => (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSvirfd
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelSvirfd>> GetByEntityAnalysisModelIdOrderByIdAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelSvirfd
                .Where(w => (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
                            && w.EntityAnalysisModelId == entityAnalysisModelId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelSvirfd>> GetApprovedByEntityAnalysisModelIdAsync(
            int entityAnalysisModelId, int approvalsRequired = 1, CancellationToken token = default)
        {
            var current = await dbContext.EntityAnalysisModelSvirfd
                .Where(w => (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
                            && w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            return await new GetApprovedEntityQuery<EntityAnalysisModelSvirfd,
                    EntityAnalysisModelSvirfdVersion>(dbContext,
                    EntityApprovalKind.EntityAnalysisModelSvirfd,
                    nameof(EntityAnalysisModelSvirfdVersion.EntityAnalysisModelSvirfdId),
                    tenantRegistryId)
                .ExecuteAsync(current, approvalsRequired, token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisModelSvirfd> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSvirfd.FirstOrDefaultAsync(w => w.Id == id
                && (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue), token);
        }

        public Task<EntityAnalysisModelSvirfd> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSvirfd.FirstOrDefaultAsync(w => w.Guid == guid
                && (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
                && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<bool> IsIpRequestXPathAsync(string requestXPathName, int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelRequestXpath.AnyAsync(w => w.EntityAnalysisModelId == entityAnalysisModelId
                && (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
                && (w.Deleted == 0 || w.Deleted == null)
                && w.Active == 1
                && w.DataTypeId == 8
                && w.Name.ToLower() == requestXPathName.ToLower(), token);
        }

        public Task<bool> ExistsEntityAnalysisModelAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModel.AnyAsync(w => w.Id == entityAnalysisModelId
                                                               && (w.TenantRegistryId == tenantRegistryId &&
                                                                   tenantRegistryId.HasValue)
                                                               && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<EntityAnalysisModelSvirfd> InsertAsync(EntityAnalysisModelSvirfd model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<EntityAnalysisModelSvirfd> UpdateAsync(EntityAnalysisModelSvirfd model,
            CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelSvirfd.FirstOrDefaultAsync(w => w.Id == model.Id
                && (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
                && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            OptimisticConcurrency.Verify(model.Version, existing.Version);

            model.Version = existing.Version + 1;
            model.EntityAnalysisModelId = existing.EntityAnalysisModelId;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.UpdatedUser = userName;
            model.UpdatedDate = DateTime.UtcNow;

            await dbContext.UpdateAsync(model, token: token);

            var audit = ArchiveMapper().Map<EntityAnalysisModelSvirfdVersion>(existing);
            audit.EntityAnalysisModelSvirfdId = existing.Id;

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

        private static Mapper ArchiveMapper()
        {
            return new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<EntityAnalysisModelSvirfd, EntityAnalysisModelSvirfdVersion>(); },
                NullLoggerFactory.Instance));
        }

        private async Task DeleteVersionedAsync(int id, CancellationToken token)
        {
            var existing = await dbContext.EntityAnalysisModelSvirfd
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
                    && w.Id == id
                    && (w.Locked == 0 || w.Locked == null)
                    && (w.Deleted == 0 || w.Deleted == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var mapper = ArchiveMapper();

            var audit = mapper.Map<EntityAnalysisModelSvirfdVersion>(existing);
            audit.EntityAnalysisModelSvirfdId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            var records = await dbContext.EntityAnalysisModelSvirfd
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId && tenantRegistryId.HasValue)
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

            var deletedState = mapper.Map<EntityAnalysisModelSvirfdVersion>(existing);
            deletedState.EntityAnalysisModelSvirfdId = existing.Id;
            deletedState.Deleted = Convert.ToByte(1);
            deletedState.DeletedDate = DateTime.UtcNow;
            deletedState.DeletedUser = userName;
            deletedState.Version = (existing.Version ?? 1) + 1;

            await dbContext.InsertAsync(deletedState, token: token);
        }
    }
}
