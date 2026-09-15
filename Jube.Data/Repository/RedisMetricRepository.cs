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
    public class RedisMetricRepository(DbContext dbContext)
    {
        public async Task<RedisMetric> InsertAsync(RedisMetric model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<RedisMetric>> GetLastAsync(int take, DateTime? from, DateTime? to,
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
                ["connectedClients"] = rows.Where(w => w.ConnectedClients.HasValue)
                    .Select(s => (double)s.ConnectedClients!.Value).ToArray(),
                ["blockedClients"] = rows.Where(w => w.BlockedClients.HasValue)
                    .Select(s => (double)s.BlockedClients!.Value).ToArray(),
                ["usedMemoryBytes"] = rows.Where(w => w.UsedMemoryBytes.HasValue)
                    .Select(s => (double)s.UsedMemoryBytes!.Value).ToArray(),
                ["usedMemoryRssBytes"] = rows.Where(w => w.UsedMemoryRssBytes.HasValue)
                    .Select(s => (double)s.UsedMemoryRssBytes!.Value).ToArray(),
                ["maxMemoryBytes"] = rows.Where(w => w.MaxMemoryBytes.HasValue)
                    .Select(s => (double)s.MaxMemoryBytes!.Value).ToArray(),
                ["instantaneousOpsPerSecond"] = rows.Where(w => w.InstantaneousOpsPerSecond.HasValue)
                    .Select(s => (double)s.InstantaneousOpsPerSecond!.Value).ToArray(),
                ["totalCommandsProcessed"] = rows.Where(w => w.TotalCommandsProcessed.HasValue)
                    .Select(s => (double)s.TotalCommandsProcessed!.Value).ToArray(),
                ["totalConnectionsReceived"] = rows.Where(w => w.TotalConnectionsReceived.HasValue)
                    .Select(s => (double)s.TotalConnectionsReceived!.Value).ToArray(),
                ["keyspaceHits"] = rows.Where(w => w.KeyspaceHits.HasValue)
                    .Select(s => (double)s.KeyspaceHits!.Value).ToArray(),
                ["keyspaceMisses"] = rows.Where(w => w.KeyspaceMisses.HasValue)
                    .Select(s => (double)s.KeyspaceMisses!.Value).ToArray(),
                ["hitRatePercent"] = rows.Where(w => w.HitRatePercent.HasValue)
                    .Select(s => s.HitRatePercent!.Value).ToArray(),
                ["evictedKeys"] = rows.Where(w => w.EvictedKeys.HasValue)
                    .Select(s => (double)s.EvictedKeys!.Value).ToArray(),
                ["expiredKeys"] = rows.Where(w => w.ExpiredKeys.HasValue)
                    .Select(s => (double)s.ExpiredKeys!.Value).ToArray(),
                ["connectedReplicas"] = rows.Where(w => w.ConnectedReplicas.HasValue)
                    .Select(s => (double)s.ConnectedReplicas!.Value).ToArray(),
                ["uptimeSeconds"] = rows.Where(w => w.UptimeSeconds.HasValue)
                    .Select(s => (double)s.UptimeSeconds!.Value).ToArray(),
                ["totalKeys"] = rows.Where(w => w.TotalKeys.HasValue)
                    .Select(s => (double)s.TotalKeys!.Value).ToArray(),
                ["memoryFragmentationRatio"] = rows.Where(w => w.MemoryFragmentationRatio.HasValue)
                    .Select(s => s.MemoryFragmentationRatio!.Value).ToArray(),
                ["rdbLastSaveAgeSeconds"] = rows.Where(w => w.RdbLastSaveAgeSeconds.HasValue)
                    .Select(s => (double)s.RdbLastSaveAgeSeconds!.Value).ToArray()
            });
        }

        private IQueryable<RedisMetric> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.RedisMetric.AsQueryable();

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

        private static IOrderedQueryable<RedisMetric> ApplySort(IQueryable<RedisMetric> query, string sortField,
            string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "instance" => query.OrderByField(o => o.Instance, descending),
                "connectedClients" => query.OrderByField(o => o.ConnectedClients, descending),
                "blockedClients" => query.OrderByField(o => o.BlockedClients, descending),
                "usedMemoryBytes" => query.OrderByField(o => o.UsedMemoryBytes, descending),
                "usedMemoryRssBytes" => query.OrderByField(o => o.UsedMemoryRssBytes, descending),
                "maxMemoryBytes" => query.OrderByField(o => o.MaxMemoryBytes, descending),
                "instantaneousOpsPerSecond" => query.OrderByField(o => o.InstantaneousOpsPerSecond, descending),
                "totalCommandsProcessed" => query.OrderByField(o => o.TotalCommandsProcessed, descending),
                "totalConnectionsReceived" => query.OrderByField(o => o.TotalConnectionsReceived, descending),
                "keyspaceHits" => query.OrderByField(o => o.KeyspaceHits, descending),
                "keyspaceMisses" => query.OrderByField(o => o.KeyspaceMisses, descending),
                "hitRatePercent" => query.OrderByField(o => o.HitRatePercent, descending),
                "evictedKeys" => query.OrderByField(o => o.EvictedKeys, descending),
                "expiredKeys" => query.OrderByField(o => o.ExpiredKeys, descending),
                "connectedReplicas" => query.OrderByField(o => o.ConnectedReplicas, descending),
                "masterReplicationOffset" => query.OrderByField(o => o.MasterReplicationOffset, descending),
                "uptimeSeconds" => query.OrderByField(o => o.UptimeSeconds, descending),
                "totalKeys" => query.OrderByField(o => o.TotalKeys, descending),
                "memoryFragmentationRatio" => query.OrderByField(o => o.MemoryFragmentationRatio, descending),
                "rdbLastSaveAgeSeconds" => query.OrderByField(o => o.RdbLastSaveAgeSeconds, descending),
                "aofEnabled" => query.OrderByField(o => o.AofEnabled, descending),
                "lastAofRewriteDate" => query.OrderByField(o => o.LastAofRewriteDate, descending),
                "lastBgSaveDate" => query.OrderByField(o => o.LastBgSaveDate, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}