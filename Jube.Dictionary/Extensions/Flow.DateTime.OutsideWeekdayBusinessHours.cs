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
            public Flow<DateTime> MatchOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeOutsideWeekdayBusinessHours(flow.Value, startHour, endHour));
            }

            public Flow<DateTime> RejectOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeOutsideWeekdayBusinessHours(flow.Value, startHour, endHour));
            }

            public Flow<DateTime> BreakOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeOutsideWeekdayBusinessHours(flow.Value, startHour, endHour));
            }

            public Flow<DateTime> RequireOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeOutsideWeekdayBusinessHours(flow.Value, startHour, endHour));
            }

            public Flow<DateTime> EnsureOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeOutsideWeekdayBusinessHours(flow.Value, startHour, endHour));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return value.Start().MatchOutsideWeekdayBusinessHours(startHour, endHour);
            }

            public Flow<DateTime> RejectOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return value.Start().RejectOutsideWeekdayBusinessHours(startHour, endHour);
            }

            public Flow<DateTime> BreakOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return value.Start().BreakOutsideWeekdayBusinessHours(startHour, endHour);
            }

            public Flow<DateTime> RequireOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return value.Start().RequireOutsideWeekdayBusinessHours(startHour, endHour);
            }

            public Flow<DateTime> EnsureOutsideWeekdayBusinessHours(int startHour, int endHour)
            {
                return value.Start().EnsureOutsideWeekdayBusinessHours(startHour, endHour);
            }
        }
    }
}
