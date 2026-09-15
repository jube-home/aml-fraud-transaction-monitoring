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
    public class EntityAnalysisModelAsynchronousQueueBalanceRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public EntityAnalysisModelAsynchronousQueueBalanceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelAsynchronousQueueBalanceRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<EntityAnalysisModelAsynchronousQueueBalance>> GetAsync(int limit,
            CancellationToken token = default)
        {
            return await dbContext
                .EntityAnalysisModelAsynchronousQueueBalance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id)
                .Take(limit).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelAsynchronousQueueBalance>> GetByEntityModelIdAsync(
            Guid entityAnalysisModelGuid,
            int limit, CancellationToken token = default)
        {
            return await dbContext
                .EntityAnalysisModelAsynchronousQueueBalance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelGuid == entityAnalysisModelGuid)
                .OrderByDescending(o => o.Id)
                .Take(limit).ToListAsync(token);
        }

        public async Task<EntityAnalysisModelAsynchronousQueueBalance> InsertAsync(
            EntityAnalysisModelAsynchronousQueueBalance model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
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

        public async Task<IEnumerable<EntityAnalysisModelAsynchronousQueueBalanceRow>> GetLastAsync(
            bool includeAllTenants, int take, DateTime? from, DateTime? to, string search,
            double? samplePercentage, string sortField, string sortDirection, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .Select(s => new EntityAnalysisModelAsynchronousQueueBalanceRow(
                    s.EntityAnalysisModelGuid, s.EntityAnalysisModel.Name, s.CreatedDate, s.Archive,
                    s.ActivationWatcher, s.Instance))
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(bool includeAllTenants, DateTime? from, DateTime? to, string search,
            double? samplePercentage, CancellationToken token = default)
        {
            return BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage).CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(bool includeAllTenants, DateTime? from, DateTime? to,
            string search, double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(includeAllTenants, from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap);

            var rows = await query.ToListAsync(token).ConfigureAwait(false);

            var archive = rows.Select(s => (double)s.Archive.GetValueOrDefault())
                .ToArray();

            var activationWatcher = rows.Select(s => (double)s.ActivationWatcher.GetValueOrDefault())
                .ToArray();

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["archive"] = archive,
                ["activationWatcher"] = activationWatcher
            });
        }
    }
}