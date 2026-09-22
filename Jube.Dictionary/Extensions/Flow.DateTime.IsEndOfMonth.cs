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
            public Flow<DateTime> MatchIsEndOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsEndOfMonth(flow.Value));
            }

            public Flow<DateTime> RejectIsEndOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsEndOfMonth(flow.Value));
            }

            public Flow<DateTime> BreakIsEndOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsEndOfMonth(flow.Value));
            }

            public Flow<DateTime> RequireIsEndOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsEndOfMonth(flow.Value));
            }

            public Flow<DateTime> EnsureIsEndOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsEndOfMonth(flow.Value));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchIsEndOfMonth()
            {
                return value.Start().MatchIsEndOfMonth();
            }

            public Flow<DateTime> RejectIsEndOfMonth()
            {
                return value.Start().RejectIsEndOfMonth();
            }

            public Flow<DateTime> BreakIsEndOfMonth()
            {
                return value.Start().BreakIsEndOfMonth();
            }

            public Flow<DateTime> RequireIsEndOfMonth()
            {
                return value.Start().RequireIsEndOfMonth();
            }

            public Flow<DateTime> EnsureIsEndOfMonth()
            {
                return value.Start().EnsureIsEndOfMonth();
            }
        }
    }
}
