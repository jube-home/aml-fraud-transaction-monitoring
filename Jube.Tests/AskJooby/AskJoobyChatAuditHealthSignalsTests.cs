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
using System.Text.Json;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyChatAuditHealthSignalsTests
    {
        private static readonly DateTime baseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void AQuietConversationWithNothingUnusualFlagsNothing()
        {
            var run = new AskJoobyRun
            {
                Id = 1, AgentType = "worker", StatusId = (byte)AskJoobyRunStatus.Completed, CreatedDate = baseTime,
                Result = "done"
            };

            var markdown = AskJoobyChatAuditHealthSignals.BuildMarkdownSection(
                [run], [], [], [], []);

            markdown.Should().Contain("no anomalies flagged");
        }

        [Fact]
        public void ThreeIdenticalToolCallsByTheSameRunAreFlaggedAsARetryLoop()
        {
            var run = new AskJoobyRun { Id = 1, AgentType = "worker", CreatedDate = baseTime };

            var toolCalls = new List<AskJoobyToolCall>();

            for (var i = 0; i < 3; i++)
            {
                toolCalls.Add(new AskJoobyToolCall
                {
                    Id = i + 1, AskJoobyRunId = 1, ToolName = "search_rag", Arguments = "{\"q\":\"same\"}",
                    CreatedDate = baseTime
                });
            }

            var markdown = AskJoobyChatAuditHealthSignals.BuildMarkdownSection(
                [run], toolCalls, [], [], []);

            markdown.Should().Contain("Tool-call retry loops**: 1 flagged");
            markdown.Should().Contain("Run #1 called **search_rag** 3 times with identical arguments");
        }

        [Fact]
        public void ARunThatBouncesBackToItsGrandparentsAgentTypeIsFlaggedAsAHandoffBounce()
        {
            var grandparent = new AskJoobyRun { Id = 1, AgentType = "worker", CreatedDate = baseTime };

            var parent = new AskJoobyRun
            {
                Id = 2, AgentType = "validator", ParentAskJoobyRunId = 1, CreatedDate = baseTime
            };
            var bouncedBack = new AskJoobyRun
            {
                Id = 3, AgentType = "worker", ParentAskJoobyRunId = 2, CreatedDate = baseTime
            };

            var markdown = AskJoobyChatAuditHealthSignals.BuildMarkdownSection(
                [grandparent, parent, bouncedBack], [], [], [], []);

            markdown.Should().Contain("Handoff bounces**: 1 flagged");
            markdown.Should().Contain("Run #3 (worker) bounced back to the same agent type as grandparent run #1");
        }

        [Fact]
        public void ARunWithManyModelCallsAndNoResultIsFlaggedAsEffortWithoutOutput()
        {
            var run = new AskJoobyRun { Id = 1, AgentType = "worker", CreatedDate = baseTime, Result = null };

            var modelCalls = new List<AskJoobyModelCall>();

            for (var i = 0; i < 3; i++)
            {
                modelCalls.Add(new AskJoobyModelCall { Id = i + 1, AskJoobyRunId = 1, CreatedDate = baseTime });
            }

            var markdown = AskJoobyChatAuditHealthSignals.BuildMarkdownSection(
                [run], [], modelCalls, [], []);

            markdown.Should().Contain("Effort without output**: 1 flagged");
            markdown.Should().Contain("Run #1 (worker) made 3 model call(s) but produced no Result");
        }

        [Fact]
        public void TheHealthJsonCarriesTheSameFlagCountAsTheMarkdownOverallLine()
        {
            var run = new AskJoobyRun { Id = 1, AgentType = "worker", CreatedDate = baseTime };

            var toolCalls = new List<AskJoobyToolCall>();

            for (var i = 0; i < 3; i++)
            {
                toolCalls.Add(new AskJoobyToolCall
                {
                    Id = i + 1, AskJoobyRunId = 1, ToolName = "search_rag", Arguments = "{\"q\":\"same\"}",
                    CreatedDate = baseTime
                });
            }

            var json = AskJoobyChatAuditHealthSignals.BuildHealthJson([run], toolCalls, [], [], []);

            using var document = JsonDocument.Parse(json);

            document.RootElement.GetProperty("overallFlagCount").GetInt32().Should().Be(1);
            document.RootElement.GetProperty("toolCallRetryLoops").GetArrayLength().Should().Be(1);
        }
    }
}