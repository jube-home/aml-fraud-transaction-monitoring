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
using Jube.Service.Extensions.TimeZonesAndFormats;
using Jube.Service.Extensions.TimeZonesAndFormats.Models;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class TimeZonesAndFormatsToolsTests
    {
        private static readonly DateTime summer = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

        [Fact]
        [Trait("Covers", "ConvertUtcToTimeZone")]
        [Trait("Covers", "ConvertTimeZoneToUtc")]
        [Trait("Covers", "ConvertBetweenTimeZones")]
        public void ConversionsAllowForDaylightSaving()
        {
            TimeZonesAndFormatsTools.ConvertUtcToTimeZone(summer, "Europe/London").Should()
                .Be(new DateTime(2026, 7, 1, 9, 0, 0));

            TimeZonesAndFormatsTools.ConvertTimeZoneToUtc(new DateTime(2026, 7, 1, 9, 0, 0), "Europe/London").Should()
                .Be(summer);

            TimeZonesAndFormatsTools
                .ConvertBetweenTimeZones(new DateTime(2026, 7, 1, 9, 0, 0), "Europe/London", "America/New_York")
                .Should().Be(new DateTime(2026, 7, 1, 4, 0, 0));

            var skipped = () =>
                TimeZonesAndFormatsTools.ConvertTimeZoneToUtc(new DateTime(2026, 3, 29, 1, 30, 0), "Europe/London");

            skipped.Should().Throw<ArgumentException>().WithMessage("*clock change*");
            var unknown = () => TimeZonesAndFormatsTools.ConvertUtcToTimeZone(summer, "Mars/Olympus");
            unknown.Should().Throw<ArgumentException>().WithMessage("*IANA*");
        }

        [Fact]
        [Trait("Covers", "DescribeTimeZoneAt")]
        [Trait("Covers", "IsValidTimeZoneId")]
        public void ZoneDescriptions()
        {
            TimeZonesAndFormatsTools.DescribeTimeZoneAt(summer, "Europe/London").Should()
                .Be(new TimeZoneOffset("Europe/London", 1, true));

            TimeZonesAndFormatsTools.DescribeTimeZoneAt(summer, "Asia/Kolkata").UtcOffsetHours.Should().Be(5.5);
            TimeZonesAndFormatsTools.IsValidTimeZoneId("Australia/Sydney").Should().BeTrue();
            TimeZonesAndFormatsTools.IsValidTimeZoneId("/etc/passwd").Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "ToUnixEpoch")]
        [Trait("Covers", "FromUnixEpoch")]
        public void EpochRoundTrip()
        {
            var instant = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
            TimeZonesAndFormatsTools.ToUnixEpoch(instant).Should().Be(new EpochTime(1790164800, 1790164800000));
            TimeZonesAndFormatsTools.FromUnixEpoch(1790164800).Should().Be(instant);
            TimeZonesAndFormatsTools.FromUnixEpoch(1790164800000, "Milliseconds").Should().Be(instant);
            var act = () => TimeZonesAndFormatsTools.FromUnixEpoch(1e15);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "FormatDateTime")]
        [Trait("Covers", "ParseDateTimeWithFormat")]
        public void FormattingAndParsing()
        {
            var instant = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
            TimeZonesAndFormatsTools.FormatDateTime(instant, "Iso8601").Should().Be("2026-09-23T12:00:00.0000000Z");
            TimeZonesAndFormatsTools.FormatDateTime(instant, "Rfc1123").Should().Be("Wed, 23 Sep 2026 12:00:00 GMT");
            TimeZonesAndFormatsTools.FormatDateTime(instant, "Sortable").Should().Be("2026-09-23T12:00:00");

            TimeZonesAndFormatsTools.ParseDateTimeWithFormat("20260923", "yyyyMMdd").Should()
                .Be(new DateTime(2026, 9, 23));

            var act = () => TimeZonesAndFormatsTools.ParseDateTimeWithFormat("23/09/2026", "yyyyMMdd");
            act.Should().Throw<ArgumentException>();
        }
    }
}