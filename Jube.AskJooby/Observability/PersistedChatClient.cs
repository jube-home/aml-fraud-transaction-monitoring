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

using System.Runtime.CompilerServices;
using Jube.AskJooby.Observability.Interfaces;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Observability
{
    public sealed class PersistedChatClient(
        IChatClient inner,
        IRunRecorder recorder,
        string runId,
        bool recordPromptPrefixHash = false)
        : IChatClient
    {
        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var sent = messages as IList<ChatMessage> ?? messages.ToList();
            var prefixHash = recordPromptPrefixHash ? PromptPrefix.Hash(sent) : null;

            var startedAt = DateTimeOffset.UtcNow;
            var response = await inner.GetResponseAsync(sent, options, cancellationToken);
            var durationMs = (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;

            await recorder.RecordModelCallAsync(
                runId,
                response.ModelId ?? options?.ModelId ?? "unknown",
                (int?)response.Usage?.InputTokenCount,
                (int?)response.Usage?.OutputTokenCount,
                PromptCacheUsage.CachedInputTokens(response.Usage, response.RawRepresentation),
                prefixHash,
                PromptCacheUsage.DescribeCounts(response.Usage),
                PromptCacheUsage.PromptDetailCachedTokens(response.Usage, response.RawRepresentation),
                response.FinishReason?.ToString(),
                durationMs,
                null,
                cancellationToken);

            return response;
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sent = messages as IList<ChatMessage> ?? messages.ToList();
            var prefixHash = recordPromptPrefixHash ? PromptPrefix.Hash(sent) : null;

            var startedAt = DateTimeOffset.UtcNow;
            DateTimeOffset? firstChunkAt = null;
            string? modelId = null;
            string? finishReason = null;
            UsageDetails? usage = null;
            object? usageRaw = null;

            await foreach (var update in inner.GetStreamingResponseAsync(sent, options, cancellationToken))
            {
                firstChunkAt ??= DateTimeOffset.UtcNow;
                modelId ??= update.ModelId;

                if (update.FinishReason is not null)
                {
                    finishReason = update.FinishReason.ToString();
                }

                foreach (var content in update.Contents)
                {
                    if (content is UsageContent usageContent)
                    {
                        usage = usageContent.Details;

                        usageRaw = update.RawRepresentation;
                    }
                }

                yield return update;
            }

            var durationMs = (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;

            var firstChunkMs = firstChunkAt is null
                ? (long?)null
                : (long)(firstChunkAt.Value - startedAt).TotalMilliseconds;

            await recorder.RecordModelCallAsync(
                runId,
                modelId ?? options?.ModelId ?? "unknown",
                (int?)usage?.InputTokenCount,
                (int?)usage?.OutputTokenCount,
                PromptCacheUsage.CachedInputTokens(usage, usageRaw),
                prefixHash,
                PromptCacheUsage.DescribeCounts(usage),
                PromptCacheUsage.PromptDetailCachedTokens(usage, usageRaw),
                finishReason,
                durationMs,
                firstChunkMs,
                cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return inner.GetService(serviceType, serviceKey);
        }

        public void Dispose()
        {
            inner.Dispose();
        }
    }
}