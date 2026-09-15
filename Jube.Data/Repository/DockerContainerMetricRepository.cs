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
    public class DockerContainerMetricRepository(DbContext dbContext)
    {
        public async Task<DockerContainerMetric> InsertAsync(DockerContainerMetric model,
            CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        private IQueryable<DockerContainerMetric> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.DockerContainerMetric.AsQueryable();

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
                    (w.Image != null && w.Image.ToLower().Contains(lowerSearch)) ||
                    (w.State != null && w.State.ToLower().Contains(lowerSearch)) ||
                    (w.Status != null && w.Status.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<DockerContainerMetric> ApplySort(
            IQueryable<DockerContainerMetric> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                "name" => query.OrderByField(o => o.Name, descending),
                "image" => query.OrderByField(o => o.Image, descending),
                "state" => query.OrderByField(o => o.State, descending),
                "status" => query.OrderByField(o => o.Status, descending),
                "restartCount" => query.OrderByField(o => o.RestartCount, descending),
                "exitCode" => query.OrderByField(o => o.ExitCode, descending),
                "healthStatus" => query.OrderByField(o => o.HealthStatus, descending),
                "cpuUsagePercent" => query.OrderByField(o => o.CpuUsagePercent, descending),
                "onlineCpus" => query.OrderByField(o => o.OnlineCpus, descending),
                "memoryUsageBytes" => query.OrderByField(o => o.MemoryUsageBytes, descending),
                "memoryLimitBytes" => query.OrderByField(o => o.MemoryLimitBytes, descending),
                "memoryPercent" => query.OrderByField(o => o.MemoryPercent, descending),
                "networkRxBytes" => query.OrderByField(o => o.NetworkRxBytes, descending),
                "networkTxBytes" => query.OrderByField(o => o.NetworkTxBytes, descending),
                "blockReadBytes" => query.OrderByField(o => o.BlockReadBytes, descending),
                "blockWriteBytes" => query.OrderByField(o => o.BlockWriteBytes, descending),
                "pidsCurrent" => query.OrderByField(o => o.PidsCurrent, descending),
                "pidsLimit" => query.OrderByField(o => o.PidsLimit, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }

        public async Task<IEnumerable<DockerContainerMetric>> GetLastAsync(int take, DateTime? from, DateTime? to,
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
            var query = BuildFilteredQuery(from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap);

            var rows = await query.ToListAsync(token).ConfigureAwait(false);

            var restartCount = rows.Select(s => (double)s.RestartCount.GetValueOrDefault())
                .ToArray();

            var cpuUsagePercent = rows.Where(w => w.CpuUsagePercent.HasValue)
                .Select(s => s.CpuUsagePercent!.Value).ToArray();

            var onlineCpus = rows.Where(w => w.OnlineCpus.HasValue)
                .Select(s => (double)s.OnlineCpus!.Value).ToArray();

            var memoryUsageBytes = rows.Where(w => w.MemoryUsageBytes.HasValue)
                .Select(s => (double)s.MemoryUsageBytes!.Value).ToArray();

            var memoryLimitBytes = rows.Where(w => w.MemoryLimitBytes.HasValue)
                .Select(s => (double)s.MemoryLimitBytes!.Value).ToArray();

            var memoryPercent = rows.Where(w => w.MemoryPercent.HasValue)
                .Select(s => s.MemoryPercent!.Value).ToArray();

            var networkRxBytes = rows.Where(w => w.NetworkRxBytes.HasValue)
                .Select(s => (double)s.NetworkRxBytes!.Value).ToArray();

            var networkTxBytes = rows.Where(w => w.NetworkTxBytes.HasValue)
                .Select(s => (double)s.NetworkTxBytes!.Value).ToArray();

            var blockReadBytes = rows.Where(w => w.BlockReadBytes.HasValue)
                .Select(s => (double)s.BlockReadBytes!.Value).ToArray();

            var blockWriteBytes = rows.Where(w => w.BlockWriteBytes.HasValue)
                .Select(s => (double)s.BlockWriteBytes!.Value).ToArray();

            var pidsCurrent = rows.Where(w => w.PidsCurrent.HasValue)
                .Select(s => (double)s.PidsCurrent!.Value).ToArray();

            var pidsLimit = rows.Where(w => w.PidsLimit.HasValue)
                .Select(s => (double)s.PidsLimit!.Value).ToArray();

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["restartCount"] = restartCount,
                ["cpuUsagePercent"] = cpuUsagePercent,
                ["onlineCpus"] = onlineCpus,
                ["memoryUsageBytes"] = memoryUsageBytes,
                ["memoryLimitBytes"] = memoryLimitBytes,
                ["memoryPercent"] = memoryPercent,
                ["networkRxBytes"] = networkRxBytes,
                ["networkTxBytes"] = networkTxBytes,
                ["blockReadBytes"] = blockReadBytes,
                ["blockWriteBytes"] = blockWriteBytes,
                ["pidsCurrent"] = pidsCurrent,
                ["pidsLimit"] = pidsLimit
            });
        }
    }
}