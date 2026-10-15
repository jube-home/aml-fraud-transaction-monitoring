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
    public class AskJoobyFileRepository(DbContext dbContext, int tenantRegistryId, string userName = null)
    {
        public Task<List<AskJoobyFile>> ListByConversationAsync(int askJoobyConversationId,
            CancellationToken token = default)
        {
            return dbContext.AskJoobyFile
                .Where(w => w.AskJoobyConversationId == askJoobyConversationId
                            && w.AskJoobyConversation.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id)
                .ToListAsync(token);
        }

        public Task<AskJoobyFile> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.AskJoobyFile.FirstOrDefaultAsync(w =>
                w.Id == id && w.AskJoobyConversation.TenantRegistryId == tenantRegistryId
                           && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<List<AskJoobyFile>> ListByMessageIdsAsync(IReadOnlyCollection<int> messageIds,
            CancellationToken token = default)
        {
            if (messageIds.Count == 0)
            {
                return Task.FromResult(new List<AskJoobyFile>());
            }

            return dbContext.AskJoobyFile
                .Where(w => w.AskJoobyMessageId.HasValue && messageIds.Contains(w.AskJoobyMessageId.Value)
                                                         && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync(token);
        }

        public Task AttachToMessageAsync(IReadOnlyCollection<int> fileIds, int askJoobyMessageId,
            CancellationToken token = default)
        {
            return dbContext.AskJoobyFile
                .Where(w => fileIds.Contains(w.Id) && w.AskJoobyConversation.TenantRegistryId == tenantRegistryId)
                .Set(s => s.AskJoobyMessageId, askJoobyMessageId)
                .UpdateAsync(token);
        }

        public async Task<AskJoobyFile> InsertAsync(AskJoobyFile model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.AskJoobyFile
                .Where(w => w.Id == id && w.AskJoobyConversation.TenantRegistryId == tenantRegistryId
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