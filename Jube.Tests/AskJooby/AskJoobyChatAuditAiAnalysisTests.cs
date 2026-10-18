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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Service.AskJoobyConversation;
using Jube.Test.Infrastructure;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyChatAuditAiAnalysisTests
    {
        private static readonly byte[] emptyJson = Encoding.UTF8.GetBytes("{}");

        [Fact]
        public void BuildPromptIncludesTheTitleAndEveryReport()
        {
            var prompt = AskJoobyChatAuditAiAnalysis.BuildPrompt(
                "Investigating a stuck handoff", [], emptyJson, emptyJson, emptyJson, emptyJson, null, null);

            prompt.Should().Contain("Investigating a stuck handoff");
            prompt.Should().Contain("Health Signals");
            prompt.Should().Contain("RAG & Tooling Gap Analysis");
            prompt.Should().Contain("RAG Performance");
            prompt.Should().Contain("Context Efficiency");
        }

        [Fact]
        public void BuildPromptGroundsInTheInvolvedAgentsOwnInstructionsWhenKnown()
        {
            var run = new AskJoobyRun { Id = 1, AgentType = "validator" };

            var prompt = AskJoobyChatAuditAiAnalysis.BuildPrompt(
                "Test", [run], emptyJson, emptyJson, emptyJson, emptyJson, null, null);

            prompt.Should().Contain("What each agent that appeared in this conversation was told to do");
            prompt.Should().Contain("Jadek");
        }

        [Fact]
        public void BuildPromptOmitsGroundingWhenNoAgentTypesAreKnown()
        {
            var run = new AskJoobyRun { Id = 1, AgentType = "some-unregistered-type" };

            var prompt = AskJoobyChatAuditAiAnalysis.BuildPrompt(
                "Test", [run], emptyJson, emptyJson, emptyJson, emptyJson, null, null);

            prompt.Should().NotContain("What each agent that appeared in this conversation was told to do");
        }

        [Fact]
        public void BuildPromptIncludesCodeGroundingSectionsWhenGiven()
        {
            var prompt = AskJoobyChatAuditAiAnalysis.BuildPrompt(
                "Test", [], emptyJson, emptyJson, emptyJson, emptyJson,
                "## Core Orchestration Code\n\nStartup wiring here.",
                "## Agent Implementation Code\n\nAgent tool code here.");

            prompt.Should().Contain("Core Orchestration Code");
            prompt.Should().Contain("Startup wiring here.");
            prompt.Should().Contain("Agent Implementation Code");
            prompt.Should().Contain("Agent tool code here.");
        }

        [Fact]
        public void BuildPromptOmitsCodeGroundingSectionsWhenNotGiven()
        {
            var prompt = AskJoobyChatAuditAiAnalysis.BuildPrompt(
                "Test", [], emptyJson, emptyJson, emptyJson, emptyJson, null, null);

            prompt.Should().NotContain("Core Orchestration Code");
            prompt.Should().NotContain("Agent Implementation Code");
        }

        [Fact]
        public async Task AnalyzeAsyncReturnsTheTrimmedResponseTextOnSuccessAsync()
        {
            var client = new FakeChatClient(_ => Task.FromResult("  Looks fine.  "));

            var result = await AskJoobyChatAuditAiAnalysis.AnalyzeAsync(client, "prompt", 5, null, TestLog.NoOp,
                CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Text.Should().Be("Looks fine.");
        }

        [Fact]
        public async Task AnalyzeAsyncReportsTheFailureReasonRatherThanThrowingWhenTheCallFailsAsync()
        {
            var client = new FakeChatClient(_ => throw new InvalidOperationException("provider is down"));

            var result = await AskJoobyChatAuditAiAnalysis.AnalyzeAsync(client, "prompt", 5, null, TestLog.NoOp,
                CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Contain("provider is down");
        }

        [Fact]
        public async Task AnalyzeAsyncReportsATimeoutReasonRatherThanHangingWhenTheCallTimesOutAsync()
        {
            var client = new FakeChatClient(async token =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);

                return "too slow";
            });

            var result = await AskJoobyChatAuditAiAnalysis.AnalyzeAsync(client, "prompt", 1, null, TestLog.NoOp,
                CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Contain("Timed out after 1s");
        }

        [Fact]
        public async Task AnalyzeAsyncReportsAFailureReasonForAnEmptyResponseAsync()
        {
            var client = new FakeChatClient(_ => Task.FromResult("   "));

            var result = await AskJoobyChatAuditAiAnalysis.AnalyzeAsync(client, "prompt", 5, null, TestLog.NoOp,
                CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Contain("empty response");
        }

        private sealed class FakeChatClient(Func<CancellationToken, Task<string>> respond) : IChatClient
        {
            public async Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                var text = await respond(cancellationToken);

                return new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var text = await respond(cancellationToken);

                yield return new ChatResponseUpdate(ChatRole.Assistant, text);
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