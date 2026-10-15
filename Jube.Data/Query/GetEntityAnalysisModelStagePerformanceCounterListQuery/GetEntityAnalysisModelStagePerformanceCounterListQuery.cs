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

namespace Jube.Data.Query.GetEntityAnalysisModelStagePerformanceCounterListQuery
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

    public class GetEntityAnalysisModelStagePerformanceCounterListQuery(DbContext dbContext, int? tenantRegistryId)
    {
        public async Task<GetEntityAnalysisModelStagePerformanceCounterListQueryDto> ExecuteAsync(
            bool includeAllTenants, int take, DateTime? from, DateTime? to, int? stageId,
            double? samplePercentage, int statisticsCap, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, stageId, samplePercentage);

            var rows = await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .Select(s => new EntityAnalysisModelStagePerformanceCounterRow(
                    s.Id, s.EntityAnalysisModelGuid, s.EntityAnalysisModel.Name, s.StageId, s.TotalMicroseconds,
                    s.MinMicroseconds, s.MaxMicroseconds, s.InvokeCount, s.CreatedDate, s.Instance))
                .ToListAsync(token).ConfigureAwait(false);

            var total = await BuildFilteredQuery(includeAllTenants, from, to, stageId, samplePercentage)
                .CountAsync(token).ConfigureAwait(false);

            var statisticsRows = await BuildFilteredQuery(includeAllTenants, from, to, stageId, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            var totalMicroseconds = statisticsRows.Select(s => (double)s.TotalMicroseconds.GetValueOrDefault())
                .ToArray();

            var minMicroseconds = statisticsRows.Select(s => (double)s.MinMicroseconds.GetValueOrDefault())
                .ToArray();

            var maxMicroseconds = statisticsRows.Select(s => (double)s.MaxMicroseconds.GetValueOrDefault())
                .ToArray();

            var invokeCount = statisticsRows.Select(s => (double)s.InvokeCount.GetValueOrDefault())
                .ToArray();

            var statistics = SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["totalMicroseconds"] = totalMicroseconds,
                ["minMicroseconds"] = minMicroseconds,
                ["maxMicroseconds"] = maxMicroseconds,
                ["invokeCount"] = invokeCount
            });

            return new GetEntityAnalysisModelStagePerformanceCounterListQueryDto
            {
                Rows = rows,
                Total = total,
                Statistics = statistics
            };
        }

        private IQueryable<EntityAnalysisModelStagePerformanceCounter> BuildFilteredQuery(
            bool includeAllTenants, DateTime? from, DateTime? to, int? stageId, double? samplePercentage)
        {
            var query = dbContext.EntityAnalysisModelStagePerformanceCounter
                .Where(w => includeAllTenants || w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId);

            if (from.HasValue)
            {
                query = query.Where(w => w.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.CreatedDate <= to.Value);
            }

            if (stageId.HasValue)
            {
                query = query.Where(w => w.StageId == stageId.Value);
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<EntityAnalysisModelStagePerformanceCounter> ApplySort(
            IQueryable<EntityAnalysisModelStagePerformanceCounter> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortField switch
            {
                "entityAnalysisModelName" => query.OrderByField(o => o.EntityAnalysisModel.Name, descending),
                "stageName" => query.OrderByField(o => o.StageId, descending),
                "totalMicroseconds" => query.OrderByField(o => o.TotalMicroseconds, descending),
                "minMicroseconds" => query.OrderByField(o => o.MinMicroseconds, descending),
                "maxMicroseconds" => query.OrderByField(o => o.MaxMicroseconds, descending),
                "invokeCount" => query.OrderByField(o => o.InvokeCount, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}