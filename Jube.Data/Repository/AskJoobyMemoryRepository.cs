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
using Jube.Data.Repository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class AskJoobyMemoryRepository(DbContext dbContext, int tenantRegistryId, string userName)
    {
        private const string InsertSql = """
                                         INSERT INTO "AskJoobyMemory"
                                             ("TenantRegistryId", "ScopeId", "CreatedUser", "Content", "Embedding", "StatusId", "CreatedDate", "ExpiresAt", "KindId", "AskJoobyConversationId")
                                         VALUES (@tenantRegistryId, @scope, @createdUser, @content, @embedding::vector, @status, @createdDate, @expiresAt, @kind, @conversationId)
                                         RETURNING "Id";
                                         """;

        public Task<List<AskJoobyMemory>> ListByConversationAsync(int askJoobyConversationId,
            CancellationToken token = default)
        {
            return dbContext.AskJoobyMemory
                .Where(w => w.AskJoobyConversationId == askJoobyConversationId
                            && w.TenantRegistryId == tenantRegistryId)
                .OrderBy(o => o.Id)
                .ToListAsync(token);
        }

        public async Task<AskJoobyMemory> InsertAsync(string content, byte scopeId, string embeddingText,
            byte? status = null, DateTime? expiresAt = null, byte kindId = (byte)AskJoobyMemoryKind.Fact,
            int? conversationId = null, CancellationToken token = default)
        {
            var createdDate = DateTime.UtcNow;

            var id = (await dbContext.QueryToListAsync<int>(InsertSql, token,
                    new DataParameter("tenantRegistryId", tenantRegistryId),
                    new DataParameter("scope", scopeId),
                    new DataParameter("createdUser", userName),
                    new DataParameter("content", content),
                    new DataParameter("embedding",
                        string.IsNullOrEmpty(embeddingText) ? DBNull.Value : embeddingText),
                    new DataParameter("status", status.HasValue ? status.Value : DBNull.Value),
                    new DataParameter("createdDate", createdDate),
                    new DataParameter("expiresAt", expiresAt.HasValue ? expiresAt.Value : DBNull.Value),
                    new DataParameter("kind", kindId),
                    new DataParameter("conversationId",
                        conversationId.HasValue ? conversationId.Value : DBNull.Value))
                .ConfigureAwait(false)).Single();

            return new AskJoobyMemory
            {
                Id = id, TenantRegistryId = tenantRegistryId, ScopeId = scopeId, CreatedUser = userName,
                Content = content, CreatedDate = createdDate, StatusId = status, ExpiresAt = expiresAt,
                KindId = kindId, AskJoobyConversationId = conversationId
            };
        }
    }
}