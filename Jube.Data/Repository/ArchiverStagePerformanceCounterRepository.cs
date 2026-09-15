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
    public class ArchiverStagePerformanceCounterRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;

        public ArchiverStagePerformanceCounterRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public ArchiverStagePerformanceCounterRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ArchiverStagePerformanceCounter> InsertAsync(ArchiverStagePerformanceCounter model,
            CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        private IQueryable<ArchiverStagePerformanceCounter> BuildFilteredQuery(
            bool includeAllTenants, DateTime? from, DateTime? to, int? stageId, double? samplePercentage)
        {
            var query = dbContext.ArchiverStagePerformanceCounter
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

        private static IOrderedQueryable<ArchiverStagePerformanceCounter> ApplySort(
            IQueryable<ArchiverStagePerformanceCounter> query, string sortField, string sortDirection)
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

        public async Task<IEnumerable<ArchiverStagePerformanceCounterRow>> GetLastAsync(
            bool includeAllTenants, int take, DateTime? from, DateTime? to, int? stageId,
            double? samplePercentage, string sortField, string sortDirection, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, stageId, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .Select(s => new ArchiverStagePerformanceCounterRow(
                    s.Id, s.EntityAnalysisModelGuid, s.EntityAnalysisModel.Name, s.StageId, s.TotalMicroseconds,
                    s.MinMicroseconds, s.MaxMicroseconds, s.InvokeCount, s.CreatedDate, s.Instance))
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(bool includeAllTenants, DateTime? from, DateTime? to, int? stageId,
            double? samplePercentage, CancellationToken token = default)
        {
            return BuildFilteredQuery(includeAllTenants, from, to, stageId, samplePercentage).CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(bool includeAllTenants, DateTime? from, DateTime? to,
            int? stageId, double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, stageId, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap);

            var rows = await query.ToListAsync(token).ConfigureAwait(false);

            var totalMicroseconds = rows.Select(s => (double)s.TotalMicroseconds.GetValueOrDefault())
                .ToArray();

            var minMicroseconds = rows.Select(s => (double)s.MinMicroseconds.GetValueOrDefault())
                .ToArray();

            var maxMicroseconds = rows.Select(s => (double)s.MaxMicroseconds.GetValueOrDefault())
                .ToArray();

            var invokeCount = rows.Select(s => (double)s.InvokeCount.GetValueOrDefault())
                .ToArray();

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["totalMicroseconds"] = totalMicroseconds,
                ["minMicroseconds"] = minMicroseconds,
                ["maxMicroseconds"] = maxMicroseconds,
                ["invokeCount"] = invokeCount
            });
        }
    }
}