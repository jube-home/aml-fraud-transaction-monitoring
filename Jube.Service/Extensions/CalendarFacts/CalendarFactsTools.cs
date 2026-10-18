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

using System.ComponentModel;
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Extensions.CalendarFacts.Models;

namespace Jube.Service.Extensions.CalendarFacts
{
    [FunctionToolGroup("CalendarFacts",
        "Calendar facts about a date: weekday, week number, quarter, fiscal period, month length, leap years, business hours and the start and end of its day, week, month, quarter or year.")]
    public static class CalendarFactsTools
    {
        [FunctionTool("DescribeDate")]
        [Description(
            "Calendar facts about one date in a single call: weekday name, day of year, ISO week number, calendar quarter, days in the month, whether it is a weekend, the last day of its month, and whether its year is a leap year. Use it instead of working out weekdays or week numbers by hand.")]
        public static DateFacts DescribeDate(
            [Description("The date to describe, in ISO 8601 form such as 2026-09-23.")]
            DateTime date)
        {
            return new DateFacts(
                date.Date,
                date.DayOfWeek.ToString(),
                date.DayOfYear,
                date.IsoWeekOfYear(),
                date.QuarterOfYear(),
                DateTime.DaysInMonth(date.Year, date.Month),
                date.IsWeekendDay(),
                date.IsLastDayOfMonth(),
                DateTime.IsLeapYear(date.Year));
        }

        [FunctionTool("PeriodBoundaries")]
        [Description(
            "First and last instant of the day, week, month, quarter or year containing a date; the week can start on any weekday. End is the last tick of the period, so an instant is inside when Start <= instant <= End.")]
        public static PeriodRange PeriodBoundaries(
            [Description("A date inside the period, in ISO 8601 form.")]
            DateTime date,
            [Description("The period kind: Day, Week, Month, Quarter or Year.")]
            string period,
            [Description("For Week only, the weekday a week starts on, such as Monday (default) or Sunday.")]
            string weekStartsOn = "Monday")
        {
            return FunctionToolInput.Choice<CalendarPeriod>(period, nameof(period)) switch
            {
                CalendarPeriod.Day => Range(date.StartOfDay(), date.StartOfDay().AddDays(1)),
                CalendarPeriod.Week => WeekRange(date,
                    FunctionToolInput.Choice<DayOfWeek>(weekStartsOn, nameof(weekStartsOn))),
                CalendarPeriod.Month => Range(date.StartOfMonth(), date.StartOfMonth().AddMonths(1)),
                CalendarPeriod.Quarter => Range(date.StartOfQuarter(), date.StartOfQuarter().AddMonths(3)),
                _ => Range(date.StartOfYear(), date.StartOfYear().AddYears(1))
            };
        }

        [FunctionTool("FiscalPeriod")]
        [Description(
            "Fiscal year and fiscal quarter of a date when the fiscal year starts in a given month. The fiscal year is named after the calendar year in which it ends; with an April start, 2026-08-17 is fiscal 2027 quarter 2.")]
        public static FiscalPeriodDetails FiscalPeriod(
            [Description("The date to place, in ISO 8601 form.")]
            DateTime date,
            [Description("Month number, 1 to 12, in which the fiscal year starts; 1 means the calendar year.")]
            int fiscalYearStartMonth)
        {
            if (fiscalYearStartMonth is < 1 or > 12)
            {
                throw new ArgumentException("fiscalYearStartMonth must be from 1 to 12.", nameof(fiscalYearStartMonth));
            }

            return new FiscalPeriodDetails(date.FiscalYear(fiscalYearStartMonth),
                date.FiscalQuarter(fiscalYearStartMonth));
        }

        [FunctionTool("DaysInMonth")]
        [Description("Number of days in a given month of a given year, allowing for leap years; February 2028 has 29.")]
        public static int DaysInMonth(
            [Description("The four-digit year.")] int year,
            [Description("The month number, 1 to 12.")]
            int month)
        {
            if (year is < 1 or > 9999 || month is < 1 or > 12)
            {
                throw new ArgumentException("year must be 1 to 9999 and month 1 to 12.");
            }

            return DateTime.DaysInMonth(year, month);
        }

        [FunctionTool("IsLeapYear")]
        [Description(
            "Whether a year is a Gregorian leap year: divisible by 4, except centuries not divisible by 400; 2000 and 2028 are, 1900 and 2026 are not.")]
        public static bool IsLeapYear([Description("The four-digit year, 1 to 9999.")] int year)
        {
            if (year is < 1 or > 9999)
            {
                throw new ArgumentException("year must be from 1 to 9999.", nameof(year));
            }

            return year.IsLeapYear();
        }

        [FunctionTool("IsWithinBusinessHours")]
        [Description(
            "Whether the clock time of a date-time falls in business hours, from startHour inclusive to endHour exclusive, optionally on weekdays only. Convert to the customer's local time zone first when that matters.")]
        public static bool IsWithinBusinessHours(
            [Description("The local date-time to test, in ISO 8601 form.")]
            DateTime dateTime,
            [Description("First business hour of the day, 0 to 23, such as 9.")]
            int startHour = 9,
            [Description("Hour business ends, 1 to 24, exclusive, such as 17.")]
            int endHour = 17,
            [Description("True to treat Saturday and Sunday as outside business hours.")]
            bool weekdaysOnly = true)
        {
            if (startHour is < 0 or > 23 || endHour is < 1 or > 24 || endHour <= startHour)
            {
                throw new ArgumentException("Use 0 <= startHour < endHour <= 24.");
            }

            return (!weekdaysOnly || dateTime.IsWeekDay()) && dateTime.IsWithinBusinessHours(startHour, endHour);
        }

        [FunctionTool("IsSameCalendarDay")]
        [Description(
            "Whether two date-times fall on the same calendar date, ignoring the time of day. Convert both to the same time zone first.")]
        public static bool IsSameCalendarDay(
            [Description("The first date-time.")] DateTime first,
            [Description("The second date-time.")] DateTime second)
        {
            return first.IsSameDayAs(second);
        }

        private static PeriodRange WeekRange(DateTime date, DayOfWeek start)
        {
            var first = date.StartOfWeek(start);

            return Range(first, first.AddDays(7));
        }

        private static PeriodRange Range(DateTime start, DateTime nextStart)
        {
            return new PeriodRange(start, nextStart.AddTicks(-1));
        }
    }
}