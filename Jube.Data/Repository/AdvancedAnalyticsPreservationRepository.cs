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
    public class AdvancedAnalyticsPreservationRepository(DbContext dbContext, int tenantRegistryId, string userName)
    {
        private const int GuidChunkSize = 500;

        public Task<List<AdvancedAnalyticsInstance>> ListInstancesAsync(CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsInstance
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task<List<AdvancedAnalyticsTrainedModel>> ListTrainedModelsAsync(
            CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainedModel
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task<List<AdvancedAnalyticsTrainingRun>> ListTrainingRunsAsync(
            CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task SoftDeleteAllOfTenantAsync(CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            await dbContext.AdvancedAnalyticsTrainedModel
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Active, (byte)0)
                .Set(s => s.Deleted, (byte)1)
                .Set(s => s.DeletedDate, now)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token).ConfigureAwait(false);

            await dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Deleted, (byte)1)
                .Set(s => s.DeletedDate, now)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token).ConfigureAwait(false);

            await dbContext.AdvancedAnalyticsInstance
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.Deleted == 0)
                .Set(s => s.Active, (byte)0)
                .Set(s => s.Deleted, (byte)1)
                .Set(s => s.DeletedDate, now)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token).ConfigureAwait(false);
        }

        public Task<HashSet<Guid>> InstanceGuidsInUseAsync(IReadOnlyCollection<Guid> guids,
            CancellationToken token = default)
        {
            return GuidsInUseAsync(guids, chunk => dbContext.AdvancedAnalyticsInstance
                .Where(w => chunk.Contains(w.Guid)).Select(s => s.Guid).ToListAsync(token));
        }

        public Task<HashSet<Guid>> TrainedModelGuidsInUseAsync(IReadOnlyCollection<Guid> guids,
            CancellationToken token = default)
        {
            return GuidsInUseAsync(guids, chunk => dbContext.AdvancedAnalyticsTrainedModel
                .Where(w => chunk.Contains(w.Guid)).Select(s => s.Guid).ToListAsync(token));
        }

        public Task<HashSet<Guid>> TrainedModelVersionGuidsInUseAsync(IReadOnlyCollection<Guid> guids,
            CancellationToken token = default)
        {
            return GuidsInUseAsync(guids, chunk => dbContext.AdvancedAnalyticsTrainedModelVersion
                .Where(w => chunk.Contains(w.Guid)).Select(s => s.Guid).ToListAsync(token));
        }

        public Task<HashSet<Guid>> TrainingRunGuidsInUseAsync(IReadOnlyCollection<Guid> guids,
            CancellationToken token = default)
        {
            return GuidsInUseAsync(guids, chunk => dbContext.AdvancedAnalyticsTrainingRun
                .Where(w => chunk.Contains(w.Guid)).Select(s => s.Guid).ToListAsync(token));
        }

        public async Task<int> InsertInstanceAsync(AdvancedAnalyticsInstance model, int? entityAnalysisModelId,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.EntityAnalysisModelId = entityAnalysisModelId;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.UpdatedDate = null;
            model.Version = 1;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<int> InsertTrainedModelAsync(AdvancedAnalyticsTrainedModel model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Active = 0;
            model.ActiveVersionId = null;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.UpdatedDate = null;
            model.Version = 1;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<int> InsertTrainedModelVersionAsync(AdvancedAnalyticsTrainedModelVersion model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<int> InsertTrainingRunAsync(AdvancedAnalyticsTrainingRun model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<int> InsertStepAsync(AdvancedAnalyticsTrainingRunStep model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<int> InsertTrialAsync(AdvancedAnalyticsTrainingRunTrial model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<long> InsertIterationAsync(AdvancedAnalyticsTrainingRunIteration model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<int> InsertVariableAsync(AdvancedAnalyticsTrainingRunVariable model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<long> InsertMetricAsync(AdvancedAnalyticsTrainingRunMetric model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task<long> InsertCurvePointAsync(AdvancedAnalyticsTrainingRunCurvePoint model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

            return model.Id;
        }

        public async Task SetTrainedModelActiveVersionAsync(int trainedModelId, int versionId, bool active,
            CancellationToken token = default)
        {
            var records = await dbContext.AdvancedAnalyticsTrainedModel
                .Where(w => w.Id == trainedModelId
                            && w.TenantRegistryId == tenantRegistryId
                            && w.Deleted == 0
                            && dbContext.AdvancedAnalyticsTrainedModelVersion.Any(v =>
                                v.Id == versionId
                                && v.AdvancedAnalyticsTrainedModelId == w.Id
                                && v.TenantRegistryId == tenantRegistryId))
                .Set(s => s.ActiveVersionId, versionId)
                .Set(s => s.Active, (byte)(active ? 1 : 0))
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        private static async Task<HashSet<Guid>> GuidsInUseAsync(IReadOnlyCollection<Guid> guids,
            Func<List<Guid>, Task<List<Guid>>> query)
        {
            var inUse = new HashSet<Guid>();

            foreach (var chunk in guids.Where(w => w != Guid.Empty).Distinct().Chunk(GuidChunkSize))
            {
                foreach (var guid in await query([.. chunk]).ConfigureAwait(false))
                {
                    inUse.Add(guid);
                }
            }

            return inUse;
        }
    }
}