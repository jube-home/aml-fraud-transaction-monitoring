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
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowZoneTests
    {
        [Fact]
        public void ToZoneConvertsAnUnspecifiedValueTreatedAsUtc()
        {
            var flow = new DateTime(2024, 3, 15, 14, 0, 0).Start().ToZone("America/New_York");

            flow.Outcome.Should().Be(FlowOutcome.Undecided);
            flow.Value.Should().Be(new DateTime(2024, 3, 15, 10, 0, 0));
        }

        [Fact]
        public void ToZoneConvertsAUtcValue()
        {
            var flow = new DateTime(2024, 3, 15, 14, 0, 0, DateTimeKind.Utc).Start().ToZone("Asia/Tokyo");

            flow.Value.Should().Be(new DateTime(2024, 3, 15, 23, 0, 0));
        }

        [Fact]
        public void ToZoneConvertsALocalValueViaItsUniversalTime()
        {
            var local = new DateTime(2024, 3, 15, 14, 0, 0, DateTimeKind.Local);

            var flow = local.Start().ToZone("UTC");

            flow.Value.Should().Be(local.ToUniversalTime());
        }

        [Fact]
        public void ToZoneAppliesDaylightSavingForTheDateConcerned()
        {
            new DateTime(2024, 1, 15, 14, 0, 0).Start().ToZone("America/New_York").Value.Hour.Should().Be(9);
            new DateTime(2024, 7, 15, 14, 0, 0).Start().ToZone("America/New_York").Value.Hour.Should().Be(10);
        }

        [Theory]
        [InlineData("Not/AZone")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ToZoneErrorsRatherThanThrowingOnAnUnknownZone(string? zone)
        {
            var flow = new DateTime(2024, 3, 15, 14, 0, 0).Start().ToZone(zone);

            flow.Outcome.Should().Be(FlowOutcome.Errored);
            flow.Label.Should().NotBeNullOrEmpty();
            flow.ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void ToZoneOnADecidedFlowLeavesItUntouched()
        {
            new DateTime(2024, 3, 15, 14, 0, 0).Start().Match().ToZone("Not/AZone").Outcome
                .Should().Be(FlowOutcome.Matched);
        }

        [Fact]
        public void AZoneConversionThenAHourTestReadsLeftToRight()
        {
            new DateTime(2024, 3, 15, 14, 0, 0).Start().ToZone("America/New_York")
                .MatchWithinBusinessHours(9, 17).ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void ZoneAwareStepsAgreeWithConvertingFirst()
        {
            var value = new DateTime(2024, 3, 18, 0, 30, 0);

            var direct = value.Start().MatchWithinWeekdayBusinessHoursInZone("America/New_York", 9, 17);
            var converted = value.Start().ToZone("America/New_York").MatchWithinWeekdayBusinessHours(9, 17);

            direct.Outcome.Should().Be(converted.Outcome);
        }
    }
}
