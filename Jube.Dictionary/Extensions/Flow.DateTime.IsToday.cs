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
            public Flow<DateTime> MatchIsToday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsToday(flow.Value));
            }

            public Flow<DateTime> RejectIsToday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsToday(flow.Value));
            }

            public Flow<DateTime> BreakIsToday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsToday(flow.Value));
            }

            public Flow<DateTime> RequireIsToday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsToday(flow.Value));
            }

            public Flow<DateTime> EnsureIsToday()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsToday(flow.Value));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchIsToday()
            {
                return value.Start().MatchIsToday();
            }

            public Flow<DateTime> RejectIsToday()
            {
                return value.Start().RejectIsToday();
            }

            public Flow<DateTime> BreakIsToday()
            {
                return value.Start().BreakIsToday();
            }

            public Flow<DateTime> RequireIsToday()
            {
                return value.Start().RequireIsToday();
            }

            public Flow<DateTime> EnsureIsToday()
            {
                return value.Start().EnsureIsToday();
            }
        }
    }
}
