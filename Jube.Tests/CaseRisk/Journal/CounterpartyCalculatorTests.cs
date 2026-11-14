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
    public sealed class CounterpartyCalculatorTests
    {
        private static readonly string[] counterparty = ["Counterparty"];

        [Fact]
        public void TheTopComeFirstWithTheirShareAndTheConcentrationIsTheSumOfSquaredShares()
        {
            var events = new[]
            {
                Event(1, Start, counterparty: "A"), Event(2, Start.AddDays(1), counterparty: "A"),
                Event(3, Start.AddDays(2), counterparty: "A"), Event(4, Start.AddDays(3), counterparty: "B")
            }.ToList();

            var profile = CounterpartyCalculator.Compute(events, counterparty).Single();

            profile.Top.Select(entry => entry.Value).Should().Equal("A", "B");
            profile.Top[0].Share.Should().Be(0.75);
            profile.Concentration.Should().BeApproximately(0.75 * 0.75 + 0.25 * 0.25, 1e-9);
            profile.Distinct.Should().Be(2);
        }

        [Fact]
        public void AnAmountFieldTotalsTheMoneyPerCounterpartyAndAbsentOneLeavesItOut()
        {
            var events = new[]
            {
                Event(1, Start, amount: 10, counterparty: "A"), Event(2, Start.AddDays(1), amount: 15, counterparty: "A"),
                Event(3, Start.AddDays(2), amount: 7, counterparty: "B")
            }.ToList();

            CounterpartyCalculator.Compute(events, counterparty, "Amount").Single().Top[0].Amount.Should().Be(25);
            CounterpartyCalculator.Compute(events, counterparty).Single().Top[0].Amount.Should().BeNull();
        }

        [Fact]
        public void ACounterpartyFirstSeenInTheLastThirtyDaysIsNew()
        {
            var events = new[]
            {
                Event(1, Start, counterparty: "Old"), Event(2, Start.AddDays(60), counterparty: "Old"),
                Event(3, Start.AddDays(70), counterparty: "Fresh")
            }.ToList();

            var profile = CounterpartyCalculator.Compute(events, counterparty).Single();

            profile.NewCount.Should().Be(1);
            profile.Top.Single(entry => entry.Value == "Fresh").New.Should().BeTrue();
            profile.Top.Single(entry => entry.Value == "Old").New.Should().BeFalse();
        }

        [Fact]
        public void OnlyTenAreListedAndAFieldWithNothingInItIsOmitted()
        {
            var events = Enumerable.Range(0, 15).Select(i => Event(i + 1, Start.AddDays(i), counterparty: $"P{i:00}")).ToList();

            CounterpartyCalculator.Compute(events, counterparty).Single().Top.Should().HaveCount(10);
            CounterpartyCalculator.Compute(events, ["Missing"]).Should().BeEmpty();
        }
    }
}
