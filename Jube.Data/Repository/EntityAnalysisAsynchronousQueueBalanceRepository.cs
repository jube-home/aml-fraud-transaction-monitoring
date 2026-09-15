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
    public class EntityAnalysisAsynchronousQueueBalanceRepository(DbContext dbContext)
    {
        private IQueryable<EntityAnalysisAsynchronousQueueBalance> BuildFilteredQuery(DateTime? from, DateTime? to,
            double? samplePercentage)
        {
            var query = dbContext.EntityAnalysisAsynchronousQueueBalance.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.CreatedDate <= to.Value);
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<EntityAnalysisAsynchronousQueueBalance> ApplySort(
            IQueryable<EntityAnalysisAsynchronousQueueBalance> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "instance" => query.OrderByField(o => o.Instance, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "caseCreation" => query.OrderByField(o => o.CaseCreation, descending),
                "tagging" => query.OrderByField(o => o.Tagging, descending),
                "notification" => query.OrderByField(o => o.Notification, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }

        public async Task<IEnumerable<EntityAnalysisAsynchronousQueueBalance>> GetAsync(int take, DateTime? from,
            DateTime? to, double? samplePercentage, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, samplePercentage);

            return await ApplySort(query, sortField, sortDirection).Take(take).ToListAsync(token);
        }

        public Task<int> CountAsync(DateTime? from, DateTime? to, double? samplePercentage,
            CancellationToken token = default)
        {
            return BuildFilteredQuery(from, to, samplePercentage).CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(DateTime? from, DateTime? to, double? samplePercentage,
            int statisticsCap, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap);

            var rows = await query.ToListAsync(token).ConfigureAwait(false);

            var caseCreation = rows.Select(s => (double)s.CaseCreation.GetValueOrDefault())
                .ToArray();

            var tagging = rows.Select(s => (double)s.Tagging.GetValueOrDefault())
                .ToArray();

            var notification = rows.Select(s => (double)s.Notification.GetValueOrDefault())
                .ToArray();

            var asynchronousEntityInvoke = rows.Select(_ => 0d).ToArray();

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["caseCreation"] = caseCreation,
                ["tagging"] = tagging,
                ["notification"] = notification,
                ["asynchronousEntityInvoke"] = asynchronousEntityInvoke
            });
        }

        public async Task<EntityAnalysisAsynchronousQueueBalance> InsertAsync(
            EntityAnalysisAsynchronousQueueBalance model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }
    }
}