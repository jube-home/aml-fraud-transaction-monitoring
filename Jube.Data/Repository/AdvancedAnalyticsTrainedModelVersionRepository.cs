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
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class AdvancedAnalyticsTrainedModelVersionRepository(
        DbContext dbContext,
        int tenantRegistryId,
        string userName)
    {
        private const string LockModelSql = """
                                            SELECT "Id" FROM "AdvancedAnalyticsTrainedModel"
                                            WHERE "Id" = @Id FOR UPDATE
                                            """;

        public Task<AdvancedAnalyticsTrainedModelVersion> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainedModelVersion.FirstOrDefaultAsync(w =>
                w.Id == id && w.TenantRegistryId == tenantRegistryId, token);
        }

        public Task<AdvancedAnalyticsTrainedModelVersion> GetByRunIdAsync(int runId,
            CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainedModelVersion.FirstOrDefaultAsync(w =>
                w.AdvancedAnalyticsTrainingRunId == runId && w.TenantRegistryId == tenantRegistryId, token);
        }

        public Task<AdvancedAnalyticsTrainedModelVersion> GetByModelIdAndVersionNumberAsync(int trainedModelId,
            int versionNumber, CancellationToken token = default)
        {
            return dbContext.AdvancedAnalyticsTrainedModelVersion.FirstOrDefaultAsync(w =>
                w.AdvancedAnalyticsTrainedModelId == trainedModelId
                && w.VersionNumber == versionNumber
                && w.TenantRegistryId == tenantRegistryId, token);
        }

        public Task<AdvancedAnalyticsTrainedModelVersion> GetActiveByModelIdAsync(int trainedModelId,
            CancellationToken token = default)
        {
            return (from v in dbContext.AdvancedAnalyticsTrainedModelVersion
                    join m in dbContext.AdvancedAnalyticsTrainedModel on v.Id equals m.ActiveVersionId
                    where m.Id == trainedModelId
                          && m.TenantRegistryId == tenantRegistryId
                          && m.Deleted == 0
                          && v.TenantRegistryId == tenantRegistryId
                    select v)
                .FirstOrDefaultAsync(token);
        }

        public async Task<IEnumerable<AdvancedAnalyticsTrainedModelVersion>> ListByModelIdAsync(int trainedModelId,
            CancellationToken token = default)
        {
            return await dbContext.AdvancedAnalyticsTrainedModelVersion
                .Where(w => w.AdvancedAnalyticsTrainedModelId == trainedModelId
                            && w.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.VersionNumber)
                .Select(s => new AdvancedAnalyticsTrainedModelVersion
                {
                    Id = s.Id,
                    Guid = s.Guid,
                    TenantRegistryId = s.TenantRegistryId,
                    AdvancedAnalyticsTrainedModelId = s.AdvancedAnalyticsTrainedModelId,
                    VersionNumber = s.VersionNumber,
                    VariablesJson = s.VariablesJson,
                    MetricsJson = s.MetricsJson,
                    CalibratorJson = s.CalibratorJson,
                    AdvancedAnalyticsTrainingRunId = s.AdvancedAnalyticsTrainingRunId,
                    CreatedDate = s.CreatedDate,
                    CreatedUser = s.CreatedUser
                })
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<AdvancedAnalyticsTrainedModelVersion> InsertAsync(
            AdvancedAnalyticsTrainedModelVersion version, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(version.ModelJson))
            {
                throw new ArgumentException("A version requires a model payload.", nameof(version));
            }

            var modelVisible = await dbContext.AdvancedAnalyticsTrainedModel.AnyAsync(w =>
                w.Id == version.AdvancedAnalyticsTrainedModelId
                && w.TenantRegistryId == tenantRegistryId
                && w.Deleted == 0, token).ConfigureAwait(false);

            if (!modelVisible)
            {
                throw new KeyNotFoundException();
            }

            if (version.AdvancedAnalyticsTrainingRunId != null)
            {
                await AdvancedAnalyticsRunGuard.ResolveInstanceIdAsync(dbContext, tenantRegistryId,
                    version.AdvancedAnalyticsTrainingRunId.Value, token).ConfigureAwait(false);
            }

            version.Guid = version.Guid == Guid.Empty ? Guid.NewGuid() : version.Guid;
            version.TenantRegistryId = tenantRegistryId;
            version.CreatedUser = userName ?? version.CreatedUser;
            version.CreatedDate = DateTime.UtcNow;

            await AdvancedAnalyticsRunGuard.InTransactionAsync(dbContext, async () =>
            {
                await new CommandInfo(dbContext, LockModelSql,
                        new DataParameter("Id", version.AdvancedAnalyticsTrainedModelId, DataType.Int32))
                    .ExecuteAsync<int>(token).ConfigureAwait(false);

                var highest = await dbContext.AdvancedAnalyticsTrainedModelVersion
                    .Where(w => w.AdvancedAnalyticsTrainedModelId == version.AdvancedAnalyticsTrainedModelId)
                    .Select(s => (int?)s.VersionNumber)
                    .MaxAsync(token).ConfigureAwait(false);

                version.VersionNumber = (highest ?? 0) + 1;

                version.Id = await dbContext.InsertWithInt32IdentityAsync(version, token: token)
                    .ConfigureAwait(false);
            }, token).ConfigureAwait(false);

            return version;
        }
    }
}