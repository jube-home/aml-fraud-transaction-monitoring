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
    public class AskJoobyMessageRepository(DbContext dbContext)
    {
        public const byte EntryTypeMessage = (byte)AskJoobyEntryType.Message;
        public const byte EntryTypeStep = (byte)AskJoobyEntryType.Step;
        public const byte EntryTypeThought = (byte)AskJoobyEntryType.Thought;
        public const byte EntryTypeThinking = (byte)AskJoobyEntryType.Thinking;
        public const byte EntryTypeDelegation = (byte)AskJoobyEntryType.Delegation;
        public const byte EntryTypeHandoff = (byte)AskJoobyEntryType.Handoff;
        public const byte EntryTypeValidation = (byte)AskJoobyEntryType.Validation;
        public const byte EntryTypeRevision = (byte)AskJoobyEntryType.Revision;

        private const string InsertWithEmbeddingSql = """
                                                      INSERT INTO "AskJoobyMessage"
                                                          ("AskJoobyConversationId", "RoleId", "Content", "CreatedDate", "EntryTypeId", "AskJoobyRunId", "IsCompact", "CompactedToAskJoobyMessageId", "IsNudge", "Embedding")
                                                      VALUES (@conversationId, @role, @content, @createdDate, @entryType, @runId, @isCompact, @compactedTo, @isNudge, @embedding::vector)
                                                      RETURNING "Id";
                                                      """;

        public Task<List<AskJoobyMessage>> ListMessagesByConversationAsync(int askJoobyConversationId,
            CancellationToken token = default)
        {
            return dbContext.AskJoobyMessage
                .Where(w => w.AskJoobyConversationId == askJoobyConversationId && w.EntryTypeId == EntryTypeMessage)
                .OrderBy(o => o.Id)
                .ToListAsync(token);
        }

        public Task<List<AskJoobyMessage>> ListEntriesByConversationAsync(int askJoobyConversationId,
            CancellationToken token = default)
        {
            return dbContext.AskJoobyMessage
                .Where(w => w.AskJoobyConversationId == askJoobyConversationId && w.IsCompact != true)
                .OrderBy(o => o.Id)
                .ToListAsync(token);
        }

        public Task<AskJoobyMessage> GetByIdAsync(int id, int askJoobyConversationId,
            CancellationToken token = default)
        {
            return dbContext.AskJoobyMessage
                .Where(w => w.Id == id && w.AskJoobyConversationId == askJoobyConversationId)
                .FirstOrDefaultAsync(token);
        }

        public async Task<AskJoobyMessage> InsertAsync(AskJoobyMessage model, CancellationToken token = default)
        {
            if (model.EntryTypeId == 0)
            {
                model.EntryTypeId = EntryTypeMessage;
            }

            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<AskJoobyMessage> InsertWithEmbeddingAsync(AskJoobyMessage model, string embeddingText,
            CancellationToken token = default)
        {
            if (model.EntryTypeId == 0)
            {
                model.EntryTypeId = EntryTypeMessage;
            }

            model.CreatedDate = DateTime.UtcNow;

            var id = (await dbContext.QueryToListAsync<int>(InsertWithEmbeddingSql, token,
                    new DataParameter("conversationId", model.AskJoobyConversationId),
                    new DataParameter("role", model.RoleId),
                    new DataParameter("content", model.Content),
                    new DataParameter("createdDate", model.CreatedDate),
                    new DataParameter("entryType", model.EntryTypeId),
                    new DataParameter("runId", model.AskJoobyRunId),
                    new DataParameter("isCompact", model.IsCompact),
                    new DataParameter("compactedTo", model.CompactedToAskJoobyMessageId),
                    new DataParameter("isNudge", model.IsNudge),
                    new DataParameter("embedding",
                        string.IsNullOrEmpty(embeddingText) ? DBNull.Value : embeddingText))
                .ConfigureAwait(false)).Single();

            model.Id = id;

            return model;
        }
    }
}