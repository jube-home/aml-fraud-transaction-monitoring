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
using System.Globalization;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class DateTimeExtensionsTests
    {
        private static readonly DateTime sample = new(2024, 6, 15, 14, 30, 45, DateTimeKind.Unspecified);

        [Fact]
        public void AgeIsComputedFromTodayRelativeToTheBirthDate()
        {
            var eighteenYearsAgoAlreadyHadBirthday = DateTime.Today.AddYears(-18).AddDays(-1);

            eighteenYearsAgoAlreadyHadBirthday.Age().Should().Be(18);
        }

        [Fact]
        public void AgeDoesNotCountThisYearWhenTheBirthdayHasNotYetOccurred()
        {
            var birthdayIsTomorrow = DateTime.Today.AddYears(-18).AddDays(1);

            birthdayIsTomorrow.Age().Should().Be(17, "the birthday has not happened yet this year");
        }

        [Fact]
        public void AgeOnTheExactBirthdayCountsTheYearAlready()
        {
            var bornExactlyTenYearsAgoToday = DateTime.Today.AddYears(-10);

            bornExactlyTenYearsAgoToday.Age().Should().Be(10);
        }

        [Fact]
        public void BetweenIsTrueOnlyStrictlyInsideTheRangeExcludingBothEndpoints()
        {
            var min = new DateTime(2024, 1, 1);
            var max = new DateTime(2024, 12, 31);
            var middle = new DateTime(2024, 6, 1);

            middle.Between(min, max).Should().BeTrue();
            min.Between(min, max).Should().BeFalse("Between is exclusive of the min endpoint");
            max.Between(min, max).Should().BeFalse("Between is exclusive of the max endpoint");
        }

        [Fact]
        public void ConvertTimeBySystemTimeZoneIdWithASingleZoneConvertsFromLocalToTheDestinationZone()
        {
            var utc = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            var expected = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utc, "UTC");

            var converted = utc.ConvertTimeBySystemTimeZoneId("UTC");

            converted.Should().Be(expected);
        }

        [Fact]
        public void ConvertTimeBySystemTimeZoneIdWithSourceAndDestinationConvertsBetweenTheTwoNamedZones()
        {
            var unspecified = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

            var converted = unspecified.ConvertTimeBySystemTimeZoneId("UTC", "America/New_York");

            var expected =
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(unspecified, "UTC", "America/New_York");
            converted.Should().Be(expected);
        }

        [Fact]
        public void ConvertTimeBySystemTimeZoneIdThrowsForAnUnknownTimeZoneId()
        {
            var act = () => DateTime.UtcNow.ConvertTimeBySystemTimeZoneId("Not/A/Real/Zone");

            act.Should().Throw<TimeZoneNotFoundException>();
        }

        [Fact]
        public void ConvertTimeWithASingleDestinationZoneMatchesTimeZoneInfoConvertTime()
        {
            var utc = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            var destination = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

            var converted = utc.ConvertTime(destination);

            converted.Should().Be(TimeZoneInfo.ConvertTime(utc, destination));
        }

        [Fact]
        public void ConvertTimeWithSourceAndDestinationZonesMatchesTimeZoneInfoConvertTime()
        {
            var unspecified = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);
            var source = TimeZoneInfo.FindSystemTimeZoneById("UTC");
            var destination = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

            var converted = unspecified.ConvertTime(source, destination);

            converted.Should().Be(TimeZoneInfo.ConvertTime(unspecified, source, destination));
        }

        [Fact]
        public void ConvertTimeFromUtcConvertsAUtcInstantIntoTheDestinationZonesLocalTime()
        {
            var utc = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            var destination = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

            var converted = utc.ConvertTimeFromUtc(destination);

            converted.Should().Be(TimeZoneInfo.ConvertTimeFromUtc(utc, destination));
        }

        [Fact]
        public void ConvertTimeToUtcWithNoSourceZoneReturnsAnAlreadyUtcValueUnchanged()
        {
            var alreadyUtc = DateTime.UtcNow;

            var converted = alreadyUtc.ConvertTimeToUtc();

            converted.Should().Be(alreadyUtc);
            converted.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void ConvertTimeToUtcWithAnExplicitSourceZoneThrowsWhenTheKindIsUtcButTheZoneIsNot()
        {
            var alreadyUtc = DateTime.UtcNow;
            var nonUtcZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

            var act = () => alreadyUtc.ConvertTimeToUtc(nonUtcZone);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ConvertTimeToUtcWithNoSourceZoneConvertsAnUnspecifiedOrLocalDateTimeToUtc()
        {
            var unspecified = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

            var converted = unspecified.ConvertTimeToUtc();

            converted.Should().Be(TimeZoneInfo.ConvertTimeToUtc(unspecified));
            converted.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void ConvertTimeToUtcWithAnExplicitSourceZoneConvertsToUtc()
        {
            var unspecified = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);
            var source = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

            var converted = unspecified.ConvertTimeToUtc(source);

            converted.Should().Be(TimeZoneInfo.ConvertTimeToUtc(unspecified, source));
            converted.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void ElapsedReturnsTheDurationSinceTheGivenDateTimeMeasuredAgainstNow()
        {
            var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);

            var elapsed = fiveMinutesAgo.Elapsed();

            elapsed.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void EndOfDayReturnsOneMillisecondBeforeMidnightOfTheSameDay()
        {
            var endOfDay = sample.EndOfDay();

            endOfDay.Should().Be(new DateTime(2024, 6, 15, 23, 59, 59, 999));
        }

        [Fact]
        public void EndOfMonthReturnsOneMillisecondBeforeMidnightOnTheFirstOfTheNextMonth()
        {
            var endOfMonth = sample.EndOfMonth();

            endOfMonth.Should().Be(new DateTime(2024, 6, 30, 23, 59, 59, 999));
        }

        [Fact]
        public void EndOfMonthHandlesFebruaryInALeapYearCorrectly()
        {
            var leapFebruary = new DateTime(2024, 2, 10);

            leapFebruary.EndOfMonth().Should().Be(new DateTime(2024, 2, 29, 23, 59, 59, 999));
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday)]
        [InlineData(DayOfWeek.Monday)]
        public void EndOfWeekReturnsTheLastMomentOfTheDayBeforeTheNextStartDayOfWeek(DayOfWeek startDayOfWeek)
        {
            var endOfWeek = sample.EndOfWeek(startDayOfWeek);

            var expectedEndDayOfWeek = startDayOfWeek == DayOfWeek.Sunday ? DayOfWeek.Saturday : DayOfWeek.Sunday;
            endOfWeek.DayOfWeek.Should().Be(expectedEndDayOfWeek);
            endOfWeek.TimeOfDay.Should().Be(new TimeSpan(0, 23, 59, 59, 999));
            endOfWeek.Should().BeOnOrAfter(sample.Date);
        }

        [Fact]
        public void EndOfYearReturnsOneMillisecondBeforeMidnightOnJanuaryFirstOfTheNextYear()
        {
            sample.EndOfYear().Should().Be(new DateTime(2024, 12, 31, 23, 59, 59, 999));
        }

        [Fact]
        public void FirstDayOfWeekReturnsTheSundayOfThatWeekAtMidnight()
        {
            var firstDay = sample.FirstDayOfWeek();

            firstDay.Should().Be(new DateTime(2024, 6, 9));
            firstDay.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        }

        [Fact]
        public void InReturnsTrueOnlyWhenTheExactDateTimeIsAmongTheProvidedValues()
        {
            var a = new DateTime(2024, 1, 1);
            var b = new DateTime(2024, 2, 1);

            a.In(a, b).Should().BeTrue();
            new DateTime(2024, 3, 1).In(a, b).Should().BeFalse();
        }

        [Fact]
        public void NotInIsTheInverseOfIn()
        {
            var a = new DateTime(2024, 1, 1);
            var b = new DateTime(2024, 2, 1);

            a.NotIn(a, b).Should().BeFalse();
            new DateTime(2024, 3, 1).NotIn(a, b).Should().BeTrue();
        }

        [Fact]
        public void InRangeIsInclusiveOfBothEndpointsUnlikeBetween()
        {
            var min = new DateTime(2024, 1, 1);
            var max = new DateTime(2024, 12, 31);

            min.InRange(min, max).Should().BeTrue();
            max.InRange(min, max).Should().BeTrue();
            new DateTime(2023, 12, 31).InRange(min, max).Should().BeFalse();
        }

        [Theory]
        [InlineData(11, 59, false)]
        [InlineData(12, 0, true)]
        [InlineData(13, 0, true)]
        public void IsAfternoonIsTrueAtOrAfterNoon(int hour, int minute, bool expected)
        {
            new DateTime(2024, 1, 1, hour, minute, 0).IsAfternoon().Should().Be(expected);
        }

        [Theory]
        [InlineData(11, 59, true)]
        [InlineData(12, 0, false)]
        public void IsMorningIsTrueOnlyBeforeNoon(int hour, int minute, bool expected)
        {
            new DateTime(2024, 1, 1, hour, minute, 0).IsMorning().Should().Be(expected);
        }

        [Fact]
        public void IsDateEqualComparesOnlyTheDatePortionIgnoringTime()
        {
            var morning = new DateTime(2024, 6, 15, 6, 0, 0);
            var evening = new DateTime(2024, 6, 15, 22, 0, 0);
            var otherDay = new DateTime(2024, 6, 16, 6, 0, 0);

            morning.IsDateEqual(evening).Should().BeTrue();
            morning.IsDateEqual(otherDay).Should().BeFalse();
        }

        [Fact]
        public void IsTimeEqualComparesOnlyTheTimeOfDayIgnoringTheDate()
        {
            var day1 = new DateTime(2024, 1, 1, 10, 30, 0);
            var day2 = new DateTime(2025, 5, 5, 10, 30, 0);
            var differentTime = new DateTime(2024, 1, 1, 10, 31, 0);

            day1.IsTimeEqual(day2).Should().BeTrue();
            day1.IsTimeEqual(differentTime).Should().BeFalse();
        }

        [Fact]
        [Obsolete("Obsolete")]
        public void IsDaylightSavingTimeDelegatesToTheLegacyTimeZoneApi()
        {
            var daylightTimes = TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year);
            var probe = DateTime.Now;

            var expected = TimeZone.IsDaylightSavingTime(probe, daylightTimes);

            probe.IsDaylightSavingTime(daylightTimes).Should().Be(expected);
        }

        [Fact]
        public void IsFutureIsTrueForADateTimeAfterNow()
        {
            var before = DateTime.Now;

            var future = DateTime.Now.AddHours(1);

            future.IsFuture().Should().BeTrue();
            before.AddSeconds(-1).IsFuture().Should().BeFalse();
        }

        [Fact]
        public void IsPastIsTrueForADateTimeBeforeNow()
        {
            var past = DateTime.Now.AddHours(-1);
            var future = DateTime.Now.AddHours(1);

            past.IsPast().Should().BeTrue();
            future.IsPast().Should().BeFalse();
        }

        [Fact]
        public void IsNowIsPracticallyAlwaysFalseBecauseExactEqualityToTheCurrentInstantIsNotAchievable()
        {
            var capturedJustNow = DateTime.Now;

            capturedJustNow.IsNow().Should().BeFalse();
        }

        [Fact]
        public void IsTodayIsTrueForAnyTimeOnTheCurrentDateAndFalseOtherwise()
        {
            var laterToday = DateTime.Today.AddHours(1);
            var yesterday = DateTime.Today.AddDays(-1);

            laterToday.IsToday().Should().BeTrue();
            yesterday.IsToday().Should().BeFalse();
        }

        [Theory]
        [InlineData(DayOfWeek.Monday, true)]
        [InlineData(DayOfWeek.Friday, true)]
        [InlineData(DayOfWeek.Saturday, false)]
        [InlineData(DayOfWeek.Sunday, false)]
        public void IsWeekDayIsFalseOnlyForSaturdayAndSunday(DayOfWeek dayOfWeek, bool expectedWeekDay)
        {
            var date = DateOnCorrectDayOfWeek(dayOfWeek);

            date.IsWeekDay().Should().Be(expectedWeekDay);
            date.IsWeekendDay().Should().Be(!expectedWeekDay);
        }

        private static DateTime DateOnCorrectDayOfWeek(DayOfWeek dayOfWeek)
        {
            var date = new DateTime(2024, 6, 9);
            while (date.DayOfWeek != dayOfWeek)
            {
                date = date.AddDays(1);
            }

            return date;
        }

        [Fact]
        public void LastDayOfWeekReturnsTheSaturdayOfThatWeekAtMidnight()
        {
            var lastDay = sample.LastDayOfWeek();

            lastDay.Should().Be(new DateTime(2024, 6, 15));
            lastDay.DayOfWeek.Should().Be(DayOfWeek.Saturday);
        }

        [Fact]
        public void SetTimeWithOnlyHourZeroesOutMinutesSecondsAndMilliseconds()
        {
            sample.SetTime(9).Should().Be(new DateTime(2024, 6, 15, 9, 0, 0, 0));
        }

        [Fact]
        public void SetTimeWithHourAndMinuteZeroesOutSecondsAndMilliseconds()
        {
            sample.SetTime(9, 15).Should().Be(new DateTime(2024, 6, 15, 9, 15, 0, 0));
        }

        [Fact]
        public void SetTimeWithHourMinuteAndSecondZeroesOutMilliseconds()
        {
            sample.SetTime(9, 15, 30).Should().Be(new DateTime(2024, 6, 15, 9, 15, 30, 0));
        }

        [Fact]
        public void SetTimeWithAllFourComponentsReplacesTheTimePortionEntirely()
        {
            sample.SetTime(9, 15, 30, 250).Should().Be(new DateTime(2024, 6, 15, 9, 15, 30, 250));
        }

        [Fact]
        public void StartOfDayZeroesOutTheTimePortion()
        {
            sample.StartOfDay().Should().Be(new DateTime(2024, 6, 15));
        }

        [Fact]
        public void StartOfMonthReturnsTheFirstOfTheMonthAtMidnight()
        {
            sample.StartOfMonth().Should().Be(new DateTime(2024, 6, 1));
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday)]
        [InlineData(DayOfWeek.Monday)]
        public void StartOfWeekReturnsTheRequestedStartDayOfWeekAtMidnightNoLaterThanTheInput(
            DayOfWeek startDayOfWeek)
        {
            var startOfWeek = sample.StartOfWeek(startDayOfWeek);

            startOfWeek.DayOfWeek.Should().Be(startDayOfWeek);
            startOfWeek.TimeOfDay.Should().Be(TimeSpan.Zero);
            startOfWeek.Should().BeOnOrBefore(sample.Date);
            (sample.Date - startOfWeek).Days.Should().BeLessThan(7);
        }

        [Fact]
        public void StartOfYearReturnsJanuaryFirstAtMidnight()
        {
            sample.StartOfYear().Should().Be(new DateTime(2024, 1, 1));
        }

        [Fact]
        public void ToEpochTimeSpanMeasuresTheDurationSinceTheUnixEpoch()
        {
            var epoch = new DateTime(1970, 1, 1);
            var oneDayAfterEpoch = epoch.AddDays(1);

            oneDayAfterEpoch.ToEpochTimeSpan().Should().Be(TimeSpan.FromDays(1));
        }

        [Fact]
        public void ToEpochTimeSpanIsNegativeForADateTimeBeforeTheEpoch()
        {
            var beforeEpoch = new DateTime(1969, 12, 31);

            beforeEpoch.ToEpochTimeSpan().Should().Be(-TimeSpan.FromDays(1));
        }

        [Fact]
        public void TomorrowAddsExactlyOneDayPreservingTheTimeOfDay()
        {
            sample.Tomorrow().Should().Be(new DateTime(2024, 6, 16, 14, 30, 45));
        }

        [Fact]
        public void YesterdaySubtractsExactlyOneDayPreservingTheTimeOfDay()
        {
            sample.Yesterday().Should().Be(new DateTime(2024, 6, 14, 14, 30, 45));
        }

        [Fact]
        public void TomorrowAndYesterdayAreInversesOfEachOther()
        {
            sample.Tomorrow().Yesterday().Should().Be(sample);
        }

        [Theory]
        [InlineData("F")]
        [InlineData("f")]
        [InlineData("D")]
        [InlineData("T")]
        [InlineData("m")]
        [InlineData("r")]
        [InlineData("G")]
        [InlineData("d")]
        [InlineData("g")]
        [InlineData("t")]
        [InlineData("s")]
        [InlineData("u")]
        [InlineData("U")]
        [InlineData("y")]
        public void EachFormatStringProducesWhatDateTimeToStringWouldProduceUnderTheSameCulture(string formatChar)
        {
            var expected = sample.ToString(formatChar, CultureInfo.InvariantCulture);

            var actual = formatChar switch
            {
                "F" => sample.ToFullDateTimeString(CultureInfo.InvariantCulture),
                "f" => sample.ToLongDateShortTimeString(CultureInfo.InvariantCulture),
                "D" => sample.ToLongDateString(CultureInfo.InvariantCulture),
                "T" => sample.ToLongTimeString(CultureInfo.InvariantCulture),
                "m" => sample.ToMonthDayString(CultureInfo.InvariantCulture),
                "r" => sample.ToRFC1123String(CultureInfo.InvariantCulture),
                "G" => sample.ToShortDateLongTimeString(CultureInfo.InvariantCulture),
                "d" => sample.ToShortDateString(CultureInfo.InvariantCulture),
                "g" => sample.ToShortDateTimeString(CultureInfo.InvariantCulture),
                "t" => sample.ToShortTimeString(CultureInfo.InvariantCulture),
                "s" => sample.ToSortableDateTimeString(CultureInfo.InvariantCulture),
                "u" => sample.ToUniversalSortableDateTimeString(CultureInfo.InvariantCulture),
                "U" => sample.ToUniversalSortableLongDateTimeString(CultureInfo.InvariantCulture),
                "y" => sample.ToYearMonthString(CultureInfo.InvariantCulture),
                _ => throw new InvalidOperationException()
            };

            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData("F")]
        [InlineData("d")]
        [InlineData("s")]
        public void TheStringCultureNameOverloadAgreesWithTheCultureInfoOverload(string formatChar)
        {
            const string cultureName = "fr-FR";
            var byName = formatChar switch
            {
                "F" => sample.ToFullDateTimeString(cultureName),
                "d" => sample.ToShortDateString(cultureName),
                "s" => sample.ToSortableDateTimeString(cultureName),
                _ => throw new InvalidOperationException()
            };
            var byCultureInfo = formatChar switch
            {
                "F" => sample.ToFullDateTimeString(new CultureInfo(cultureName)),
                "d" => sample.ToShortDateString(new CultureInfo(cultureName)),
                "s" => sample.ToSortableDateTimeString(new CultureInfo(cultureName)),
                _ => throw new InvalidOperationException()
            };

            byName.Should().Be(byCultureInfo);
        }

        [Fact]
        public void TheSortableDateTimeStringFormatIsCultureInvariantByDesignEvenWithADifferentCultureRequested()
        {
            var invariant = sample.ToSortableDateTimeString(CultureInfo.InvariantCulture);
            var french = sample.ToSortableDateTimeString(new CultureInfo("fr-FR"));

            invariant.Should().Be(french).And.Be("2024-06-15T14:30:45");
        }

        [Fact]
        public void ToLongDateTimeStringIsAnAliasForTheFullDateTimeStringFormat()
        {
            sample.ToLongDateTimeString().Should().Be(sample.ToFullDateTimeString());
        }

        [Fact]
        public void ToLongDateTimeStringWithACultureNameAgreesWithTheCultureInfoOverload()
        {
            const string cultureName = "fr-FR";

            sample.ToLongDateTimeString(cultureName).Should()
                .Be(sample.ToLongDateTimeString(new CultureInfo(cultureName)));
        }

        [Fact]
        public void ToLongDateTimeStringMatchesDateTimeToStringWithTheFFormat()
        {
            sample.ToLongDateTimeString(CultureInfo.InvariantCulture).Should()
                .Be(sample.ToString("F", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void BusinessDaysUntilCountsWeekdaysAfterTheStartUpToAndIncludingTheEnd()
        {
            var monday = new DateTime(2024, 6, 10);
            var friday = new DateTime(2024, 6, 14);
            var nextMonday = new DateTime(2024, 6, 17);

            monday.BusinessDaysUntil(friday).Should().Be(4);
            monday.BusinessDaysUntil(nextMonday).Should().Be(5);
        }

        [Fact]
        public void BusinessDaysUntilOfTheSameDateIsZero()
        {
            var monday = new DateTime(2024, 6, 10);

            monday.BusinessDaysUntil(monday).Should().Be(0);
        }

        [Fact]
        public void BusinessDaysUntilIsNegativeWhenTheOtherDateIsEarlier()
        {
            var monday = new DateTime(2024, 6, 10);
            var friday = new DateTime(2024, 6, 14);

            friday.BusinessDaysUntil(monday).Should().Be(-4);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(3, 1)]
        [InlineData(4, 2)]
        [InlineData(6, 2)]
        [InlineData(7, 3)]
        [InlineData(9, 3)]
        [InlineData(10, 4)]
        [InlineData(12, 4)]
        public void QuarterOfYearMapsTheMonthToItsQuarter(int month, int expectedQuarter)
        {
            new DateTime(2024, month, 1).QuarterOfYear().Should().Be(expectedQuarter);
        }

        [Fact]
        public void IsoWeekOfYearOfTheFirstMondayOfTwentyTwentyFourIsWeekOne()
        {
            new DateTime(2024, 1, 1).IsoWeekOfYear().Should().Be(1);
        }

        [Fact]
        public void IsoWeekOfYearDelegatesToTheBclIsoWeekImplementation()
        {
            sample.IsoWeekOfYear().Should().Be(ISOWeek.GetWeekOfYear(sample));
        }

        [Fact]
        public void AgeInDaysCountsWholeDaysSinceTheDate()
        {
            DateTime.Today.AddDays(-30).AgeInDays().Should().Be(30);
            DateTime.Today.AgeInDays().Should().Be(0);
        }

        [Fact]
        public void AgeInDaysIsNegativeForAFutureDate()
        {
            DateTime.Today.AddDays(5).AgeInDays().Should().Be(-5);
        }

        [Theory]
        [InlineData(14, 9, 17, true)]
        [InlineData(8, 9, 17, false)]
        [InlineData(17, 9, 17, false)]
        [InlineData(9, 9, 17, true)]
        public void IsWithinBusinessHoursChecksTheHourAgainstAHalfOpenRange(int hour, int startHour, int endHour,
            bool expected)
        {
            new DateTime(2024, 6, 10, hour, 0, 0).IsWithinBusinessHours(startHour, endHour).Should().Be(expected);
        }
    }
}