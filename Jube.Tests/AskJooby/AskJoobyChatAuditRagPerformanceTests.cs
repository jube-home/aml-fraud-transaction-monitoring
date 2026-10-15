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
    public sealed class AskJoobyChatAuditRagPerformanceTests
    {
        private static readonly DateTime baseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void NoSearchRagCallsProducesAnEmptySection()
        {
            var markdown = AskJoobyChatAuditRagPerformance.BuildMarkdownSection([]);

            markdown.Should().Contain("No SearchRag calls were made");
        }

        [Fact]
        public void SummarisesHitCountResponseSizeAndDuration()
        {
            var toolCalls = new List<AskJoobyToolCall>
            {
                new()
                {
                    Id = 1, AskJoobyRunId = 5, ToolName = "SearchRag", Arguments = "{\"query\":\"token refresh\"}",
                    Result = "Found 3 Bites.\n\n[CRITICAL CONTEXT START]\n" + new string('x', 9500) +
                             "\n[CRITICAL CONTEXT END]\n",
                    DurationMs = 250, CreatedDate = baseTime
                },
                new()
                {
                    Id = 2, AskJoobyRunId = 5, ToolName = "SearchRag", Arguments = "{\"query\":\"nothing here\"}",
                    Result = "No matching Bites found.", DurationMs = 50, CreatedDate = baseTime
                }
            };

            var markdown = AskJoobyChatAuditRagPerformance.BuildMarkdownSection(toolCalls);
            var json = AskJoobyChatAuditRagPerformance.BuildRagPerformanceJson(toolCalls);

            markdown.Should().Contain("Searches**: 2 total, 1 returned nothing");
            markdown.Should().Contain("token refresh");

            using var document = JsonDocument.Parse(json);

            document.RootElement.GetProperty("searchCount").GetInt32().Should().Be(2);
            document.RootElement.GetProperty("emptyCount").GetInt32().Should().Be(1);
            document.RootElement.GetProperty("maxResponseCharacters").GetInt32().Should().BeGreaterThan(9500);

            var largest = document.RootElement.GetProperty("largestSearches");
            largest[0].GetProperty("query").GetString().Should().Be("token refresh");
            largest[0].GetProperty("hitCount").GetInt32().Should().Be(3);
        }

        [Fact]
        public void NonSearchRagToolCallsAreIgnored()
        {
            var toolCalls = new List<AskJoobyToolCall>
            {
                new() { Id = 1, ToolName = "RunSqlQuery", Result = "ok", CreatedDate = baseTime }
            };

            var markdown = AskJoobyChatAuditRagPerformance.BuildMarkdownSection(toolCalls);

            markdown.Should().Contain("No SearchRag calls were made");
        }
    }
}