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
    using LinqToDB.Data;
    using Poco;

    public class GraphEdgeEventRepository(DbContext dbContext)
    {
        public Task BulkCopyAsync(List<GraphEdgeEvent> events, CancellationToken token = default)
        {
            return events.Count == 0
                ? Task.CompletedTask
                : dbContext.BulkCopyAsync(new BulkCopyOptions { KeepIdentity = false, BulkCopyType = BulkCopyType.ProviderSpecific }, events, token);
        }

        public Task<List<GraphEdgeEvent>> GetByEntryGuidsAsync(IReadOnlyCollection<Guid> entryGuids,
            CancellationToken token = default)
        {
            return dbContext.GraphEdgeEvent
                .Where(e => entryGuids.Contains(e.EntityAnalysisModelInstanceEntryGuid) && e.Retracted == 0)
                .ToListAsync(token);
        }

        public Task<List<GraphEdgeEvent>> GetByEdgeAsync(long edgeId, int limit, CancellationToken token = default)
        {
            return dbContext.GraphEdgeEvent.Where(e => e.GraphEdgeId == edgeId && e.Retracted == 0)
                .OrderByDescending(e => e.OccurredDate).Take(limit).ToListAsync(token);
        }

        public Task<List<GraphEdgeEvent>> GetByEdgeInWindowAsync(long edgeId, DateTime from, DateTime to, int limit,
            CancellationToken token = default)
        {
            return dbContext.GraphEdgeEvent.Where(e => e.GraphEdgeId == edgeId && e.Retracted == 0
                                                       && e.OccurredDate >= from && e.OccurredDate <= to)
                .OrderByDescending(e => e.OccurredDate).Take(limit).ToListAsync(token);
        }

        public Task<int> RetractAsync(IReadOnlyCollection<long> eventIds, CancellationToken token = default)
        {
            return eventIds.Count == 0
                ? Task.FromResult(0)
                : dbContext.GraphEdgeEvent.Where(e => eventIds.Contains(e.Id))
                    .Set(e => e.Retracted, (byte)1)
                    .UpdateAsync(token);
        }
    }
}
