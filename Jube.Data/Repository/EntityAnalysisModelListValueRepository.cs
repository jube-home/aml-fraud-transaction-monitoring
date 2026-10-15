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

using Jube.Data.Query;
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
using Jube.Data.Helpers;
using Jube.Data.Poco;
using LinqToDB;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jube.Data.Repository
{
    public class EntityAnalysisModelListValueRepository :
        VersionedApprovalRepository<EntityAnalysisModelListValue, EntityAnalysisModelListValueVersion>
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelListValueRepository(DbContext dbContext, string userName) : this(dbContext,
            dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId).FirstOrDefault(),
            userName)
        {
        }

        public EntityAnalysisModelListValueRepository(DbContext dbContext, int tenantRegistryId) : this(dbContext,
            tenantRegistryId, null)
        {
        }

        public EntityAnalysisModelListValueRepository(DbContext dbContext) : this(dbContext, null, null)
        {
        }

        private EntityAnalysisModelListValueRepository(DbContext dbContext, int? tenantRegistryId,
            string userName) : base(dbContext, EntityApprovalKind.EntityAnalysisModelListValue,
            nameof(EntityAnalysisModelListValueVersion.EntityAnalysisModelListValueId), tenantRegistryId,
            userName)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
            this.userName = userName;
        }

        protected override Task<EntityAnalysisModelListValue> LoadForMutationAsync(int id, CancellationToken token)
        {
            return GetByIdAsync(id, token);
        }

        public async Task<IEnumerable<EntityAnalysisModelListValue>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelListValue
                .Where(w =>
                    w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                    !tenantRegistryId.HasValue)
                .ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelListValue>> GetByEntityAnalysisModelListIdOrderByIdAsync(
            int entityAnalysisModelListId, CancellationToken token = default, bool includeDeleted = false)
        {
            return await dbContext.EntityAnalysisModelListValue
                .Where(w =>
                    (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                     !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelListId == entityAnalysisModelListId
                    && (w.EntityAnalysisModelList.EntityAnalysisModel.Deleted == 0 ||
                        w.EntityAnalysisModelList.EntityAnalysisModel.Deleted == null)
                    && (includeDeleted || w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task<EntityAnalysisModelListValue> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelListValue.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                 !tenantRegistryId.HasValue)
                && w.Id == id
                && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
        }

        public Task<EntityAnalysisModelListValue> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelListValue.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                 !tenantRegistryId.HasValue)
                && w.Guid == guid && (w.Deleted == 0 || w.Deleted == null)
                && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
        }

        public Task<bool> ExistsLiveDuplicateAsync(int entityAnalysisModelListId, int excludeId, string listValue,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelListValue.AnyAsync(w =>
                (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                 !tenantRegistryId.HasValue)
                && w.EntityAnalysisModelListId == entityAnalysisModelListId
                && w.Id != excludeId
                && (w.Deleted == 0 || w.Deleted == null)
                && w.ListValue == listValue, token);
        }

        public Task<EntityAnalysisModelListValue> GetByListIdAndValueAsync(int entityAnalysisModelListId,
            string listValue, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelListValue.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                 !tenantRegistryId.HasValue)
                && w.EntityAnalysisModelListId == entityAnalysisModelListId && w.ListValue == listValue
                && (w.Deleted == 0 || w.Deleted == null)
                && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
        }

        public async Task<bool> ExistsUnresolvedDeletionAsync(int entityAnalysisModelListId, int excludeId,
            string listValue, int approvalsRequired, CancellationToken token = default)
        {
            var candidates = await dbContext.EntityAnalysisModelListValue
                .Where(w =>
                    (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                     !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelListId == entityAnalysisModelListId
                    && w.Id != excludeId
                    && w.Deleted == 1
                    && w.ListValue == listValue)
                .ToListAsync(token).ConfigureAwait(false);

            if (candidates.Count == 0)
            {
                return false;
            }

            var approvalRepository = tenantRegistryId.HasValue
                ? new EntityApprovalRepository(dbContext, tenantRegistryId.Value)
                : new EntityApprovalRepository(dbContext);

            var approvals = await approvalRepository.GetRowsByKindAndEntityIdsAsync(
                    EntityApprovalKind.EntityAnalysisModelListValue, candidates.Select(c => c.Id).ToList(), token)
                .ConfigureAwait(false);

            return candidates.Any(c => EntityApprovalResolver.EffectiveVersion(
                approvals.Where(a => a.EntityId == c.Id), c.Version ?? 1, approvalsRequired) != c.Version);
        }

        public async Task<EntityAnalysisModelListValue> InsertAsync(EntityAnalysisModelListValue model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;

            try
            {
                model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            }
            catch (Exception ex) when (GrantInsertion.IsUniqueViolation(ex))
            {
                throw new InvalidOperationException("A live value with this text already exists.", ex);
            }

            return model;
        }

        public async Task<EntityAnalysisModelListValue> UpdateAsync(EntityAnalysisModelListValue model,
            CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelListValue
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId ==
                                          tenantRegistryId
                                          && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow),
                    token);

            if (existing == null)
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

            try
            {
                await dbContext.UpdateAsync(model, token: token);
            }
            catch (Exception ex) when (GrantInsertion.IsUniqueViolation(ex))
            {
                throw new InvalidOperationException("A live value with this text already exists.", ex);
            }

            var mapper = new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<EntityAnalysisModelListValue, EntityAnalysisModelListValueVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelListValueVersion>(existing);
            audit.EntityAnalysisModelListValueId = existing.Id;

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
            return dbContext.EntityAnalysisModelListValue
                .Where(d =>
                    d.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        private async Task DeleteVersionedAsync(int id, CancellationToken token)
        {
            var existing = await dbContext.EntityAnalysisModelListValue
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                     !tenantRegistryId.HasValue)
                    && w.Id == id
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var mapper = new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<EntityAnalysisModelListValue, EntityAnalysisModelListValueVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelListValueVersion>(existing);
            audit.EntityAnalysisModelListValueId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            var records = await dbContext.EntityAnalysisModelListValue
                .Where(d =>
                    (d.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                     !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null)
                    && (d.DeleteExpiryDate == null || d.DeleteExpiryDate > DateTime.UtcNow))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .Set(s => s.Version, (existing.Version ?? 1) + 1)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }

            var deletedState = mapper.Map<EntityAnalysisModelListValueVersion>(existing);
            deletedState.EntityAnalysisModelListValueId = existing.Id;
            deletedState.Deleted = Convert.ToByte(1);
            deletedState.DeletedDate = DateTime.UtcNow;
            deletedState.DeletedUser = userName;
            deletedState.Version = (existing.Version ?? 1) + 1;

            await dbContext.InsertAsync(deletedState, token: token);
        }

        public async Task<IEnumerable<EntityAnalysisModelListValue>>
            GetApprovedByEntityAnalysisModelListIdOrderByIdAsync(
                int entityAnalysisModelListId, int approvalsRequired = 1,
                CancellationToken token = default)
        {
            var current = await dbContext.EntityAnalysisModelListValue
                .Where(w =>
                    (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                     !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelListId == entityAnalysisModelListId
                    && (w.EntityAnalysisModelList.EntityAnalysisModel.Deleted == 0 ||
                        w.EntityAnalysisModelList.EntityAnalysisModel.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow))
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            return await new GetApprovedEntityQuery<EntityAnalysisModelListValue, EntityAnalysisModelListValueVersion>(
                    dbContext,
                    EntityApprovalKind.EntityAnalysisModelListValue,
                    nameof(EntityAnalysisModelListValueVersion.EntityAnalysisModelListValueId), tenantRegistryId)
                .ExecuteAsync(current, approvalsRequired, token).ConfigureAwait(false);
        }
    }
}