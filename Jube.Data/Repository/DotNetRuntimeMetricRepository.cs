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
    public class DotNetRuntimeMetricRepository(DbContext dbContext)
    {
        public async Task<DotNetRuntimeMetric> InsertAsync(DotNetRuntimeMetric model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<DotNetRuntimeMetric>> GetLastAsync(int take, DateTime? from, DateTime? to,
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
                ["processorCount"] = rows.Where(w => w.ProcessorCount.HasValue)
                    .Select(s => (double)s.ProcessorCount!.Value).ToArray(),
                ["gen0CollectionCount"] = rows.Where(w => w.Gen0CollectionCount.HasValue)
                    .Select(s => (double)s.Gen0CollectionCount!.Value).ToArray(),
                ["gen1CollectionCount"] = rows.Where(w => w.Gen1CollectionCount.HasValue)
                    .Select(s => (double)s.Gen1CollectionCount!.Value).ToArray(),
                ["gen2CollectionCount"] = rows.Where(w => w.Gen2CollectionCount.HasValue)
                    .Select(s => (double)s.Gen2CollectionCount!.Value).ToArray(),
                ["totalAllocatedBytes"] = rows.Where(w => w.TotalAllocatedBytes.HasValue)
                    .Select(s => (double)s.TotalAllocatedBytes!.Value).ToArray(),
                ["heapSizeBytes"] = rows.Where(w => w.HeapSizeBytes.HasValue)
                    .Select(s => (double)s.HeapSizeBytes!.Value).ToArray(),
                ["fragmentedBytes"] = rows.Where(w => w.FragmentedBytes.HasValue)
                    .Select(s => (double)s.FragmentedBytes!.Value).ToArray(),
                ["memoryLoadBytes"] = rows.Where(w => w.MemoryLoadBytes.HasValue)
                    .Select(s => (double)s.MemoryLoadBytes!.Value).ToArray(),
                ["highMemoryLoadThresholdBytes"] = rows.Where(w => w.HighMemoryLoadThresholdBytes.HasValue)
                    .Select(s => (double)s.HighMemoryLoadThresholdBytes!.Value).ToArray(),
                ["workingSetBytes"] = rows.Where(w => w.WorkingSetBytes.HasValue)
                    .Select(s => (double)s.WorkingSetBytes!.Value).ToArray(),
                ["privateMemoryBytes"] = rows.Where(w => w.PrivateMemoryBytes.HasValue)
                    .Select(s => (double)s.PrivateMemoryBytes!.Value).ToArray(),
                ["threadCount"] = rows.Where(w => w.ThreadCount.HasValue)
                    .Select(s => (double)s.ThreadCount!.Value).ToArray(),
                ["threadPoolWorkerThreadsAvailable"] = rows.Where(w => w.ThreadPoolWorkerThreadsAvailable.HasValue)
                    .Select(s => (double)s.ThreadPoolWorkerThreadsAvailable!.Value).ToArray(),
                ["threadPoolWorkerThreadsMax"] = rows.Where(w => w.ThreadPoolWorkerThreadsMax.HasValue)
                    .Select(s => (double)s.ThreadPoolWorkerThreadsMax!.Value).ToArray(),
                ["threadPoolCompletionPortThreadsAvailable"] = rows
                    .Where(w => w.ThreadPoolCompletionPortThreadsAvailable.HasValue)
                    .Select(s => (double)s.ThreadPoolCompletionPortThreadsAvailable!.Value).ToArray(),
                ["threadPoolCompletionPortThreadsMax"] = rows
                    .Where(w => w.ThreadPoolCompletionPortThreadsMax.HasValue)
                    .Select(s => (double)s.ThreadPoolCompletionPortThreadsMax!.Value).ToArray(),
                ["threadPoolQueueLength"] = rows.Where(w => w.ThreadPoolQueueLength.HasValue)
                    .Select(s => (double)s.ThreadPoolQueueLength!.Value).ToArray(),
                ["cpuTimeMicroseconds"] = rows.Where(w => w.CpuTimeMicroseconds.HasValue)
                    .Select(s => (double)s.CpuTimeMicroseconds!.Value).ToArray(),
                ["runtimeAvailableMemoryBytes"] = rows.Where(w => w.RuntimeAvailableMemoryBytes.HasValue)
                    .Select(s => (double)s.RuntimeAvailableMemoryBytes!.Value).ToArray(),
                ["runtimeCommittedMemoryBytes"] = rows.Where(w => w.RuntimeCommittedMemoryBytes.HasValue)
                    .Select(s => (double)s.RuntimeCommittedMemoryBytes!.Value).ToArray(),
                ["containerCpuLimitCores"] = rows.Where(w => w.ContainerCpuLimitCores.HasValue)
                    .Select(s => s.ContainerCpuLimitCores!.Value).ToArray(),
                ["containerCpuUsageMicroseconds"] = rows.Where(w => w.ContainerCpuUsageMicroseconds.HasValue)
                    .Select(s => (double)s.ContainerCpuUsageMicroseconds!.Value).ToArray(),
                ["containerCpuThrottledPeriods"] = rows.Where(w => w.ContainerCpuThrottledPeriods.HasValue)
                    .Select(s => (double)s.ContainerCpuThrottledPeriods!.Value).ToArray(),
                ["containerCpuThrottledMicroseconds"] = rows.Where(w => w.ContainerCpuThrottledMicroseconds.HasValue)
                    .Select(s => (double)s.ContainerCpuThrottledMicroseconds!.Value).ToArray(),
                ["containerMemoryLimitBytes"] = rows.Where(w => w.ContainerMemoryLimitBytes.HasValue)
                    .Select(s => (double)s.ContainerMemoryLimitBytes!.Value).ToArray(),
                ["containerMemoryUsageBytes"] = rows.Where(w => w.ContainerMemoryUsageBytes.HasValue)
                    .Select(s => (double)s.ContainerMemoryUsageBytes!.Value).ToArray(),
                ["gcPauseTimeMicroseconds"] = rows.Where(w => w.GcPauseTimeMicroseconds.HasValue)
                    .Select(s => (double)s.GcPauseTimeMicroseconds!.Value).ToArray(),
                ["lockContentionCount"] = rows.Where(w => w.LockContentionCount.HasValue)
                    .Select(s => (double)s.LockContentionCount!.Value).ToArray()
            });
        }

        private IQueryable<DotNetRuntimeMetric> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.DotNetRuntimeMetric.AsQueryable();

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

        private static IOrderedQueryable<DotNetRuntimeMetric> ApplySort(IQueryable<DotNetRuntimeMetric> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "instance" => query.OrderByField(o => o.Instance, descending),
                "processorCount" => query.OrderByField(o => o.ProcessorCount, descending),
                "gen0CollectionCount" => query.OrderByField(o => o.Gen0CollectionCount, descending),
                "gen1CollectionCount" => query.OrderByField(o => o.Gen1CollectionCount, descending),
                "gen2CollectionCount" => query.OrderByField(o => o.Gen2CollectionCount, descending),
                "totalAllocatedBytes" => query.OrderByField(o => o.TotalAllocatedBytes, descending),
                "heapSizeBytes" => query.OrderByField(o => o.HeapSizeBytes, descending),
                "fragmentedBytes" => query.OrderByField(o => o.FragmentedBytes, descending),
                "memoryLoadBytes" => query.OrderByField(o => o.MemoryLoadBytes, descending),
                "highMemoryLoadThresholdBytes" => query.OrderByField(o => o.HighMemoryLoadThresholdBytes,
                    descending),
                "workingSetBytes" => query.OrderByField(o => o.WorkingSetBytes, descending),
                "privateMemoryBytes" => query.OrderByField(o => o.PrivateMemoryBytes, descending),
                "threadCount" => query.OrderByField(o => o.ThreadCount, descending),
                "threadPoolWorkerThreadsAvailable" => query.OrderByField(o => o.ThreadPoolWorkerThreadsAvailable,
                    descending),
                "threadPoolWorkerThreadsMax" => query.OrderByField(o => o.ThreadPoolWorkerThreadsMax, descending),
                "threadPoolCompletionPortThreadsAvailable" => query.OrderByField(
                    o => o.ThreadPoolCompletionPortThreadsAvailable, descending),
                "threadPoolCompletionPortThreadsMax" => query.OrderByField(
                    o => o.ThreadPoolCompletionPortThreadsMax, descending),
                "threadPoolQueueLength" => query.OrderByField(o => o.ThreadPoolQueueLength, descending),
                "cpuTimeMicroseconds" => query.OrderByField(o => o.CpuTimeMicroseconds, descending),
                "runtimeAvailableMemoryBytes" => query.OrderByField(o => o.RuntimeAvailableMemoryBytes, descending),
                "runtimeCommittedMemoryBytes" => query.OrderByField(o => o.RuntimeCommittedMemoryBytes, descending),
                "containerCpuLimitCores" => query.OrderByField(o => o.ContainerCpuLimitCores, descending),
                "containerCpuUsageMicroseconds" => query.OrderByField(o => o.ContainerCpuUsageMicroseconds,
                    descending),
                "containerCpuThrottledPeriods" => query.OrderByField(o => o.ContainerCpuThrottledPeriods,
                    descending),
                "containerCpuThrottledMicroseconds" => query.OrderByField(o => o.ContainerCpuThrottledMicroseconds,
                    descending),
                "containerMemoryLimitBytes" => query.OrderByField(o => o.ContainerMemoryLimitBytes, descending),
                "containerMemoryUsageBytes" => query.OrderByField(o => o.ContainerMemoryUsageBytes, descending),
                "gcPauseTimeMicroseconds" => query.OrderByField(o => o.GcPauseTimeMicroseconds, descending),
                "lockContentionCount" => query.OrderByField(o => o.LockContentionCount, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}