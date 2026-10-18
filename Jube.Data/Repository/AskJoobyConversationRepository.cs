/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class AskJoobyConversationRepository(DbContext dbContext, int tenantRegistryId, string userName = null)
    {
        public Task<List<AskJoobyConversation>> ListAsync(string search, int take, int afterId,
            CancellationToken token = default)
        {
            var query = dbContext.AskJoobyConversation
                .Where(w => w.TenantRegistryId == tenantRegistryId && (w.Deleted == 0 || w.Deleted == null));

            if (afterId > 0)
            {
                query = query.Where(w => w.Id < afterId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(w => w.Title != null && w.Title.ToLower().Contains(search.ToLower()));
            }

            return query
                .OrderByDescending(o => o.UpdatedDate ?? o.CreatedDate)
                .ThenByDescending(o => o.Id)
                .Take(take)
                .ToListAsync(token);
        }

        public Task<AskJoobyConversation> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.AskJoobyConversation.FirstOrDefaultAsync(w =>
                    w.Id == id && w.TenantRegistryId == tenantRegistryId && (w.Deleted == 0 || w.Deleted == null),
                token);
        }

        public async Task<AskJoobyConversation> InsertAsync(AskJoobyConversation model,
            CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.Guid = Guid.NewGuid();
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task UpdateTitleAsync(int id, string title, CancellationToken token = default)
        {
            var existing = await GetByIdAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var version = (existing.Version ?? 0) + 1;

            await dbContext.AskJoobyConversation
                .Where(w => w.Id == id)
                .Set(s => s.Title, title)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .Set(s => s.UpdatedUser, userName)
                .Set(s => s.Version, version)
                .UpdateAsync(token);
        }

        public Task TouchAsync(int id, CancellationToken token = default)
        {
            return dbContext.AskJoobyConversation
                .Where(w => w.Id == id)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .Set(s => s.UpdatedUser, userName)
                .UpdateAsync(token);
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.AskJoobyConversation
                .Where(w => w.Id == id && w.TenantRegistryId == tenantRegistryId
                                       && (w.Deleted == 0 || w.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}