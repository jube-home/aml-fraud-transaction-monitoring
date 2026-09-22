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
            public Flow<DateTime> MatchIsWeekend()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsWeekend(flow.Value));
            }

            public Flow<DateTime> RejectIsWeekend()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsWeekend(flow.Value));
            }

            public Flow<DateTime> BreakIsWeekend()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsWeekend(flow.Value));
            }

            public Flow<DateTime> RequireIsWeekend()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsWeekend(flow.Value));
            }

            public Flow<DateTime> EnsureIsWeekend()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsWeekend(flow.Value));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchIsWeekend()
            {
                return value.Start().MatchIsWeekend();
            }

            public Flow<DateTime> RejectIsWeekend()
            {
                return value.Start().RejectIsWeekend();
            }

            public Flow<DateTime> BreakIsWeekend()
            {
                return value.Start().BreakIsWeekend();
            }

            public Flow<DateTime> RequireIsWeekend()
            {
                return value.Start().RequireIsWeekend();
            }

            public Flow<DateTime> EnsureIsWeekend()
            {
                return value.Start().EnsureIsWeekend();
            }
        }
    }
}
