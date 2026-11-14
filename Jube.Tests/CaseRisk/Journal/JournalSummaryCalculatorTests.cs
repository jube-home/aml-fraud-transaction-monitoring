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
    public sealed class JournalSummaryCalculatorTests
    {
        [Fact]
        public void TheStripCountsTheSpanTheDaysAndTheActivations()
        {
            var events = new[]
            {
                Event(1, Start, 10, activations: 2, elevation: 3), Event(2, Start.AddHours(1), 30),
                Event(3, Start.AddDays(3), 20, activations: 1, elevation: 9)
            }.ToList();

            var summary = JournalSummaryCalculator.Compute(events, ["Amount"]);

            summary.Count.Should().Be(3);
            summary.First.Should().Be(Start);
            summary.Last.Should().Be(Start.AddDays(3));
            summary.ActiveDays.Should().Be(2);
            summary.Activations.Should().Be(3);
            summary.ActivationRows.Should().Be(2);
            summary.MaxElevation.Should().Be(9);
        }

        [Fact]
        public void EachMeasureGetsItsTotalsAndTheLatestValueByTime()
        {
            var events = new[] { Event(1, Start, 10), Event(2, Start.AddDays(2), 30), Event(3, Start.AddDays(1), 20) }
                .ToList();

            var field = JournalSummaryCalculator.Compute(events, ["Amount", "Missing"]).Fields.Single();

            field.Sum.Should().Be(60);
            field.Mean.Should().Be(20);
            field.Median.Should().Be(20);
            field.Max.Should().Be(30);
            field.Latest.Should().Be(30);
        }

        [Fact]
        public void NothingGivesAnEmptySummary()
        {
            var summary = JournalSummaryCalculator.Compute([], ["Amount"]);

            summary.Count.Should().Be(0);
            summary.First.Should().BeNull();
            summary.Fields.Should().BeEmpty();
        }
    }
}
