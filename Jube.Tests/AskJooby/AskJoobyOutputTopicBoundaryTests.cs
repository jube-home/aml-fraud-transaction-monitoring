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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Core.Abstractions;
using AgentGuard.Core.Builders;
using AgentGuard.Core.Guardrails;
using FluentAssertions;
using Jube.AskJooby.Security;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyOutputTopicBoundaryTests
    {
        private const string ValidatorPassVerdict = "STATUS: PASSED\nREASON: \nKIND: ";

        [Fact]
        public async Task A_bare_validator_pass_verdict_is_off_topic_under_the_plain_default_topic_promptAsync()
        {
            var judge = new ScriptedTopicJudgeChatClient();

            var pipeline = new GuardrailPipeline(
                new GuardrailPolicyBuilder("reproduction")
                    .EnforceTopicBoundaryWithLlm(judge, GetAllowedTopics())
                    .Build(),
                NullLogger<GuardrailPipeline>.Instance);

            var result = await pipeline.RunAsync(new GuardrailContext
            {
                Text = ValidatorPassVerdict,
                Phase = GuardrailPhase.Input
            });

            result.IsBlocked.Should().BeTrue(
                "reproduces the real transcript: a terse STATUS/REASON/KIND verdict with nothing " +
                "marking it as a validator's own verdict reads as unrelated to fraud/AML to a " +
                "classifier given only the bare allowed-topics list");
        }

        [Fact]
        public async Task AskJoobys_own_output_pipeline_lets_the_same_validator_verdict_throughAsync()
        {
            var judge = new ScriptedTopicJudgeChatClient();
            var pipeline = BuildOutputPipeline(judge);

            var result = await pipeline.RunAsync(new GuardrailContext
            {
                Text = ValidatorPassVerdict,
                Phase = GuardrailPhase.Input
            });

            result.IsBlocked.Should().BeFalse(
                "AskJoobyInjectionGuard's output topic-boundary prompt explicitly frames a " +
                "STATUS/REASON/KIND verdict as on-topic, which is what actually fixes the false " +
                "positive seen in the transcript");
        }

        [Fact]
        public async Task A_genuinely_unrelated_reply_is_still_blocked_through_AskJoobys_output_pipelineAsync()
        {
            var judge = new ScriptedTopicJudgeChatClient();
            var pipeline = BuildOutputPipeline(judge);

            var result = await pipeline.RunAsync(new GuardrailContext
            {
                Text = "Here's a recipe for banana bread, step by step.",
                Phase = GuardrailPhase.Input
            });

            result.IsBlocked.Should().BeTrue(
                "the fix only widens what counts as a validator verdict about Jube's own content, " +
                "not the topic boundary itself");
        }

        [Fact]
        public void The_landlord_topic_list_is_the_base_list_plus_exactly_one_platform_ops_topic()
        {
            var baseTopics = GetAllowedTopics();
            var landlordTopics = GetAllowedTopicsForLandlord();

            landlordTopics.Should().HaveCount(baseTopics.Length + 1,
                "the Landlord widening adds one topic on top of the base two, never replaces or removes any");

            landlordTopics.Take(baseTopics.Length).Should().BeEquivalentTo(baseTopics,
                options => options.WithStrictOrdering(),
                "a non-Landlord caller's own AllowedTopics must stay byte-for-byte what they were before this change");

            var extraTopic = landlordTopics.Last();

            extraTopic.Should().Contain("Postgres").And.Contain("Landlord",
                "the extra topic exists specifically to cover the Landlord-only platform-ops " +
                "surface (Postgres/Redis/Docker/etcd/Patroni) that caused the real false positive");
        }

        [Fact]
        public async Task CheckAsync_and_CheckOutputAsync_accept_an_isLandlord_flag_without_throwing_either_wayAsync()
        {
            await AskJoobyInjectionGuard.CheckAsync("does the infrastructure look ok", isLandlord: false);
            await AskJoobyInjectionGuard.CheckAsync("does the infrastructure look ok", isLandlord: true);
            await AskJoobyInjectionGuard.CheckOutputAsync("Postgres cache hit ratio is 99.98%.", isLandlord: false);
            await AskJoobyInjectionGuard.CheckOutputAsync("Postgres cache hit ratio is 99.98%.", isLandlord: true);
        }

        private static string[] GetAllowedTopics()
        {
            var field = typeof(AskJoobyInjectionGuard).GetField(
                "allowedTopics", BindingFlags.NonPublic | BindingFlags.Static)!;

            return (string[])field.GetValue(null)!;
        }

        private static string[] GetAllowedTopicsForLandlord()
        {
            var field = typeof(AskJoobyInjectionGuard).GetField(
                "allowedTopicsForLandlord", BindingFlags.NonPublic | BindingFlags.Static)!;

            return (string[])field.GetValue(null)!;
        }

        private static GuardrailPipeline BuildOutputPipeline(IChatClient judge)
        {
            var method = typeof(AskJoobyInjectionGuard).GetMethod(
                "BuildOutputPipeline", BindingFlags.NonPublic | BindingFlags.Static)!;

            return (GuardrailPipeline)method.Invoke(null, [judge, GetAllowedTopics()])!;
        }

        private sealed class ScriptedTopicJudgeChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
                ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                var list = messages.ToList();
                var systemText = list.FirstOrDefault(m => m.Role == ChatRole.System)?.Text ?? string.Empty;
                var userText = list.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;

                var framesValidatorVerdictsAsInScope =
                    systemText.Contains("STATUS/REASON/KIND", StringComparison.OrdinalIgnoreCase);

                var looksLikeAValidatorVerdict =
                    userText.TrimStart().StartsWith("STATUS:", StringComparison.OrdinalIgnoreCase);

                var mentionsAnAllowedTopicKeyword =
                    userText.Contains("fraud", StringComparison.OrdinalIgnoreCase)
                    || userText.Contains("AML", StringComparison.Ordinal)
                    || userText.Contains("rule", StringComparison.OrdinalIgnoreCase);

                var onTopic = (framesValidatorVerdictsAsInScope && looksLikeAValidatorVerdict)
                              || mentionsAnAllowedTopicKeyword;

                return Task.FromResult(new ChatResponse(
                    new ChatMessage(ChatRole.Assistant, onTopic ? "ON_TOPIC" : "OFF_TOPIC")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var response = await GetResponseAsync(messages, options, cancellationToken)
                    .ConfigureAwait(false);

                yield return new ChatResponseUpdate(response.Messages[0].Role, response.Text);
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