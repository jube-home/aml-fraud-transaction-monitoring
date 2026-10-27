/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Xunit;
using static Jube.Test.CaseRisk.Journal.JournalEvents;

namespace Jube.Test.CaseRisk.Journal
{
    [Trait("Category", "Unit")]
    public sealed class NoveltyCalculatorTests
    {
        private static readonly string[] counterparty = ["Counterparty"];

        [Fact]
        public void AFirstSeenValueAfterTheWarmUpIsFlaggedOnItsRowAndNothingBeforeIt()
        {
            var events = Enumerable.Range(0, 10).Select(i => Event(i + 1, Start.AddDays(i), counterparty: "A")).ToList();
            events.Add(Event(11, Start.AddDays(10), counterparty: "B"));
            events.Add(Event(12, Start.AddDays(11), counterparty: "B"));

            var report = NoveltyCalculator.Compute(events, counterparty);

            report.Rows.Keys.Should().Equal(11);
            report.Rows[11].Should().Equal("Counterparty");
            report.Fields.Single().Distinct.Should().Be(2);
        }

        [Fact]
        public void TheFirstFewTransactionsAreNeverNovelBecauseThereIsNoHistoryYet()
        {
            var events = Enumerable.Range(0, 4).Select(i => Event(i + 1, Start.AddDays(i), counterparty: $"P{i}")).ToList();

            NoveltyCalculator.Compute(events, counterparty).Rows.Should().BeEmpty();
        }

        [Fact]
        public void OnlyValuesFirstSeenInTheLastWeekAreListedAsRecentNewestFirst()
        {
            var events = Enumerable.Range(0, 10).Select(i => Event(i + 1, Start.AddDays(i), counterparty: "A")).ToList();
            events.Add(Event(11, Start.AddDays(11), counterparty: "Old"));
            events.Add(Event(12, Start.AddDays(40), counterparty: "Mid"));
            events.Add(Event(13, Start.AddDays(44), counterparty: "New"));

            var recent = NoveltyCalculator.Compute(events, counterparty).Fields.Single().Recent;

            recent.Select(item => item.Value).Should().Equal("New", "Mid");
        }

        [Fact]
        public void BlankAndMissingValuesAreIgnored()
        {
            var events = Enumerable.Range(0, 8).Select(i => Event(i + 1, Start.AddDays(i), counterparty: i % 2 == 0 ? "" : null))
                .ToList();

            var report = NoveltyCalculator.Compute(events, counterparty);

            report.Rows.Should().BeEmpty();
            report.Fields.Single().Distinct.Should().Be(0);
        }
    }
}
