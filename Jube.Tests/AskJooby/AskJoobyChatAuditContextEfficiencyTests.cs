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
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyChatAuditContextEfficiencyTests
    {
        private static readonly DateTime baseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void NoModelCallsProducesAnEmptySection()
        {
            var markdown = AskJoobyChatAuditContextEfficiency.BuildMarkdownSection([], [], [], []);

            markdown.Should().Contain("No model calls or tool calls were recorded");
        }

        [Fact]
        public void TheLargestModelCallByInputTokensIsSurfacedWithItsAgentType()
        {
            var run = new AskJoobyRun { Id = 1, AgentType = "worker", CreatedDate = baseTime };

            var modelCalls = new List<AskJoobyModelCall>
            {
                new() { Id = 1, AskJoobyRunId = 1, InputTokens = 500, OutputTokens = 50, CreatedDate = baseTime },
                new() { Id = 2, AskJoobyRunId = 1, InputTokens = 48_000, OutputTokens = 200, CreatedDate = baseTime }
            };

            var markdown = AskJoobyChatAuditContextEfficiency.BuildMarkdownSection([run], [], modelCalls, []);
            var json = AskJoobyChatAuditContextEfficiency.BuildContextEfficiencyJson([run], [], modelCalls, []);

            markdown.Should().Contain("48000 tokens at the largest");
            markdown.Should().Contain("worker");

            using var document = JsonDocument.Parse(json);

            document.RootElement.GetProperty("maxInputTokens").GetInt32().Should().Be(48_000);

            document.RootElement.GetProperty("largestModelCalls")[0].GetProperty("inputTokens").GetInt32()
                .Should().Be(48_000);
        }

        [Fact]
        public void TheLargestToolResultAcrossAnyToolIsSurfacedNotJustRag()
        {
            var bigResult = new string('x', 20_000);

            var toolCalls = new List<AskJoobyToolCall>
            {
                new() { Id = 1, ToolName = "RunSqlQuery", Result = bigResult, CreatedDate = baseTime },
                new() { Id = 2, ToolName = "SearchRag", Result = "small", CreatedDate = baseTime }
            };

            var markdown = AskJoobyChatAuditContextEfficiency.BuildMarkdownSection([], toolCalls, [], []);

            markdown.Should().Contain("RunSqlQuery");
            markdown.Should().Contain("20000");
        }

        [Fact]
        public void CompactionPressureReportsHowMuchHadAlreadyBuiltUpBeforeTheFirstOne()
        {
            var run = new AskJoobyRun { Id = 1, AgentType = "worker", CreatedDate = baseTime };

            var modelCalls = new List<AskJoobyModelCall>
            {
                new() { Id = 1, AskJoobyRunId = 1, InputTokens = 100, CreatedDate = baseTime }
            };

            var compactions = new List<AskJoobyCompaction>
            {
                new()
                {
                    Id = 1, BeforeCharacters = 40_000, AfterCharacters = 10_000, CreatedDate = baseTime
                }
            };

            var markdown = AskJoobyChatAuditContextEfficiency.BuildMarkdownSection([run], [], modelCalls,
                compactions);

            markdown.Should().Contain("1 compaction(s)");
            markdown.Should().Contain("40000 characters already built up before the first one fired");
        }
    }
}