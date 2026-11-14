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
using FluentAssertions;
using Jube.Data.Context;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyChatAuditCoverageTests
    {
        private static readonly HashSet<string> renderedInTheAudit = new(StringComparer.Ordinal)
        {
            "AskJoobyAgentGuardRejection",
            "AskJoobyCompaction",
            "AskJoobyConversation",
            "AskJoobyDataCache",
            "AskJoobyFile",
            "AskJoobyMemory",
            "AskJoobyMessage",
            "AskJoobyModelCall",
            "AskJoobyOpenCorporatesCall",
            "AskJoobyRouting",
            "AskJoobyRun",
            "AskJoobyToolCall",
            "AskJoobyWebSearchCall",
            "AskJoobyWorkflowStep"
        };

        private static readonly Dictionary<string, string> deliberatelyExcluded = new(StringComparer.Ordinal)
        {
            ["AskJoobyRagChunk"] =
                "the Pasture corpus is tenant-wide reference material, not a record of this conversation -- " +
                "which chunks a turn actually read is already in its SearchRag tool calls",
            ["AskJoobyAgentGuardRule"] =
                "guard rules are backend-maintained configuration deciding which guard checks run and with what " +
                "guidance, not something a conversation said or did -- the guard OUTCOMES a turn produced are " +
                "already on the turn's own records",
            ["AskJoobyErrata"] =
                "errata are backend-written standing guidance rendered into each agent's prompt header, not " +
                "something said in a conversation -- nobody typed them and no turn created them, so they are " +
                "configuration rather than audit record",
            ["AskJoobyEmbeddingCall"] =
                "per-call embedding usage feeding the EnableAskJoobyCostBanner total -- a billing record, not " +
                "something said in the conversation",
            ["AskJoobyRerankCall"] =
                "per-call reranker usage feeding the EnableAskJoobyCostBanner total -- a billing record, not " +
                "something said in the conversation",
            ["AskJoobyTurnCost"] =
                "the persisted per-turn cost summary EnableAskJoobyCostBanner writes once a turn completes -- " +
                "a derived billing figure, not something said in the conversation",
            ["AskJoobyEndpointFee"] =
                "a model/endpoint token-rate table for EnableAskJoobyCostBanner's cost calculation -- " +
                "platform-wide pricing configuration, not scoped to any one conversation at all",
            ["AskJoobyAnalyticsInstance"] =
                "named analytics configuration allocated to a model and run by the engine -- platform state that " +
                "Ask Jooby manages through tools, not a record of any conversation",
            ["AskJoobyAnalyticsTrainedModel"] =
                "a trained model attached to an analytics instance -- platform state, not a record of any conversation",
            ["AskJoobyAnalyticsTrainedModelVersion"] =
                "one fitted version of a trained model -- platform state, not a record of any conversation",
            ["AskJoobyRerankFee"] =
                "a model/endpoint per-document rerank-rate table for EnableAskJoobyCostBanner's cost " +
                "calculation -- platform-wide pricing configuration, not scoped to any one conversation at all"
        };

        private static IReadOnlyList<string> ContextTables()
        {
            return
            [
                .. typeof(DbContext).GetProperties()
                    .Where(property => property.Name.StartsWith("AskJooby", StringComparison.Ordinal))
                    .Where(property => property.PropertyType.IsGenericType
                                       && property.PropertyType.GetGenericTypeDefinition() == typeof(ITable<>))
                    .Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
            ];
        }

        [Fact]
        public void EveryAskJoobyRelationIsEitherRenderedOrDeliberatelyExcluded()
        {
            var unaccountedFor = ContextTables()
                .Where(table => !renderedInTheAudit.Contains(table))
                .Where(table => !deliberatelyExcluded.ContainsKey(table))
                .ToList();

            unaccountedFor.Should().BeEmpty(
                "a conversation table that is neither in the transcript nor explicitly ruled out is a " +
                "hole in the audit nobody decided to leave");
        }

        [Fact]
        public void NothingIsClaimedAsRenderedThatTheContextDoesNotHave()
        {
            var contextTables = ContextTables().ToHashSet(StringComparer.Ordinal);

            renderedInTheAudit.Concat(deliberatelyExcluded.Keys)
                .Should().OnlyContain(table => contextTables.Contains(table),
                    "a stale name here would quietly weaken the check that guards the rest");
        }

        [Fact]
        public void EveryExclusionCarriesAReason()
        {
            deliberatelyExcluded.Values.Should()
                .OnlyContain(reason => !string.IsNullOrWhiteSpace(reason),
                    "an exclusion without a reason is indistinguishable from an oversight");
        }
    }
}