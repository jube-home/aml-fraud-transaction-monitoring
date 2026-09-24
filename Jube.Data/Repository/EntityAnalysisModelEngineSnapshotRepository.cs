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
    using Context;
    using LinqToDB;
    using Poco;

    public class EntityAnalysisModelEngineSnapshotRepository(DbContext dbContext)
    {
        public async Task UpsertAsync(string instance, int tenantRegistryId, int entityAnalysisModelId, string json,
            CancellationToken token = default)
        {
            var now = DateTime.UtcNow;
            var updated = await dbContext.EntityAnalysisModelEngineSnapshot
                .Where(w => w.Instance == instance && w.EntityAnalysisModelId == entityAnalysisModelId)
                .Set(s => s.TenantRegistryId, tenantRegistryId)
                .Set(s => s.Json, json)
                .Set(s => s.CreatedDate, now)
                .UpdateAsync(token)
                .ConfigureAwait(false);

            if (updated == 0)
            {
                await dbContext.InsertAsync(new EntityAnalysisModelEngineSnapshot
                {
                    Instance = instance,
                    TenantRegistryId = tenantRegistryId,
                    EntityAnalysisModelId = entityAnalysisModelId,
                    Json = json,
                    CreatedDate = now
                }, token: token).ConfigureAwait(false);
            }
        }

        public Task<List<EntityAnalysisModelEngineSnapshot>> GetByEntityAnalysisModelIdAsync(int tenantRegistryId,
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelEngineSnapshot
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.Instance)
                .ToListAsync(token);
        }
    }
}