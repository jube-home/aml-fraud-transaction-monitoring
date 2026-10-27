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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetApprovedEntityQuery;
using Jube.Data.Repository.Models;
using LinqToDB;
using Microsoft.Extensions.Logging.Abstractions;
using Jube.Data.Helpers;

namespace Jube.Data.Repository
{
    public class AskJoobyAnalyticsInstanceRepository(DbContext dbContext, int tenantRegistryId, string userName)
        : VersionedApprovalRepository<AskJoobyAnalyticsInstance, AskJoobyAnalyticsInstanceVersion>(dbContext,
            EntityApprovalKind.AskJoobyAnalyticsInstance,
            nameof(AskJoobyAnalyticsInstanceVersion.AskJoobyAnalyticsInstanceId), tenantRegistryId, userName)
    {
        public Task<AskJoobyAnalyticsInstance> GetByIdAsync(int id, CancellationToken token = default)
        {
            return DbContext.AskJoobyAnalyticsInstance.FirstOrDefaultAsync(w =>
                w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0, token);
        }

        public Task<AskJoobyAnalyticsInstance> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return DbContext.AskJoobyAnalyticsInstance.FirstOrDefaultAsync(w =>
                w.Guid == guid && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0, token);
        }

        public async Task<IEnumerable<AskJoobyAnalyticsInstance>> ListAsync(int? entityAnalysisModelId = null,
            bool includeDeleted = false, CancellationToken token = default)
        {
            return await DbContext.AskJoobyAnalyticsInstance
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && (includeDeleted || w.Deleted == 0)
                            && (entityAnalysisModelId == null || w.EntityAnalysisModelId == entityAnalysisModelId))
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<AskJoobyAnalyticsInstance> InsertAsync(AskJoobyAnalyticsInstance model,
            CancellationToken token = default)
        {
            await EnsureModelVisibleAsync(model.EntityAnalysisModelId, token).ConfigureAwait(false);

            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.TenantRegistryId = tenantRegistryId;
            model.CreatedUser = UserName ?? model.CreatedUser;
            model.CreatedDate = DateTime.UtcNow;
            model.UpdatedDate = null;
            model.Version = 1;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Id = await DbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task<AskJoobyAnalyticsInstance> UpdateAsync(AskJoobyAnalyticsInstance model,
            CancellationToken token = default)
        {
            await EnsureModelVisibleAsync(model.EntityAnalysisModelId, token).ConfigureAwait(false);

            var existing = await DbContext.AskJoobyAnalyticsInstance
                .FirstOrDefaultAsync(w => w.Id == model.Id && w.TenantRegistryId == tenantRegistryId
                                                           && w.Deleted == 0, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var activeChanged = existing.Active != model.Active;

            if (activeChanged)
            {
                await InsertVersionSnapshotAsync(existing, token).ConfigureAwait(false);
            }

            var update = DbContext.AskJoobyAnalyticsInstance
                .Where(w => w.Id == model.Id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Name, model.Name)
                .Set(s => s.EntityAnalysisModelId, model.EntityAnalysisModelId)
                .Set(s => s.Active, model.Active)
                .Set(s => s.ResponsePayload, model.ResponsePayload)
                .Set(s => s.ReportTable, model.ReportTable)
                .Set(s => s.UpdatedDate, DateTime.UtcNow);

            if (activeChanged)
            {
                update = update.Set(s => s.Version, s => s.Version + 1);
            }

            var records = await update.UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }

            return await GetByIdAsync(model.Id, token).ConfigureAwait(false);
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var existing = await DbContext.AskJoobyAnalyticsInstance
                .FirstOrDefaultAsync(w => w.Id == id && w.TenantRegistryId == tenantRegistryId
                                                     && w.Deleted == 0, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            await InsertVersionSnapshotAsync(existing, token).ConfigureAwait(false);

            var records = await DbContext.AskJoobyAnalyticsInstance
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Active, (byte)0)
                .Set(s => s.Deleted, (byte)1)
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, UserName)
                .Set(s => s.Version, s => s.Version + 1)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }

            var deletedState = ToVersionSnapshot(existing);
            deletedState.Deleted = 1;
            deletedState.DeletedDate = DateTime.UtcNow;
            deletedState.DeletedUser = UserName;
            deletedState.Version = existing.Version + 1;

            await DbContext.InsertAsync(deletedState, token: token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AskJoobyAnalyticsInstance>> GetApprovedByEntityAnalysisModelIdOrderByIdAsync(
            int entityAnalysisModelId, int approvalsRequired = 1, CancellationToken token = default)
        {
            var current = await DbContext.AskJoobyAnalyticsInstance
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            return await new GetApprovedEntityQuery<AskJoobyAnalyticsInstance, AskJoobyAnalyticsInstanceVersion>(
                    DbContext,
                    EntityApprovalKind.AskJoobyAnalyticsInstance,
                    nameof(AskJoobyAnalyticsInstanceVersion.AskJoobyAnalyticsInstanceId), tenantRegistryId)
                .ExecuteAsync(current, approvalsRequired, token).ConfigureAwait(false);
        }

        protected override Task<AskJoobyAnalyticsInstance> LoadForMutationAsync(int id, CancellationToken token)
        {
            return DbContext.AskJoobyAnalyticsInstance
                .FirstOrDefaultAsync(w => w.Id == id && w.TenantRegistryId == tenantRegistryId, token);
        }

        private static readonly IMapper versionMapper = new Mapper(new MapperConfiguration(
            cfg => { cfg.CreateMap<AskJoobyAnalyticsInstance, AskJoobyAnalyticsInstanceVersion>(); },
            NullLoggerFactory.Instance));

        private static AskJoobyAnalyticsInstanceVersion ToVersionSnapshot(AskJoobyAnalyticsInstance existing)
        {
            var audit = versionMapper.Map<AskJoobyAnalyticsInstanceVersion>(existing);
            audit.AskJoobyAnalyticsInstanceId = existing.Id;

            return audit;
        }

        private Task InsertVersionSnapshotAsync(AskJoobyAnalyticsInstance existing, CancellationToken token)
        {
            return DbContext.InsertAsync(ToVersionSnapshot(existing), token: token);
        }

        private async Task EnsureModelVisibleAsync(int? entityAnalysisModelId, CancellationToken token)
        {
            if (entityAnalysisModelId == null)
            {
                return;
            }

            if (!await EntityAnalysisModelParentGuard
                    .IsVisibleAsync(DbContext, tenantRegistryId, entityAnalysisModelId.Value, token)
                    .ConfigureAwait(false))
            {
                throw new KeyNotFoundException();
            }
        }
    }
}