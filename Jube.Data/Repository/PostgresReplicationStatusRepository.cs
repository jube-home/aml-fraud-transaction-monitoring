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
    public class PostgresReplicationStatusRepository(DbContext dbContext)
    {
        public async Task<PostgresReplicationStatus> InsertAsync(PostgresReplicationStatus model,
            CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<PostgresReplicationStatus>> GetLastAsync(int take, DateTime? from,
            DateTime? to, string search, double? samplePercentage, string sortField, string sortDirection,
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
                ["writeLagSeconds"] = rows.Where(w => w.WriteLagSeconds.HasValue)
                    .Select(s => s.WriteLagSeconds!.Value).ToArray(),
                ["flushLagSeconds"] = rows.Where(w => w.FlushLagSeconds.HasValue)
                    .Select(s => s.FlushLagSeconds!.Value).ToArray(),
                ["replayLagSeconds"] = rows.Where(w => w.ReplayLagSeconds.HasValue)
                    .Select(s => s.ReplayLagSeconds!.Value).ToArray(),
                ["syncPriority"] = rows.Where(w => w.SyncPriority.HasValue)
                    .Select(s => (double)s.SyncPriority!.Value).ToArray()
            });
        }

        private IQueryable<PostgresReplicationStatus> BuildFilteredQuery(DateTime? from, DateTime? to,
            string search, double? samplePercentage)
        {
            var query = dbContext.PostgresReplicationStatus.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.OccurredDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.OccurredDate <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(w =>
                    (w.UserName != null && w.UserName.ToLower().Contains(lowerSearch)) ||
                    (w.ApplicationName != null && w.ApplicationName.ToLower().Contains(lowerSearch)) ||
                    (w.ClientAddress != null && w.ClientAddress.ToLower().Contains(lowerSearch)) ||
                    (w.State != null && w.State.ToLower().Contains(lowerSearch)) ||
                    (w.SyncState != null && w.SyncState.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<PostgresReplicationStatus> ApplySort(
            IQueryable<PostgresReplicationStatus> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "userName" => query.OrderByField(o => o.UserName, descending),
                "applicationName" => query.OrderByField(o => o.ApplicationName, descending),
                "clientAddress" => query.OrderByField(o => o.ClientAddress, descending),
                "state" => query.OrderByField(o => o.State, descending),
                "sentLsn" => query.OrderByField(o => o.SentLsn, descending),
                "writeLsn" => query.OrderByField(o => o.WriteLsn, descending),
                "flushLsn" => query.OrderByField(o => o.FlushLsn, descending),
                "replayLsn" => query.OrderByField(o => o.ReplayLsn, descending),
                "writeLagSeconds" => query.OrderByField(o => o.WriteLagSeconds, descending),
                "flushLagSeconds" => query.OrderByField(o => o.FlushLagSeconds, descending),
                "replayLagSeconds" => query.OrderByField(o => o.ReplayLagSeconds, descending),
                "syncState" => query.OrderByField(o => o.SyncState, descending),
                "syncPriority" => query.OrderByField(o => o.SyncPriority, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}