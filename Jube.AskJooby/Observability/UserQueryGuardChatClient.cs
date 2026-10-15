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

using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Observability
{
    public sealed class UserQueryGuardChatClient(IChatClient inner) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return inner.GetResponseAsync(Guard(messages), options, cancellationToken);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return inner.GetStreamingResponseAsync(Guard(messages), options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return inner.GetService(serviceType, serviceKey);
        }

        public void Dispose()
        {
            inner.Dispose();
        }

        private static IReadOnlyList<ChatMessage> Guard(IEnumerable<ChatMessage> messages)
        {
            var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
            var hasUserContent = list.Any(m => m.Role == ChatRole.User && !string.IsNullOrWhiteSpace(m.Text));

            if (!hasUserContent)
            {
                var roles = string.Join(", ", list.Select(m => $"{m.Role}({m.Text.Length} chars)"));

                throw new InvalidOperationException(
                    "AskJooby: about to send a chat request with no non-empty user message -- the " +
                    $"inference endpoint always rejects this. Messages were: [{roles}].");
            }

            return list;
        }
    }
}