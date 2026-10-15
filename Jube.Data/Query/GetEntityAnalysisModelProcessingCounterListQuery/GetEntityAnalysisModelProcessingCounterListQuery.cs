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

namespace Jube.Data.Query.GetEntityAnalysisModelProcessingCounterListQuery
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

    public class GetEntityAnalysisModelProcessingCounterListQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<GetEntityAnalysisModelProcessingCounterListQueryDto> ExecuteAsync(
            bool includeAllTenants, int take, DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage);

            var rows = await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .Select(s => new EntityAnalysisModelProcessingCounterRow(
                    s.EntityAnalysisModelGuid, s.EntityAnalysisModel.Name, s.CreatedDate, s.Instance,
                    s.ModelInvoke, s.GatewayMatch, s.ResponseElevation, s.ResponseElevationSum,
                    s.ActivationWatcher, s.ResponseElevationLimit, s.ModelTotalResponseTime,
                    s.MinResponseTimeMicroseconds, s.MaxResponseTimeMicroseconds, s.ArchiveWalPendingCount))
                .ToListAsync(token).ConfigureAwait(false);

            var total = await BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage)
                .CountAsync(token).ConfigureAwait(false);

            var statisticsRows = await BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            var modelInvoke = statisticsRows.Select(s => (double)s.ModelInvoke.GetValueOrDefault())
                .ToArray();

            var gatewayMatch = statisticsRows.Select(s => (double)s.GatewayMatch.GetValueOrDefault())
                .ToArray();

            var responseElevation = statisticsRows.Select(s => (double)s.ResponseElevation.GetValueOrDefault())
                .ToArray();

            var responseElevationSum = statisticsRows
                .Where(w => w.ResponseElevationSum.HasValue)
                .Select(s => s.ResponseElevationSum!.Value)
                .ToArray();

            var activationWatcher = statisticsRows
                .Where(w => w.ActivationWatcher.HasValue)
                .Select(s => s.ActivationWatcher!.Value)
                .ToArray();

            var responseElevationLimit = statisticsRows
                .Select(s => (double)s.ResponseElevationLimit.GetValueOrDefault())
                .ToArray();

            var modelTotalResponseTime = statisticsRows.Select(s => (double)s.ModelTotalResponseTime)
                .ToArray();

            var minResponseTimeMicroseconds = statisticsRows
                .Select(s => (double)s.MinResponseTimeMicroseconds.GetValueOrDefault())
                .ToArray();

            var maxResponseTimeMicroseconds = statisticsRows
                .Select(s => (double)s.MaxResponseTimeMicroseconds.GetValueOrDefault())
                .ToArray();

            var archiveWalPendingCount = statisticsRows
                .Select(s => (double)s.ArchiveWalPendingCount.GetValueOrDefault())
                .ToArray();

            var statistics = SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["modelInvoke"] = modelInvoke,
                ["gatewayMatch"] = gatewayMatch,
                ["responseElevation"] = responseElevation,
                ["responseElevationSum"] = responseElevationSum,
                ["activationWatcher"] = activationWatcher,
                ["responseElevationLimit"] = responseElevationLimit,
                ["modelTotalResponseTime"] = modelTotalResponseTime,
                ["minResponseTimeMicroseconds"] = minResponseTimeMicroseconds,
                ["maxResponseTimeMicroseconds"] = maxResponseTimeMicroseconds,
                ["archiveWalPendingCount"] = archiveWalPendingCount
            });

            return new GetEntityAnalysisModelProcessingCounterListQueryDto
            {
                Rows = rows,
                Total = total,
                Statistics = statistics
            };
        }

        private IQueryable<EntityAnalysisModelProcessingCounter> BuildFilteredQuery(
            bool includeAllTenants, DateTime? from, DateTime? to, string search, double? samplePercentage)
        {
            var query = dbContext.EntityAnalysisModelProcessingCounter
                .Where(w => includeAllTenants || w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId);

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
                query = query.Where(w => w.EntityAnalysisModel.Name.ToLower().Contains(search.ToLower()));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<EntityAnalysisModelProcessingCounter> ApplySort(
            IQueryable<EntityAnalysisModelProcessingCounter> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortField switch
            {
                "name" => query.OrderByField(o => o.EntityAnalysisModel.Name, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "modelInvoke" => query.OrderByField(o => o.ModelInvoke, descending),
                "gatewayMatch" => query.OrderByField(o => o.GatewayMatch, descending),
                "responseElevation" => query.OrderByField(o => o.ResponseElevation, descending),
                "responseElevationSum" => query.OrderByField(o => o.ResponseElevationSum, descending),
                "activationWatcher" => query.OrderByField(o => o.ActivationWatcher, descending),
                "responseElevationLimit" => query.OrderByField(o => o.ResponseElevationLimit, descending),
                "modelTotalResponseTime" => query.OrderByField(o => o.ModelTotalResponseTime, descending),
                "minResponseTimeMicroseconds" => query.OrderByField(o => o.MinResponseTimeMicroseconds, descending),
                "maxResponseTimeMicroseconds" => query.OrderByField(o => o.MaxResponseTimeMicroseconds, descending),
                "archiveWalPendingCount" => query.OrderByField(o => o.ArchiveWalPendingCount, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}