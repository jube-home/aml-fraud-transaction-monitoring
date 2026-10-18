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
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class RepeatedToolLedgerTests
    {
        [Fact]
        public void TheSameReadWithTheSameArgumentsIsAnsweredFromTheFirstOne()
        {
            var ledger = new RepeatedToolLedger();
            ledger.Remember("ListQueryableTables", "{\"nameContains\":\"rule\"}", "12 tables");

            ledger.TryRecall("ListQueryableTables", "{\"nameContains\":\"rule\"}", out var previous)
                .Should().BeTrue();

            previous.Should().Be("12 tables");
            ledger.Served.Should().Be(1);
        }

        [Fact]
        public void DifferentArgumentsAreADifferentQuestionAndRunAgain()
        {
            var ledger = new RepeatedToolLedger();
            ledger.Remember("ListQueryableTables", "{\"nameContains\":\"rule\"}", "12 tables");

            ledger.TryRecall("ListQueryableTables", "{\"nameContains\":\"inline\"}", out _)
                .Should().BeFalse("asking about a different fragment is a different question, and "
                                  + "handing back the first answer would be answering the wrong one");
        }

        [Fact]
        public void ALiveReadingIsNeverRepeatedBecauseAskingTwiceMayBeThePoint()
        {
            var ledger = new RepeatedToolLedger();

            ledger.CanRepeat("PostgresActivityList").Should().BeFalse(
                "an agent reading live activity twice in a turn may be watching for movement, and "
                + "giving it the earlier reading would answer a question it did not ask");

            ledger.CanRepeat("RedisMetricList").Should().BeFalse();
        }

        [Fact]
        public void AWriteIsNeverRepeatedHoweverIdenticalTheArgumentsAre()
        {
            var ledger = new RepeatedToolLedger();

            ledger.CanRepeat("Remember").Should().BeFalse();
            ledger.CanRepeat("ExportCachedTableToCsv").Should().BeFalse();
            ledger.CanRepeat("RenderChart").Should().BeFalse();
            ledger.CanRepeat("PreservationSnapshot").Should().BeFalse();
        }

        [Fact]
        public void RememberingAnUnrepeatableToolIsIgnoredRatherThanStored()
        {
            var ledger = new RepeatedToolLedger();
            ledger.Remember("PostgresActivityList", "{}", "a reading");

            ledger.TryRecall("PostgresActivityList", "{}", out _).Should().BeFalse();
            ledger.Served.Should().Be(0);
        }

        [Fact]
        public void TheSearchesThatWentNowhereAreTheOnesWorthNotRepeating()
        {
            var ledger = new RepeatedToolLedger();
            ledger.Remember("SearchRag", "{\"query\":\"Jube rules overview\"}", "No matching Bites found.");

            ledger.TryRecall("SearchRag", "{\"query\":\"Jube rules overview\"}", out var previous)
                .Should().BeTrue("an identical query against an unchanged index returns the identical "
                                 + "nothing, and paying for it twice buys the same nothing");

            previous.Should().Be("No matching Bites found.");
        }

        [Fact]
        public void ARepeatableToolIsAnnouncedOnceAcrossTheWholeTurnHoweverManyAgentsAskForIt()
        {
            var ledger = new RepeatedToolLedger();

            ledger.ClaimNarration("ListQueryableTables", "{}").Should().BeTrue();

            ledger.ClaimNarration("ListQueryableTables", "{}").Should().BeFalse(
                "five agents each deciding they need the table list is one piece of news, not five");

            ledger.ClaimNarration("ListQueryableTables", "{}").Should().BeFalse();
        }

        [Fact]
        public void TheSameToolAskedADifferentQuestionIsStillAnnounced()
        {
            var ledger = new RepeatedToolLedger();

            ledger.ClaimNarration("ListQueryableTables", "{\"nameContains\":\"case\"}").Should().BeTrue();

            ledger.ClaimNarration("ListQueryableTables", "{\"nameContains\":\"exhaustive\"}").Should()
                .BeTrue("suppressing a different question would hide real work");
        }

        [Fact]
        public void AToolThatIsNotRepeatableIsAlwaysAnnounced()
        {
            var ledger = new RepeatedToolLedger();

            ledger.ClaimNarration("PostgresActivityList", "{}").Should().BeTrue();

            ledger.ClaimNarration("PostgresActivityList", "{}").Should().BeTrue(
                "a live reading asked twice is two different questions");
        }
    }
}