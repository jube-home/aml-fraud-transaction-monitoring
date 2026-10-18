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
using Jube.Data.Repository.Models;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class ExhaustiveSearchInstanceTrialInstanceRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;

        public ExhaustiveSearchInstanceTrialInstanceRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public ExhaustiveSearchInstanceTrialInstanceRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public ExhaustiveSearchInstanceTrialInstanceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<ExhaustiveSearchInstanceTrialInstance> InsertAsync(
            ExhaustiveSearchInstanceTrialInstance model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task<IEnumerable<ExhaustiveSearchInstanceTrialInstance>>
            GetByExhaustiveSearchInstanceIdOrderByIdAsync(
                int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstanceTrialInstance.Where(w
                    => w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId)
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<List<ExhaustiveSearchInstanceTrialInstance>> GetByExhaustiveSearchInstanceIdOrderByIdDescAsync(
            int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstance.Deleted == 0 || w.ExhaustiveSearchInstance.Deleted == null)
                    && (w.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                        || !tenantRegistryId.HasValue))
                .OrderByDescending(o => o.Id)
                .ToListAsync(token);
        }

        public Task<ExhaustiveSearchInstanceTrialInstance> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(w =>
                    w.Id == id
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstance.Deleted == 0 || w.ExhaustiveSearchInstance.Deleted == null)
                    && (w.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                        || !tenantRegistryId.HasValue))
                .FirstOrDefaultAsync(token);
        }

        public Task UpdateOutcomeAsync(int id, AdvancedAnalyticsModelKind kind, double score, string json,
            string metricsJson, string variablesJson, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(d => d.Id == id)
                .Set(s => s.KindId, kind)
                .Set(s => s.Score, score)
                .Set(s => s.Json, json)
                .Set(s => s.MetricsJson, metricsJson)
                .Set(s => s.VariablesJson, variablesJson)
                .UpdateAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(d =>
                    d.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}