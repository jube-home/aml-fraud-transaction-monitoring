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
    public class HttpProcessingCounterRepository(DbContext dbContext)
    {
        private IQueryable<HttpProcessingCounter> BuildFilteredQuery(DateTime? from, DateTime? to,
            double? samplePercentage)
        {
            var query = dbContext.HttpProcessingCounter.AsQueryable();

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

        private static IOrderedQueryable<HttpProcessingCounter> ApplySort(
            IQueryable<HttpProcessingCounter> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "instance" => query.OrderByField(o => o.Instance, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "model" => query.OrderByField(o => o.Model, descending),
                "asynchronousModel" => query.OrderByField(o => o.AsynchronousModel, descending),
                "tag" => query.OrderByField(o => o.Tag, descending),
                "error" => query.OrderByField(o => o.Error, descending),
                "sanction" => query.OrderByField(o => o.Sanction, descending),
                "exhaustive" => query.OrderByField(o => o.Exhaustive, descending),
                "all" => query.OrderByField(o => o.All, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }

        public async Task<IEnumerable<HttpProcessingCounter>> GetAsync(int take, DateTime? from, DateTime? to,
            double? samplePercentage, string sortField, string sortDirection, CancellationToken token = default)
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

            var model = rows.Select(s => (double)s.Model.GetValueOrDefault()).ToArray();

            var asynchronousModel = rows.Select(s => (double)s.AsynchronousModel.GetValueOrDefault())
                .ToArray();

            var tag = rows.Select(s => (double)s.Tag.GetValueOrDefault()).ToArray();

            var error = rows.Select(s => (double)s.Error.GetValueOrDefault()).ToArray();

            var sanction = rows.Select(s => (double)s.Sanction.GetValueOrDefault()).ToArray();

            var exhaustive = rows.Select(s => (double)s.Exhaustive.GetValueOrDefault()).ToArray();

            var all = rows.Select(s => (double)s.All.GetValueOrDefault()).ToArray();

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["model"] = model,
                ["asynchronousModel"] = asynchronousModel,
                ["tag"] = tag,
                ["error"] = error,
                ["sanction"] = sanction,
                ["exhaustive"] = exhaustive,
                ["all"] = all
            });
        }

        public async Task<HttpProcessingCounter> InsertAsync(HttpProcessingCounter model,
            CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }
    }
}