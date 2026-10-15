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