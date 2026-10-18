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
using Jube.Service.Extensions.DateArithmetic.Models;

namespace Jube.Service.Extensions.DateArithmetic
{
    [FunctionToolGroup("DateArithmetic",
        "Adds time to dates and measures the time between them exactly, including business days, whole months and years, ages as of a date, and interval overlap.")]
    public static class DateArithmeticTools
    {
        [FunctionTool("AddToDate")]
        [Description(
            "Adds (or with a negative amount subtracts) a number of units to a date-time. Months and Years keep the day where possible and clamp to month end, so 2026-01-31 plus 1 month is 2026-02-28.")]
        public static DateTime AddToDate(
            [Description("The starting date-time, in ISO 8601 form.")]
            DateTime date,
            [Description("How many units to add; negative subtracts. Months and Years must be whole numbers.")]
            double amount,
            [Description("The unit: Seconds, Minutes, Hours, Days, Weeks, Months or Years.")]
            string unit)
        {
            FunctionToolInput.Finite(amount, nameof(amount));
            var chosen = FunctionToolInput.Choice<DateUnit>(unit, nameof(unit));

            if (chosen is DateUnit.Months or DateUnit.Years &&
                (amount != Math.Floor(amount) || Math.Abs(amount) > 120000))
            {
                throw new ArgumentException("Months and Years must be whole numbers within range.", nameof(amount));
            }

            try
            {
                return chosen switch
                {
                    DateUnit.Seconds => date.AddSeconds(amount),
                    DateUnit.Minutes => date.AddMinutes(amount),
                    DateUnit.Hours => date.AddHours(amount),
                    DateUnit.Days => date.AddDays(amount),
                    DateUnit.Weeks => date.AddDays(amount * 7),
                    DateUnit.Months => date.AddMonths((int)amount),
                    _ => date.AddYears((int)amount)
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException("The result is outside the years 1 to 9999.", nameof(amount));
            }
        }

        [FunctionTool("AddBusinessDays")]
        [Description(
            "Moves a date forward (or backward, if negative) by a number of Monday-to-Friday business days, skipping weekends; Friday plus 1 is Monday. Public holidays are not known and are not skipped.")]
        public static DateTime AddBusinessDays(
            [Description("The starting date, in ISO 8601 form.")]
            DateTime date,
            [Description("Business days to move; negative moves backward.")]
            int businessDays)
        {
            if (Math.Abs((long)businessDays) > 2_000_000)
            {
                throw new ArgumentException("businessDays is out of range.", nameof(businessDays));
            }

            try
            {
                return date.AddBusinessDays(businessDays);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException("The result is outside the years 1 to 9999.", nameof(businessDays));
            }
        }

        [FunctionTool("BusinessDaysBetween")]
        [Description(
            "Number of Monday-to-Friday days after the start date up to and including the end date; negative when the end is earlier. Wednesday to the following Wednesday is 5. Public holidays are not known.")]
        public static int BusinessDaysBetween(
            [Description("The start date, in ISO 8601 form.")]
            DateTime start,
            [Description("The end date, in ISO 8601 form.")]
            DateTime end)
        {
            return start.BusinessDaysUntil(end);
        }

        [FunctionTool("TimeBetween")]
        [Description(
            "Exact elapsed time from a start to an end date-time in several units at once: total days, hours, minutes and seconds (fractional), plus completed whole months and whole years. Values are negative when the end is earlier.")]
        public static ElapsedTime TimeBetween(
            [Description("The start date-time, in ISO 8601 form.")]
            DateTime start,
            [Description("The end date-time, in ISO 8601 form.")]
            DateTime end)
        {
            return new ElapsedTime(
                start.DaysBetween(end),
                start.HoursBetween(end),
                start.MinutesBetween(end),
                start.SecondsBetween(end),
                start.WholeMonthsBetween(end),
                start.WholeYearsBetween(end));
        }

        [FunctionTool("AgeAsOf")]
        [Description(
            "Age in completed years and in days of someone or something born on a date, measured at an as-of date; born 1990-06-15 is 35 on 2026-06-14 and 36 the next day. Use CurrentUtcTime for today's date.")]
        public static AgeDetails AgeAsOf(
            [Description("The birth or creation date, in ISO 8601 form.")]
            DateTime bornOn,
            [Description("The date to measure the age at, in ISO 8601 form.")]
            DateTime asOf)
        {
            return new AgeDetails(bornOn.AgeAsOf(asOf), bornOn.AgeInDaysAsOf(asOf));
        }

        [FunctionTool("IntervalsOverlap")]
        [Description(
            "Whether two time intervals overlap. Intervals are half-open (start included, end excluded), so one ending exactly when the other starts does not overlap.")]
        public static bool IntervalsOverlap(
            [Description("Start of the first interval.")]
            DateTime firstStart,
            [Description("End of the first interval; must not be before its start.")]
            DateTime firstEnd,
            [Description("Start of the second interval.")]
            DateTime secondStart,
            [Description("End of the second interval; must not be before its start.")]
            DateTime secondEnd)
        {
            if (firstEnd < firstStart || secondEnd < secondStart)
            {
                throw new ArgumentException("An interval ends before it starts.");
            }

            return firstStart.IntervalOverlaps(firstEnd, secondStart, secondEnd);
        }

        [FunctionTool("IsDateBetween")]
        [Description(
            "Whether a date-time lies within an inclusive range from a start to an end. Use it for effective dates, statement periods and lookback windows.")]
        public static bool IsDateBetween(
            [Description("The date-time to test.")]
            DateTime date,
            [Description("The first instant of the range.")]
            DateTime start,
            [Description("The last instant of the range.")]
            DateTime end)
        {
            return date.InRange(start, end);
        }
    }
}