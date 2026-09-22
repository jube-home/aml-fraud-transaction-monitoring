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
            public Flow<DateTime> MatchInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeInRange(flow.Value, rangeStart, rangeEnd));
            }

            public Flow<DateTime> RejectInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeInRange(flow.Value, rangeStart, rangeEnd));
            }

            public Flow<DateTime> BreakInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeInRange(flow.Value, rangeStart, rangeEnd));
            }

            public Flow<DateTime> RequireInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeInRange(flow.Value, rangeStart, rangeEnd));
            }

            public Flow<DateTime> EnsureInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeInRange(flow.Value, rangeStart, rangeEnd));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().MatchInRange(rangeStart, rangeEnd);
            }

            public Flow<DateTime> RejectInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().RejectInRange(rangeStart, rangeEnd);
            }

            public Flow<DateTime> BreakInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().BreakInRange(rangeStart, rangeEnd);
            }

            public Flow<DateTime> RequireInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().RequireInRange(rangeStart, rangeEnd);
            }

            public Flow<DateTime> EnsureInRange(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().EnsureInRange(rangeStart, rangeEnd);
            }
        }
    }
}
