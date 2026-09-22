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
            public Flow<DateTime> MatchWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            public Flow<DateTime> RejectWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            public Flow<DateTime> BreakWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            public Flow<DateTime> RequireWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            public Flow<DateTime> EnsureWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().MatchWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }

            public Flow<DateTime> RejectWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().RejectWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }

            public Flow<DateTime> BreakWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().BreakWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }

            public Flow<DateTime> RequireWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().RequireWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }

            public Flow<DateTime> EnsureWithinBusinessHoursInZone(string zoneId, int startHour, int endHour)
            {
                return value.Start().EnsureWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }
        }
    }
}
