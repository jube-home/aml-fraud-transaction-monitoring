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
    public class EntityAnalysisModelTaskPerformanceCounterRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;

        public EntityAnalysisModelTaskPerformanceCounterRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelTaskPerformanceCounterRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<EntityAnalysisModelTaskPerformanceCounter> InsertAsync(
            EntityAnalysisModelTaskPerformanceCounter model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
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

        public async Task<IEnumerable<EntityAnalysisModelTaskPerformanceCounterRow>> GetLastAsync(
            bool includeAllTenants, int take, DateTime? from, DateTime? to, int? directionId, int? taskTypeId,
            double? samplePercentage, string sortField, string sortDirection, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, directionId, taskTypeId, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .Select(s => new EntityAnalysisModelTaskPerformanceCounterRow(
                    s.Id, s.EntityAnalysisModelGuid, s.EntityAnalysisModel.Name, s.DirectionId, s.TaskTypeId,
                    s.TotalMicroseconds, s.MinMicroseconds, s.MaxMicroseconds, s.TotalAllocatedBytes,
                    s.MinAllocatedBytes, s.MaxAllocatedBytes, s.InvokeCount, s.CreatedDate, s.Instance))
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(bool includeAllTenants, DateTime? from, DateTime? to, int? directionId,
            int? taskTypeId, double? samplePercentage, CancellationToken token = default)
        {
            return BuildFilteredQuery(includeAllTenants, from, to, directionId, taskTypeId, samplePercentage)
                .CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(bool includeAllTenants, DateTime? from, DateTime? to,
            int? directionId, int? taskTypeId, double? samplePercentage, int statisticsCap,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, directionId, taskTypeId, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap);

            var rows = await query.ToListAsync(token).ConfigureAwait(false);

            var totalMicroseconds = rows.Select(s => (double)s.TotalMicroseconds.GetValueOrDefault())
                .ToArray();

            var minMicroseconds = rows.Select(s => (double)s.MinMicroseconds.GetValueOrDefault())
                .ToArray();

            var maxMicroseconds = rows.Select(s => (double)s.MaxMicroseconds.GetValueOrDefault())
                .ToArray();

            var totalAllocatedBytes = rows.Select(s => (double)s.TotalAllocatedBytes.GetValueOrDefault())
                .ToArray();

            var minAllocatedBytes = rows.Select(s => (double)s.MinAllocatedBytes.GetValueOrDefault())
                .ToArray();

            var maxAllocatedBytes = rows.Select(s => (double)s.MaxAllocatedBytes.GetValueOrDefault())
                .ToArray();

            var invokeCount = rows.Select(s => (double)s.InvokeCount.GetValueOrDefault())
                .ToArray();

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["totalMicroseconds"] = totalMicroseconds,
                ["minMicroseconds"] = minMicroseconds,
                ["maxMicroseconds"] = maxMicroseconds,
                ["totalAllocatedBytes"] = totalAllocatedBytes,
                ["minAllocatedBytes"] = minAllocatedBytes,
                ["maxAllocatedBytes"] = maxAllocatedBytes,
                ["invokeCount"] = invokeCount
            });
        }
    }
}