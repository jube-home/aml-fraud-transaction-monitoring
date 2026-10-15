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

namespace Jube.Data.Query.GetEntityAnalysisModelAsynchronousQueueBalanceListQuery
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

    public class GetEntityAnalysisModelAsynchronousQueueBalanceListQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<GetEntityAnalysisModelAsynchronousQueueBalanceListQueryDto> ExecuteAsync(
            bool includeAllTenants, int take, DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage);

            var rows = await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .Select(s => new EntityAnalysisModelAsynchronousQueueBalanceRow(
                    s.EntityAnalysisModelGuid, s.EntityAnalysisModel.Name, s.CreatedDate, s.Archive,
                    s.ActivationWatcher, s.Instance))
                .ToListAsync(token).ConfigureAwait(false);

            var total = await BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage)
                .CountAsync(token).ConfigureAwait(false);

            var statisticsRows = await BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            var archive = statisticsRows.Select(s => (double)s.Archive.GetValueOrDefault())
                .ToArray();
            var activationWatcher = statisticsRows.Select(s => (double)s.ActivationWatcher.GetValueOrDefault())
                .ToArray();

            var statistics = SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["archive"] = archive,
                ["activationWatcher"] = activationWatcher
            });

            return new GetEntityAnalysisModelAsynchronousQueueBalanceListQueryDto
            {
                Rows = rows,
                Total = total,
                Statistics = statistics
            };
        }

        private IQueryable<EntityAnalysisModelAsynchronousQueueBalance> BuildFilteredQuery(
            bool includeAllTenants, DateTime? from, DateTime? to, string search, double? samplePercentage)
        {
            var query = dbContext.EntityAnalysisModelAsynchronousQueueBalance
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

        private static IOrderedQueryable<EntityAnalysisModelAsynchronousQueueBalance> ApplySort(
            IQueryable<EntityAnalysisModelAsynchronousQueueBalance> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortField switch
            {
                "name" => query.OrderByField(o => o.EntityAnalysisModel.Name, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "archive" => query.OrderByField(o => o.Archive, descending),
                "activationWatcher" => query.OrderByField(o => o.ActivationWatcher, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}