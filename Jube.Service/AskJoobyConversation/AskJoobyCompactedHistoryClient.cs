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

using Jube.AskJooby.Compaction;
using Jube.AskJooby.Tools.Compaction.Interfaces;
using Jube.Data.Query.SearchAskJoobyCompactionQuery;
using Jube.Service.AskJoobyRagChunk;
using Microsoft.Extensions.AI;

namespace Jube.Service.AskJoobyConversation
{
    internal sealed class AskJoobyCompactedHistoryClient(
        SearchAskJoobyCompactionQuery query,
        int tenantRegistryId,
        string userName,
        int conversationId,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator) : ICompactedHistoryClient
    {
        private const int EmbeddingTimeoutSeconds = 5;

        public async Task<IReadOnlyList<CompactedEntry>> SearchAsync(string searchText,
            bool thisConversationOnly, int topK = 3, CancellationToken token = default)
        {
            var embeddingText = string.Empty;

            if (embeddingGenerator is not null)
            {
                using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(EmbeddingTimeoutSeconds));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, guard.Token);

                try
                {
                    var embedding = await embeddingGenerator.GenerateAsync(searchText, cancellationToken: linked.Token)
                        .ConfigureAwait(false);

                    embeddingText = AskJoobyRagClient.ToPgVectorText(embedding.Vector.Span);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                }
            }

            var rows = await query.ExecuteAsync(searchText, embeddingText, "User", tenantRegistryId, userName,
                    thisConversationOnly ? conversationId : 0, topK, token)
                .ConfigureAwait(false);

            return rows.Select(row => new CompactedEntry(row.Id, row.ConversationId, row.Title, row.Before,
                    row.Score, row.CreatedDate))
                .ToList();
        }
    }
}