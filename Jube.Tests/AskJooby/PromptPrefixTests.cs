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
using Jube.AskJooby.Observability;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class PromptPrefixTests
    {
        [Fact]
        public void TheSameSystemPromptHashesTheSameHoweverTheRestOfTheTurnDiffers()
        {
            var first = PromptPrefix.Hash([
                new ChatMessage(ChatRole.System, "You own the model admin slice."),
                new ChatMessage(ChatRole.User, "How many rules do I have?")
            ]);

            var second = PromptPrefix.Hash([
                new ChatMessage(ChatRole.System, "You own the model admin slice."),
                new ChatMessage(ChatRole.User, "Something else entirely, much longer."),
                new ChatMessage(ChatRole.Assistant, "And a reply.")
            ]);

            first.Should().Be(second,
                "a prompt cache matches from token zero, so what the hash has to track is the "
                + "prefix alone -- it would be useless if it moved every time the conversation did");
        }

        [Fact]
        public void OneCharacterAtTheStartChangesTheHashBecauseItAlsoBreaksTheCache()
        {
            var clean = PromptPrefix.Hash([new ChatMessage(ChatRole.System, "You own the slice.")]);
            var injected = PromptPrefix.Hash([new ChatMessage(ChatRole.System, " You own the slice.")]);

            injected.Should().NotBe(clean,
                "DigitalOcean match byte for byte from the first character, so a single injected "
                + "space is a total miss and the hash has to be able to show that");
        }

        [Fact]
        public void TheSystemMessageIsTakenEvenWhenItIsNotFirstInTheList()
        {
            var hash = PromptPrefix.Hash([
                new ChatMessage(ChatRole.User, "asked first"),
                new ChatMessage(ChatRole.System, "the instructions")
            ]);

            hash.Should().Be(PromptPrefix.Hash([new ChatMessage(ChatRole.System, "the instructions")]));
        }

        [Fact]
        public void WithNoSystemMessageTheFirstMessageIsHashedInstead()
        {
            (PromptPrefix.Hash([new ChatMessage(ChatRole.User, "only this")]) is null)
                .Should().BeFalse("a call with no system message still has a prefix worth tracking");
        }

        [Fact]
        public void NothingToHashIsReportedAsNothingRatherThanThrowing()
        {
            (PromptPrefix.Hash(null) is null).Should().BeTrue();
            (PromptPrefix.Hash([]) is null).Should().BeTrue();
            (PromptPrefix.Hash([new ChatMessage(ChatRole.System, "")]) is null).Should().BeTrue();
        }
    }
}