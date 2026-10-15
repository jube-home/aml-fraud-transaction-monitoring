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

using Jube.AskJooby.Memory;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.Data.Poco;
using Jube.Data.Query.SearchAskJoobyMemoryQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyRagChunk;
using log4net;
using Microsoft.Extensions.AI;

namespace Jube.Service.AskJoobyMemory
{
    public sealed class AskJoobyMemoryClient(
        AskJoobyMemoryRepository repository,
        SearchAskJoobyMemoryQuery query,
        int tenantRegistryId,
        string userName,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator,
        int? conversationId = null,
        AskJoobyAgentGuardRejectionRepository? guardRejectionRepository = null) : IMemoryClient
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AskJoobyMemoryClient));

        private const int EmbeddingTimeoutSeconds = 5;

        public Task<MemoryEntry> RememberAsync(string content, string scope, byte? status = null,
            DateTime? expiresAt = null, string kind = "Fact", CancellationToken token = default)
        {
            return RememberAsync(content, scope, false, status, expiresAt, kind, token);
        }

        public async Task<MemoryEntry> RememberAsync(string content, string scope, bool redactPersonalData,
            byte? status = null, DateTime? expiresAt = null, string kind = "Fact", CancellationToken token = default)
        {
            var sensitiveFinding = await AskJoobySensitiveDataGuard.CheckAsync(content).ConfigureAwait(false);

            if (sensitiveFinding is not null)
            {
                if (guardRejectionRepository is not null)
                {
                    await guardRejectionRepository.InsertAsync(new AskJoobyAgentGuardRejection
                    {
                        AskJoobyConversationId = conversationId,
                        TenantRegistryId = tenantRegistryId,
                        PhaseId = MapPhase(sensitiveFinding.Phase),
                        PipelineName = sensitiveFinding.PipelineName,
                        RuleName = sensitiveFinding.RuleName,
                        Reason = sensitiveFinding.Reason,
                        ActionId = MapAction(sensitiveFinding.Action),
                        CreatedUser = userName
                    }, token).ConfigureAwait(false);
                }

                if (!redactPersonalData || sensitiveFinding.RedactedContent is null)
                {
                    throw new ArgumentException(sensitiveFinding.Message);
                }

                content = sensitiveFinding.RedactedContent;
            }

            var scopeId = MapScope(scope);
            var effectiveConversationId = scopeId == (byte)AskJoobyMemoryScope.Conversation ? conversationId : null;
            var kindId = MapKind(kind);

            var embeddingText = await EmbedAsync(content, token).ConfigureAwait(false);

            var saved = await repository
                .InsertAsync(content, scopeId, embeddingText, status, expiresAt, kindId, effectiveConversationId,
                    token)
                .ConfigureAwait(false);

            return new MemoryEntry(saved.Id, MapScope(saved.ScopeId), saved.CreatedUser, saved.Content,
                Status: saved.StatusId, ExpiresAt: saved.ExpiresAt, CreatedDate: saved.CreatedDate,
                Kind: MapKind(saved.KindId));
        }

        public async Task<IReadOnlyList<MemoryEntry>> RecallAsync(string queryText, int topK = 5,
            DateTime? since = null, bool includeReflections = false, CancellationToken token = default)
        {
            var embeddingText = await EmbedQueryAsync(queryText, token).ConfigureAwait(false);

            var rows = await query
                .ExecuteAsync(queryText, embeddingText, tenantRegistryId, userName, topK, since,
                    includeReflections, conversationId, token)
                .ConfigureAwait(false);

            return rows.Select(row =>
                    new MemoryEntry(row.Id, MapScope(row.ScopeId), row.CreatedUser, row.Content, row.Score,
                        row.StatusId, row.ExpiresAt, row.CreatedDate, MapKind(row.KindId)))
                .ToList();
        }

        private static byte MapScope(string scope)
        {
            return Enum.TryParse<AskJoobyMemoryScope>(scope, out var parsed)
                ? (byte)parsed
                : (byte)AskJoobyMemoryScope.User;
        }

        private static string MapScope(byte scopeId)
        {
            return Enum.IsDefined(typeof(AskJoobyMemoryScope), scopeId)
                ? ((AskJoobyMemoryScope)scopeId).ToString()
                : AskJoobyMemoryScope.User.ToString();
        }

        private static byte MapKind(string kind)
        {
            return Enum.TryParse<AskJoobyMemoryKind>(kind, out var parsed)
                ? (byte)parsed
                : (byte)AskJoobyMemoryKind.Fact;
        }

        private static string MapKind(byte kindId)
        {
            return Enum.IsDefined(typeof(AskJoobyMemoryKind), kindId)
                ? ((AskJoobyMemoryKind)kindId).ToString()
                : AskJoobyMemoryKind.Fact.ToString();
        }

        private static AskJoobyGuardPhase? MapPhase(string? phase)
        {
            return Enum.TryParse<AskJoobyGuardPhase>(phase, true, out var parsed) ? parsed : null;
        }

        private static AskJoobyGuardAction? MapAction(string? action)
        {
            return Enum.TryParse<AskJoobyGuardAction>(action, true, out var parsed) ? parsed : null;
        }

        private Task<string> EmbedQueryAsync(string text, CancellationToken token)
        {
            if (embeddingGenerator is null || string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(string.Empty);
            }

            return EmbedAsync(text, token);
        }

        private async Task<string> EmbedAsync(string text, CancellationToken token)
        {
            if (embeddingGenerator is null || string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(EmbeddingTimeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, guard.Token);

            try
            {
                var embedding = await embeddingGenerator.GenerateAsync(text, cancellationToken: linked.Token)
                    .ConfigureAwait(false);

                return AskJoobyRagClient.ToPgVectorText(embedding.Vector.Span);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                log.Warn($"AskJooby memory: embedding timed out after {EmbeddingTimeoutSeconds}s, falling back "
                         + "to an unembedded save/search.");

                return string.Empty;
            }
            catch (Exception exception) when (!token.IsCancellationRequested)
            {
                log.Warn("AskJooby memory: embedding call failed, falling back to an unembedded save/search.",
                    exception);

                return string.Empty;
            }
        }
    }
}