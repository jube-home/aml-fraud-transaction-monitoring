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
    using LinqToDB.Async;
    using Poco;

    public class GraphPatternRepository(DbContext dbContext)
    {
        public Task<GraphPattern> GetByIdAsync(int tenantRegistryId, long id, CancellationToken token = default)
        {
            return dbContext.GraphPattern.FirstOrDefaultAsync(p => p.TenantRegistryId == tenantRegistryId && p.Id == id, token);
        }

        public Task<List<GraphPattern>> ListAsync(int tenantRegistryId, byte? stateId, byte? kindId, int limit,
            CancellationToken token = default)
        {
            return dbContext.GraphPattern.Where(p => p.TenantRegistryId == tenantRegistryId
                                                     && (stateId == null || p.StateId == stateId)
                                                     && (kindId == null || p.KindId == kindId))
                .OrderByDescending(p => p.LastDetectedDate).ThenByDescending(p => p.Score).Take(limit).ToListAsync(token);
        }

        public Task<List<GraphPattern>> GetByNodeAsync(int tenantRegistryId, long nodeId, CancellationToken token = default)
        {
            var patternIds = dbContext.GraphPatternNode.Where(m => m.GraphNodeId == nodeId).Select(m => m.GraphPatternId);

            return dbContext.GraphPattern.Where(p => p.TenantRegistryId == tenantRegistryId && patternIds.Contains(p.Id)
                                                     && p.StateId != 3)
                .OrderByDescending(p => p.LastDetectedDate).ToListAsync(token);
        }

        public Task<List<GraphPatternNode>> GetMembersAsync(long patternId, CancellationToken token = default)
        {
            return dbContext.GraphPatternNode.Where(m => m.GraphPatternId == patternId).ToListAsync(token);
        }

        public async Task<long> UpsertAsync(GraphPattern pattern, IReadOnlyList<(long NodeId, string Role)> members,
            CancellationToken token = default)
        {
            var existing = await dbContext.GraphPattern.FirstOrDefaultAsync(p => p.TenantRegistryId == pattern.TenantRegistryId
                && p.KindId == pattern.KindId && p.AnchorNodeId == pattern.AnchorNodeId && p.WindowFrom == pattern.WindowFrom,
                token);

            if (existing != null)
            {
                await dbContext.GraphPattern.Where(p => p.Id == existing.Id)
                    .Set(p => p.Score, pattern.Score)
                    .Set(p => p.WindowTo, pattern.WindowTo)
                    .Set(p => p.Parameters, pattern.Parameters)
                    .Set(p => p.ArchiveIds, pattern.ArchiveIds)
                    .Set(p => p.MemberCount, pattern.MemberCount)
                    .Set(p => p.LastDetectedDate, pattern.LastDetectedDate)
                    .UpdateAsync(token);

                return existing.Id;
            }

            pattern.Id = await dbContext.InsertWithInt64IdentityAsync(pattern, token: token);

            foreach (var (nodeId, role) in members)
            {
                await dbContext.InsertAsync(new GraphPatternNode { GraphPatternId = pattern.Id, GraphNodeId = nodeId, Role = role },
                    token: token);
            }

            return pattern.Id;
        }

        public Task<int> DecideAsync(int tenantRegistryId, long id, byte stateId, string userName, int? caseId,
            CancellationToken token = default)
        {
            return dbContext.GraphPattern.Where(p => p.TenantRegistryId == tenantRegistryId && p.Id == id)
                .Set(p => p.StateId, stateId)
                .Set(p => p.DecidedUser, userName)
                .Set(p => p.DecidedDate, DateTime.UtcNow)
                .Set(p => p.CaseId, p => caseId ?? p.CaseId)
                .UpdateAsync(token);
        }
    }
}
