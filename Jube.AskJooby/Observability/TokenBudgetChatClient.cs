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
    public sealed class TokenBudgetChatClient(IChatClient inner, ITokenBudget budget) : IChatClient
    {
        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var response = await inner.GetResponseAsync(messages, options, cancellationToken);

            budget.Record(response.Usage?.InputTokenCount ?? 0,
                response.Usage?.OutputTokenCount ?? 0,
                PromptCacheUsage.CachedInputTokens(response.Usage, response.RawRepresentation) ?? 0);

            return response;
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var update in inner.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                foreach (var content in update.Contents)
                {
                    if (content is UsageContent usage)
                    {
                        budget.Record(usage.Details.InputTokenCount ?? 0,
                            usage.Details.OutputTokenCount ?? 0,
                            PromptCacheUsage.CachedInputTokens(usage.Details, update.RawRepresentation) ?? 0);
                    }
                }

                yield return update;
            }
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