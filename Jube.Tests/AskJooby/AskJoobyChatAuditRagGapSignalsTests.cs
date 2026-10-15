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
using Jube.Data.Repository;
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyChatAuditRagGapSignalsTests
    {
        private static readonly DateTime baseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void AConversationWithNoGapsFlagsNothing()
        {
            var markdown = AskJoobyChatAuditRagGapSignals.BuildMarkdownSection([], []);

            markdown.Should().Contain("no coverage gaps flagged");
        }

        [Fact]
        public void AnEmptySearchRagResultIsFlaggedWithItsQuery()
        {
            var toolCall = new AskJoobyToolCall
            {
                Id = 1, AskJoobyRunId = 5, ToolName = "SearchRag", Arguments = "{\"query\":\"token refresh\"}",
                Result = "No matching Bites found.", CreatedDate = baseTime
            };

            var markdown = AskJoobyChatAuditRagGapSignals.BuildMarkdownSection([], [toolCall]);

            markdown.Should().Contain("Empty Pasture searches**: 1 flagged");
            markdown.Should().Contain("Run #5 searched for \"token refresh\" and found nothing");
        }

        [Fact]
        public void AValidationOmissionEntryIsFlaggedAsAPastureCoverageGap()
        {
            var entry = new AskJoobyMessage
            {
                Id = 1, EntryTypeId = AskJoobyMessageRepository.EntryTypeValidation,
                Content = "Jadek found a gap the docs don't cover, noting it...", CreatedDate = baseTime
            };

            var markdown = AskJoobyChatAuditRagGapSignals.BuildMarkdownSection([entry], []);

            markdown.Should().Contain("Pasture coverage gaps (validation omissions)**: 1 flagged");
        }

        [Fact]
        public void AValidationThatNeverPickedUpIsFlaggedSeparatelyFromAnOmission()
        {
            var entry = new AskJoobyMessage
            {
                Id = 1, EntryTypeId = AskJoobyMessageRepository.EntryTypeValidation,
                Content = "Jadek never picked up this time -- going with the first draft. (timeout)",
                CreatedDate = baseTime
            };

            var markdown = AskJoobyChatAuditRagGapSignals.BuildMarkdownSection([entry], []);

            markdown.Should().Contain("Validation never completed**: 1 flagged");
            markdown.Should().NotContain("Pasture coverage gaps (validation omissions)**: 1 flagged");
        }

        [Fact]
        public void SqlFallbackIsCountedButNeverCountedAsAFlaggedGap()
        {
            var toolCalls = new List<AskJoobyToolCall>
            {
                new() { Id = 1, ToolName = "RunSqlQuery", Arguments = "{}", Result = "ok", CreatedDate = baseTime }
            };

            var markdown = AskJoobyChatAuditRagGapSignals.BuildMarkdownSection([], toolCalls);
            var json = AskJoobyChatAuditRagGapSignals.BuildRagGapJson([], toolCalls);

            markdown.Should().Contain("no coverage gaps flagged");
            markdown.Should().Contain("1 RunSqlQuery call(s)");

            using var document = JsonDocument.Parse(json);

            document.RootElement.GetProperty("overallGapCount").GetInt32().Should().Be(0);
            document.RootElement.GetProperty("sqlFallbackCount").GetInt32().Should().Be(1);
        }
    }
}