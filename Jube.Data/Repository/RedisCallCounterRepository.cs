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
    public class RedisCallCounterRepository(DbContext dbContext)
    {
        public Task BulkCopyAsync(List<RedisCallCounter> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }

        private IQueryable<RedisCallCounter> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.RedisCallCounter.AsQueryable();

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
                var lowerSearch = search.ToLower();
                query = query.Where(w => w.Call != null && w.Call.ToLower().Contains(lowerSearch));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<RedisCallCounter> ApplySort(
            IQueryable<RedisCallCounter> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "call" => query.OrderByField(o => o.Call, descending),
                "count" => query.OrderByField(o => o.Count, descending),
                "totalMicroseconds" => query.OrderByField(o => o.TotalMicroseconds, descending),
                "minMicroseconds" => query.OrderByField(o => o.MinMicroseconds, descending),
                "maxMicroseconds" => query.OrderByField(o => o.MaxMicroseconds, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }

        public async Task<IEnumerable<RedisCallCounter>> GetLastAsync(int take, DateTime? from, DateTime? to,
            string search, double? samplePercentage, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, search, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(DateTime? from, DateTime? to, string search, double? samplePercentage,
            CancellationToken token = default)
        {
            return BuildFilteredQuery(from, to, search, samplePercentage).CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap);

            var rows = await query.ToListAsync(token).ConfigureAwait(false);

            var count = rows.Select(s => (double)s.Count.GetValueOrDefault()).ToArray();

            var totalMicroseconds = rows.Select(s => (double)s.TotalMicroseconds.GetValueOrDefault())
                .ToArray();

            var minMicroseconds = rows.Select(s => (double)s.MinMicroseconds.GetValueOrDefault())
                .ToArray();

            var maxMicroseconds = rows.Select(s => (double)s.MaxMicroseconds.GetValueOrDefault())
                .ToArray();

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["count"] = count,
                ["totalMicroseconds"] = totalMicroseconds,
                ["minMicroseconds"] = minMicroseconds,
                ["maxMicroseconds"] = maxMicroseconds
            });
        }
    }
}