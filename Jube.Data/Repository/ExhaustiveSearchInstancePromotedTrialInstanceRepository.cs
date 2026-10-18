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
    public class ExhaustiveSearchInstancePromotedTrialInstanceRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;

        public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task UpdateActiveAsync(int id, bool active, CancellationToken token = default)
        {
            var existing = await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(d =>
                    (d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id)
                .Select(d => new
                {
                    d.Id,
                    d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId
                })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            await AdvancedAnalyticsRunGuard.InTransactionAsync(dbContext, async () =>
            {
                if (active)
                {
                    await DeactivateAllForExhaustiveSearchInstanceAsync(
                        existing.ExhaustiveSearchInstanceId.GetValueOrDefault(), token).ConfigureAwait(false);

                    await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                        .Where(d => d.Id == id)
                        .Set(s => s.Active, (byte)1)
                        .UpdateAsync(token).ConfigureAwait(false);

                    return;
                }

                await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                    .Where(d => d.Id == id)
                    .Set(s => s.Active, (byte)0)
                    .UpdateAsync(token).ConfigureAwait(false);

                var nextBest = await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                    .Where(d =>
                        d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId ==
                        existing.ExhaustiveSearchInstanceId
                        && d.Id != id
                        && (d.Deleted == 0 || d.Deleted == null))
                    .OrderByDescending(o => o.Score)
                    .Select(d => (int?)d.Id)
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (nextBest.HasValue)
                {
                    await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                        .Where(d => d.Id == nextBest.Value)
                        .Set(s => s.Active, (byte)1)
                        .UpdateAsync(token).ConfigureAwait(false);
                }
            }, token).ConfigureAwait(false);
        }

        public Task DeactivateAllForExhaustiveSearchInstanceAsync(int exhaustiveSearchInstanceId,
            CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(d =>
                    d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId
                    && d.Active == 1)
                .Set(s => s.Active, (byte)0)
                .UpdateAsync(token);
        }

        public async Task<ExhaustiveSearchInstancePromotedTrialInstance> InsertAsync(
            ExhaustiveSearchInstancePromotedTrialInstance model, CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task<IEnumerable<ExhaustiveSearchInstancePromotedTrialInstance>>
            GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                int exhaustiveSearchInstanceTrialInstanceId, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstancePromotedTrialInstance.Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstanceId == exhaustiveSearchInstanceTrialInstanceId)
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<ExhaustiveSearchInstancePromotedTrialInstance> GetLastActiveByExhaustiveSearchInstanceIdAsync(
            int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.Active == 1 && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id ==
                                  exhaustiveSearchInstanceId
                                  && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                                          .EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                                      !tenantRegistryId.HasValue))
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync(token);
        }

        public Task<ExhaustiveSearchInstancePromotedTrialInstance> GetActiveByExhaustiveSearchInstanceIdAsync(
            int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && w.Active == 1
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == 0
                        || w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == null)
                    && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .FirstOrDefaultAsync(token);
        }

        public Task<List<ExhaustiveSearchInstancePromotedTrialInstance>>
            GetByExhaustiveSearchInstanceIdOrderByIdAscAsync(
                int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == 0
                        || w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == null)
                    && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .OrderBy(o => o.Id)
                .ToListAsync(token);
        }

        public Task<List<ExhaustiveSearchInstancePromotedTrialInstance>>
            GetByExhaustiveSearchInstanceIdOrderByIdDescAsync(
                int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == 0
                        || w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == null)
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue))
                .OrderByDescending(o => o.Id)
                .ToListAsync(token);
        }

        public Task<List<int>> GetNonDeletedTrialInstanceIdsAsync(
            IEnumerable<int> exhaustiveSearchInstanceTrialInstanceIds, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstanceId.HasValue
                    && exhaustiveSearchInstanceTrialInstanceIds.Contains(
                        w.ExhaustiveSearchInstanceTrialInstanceId.Value)
                    && (w.Deleted == 0 || w.Deleted == null))
                .Select(s => s.ExhaustiveSearchInstanceTrialInstanceId.Value)
                .Distinct()
                .ToListAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(d =>
                    d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.EntityAnalysisModel
                        .TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}