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

using System.Linq;
using FluentAssertions;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ReplyDraftTests
    {
        [Fact]
        public void NarrationWrittenBeforeAToolCallIsNotPartOfTheAnswer()
        {
            var draft = new ReplyDraft();

            draft.Append("I'll survey the schema for authored rule tables.");
            draft.Observe(new AgentStreamItem(AgentStreamItemKind.Step, "Listing queryable tables..."));
            draft.Append("Query shape works. Now let me pull counts.");
            draft.Observe(new AgentStreamItem(AgentStreamItemKind.Step, "Running an SQL query..."));
            draft.Append("Surveyed the core slice. Here is what I found.");

            draft.ToString().Should().Be("Surveyed the core slice. Here is what I found.",
                "each line before a tool call is a complete assistant message that ended in work, "
                + "not in an answer, and the reader has already watched that work scroll past");
        }

        [Fact]
        public void FilingAMemoryAfterTheAnswerDoesNotDiscardTheAnswer()
        {
            var draft = new ReplyDraft();

            draft.Append("Abstraction 3, Activation 8, Gateway 1, Sanction 1. Thirteen in total.");

            draft.Observe(new AgentStreamItem(
                AgentStreamItemKind.Step, "Slapping that down at the Salt Lick...",
                AnswerPreserving: true));

            draft.Append(" Point me at a different set and I'll recount.");

            draft.ToString().Should().Be(
                "Abstraction 3, Activation 8, Gateway 1, Sanction 1. Thirteen in total. "
                + "Point me at a different set and I'll recount.",
                "recording what was already worked out cannot have superseded it, so filing a "
                + "memory must never be read as the signal that what came before it was narration");
        }

        [Fact]
        public void AnAnswerDiscardedByAToolCallIsRecoveredWhenOnlyASignOffFollowsIt()
        {
            var draft = new ReplyDraft();

            var answer = string.Join(" ", Enumerable.Repeat(
                "Activation rules are counted from EntityAnalysisModelActivationRule.", 12));

            draft.Append(answer);
            draft.Observe(new AgentStreamItem(AgentStreamItemKind.Step, "Calling something..."));
            draft.Append("Point me at a different set and I'll recount.");

            draft.ToString().Should().Be(
                answer + "\n\nPoint me at a different set and I'll recount.");
        }

        [Fact]
        public void AForwardedJoobriksActivityNeverTruncatesTheAnswerBeingWritten()
        {
            var draft = new ReplyDraft();

            draft.Append("Here is the breakdown. ");

            draft.Observe(new AgentStreamItem(
                AgentStreamItemKind.Step, "Running an SQL query...", "Model Admin Activation"));
            draft.Observe(new AgentStreamItem(
                AgentStreamItemKind.Delegation, "reported back.", "Model Admin Core"));

            draft.Append("Seven activation rules.");

            draft.ToString().Should().Be("Here is the breakdown. Seven activation rules.");
        }

        [Fact]
        public void ThinkingAndThoughtLeaveTheAnswerAlone()
        {
            var draft = new ReplyDraft();

            draft.Append("The count is ");
            draft.Observe(new AgentStreamItem(AgentStreamItemKind.Thinking, "weighing the two tables"));
            draft.Observe(new AgentStreamItem(AgentStreamItemKind.Thought, "both are authored config"));
            draft.Append("ten.");

            draft.ToString().Should().Be("The count is ten.");
        }

        [Fact]
        public void AnAnswerWithNoToolCallAtAllSurvivesWhole()
        {
            var draft = new ReplyDraft();

            draft.Append("Jube keeps several unrelated rule families.");

            draft.ToString().Should().Be("Jube keeps several unrelated rule families.");
            draft.Length.Should().BeGreaterThan(0);
        }
    }
}