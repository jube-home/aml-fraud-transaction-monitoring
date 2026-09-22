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
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowDateTimeTests
    {
        [Theory]
        [InlineData("2024-03-15T10:00:00", true)]
        [InlineData("2024-03-16T10:00:00", false)]
        [InlineData("2024-03-18T00:00:00", true)]
        [InlineData("2024-03-17T23:59:59", false)]
        [InlineData("2024-03-11T12:00:00", true)]
        public void IsWeekdayAppliesTheOutcomeOfEveryStepKind(string valueText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsWeekday(),
                start.RejectIsWeekday(),
                start.BreakIsWeekday(),
                start.RequireIsWeekday(),
                start.EnsureIsWeekday());

            FlowAssert.Kinds(expected,
                value.MatchIsWeekday(),
                value.RejectIsWeekday(),
                value.BreakIsWeekday(),
                value.RequireIsWeekday(),
                value.EnsureIsWeekday());
        }

        [Theory]
        [InlineData("2024-03-16T10:00:00", true)]
        [InlineData("2024-03-17T10:00:00", true)]
        [InlineData("2024-03-15T10:00:00", false)]
        public void IsWeekendAppliesTheOutcomeOfEveryStepKind(string valueText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsWeekend(),
                start.RejectIsWeekend(),
                start.BreakIsWeekend(),
                start.RequireIsWeekend(),
                start.EnsureIsWeekend());

            FlowAssert.Kinds(expected,
                value.MatchIsWeekend(),
                value.RejectIsWeekend(),
                value.BreakIsWeekend(),
                value.RequireIsWeekend(),
                value.EnsureIsWeekend());
        }

        [Theory]
        [InlineData("2024-03-15T09:00:00", true)]
        [InlineData("2024-03-15T12:00:00", false)]
        [InlineData("2024-03-15T00:00:00", true)]
        [InlineData("2024-03-15T11:59:59", true)]
        [InlineData("2024-03-15T23:59:59", false)]
        public void IsMorningAppliesTheOutcomeOfEveryStepKind(string valueText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsMorning(),
                start.RejectIsMorning(),
                start.BreakIsMorning(),
                start.RequireIsMorning(),
                start.EnsureIsMorning());

            FlowAssert.Kinds(expected,
                value.MatchIsMorning(),
                value.RejectIsMorning(),
                value.BreakIsMorning(),
                value.RequireIsMorning(),
                value.EnsureIsMorning());
        }

        [Theory]
        [InlineData("2024-03-15T12:00:00", true)]
        [InlineData("2024-03-15T09:00:00", false)]
        [InlineData("2024-03-15T23:59:59", true)]
        [InlineData("2024-03-15T11:59:59", false)]
        [InlineData("2024-03-15T00:00:00", false)]
        public void IsAfternoonAppliesTheOutcomeOfEveryStepKind(string valueText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsAfternoon(),
                start.RejectIsAfternoon(),
                start.BreakIsAfternoon(),
                start.RequireIsAfternoon(),
                start.EnsureIsAfternoon());

            FlowAssert.Kinds(expected,
                value.MatchIsAfternoon(),
                value.RejectIsAfternoon(),
                value.BreakIsAfternoon(),
                value.RequireIsAfternoon(),
                value.EnsureIsAfternoon());
        }

        [Theory]
        [InlineData("2024-03-15T10:00:00", "2024-03-16T10:00:00", true)]
        [InlineData("2024-03-16T10:00:00", "2024-03-16T10:00:00", false)]
        [InlineData("2024-03-17T10:00:00", "2024-03-16T10:00:00", false)]
        public void BeforeAppliesTheOutcomeOfEveryStepKind(string valueText, string otherText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var other = DateTime.Parse(otherText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchBefore(other),
                start.RejectBefore(other),
                start.BreakBefore(other),
                start.RequireBefore(other),
                start.EnsureBefore(other));

            FlowAssert.Kinds(expected,
                value.MatchBefore(other),
                value.RejectBefore(other),
                value.BreakBefore(other),
                value.RequireBefore(other),
                value.EnsureBefore(other));
        }

        [Theory]
        [InlineData("2024-03-17T10:00:00", "2024-03-16T10:00:00", true)]
        [InlineData("2024-03-16T10:00:00", "2024-03-16T10:00:00", false)]
        [InlineData("2024-03-15T10:00:00", "2024-03-16T10:00:00", false)]
        public void AfterAppliesTheOutcomeOfEveryStepKind(string valueText, string otherText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var other = DateTime.Parse(otherText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchAfter(other),
                start.RejectAfter(other),
                start.BreakAfter(other),
                start.RequireAfter(other),
                start.EnsureAfter(other));

            FlowAssert.Kinds(expected,
                value.MatchAfter(other),
                value.RejectAfter(other),
                value.BreakAfter(other),
                value.RequireAfter(other),
                value.EnsureAfter(other));
        }

        [Theory]
        [InlineData("2024-03-15T10:00:00", "2024-03-14T00:00:00", "2024-03-16T00:00:00", true)]
        [InlineData("2024-03-14T00:00:00", "2024-03-14T00:00:00", "2024-03-16T00:00:00", false)]
        [InlineData("2024-03-16T00:00:00", "2024-03-14T00:00:00", "2024-03-16T00:00:00", false)]
        public void BetweenAppliesTheOutcomeOfEveryStepKind(string valueText, string rangeStartText, string rangeEndText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var rangeStart = DateTime.Parse(rangeStartText, CultureInfo.InvariantCulture);
            var rangeEnd = DateTime.Parse(rangeEndText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchBetween(rangeStart, rangeEnd),
                start.RejectBetween(rangeStart, rangeEnd),
                start.BreakBetween(rangeStart, rangeEnd),
                start.RequireBetween(rangeStart, rangeEnd),
                start.EnsureBetween(rangeStart, rangeEnd));

            FlowAssert.Kinds(expected,
                value.MatchBetween(rangeStart, rangeEnd),
                value.RejectBetween(rangeStart, rangeEnd),
                value.BreakBetween(rangeStart, rangeEnd),
                value.RequireBetween(rangeStart, rangeEnd),
                value.EnsureBetween(rangeStart, rangeEnd));
        }

        [Theory]
        [InlineData("2024-03-15T10:00:00", "2024-03-14T00:00:00", "2024-03-16T00:00:00", true)]
        [InlineData("2024-03-14T00:00:00", "2024-03-14T00:00:00", "2024-03-16T00:00:00", true)]
        [InlineData("2024-03-16T00:00:00", "2024-03-14T00:00:00", "2024-03-16T00:00:00", true)]
        [InlineData("2024-03-17T00:00:00", "2024-03-14T00:00:00", "2024-03-16T00:00:00", false)]
        public void InRangeAppliesTheOutcomeOfEveryStepKind(string valueText, string rangeStartText, string rangeEndText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var rangeStart = DateTime.Parse(rangeStartText, CultureInfo.InvariantCulture);
            var rangeEnd = DateTime.Parse(rangeEndText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInRange(rangeStart, rangeEnd),
                start.RejectInRange(rangeStart, rangeEnd),
                start.BreakInRange(rangeStart, rangeEnd),
                start.RequireInRange(rangeStart, rangeEnd),
                start.EnsureInRange(rangeStart, rangeEnd));

            FlowAssert.Kinds(expected,
                value.MatchInRange(rangeStart, rangeEnd),
                value.RejectInRange(rangeStart, rangeEnd),
                value.BreakInRange(rangeStart, rangeEnd),
                value.RequireInRange(rangeStart, rangeEnd),
                value.EnsureInRange(rangeStart, rangeEnd));
        }

        [Theory]
        [InlineData("2024-03-15T01:00:00", "2024-03-15T23:00:00", true)]
        [InlineData("2024-03-15T23:00:00", "2024-03-16T00:00:00", false)]
        [InlineData("2023-12-31T23:59:59", "2024-01-01T00:00:00", false)]
        [InlineData("2023-03-15T10:00:00", "2024-03-15T10:00:00", false)]
        [InlineData("2024-03-15T00:00:00", "2024-03-15T23:59:59", true)]
        public void SameDayAsAppliesTheOutcomeOfEveryStepKind(string valueText, string otherText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var other = DateTime.Parse(otherText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSameDayAs(other),
                start.RejectSameDayAs(other),
                start.BreakSameDayAs(other),
                start.RequireSameDayAs(other),
                start.EnsureSameDayAs(other));

            FlowAssert.Kinds(expected,
                value.MatchSameDayAs(other),
                value.RejectSameDayAs(other),
                value.BreakSameDayAs(other),
                value.RequireSameDayAs(other),
                value.EnsureSameDayAs(other));
        }

        [Theory]
        [InlineData("2024-03-15T09:00:00", 9, 17, true)]
        [InlineData("2024-03-15T16:59:00", 9, 17, true)]
        [InlineData("2024-03-15T17:00:00", 9, 17, false)]
        [InlineData("2024-03-15T08:59:00", 9, 17, false)]
        public void WithinBusinessHoursAppliesTheOutcomeOfEveryStepKind(string valueText, int startHour, int endHour, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinBusinessHours(startHour, endHour),
                start.RejectWithinBusinessHours(startHour, endHour),
                start.BreakWithinBusinessHours(startHour, endHour),
                start.RequireWithinBusinessHours(startHour, endHour),
                start.EnsureWithinBusinessHours(startHour, endHour));

            FlowAssert.Kinds(expected,
                value.MatchWithinBusinessHours(startHour, endHour),
                value.RejectWithinBusinessHours(startHour, endHour),
                value.BreakWithinBusinessHours(startHour, endHour),
                value.RequireWithinBusinessHours(startHour, endHour),
                value.EnsureWithinBusinessHours(startHour, endHour));
        }

        [Theory]
        [InlineData("2024-03-15T17:00:00", 9, 17, true)]
        [InlineData("2024-03-15T03:00:00", 9, 17, true)]
        [InlineData("2024-03-15T09:00:00", 9, 17, false)]
        public void OutsideBusinessHoursAppliesTheOutcomeOfEveryStepKind(string valueText, int startHour, int endHour, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchOutsideBusinessHours(startHour, endHour),
                start.RejectOutsideBusinessHours(startHour, endHour),
                start.BreakOutsideBusinessHours(startHour, endHour),
                start.RequireOutsideBusinessHours(startHour, endHour),
                start.EnsureOutsideBusinessHours(startHour, endHour));

            FlowAssert.Kinds(expected,
                value.MatchOutsideBusinessHours(startHour, endHour),
                value.RejectOutsideBusinessHours(startHour, endHour),
                value.BreakOutsideBusinessHours(startHour, endHour),
                value.RequireOutsideBusinessHours(startHour, endHour),
                value.EnsureOutsideBusinessHours(startHour, endHour));
        }

        [Theory]
        [InlineData("2024-03-15T10:00:00", "2024-03-15T10:05:00", 5, true)]
        [InlineData("2024-03-15T10:00:00", "2024-03-15T10:06:00", 5, false)]
        [InlineData("2024-03-15T10:06:00", "2024-03-15T10:00:00", 5, false)]
        public void WithinMinutesOfAppliesTheOutcomeOfEveryStepKind(string valueText, string otherText, double minutes, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var other = DateTime.Parse(otherText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinMinutesOf(other, minutes),
                start.RejectWithinMinutesOf(other, minutes),
                start.BreakWithinMinutesOf(other, minutes),
                start.RequireWithinMinutesOf(other, minutes),
                start.EnsureWithinMinutesOf(other, minutes));

            FlowAssert.Kinds(expected,
                value.MatchWithinMinutesOf(other, minutes),
                value.RejectWithinMinutesOf(other, minutes),
                value.BreakWithinMinutesOf(other, minutes),
                value.RequireWithinMinutesOf(other, minutes),
                value.EnsureWithinMinutesOf(other, minutes));
        }

        [Theory]
        [InlineData("2024-03-15T10:00:00", "2024-03-15T12:00:00", 2, true)]
        [InlineData("2024-03-15T10:00:00", "2024-03-15T13:00:00", 2, false)]
        [InlineData("2024-03-15T10:00:00", "2024-03-15T08:00:00", 2, true)]
        [InlineData("2024-03-15T10:00:00", "2024-03-15T07:59:00", 2, false)]
        [InlineData("2024-03-15T10:00:00", "2024-03-15T10:00:00", 0, true)]
        public void WithinHoursOfAppliesTheOutcomeOfEveryStepKind(string valueText, string otherText, double hours, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var other = DateTime.Parse(otherText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinHoursOf(other, hours),
                start.RejectWithinHoursOf(other, hours),
                start.BreakWithinHoursOf(other, hours),
                start.RequireWithinHoursOf(other, hours),
                start.EnsureWithinHoursOf(other, hours));

            FlowAssert.Kinds(expected,
                value.MatchWithinHoursOf(other, hours),
                value.RejectWithinHoursOf(other, hours),
                value.BreakWithinHoursOf(other, hours),
                value.RequireWithinHoursOf(other, hours),
                value.EnsureWithinHoursOf(other, hours));
        }

        [Theory]
        [InlineData("2024-03-15T10:00:00", "2024-03-17T10:00:00", 2, true)]
        [InlineData("2024-03-15T10:00:00", "2024-03-18T10:00:00", 2, false)]
        [InlineData("2024-03-17T10:00:00", "2024-03-15T10:00:00", 2, true)]
        [InlineData("2024-03-17T10:00:01", "2024-03-15T10:00:00", 2, false)]
        public void WithinDaysOfAppliesTheOutcomeOfEveryStepKind(string valueText, string otherText, double days, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var other = DateTime.Parse(otherText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinDaysOf(other, days),
                start.RejectWithinDaysOf(other, days),
                start.BreakWithinDaysOf(other, days),
                start.RequireWithinDaysOf(other, days),
                start.EnsureWithinDaysOf(other, days));

            FlowAssert.Kinds(expected,
                value.MatchWithinDaysOf(other, days),
                value.RejectWithinDaysOf(other, days),
                value.BreakWithinDaysOf(other, days),
                value.RequireWithinDaysOf(other, days),
                value.EnsureWithinDaysOf(other, days));
        }

        [Theory]
        [InlineData("2024-03-15T10:10:00", "2024-03-15T10:00:00", 5, true)]
        [InlineData("2024-03-15T10:05:00", "2024-03-15T10:00:00", 5, false)]
        [InlineData("2024-03-15T09:00:00", "2024-03-15T10:00:00", 5, false)]
        public void MoreThanMinutesAfterAppliesTheOutcomeOfEveryStepKind(string valueText, string otherText, double minutes, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var other = DateTime.Parse(otherText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMoreThanMinutesAfter(other, minutes),
                start.RejectMoreThanMinutesAfter(other, minutes),
                start.BreakMoreThanMinutesAfter(other, minutes),
                start.RequireMoreThanMinutesAfter(other, minutes),
                start.EnsureMoreThanMinutesAfter(other, minutes));

            FlowAssert.Kinds(expected,
                value.MatchMoreThanMinutesAfter(other, minutes),
                value.RejectMoreThanMinutesAfter(other, minutes),
                value.BreakMoreThanMinutesAfter(other, minutes),
                value.RequireMoreThanMinutesAfter(other, minutes),
                value.EnsureMoreThanMinutesAfter(other, minutes));
        }

        [Theory]
        [InlineData("2024-03-20T10:00:00", "2024-03-15T10:00:00", 4, true)]
        [InlineData("2024-03-19T10:00:00", "2024-03-15T10:00:00", 4, false)]
        [InlineData("2024-03-19T10:00:01", "2024-03-15T10:00:00", 4, true)]
        [InlineData("2024-03-10T10:00:00", "2024-03-15T10:00:00", 4, false)]
        public void MoreThanDaysAfterAppliesTheOutcomeOfEveryStepKind(string valueText, string otherText, double days, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var other = DateTime.Parse(otherText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMoreThanDaysAfter(other, days),
                start.RejectMoreThanDaysAfter(other, days),
                start.BreakMoreThanDaysAfter(other, days),
                start.RequireMoreThanDaysAfter(other, days),
                start.EnsureMoreThanDaysAfter(other, days));

            FlowAssert.Kinds(expected,
                value.MatchMoreThanDaysAfter(other, days),
                value.RejectMoreThanDaysAfter(other, days),
                value.BreakMoreThanDaysAfter(other, days),
                value.RequireMoreThanDaysAfter(other, days),
                value.EnsureMoreThanDaysAfter(other, days));
        }

        [Theory]
        [InlineData("2024-03-15T10:00:00", new[] { 5, 6 }, true)]
        [InlineData("2024-03-15T10:00:00", new[] { 0, 1 }, false)]
        [InlineData("2024-03-17T10:00:00", new[] { 0 }, true)]
        public void DayOfWeekInAppliesTheOutcomeOfEveryStepKind(string valueText, int[] days, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDayOfWeekIn(days),
                start.RejectDayOfWeekIn(days),
                start.BreakDayOfWeekIn(days),
                start.RequireDayOfWeekIn(days),
                start.EnsureDayOfWeekIn(days));

            FlowAssert.Kinds(expected,
                value.MatchDayOfWeekIn(days),
                value.RejectDayOfWeekIn(days),
                value.BreakDayOfWeekIn(days),
                value.RequireDayOfWeekIn(days),
                value.EnsureDayOfWeekIn(days));
        }

        [Theory]
        [InlineData("2024-03-01T10:00:00", true)]
        [InlineData("2024-03-02T10:00:00", false)]
        [InlineData("2024-01-01T00:00:00", true)]
        [InlineData("2024-01-31T23:59:59", false)]
        public void IsStartOfMonthAppliesTheOutcomeOfEveryStepKind(string valueText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsStartOfMonth(),
                start.RejectIsStartOfMonth(),
                start.BreakIsStartOfMonth(),
                start.RequireIsStartOfMonth(),
                start.EnsureIsStartOfMonth());

            FlowAssert.Kinds(expected,
                value.MatchIsStartOfMonth(),
                value.RejectIsStartOfMonth(),
                value.BreakIsStartOfMonth(),
                value.RequireIsStartOfMonth(),
                value.EnsureIsStartOfMonth());
        }

        [Theory]
        [InlineData("2024-02-29T10:00:00", true)]
        [InlineData("2024-03-31T10:00:00", true)]
        [InlineData("2024-03-30T10:00:00", false)]
        [InlineData("2023-02-28T10:00:00", true)]
        [InlineData("2024-02-28T10:00:00", false)]
        [InlineData("2024-12-31T00:00:00", true)]
        public void IsEndOfMonthAppliesTheOutcomeOfEveryStepKind(string valueText, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsEndOfMonth(),
                start.RejectIsEndOfMonth(),
                start.BreakIsEndOfMonth(),
                start.RequireIsEndOfMonth(),
                start.EnsureIsEndOfMonth());

            FlowAssert.Kinds(expected,
                value.MatchIsEndOfMonth(),
                value.RejectIsEndOfMonth(),
                value.BreakIsEndOfMonth(),
                value.RequireIsEndOfMonth(),
                value.EnsureIsEndOfMonth());
        }

        [Theory]
        [InlineData("2024-03-15T14:00:00", "America/New_York", 9, 17, true)]
        [InlineData("2024-03-15T02:00:00", "America/New_York", 9, 17, false)]
        [InlineData("2024-03-15T14:00:00", "Asia/Tokyo", 9, 17, false)]
        [InlineData("2024-03-15T14:00:00", "Not/AZone", 9, 17, false)]
        [InlineData("2024-03-15T14:00:00", null, 9, 17, false)]
        public void WithinBusinessHoursInZoneAppliesTheOutcomeOfEveryStepKind(string valueText, string zoneId, int startHour, int endHour, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinBusinessHoursInZone(zoneId, startHour, endHour),
                start.RejectWithinBusinessHoursInZone(zoneId, startHour, endHour),
                start.BreakWithinBusinessHoursInZone(zoneId, startHour, endHour),
                start.RequireWithinBusinessHoursInZone(zoneId, startHour, endHour),
                start.EnsureWithinBusinessHoursInZone(zoneId, startHour, endHour));

            FlowAssert.Kinds(expected,
                value.MatchWithinBusinessHoursInZone(zoneId, startHour, endHour),
                value.RejectWithinBusinessHoursInZone(zoneId, startHour, endHour),
                value.BreakWithinBusinessHoursInZone(zoneId, startHour, endHour),
                value.RequireWithinBusinessHoursInZone(zoneId, startHour, endHour),
                value.EnsureWithinBusinessHoursInZone(zoneId, startHour, endHour));
        }

        [Theory]
        [InlineData("2024-03-15T02:00:00", "America/New_York", 9, 17, true)]
        [InlineData("2024-03-15T14:00:00", "America/New_York", 9, 17, false)]
        [InlineData("2024-03-15T02:00:00", "Not/AZone", 9, 17, false)]
        [InlineData("2024-03-15T02:00:00", null, 9, 17, false)]
        public void OutsideBusinessHoursInZoneAppliesTheOutcomeOfEveryStepKind(string valueText, string zoneId, int startHour, int endHour, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchOutsideBusinessHoursInZone(zoneId, startHour, endHour),
                start.RejectOutsideBusinessHoursInZone(zoneId, startHour, endHour),
                start.BreakOutsideBusinessHoursInZone(zoneId, startHour, endHour),
                start.RequireOutsideBusinessHoursInZone(zoneId, startHour, endHour),
                start.EnsureOutsideBusinessHoursInZone(zoneId, startHour, endHour));

            FlowAssert.Kinds(expected,
                value.MatchOutsideBusinessHoursInZone(zoneId, startHour, endHour),
                value.RejectOutsideBusinessHoursInZone(zoneId, startHour, endHour),
                value.BreakOutsideBusinessHoursInZone(zoneId, startHour, endHour),
                value.RequireOutsideBusinessHoursInZone(zoneId, startHour, endHour),
                value.EnsureOutsideBusinessHoursInZone(zoneId, startHour, endHour));
        }

        [Theory]
        [InlineData("2024-03-15T10:00:00", 9, 17, true)]
        [InlineData("2024-03-16T10:00:00", 9, 17, false)]
        [InlineData("2024-03-15T18:00:00", 9, 17, false)]
        public void WithinWeekdayBusinessHoursAppliesTheOutcomeOfEveryStepKind(string valueText, int startHour, int endHour, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinWeekdayBusinessHours(startHour, endHour),
                start.RejectWithinWeekdayBusinessHours(startHour, endHour),
                start.BreakWithinWeekdayBusinessHours(startHour, endHour),
                start.RequireWithinWeekdayBusinessHours(startHour, endHour),
                start.EnsureWithinWeekdayBusinessHours(startHour, endHour));

            FlowAssert.Kinds(expected,
                value.MatchWithinWeekdayBusinessHours(startHour, endHour),
                value.RejectWithinWeekdayBusinessHours(startHour, endHour),
                value.BreakWithinWeekdayBusinessHours(startHour, endHour),
                value.RequireWithinWeekdayBusinessHours(startHour, endHour),
                value.EnsureWithinWeekdayBusinessHours(startHour, endHour));
        }

        [Theory]
        [InlineData("2024-03-16T10:00:00", 9, 17, true)]
        [InlineData("2024-03-15T18:00:00", 9, 17, true)]
        [InlineData("2024-03-15T10:00:00", 9, 17, false)]
        public void OutsideWeekdayBusinessHoursAppliesTheOutcomeOfEveryStepKind(string valueText, int startHour, int endHour, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchOutsideWeekdayBusinessHours(startHour, endHour),
                start.RejectOutsideWeekdayBusinessHours(startHour, endHour),
                start.BreakOutsideWeekdayBusinessHours(startHour, endHour),
                start.RequireOutsideWeekdayBusinessHours(startHour, endHour),
                start.EnsureOutsideWeekdayBusinessHours(startHour, endHour));

            FlowAssert.Kinds(expected,
                value.MatchOutsideWeekdayBusinessHours(startHour, endHour),
                value.RejectOutsideWeekdayBusinessHours(startHour, endHour),
                value.BreakOutsideWeekdayBusinessHours(startHour, endHour),
                value.RequireOutsideWeekdayBusinessHours(startHour, endHour),
                value.EnsureOutsideWeekdayBusinessHours(startHour, endHour));
        }

        [Theory]
        [InlineData("2024-03-15T14:00:00", "America/New_York", 9, 17, true)]
        [InlineData("2024-03-16T14:00:00", "America/New_York", 9, 17, false)]
        [InlineData("2024-03-15T14:00:00", "Asia/Tokyo", 9, 17, false)]
        [InlineData("2024-03-18T00:30:00", "America/New_York", 9, 17, false)]
        [InlineData("2024-03-15T14:00:00", "Not/AZone", 9, 17, false)]
        public void WithinWeekdayBusinessHoursInZoneAppliesTheOutcomeOfEveryStepKind(string valueText, string zoneId, int startHour, int endHour, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                start.RejectWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                start.BreakWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                start.RequireWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                start.EnsureWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour));

            FlowAssert.Kinds(expected,
                value.MatchWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                value.RejectWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                value.BreakWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                value.RequireWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                value.EnsureWithinWeekdayBusinessHoursInZone(zoneId, startHour, endHour));
        }

        [Theory]
        [InlineData("2024-03-16T14:00:00", "America/New_York", 9, 17, true)]
        [InlineData("2024-03-15T02:00:00", "America/New_York", 9, 17, true)]
        [InlineData("2024-03-15T14:00:00", "America/New_York", 9, 17, false)]
        [InlineData("2024-03-15T14:00:00", "Not/AZone", 9, 17, false)]
        public void OutsideWeekdayBusinessHoursInZoneAppliesTheOutcomeOfEveryStepKind(string valueText, string zoneId, int startHour, int endHour, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                start.RejectOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                start.BreakOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                start.RequireOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                start.EnsureOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour));

            FlowAssert.Kinds(expected,
                value.MatchOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                value.RejectOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                value.BreakOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                value.RequireOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour),
                value.EnsureOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour));
        }

        [Theory]
        [InlineData("2024-03-15T14:00:00", "America/New_York", true)]
        [InlineData("2024-03-16T14:00:00", "America/New_York", false)]
        [InlineData("2024-03-18T02:00:00", "America/New_York", false)]
        [InlineData("2024-03-15T14:00:00", "Not/AZone", false)]
        public void IsWeekdayInZoneAppliesTheOutcomeOfEveryStepKind(string valueText, string zoneId, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsWeekdayInZone(zoneId),
                start.RejectIsWeekdayInZone(zoneId),
                start.BreakIsWeekdayInZone(zoneId),
                start.RequireIsWeekdayInZone(zoneId),
                start.EnsureIsWeekdayInZone(zoneId));

            FlowAssert.Kinds(expected,
                value.MatchIsWeekdayInZone(zoneId),
                value.RejectIsWeekdayInZone(zoneId),
                value.BreakIsWeekdayInZone(zoneId),
                value.RequireIsWeekdayInZone(zoneId),
                value.EnsureIsWeekdayInZone(zoneId));
        }

        [Theory]
        [InlineData("2024-03-16T14:00:00", "America/New_York", true)]
        [InlineData("2024-03-18T02:00:00", "America/New_York", true)]
        [InlineData("2024-03-15T14:00:00", "America/New_York", false)]
        [InlineData("2024-03-16T14:00:00", "Not/AZone", false)]
        public void IsWeekendInZoneAppliesTheOutcomeOfEveryStepKind(string valueText, string zoneId, bool expected)
        {
            var value = DateTime.Parse(valueText, CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsWeekendInZone(zoneId),
                start.RejectIsWeekendInZone(zoneId),
                start.BreakIsWeekendInZone(zoneId),
                start.RequireIsWeekendInZone(zoneId),
                start.EnsureIsWeekendInZone(zoneId));

            FlowAssert.Kinds(expected,
                value.MatchIsWeekendInZone(zoneId),
                value.RejectIsWeekendInZone(zoneId),
                value.BreakIsWeekendInZone(zoneId),
                value.RequireIsWeekendInZone(zoneId),
                value.EnsureIsWeekendInZone(zoneId));
        }
    }
}
