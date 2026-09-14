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
    public class PatroniMemberStatusRepository(DbContext dbContext)
    {
        public async Task<PatroniMemberStatus> InsertAsync(PatroniMemberStatus model,
            CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<PatroniMemberStatus>> GetLastAsync(int take, DateTime? from, DateTime? to,
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
                ["lagBytes"] = rows.Where(w => w.LagBytes.HasValue)
                    .Select(s => (double)s.LagBytes!.Value).ToArray()
            });
        }

        private IQueryable<PatroniMemberStatus> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.PatroniMemberStatus.AsQueryable();

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
                    (w.Name != null && w.Name.ToLower().Contains(lowerSearch)) ||
                    (w.Host != null && w.Host.ToLower().Contains(lowerSearch)) ||
                    (w.Role != null && w.Role.ToLower().Contains(lowerSearch)) ||
                    (w.State != null && w.State.ToLower().Contains(lowerSearch)) ||
                    (w.Scope != null && w.Scope.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<PatroniMemberStatus> ApplySort(IQueryable<PatroniMemberStatus> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "name" => query.OrderByField(o => o.Name, descending),
                "host" => query.OrderByField(o => o.Host, descending),
                "port" => query.OrderByField(o => o.Port, descending),
                "role" => query.OrderByField(o => o.Role, descending),
                "state" => query.OrderByField(o => o.State, descending),
                "timelineId" => query.OrderByField(o => o.TimelineId, descending),
                "lagBytes" => query.OrderByField(o => o.LagBytes, descending),
                "pendingRestart" => query.OrderByField(o => o.PendingRestart, descending),
                "patroniVersion" => query.OrderByField(o => o.PatroniVersion, descending),
                "scope" => query.OrderByField(o => o.Scope, descending),
                "postgresServerVersion" => query.OrderByField(o => o.PostgresServerVersion, descending),
                "databaseSystemIdentifier" => query.OrderByField(o => o.DatabaseSystemIdentifier, descending),
                "xlogLocationBytes" => query.OrderByField(o => o.XlogLocationBytes, descending),
                "receivedLocationBytes" => query.OrderByField(o => o.ReceivedLocationBytes, descending),
                "replayPaused" => query.OrderByField(o => o.ReplayPaused, descending),
                "clusterUnlocked" => query.OrderByField(o => o.ClusterUnlocked, descending),
                "syncStandby" => query.OrderByField(o => o.SyncStandby, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}