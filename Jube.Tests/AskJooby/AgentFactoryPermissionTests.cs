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
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.Agents.Exceptions;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.History;
using Jube.AskJooby.Memory;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.AskJooby.ScriptExecution.Models;
using Jube.AskJooby.Security;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AgentFactoryPermissionTests
    {
        [Fact]
        public void IsPermittedReturnsTrueWhenNoPermissionsAreRequired()
        {
            AgentDefinitions.IsPermitted([], _ => false).Should().BeTrue();
        }

        [Fact]
        public void IsPermittedDefersToHasPermissionWhenPermissionsAreRequired()
        {
            AgentDefinitions.IsPermitted([AskJoobyPermissions.Access], _ => true).Should().BeTrue();
            AgentDefinitions.IsPermitted([AskJoobyPermissions.Access], _ => false).Should().BeFalse();
        }

        [Fact]
        public void AvailableExcludesAgentTypesTheCallerIsNotPermittedToUse()
        {
            var granted = AgentDefinitions.Available(_ => true);
            var denied = AgentDefinitions.Available(_ => false);

            granted.Should().ContainKey("general");
            denied.Should().NotContainKey("general");
        }

        [Fact]
        public void AvailableExcludesInternalAgentTypesLikeTheValidatorEvenWhenPermitted()
        {
            var granted = AgentDefinitions.Available(_ => true);

            granted.Should().NotContainKey("validator");
        }

        [Fact]
        public async Task CreateAsyncStillBuildsAnInternalAgentTypeByNameForServiceCodeAsync()
        {
            var context = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var (agent, runId) = await AgentFactory.CreateAsync(
                "validator", new FakeChatClient(), new FakeRagClient(), new FakeMemoryClient(),
                new FakeHistoryClient(), "task", 0, context);

            agent.Should().NotBeNull();
            runId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public Task CreateAsyncThrowsWhenTheCallerLacksTheAgentTypesRequiredPermissionAsync()
        {
            var context = new RunContext(null, null, null, _ => false);

            var act = () => AgentFactory.CreateAsync(
                "general", new FakeChatClient(), new FakeRagClient(), new FakeMemoryClient(),
                new FakeHistoryClient(), "task", 0, context);

            return act.Should().ThrowAsync<AgentPermissionDeniedException>();
        }

        [Fact]
        public async Task CreateAsyncBuildsOnlyThePermittedToolsWhenTheCallerIsPermittedAsync()
        {
            var context = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access));

            var (agent, runId) = await AgentFactory.CreateAsync(
                "general", new FakeChatClient(), new FakeRagClient(), new FakeMemoryClient(),
                new FakeHistoryClient(), "task", 0, context);

            agent.Should().NotBeNull();
            runId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void OnlyTheGeneralAgentIsOfferedAskClarifyingQuestion()
        {
            var context = new RunContext(null, null, null, _ => true);

            var args = new AgentToolFactoryArgs(
                new FakeRagClient(), new FakeMemoryClient(), new FakeHistoryClient(), new FakeChatClient(), 0,
                context);

            var generalTools = AgentDefinitions.All["general"].ToolsFactory(args);
            var researcherTools = AgentDefinitions.All["researcher"].ToolsFactory(args);

            generalTools.Should().Contain(t => t.Function.Name == "AskClarifyingQuestion");
            researcherTools.Should().NotContain(t => t.Function.Name == "AskClarifyingQuestion");
        }

        [Fact]
        public void CountAvailableToolsIncludesTheDatabaseToolsOnlyWhenDatabaseAccessIsAvailable()
        {
            var withDatabase = AgentDefinitions.CountAvailableTools(_ => true, true);
            var withoutDatabase = AgentDefinitions.CountAvailableTools(_ => true, false);

            (withDatabase - withoutDatabase).Should().Be(16);
        }

        [Fact]
        public void TheFourRScriptToolsAreOfferedToEveryAgentOnlyWhenScriptExecutionIsWiredUp()
        {
            var context = new RunContext(null, null, null, _ => true);

            var withScriptExecution = new AgentToolFactoryArgs(
                new FakeRagClient(), new FakeMemoryClient(), new FakeHistoryClient(), new FakeChatClient(), 0,
                context, ScriptExecution: new NeverCalledScriptExecutionClient());

            var withoutScriptExecution = new AgentToolFactoryArgs(
                new FakeRagClient(), new FakeMemoryClient(), new FakeHistoryClient(), new FakeChatClient(), 0,
                context);

            string[] rScriptToolNames = ["CheckRServerHealth", "ListRPackages", "ValidateRScript", "RunScript"];

            foreach (var agentType in new[] { "general", "researcher" })
            {
                var withTools = AgentDefinitions.All[agentType].ToolsFactory(withScriptExecution)
                    .Select(tool => tool.Function.Name).ToList();
                var withoutTools = AgentDefinitions.All[agentType].ToolsFactory(withoutScriptExecution)
                    .Select(tool => tool.Function.Name).ToList();

                withTools.Should().Contain(rScriptToolNames);
                withoutTools.Should().NotContain(rScriptToolNames);
            }
        }

        private sealed class NeverCalledScriptExecutionClient : IScriptExecutionClient
        {
            public Task<RServerHealthResult> CheckHealthAsync(CancellationToken token = default)
            {
                throw new NotSupportedException("Exists only to make ScriptExecution non-null.");
            }

            public Task<RServerPackagesResult> ListPackagesAsync(CancellationToken token = default)
            {
                throw new NotSupportedException("Exists only to make ScriptExecution non-null.");
            }

            public Task<RServerValidateResult> ValidateAsync(string script, CancellationToken token = default)
            {
                throw new NotSupportedException("Exists only to make ScriptExecution non-null.");
            }

            public Task<RunScriptResult> RunScriptAsync(string script, int? timeoutSeconds,
                IReadOnlyList<RScriptDataframeRequest>? dataframes = null,
                IReadOnlyList<RScriptVariableRequest>? variables = null, CancellationToken token = default)
            {
                throw new NotSupportedException("Exists only to make ScriptExecution non-null.");
            }
        }

        [Fact]
        public void CountAvailableToolsAlwaysCountsThink()
        {
            AgentDefinitions.CountAvailableTools(_ => false, false)
                .Should().BeGreaterThan(0, "Think is registered with no permissions and no switch");
        }

        [Fact]
        public void CountAvailableToolsStillCountsThePermissionFreeToolsWhenEverythingElseIsDenied()
        {
            var count = AgentDefinitions.CountAvailableTools(_ => false, true);

            count.Should().Be(4);
        }

        [Fact]
        public void CountAvailableJoobriksExcludesGeneralAndIsInternalAgentTypes()
        {
            var available = AgentDefinitions.Available(_ => true).Keys.ToList();

            available.Should().Contain("general")
                .And.NotContain("validator", "an IsInternal agent is never offered to SpawnAgent");

            AgentDefinitions.CountAvailableJoobriks(_ => true).Should().Be(available.Count - 1,
                "the Worker itself is never a delegation target, and nothing else is excluded");
        }

        [Fact]
        public void CountAvailableJoobriksIsZeroWhenPermissionIsDenied()
        {
            AgentDefinitions.CountAvailableJoobriks(_ => false).Should().Be(0);
        }

        [Fact]
        public async Task CreateAsyncStillBuildsAnAgentThatHasReachedTheSpawnDepthCeilingAsync()
        {
            var context = new RunContext(
                null, null, null, specs => specs.Contains(AskJoobyPermissions.Access), MaxDepth: 0);

            var (agent, runId) = await AgentFactory.CreateAsync(
                "general", new FakeChatClient(), new FakeRagClient(), new FakeMemoryClient(),
                new FakeHistoryClient(), "task", 0, context);

            agent.Should().NotBeNull();
            runId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public Task CreateAsyncThrowsWhenTheCallerIsNotLandlordForALandlordOnlyAgentTypeAsync()
        {
            var context = new RunContext(null, null, null, _ => true, IsLandlord: false);

            var act = () => AgentFactory.CreateAsync(
                "tenant-registry-inspector", new FakeChatClient(), new FakeRagClient(), new FakeMemoryClient(),
                new FakeHistoryClient(), "task", 0, context);

            return act.Should().ThrowAsync<AgentPermissionDeniedException>();
        }

        [Fact]
        public async Task CreateAsyncSucceedsWhenTheCallerIsLandlordForALandlordOnlyAgentTypeAsync()
        {
            var context = new RunContext(null, null, null, _ => true, IsLandlord: true);

            var (agent, runId) = await AgentFactory.CreateAsync(
                "tenant-registry-inspector", new FakeChatClient(), new FakeRagClient(), new FakeMemoryClient(),
                new FakeHistoryClient(), "task", 0, context);

            agent.Should().NotBeNull();
            runId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public Task UnknownAgentTypeRemainsAPlainArgumentExceptionAsync()
        {
            var context = new RunContext(null, null, null, _ => true);

            var act = () => AgentFactory.CreateAsync(
                "not-a-real-agent-type", new FakeChatClient(), new FakeRagClient(), new FakeMemoryClient(),
                new FakeHistoryClient(), "task", 0, context);

            return act.Should().ThrowAsync<ArgumentException>();
        }

        private sealed class FakeRagClient : IRagClient
        {
            public Task<IReadOnlyList<RagChunk>> SearchAsync(
                string query, int topK = 5, double minScore = 0, int candidates = 0, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<RagChunk>>([]);
            }
        }

        private sealed class FakeMemoryClient : IMemoryClient
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

        private sealed class FakeHistoryClient : IConversationHistoryClient
        {
            public Task<IReadOnlyList<HistoryEntry>> SearchAsync(
                string query, string scope, int topK = 5, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<HistoryEntry>>([]);
            }
        }

        private sealed class FakeChatClient : IChatClient
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