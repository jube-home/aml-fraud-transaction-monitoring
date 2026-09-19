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
    public class UserLogoutRepository(DbContext dbContext, int? scopeToTenantRegistryId = null)
    {
        public async Task<UserLogout> InsertAsync(UserLogout model, CancellationToken token = default)
        {
            model.CreatedDate ??= DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<UserLogout>> GetLastAsync(int take, DateTime? from, DateTime? to,
            string search, double? samplePercentage, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, search, samplePercentage);
            query = ApplySort(query, sortField, sortDirection);

            return await query
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

        private IQueryable<UserLogout> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.UserLogout.AsQueryable();

            if (scopeToTenantRegistryId.HasValue)
            {
                var scopedTenantRegistryId = scopeToTenantRegistryId.Value;
                query = query.Where(w => w.TenantRegistryId == scopedTenantRegistryId);
            }

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
                    (w.CreatedUser != null && w.CreatedUser.ToLower().Contains(lowerSearch)) ||
                    (w.CutByUser != null && w.CutByUser.ToLower().Contains(lowerSearch)) ||
                    (w.RemoteIp != null && w.RemoteIp.ToLower().Contains(lowerSearch)) ||
                    (w.UserAgent != null && w.UserAgent.ToLower().Contains(lowerSearch)) ||
                    (w.Message != null && w.Message.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<UserLogout> ApplySort(IQueryable<UserLogout> query, string sortField,
            string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "createdUser" => query.OrderByField(o => o.CreatedUser, descending),
                "reasonId" => query.OrderByField(o => o.ReasonId, descending),
                "outcomeId" => query.OrderByField(o => o.OutcomeId, descending),
                "remoteIp" => query.OrderByField(o => o.RemoteIp, descending),
                "localIp" => query.OrderByField(o => o.LocalIp, descending),
                "userAgent" => query.OrderByField(o => o.UserAgent, descending),
                "sessionStartDate" => query.OrderByField(o => o.SessionStartDate, descending),
                "cutByUser" => query.OrderByField(o => o.CutByUser, descending),
                "message" => query.OrderByField(o => o.Message, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}