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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;
using ModelReaderWriter = System.ClientModel.Primitives.ModelReaderWriter;
using StreamingChatCompletionUpdate = OpenAI.Chat.StreamingChatCompletionUpdate;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class BasicAgentRunnableThinkingTests
    {
        private static ChatResponseUpdate ReasoningDelta(string reasoning)
        {
            var json = $$"""
                         {"id":"c1","object":"chat.completion.chunk","created":1,"model":"deepseek-v4.1-flash",
                          "choices":[{"index":0,"delta":{"reasoning_content":"{{reasoning}}"},"finish_reason":null}]}
                         """;
#pragma warning disable SCME0001

            var raw = ModelReaderWriter.Read<StreamingChatCompletionUpdate>(BinaryData.FromString(json));
#pragma warning restore SCME0001

            return new ChatResponseUpdate(ChatRole.Assistant, (string?)null) { RawRepresentation = raw };
        }

        private static ChatResponseUpdate ContentDelta(string content)
        {
            var json = $$"""
                         {"id":"c1","object":"chat.completion.chunk","created":1,"model":"deepseek-v4.1-flash",
                          "choices":[{"index":0,"delta":{"content":"{{content}}"},"finish_reason":null}]}
                         """;

            var raw = ModelReaderWriter.Read<StreamingChatCompletionUpdate>(BinaryData.FromString(json));

            return new ChatResponseUpdate(ChatRole.Assistant, content) { RawRepresentation = raw };
        }

        private static AIAgent BuildAgent(IReadOnlyList<ChatResponseUpdate> updates)
        {
            var chatClient = new FakeStreamingChatClient(updates);

            return chatClient.AsHarnessAgent(new HarnessAgentOptions
            {
                Name = "test", HarnessInstructions = "test", ChatOptions = new ChatOptions(),
                DisableWebSearch = true, DisableFileMemory = true, DisableTodoProvider = true,
                DisableAgentModeProvider = true
            });
        }

        [Fact]
        public async Task SurfacesReasoningContentAsThinkingItemsWhenEnableThinkingIsOnAsync()
        {
            var agent = BuildAgent([ReasoningDelta("Let me think"), ContentDelta("Hello")]);
            var context = new RunContext(null, null, null, _ => true, true);
            var runnable = new BasicAgentRunnable(agent, "run-1", context);

            var items = await CollectAsync(runnable.RunStreamingAsync("hi"));

            items.Should().Contain(i => i.Kind == AgentStreamItemKind.Thinking && i.Text == "Let me think");
            items.Should().Contain(i => i.Kind == AgentStreamItemKind.Text && i.Text == "Hello");
        }

        [Fact]
        public async Task NeverSurfacesReasoningContentWhenEnableThinkingIsOffAsync()
        {
            var agent = BuildAgent([ReasoningDelta("Let me think"), ContentDelta("Hello")]);
            var context = new RunContext(null, null, null, _ => true);
            var runnable = new BasicAgentRunnable(agent, "run-1", context);

            var items = await CollectAsync(runnable.RunStreamingAsync("hi"));

            items.Should().NotContain(i => i.Kind == AgentStreamItemKind.Thinking);
            items.Should().Contain(i => i.Kind == AgentStreamItemKind.Text && i.Text == "Hello");
        }

        private static async Task<List<AgentStreamItem>> CollectAsync(IAsyncEnumerable<AgentStreamItem> source)
        {
            var items = new List<AgentStreamItem>();

            await foreach (var item in source)
            {
                items.Add(item);
            }

            return items;
        }

        private sealed class FakeStreamingChatClient(IReadOnlyList<ChatResponseUpdate> updates) : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;

                foreach (var update in updates)
                {
                    yield return update;
                }
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }
    }
}