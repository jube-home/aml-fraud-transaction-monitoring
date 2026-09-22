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
    public static partial class Extensions
    {
        extension(Flow<DateTime> flow)
        {
            public Flow<DateTime> MatchOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            public Flow<DateTime> RejectOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            public Flow<DateTime> BreakOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            public Flow<DateTime> RequireOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            public Flow<DateTime> EnsureOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().MatchOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }

            public Flow<DateTime> RejectOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().RejectOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }

            public Flow<DateTime> BreakOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().BreakOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }

            public Flow<DateTime> RequireOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().RequireOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }

            public Flow<DateTime> EnsureOutsideWeekdayBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().EnsureOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }
        }
    }
}
