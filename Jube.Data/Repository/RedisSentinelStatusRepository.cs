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
    public class RedisSentinelStatusRepository(DbContext dbContext)
    {
        public async Task<RedisSentinelStatus> InsertAsync(RedisSentinelStatus model,
            CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<RedisSentinelStatus>> GetLastAsync(int take, DateTime? from, DateTime? to,
            int? entityTypeId, string search, double? samplePercentage, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, entityTypeId, search, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(DateTime? from, DateTime? to, int? entityTypeId, string search,
            double? samplePercentage, CancellationToken token = default)
        {
            return BuildFilteredQuery(from, to, entityTypeId, search, samplePercentage).CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(DateTime? from, DateTime? to, int? entityTypeId,
            string search, double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var rows = await BuildFilteredQuery(from, to, entityTypeId, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["replicationLagSeconds"] = rows.Where(w => w.ReplicationLagSeconds.HasValue)
                    .Select(s => s.ReplicationLagSeconds!.Value).ToArray(),
                ["numSlaves"] = rows.Where(w => w.NumSlaves.HasValue)
                    .Select(s => (double)s.NumSlaves!.Value).ToArray(),
                ["numOtherSentinels"] = rows.Where(w => w.NumOtherSentinels.HasValue)
                    .Select(s => (double)s.NumOtherSentinels!.Value).ToArray(),
                ["quorum"] = rows.Where(w => w.Quorum.HasValue)
                    .Select(s => (double)s.Quorum!.Value).ToArray(),
                ["downAfterMilliseconds"] = rows.Where(w => w.DownAfterMilliseconds.HasValue)
                    .Select(s => (double)s.DownAfterMilliseconds!.Value).ToArray()
            });
        }

        private IQueryable<RedisSentinelStatus> BuildFilteredQuery(DateTime? from, DateTime? to, int? entityTypeId,
            string search, double? samplePercentage)
        {
            var query = dbContext.RedisSentinelStatus.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.OccurredDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.OccurredDate <= to.Value);
            }

            if (entityTypeId.HasValue)
            {
                query = query.Where(w => w.EntityTypeId == entityTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(w =>
                    (w.Name != null && w.Name.ToLower().Contains(lowerSearch)) ||
                    (w.Ip != null && w.Ip.ToLower().Contains(lowerSearch)) ||
                    (w.Flags != null && w.Flags.ToLower().Contains(lowerSearch)) ||
                    (w.MasterLinkStatus != null && w.MasterLinkStatus.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<RedisSentinelStatus> ApplySort(IQueryable<RedisSentinelStatus> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "entityTypeName" => query.OrderByField(o => o.EntityTypeId, descending),
                "name" => query.OrderByField(o => o.Name, descending),
                "ip" => query.OrderByField(o => o.Ip, descending),
                "port" => query.OrderByField(o => o.Port, descending),
                "flags" => query.OrderByField(o => o.Flags, descending),
                "masterLinkStatus" => query.OrderByField(o => o.MasterLinkStatus, descending),
                "masterHost" => query.OrderByField(o => o.MasterHost, descending),
                "masterPort" => query.OrderByField(o => o.MasterPort, descending),
                "slaveReplOffset" => query.OrderByField(o => o.SlaveReplOffset, descending),
                "replicationLagSeconds" => query.OrderByField(o => o.ReplicationLagSeconds, descending),
                "numSlaves" => query.OrderByField(o => o.NumSlaves, descending),
                "numOtherSentinels" => query.OrderByField(o => o.NumOtherSentinels, descending),
                "quorum" => query.OrderByField(o => o.Quorum, descending),
                "downAfterMilliseconds" => query.OrderByField(o => o.DownAfterMilliseconds, descending),
                "runId" => query.OrderByField(o => o.RunId, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}