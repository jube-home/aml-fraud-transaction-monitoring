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

using Jube.AskJooby.History;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.Data.Query.SearchAskJoobyConversationHistoryQuery;
using Jube.Service.AskJoobyRagChunk;
using log4net;
using Microsoft.Extensions.AI;

namespace Jube.Service.AskJoobyConversation
{
    public sealed class AskJoobyConversationHistoryClient(
        SearchAskJoobyConversationHistoryQuery query,
        int tenantRegistryId,
        string userName,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator) : IConversationHistoryClient
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AskJoobyConversationHistoryClient));

        private const int EmbeddingTimeoutSeconds = 5;

        public async Task<IReadOnlyList<HistoryEntry>> SearchAsync(string queryText, string scope, int topK = 5,
            CancellationToken token = default)
        {
            var embeddingText = string.Empty;

            if (embeddingGenerator is not null && !string.IsNullOrWhiteSpace(queryText))
            {
                using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(EmbeddingTimeoutSeconds));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, guard.Token);

                try
                {
                    var embedding = await embeddingGenerator.GenerateAsync(queryText, cancellationToken: linked.Token)
                        .ConfigureAwait(false);

                    embeddingText = AskJoobyRagClient.ToPgVectorText(embedding.Vector.Span);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    log.Warn($"AskJooby history: query embedding timed out after {EmbeddingTimeoutSeconds}s, "
                             + "falling back to a text-only search.");
                }
                catch (Exception exception) when (!token.IsCancellationRequested)
                {
                    log.Warn("AskJooby history: query embedding call failed, falling back to a text-only search.",
                        exception);
                }
            }

            var rows = await query
                .ExecuteAsync(queryText, embeddingText, scope, tenantRegistryId, userName, topK, token)
                .ConfigureAwait(false);

            return rows.Select(row =>
                    new HistoryEntry(row.Id, row.ConversationId, row.Title, row.Content, row.IsCompact, row.Score,
                        row.CreatedDate))
                .ToList();
        }
    }
}