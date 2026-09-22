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
            public Flow<DateTime> MatchIsStartOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsStartOfMonth(flow.Value));
            }

            public Flow<DateTime> RejectIsStartOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsStartOfMonth(flow.Value));
            }

            public Flow<DateTime> BreakIsStartOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsStartOfMonth(flow.Value));
            }

            public Flow<DateTime> RequireIsStartOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsStartOfMonth(flow.Value));
            }

            public Flow<DateTime> EnsureIsStartOfMonth()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsStartOfMonth(flow.Value));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchIsStartOfMonth()
            {
                return value.Start().MatchIsStartOfMonth();
            }

            public Flow<DateTime> RejectIsStartOfMonth()
            {
                return value.Start().RejectIsStartOfMonth();
            }

            public Flow<DateTime> BreakIsStartOfMonth()
            {
                return value.Start().BreakIsStartOfMonth();
            }

            public Flow<DateTime> RequireIsStartOfMonth()
            {
                return value.Start().RequireIsStartOfMonth();
            }

            public Flow<DateTime> EnsureIsStartOfMonth()
            {
                return value.Start().EnsureIsStartOfMonth();
            }
        }
    }
}
