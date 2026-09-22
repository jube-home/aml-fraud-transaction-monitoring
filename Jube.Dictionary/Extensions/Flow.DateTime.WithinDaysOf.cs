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
            public Flow<DateTime> MatchWithinDaysOf(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeWithinDaysOf(flow.Value, other, days));
            }

            public Flow<DateTime> RejectWithinDaysOf(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeWithinDaysOf(flow.Value, other, days));
            }

            public Flow<DateTime> BreakWithinDaysOf(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeWithinDaysOf(flow.Value, other, days));
            }

            public Flow<DateTime> RequireWithinDaysOf(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeWithinDaysOf(flow.Value, other, days));
            }

            public Flow<DateTime> EnsureWithinDaysOf(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeWithinDaysOf(flow.Value, other, days));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchWithinDaysOf(DateTime other, double days)
            {
                return value.Start().MatchWithinDaysOf(other, days);
            }

            public Flow<DateTime> RejectWithinDaysOf(DateTime other, double days)
            {
                return value.Start().RejectWithinDaysOf(other, days);
            }

            public Flow<DateTime> BreakWithinDaysOf(DateTime other, double days)
            {
                return value.Start().BreakWithinDaysOf(other, days);
            }

            public Flow<DateTime> RequireWithinDaysOf(DateTime other, double days)
            {
                return value.Start().RequireWithinDaysOf(other, days);
            }

            public Flow<DateTime> EnsureWithinDaysOf(DateTime other, double days)
            {
                return value.Start().EnsureWithinDaysOf(other, days);
            }
        }
    }
}
