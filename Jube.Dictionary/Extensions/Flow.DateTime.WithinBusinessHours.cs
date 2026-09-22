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
            public Flow<DateTime> MatchWithinBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }

            public Flow<DateTime> RejectWithinBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }

            public Flow<DateTime> BreakWithinBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }

            public Flow<DateTime> RequireWithinBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }

            public Flow<DateTime> EnsureWithinBusinessHours(int startHour, int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchWithinBusinessHours(int startHour, int endHour)
            {
                return value.Start().MatchWithinBusinessHours(startHour, endHour);
            }

            public Flow<DateTime> RejectWithinBusinessHours(int startHour, int endHour)
            {
                return value.Start().RejectWithinBusinessHours(startHour, endHour);
            }

            public Flow<DateTime> BreakWithinBusinessHours(int startHour, int endHour)
            {
                return value.Start().BreakWithinBusinessHours(startHour, endHour);
            }

            public Flow<DateTime> RequireWithinBusinessHours(int startHour, int endHour)
            {
                return value.Start().RequireWithinBusinessHours(startHour, endHour);
            }

            public Flow<DateTime> EnsureWithinBusinessHours(int startHour, int endHour)
            {
                return value.Start().EnsureWithinBusinessHours(startHour, endHour);
            }
        }
    }
}
