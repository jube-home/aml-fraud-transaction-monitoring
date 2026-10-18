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
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Data.Repository.Models.AskJoobyDataCache;
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyChatAuditMarkdownBuilderGuidanceNudgeTests
    {
        private static readonly DateTime baseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private static string BuildMarkdown(IReadOnlyList<AskJoobyMessage> entries, IReadOnlyList<AskJoobyRun> runs)
        {
            var conversation = new AskJoobyConversation
            {
                Id = 1, Title = "Test conversation", CreatedDate = baseTime, CreatedUser = "tester"
            };

            return AskJoobyChatAuditMarkdownBuilder.Build(
                conversation,
                entries,
                runs,
                toolCalls: [],
                modelCalls: [],
                memories: [],
                dataCacheEntries: [],
                sqlLog: [],
                files: [],
                compactions: [],
                routings: [],
                workflowSteps: [],
                agentGuardRejections: [],
                webSearchCalls: [],
                openCorporatesCalls: [],
                generatedAtUtc: baseTime.AddMinutes(10),
                generatedByUser: "tester");
        }

        private static AskJoobyMessage Nudge(int id, DateTime createdDate, string content)
        {
            return new AskJoobyMessage
            {
                Id = id, AskJoobyConversationId = 1, RoleId = (byte)AskJoobyMessageRole.User, Content = content,
                CreatedDate = createdDate, EntryTypeId = AskJoobyMessageRepository.EntryTypeMessage, IsNudge = true
            };
        }

        private static AskJoobyMessage WasToldStep(int id, DateTime createdDate, string label, string drainedText)
        {
            return new AskJoobyMessage
            {
                Id = id, AskJoobyConversationId = 1, RoleId = null, Content = $"{label} was told: {drainedText}",
                CreatedDate = createdDate, EntryTypeId = AskJoobyMessageRepository.EntryTypeStep
            };
        }

        private static AskJoobyMessage Step(int id, DateTime createdDate, string content)
        {
            return new AskJoobyMessage
            {
                Id = id, AskJoobyConversationId = 1, RoleId = null, Content = content, CreatedDate = createdDate,
                EntryTypeId = AskJoobyMessageRepository.EntryTypeStep
            };
        }

        private static AskJoobyRun Run(int id, DateTime createdDate, string agentType)
        {
            return new AskJoobyRun
            {
                Id = id, AskJoobyConversationId = 1, ParentAskJoobyRunId = null, AgentType = agentType, Depth = 0,
                StatusId = (byte)AskJoobyRunStatus.Completed, CreatedDate = createdDate, CreatedUser = "tester"
            };
        }

        [Fact]
        public void ANudgeThatWasPickedUpShowsWhenAndTheAgentsImmediateReaction()
        {
            var run = Run(10, baseTime, "worker");

            var entries = new List<AskJoobyMessage>
            {
                Nudge(2, baseTime.AddSeconds(5), "please also check compliance"),
                WasToldStep(3, baseTime.AddSeconds(8), "Jadek", "please also check compliance"),
                Step(4, baseTime.AddSeconds(9), "[Jadek] now checking compliance given the new instruction")
            };

            var markdown = BuildMarkdown(entries, [run]);

            markdown.Should().Contain("## Guidance Nudges");
            markdown.Should().Contain("please also check compliance");
            markdown.Should().Contain("Run #10 -- worker");
            markdown.Should().Contain("**Picked up**: 2026-01-01 12:00:08 UTC");
            markdown.Should().Contain(
                "**Agent's next move afterwards**: [Step] [Jadek] now checking compliance given the new instruction");
            markdown.Should().NotContain("never -- the turn finished");
        }

        [Fact]
        public void ANudgeThatWasNeverDrainedSaysSoRatherThanHidingIt()
        {
            var run = Run(10, baseTime, "worker");

            var entries = new List<AskJoobyMessage>
            {
                Nudge(2, baseTime.AddSeconds(5), "are you still there")
            };

            var markdown = BuildMarkdown(entries, [run]);

            markdown.Should().Contain("## Guidance Nudges");
            markdown.Should().Contain("are you still there");
            markdown.Should().Contain("_never -- the turn finished before its next model call_");
        }

        [Fact]
        public void SeveralNudgesDrainedByOneStepShareOnePickupAndAreNotAssumedOneToOne()
        {
            var run = Run(10, baseTime, "worker");

            const string firstNudgeText = "also check the UK entity";
            const string secondNudgeText = "and flag it if sanctioned";
            var drainedText = "- " + firstNudgeText + "\n- " + secondNudgeText;

            var entries = new List<AskJoobyMessage>
            {
                Nudge(2, baseTime.AddSeconds(5), firstNudgeText),
                Nudge(3, baseTime.AddSeconds(6), secondNudgeText),
                WasToldStep(4, baseTime.AddSeconds(9), "Jadek", drainedText),
                Step(5, baseTime.AddSeconds(10), "[Jadek] widening the search to the UK entity too")
            };

            var markdown = BuildMarkdown(entries, [run]);

            markdown.Should().Contain(firstNudgeText);
            markdown.Should().Contain(secondNudgeText);
            markdown.Should().Contain("2026-01-01 12:00:09 UTC (drained together with 1 other nudge(s))");

            var firstPickupCount = CountOccurrences(markdown, "**Picked up**: 2026-01-01 12:00:09 UTC");
            firstPickupCount.Should().Be(2, "both nudges were drained by the same Step and must both say so");
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            var count = 0;
            var index = 0;

            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }
    }
}