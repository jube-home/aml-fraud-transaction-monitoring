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
    public class RedisConnectionEventRepository(DbContext dbContext)
    {
        public Task BulkCopyAsync(List<RedisConnectionEvent> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }

        public async Task<IEnumerable<RedisConnectionEvent>> GetLastAsync(int take, DateTime? from, DateTime? to,
            int? eventTypeId, int? connectionTypeId, int? failureTypeId, string search,
            double? samplePercentage, string sortField, string sortDirection, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, eventTypeId, connectionTypeId, failureTypeId, search,
                samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(DateTime? from, DateTime? to, int? eventTypeId, int? connectionTypeId,
            int? failureTypeId, string search, double? samplePercentage, CancellationToken token = default)
        {
            return BuildFilteredQuery(from, to, eventTypeId, connectionTypeId, failureTypeId, search, samplePercentage)
                .CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(DateTime? from, DateTime? to, int? eventTypeId,
            int? connectionTypeId, int? failureTypeId, string search, double? samplePercentage, int statisticsCap,
            CancellationToken token = default)
        {
            var rows = await BuildFilteredQuery(from, to, eventTypeId, connectionTypeId, failureTypeId, search,
                    samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["retryCount"] = rows.Where(w => w.RetryCount.HasValue)
                    .Select(s => (double)s.RetryCount!.Value).ToArray(),
                ["backoffMilliseconds"] = rows.Where(w => w.BackoffMilliseconds.HasValue)
                    .Select(s => (double)s.BackoffMilliseconds!.Value).ToArray(),
                ["transactionsImpacted"] = rows.Where(w => w.TransactionsImpacted.HasValue)
                    .Select(s => (double)s.TransactionsImpacted!.Value).ToArray()
            });
        }

        private IQueryable<RedisConnectionEvent> BuildFilteredQuery(DateTime? from, DateTime? to, int? eventTypeId,
            int? connectionTypeId, int? failureTypeId, string search, double? samplePercentage)
        {
            var query = dbContext.RedisConnectionEvent.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.OccurredDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.OccurredDate <= to.Value);
            }

            if (eventTypeId.HasValue)
            {
                query = query.Where(w => w.EventTypeId == eventTypeId.Value);
            }

            if (connectionTypeId.HasValue)
            {
                query = query.Where(w => w.ConnectionTypeId == connectionTypeId.Value);
            }

            if (failureTypeId.HasValue)
            {
                query = query.Where(w => w.FailureTypeId == failureTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(w =>
                    (w.EndPoint != null && w.EndPoint.ToLower().Contains(lowerSearch)) ||
                    (w.Message != null && w.Message.ToLower().Contains(lowerSearch)) ||
                    (w.Exception != null && w.Exception.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<RedisConnectionEvent> ApplySort(IQueryable<RedisConnectionEvent> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "eventTypeName" => query.OrderByField(o => o.EventTypeId, descending),
                "endPoint" => query.OrderByField(o => o.EndPoint, descending),
                "connectionTypeName" => query.OrderByField(o => o.ConnectionTypeId, descending),
                "failureTypeName" => query.OrderByField(o => o.FailureTypeId, descending),
                "origin" => query.OrderByField(o => o.Origin, descending),
                "message" => query.OrderByField(o => o.Message, descending),
                "exception" => query.OrderByField(o => o.Exception, descending),
                "retryCount" => query.OrderByField(o => o.RetryCount, descending),
                "backoffMilliseconds" => query.OrderByField(o => o.BackoffMilliseconds, descending),
                "transactionsImpacted" => query.OrderByField(o => o.TransactionsImpacted, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}