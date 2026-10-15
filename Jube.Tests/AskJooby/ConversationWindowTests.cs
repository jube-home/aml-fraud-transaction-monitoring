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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyConversation.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class ConversationWindowTests
    {
        private static AskJoobyMessage Message(int id, string content, bool compact = false, int? anchor = null)
        {
            return new AskJoobyMessage
            {
                Id = id,
                Content = content,
                RoleId = compact ? (byte)AskJoobyMessageRole.System : (byte)AskJoobyMessageRole.User,
                IsCompact = compact ? true : null,
                CompactedToAskJoobyMessageId = anchor
            };
        }

        [Fact]
        public void WithNoCompactEverythingIsCarried()
        {
            var messages = new List<AskJoobyMessage> { Message(1, "one"), Message(2, "two") };

            ConversationWindow.Select(messages).Select(m => m.Content).Should().Equal("one", "two");
        }

        [Fact]
        public void ACompactStandsInForWhatItCoveredAndTheRestIsCarried()
        {
            var messages = new List<AskJoobyMessage>
            {
                Message(1, "one"), Message(2, "two"),
                Message(3, "summary", true, 2),
                Message(4, "four")
            };

            ConversationWindow.Select(messages).Select(m => m.Content).Should().Equal("summary", "four");
        }

        [Fact]
        public void AMessageWrittenWhileCompactionWasStillRunningIsNotLost()
        {
            var messages = new List<AskJoobyMessage>
            {
                Message(1, "one"), Message(2, "two"), Message(3, "three"),
                Message(4, "summary of one and two", true, 2)
            };

            ConversationWindow.Select(messages).Select(m => m.Content)
                .Should().Equal(["summary of one and two", "three"],
                    "a positional rule drops exactly the messages that arrived during compaction, which "
                    + "stays invisible because they are still in the transcript");
        }

        [Fact]
        public void OnlyTheNewestCompactIsCarriedBecauseItSubsumesTheOlder()
        {
            var messages = new List<AskJoobyMessage>
            {
                Message(1, "one"),
                Message(2, "first summary", true, 1),
                Message(3, "three"),
                Message(4, "second summary", true, 3),
                Message(5, "five")
            };

            ConversationWindow.Select(messages).Select(m => m.Content)
                .Should().Equal("second summary", "five");
        }

        [Fact]
        public void ACompactWrittenBeforeTheAnchorExistedStillBehavesAsItDid()
        {
            var messages = new List<AskJoobyMessage>
            {
                Message(1, "one"),
                Message(2, "summary", true),
                Message(3, "three")
            };

            ConversationWindow.Select(messages).Select(m => m.Content).Should().Equal(["summary", "three"],
                "rows written before the column existed covered everything up to themselves, which is "
                + "what the positional rule assumed and remains true for them");
        }
    }
}