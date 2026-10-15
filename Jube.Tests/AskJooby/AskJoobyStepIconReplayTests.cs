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

using FluentAssertions;
using Jube.AskJooby.Agents.Models;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class AskJoobyStepIconReplayTests
    {
        [Fact]
        public void AStoredStepReplaysWithTheIconItWasStreamedWith()
        {
            var step = new AskJoobyMessage
            {
                Id = 7,
                EntryTypeId = AskJoobyMessageRepository.EntryTypeStep,
                Content = "Enjoying the Salt Lick...",
                IconId = (int)AgentToolIcon.Database
            };

            var entry = AskJoobyConversationMapper.ToEntryDto(step);

            entry.Kind.Should().Be("Step");
            entry.IconId.Should().Be((int)AgentToolIcon.Database);
        }

        [Fact]
        public void AStepRecordedBeforeIconsExistedReplaysWithNoIcon()
        {
            var step = new AskJoobyMessage
            {
                Id = 8,
                EntryTypeId = AskJoobyMessageRepository.EntryTypeStep,
                Content = "Listing user registry...",
                IconId = null
            };

            var entry = AskJoobyConversationMapper.ToEntryDto(step);

            entry.Kind.Should().Be("Step");
            entry.IconId.Should().BeNull();
        }

        [Fact]
        public void AStoredStepReplaysWithTheDetailItWasStreamedWith()
        {
            var step = new AskJoobyMessage
            {
                Id = 9,
                EntryTypeId = AskJoobyMessageRepository.EntryTypeStep,
                Content = "Querying the registry...",
                Detail = "Arguments: {\"query\":\"select 1\"}\n\nResult: 1 row"
            };

            var entry = AskJoobyConversationMapper.ToEntryDto(step);

            entry.Kind.Should().Be("Step");
            entry.Detail.Should().Be("Arguments: {\"query\":\"select 1\"}\n\nResult: 1 row");
        }

        [Fact]
        public void AStepRecordedBeforeDetailExistedReplaysWithNoDetail()
        {
            var step = new AskJoobyMessage
            {
                Id = 10,
                EntryTypeId = AskJoobyMessageRepository.EntryTypeStep,
                Content = "Listing user registry...",
                Detail = null
            };

            var entry = AskJoobyConversationMapper.ToEntryDto(step);

            entry.Kind.Should().Be("Step");
            entry.Detail.Should().BeNull();
        }
    }
}