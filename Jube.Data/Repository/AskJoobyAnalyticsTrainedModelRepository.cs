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
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class AskJoobyAnalyticsTrainedModelRepository(DbContext dbContext, int tenantRegistryId, string userName)
    {
        public Task<AskJoobyAnalyticsTrainedModel> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.AskJoobyAnalyticsTrainedModel.FirstOrDefaultAsync(w =>
                w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0, token);
        }

        public Task<AskJoobyAnalyticsTrainedModel> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.AskJoobyAnalyticsTrainedModel.FirstOrDefaultAsync(w =>
                w.Guid == guid && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0, token);
        }

        public async Task<IEnumerable<AskJoobyAnalyticsTrainedModel>> ListAsync(int? askJoobyAnalyticsInstanceId = null,
            CancellationToken token = default)
        {
            return await dbContext.AskJoobyAnalyticsTrainedModel
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && (askJoobyAnalyticsInstanceId == null
                                || w.AskJoobyAnalyticsInstanceId == askJoobyAnalyticsInstanceId))
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AskJoobyAnalyticsTrainedModel>> ListByCorrelationKeyAsync(string correlationKey,
            CancellationToken token = default)
        {
            return await dbContext.AskJoobyAnalyticsTrainedModel
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && w.CorrelationKey == correlationKey)
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AskJoobyAnalyticsTrainedModel>> ListActiveAsync(
            CancellationToken token = default)
        {
            return await dbContext.AskJoobyAnalyticsTrainedModel
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && w.Active == 1
                            && w.ActiveVersionId != null)
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AskJoobyAnalyticsTrainedModel>> ListActiveByEntityAnalysisModelIdOrderByIdAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return await (from trainedModel in dbContext.AskJoobyAnalyticsTrainedModel
                    join instance in dbContext.AskJoobyAnalyticsInstance
                        on trainedModel.AskJoobyAnalyticsInstanceId equals instance.Id
                    where trainedModel.TenantRegistryId == tenantRegistryId
                          && trainedModel.Deleted == 0
                          && trainedModel.Active == 1
                          && trainedModel.ActiveVersionId != null
                          && instance.TenantRegistryId == tenantRegistryId
                          && instance.Deleted == 0
                          && instance.Active == 1
                          && instance.EntityAnalysisModelId == entityAnalysisModelId
                    orderby trainedModel.Id
                    select trainedModel)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<AskJoobyAnalyticsTrainedModel> InsertAsync(AskJoobyAnalyticsTrainedModel model,
            CancellationToken token = default)
        {
            var instanceVisible = await dbContext.AskJoobyAnalyticsInstance.AnyAsync(w =>
                    w.Id == model.AskJoobyAnalyticsInstanceId
                    && w.TenantRegistryId == tenantRegistryId
                    && w.Deleted == 0, token)
                .ConfigureAwait(false);

            if (!instanceVisible)
            {
                throw new KeyNotFoundException();
            }

            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.TenantRegistryId = tenantRegistryId;
            model.Active = 0;
            model.ActiveVersionId = null;
            model.CreatedUser = userName ?? model.CreatedUser;
            model.CreatedDate = DateTime.UtcNow;
            model.UpdatedDate = null;
            model.Version = 1;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task UpdateAsync(int id, string name, string correlationKey, CancellationToken token = default)
        {
            var records = await dbContext.AskJoobyAnalyticsTrainedModel
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Name, name)
                .Set(s => s.CorrelationKey, correlationKey)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .Set(s => s.Version, s => s.Version + 1)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task SetActiveVersionAsync(int id, int versionId, CancellationToken token = default)
        {
            var records = await dbContext.AskJoobyAnalyticsTrainedModel
                .Where(w => w.Id == id
                            && w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && dbContext.AskJoobyAnalyticsTrainedModelVersion.Any(v =>
                                v.Id == versionId
                                && v.AskJoobyAnalyticsTrainedModelId == w.Id
                                && v.TenantRegistryId == tenantRegistryId))
                .Set(s => s.ActiveVersionId, versionId)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .Set(s => s.Version, s => s.Version + 1)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task SetActiveAsync(int id, bool active, CancellationToken token = default)
        {
            var existing = await GetByIdAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            if (active && existing.ActiveVersionId == null)
            {
                throw new InvalidOperationException("A model without an active version cannot be activated.");
            }

            await dbContext.AskJoobyAnalyticsTrainedModel
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Active, (byte)(active ? 1 : 0))
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .Set(s => s.Version, s => s.Version + 1)
                .UpdateAsync(token).ConfigureAwait(false);
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.AskJoobyAnalyticsTrainedModel
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Active, (byte)0)
                .Set(s => s.Deleted, (byte)1)
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}