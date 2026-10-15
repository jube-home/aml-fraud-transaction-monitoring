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
using Jube.AskJooby.Observability;
using Microsoft.Extensions.AI;
using Xunit;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class UserQueryGuardChatClientTests
    {
        [Fact]
        public async Task PassesThroughWhenANonEmptyUserMessageIsPresentAsync()
        {
            var recordingClient = new RecordingChatClient();
            var client = new UserQueryGuardChatClient(recordingClient);

            await client.GetResponseAsync(
                [new ChatMessage(ChatRole.System, "be helpful"), new ChatMessage(ChatRole.User, "hi")]);

            recordingClient.WasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task ThrowsBeforeCallingTheInnerClientWhenThereIsNoUserMessageAsync()
        {
            var recordingClient = new RecordingChatClient();
            var client = new UserQueryGuardChatClient(recordingClient);

            var act = () => client.GetResponseAsync(
                [new ChatMessage(ChatRole.System, "be helpful"), new ChatMessage(ChatRole.Assistant, "ok")]);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*no non-empty user message*");

            recordingClient.WasCalled.Should().BeFalse();
        }

        [Fact]
        public async Task ThrowsWhenTheOnlyUserMessageIsBlankAsync()
        {
            var recordingClient = new RecordingChatClient();
            var client = new UserQueryGuardChatClient(recordingClient);

            var act = () => DrainAsync(
                client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "   ")]));

            await act.Should().ThrowAsync<InvalidOperationException>();
            recordingClient.WasCalled.Should().BeFalse();
        }

        private static async Task DrainAsync(IAsyncEnumerable<ChatResponseUpdate> source)
        {
            await foreach (var _ in source)
            {
            }
        }

        private sealed class RecordingChatClient : IChatClient
        {
            public bool WasCalled { get; private set; }

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                WasCalled = true;

                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                WasCalled = true;
                await Task.CompletedTask;
                yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
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