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
    public class UserLoginRepository(DbContext dbContext, string userName = null, int? scopeToTenantRegistryId = null)
    {
        public async Task<UserLogin> InsertAsync(UserLogin model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<IEnumerable<UserLogin>> GetLastAsync(int take, DateTime? from, DateTime? to,
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

        private IQueryable<UserLogin> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.UserLogin.AsQueryable();

            if (scopeToTenantRegistryId.HasValue)
            {
                var scopedTenantRegistryId = scopeToTenantRegistryId.Value;
                query = query.Where(w => dbContext.UserInTenant.Any(u =>
                    u.User == w.CreatedUser && u.TenantRegistryId == scopedTenantRegistryId));
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
                    (w.RemoteIp != null && w.RemoteIp.ToLower().Contains(lowerSearch)) ||
                    (w.UserAgent != null && w.UserAgent.ToLower().Contains(lowerSearch)) ||
                    (w.FailureMessage != null && w.FailureMessage.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<UserLogin> ApplySort(IQueryable<UserLogin> query, string sortField,
            string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "createdUser" => query.OrderByField(o => o.CreatedUser, descending),
                "failed" => query.OrderByField(o => o.Failed, descending),
                "authenticationTypeId" => query.OrderByField(o => o.AuthenticationTypeId, descending),
                "failureTypeId" => query.OrderByField(o => o.FailureTypeId, descending),
                "failureMessage" => query.OrderByField(o => o.FailureMessage, descending),
                "remoteIp" => query.OrderByField(o => o.RemoteIp, descending),
                "localIp" => query.OrderByField(o => o.LocalIp, descending),
                "userAgent" => query.OrderByField(o => o.UserAgent, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}