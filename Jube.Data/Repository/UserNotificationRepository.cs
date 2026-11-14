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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using Npgsql;
    using Poco;

    public class UserNotificationRepository(DbContext dbContext)
    {
        public async Task<bool> InsertIfNewAsync(UserNotification model, CancellationToken token = default)
        {
            var exists = await dbContext.UserNotification.AnyAsync(w =>
                w.TenantRegistryId == model.TenantRegistryId
                && w.RecipientUser == model.RecipientUser
                && w.DedupeKey == model.DedupeKey
                && w.ResolvedDate == null
                && w.DismissedDate == null, token).ConfigureAwait(false);

            if (exists)
            {
                return false;
            }

            try
            {
                model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);

                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return false;
            }
        }

        public Task<List<UserNotification>> ListActiveAsync(int tenantRegistryId, string recipientUser, int take,
            CancellationToken token = default)
        {
            return dbContext.UserNotification
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && w.RecipientUser == recipientUser
                            && w.ResolvedDate == null
                            && w.DismissedDate == null)
                .OrderByDescending(o => o.CreatedDate)
                .ThenByDescending(o => o.Id)
                .Take(take)
                .ToListAsync(token);
        }

        public Task<int> MarkReadAsync(int tenantRegistryId, string recipientUser, Guid guid,
            CancellationToken token = default)
        {
            return dbContext.UserNotification
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && w.RecipientUser == recipientUser
                            && w.Guid == guid
                            && w.ReadDate == null)
                .Set(s => s.ReadDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public Task<int> MarkAllReadAsync(int tenantRegistryId, string recipientUser,
            CancellationToken token = default)
        {
            return dbContext.UserNotification
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && w.RecipientUser == recipientUser
                            && w.ReadDate == null)
                .Set(s => s.ReadDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public Task<int> ResolveForCaseAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.UserNotification
                .Where(w => w.CaseId == caseId && w.ResolvedDate == null)
                .Set(s => s.ResolvedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
