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
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.Routing;
using Jube.AskJooby.Security;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AgentRouterPermissionTests
    {
        [Fact]
        public async Task RouteAsyncNeverReturnsAnAgentTypeNotInTheAvailableSetAsync()
        {
            var availableAgents = AgentDefinitions.Available(specs => specs.Contains(AskJoobyPermissions.Access));
            availableAgents.Should().ContainKey("researcher");

            var deniedAgents = new Dictionary<string, AgentDefinition>();
            var chatClient = new FakeRoutingChatClient("researcher");

            var agentType = await AgentRouter.RouteAsync(
                "Look up what the documentation says about abstraction rules", chatClient, null, deniedAgents);

            agentType.Should().Be("general");
        }

        [Fact]
        public async Task RouteAsyncRegexMatchesAnObviousResearchTaskAsync()
        {
            var availableAgents = AgentDefinitions.Available(_ => true);
            var chatClient = new FakeRoutingChatClient("general");

            var agentType = await AgentRouter.RouteAsync(
                "Please look up what the documentation says about TTL counters", chatClient, null, availableAgents);

            agentType.Should().Be("researcher");
        }

        [Fact]
        public async Task RouteAsyncFallsBackToGeneralWhenNothingMatchesAsync()
        {
            var availableAgents = AgentDefinitions.Available(_ => true);
            var chatClient = new FakeRoutingChatClient("general");

            var agentType = await AgentRouter.RouteAsync(
                "What's a reasonable velocity threshold for this merchant category?", chatClient, null,
                availableAgents);

            agentType.Should().Be("general");
        }

        [Fact]
        public async Task RouteAsyncUsesTheLlmFallbackWhenNoEmbedderIsConfiguredAndRegexDoesNotMatchAsync()
        {
            var availableAgents = AgentDefinitions.Available(_ => true);
            var chatClient = new FakeRoutingChatClient("researcher");

            var agentType = await AgentRouter.RouteAsync(
                "I need a written summary of everything the knowledge base has on sanctions matching",
                chatClient, null, availableAgents);

            agentType.Should().Be("researcher");
        }

        private sealed class FakeRoutingChatClient(string reply) : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
                yield return new ChatResponseUpdate(ChatRole.Assistant, reply);
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