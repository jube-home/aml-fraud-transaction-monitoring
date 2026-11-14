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
    using Dto.Options;
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;

    public class EntityRiskRatingRequestRepository(DbContext dbContext)
    {
        public Task<EntityRiskRatingRequest> GetAsync(int tenantRegistryId, Guid guid,
            CancellationToken token = default)
        {
            return dbContext.EntityRiskRatingRequest.FirstOrDefaultAsync(w => w.TenantRegistryId == tenantRegistryId
                                                                              && w.Guid == guid, token);
        }

        public Task<List<EntityRiskRatingRequest>> ListPendingAsync(int tenantRegistryId, int take,
            CancellationToken token = default)
        {
            var pending = (short)EntityRiskRequestState.Pending;

            return dbContext.EntityRiskRatingRequest
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.StateId == pending)
                .OrderBy(o => o.Id).Take(take).ToListAsync(token);
        }

        public Task<int> CountPendingForKeyAsync(int tenantRegistryId, string keyName, string keyValue,
            CancellationToken token = default)
        {
            var pending = (short)EntityRiskRequestState.Pending;

            return dbContext.EntityRiskRatingRequest.CountAsync(w => w.TenantRegistryId == tenantRegistryId
                                                                     && w.KeyName == keyName
                                                                     && w.KeyValue == keyValue
                                                                     && w.StateId == pending, token);
        }

        public async Task<EntityRiskRatingRequest> InsertAsync(EntityRiskRatingRequest model,
            CancellationToken token = default)
        {
            model.Guid = Guid.NewGuid();
            model.RequestedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public Task<int> SetResultingVersionAsync(long id, int version, CancellationToken token = default)
        {
            return dbContext.EntityRiskRatingRequest.Where(w => w.Id == id)
                .Set(s => s.ResultingVersion, (int?)version).UpdateAsync(token);
        }

        public Task<int> DecideAsync(long id, EntityRiskRequestState to, string user, string note,
            int? resultingVersion, CancellationToken token = default)
        {
            var pending = (short)EntityRiskRequestState.Pending;

            return dbContext.EntityRiskRatingRequest.Where(w => w.Id == id && w.StateId == pending)
                .Set(s => s.StateId, (short)to)
                .Set(s => s.DecidedUser, user)
                .Set(s => s.DecidedDate, DateTime.UtcNow)
                .Set(s => s.DecisionNote, note)
                .Set(s => s.ResultingVersion, resultingVersion)
                .UpdateAsync(token);
        }
    }
}
