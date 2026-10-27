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
    public sealed class GapCalculatorTests
    {
        [Fact]
        public void EveryGapLandsInOneBinAndTheLabelsReadInTheUnitThatFits()
        {
            var events = new[]
            {
                Event(1, Start), Event(2, Start.AddMinutes(3)), Event(3, Start.AddMinutes(63)),
                Event(4, Start.AddDays(2)), Event(5, Start.AddDays(40))
            }.ToList();

            var report = GapCalculator.Compute(events);

            report.Bins.Sum(bin => bin.Count).Should().Be(4);
            report.Bins[1].Label.Should().Be("1 min – 5 min");
            report.Bins[0].Label.Should().Be("< 1 min");
            report.Bins[^1].Label.Should().Be("30 d+");
            report.Bins[^1].Count.Should().Be(1);
            report.Bins[^2].Label.Should().Be("7 d – 30 d");
            report.Bins[^2].Count.Should().Be(0);
            report.Bins.Single(bin => bin.Label == "1 d – 3 d").Count.Should().Be(1);
        }

        [Fact]
        public void TheLongestDormancyIsFoundWithWhatHappenedInTheDayAfterIt()
        {
            var events = new[]
            {
                Event(1, Start), Event(2, Start.AddHours(1)), Event(3, Start.AddDays(10)),
                Event(4, Start.AddDays(10).AddHours(2)), Event(5, Start.AddDays(12))
            }.ToList();

            var longest = GapCalculator.Compute(events).Longest!;

            longest.From.Should().Be(Start.AddHours(1));
            longest.To.Should().Be(Start.AddDays(10));
            longest.Hours.Should().BeApproximately(239, 1e-9);
            longest.EventsInNextDay.Should().Be(2);
        }

        [Fact]
        public void AGapExactlyOnAnEdgeBelongsToTheBinThatStartsThere()
        {
            var events = new[]
            {
                Event(1, Start), Event(2, Start.AddMinutes(5)), Event(3, Start.AddMinutes(20))
            }.ToList();

            var bins = GapCalculator.Compute(events).Bins;

            bins.Single(bin => bin.Label == "5 min – 15 min").Count.Should().Be(1);
            bins.Single(bin => bin.Label == "15 min – 1 h").Count.Should().Be(1);
            bins.Single(bin => bin.Label == "1 min – 5 min").Count.Should().Be(0);
        }

        [Fact]
        public void TheMedianGapIsInHoursAndOneEventHasNoGaps()
        {
            var events = new[] { Event(1, Start), Event(2, Start.AddHours(2)), Event(3, Start.AddHours(8)) }.ToList();

            GapCalculator.Compute(events).MedianHours.Should().Be(4);
            GapCalculator.Compute([Event(1, Start)]).Longest.Should().BeNull();
        }
    }
}
