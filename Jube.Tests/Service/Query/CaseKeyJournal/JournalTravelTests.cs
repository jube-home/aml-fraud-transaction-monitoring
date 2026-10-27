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

using System;
using System.Linq;
using FluentAssertions;
using Jube.Service.Query.CaseKeyJournal;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class JournalTravelTests
    {
        private static readonly DateTimeOffset start = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

        private static JournalTravel.Point At(double hours, double latitude, double longitude, string lane = "A")
        {
            return new JournalTravel.Point(Guid.NewGuid(), start.AddHours(hours), latitude, longitude, lane);
        }

        [Fact]
        public void TheDistanceBetweenTwoCitiesIsRight()
        {
            JournalTravel.Kilometres(51.5074, -0.1278, 48.8566, 2.3522).Should().BeApproximately(343.5, 2);
            JournalTravel.Kilometres(51.5074, -0.1278, 51.5074, -0.1278).Should().Be(0);
        }

        [Theory]
        [InlineData(51.5, -0.1, true)]
        [InlineData(0, 0, false)]
        [InlineData(91, 0, false)]
        [InlineData(0, 181, false)]
        [InlineData(double.NaN, 1, false)]
        public void OnlyARealPositionIsLocated(double latitude, double longitude, bool expected)
        {
            JournalTravel.Located(latitude, longitude).Should().Be(expected);
        }

        [Fact]
        public void NoLocatedTransactionsMeansNoMovement()
        {
            var none = JournalTravel.Summarise([]);
            var one = JournalTravel.Summarise([At(0, 51.5, -0.1)]);

            none.Legs.Should().Be(0);
            one.Legs.Should().Be(0);
            one.LimitKmh.Should().Be(900);
            one.MinimumKilometres.Should().Be(50);
        }

        [Fact]
        public void ALegIsImpossibleOnlyWhenItIsFarEnoughAndTooFast()
        {
            var summary = JournalTravel.Summarise(
            [
                At(0, 51.5074, -0.1278),
                At(1, 48.8566, 2.3522),
                At(1.1, 48.9, 2.4),
                At(1.2, 40.7128, -74.006)
            ]);

            summary.Legs.Should().Be(3);
            summary.Impossible.Should().Be(1);
            summary.Worst.Single().Kilometres.Should().BeInRange(5800, 5900);
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(1, 0, true)]
        [InlineData(10000, 20000, true)]
        [InlineData(0.5, null, false)]
        [InlineData(10001, null, false)]
        [InlineData(null, -1, false)]
        [InlineData(null, 20001, false)]
        [InlineData(double.NaN, null, false)]
        [InlineData(null, double.PositiveInfinity, false)]
        public void OnlySensibleLimitsAreAccepted(double? limit, double? minimum, bool expected)
        {
            JournalTravel.Valid(limit, minimum).Should().Be(expected);
        }

        [Fact]
        public void TheLimitsDecideWhichLegsAreImpossible()
        {
            var points = new[] { At(0, 51.5074, -0.1278), At(1, 48.8566, 2.3522) };

            JournalTravel.Summarise(points).Impossible.Should().Be(0);
            JournalTravel.Summarise(points, 300).Impossible.Should().Be(1);
            JournalTravel.Summarise(points, 300, 400).Impossible.Should().Be(0);

            var summary = JournalTravel.Summarise(points, 250, 100);

            summary.LimitKmh.Should().Be(250);
            summary.MinimumKilometres.Should().Be(100);
            summary.Impossible.Should().Be(1);
        }

        [Fact]
        public void TheSameInstantAtTwoPlacesIsImpossibleWithNoSpeedToName()
        {
            var summary = JournalTravel.Summarise([At(0, 51.5074, -0.1278), At(0, 48.8566, 2.3522)]);

            summary.Impossible.Should().Be(1);
            summary.Worst.Single().SpeedKmh.Should().BeNull();
            summary.Worst.Single().Hours.Should().Be(0);
        }

        [Fact]
        public void ALegUnderTheMinimumDistanceIsNeverImpossibleHoweverFast()
        {
            var summary = JournalTravel.Summarise([At(0, 51.5074, -0.1278), At(0.001, 51.52, -0.1)]);

            summary.Legs.Should().Be(1);
            summary.Impossible.Should().Be(0);
        }

        [Fact]
        public void EachLaneIsItsOwnTrailAndTheOrderOfArrivalDoesNotMatter()
        {
            var summary = JournalTravel.Summarise(
            [
                At(2, 40.7128, -74.006, "B"),
                At(1, 48.8566, 2.3522),
                At(0, 51.5074, -0.1278),
                At(0.5, 51.5074, -0.1278, "B")
            ]);

            summary.Legs.Should().Be(2);
            summary.Impossible.Should().Be(1);
            summary.Worst.Single().Lane.Should().Be("B");
        }

        [Fact]
        public void OnlyTheFiveWorstLegsAreKeptFastestFirst()
        {
            var points = Enumerable.Range(0, 9)
                .Select(index => At(index * 0.1, index % 2 == 0 ? 51.5074 : 40.7128, index % 2 == 0 ? -0.1278 : -74.006))
                .ToList();

            var summary = JournalTravel.Summarise(points);

            summary.Impossible.Should().Be(8);
            summary.Worst.Should().HaveCount(JournalTravel.Worst);
            summary.Worst.Select(leg => leg.SpeedKmh).Should().BeInDescendingOrder();
        }
    }
}
