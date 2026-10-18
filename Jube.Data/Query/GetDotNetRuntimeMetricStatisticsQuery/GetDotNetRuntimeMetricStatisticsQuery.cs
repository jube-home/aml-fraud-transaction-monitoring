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

namespace Jube.Data.Query.GetDotNetRuntimeMetricStatisticsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Helpers;
    using LinqToDB;
    using Poco;
    using Dto.Payload;

    public class GetDotNetRuntimeMetricStatisticsQuery(DbContext dbContext)
    {
        public async Task<PayloadStatistics> ExecuteAsync(DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var rows = await BuildFilteredQuery(from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["processorCount"] =
                [
                    .. rows.Where(w => w.ProcessorCount.HasValue)
                        .Select(s => (double)s.ProcessorCount!.Value)
                ],
                ["gen0CollectionCount"] =
                [
                    .. rows.Where(w => w.Gen0CollectionCount.HasValue)
                        .Select(s => (double)s.Gen0CollectionCount!.Value)
                ],
                ["gen1CollectionCount"] =
                [
                    .. rows.Where(w => w.Gen1CollectionCount.HasValue)
                        .Select(s => (double)s.Gen1CollectionCount!.Value)
                ],
                ["gen2CollectionCount"] =
                [
                    .. rows.Where(w => w.Gen2CollectionCount.HasValue)
                        .Select(s => (double)s.Gen2CollectionCount!.Value)
                ],
                ["totalAllocatedBytes"] =
                [
                    .. rows.Where(w => w.TotalAllocatedBytes.HasValue)
                        .Select(s => (double)s.TotalAllocatedBytes!.Value)
                ],
                ["heapSizeBytes"] =
                [
                    .. rows.Where(w => w.HeapSizeBytes.HasValue)
                        .Select(s => (double)s.HeapSizeBytes!.Value)
                ],
                ["fragmentedBytes"] =
                [
                    .. rows.Where(w => w.FragmentedBytes.HasValue)
                        .Select(s => (double)s.FragmentedBytes!.Value)
                ],
                ["memoryLoadBytes"] =
                [
                    .. rows.Where(w => w.MemoryLoadBytes.HasValue)
                        .Select(s => (double)s.MemoryLoadBytes!.Value)
                ],
                ["highMemoryLoadThresholdBytes"] =
                [
                    .. rows.Where(w => w.HighMemoryLoadThresholdBytes.HasValue)
                        .Select(s => (double)s.HighMemoryLoadThresholdBytes!.Value)
                ],
                ["workingSetBytes"] =
                [
                    .. rows.Where(w => w.WorkingSetBytes.HasValue)
                        .Select(s => (double)s.WorkingSetBytes!.Value)
                ],
                ["privateMemoryBytes"] =
                [
                    .. rows.Where(w => w.PrivateMemoryBytes.HasValue)
                        .Select(s => (double)s.PrivateMemoryBytes!.Value)
                ],
                ["threadCount"] =
                [
                    .. rows.Where(w => w.ThreadCount.HasValue)
                        .Select(s => (double)s.ThreadCount!.Value)
                ],
                ["threadPoolWorkerThreadsAvailable"] =
                [
                    .. rows.Where(w => w.ThreadPoolWorkerThreadsAvailable.HasValue)
                        .Select(s => (double)s.ThreadPoolWorkerThreadsAvailable!.Value)
                ],
                ["threadPoolWorkerThreadsMax"] =
                [
                    .. rows.Where(w => w.ThreadPoolWorkerThreadsMax.HasValue)
                        .Select(s => (double)s.ThreadPoolWorkerThreadsMax!.Value)
                ],
                ["threadPoolCompletionPortThreadsAvailable"] =
                [
                    .. rows
                        .Where(w => w.ThreadPoolCompletionPortThreadsAvailable.HasValue)
                        .Select(s => (double)s.ThreadPoolCompletionPortThreadsAvailable!.Value)
                ],
                ["threadPoolCompletionPortThreadsMax"] =
                [
                    .. rows
                        .Where(w => w.ThreadPoolCompletionPortThreadsMax.HasValue)
                        .Select(s => (double)s.ThreadPoolCompletionPortThreadsMax!.Value)
                ],
                ["threadPoolQueueLength"] =
                [
                    .. rows.Where(w => w.ThreadPoolQueueLength.HasValue)
                        .Select(s => (double)s.ThreadPoolQueueLength!.Value)
                ],
                ["cpuTimeMicroseconds"] =
                [
                    .. rows.Where(w => w.CpuTimeMicroseconds.HasValue)
                        .Select(s => (double)s.CpuTimeMicroseconds!.Value)
                ],
                ["runtimeAvailableMemoryBytes"] =
                [
                    .. rows.Where(w => w.RuntimeAvailableMemoryBytes.HasValue)
                        .Select(s => (double)s.RuntimeAvailableMemoryBytes!.Value)
                ],
                ["runtimeCommittedMemoryBytes"] =
                [
                    .. rows.Where(w => w.RuntimeCommittedMemoryBytes.HasValue)
                        .Select(s => (double)s.RuntimeCommittedMemoryBytes!.Value)
                ],
                ["containerCpuLimitCores"] =
                [
                    .. rows.Where(w => w.ContainerCpuLimitCores.HasValue)
                        .Select(s => s.ContainerCpuLimitCores!.Value)
                ],
                ["containerCpuUsageMicroseconds"] =
                [
                    .. rows.Where(w => w.ContainerCpuUsageMicroseconds.HasValue)
                        .Select(s => (double)s.ContainerCpuUsageMicroseconds!.Value)
                ],
                ["containerCpuThrottledPeriods"] =
                [
                    .. rows.Where(w => w.ContainerCpuThrottledPeriods.HasValue)
                        .Select(s => (double)s.ContainerCpuThrottledPeriods!.Value)
                ],
                ["containerCpuThrottledMicroseconds"] =
                [
                    .. rows.Where(w => w.ContainerCpuThrottledMicroseconds.HasValue)
                        .Select(s => (double)s.ContainerCpuThrottledMicroseconds!.Value)
                ],
                ["containerMemoryLimitBytes"] =
                [
                    .. rows.Where(w => w.ContainerMemoryLimitBytes.HasValue)
                        .Select(s => (double)s.ContainerMemoryLimitBytes!.Value)
                ],
                ["containerMemoryUsageBytes"] =
                [
                    .. rows.Where(w => w.ContainerMemoryUsageBytes.HasValue)
                        .Select(s => (double)s.ContainerMemoryUsageBytes!.Value)
                ],
                ["gcPauseTimeMicroseconds"] =
                [
                    .. rows.Where(w => w.GcPauseTimeMicroseconds.HasValue)
                        .Select(s => (double)s.GcPauseTimeMicroseconds!.Value)
                ],
                ["lockContentionCount"] =
                [
                    .. rows.Where(w => w.LockContentionCount.HasValue)
                        .Select(s => (double)s.LockContentionCount!.Value)
                ]
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
    }
}