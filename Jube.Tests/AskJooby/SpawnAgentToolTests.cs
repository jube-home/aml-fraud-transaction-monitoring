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
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Exceptions;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.History;
using Jube.AskJooby.Memory;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.Security;
using Jube.AskJooby.Tools.Delegation;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Xunit;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class SpawnAgentToolTests
    {
        private static string ResearcherName => AgentDefinitions.All["researcher"].DisplayName!;

        [Fact]
        public async Task SpawnAgentIsRefusedWhenThePlanHasNoStepForTheSpecialistsStableAsync()
        {
            var childContext = new RunContext(null, null, null, _ => true);
            var turn = new TurnActivityLedger();

            turn.RecordPlan("1. Count the cases -> case-analyst stable, case-analyst-trend. Check: a count comes back.",
                new HashSet<string> { "case-analyst" });

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0,
                childContext with { TurnActivity = turn });

            var refused = await tool.SpawnAgent("advanced-analytics-model-inspector", "Inspect the model");

            refused.Should().Contain("The plan has no step for the model-risk stable");
            turn.AgentCalls.Should().Be(0);
        }

        [Fact]
        public async Task AStableIsRefusedAFanOutUntilItHasItsOwnPlanAsync()
        {
            var childContext = new RunContext(null, null, null, _ => true);
            var turn = new TurnActivityLedger();

            turn.RecordPlan("1. Inspect -> model-risk stable, advanced-analytics-model-inspector. Check: it is read.",
                new HashSet<string> { "model-risk" });

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 1, childContext with { TurnActivity = turn },
                spawnerAgentType: "model-risk");

            var refused = await tool.SpawnAgents(
            [
                new SpawnAgentSubTask("advanced-analytics-model-inspector", "Inspect the first model"),
                new SpawnAgentSubTask("advanced-analytics-model-author", "Author the second model")
            ]);

            refused.Should().Contain("write a plan for your stable first");
            turn.AgentCalls.Should().Be(0);
        }

        private static RunContext WithPlan(RunContext context)
        {
            var turn = new TurnActivityLedger();

            turn.RecordPlan("1. Look up the counters -> researcher. Check: the counter names come back.",
                new HashSet<string>());

            return context with { TurnActivity = turn };
        }

        [Fact]
        public Task SpawnAgentThrowsWhenTheCallerLacksThePermissionForTheRequestedSpecialistAsync()
        {
            var childContext = new RunContext(null, null, null, _ => false);

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            var act = () => tool.SpawnAgent("researcher", "Look up TTL counters");

            return act.Should().ThrowAsync<AgentPermissionDeniedException>();
        }

        [Fact]
        public Task SpawnAgentThrowsForAnUnknownAgentTypeAsync()
        {
            var childContext = new RunContext(null, null, null, _ => true);

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            var act = () => tool.SpawnAgent("not-a-real-specialist", "do something");

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SpawnAgentDelegatesToAPermittedSpecialistAndReturnsItsFinalAnswerAsync()
        {
            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            var result = await tool.SpawnAgent("researcher", "Look up TTL counters");

            result.Should().StartWith("ok");
        }

        [Fact]
        public async Task SpawnAgentTellsTheCallerToSpeakUpWhenAHandBackLeavesNowhereFurtherToRouteItAsync()
        {
            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var tool = new SpawnAgentTool(new HandBackCallingFakeChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            var result = await tool.SpawnAgent("researcher", "What is the capital of Mongolia?");

            result.Should().Contain("say so to the person now, plainly",
                "a hand-back that reaches a caller with no further specialist to try and nobody above it "
                + "must still end in a reply to the person -- an empty final turn here is exactly what "
                + "reads to the caller as the conversation dying rather than redirecting");

            result.Should().Contain("Ending this turn with no reply at all is not a valid way to act on "
                                    + "a hand-back.");
        }

        [Fact]
        public async Task SpawnAgentForwardsTheSpawnedJoobriksOwnStepsLiveLabelledWithItsAgentTypeAsync()
        {
            var channel = Channel.CreateUnbounded<AgentStreamItem>();

            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access),
                DelegationChannel: channel.Writer);

            var tool = new SpawnAgentTool(new ToolCallingFakeChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            var result = await tool.SpawnAgent("researcher", "Look up TTL counters");
            channel.Writer.Complete();

            var forwarded = new List<AgentStreamItem>();

            await foreach (var item in channel.Reader.ReadAllAsync())
            {
                forwarded.Add(item);
            }

            result.Should().StartWith("Here's what I found.");
            forwarded.Should().Contain(i => i.Kind == AgentStreamItemKind.Step && i.AgentLabel == ResearcherName);
            forwarded.Should().NotContain(i => i.Kind == AgentStreamItemKind.Text);
        }

        [Fact]
        public async Task SpawnAgentForwardsTheSpawnedJoobriksOwnReasoningLabelledWithItsAgentTypeAsync()
        {
            var channel = Channel.CreateUnbounded<AgentStreamItem>();

            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access),
                true, DelegationChannel: channel.Writer);

            var tool = new SpawnAgentTool(new ReasoningFakeChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            await tool.SpawnAgent("researcher", "Look up TTL counters");
            channel.Writer.Complete();

            var forwarded = new List<AgentStreamItem>();

            await foreach (var item in channel.Reader.ReadAllAsync())
            {
                forwarded.Add(item);
            }

            forwarded.Should().Contain(
                i => i.Kind == AgentStreamItemKind.Thinking && i.AgentLabel == ResearcherName,
                "a Joobrik that announces itself and then reasons in silence is indistinguishable "
                + "from a hang, and during a fan out that silence is where most of the time goes");
        }

        [Fact]
        public async Task SpawnAgentAnnouncesTheAgentItIsPickingUpBeforeItStartsWorkingAsync()
        {
            var channel = Channel.CreateUnbounded<AgentStreamItem>();

            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access),
                DelegationChannel: channel.Writer);

            var tool = new SpawnAgentTool(new ToolCallingFakeChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            await tool.SpawnAgent("researcher", "Look up TTL counters");
            channel.Writer.Complete();

            var forwarded = new List<AgentStreamItem>();

            await foreach (var item in channel.Reader.ReadAllAsync())
            {
                forwarded.Add(item);
            }

            forwarded.Should().NotBeEmpty();

            forwarded[0].Kind.Should().Be(AgentStreamItemKind.Delegation,
                "a spawn announces the agent taking it on before any of its work appears, so the "
                + "reader is never shown tool activity from an agent they were not told about");

            forwarded[0].Text.Should().Contain(ResearcherName);
        }

        [Fact]
        public Task SpawnAgentsThrowsForFewerThanTwoSubTasksAsync()
        {
            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            var act = () => tool.SpawnAgents([new SpawnAgentSubTask("researcher", "only one")]);

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public Task SpawnAgentsThrowsWhenExceedingMaxFanOutAsync()
        {
            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access), MaxFanOut: 2);

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            var act = () => tool.SpawnAgents(
            [
                new SpawnAgentSubTask("researcher", "a"),
                new SpawnAgentSubTask("researcher", "b"),
                new SpawnAgentSubTask("researcher", "c")
            ]);

            return act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SpawnAgentsRunsIndependentSubTasksConcurrentlyAndLabelsEachWithAnOrdinalAsync()
        {
            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var chatClient = new DelayedFakeChatClient();

            var tool = new SpawnAgentTool(chatClient, new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            var subTasks = new List<SpawnAgentSubTask>
            {
                new("researcher", "Sub-task A"),
                new("researcher", "Sub-task B"),
                new("researcher", "Sub-task C")
            };

            var result = await tool.SpawnAgents(subTasks);

            chatClient.MaxConcurrentCalls.Should().Be(3,
                "all three sub-tasks must be in flight at once, not run one after another");

            result.Should().Contain("[researcher #1] Sub-task A")
                .And.Contain("[researcher #2] Sub-task B")
                .And.Contain("[researcher #3] Sub-task C");
        }

        [Fact]
        public async Task ACutOffChildIsRecordedAsCancelledRatherThanLeftRunningAsync()
        {
            var recorder = new RecordingRunRecorder();

            var childContext = new RunContext(
                recorder, null, null, specs => specs.Contains(AskJoobyPermissions.Access),
                AgentIdleTimeoutSeconds: 1);

            var tool = new SpawnAgentTool(new SlowFakeChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, WithPlan(childContext));

            await tool.SpawnAgent("researcher", "Look up TTL counters");

            recorder.CompletedStatuses.Should().Contain("Cancelled");
        }

        private sealed class DelayedFakeChatClient : IChatClient
        {
            private int concurrentCalls;
            private int maxConcurrentCalls;
            public int MaxConcurrentCalls => maxConcurrentCalls;

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var current = Interlocked.Increment(ref concurrentCalls);
                InterlockedMax(ref maxConcurrentCalls, current);

                try
                {
                    await Task.Delay(300, cancellationToken);
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "done");
                }
                finally
                {
                    Interlocked.Decrement(ref concurrentCalls);
                }
            }

            private static void InterlockedMax(ref int target, int candidate)
            {
                var initial = target;

                while (candidate > initial)
                {
                    var previous = Interlocked.CompareExchange(ref target, candidate, initial);

                    if (previous == initial)
                    {
                        return;
                    }

                    initial = previous;
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

        private sealed class ToolCallingFakeChatClient : IChatClient
        {
            private int turn;

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
                var currentTurn = ++turn;

                if (currentTurn == 1)
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant,
                    [
                        new FunctionCallContent(
                            "call-1", "SearchRag", new Dictionary<string, object?> { ["query"] = "TTL counters" })
                    ]);
                }
                else
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "Here's what I found.");
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

        private sealed class HandBackCallingFakeChatClient : IChatClient
        {
            private int turn;

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
                var currentTurn = ++turn;

                if (currentTurn == 1)
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant,
                    [
                        new FunctionCallContent("call-1", "HandBack", new Dictionary<string, object?>
                        {
                            ["whatIsNeeded"] = "What is the capital of Mongolia?",
                            ["whatIEstablished"] = "nothing",
                            ["owningAgent"] = null
                        })
                    ]);
                }
                else
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
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

        private sealed class SlowFakeChatClient : IChatClient
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
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                yield return new ChatResponseUpdate(ChatRole.Assistant, "too late");
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class RecordingRunRecorder : IRunRecorder
        {
            public List<string> CompletedStatuses { get; } = [];

            public Task<string> StartRunAsync(string agentType, int depth, string? parentRunId, string task,
                CancellationToken token = default)
            {
                return Task.FromResult("1");
            }

            public Task CompleteRunAsync(string runId, string status, string? result, long durationMs,
                CancellationToken token = default)
            {
                CompletedStatuses.Add(status);

                return Task.CompletedTask;
            }

            public Task RecordToolCallAsync(string runId, string toolName, string? argumentsJson, string? result,
                string status, long durationMs, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public Task RecordModelCallAsync(string runId, string model, int? inputTokens, int? outputTokens,
                long? cachedInputTokens, string? promptPrefixHash, string? usageCountsJson,
                long? promptDetailCachedTokens,
                string? finishReason, long durationMs, long? firstChunkMs, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task ASingleHandoverNeedsNoPlanButIsCountedAsUnplannedAsync()
        {
            var turn = new TurnActivityLedger();

            var childContext = new RunContext(
                    null, null, null, specs => specs.Contains(AskJoobyPermissions.Access)) with
                {
                    TurnActivity = turn
                };

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, childContext);

            var answered = await tool.SpawnAgent("researcher", "Look up TTL counters");

            answered.Should().StartWith("ok");
            turn.AgentCalls.Should().Be(1);
            turn.UnplannedHandovers.Should().Be(1);
        }

        [Fact]
        public async Task GeneralCannotJumpStraightToASpecialistOutsideItsOwnStablesAsync()
        {
            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, childContext,
                spawnerAgentType: "general");

            var refused = await tool.SpawnAgent("advanced-analytics-instance-inspector", "List the instances");

            refused.Should().Contain(
                "advanced-analytics-instance-inspector is not one of the specialists general may hand over to");
        }

        [Fact]
        public async Task GeneralCanStillReachOneOfItsOwnDeclaredStablesAsync()
        {
            var turn = new TurnActivityLedger();

            var childContext = new RunContext(
                    null, null, null, _ => true) with
                {
                    TurnActivity = turn
                };

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, childContext,
                spawnerAgentType: "general");

            var answered = await tool.SpawnAgent("model-risk", "Explain the model's backtest");

            answered.Should().StartWith("ok");
            turn.AgentCalls.Should().Be(1);
        }

        [Fact]
        public async Task SpawnAgentsAcrossTwoStablesIsRefusedUntilAPlanExistsAsync()
        {
            var turn = new TurnActivityLedger();

            var childContext = new RunContext(
                    null, null, null, _ => true) with
                {
                    TurnActivity = turn
                };

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, childContext);

            var batch = new List<SpawnAgentSubTask>
            {
                new("advanced-analytics-model-inspector", "Inspect the model"),
                new("case-analyst-trend", "Count the cases")
            };

            var refused = await tool.SpawnAgents(batch);

            refused.Should().StartWith("No plan has been written for this turn");
            turn.UnplannedHandovers.Should().Be(1);
            turn.AgentCalls.Should().Be(0);
        }

        [Fact]
        public async Task SpawnAgentIsRefusedWhenTheTurnHasNoLedgerAtAllAsync()
        {
            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 0, childContext);

            var refused = await tool.SpawnAgent("researcher", "Look up TTL counters");

            refused.Should().StartWith("No plan has been written for this turn");
        }

        [Fact]
        public async Task ANestedJoobrikIsNotAskedToPlanAgainAsync()
        {
            var childContext = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var tool = new SpawnAgentTool(new FakeSpawnChatClient(), new FakeSpawnRagClient(),
                new FakeSpawnMemoryClient(), new FakeSpawnHistoryClient(), 1, childContext);

            var answered = await tool.SpawnAgent("researcher", "Look up TTL counters");

            answered.Should().Be("ok");
        }

        private sealed class FakeSpawnRagClient : IRagClient
        {
            public Task<IReadOnlyList<RagChunk>> SearchAsync(
                string query, int topK = 5, double minScore = 0, int candidates = 0, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<RagChunk>>([]);
            }
        }

        private sealed class FakeSpawnMemoryClient : IMemoryClient
        {
            public Task<MemoryEntry> RememberAsync(string content, string scope, byte? status = null,
                DateTime? expiresAt = null, string kind = "Fact", CancellationToken token = default)
            {
                return Task.FromResult(new MemoryEntry(1, scope, "user", content));
            }

            public Task<IReadOnlyList<MemoryEntry>> RecallAsync(string query, int topK = 5, DateTime? since = null,
                bool includeReflections = false, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<MemoryEntry>>([]);
            }
        }

        private sealed class FakeSpawnHistoryClient : IConversationHistoryClient
        {
            public Task<IReadOnlyList<HistoryEntry>> SearchAsync(
                string query, string scope, int topK = 5, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<HistoryEntry>>([]);
            }
        }

        private sealed class ReasoningFakeChatClient : IChatClient
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

                const string json = """
                                    {"id":"c1","object":"chat.completion.chunk","created":1,"model":"reasoner",
                                     "choices":[{"index":0,"delta":{"reasoning_content":"weighing it up"},
                                     "finish_reason":null}]}
                                    """;
#pragma warning disable SCME0001

                var raw = ModelReaderWriter
                    .Read<StreamingChatCompletionUpdate>(BinaryData.FromString(json));
#pragma warning restore SCME0001

                yield return new ChatResponseUpdate(ChatRole.Assistant, (string?)null)
                {
                    RawRepresentation = raw
                };

                yield return new ChatResponseUpdate(ChatRole.Assistant, "Here's what I found.");
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakeSpawnChatClient : IChatClient
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