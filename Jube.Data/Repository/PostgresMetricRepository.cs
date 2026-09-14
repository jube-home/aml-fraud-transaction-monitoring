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
using Jube.Data.Helpers;
using Jube.Data.Poco;
using Jube.Dto.Payload;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class PostgresMetricRepository(DbContext dbContext)
    {
        public async Task<PostgresMetric> InsertAsync(PostgresMetric model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<PostgresMetric>> GetLastAsync(int take, DateTime? from, DateTime? to,
            string search, double? samplePercentage, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, search, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(DateTime? from, DateTime? to, string search, double? samplePercentage,
            CancellationToken token = default)
        {
            return BuildFilteredQuery(from, to, search, samplePercentage).CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var rows = await BuildFilteredQuery(from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["activeConnections"] = rows.Where(w => w.ActiveConnections.HasValue)
                    .Select(s => (double)s.ActiveConnections!.Value).ToArray(),
                ["transactionsCommitted"] = rows.Where(w => w.TransactionsCommitted.HasValue)
                    .Select(s => (double)s.TransactionsCommitted!.Value).ToArray(),
                ["transactionsRolledBack"] = rows.Where(w => w.TransactionsRolledBack.HasValue)
                    .Select(s => (double)s.TransactionsRolledBack!.Value).ToArray(),
                ["blocksRead"] = rows.Where(w => w.BlocksRead.HasValue)
                    .Select(s => (double)s.BlocksRead!.Value).ToArray(),
                ["blocksHit"] = rows.Where(w => w.BlocksHit.HasValue)
                    .Select(s => (double)s.BlocksHit!.Value).ToArray(),
                ["cacheHitRatioPercent"] = rows.Where(w => w.CacheHitRatioPercent.HasValue)
                    .Select(s => s.CacheHitRatioPercent!.Value).ToArray(),
                ["rowsReturned"] = rows.Where(w => w.RowsReturned.HasValue)
                    .Select(s => (double)s.RowsReturned!.Value).ToArray(),
                ["rowsFetched"] = rows.Where(w => w.RowsFetched.HasValue)
                    .Select(s => (double)s.RowsFetched!.Value).ToArray(),
                ["rowsInserted"] = rows.Where(w => w.RowsInserted.HasValue)
                    .Select(s => (double)s.RowsInserted!.Value).ToArray(),
                ["rowsUpdated"] = rows.Where(w => w.RowsUpdated.HasValue)
                    .Select(s => (double)s.RowsUpdated!.Value).ToArray(),
                ["rowsDeleted"] = rows.Where(w => w.RowsDeleted.HasValue)
                    .Select(s => (double)s.RowsDeleted!.Value).ToArray(),
                ["deadlocks"] = rows.Where(w => w.Deadlocks.HasValue)
                    .Select(s => (double)s.Deadlocks!.Value).ToArray(),
                ["tempFilesCreated"] = rows.Where(w => w.TempFilesCreated.HasValue)
                    .Select(s => (double)s.TempFilesCreated!.Value).ToArray(),
                ["tempBytesWritten"] = rows.Where(w => w.TempBytesWritten.HasValue)
                    .Select(s => (double)s.TempBytesWritten!.Value).ToArray(),
                ["conflicts"] = rows.Where(w => w.Conflicts.HasValue)
                    .Select(s => (double)s.Conflicts!.Value).ToArray(),
                ["replicationLagSeconds"] = rows.Where(w => w.ReplicationLagSeconds.HasValue)
                    .Select(s => s.ReplicationLagSeconds!.Value).ToArray(),
                ["replicaCount"] = rows.Where(w => w.ReplicaCount.HasValue)
                    .Select(s => (double)s.ReplicaCount!.Value).ToArray(),
                ["databaseSizeBytes"] = rows.Where(w => w.DatabaseSizeBytes.HasValue)
                    .Select(s => (double)s.DatabaseSizeBytes!.Value).ToArray(),
                ["longestRunningQuerySeconds"] = rows.Where(w => w.LongestRunningQuerySeconds.HasValue)
                    .Select(s => s.LongestRunningQuerySeconds!.Value).ToArray(),
                ["waitingBackends"] = rows.Where(w => w.WaitingBackends.HasValue)
                    .Select(s => (double)s.WaitingBackends!.Value).ToArray(),
                ["walBytesGenerated"] = rows.Where(w => w.WalBytesGenerated.HasValue)
                    .Select(s => (double)s.WalBytesGenerated!.Value).ToArray()
            });
        }

        private IQueryable<PostgresMetric> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.PostgresMetric.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.CreatedDate <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(w => w.Instance.ToLower().Contains(search.ToLower()));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<PostgresMetric> ApplySort(IQueryable<PostgresMetric> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "instance" => query.OrderByField(o => o.Instance, descending),
                "activeConnections" => query.OrderByField(o => o.ActiveConnections, descending),
                "transactionsCommitted" => query.OrderByField(o => o.TransactionsCommitted, descending),
                "transactionsRolledBack" => query.OrderByField(o => o.TransactionsRolledBack, descending),
                "blocksRead" => query.OrderByField(o => o.BlocksRead, descending),
                "blocksHit" => query.OrderByField(o => o.BlocksHit, descending),
                "cacheHitRatioPercent" => query.OrderByField(o => o.CacheHitRatioPercent, descending),
                "rowsReturned" => query.OrderByField(o => o.RowsReturned, descending),
                "rowsFetched" => query.OrderByField(o => o.RowsFetched, descending),
                "rowsInserted" => query.OrderByField(o => o.RowsInserted, descending),
                "rowsUpdated" => query.OrderByField(o => o.RowsUpdated, descending),
                "rowsDeleted" => query.OrderByField(o => o.RowsDeleted, descending),
                "deadlocks" => query.OrderByField(o => o.Deadlocks, descending),
                "tempFilesCreated" => query.OrderByField(o => o.TempFilesCreated, descending),
                "tempBytesWritten" => query.OrderByField(o => o.TempBytesWritten, descending),
                "conflicts" => query.OrderByField(o => o.Conflicts, descending),
                "isInRecovery" => query.OrderByField(o => o.IsInRecovery, descending),
                "replicationLagSeconds" => query.OrderByField(o => o.ReplicationLagSeconds, descending),
                "replicaCount" => query.OrderByField(o => o.ReplicaCount, descending),
                "databaseSizeBytes" => query.OrderByField(o => o.DatabaseSizeBytes, descending),
                "longestRunningQuerySeconds" => query.OrderByField(o => o.LongestRunningQuerySeconds, descending),
                "waitingBackends" => query.OrderByField(o => o.WaitingBackends, descending),
                "walBytesGenerated" => query.OrderByField(o => o.WalBytesGenerated, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}