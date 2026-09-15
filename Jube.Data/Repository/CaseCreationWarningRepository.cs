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
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class CaseCreationWarningRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;

        public CaseCreationWarningRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseCreationWarningRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task BulkCopyAsync(List<CaseCreationWarning> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }

        private IQueryable<CaseCreationWarning> BuildFilteredQuery(bool includeAllTenants, DateTime? from,
            DateTime? to, int? stageId, string search, double? samplePercentage)
        {
            var query = dbContext.CaseCreationWarning
                .Where(w => includeAllTenants || w.TenantRegistryId == tenantRegistryId);

            if (from.HasValue)
            {
                query = query.Where(w => w.OccurredDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.OccurredDate <= to.Value);
            }

            if (stageId.HasValue)
            {
                query = query.Where(w => w.StageId == stageId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(w => w.CaseKeyValue != null && w.CaseKeyValue.ToLower().Contains(lowerSearch));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<CaseCreationWarning> ApplySort(
            IQueryable<CaseCreationWarning> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                "caseKey" => query.OrderByField(o => o.CaseKey, descending),
                "caseKeyValue" => query.OrderByField(o => o.CaseKeyValue, descending),
                "stageName" => query.OrderByField(o => o.StageId, descending),
                "destination" => query.OrderByField(o => o.Destination, descending),
                "durationMicroseconds" => query.OrderByField(o => o.DurationMicroseconds, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }

        public async Task<IEnumerable<CaseCreationWarning>> GetLastAsync(bool includeAllTenants, int take,
            DateTime? from, DateTime? to, int? stageId, string search, double? samplePercentage,
            string sortField, string sortDirection, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, stageId, search, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(bool includeAllTenants, DateTime? from, DateTime? to, int? stageId,
            string search, double? samplePercentage, CancellationToken token = default)
        {
            return BuildFilteredQuery(includeAllTenants, from, to, stageId, search, samplePercentage)
                .CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(bool includeAllTenants, DateTime? from, DateTime? to,
            int? stageId, string search, double? samplePercentage, int statisticsCap,
            CancellationToken token = default)
        {
            var durationMicroseconds = await BuildFilteredQuery(includeAllTenants, from, to, stageId, search,
                    samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .Select(s => (double)s.DurationMicroseconds.GetValueOrDefault())
                .ToArrayAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["durationMicroseconds"] = durationMicroseconds
            });
        }
    }
}