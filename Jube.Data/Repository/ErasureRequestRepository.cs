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
    using LinqToDB.Data;
    using Poco;

    public class ErasureRequestRepository(DbContext dbContext)
    {
        public Task<ErasureRequest> GetAsync(int tenantRegistryId, Guid guid, CancellationToken token = default)
        {
            return dbContext.ErasureRequest.FirstOrDefaultAsync(w => w.TenantRegistryId == tenantRegistryId
                                                                      && w.Guid == guid, token);
        }

        public Task<List<ErasureRequest>> ListAsync(int tenantRegistryId, int take, CancellationToken token = default)
        {
            return dbContext.ErasureRequest.Where(w => w.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id).Take(take).ToListAsync(token);
        }

        public Task<int> CountOpenForSubjectAsync(int tenantRegistryId, string keyName, string keyValue,
            CancellationToken token = default)
        {
            var pending = (short)ErasureState.Pending;
            var approved = (short)ErasureState.Approved;

            return dbContext.ErasureRequest.CountAsync(w => w.TenantRegistryId == tenantRegistryId
                                                            && w.KeyName == keyName && w.KeyValue == keyValue
                                                            && (w.StateId == pending || w.StateId == approved), token);
        }

        public async Task<ErasureRequest> InsertAsync(ErasureRequest model, CancellationToken token = default)
        {
            model.Guid = Guid.NewGuid();
            model.RequestedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public Task<int> DecideAsync(long id, ErasureState from, ErasureState to, string user, string note,
            CancellationToken token = default)
        {
            var fromId = (short)from;

            return dbContext.ErasureRequest.Where(w => w.Id == id && w.StateId == fromId)
                .Set(s => s.StateId, (short)to)
                .Set(s => s.DecidedUser, user)
                .Set(s => s.DecidedDate, DateTime.UtcNow)
                .Set(s => s.DecisionNote, note)
                .UpdateAsync(token);
        }

        public async Task<ErasureRequest> ClaimNextApprovedAsync(CancellationToken token = default)
        {
            var ids = await dbContext.QueryToListAsync<long>(
                "SELECT \"Id\" FROM \"ErasureRequest\" WHERE \"StateId\" = 2 ORDER BY \"Id\" LIMIT 1 FOR UPDATE SKIP LOCKED",
                token).ConfigureAwait(false);

            if (ids.Count == 0)
            {
                return null;
            }

            var id = ids[0];

            return await dbContext.ErasureRequest.FirstAsync(w => w.Id == id, token).ConfigureAwait(false);
        }

        public Task<int> CompleteAsync(long id, ErasureState to, ErasureRefusalReason? refusal, int? found,
            int? archiveRemoved, int? nodesRemoved, int? edgesRemoved, int? payloadsNulled,
            CancellationToken token = default)
        {
            var approved = (short)ErasureState.Approved;
            short? refusalId = refusal is null ? null : (short)refusal.Value;

            return dbContext.ErasureRequest.Where(w => w.Id == id && w.StateId == approved)
                .Set(s => s.StateId, (short)to)
                .Set(s => s.RefusalReasonId, refusalId)
                .Set(s => s.ArchiveRowsFound, found)
                .Set(s => s.ArchiveRowsRemoved, archiveRemoved)
                .Set(s => s.GraphNodesRemoved, nodesRemoved)
                .Set(s => s.GraphEdgesRemoved, edgesRemoved)
                .Set(s => s.CasePayloadsNulled, payloadsNulled)
                .Set(s => s.ExecutedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
