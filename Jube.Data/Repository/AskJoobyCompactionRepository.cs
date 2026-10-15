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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class AskJoobyCompactionRepository(DbContext dbContext, int tenantRegistryId, string userName)
    {
        private const string InsertSql = """
                                         INSERT INTO "AskJoobyCompaction"
                                             ("Guid", "TenantRegistryId", "AskJoobyConversationId",
                                              "AskJoobyMessageId", "CompactedToAskJoobyMessageId",
                                              "MessageCount", "BeforeCharacters", "AfterCharacters",
                                              "Before", "After", "DurationMs", "CreatedUser",
                                              "CreatedDate", "Embedding")
                                         VALUES (@guid, @tenantRegistryId, @conversationId, @messageId,
                                                 @compactedTo, @messageCount, @beforeCharacters,
                                                 @afterCharacters, @before, @after, @durationMs,
                                                 @createdUser, @createdDate, @embedding::vector)
                                         RETURNING "Id";
                                         """;

        private const string ListByConversationSql = """
                                                     SELECT "Id", "AskJoobyMessageId", "CompactedToAskJoobyMessageId",
                                                            "MessageCount", "BeforeCharacters", "AfterCharacters",
                                                            "Before", "After", "DurationMs", "CreatedUser", "CreatedDate"
                                                     FROM "AskJoobyCompaction"
                                                     WHERE "AskJoobyConversationId" = @conversationId
                                                       AND "TenantRegistryId" = @tenantRegistryId
                                                     ORDER BY "Id"
                                                     """;

        public async Task<AskJoobyCompaction> InsertWithEmbeddingAsync(AskJoobyCompaction model,
            string embeddingText, CancellationToken token = default)
        {
            model.TenantRegistryId = tenantRegistryId;
            model.CreatedUser = userName ?? model.CreatedUser;
            model.CreatedDate = DateTime.UtcNow;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;

            model.Id = await dbContext.ExecuteAsync<int>(InsertSql, token,
                new DataParameter("guid", model.Guid),
                new DataParameter("tenantRegistryId", model.TenantRegistryId),
                new DataParameter("conversationId", model.AskJoobyConversationId),
                new DataParameter("messageId", model.AskJoobyMessageId),
                new DataParameter("compactedTo", model.CompactedToAskJoobyMessageId),
                new DataParameter("messageCount", model.MessageCount),
                new DataParameter("beforeCharacters", model.BeforeCharacters),
                new DataParameter("afterCharacters", model.AfterCharacters),
                new DataParameter("before", model.Before),
                new DataParameter("after", model.After),
                new DataParameter("durationMs", model.DurationMs),
                new DataParameter("createdUser", model.CreatedUser),
                new DataParameter("createdDate", model.CreatedDate),
                new DataParameter("embedding", embeddingText)).ConfigureAwait(false);

            return model;
        }

        public Task<List<AskJoobyCompaction>> ListByConversationAsync(int conversationId,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<AskJoobyCompaction>(ListByConversationSql, token,
                new DataParameter("conversationId", conversationId),
                new DataParameter("tenantRegistryId", tenantRegistryId));
        }
    }
}