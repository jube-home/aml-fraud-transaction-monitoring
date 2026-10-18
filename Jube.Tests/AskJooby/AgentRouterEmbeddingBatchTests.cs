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
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Observability.Models;
using Jube.AskJooby.Routing;
using Jube.AskJooby.Routing.Interfaces;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AgentRouterEmbeddingBatchTests
    {
        [Fact]
        public async Task TheWholeRoutingCorpusIsEmbeddedInOneRequestAndNotOnePerExemplarAsync()
        {
            var embedder = new CountingEmbeddingGenerator();
            var availableAgents = AgentDefinitions.Available(_ => true);

            await AgentRouter.RouteAsync(
                "What's a reasonable velocity threshold for this merchant category?",
                new StubRoutingChatClient("general"), embedder, availableAgents);

            embedder.Requests.Should().Be(1,
                "the exemplars are compile time constants and the task is one string, so routing is "
                + "one batch. Embedding them one at a time cost fifty five serialised round trips on "
                + "every cold cache, spent before a single token reached the chat provider -- and "
                + "spent in full even when the chat credential was wrong and the turn was about to "
                + "fail anyway");

            embedder.Values.Should().BeGreaterThan(1,
                "the batch must actually carry the exemplars, not just the task");
        }

        [Fact]
        public async Task WarmingTheCacheTakesOneRequestAndLeavesTheFirstTurnNothingToEmbedButItsOwnTaskAsync()
        {
            var warmer = new CountingEmbeddingGenerator();

            var warmed = await AgentRouter.WarmExemplarCacheAsync(warmer, CancellationToken.None);

            warmer.Requests.Should().BeLessThanOrEqualTo(1,
                "warming is one batch of compile time constants, not one call per exemplar");

            (warmed >= 0).Should().BeTrue();

            var firstTurn = new CountingEmbeddingGenerator();

            await AgentRouter.RouteAsync("an entirely unseen question about nothing in particular",
                new StubRoutingChatClient("general"), firstTurn, AgentDefinitions.Available(_ => true));

            firstTurn.Values.Should().Be(1,
                "after warming, the first caller of the process embeds only their own question -- "
                + "which is the whole point of doing it at startup");
        }

        [Fact]
        public async Task AnEmbeddingProviderThatRejectsTheRequestFallsThroughAndSaysSoAsync()
        {
            var ledger = new RecordingLedger();

            var agentType = await AgentRouter.RouteAsync(
                "What's a reasonable velocity threshold for this merchant category?",
                new StubRoutingChatClient("model-admin"), new ThrowingEmbeddingGenerator(),
                AgentDefinitions.Available(_ => true), CancellationToken.None, null, null, ledger);

            agentType.Should().Be("model-admin",
                "the embedding stage exists only to avoid paying for a model call, so its failure "
                + "must cost the turn nothing -- the model stage answers the same question");

            ledger.Last!.Stage.Should().Be(AgentRoutingStage.Model,
                "falling through to the model tier is the expensive outcome and the one that used to "
                + "be indistinguishable from a slow model");

            (ledger.Last!.Error is null).Should().BeFalse("a swallowed failure is how this stayed invisible");
        }

        [Fact]
        public async Task TheLedgerSaysWhichTierDecidedAndWhetherEmbeddingsWereReachedForAsync()
        {
            var patternLedger = new RecordingLedger();

            await AgentRouter.RouteAsync(
                "Please look up what the documentation says about TTL counters",
                new StubRoutingChatClient("general"), new CountingEmbeddingGenerator(),
                AgentDefinitions.Available(_ => true), CancellationToken.None, null, null, patternLedger);

            patternLedger.Last!.Stage.Should().Be(AgentRoutingStage.Pattern);
            patternLedger.Last!.AgentType.Should().Be("researcher");

            patternLedger.Last!.EmbeddingRequests.Should().Be(0,
                "a pattern match settles it before any provider is contacted, and the record has to "
                + "show that no embedding was reached for on this call");
        }

        [Fact]
        public async Task ASecondRoutingCallEmbedsOnlyTheNewTaskBecauseTheExemplarsAreCachedAsync()
        {
            var availableAgents = AgentDefinitions.Available(_ => true);

            await AgentRouter.RouteAsync("warm the exemplar cache please",
                new StubRoutingChatClient("general"), new CountingEmbeddingGenerator(), availableAgents);

            var embedder = new CountingEmbeddingGenerator();

            await AgentRouter.RouteAsync("and now a second unrelated question entirely",
                new StubRoutingChatClient("general"), embedder, availableAgents);

            embedder.Requests.Should().Be(1);

            embedder.Values.Should().Be(1,
                "the exemplars never change between builds, so a warm cache leaves only the live task");
        }

        private sealed class CountingEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
        {
            public int Requests { get; private set; }
            public int Values { get; private set; }

            public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
                IEnumerable<string> values, EmbeddingGenerationOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                var materialised = values.ToList();

                Requests++;
                Values += materialised.Count;

                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
                    materialised.Select(_ => new Embedding<float>(new[] { 0.01f, 0.01f, 0.01f }))));
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class StubRoutingChatClient(string reply) : IChatClient
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

        private sealed class ThrowingEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
        {
            public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
                IEnumerable<string> values, EmbeddingGenerationOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("HTTP 401 (invalid_request_error): bad credential");
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class RecordingLedger : IAgentRoutingLedger
        {
            public AgentRoutingDecision? Last { get; private set; }

            public Task RecordAsync(AgentRoutingDecision decision, CancellationToken token = default)
            {
                Last = decision;

                return Task.CompletedTask;
            }
        }
    }
}