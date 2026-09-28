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
    public class WafAttackRepository(DbContext dbContext)
    {
        public Task BulkCopyAsync(List<WafAttack> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }

        public async Task<IEnumerable<WafAttack>> GetLastAsync(int take, DateTime? from, DateTime? to, string search,
            double? samplePercentage, string sortField, string sortDirection, CancellationToken token = default)
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

        public Task<PayloadStatistics> GetStatisticsAsync(DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            return Task.FromResult(new PayloadStatistics(new Dictionary<string, ColumnStatistics>()));
        }

        private IQueryable<WafAttack> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.WafAttack.AsQueryable();

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
                query = query.Where(w =>
                    (w.MatchedValue != null && w.MatchedValue.ToLower().Contains(lowerSearch)) ||
                    (w.Route != null && w.Route.ToLower().Contains(lowerSearch)) ||
                    (w.SignatureName != null && w.SignatureName.ToLower().Contains(lowerSearch)) ||
                    (w.Category != null && w.Category.ToLower().Contains(lowerSearch)) ||
                    (w.MatchedField != null && w.MatchedField.ToLower().Contains(lowerSearch)) ||
                    (w.UserName != null && w.UserName.ToLower().Contains(lowerSearch)) ||
                    (w.RemoteIp != null && w.RemoteIp.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<WafAttack> ApplySort(IQueryable<WafAttack> query, string sortField,
            string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "transport" => query.OrderByField(o => o.Transport, descending),
                "route" => query.OrderByField(o => o.Route, descending),
                "method" => query.OrderByField(o => o.Method, descending),
                "remoteIp" => query.OrderByField(o => o.RemoteIp, descending),
                "userName" => query.OrderByField(o => o.UserName, descending),
                "signatureName" => query.OrderByField(o => o.SignatureName, descending),
                "category" => query.OrderByField(o => o.Category, descending),
                "matchedField" => query.OrderByField(o => o.MatchedField, descending),
                "action" => query.OrderByField(o => o.Action, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}