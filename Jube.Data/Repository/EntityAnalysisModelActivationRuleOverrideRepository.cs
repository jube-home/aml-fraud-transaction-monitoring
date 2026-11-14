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
    using Context;
    using Extensions;
    using LinqToDB;
    using LinqToDB.Async;
    using Microsoft.Extensions.Logging.Abstractions;
    using Models;
    using Poco;
    using Query.GetApprovedEntityQuery;

    public class EntityAnalysisModelActivationRuleOverrideRepository :
        VersionedApprovalRepository<EntityAnalysisModelActivationRuleOverride,
            EntityAnalysisModelActivationRuleOverrideVersion>
    {
        private const byte ForceKind = 1;

        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelActivationRuleOverrideRepository(DbContext dbContext, string userName) : this(
            dbContext,
            dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId)
                .FirstOrDefault(), userName)
        {
        }

        public EntityAnalysisModelActivationRuleOverrideRepository(DbContext dbContext, int tenantRegistryId) :
            this(dbContext, tenantRegistryId, null)
        {
        }

        public EntityAnalysisModelActivationRuleOverrideRepository(DbContext dbContext) : this(dbContext, null,
            null)
        {
        }

        private EntityAnalysisModelActivationRuleOverrideRepository(DbContext dbContext, int? tenantRegistryId,
            string userName) : base(dbContext, EntityApprovalKind.EntityAnalysisModelActivationRuleOverride,
            nameof(EntityAnalysisModelActivationRuleOverrideVersion.EntityAnalysisModelActivationRuleOverrideId),
            tenantRegistryId, userName)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
            this.userName = userName;
        }

        protected override Task<EntityAnalysisModelActivationRuleOverride> LoadForMutationAsync(int id,
            CancellationToken token)
        {
            return dbContext.EntityAnalysisModelActivationRuleOverride.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id, token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRuleOverride>>
            GetApprovedByEntityAnalysisModelGuidOrderByIdAsync(Guid entityAnalysisModelGuid,
                int approvalsRequired = 1, CancellationToken token = default)
        {
            var current = await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid)
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            var now = DateTime.UtcNow;

            var suppress = current.Where(w => w.OverrideKind != ForceKind
                                              && (w.Deleted == 0 || w.Deleted == null)
                                              && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > now));

            var approvedForce = await new GetApprovedEntityQuery<EntityAnalysisModelActivationRuleOverride,
                    EntityAnalysisModelActivationRuleOverrideVersion>(dbContext,
                    EntityApprovalKind.EntityAnalysisModelActivationRuleOverride,
                    nameof(EntityAnalysisModelActivationRuleOverrideVersion
                        .EntityAnalysisModelActivationRuleOverrideId), tenantRegistryId)
                .ExecuteAsync(current.Where(w => w.OverrideKind == ForceKind).ToList(), approvalsRequired, token)
                .ConfigureAwait(false);

            return suppress
                .Concat(approvedForce.Where(w => w.DeleteExpiryDate == null || w.DeleteExpiryDate > now))
                .OrderBy(o => o.Id)
                .ToList();
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRuleOverride>> GetAsync(
            CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w =>
                    w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue
                ).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRuleOverride>>
            GetByEntityAnalysisModelGuidOrderByIdAsync(
                Guid entityAnalysisModelGuid, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow))
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisModelActivationRuleOverride> GetByIdAsync(int id,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelActivationRuleOverride.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null)
                && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
        }

        public async Task<List<EntityAnalysisModelActivationRuleOverride>> GetForcePendingRemovalAsync(
            Guid entityAnalysisModelGuid, string overrideKey, string overrideKeyValue, int approvalsRequired = 1,
            CancellationToken token = default)
        {
            var removed = await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && w.OverrideKey == overrideKey
                    && w.OverrideKeyValue == overrideKeyValue
                    && w.OverrideKind == ForceKind
                    && w.Deleted == 1)
                .ToListAsync(token).ConfigureAwait(false);

            var now = DateTime.UtcNow;

            var stillLive = await new GetApprovedEntityQuery<EntityAnalysisModelActivationRuleOverride,
                    EntityAnalysisModelActivationRuleOverrideVersion>(dbContext,
                    EntityApprovalKind.EntityAnalysisModelActivationRuleOverride,
                    nameof(EntityAnalysisModelActivationRuleOverrideVersion
                        .EntityAnalysisModelActivationRuleOverrideId), tenantRegistryId)
                .ExecuteAsync(removed, approvalsRequired, token).ConfigureAwait(false);

            return stillLive.Where(w => w.DeleteExpiryDate == null || w.DeleteExpiryDate > now).ToList();
        }

        public Task<EntityAnalysisModelActivationRuleOverride> GetByIdIncludingRemovedAsync(int id,
            CancellationToken token = default)
        {
            return LoadForMutationAsync(id, token);
        }

        public async Task<EntityAnalysisModelActivationRuleOverride> InsertAsync(
            EntityAnalysisModelActivationRuleOverride model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<EntityAnalysisModelActivationRuleOverride> UpdateAsync(
            EntityAnalysisModelActivationRuleOverride model, CancellationToken token = default)
        {
            EntityAnalysisModelActivationRuleOverride existing;

            if (model.Id != 0)
            {
                existing = await dbContext.EntityAnalysisModelActivationRuleOverride
                    .FirstOrDefaultAsync(w =>
                        (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                        && w.Id == model.Id
                        && (w.Deleted == 0 || w.Deleted == null)
                        && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
            }
            else
            {
                existing = await dbContext.EntityAnalysisModelActivationRuleOverride
                    .FirstOrDefaultAsync(w =>
                        (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                        && w.OverrideKey == model.OverrideKey
                        && w.OverrideKeyValue == model.OverrideKeyValue
                        && w.EntityAnalysisModelGuid == model.EntityAnalysisModelGuid
                        && w.EntityAnalysisModelActivationRuleName ==
                        model.EntityAnalysisModelActivationRuleName
                        && (w.Deleted == 0 || w.Deleted == null)
                        && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
            }

            if (existing == null && model.Id != 0)
            {
                throw new KeyNotFoundException();
            }

            if (existing != null)
            {
                await DeleteAsync(existing.Id, token);
            }
            else
            {
                model.CreatedUser = userName;
                model.CreatedDate = DateTime.UtcNow;
                model.Version = 1;
                var id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
                model.Id = id;
            }

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelActivationRuleOverride
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.Id == id
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            if (existing.OverrideKind == ForceKind)
            {
                await dbContext.InTransactionAsync(async () =>
                {
                    await InsertVersionAsync(existing, token);

                    await dbContext.EntityAnalysisModelActivationRuleOverride
                        .Where(d => d.Id == id)
                        .Set(s => s.Deleted, Convert.ToByte(1))
                        .Set(s => s.DeletedDate, DateTime.UtcNow)
                        .Set(s => s.DeletedUser, userName)
                        .Set(s => s.Version, (existing.Version ?? 1) + 1)
                        .UpdateAsync(token);
                }, token);

                return;
            }

            await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(d => d.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            await InsertVersionAsync(existing, token);
        }

        public Task<bool> IsActiveForceAsync(Guid entityAnalysisModelGuid, string overrideKey,
            string overrideKeyValue, string entityAnalysisModelActivationRuleName, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelActivationRuleOverride
                .AnyAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.OverrideKey == overrideKey
                    && w.OverrideKeyValue == overrideKeyValue
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && w.EntityAnalysisModelActivationRuleName == entityAnalysisModelActivationRuleName
                    && w.OverrideKind == 1
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
        }

        public async Task<EntityAnalysisModelActivationRuleOverride> UpdateDeleteExpiryDateAsync(
            Guid entityAnalysisModelGuid, string overrideKey, string overrideKeyValue,
            string entityAnalysisModelActivationRuleName, DateTime? deleteExpiryDate, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelActivationRuleOverride
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.OverrideKey == overrideKey
                    && w.OverrideKeyValue == overrideKeyValue
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && w.EntityAnalysisModelActivationRuleName == entityAnalysisModelActivationRuleName
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var version = (existing.Version ?? 0) + 1;

            await InsertVersionAsync(existing, token);

            var update = dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => w.Id == existing.Id)
                .Set(s => s.DeleteExpiryDate, deleteExpiryDate)
                .Set(s => s.Version, version);

            if (existing.OverrideKind == ForceKind)
            {
                var createdDate = DateTime.UtcNow;

                await update
                    .Set(s => s.CreatedUser, userName)
                    .Set(s => s.CreatedDate, createdDate)
                    .UpdateAsync(token);

                existing.CreatedUser = userName;
                existing.CreatedDate = createdDate;
            }
            else
            {
                await update.UpdateAsync(token);
            }

            existing.DeleteExpiryDate = deleteExpiryDate;
            existing.Version = version;

            return existing;
        }

        public async Task<EntityAnalysisModelActivationRuleOverride> UpdateOverrideKindAsync(
            Guid entityAnalysisModelGuid, string overrideKey, string overrideKeyValue,
            string entityAnalysisModelActivationRuleName, byte overrideKind, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelActivationRuleOverride
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.OverrideKey == overrideKey
                    && w.OverrideKeyValue == overrideKeyValue
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && w.EntityAnalysisModelActivationRuleName == entityAnalysisModelActivationRuleName
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var version = (existing.Version ?? 0) + 1;

            await InsertVersionAsync(existing, token);

            var update = dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => w.Id == existing.Id)
                .Set(s => s.OverrideKind, overrideKind)
                .Set(s => s.Version, version);

            if (overrideKind == ForceKind || existing.OverrideKind == ForceKind)
            {
                var createdDate = DateTime.UtcNow;

                await update
                    .Set(s => s.CreatedUser, userName)
                    .Set(s => s.CreatedDate, createdDate)
                    .UpdateAsync(token);

                existing.CreatedUser = userName;
                existing.CreatedDate = createdDate;
            }
            else
            {
                await update.UpdateAsync(token);
            }

            existing.OverrideKind = overrideKind;
            existing.Version = version;

            return existing;
        }

        private Task InsertVersionAsync(EntityAnalysisModelActivationRuleOverride existing, CancellationToken token)
        {
            var mapper = new Mapper(new MapperConfiguration(
                cfg =>
                {
                    cfg.CreateMap<EntityAnalysisModelActivationRuleOverride,
                        EntityAnalysisModelActivationRuleOverrideVersion>();
                }, NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelActivationRuleOverrideVersion>(existing);
            audit.EntityAnalysisModelActivationRuleOverrideId = existing.Id;

            return dbContext.InsertAsync(audit, token: token);
        }
    }
}