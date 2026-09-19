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
    public class OpenTelemetryMetricRepository(DbContext dbContext)
    {
        public Task BulkCopyAsync(List<OpenTelemetryMetric> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }

        public async Task<IEnumerable<OpenTelemetryMetric>> GetLastAsync(int take, DateTime? from, DateTime? to,
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
            var rows = await BuildFilteredQuery(from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .Select(s => new { s.Count, s.Sum, s.Min, s.Max })
                .ToListAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["count"] = rows.Where(w => w.Count.HasValue).Select(s => (double)s.Count!.Value).ToArray(),
                ["sum"] = rows.Where(w => w.Sum.HasValue).Select(s => s.Sum!.Value).ToArray(),
                ["min"] = rows.Where(w => w.Min.HasValue).Select(s => s.Min!.Value).ToArray(),
                ["max"] = rows.Where(w => w.Max.HasValue).Select(s => s.Max!.Value).ToArray()
            });
        }

        private IQueryable<OpenTelemetryMetric> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.OpenTelemetryMetric.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.OccurredDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.OccurredDate <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(w =>
                    (w.MetricName != null && w.MetricName.ToLower().Contains(lowerSearch)) ||
                    (w.Tags != null && w.Tags.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<OpenTelemetryMetric> ApplySort(IQueryable<OpenTelemetryMetric> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "metricName" => query.OrderByField(o => o.MetricName, descending),
                "instrumentType" => query.OrderByField(o => o.InstrumentType, descending),
                "tags" => query.OrderByField(o => o.Tags, descending),
                "count" => query.OrderByField(o => o.Count, descending),
                "sum" => query.OrderByField(o => o.Sum, descending),
                "min" => query.OrderByField(o => o.Min, descending),
                "max" => query.OrderByField(o => o.Max, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}