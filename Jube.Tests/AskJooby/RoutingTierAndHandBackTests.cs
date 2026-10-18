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
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Delegation;
using Jube.AskJooby.History;
using Jube.AskJooby.Memory;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.SqlQuery.Models;
using Jube.AskJooby.Tools.Delegation;
using Jube.AskJooby.Tools.Delegation.Interfaces;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class RoutingTierAndHandBackTests
    {
        private const string Tier = "model-admin.activation";
        private const string Leaf = "entity-analysis-model-activation-rule-author";

        private static readonly string[] freeFormSql =
        [
            "ListQueryableTables", "RunSqlQuery", "ValidateSqlQuery", "ExplainSqlQuery",
            "InspectTableStatistics", "AnalyseQueryHistory"
        ];

        private static IReadOnlyList<string> ToolNames(string agentType, IHandBackSignal? handBack = null,
            IReadOnlyDictionary<string, IReadOnlyList<AgentToolDefinition>>? bound = null)
        {
            var context = new RunContext(null, null, null, _ => true, HandBack: handBack,
                ServiceBoundTools: bound);

            var args = new AgentToolFactoryArgs(
                new StubRag(), new StubMemory(), new StubHistory(), new StubChat(), 1, context,
                new StubSql(), new StubCache());

            return
            [
                .. AgentDefinitions.All[agentType].ToolsFactory(args)
                    .Select(tool => tool.Function.Name)
            ];
        }

        [Fact]
        public void ARoutingTierKeepsFreeFormSqlAsTheSameUniversalFallbackEveryAgentHolds()
        {
            var names = ToolNames(Tier);

            names.Should().Contain(freeFormSql,
                "SQL is the same last-resort fallback every agent carries, exactly like SearchRag -- a "
                + "tier that cannot name the one real owner of an ad hoc cross-slice question must still "
                + "be able to answer it rather than fabricate a hand-back target");
        }

        [Fact]
        public void TheVisualisationDefinitionRoutingTierCanFallBackToSqlRatherThanFabricateAHandBackTarget()
        {
            var names = ToolNames("data-analyst.visualisation.definition");

            names.Should().Contain(freeFormSql,
                "the 'how many rules are there, broken down by type' transcript failed because this exact "
                + "routing tier had no SQL fallback and invented a plausible-sounding owning agent instead "
                + "of answering the ad hoc cross-table count itself");
        }

        [Fact]
        public void ARoutingTierKeepsTheCachedResultReadersSoItCanReadWhatItsJoobriksProduced()
        {
            var names = ToolNames(Tier);

            names.Should().Contain("QueryCachedData");
            names.Should().Contain("PreviewDataRows");
        }

        [Fact]
        public void ALeafKeepsFreeFormSqlBecauseAnAggregateAcrossSlicesIsARealNeed()
        {
            var names = ToolNames(Leaf);

            names.Should().Contain(freeFormSql);
        }

        [Fact]
        public void EveryAgentWithoutOperationsOfItsOwnIsTreatedAsARoutingTier()
        {
            var tiers = AgentDefinitions.All
                .Where(pair => !AgentDefinitions.HierarchyToolNames.ContainsKey(pair.Key))
                .Where(pair => pair.Value.AllowedDelegates is { Count: > 0 })
                .Select(pair => pair.Key)
                .ToList();

            tiers.Should().Contain(Tier);
        }

        [Fact]
        public void HandBackIsOfferedOnlyWhereSomeoneIsWaitingOnTheAnswer()
        {
            ToolNames(Leaf, new HandBackSignal()).Should().Contain("HandBack");

            ToolNames(Leaf).Should().NotContain("HandBack",
                "the Worker answers the caller directly and has nobody to hand back to");
        }

        [Fact]
        public void HandBackRecordsWhatIsNeededAndWhoOwnsIt()
        {
            var signal = new HandBackSignal();

            new HandBackTool(signal).HandBack(
                "Which payload key carries the response code on model 2?",
                "The request x-paths hold no mapping of values.",
                "entity-analysis-model-abstraction-rule-inspector");

            signal.Raised.Should().NotBeNull();
            signal.Raised!.OwningAgent.Should().Be("entity-analysis-model-abstraction-rule-inspector");
            signal.Raised.WhatIsNeeded.Should().Contain("response code");
            signal.Raised.WhatIEstablished.Should().Contain("no mapping");
        }

        [Fact]
        public void AnUnnamedOwnerIsCarriedAsUnknownRatherThanAsAnEmptyString()
        {
            var signal = new HandBackSignal();

            new HandBackTool(signal).HandBack("What is needed", "Nothing", "   ");

            signal.Raised!.OwningAgent.Should().BeNull(
                "a blank name is the honest 'I could not say', and must not read as an agent slug");
        }

        [Fact]
        public void AFabricatedOwningAgentNameIsDroppedRatherThanPassedOn()
        {
            var signal = new HandBackSignal();

            var result = new HandBackTool(signal).HandBack(
                "How many rules are there, broken down by type?", "The real rule tables, not how to count them.",
                "entity-analysis-model-plausible-sounding-but-not-real-inspector");

            signal.Raised!.OwningAgent.Should().BeNull(
                "a name that is not on the real roster is no better than no name, and must not reach the "
                + "caller dressed up as a confirmed destination");

            result.Should().Contain(
                "No agent named 'entity-analysis-model-plausible-sounding-but-not-real-inspector' is registered");
        }

        [Fact]
        public void ARealAgentNameStillPassesEvenWhenItIsNotTheRightOneForThisQuestion()
        {
            var signal = new HandBackSignal();

            new HandBackTool(signal).HandBack(
                "How many rules are there, broken down by type?", "The real rule tables.",
                "entity-analysis-model-abstraction-rule-inspector");

            signal.Raised!.OwningAgent.Should().Be("entity-analysis-model-abstraction-rule-inspector",
                "the check is only that the name is real, not that it is the correct owner -- that is "
                + "for whoever receives the hand-back to judge");
        }

        [Fact]
        public void TheFirstHandBackWins()
        {
            var signal = new HandBackSignal();
            var tool = new HandBackTool(signal);

            tool.HandBack("First", "Nothing", "slice-one");
            tool.HandBack("Second", "Nothing", "slice-two");

            signal.Raised!.WhatIsNeeded.Should().Be("First");
        }

        [Fact]
        public void ARoutingTierHoldsTheReadsOfItsDelegatesButNeverTheirWrites()
        {
            var bound = new Dictionary<string, IReadOnlyList<AgentToolDefinition>>
            {
                [Leaf] =
                [
                    new AgentToolDefinition(AIFunctionFactory.Create(ReadStub, "ListWidgets"), []),
                    new AgentToolDefinition(AIFunctionFactory.Create(WriteStub, "CreateWidget"), [],
                        true)
                ]
            };

            var names = ToolNames(Tier, bound: bound);

            names.Should().Contain("ListWidgets",
                "a read-only question a delegate would answer in one call is the tier's to answer");
            names.Should().NotContain("CreateWidget", "shared reads never widen to writes");
        }

        [Fact]
        public void AWorkerIsNotHandedAWriteThroughTheTierEvenWhenItsDelegateHoldsOne()
        {
            var bound = new Dictionary<string, IReadOnlyList<AgentToolDefinition>>
            {
                [Leaf] =
                [
                    new AgentToolDefinition(AIFunctionFactory.Create(WriteStub, "CreateWidget"), [],
                        true)
                ]
            };

            ToolNames(Tier, bound: bound).Should().NotContain("CreateWidget");
        }

        [Fact]
        public void StaticGraphAndFieldLookupsAreAnsweredOncePerTurn()
        {
            var ledger = new RepeatedToolLedger();

            ledger.CanRepeat("FindGraphWorkflow").Should().BeTrue();
            ledger.CanRepeat("GetGraphWorkflow").Should().BeTrue();
            ledger.CanRepeat("EntityAnalysisModelActivationRuleFilterFields").Should().BeTrue();

            ledger.CanRepeat("PostgresActivityList").Should().BeFalse(
                "a live reading is deliberately never answered from an earlier one");
        }

        [Fact]
        public void TheWorkerIsNeverToldItHoldsNoSql()
        {
            foreach (var agentType in new[] { "general", "researcher", "validator" })
            {
                AgentDefinitions.All[agentType].Instructions.Should().NotContain(
                    "no free-form SQL", $"{agentType} holds the dynamic SQL tool and must not be told otherwise");
            }
        }

        [Fact]
        public void EveryLeafIsAHierarchyAgentThatCannotDelegateExceptToTheSharedWebResearchSpecialist()
        {
            foreach (var agentType in AgentDefinitions.HierarchyToolNames.Keys)
            {
                var allowedDelegates = AgentDefinitions.All[agentType].AllowedDelegates ?? [];

                allowedDelegates.Should().BeSubsetOf(["web-researcher"],
                    $"{agentType} holds operations of its own, and a leaf cannot delegate except to bounce " +
                    "an open-internet question to the one specialist that reads external search results "
                    + "directly");
            }
        }

        private static string ReadStub()
        {
            return string.Empty;
        }

        private static string WriteStub()
        {
            return string.Empty;
        }

        private sealed class StubSql : ISqlQueryClient
        {
            public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteAsync(
                string sql, CancellationToken token = default)
            {
                IReadOnlyList<IReadOnlyDictionary<string, object?>> rows = [];

                return Task.FromResult(rows);
            }

            public Task<SqlValidation> ValidateAsync(string sql, CancellationToken token = default)
            {
                return Task.FromResult(new SqlValidation(true, null, null));
            }

            public Task<SqlPlanSummary> ExplainAsync(string sql, CancellationToken token = default)
            {
                return Task.FromResult(new SqlPlanSummary(1, 0, 1, [], [], []));
            }

            public Task<IReadOnlyList<SqlTableStatistic>> TableStatisticsAsync(
                string sql, CancellationToken token = default)
            {
                IReadOnlyList<SqlTableStatistic> statistics = [];

                return Task.FromResult(statistics);
            }

            public Task<SqlQueryHistory> QueryHistoryAsync(string sql, CancellationToken token = default)
            {
                return Task.FromResult(new SqlQueryHistory(false, 0, 0, 0, 0, 0));
            }

            public IReadOnlyList<string> ListQueryableTables()
            {
                return [];
            }

            public Task<IReadOnlyDictionary<string, string>> TableCommentsAsync(IReadOnlyList<string> tables,
                CancellationToken token = default)
            {
                IReadOnlyDictionary<string, string> comments = new Dictionary<string, string>();

                return Task.FromResult(comments);
            }
        }

        private sealed class StubCache : IToolResultCache
        {
            public Task<string> StoreAsync(CachedTable table, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                return Task.FromResult("tbl_stub");
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult<CachedTable?>(null);
            }
        }

        private sealed class StubRag : IRagClient
        {
            public Task<IReadOnlyList<RagChunk>> SearchAsync(
                string query, int topK = 5, double minScore = 0, int candidates = 0, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<RagChunk>>([]);
            }
        }

        private sealed class StubMemory : IMemoryClient
        {
            public Task<MemoryEntry> RememberAsync(string content, string scope, byte? status = null,
                DateTime? expiresAt = null, string kind = "Fact", CancellationToken token = default)
            {
                return Task.FromResult(new MemoryEntry(1, scope, "user", content));
            }

            public Task<IReadOnlyList<MemoryEntry>> RecallAsync(string query, int topK = 5,
                DateTime? since = null, bool includeReflections = false, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<MemoryEntry>>([]);
            }
        }

        private sealed class StubHistory : IConversationHistoryClient
        {
            public Task<IReadOnlyList<HistoryEntry>> SearchAsync(
                string query, string scope, int topK = 5, CancellationToken token = default)
            {
                return Task.FromResult<IReadOnlyList<HistoryEntry>>([]);
            }
        }

        private sealed class StubChat : IChatClient
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