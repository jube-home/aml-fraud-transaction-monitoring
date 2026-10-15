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
using FluentAssertions;
using Jube.Service.Helpers.ClockAndIdentifiers;
using Jube.Service.Helpers.ClockAndIdentifiers.Models;
using Xunit;

namespace Jube.Test.Service.Helpers
{
    [Trait("Category", "Unit")]
    public sealed class ClockAndIdentifiersToolsTests
    {
        private static readonly DateTimeOffset now = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);

        private static ClockAndIdentifiersTools Tools()
        {
            return new ClockAndIdentifiersTools(new FixedClock(now));
        }

        [Fact]
        [Trait("Covers", "CurrentUtcTime")]
        [Trait("Covers", "CurrentTimeInZone")]
        [Trait("Covers", "TimeSince")]
        public void ClockToolsReadTheInjectedClock()
        {
            Tools().CurrentUtcTime().Should().Be(new CurrentTime("UTC", now.UtcDateTime, new DateTime(2026, 7, 1), 0));
            var london = Tools().CurrentTimeInZone("Europe/London");
            london.Now.Should().Be(new DateTime(2026, 7, 1, 9, 0, 0));
            london.UtcOffsetHours.Should().Be(1);
            Tools().TimeSince(new DateTime(2024, 7, 1, 8, 0, 0, DateTimeKind.Utc)).WholeYears.Should().Be(2);
            var act = () => Tools().CurrentTimeInZone("Nowhere/Nothing");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ListTimeZoneIds")]
        public void ListTimeZoneIdsFilters()
        {
            Tools().ListTimeZoneIds("London").Should().Contain("Europe/London");
            Tools().ListTimeZoneIds().Length.Should().BeLessThanOrEqualTo(100);
        }

        [Fact]
        [Trait("Covers", "NewRandomUuid")]
        [Trait("Covers", "NewTimeOrderedUuid")]
        [Trait("Covers", "UuidVersion7Timestamp")]
        public void UuidTools()
        {
            var random = Tools().NewRandomUuid(3);
            random.Should().HaveCount(3).And.OnlyHaveUniqueItems();
            random.Should().OnlyContain(u => Guid.Parse(u).Version == 4);
            var ordered = Tools().NewTimeOrderedUuid()[0];
            Guid.Parse(ordered).Version.Should().Be(7);
            Tools().UuidVersion7Timestamp(ordered).Should().Be(now.UtcDateTime);
            var act = () => Tools().NewRandomUuid(101);
            act.Should().Throw<ArgumentException>();
            var notV7 = () => Tools().UuidVersion7Timestamp(random[0]);
            notV7.Should().Throw<ArgumentException>();
        }

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow()
            {
                return now;
            }
        }
    }
}