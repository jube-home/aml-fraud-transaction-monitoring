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
using System.Threading.Channels;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.Guidance.Interfaces;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Guidance
{
    public sealed class GuidanceInjectingChatClient(
        IChatClient inner,
        IAgentGuidanceExchange exchange,
        string conversationId,
        string runId,
        string agentType,
        ChannelWriter<AgentStreamItem>? transcript = null) : IChatClient
    {
        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return await inner.GetResponseAsync(await WithGuidanceAsync(messages).ConfigureAwait(false),
                options, cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var withGuidance = await WithGuidanceAsync(messages).ConfigureAwait(false);

            await foreach (var update in inner
                               .GetStreamingResponseAsync(withGuidance, options, cancellationToken)
                               .ConfigureAwait(false))
            {
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

        private async Task<IEnumerable<ChatMessage>> WithGuidanceAsync(IEnumerable<ChatMessage> messages)
        {
            var pending = exchange.Drain(conversationId, runId);

            if (pending.Count == 0)
            {
                return messages;
            }

            var text = pending.Count == 1
                ? pending[0]
                : string.Join("\n", pending.Select(entry => "- " + entry));

            if (transcript is not null)
            {
                await transcript.WriteAsync(new AgentStreamItem(
                    AgentStreamItemKind.Step,
                    $"{AgentStreamNarrator.DescribeAgentLabel(agentType)} was told: {text}")).ConfigureAwait(false);
            }

            return messages.Concat([
                new ChatMessage(ChatRole.User,
                    "The caller has said this while you were working, and it takes precedence over your "
                    + "current plan where the two disagree:\n\n" + text)
            ]);
        }
    }
}