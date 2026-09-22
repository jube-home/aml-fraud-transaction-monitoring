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
            public Flow<DateTime> MatchIsWeekday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsWeekday(flow.Value));
            }

            public Flow<DateTime> RejectIsWeekday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsWeekday(flow.Value));
            }

            public Flow<DateTime> BreakIsWeekday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsWeekday(flow.Value));
            }

            public Flow<DateTime> RequireIsWeekday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsWeekday(flow.Value));
            }

            public Flow<DateTime> EnsureIsWeekday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsWeekday(flow.Value));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchIsWeekday()
            {
                return value.Start().MatchIsWeekday();
            }

            public Flow<DateTime> RejectIsWeekday()
            {
                return value.Start().RejectIsWeekday();
            }

            public Flow<DateTime> BreakIsWeekday()
            {
                return value.Start().BreakIsWeekday();
            }

            public Flow<DateTime> RequireIsWeekday()
            {
                return value.Start().RequireIsWeekday();
            }

            public Flow<DateTime> EnsureIsWeekday()
            {
                return value.Start().EnsureIsWeekday();
            }
        }
    }
}
