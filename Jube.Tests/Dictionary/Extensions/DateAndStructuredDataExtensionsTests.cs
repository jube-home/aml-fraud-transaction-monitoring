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
    public sealed class DateAndStructuredDataExtensionsTests
    {
        [Theory]
        [InlineData("1990-06-15", "2026-06-14", 35)]
        [InlineData("1990-06-15", "2026-06-15", 36)]
        [InlineData("2000-02-29", "2026-02-28", 25)]
        public void AgeAsOfCountsCompletedYears(string born, string asOf, int expected)
        {
            DateTime.Parse(born).AgeAsOf(DateTime.Parse(asOf)).Should().Be(expected);
        }

        [Fact]
        public void AgeInDaysAsOfIgnoresTheTimeOfDay()
        {
            new DateTime(2026, 1, 1, 23, 0, 0).AgeInDaysAsOf(new DateTime(2026, 1, 3, 1, 0, 0)).Should().Be(2);
        }

        [Fact]
        public void ComparisonHelpersCompareInstants()
        {
            var a = new DateTime(2026, 1, 1, 9, 0, 0);
            var b = new DateTime(2026, 1, 1, 17, 30, 0);
            a.IsSameDayAs(b).Should().BeTrue();
            a.IsBefore(b).Should().BeTrue();
            a.IsAfter(b).Should().BeFalse();
            a.HoursBetween(b).Should().Be(8.5);
            a.MinutesBetween(b).Should().Be(510);
            a.SecondsBetween(b).Should().Be(30600);
            b.DaysBetween(a).Should().BeApproximately(-8.5 / 24, 1e-12);
        }

        [Theory]
        [InlineData("2026-01-31", "2026-02-28", 1)]
        [InlineData("2026-01-30", "2026-02-27", 0)]
        [InlineData("2026-01-31", "2026-03-31", 2)]
        [InlineData("2026-03-15", "2025-01-15", -14)]
        public void WholeMonthsBetweenCountsCompletedMonths(string from, string to, int expected)
        {
            DateTime.Parse(from).WholeMonthsBetween(DateTime.Parse(to)).Should().Be(expected);
        }

        [Fact]
        public void WholeYearsBetweenCountsCompletedYears()
        {
            new DateTime(2020, 5, 1).WholeYearsBetween(new DateTime(2026, 4, 30)).Should().Be(5);
        }

        [Theory]
        [InlineData("2026-09-23", 1, "2026-09-24")]
        [InlineData("2026-09-25", 1, "2026-09-28")]
        [InlineData("2026-09-23", 6, "2026-10-01")]
        [InlineData("2026-09-26", 5, "2026-10-02")]
        [InlineData("2026-09-28", -1, "2026-09-25")]
        [InlineData("2026-09-23", 0, "2026-09-23")]
        public void AddBusinessDaysSkipsWeekends(string start, int days, string expected)
        {
            DateTime.Parse(start).AddBusinessDays(days).Should().Be(DateTime.Parse(expected));
        }

        [Fact]
        public void AddBusinessDaysAgreesWithBusinessDaysUntil()
        {
            var start = new DateTime(2026, 9, 23);
            for (var n = 1; n <= 40; n++)
            {
                start.BusinessDaysUntil(start.AddBusinessDays(n)).Should().Be(n);
            }
        }

        [Fact]
        public void QuarterBoundariesAndFiscalPeriods()
        {
            var date = new DateTime(2026, 8, 17, 10, 0, 0);
            date.StartOfQuarter().Should().Be(new DateTime(2026, 7, 1));
            date.EndOfQuarter().Should().Be(new DateTime(2026, 10, 1).AddTicks(-1));
            date.FiscalYear(4).Should().Be(2027);
            date.FiscalQuarter(4).Should().Be(2);
            new DateTime(2026, 3, 31).FiscalYear(4).Should().Be(2026);
            new DateTime(2026, 3, 31).FiscalQuarter(4).Should().Be(4);
            date.FiscalYear(1).Should().Be(2026);
            var act = () => date.FiscalYear(13);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void IsLastDayOfMonthHandlesLeapYears()
        {
            new DateTime(2028, 2, 29).IsLastDayOfMonth().Should().BeTrue();
            new DateTime(2028, 2, 28).IsLastDayOfMonth().Should().BeFalse();
        }

        [Fact]
        public void IntervalOverlapsTreatsTouchingIntervalsAsDisjoint()
        {
            var start = new DateTime(2026, 1, 1);
            start.IntervalOverlaps(start.AddHours(2), start.AddHours(1), start.AddHours(3)).Should().BeTrue();
            start.IntervalOverlaps(start.AddHours(2), start.AddHours(2), start.AddHours(3)).Should().BeFalse();
        }

        [Fact]
        public void EpochConversionsRoundTrip()
        {
            var instant = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
            instant.ToEpochSeconds().Should().Be(1790164800);
            instant.ToEpochMilliseconds().Should().Be(1790164800000);
            1790164800d.FromEpochSeconds().Should().Be(instant);
            1790164800000d.FromEpochMilliseconds().Should().Be(instant);
        }

        [Fact]
        public void Iso8601AndExactParsing()
        {
            new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc).ToIso8601String().Should()
                .Be("2026-09-23T12:00:00.0000000Z");
            "23/09/2026 14:05".ToDateTimeWithFormat("dd/MM/yyyy HH:mm").Should()
                .Be(new DateTime(2026, 9, 23, 14, 5, 0));
            "2026-13-01".ToDateTimeWithFormat("yyyy-MM-dd").Should().BeNull();
        }

        [Fact]
        public void TimeZoneHelpersUseIanaZones()
        {
            var summer = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
            var winter = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            summer.UtcOffsetHoursIn("Europe/London").Should().Be(1);
            winter.UtcOffsetHoursIn("Europe/London").Should().Be(0);
            summer.IsDaylightSavingTimeIn("Europe/London").Should().BeTrue();
            winter.IsDaylightSavingTimeIn("Europe/London").Should().BeFalse();
            summer.UtcToZone("Asia/Kolkata").Should().Be(new DateTime(2026, 7, 1, 17, 30, 0));
            new DateTime(2026, 7, 1, 17, 30, 0).ZoneToUtc("Asia/Kolkata").Should().Be(summer);
            "Europe/Paris".IsValidTimeZoneId().Should().BeTrue();
            "../../etc/passwd".IsValidTimeZoneId().Should().BeFalse();
            summer.UtcOffsetHoursIn("Nowhere/Nothing").Should().Be(double.NaN);
        }

        private const string Json =
            "{\"customer\":{\"name\":\"Ann\",\"tags\":[\"a\",\"b\"],\"limits\":[{\"amount\":10.5}]},\"odd key\":1}";

        [Fact]
        public void JsonHelpersNavigateDottedAndIndexedPaths()
        {
            Json.IsValidJson().Should().BeTrue();
            "{bad".IsValidJson().Should().BeFalse();
            Json.JsonTextAt("$.customer.name").Should().Be("Ann");
            Json.JsonTextAt("customer.tags[-1]").Should().Be("b");
            Json.JsonTextAt("$.customer.tags").Should().Be("[\"a\",\"b\"]");
            Json.JsonNumberAt("$.customer.limits[0].amount").Should().Be(10.5);
            Json.JsonNumberAt("$.customer.name").Should().Be(double.NaN);
            Json.JsonArrayLengthAt("$.customer.tags").Should().Be(2);
            Json.JsonArrayLengthAt("$.customer").Should().Be(-1);
            Json.JsonHasPath("$['odd key']").Should().BeTrue();
            Json.JsonHasPath("$.customer.missing").Should().BeFalse();
            "{ \"a\" : [ 1, 2 ] }".JsonMinified().Should().Be("{\"a\":[1,2]}");
        }

        [Fact]
        public void XmlHelpersReadValuesAndRefuseDtds()
        {
            const string xml = "<doc><party role=\"payer\"><name>Ann</name></party></doc>";
            xml.IsValidXml().Should().BeTrue();
            xml.XmlTextAt("/doc/party[@role='payer']/name").Should().Be("Ann");
            xml.XmlTextAt("/doc/missing").Should().BeEmpty();
            xml.XmlTextAt("[[[").Should().BeEmpty();
            const string entity = "<!DOCTYPE d [<!ENTITY x \"boom\">]><d>&x;</d>";
            entity.IsValidXml().Should().BeFalse();
        }
    }
}