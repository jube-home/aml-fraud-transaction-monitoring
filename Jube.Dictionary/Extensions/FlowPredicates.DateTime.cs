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

namespace Jube.Dictionary.Extensions
{
    internal static partial class FlowPredicates
    {
        internal static bool DateTimeIsPast(DateTime v)
        {
            return v.IsPast();
        }

        internal static bool DateTimeIsFuture(DateTime v)
        {
            return v.IsFuture();
        }

        internal static bool DateTimeIsToday(DateTime v)
        {
            return v.IsToday();
        }

        internal static bool DateTimeIsWeekday(DateTime v)
        {
            return v.IsWeekDay();
        }

        internal static bool DateTimeIsWeekend(DateTime v)
        {
            return v.IsWeekendDay();
        }

        internal static bool DateTimeIsMorning(DateTime v)
        {
            return v.IsMorning();
        }

        internal static bool DateTimeIsAfternoon(DateTime v)
        {
            return v.IsAfternoon();
        }

        internal static bool DateTimeBefore(DateTime v, DateTime other)
        {
            return v < other;
        }

        internal static bool DateTimeAfter(DateTime v, DateTime other)
        {
            return v > other;
        }

        internal static bool DateTimeBetween(DateTime v, DateTime rangeStart, DateTime rangeEnd)
        {
            return v.Between(rangeStart, rangeEnd);
        }

        internal static bool DateTimeInRange(DateTime v, DateTime rangeStart, DateTime rangeEnd)
        {
            return v.InRange(rangeStart, rangeEnd);
        }

        internal static bool DateTimeSameDayAs(DateTime v, DateTime other)
        {
            return v.Date == other.Date;
        }

        internal static bool DateTimeWithinBusinessHours(DateTime v, int startHour, int endHour)
        {
            return v.IsWithinBusinessHours(startHour, endHour);
        }

        internal static bool DateTimeOutsideBusinessHours(DateTime v, int startHour, int endHour)
        {
            return !v.IsWithinBusinessHours(startHour, endHour);
        }

        internal static bool DateTimeOlderThanDays(DateTime v, int days)
        {
            return v.AgeInDays() > days;
        }

        internal static bool DateTimeYoungerThanDays(DateTime v, int days)
        {
            return v.AgeInDays() < days;
        }

        internal static bool DateTimeWithinMinutesOf(DateTime v, DateTime other, double minutes)
        {
            return Math.Abs((v - other).TotalMinutes) <= minutes;
        }

        internal static bool DateTimeWithinHoursOf(DateTime v, DateTime other, double hours)
        {
            return Math.Abs((v - other).TotalHours) <= hours;
        }

        internal static bool DateTimeWithinDaysOf(DateTime v, DateTime other, double days)
        {
            return Math.Abs((v - other).TotalDays) <= days;
        }

        internal static bool DateTimeMoreThanMinutesAfter(DateTime v, DateTime other, double minutes)
        {
            return (v - other).TotalMinutes > minutes;
        }

        internal static bool DateTimeMoreThanDaysAfter(DateTime v, DateTime other, double days)
        {
            return (v - other).TotalDays > days;
        }

        internal static bool DateTimeDayOfWeekIn(DateTime v, int[] days)
        {
            return days.Contains((int)v.DayOfWeek);
        }

        internal static bool DateTimeIsStartOfMonth(DateTime v)
        {
            return v.Day == 1;
        }

        internal static bool DateTimeIsEndOfMonth(DateTime v)
        {
            return v.Day == DateTime.DaysInMonth(v.Year, v.Month);
        }

        internal static bool DateTimeWithinBusinessHoursInZone(DateTime v, string zoneId, int startHour, int endHour)
        {
            return FlowPredicates.LocalHourWithin(v, zoneId, startHour, endHour, false);
        }

        internal static bool DateTimeOutsideBusinessHoursInZone(DateTime v, string zoneId, int startHour, int endHour)
        {
            return FlowPredicates.LocalHourOutside(v, zoneId, startHour, endHour, false);
        }

        internal static bool DateTimeWithinWeekdayBusinessHours(DateTime v, int startHour, int endHour)
        {
            return v.IsWeekDay() && v.IsWithinBusinessHours(startHour, endHour);
        }

        internal static bool DateTimeOutsideWeekdayBusinessHours(DateTime v, int startHour, int endHour)
        {
            return !(v.IsWeekDay() && v.IsWithinBusinessHours(startHour, endHour));
        }

        internal static bool DateTimeWithinWeekdayBusinessHoursInZone(DateTime v, string zoneId, int startHour, int endHour)
        {
            return FlowPredicates.LocalHourWithin(v, zoneId, startHour, endHour, true);
        }

        internal static bool DateTimeOutsideWeekdayBusinessHoursInZone(DateTime v, string zoneId, int startHour, int endHour)
        {
            return FlowPredicates.LocalHourOutside(v, zoneId, startHour, endHour, true);
        }

        internal static bool DateTimeIsWeekdayInZone(DateTime v, string zoneId)
        {
            return FlowPredicates.LocalWeekday(v, zoneId) == true;
        }

        internal static bool DateTimeIsWeekendInZone(DateTime v, string zoneId)
        {
            return FlowPredicates.LocalWeekday(v, zoneId) == false;
        }
    }
}
