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
    using LinqToDB;
    using Microsoft.Extensions.Logging.Abstractions;
    using Poco;

    public class EntityAnalysisModelActivationRuleOverrideRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelActivationRuleOverrideRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelActivationRuleOverrideRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelActivationRuleOverrideRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
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

            await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(d => d.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            await InsertVersionAsync(existing, token);
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

            await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => w.Id == existing.Id)
                .Set(s => s.DeleteExpiryDate, deleteExpiryDate)
                .Set(s => s.Version, version)
                .UpdateAsync(token);

            await InsertVersionAsync(existing, token);

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

            await dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => w.Id == existing.Id)
                .Set(s => s.OverrideKind, overrideKind)
                .Set(s => s.Version, version)
                .UpdateAsync(token);

            await InsertVersionAsync(existing, token);

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