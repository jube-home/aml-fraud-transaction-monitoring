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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Reasoning;
using Microsoft.Extensions.AI;
using Xunit;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ProviderMarkupTruncationTests
    {
        [Theory]
        [InlineData("Steps: 1. Find the graph-directed workflow for", true)]
        [InlineData("The variable is required because the", true)]
        [InlineData("Steps: 1. Find the graph-directed workflow.", false)]
        [InlineData("1. Check the config file", false)]
        [InlineData("Done.", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("for", true)]
        [InlineData("Submitted the run for", true)]
        [InlineData("This sentence ends in a parenthetical (for example).", false)]
        public void LooksTruncatedMatchesOnlyADanglingTrailingWord(string text, bool expected)
        {
            ProviderMarkup.LooksTruncated(text).Should().Be(expected);
        }

        [Fact]
        public void LooksTruncatedIsNullSafe()
        {
            ProviderMarkup.LooksTruncated(null).Should().BeFalse();
        }

        [Fact]
        public async Task ANonStreamingReplyThatStopsOnADanglingWordIsReaskedAndStitchedTogetherAsync()
        {
            var inner = new ScriptedChatClient(
                Response("Steps: 1. Find the graph-directed workflow for", ChatFinishReason.Stop),
                Response(" training.", ChatFinishReason.Stop));

            var client = new ProviderMarkupChatClient(inner);

            var result = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "how do I train?")]);

            inner.Calls.Should().Be(2);
            result.Text.Should().Be("Steps: 1. Find the graph-directed workflow for training.");
        }

        [Fact]
        public async Task ANonStreamingReplyThatEndsCleanlyIsNeverReaskedAsync()
        {
            var inner = new ScriptedChatClient(
                Response("Steps: 1. Find the graph-directed workflow for training.", ChatFinishReason.Stop));

            var client = new ProviderMarkupChatClient(inner);

            var result = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "how do I train?")]);

            inner.Calls.Should().Be(1);
            result.Text.Should().Be("Steps: 1. Find the graph-directed workflow for training.");
        }

        [Fact]
        public async Task ADanglingWordAtLengthBudgetIsNotTreatedAsTruncationAsync()
        {
            var inner = new ScriptedChatClient(
                Response("Steps: 1. Find the graph-directed workflow for", ChatFinishReason.Length));

            var client = new ProviderMarkupChatClient(inner);

            var result = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "how do I train?")]);

            inner.Calls.Should().Be(1);
            result.Text.Should().Be("Steps: 1. Find the graph-directed workflow for");
        }

        [Fact]
        public async Task ADanglingWordAlongsideARealToolCallIsNotTreatedAsTruncationAsync()
        {
            var withCall = new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("Submitted the run for"),
                new FunctionCallContent("call-1", "SubmitLogisticRegressionTraining")
            ]))
            {
                FinishReason = ChatFinishReason.Stop
            };

            var inner = new ScriptedChatClient(withCall);
            var client = new ProviderMarkupChatClient(inner);

            var result = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "train it")]);

            inner.Calls.Should().Be(1);
            result.Text.Should().Be("Submitted the run for");
        }

        [Fact]
        public async Task AStreamingReplyThatStopsOnADanglingWordIsReaskedAndContinuedAsync()
        {
            var inner = new ScriptedStreamingChatClient(
                ["Steps: 1. Find the graph-directed workflow for"],
                [" training."]);

            var client = new ProviderMarkupChatClient(inner);

            var updates = await DrainAsync(
                client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "how do I train?")]));

            inner.Calls.Should().Be(2);

            string.Concat(updates.Select(u => u.Text)).Should().Be(
                "Steps: 1. Find the graph-directed workflow for training.");
        }

        [Fact]
        public async Task AStreamingReplyThatEndsCleanlyIsNeverReaskedAsync()
        {
            var inner = new ScriptedStreamingChatClient(
                ["Steps: 1. Find the graph-directed workflow for training."]);

            var client = new ProviderMarkupChatClient(inner);

            var updates = await DrainAsync(
                client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "how do I train?")]));

            inner.Calls.Should().Be(1);

            string.Concat(updates.Select(u => u.Text)).Should().Be(
                "Steps: 1. Find the graph-directed workflow for training.");
        }

        [Fact]
        public async Task AStreamingReplyStillDanglingAfterOneReaskSaysSoRatherThanLoopingAsync()
        {
            var inner = new ScriptedStreamingChatClient(
                ["Steps: 1. Find the graph-directed workflow for"],
                [" training for"]);

            var client = new ProviderMarkupChatClient(inner);

            var updates = await DrainAsync(
                client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "how do I train?")]));

            inner.Calls.Should().Be(2);
            string.Concat(updates.Select(u => u.Text)).Should().Contain("not able to finish that reply");
        }

        private static async Task<IReadOnlyList<ChatResponseUpdate>> DrainAsync(
            IAsyncEnumerable<ChatResponseUpdate> source)
        {
            var updates = new List<ChatResponseUpdate>();

            await foreach (var update in source)
            {
                updates.Add(update);
            }

            return updates;
        }

        private static ChatResponse Response(string text, ChatFinishReason finishReason)
        {
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, text)) { FinishReason = finishReason };
        }

        private sealed class ScriptedChatClient(params ChatResponse[] responses) : IChatClient
        {
            private int next;
            public int Calls { get; private set; }

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                Calls++;
                var response = responses[Math.Min(next, responses.Length - 1)];
                next++;

                return Task.FromResult(response);
            }

            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class ScriptedStreamingChatClient(params string[][] passes) : IChatClient
        {
            private int next;
            public int Calls { get; private set; }

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                Calls++;
                var chunks = passes[Math.Min(next, passes.Length - 1)];
                next++;
                await Task.CompletedTask;

                foreach (var chunk in chunks)
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
                }

                yield return new ChatResponseUpdate(ChatRole.Assistant, string.Empty)
                {
                    FinishReason = ChatFinishReason.Stop
                };
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