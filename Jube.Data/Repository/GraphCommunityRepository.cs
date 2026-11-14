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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Poco;

    public class GraphCommunityRepository(DbContext dbContext)
    {
        public Task<GraphCommunity> GetByIdAsync(int tenantRegistryId, long id, CancellationToken token = default)
        {
            return dbContext.GraphCommunity.FirstOrDefaultAsync(c => c.TenantRegistryId == tenantRegistryId && c.Id == id
                                                                     && c.Deleted == 0, token);
        }

        public Task<List<GraphCommunity>> GetTopAsync(int tenantRegistryId, int limit, CancellationToken token = default)
        {
            return dbContext.GraphCommunity.Where(c => c.TenantRegistryId == tenantRegistryId && c.Deleted == 0)
                .OrderByDescending(c => c.ConfirmedCount).ThenByDescending(c => c.Size).Take(limit).ToListAsync(token);
        }

        public Task<int> MarkAllDeletedAsync(int tenantRegistryId, CancellationToken token = default)
        {
            return dbContext.GraphCommunity.Where(c => c.TenantRegistryId == tenantRegistryId && c.Deleted == 0)
                .Set(c => c.Deleted, (byte)1).UpdateAsync(token);
        }

        public async Task<long> InsertAsync(GraphCommunity community, CancellationToken token = default)
        {
            community.Id = await dbContext.InsertWithInt64IdentityAsync(community, token: token);

            return community.Id;
        }

        public Task AssignNodesAsync(IReadOnlyList<long> nodeIds, long communityId, CancellationToken token = default)
        {
            return dbContext.ExecuteAsync("""UPDATE "GraphNode" SET "CommunityId" = @community WHERE "Id" = ANY(@nodes::bigint[])""",
                token, new DataParameter("community", communityId, DataType.Int64), new DataParameter("nodes", nodeIds.ToArray()));
        }
    }
}
