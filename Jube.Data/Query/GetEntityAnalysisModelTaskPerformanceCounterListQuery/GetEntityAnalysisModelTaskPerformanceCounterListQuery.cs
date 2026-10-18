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

namespace Jube.Data.Query.GetEntityAnalysisModelTaskPerformanceCounterListQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Helpers;
    using LinqToDB;
    using Models;
    using Poco;

    public class GetEntityAnalysisModelTaskPerformanceCounterListQuery(DbContext dbContext, int? tenantRegistryId)
    {
        public async Task<GetEntityAnalysisModelTaskPerformanceCounterListQueryDto> ExecuteAsync(
            bool includeAllTenants, int take, DateTime? from, DateTime? to, int? directionId, int? taskTypeId,
            double? samplePercentage, int statisticsCap, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, directionId, taskTypeId, samplePercentage);

            var rows = await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .Select(s => new EntityAnalysisModelTaskPerformanceCounterRow(
                    s.Id, s.EntityAnalysisModelGuid, s.EntityAnalysisModel.Name, s.DirectionId, s.TaskTypeId,
                    s.TotalMicroseconds, s.MinMicroseconds, s.MaxMicroseconds, s.TotalAllocatedBytes,
                    s.MinAllocatedBytes, s.MaxAllocatedBytes, s.InvokeCount, s.CreatedDate, s.Instance))
                .ToListAsync(token).ConfigureAwait(false);

            var total = await BuildFilteredQuery(includeAllTenants, from, to, directionId, taskTypeId,
                samplePercentage).CountAsync(token).ConfigureAwait(false);

            var statisticsRows = await BuildFilteredQuery(includeAllTenants, from, to, directionId, taskTypeId,
                    samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            var totalMicroseconds = statisticsRows.Select(s => (double)s.TotalMicroseconds.GetValueOrDefault())
                .ToArray();

            var minMicroseconds = statisticsRows.Select(s => (double)s.MinMicroseconds.GetValueOrDefault())
                .ToArray();

            var maxMicroseconds = statisticsRows.Select(s => (double)s.MaxMicroseconds.GetValueOrDefault())
                .ToArray();

            var totalAllocatedBytes = statisticsRows.Select(s => (double)s.TotalAllocatedBytes.GetValueOrDefault())
                .ToArray();

            var minAllocatedBytes = statisticsRows.Select(s => (double)s.MinAllocatedBytes.GetValueOrDefault())
                .ToArray();

            var maxAllocatedBytes = statisticsRows.Select(s => (double)s.MaxAllocatedBytes.GetValueOrDefault())
                .ToArray();

            var invokeCount = statisticsRows.Select(s => (double)s.InvokeCount.GetValueOrDefault())
                .ToArray();

            var statistics = SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["totalMicroseconds"] = totalMicroseconds,
                ["minMicroseconds"] = minMicroseconds,
                ["maxMicroseconds"] = maxMicroseconds,
                ["totalAllocatedBytes"] = totalAllocatedBytes,
                ["minAllocatedBytes"] = minAllocatedBytes,
                ["maxAllocatedBytes"] = maxAllocatedBytes,
                ["invokeCount"] = invokeCount
            });

            return new GetEntityAnalysisModelTaskPerformanceCounterListQueryDto
            {
                Rows = rows,
                Total = total,
                Statistics = statistics
            };
        }

        private IQueryable<EntityAnalysisModelTaskPerformanceCounter> BuildFilteredQuery(
            bool includeAllTenants, DateTime? from, DateTime? to, int? directionId, int? taskTypeId,
            double? samplePercentage)
        {
            var query = dbContext.EntityAnalysisModelTaskPerformanceCounter
                .Where(w => includeAllTenants || w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId);

            if (from.HasValue)
            {
                query = query.Where(w => w.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.CreatedDate <= to.Value);
            }

            if (directionId.HasValue)
            {
                query = query.Where(w => w.DirectionId == directionId.Value);
            }

            if (taskTypeId.HasValue)
            {
                query = query.Where(w => w.TaskTypeId == taskTypeId.Value);
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<EntityAnalysisModelTaskPerformanceCounter> ApplySort(
            IQueryable<EntityAnalysisModelTaskPerformanceCounter> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortField switch
            {
                "entityAnalysisModelName" => query.OrderByField(o => o.EntityAnalysisModel.Name, descending),
                "directionName" => query.OrderByField(o => o.DirectionId, descending),
                "taskName" => query.OrderByField(o => o.TaskTypeId, descending),
                "totalMicroseconds" => query.OrderByField(o => o.TotalMicroseconds, descending),
                "minMicroseconds" => query.OrderByField(o => o.MinMicroseconds, descending),
                "maxMicroseconds" => query.OrderByField(o => o.MaxMicroseconds, descending),
                "totalAllocatedBytes" => query.OrderByField(o => o.TotalAllocatedBytes, descending),
                "minAllocatedBytes" => query.OrderByField(o => o.MinAllocatedBytes, descending),
                "maxAllocatedBytes" => query.OrderByField(o => o.MaxAllocatedBytes, descending),
                "invokeCount" => query.OrderByField(o => o.InvokeCount, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}