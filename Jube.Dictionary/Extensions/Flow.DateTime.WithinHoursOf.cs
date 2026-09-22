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
            public Flow<DateTime> MatchWithinHoursOf(DateTime other, double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }

            public Flow<DateTime> RejectWithinHoursOf(DateTime other, double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }

            public Flow<DateTime> BreakWithinHoursOf(DateTime other, double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }

            public Flow<DateTime> RequireWithinHoursOf(DateTime other, double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }

            public Flow<DateTime> EnsureWithinHoursOf(DateTime other, double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchWithinHoursOf(DateTime other, double hours)
            {
                return value.Start().MatchWithinHoursOf(other, hours);
            }

            public Flow<DateTime> RejectWithinHoursOf(DateTime other, double hours)
            {
                return value.Start().RejectWithinHoursOf(other, hours);
            }

            public Flow<DateTime> BreakWithinHoursOf(DateTime other, double hours)
            {
                return value.Start().BreakWithinHoursOf(other, hours);
            }

            public Flow<DateTime> RequireWithinHoursOf(DateTime other, double hours)
            {
                return value.Start().RequireWithinHoursOf(other, hours);
            }

            public Flow<DateTime> EnsureWithinHoursOf(DateTime other, double hours)
            {
                return value.Start().EnsureWithinHoursOf(other, hours);
            }
        }
    }
}
