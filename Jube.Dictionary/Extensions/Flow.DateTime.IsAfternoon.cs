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
            public Flow<DateTime> MatchIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }

            public Flow<DateTime> RejectIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }

            public Flow<DateTime> BreakIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }

            public Flow<DateTime> RequireIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }

            public Flow<DateTime> EnsureIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchIsAfternoon()
            {
                return value.Start().MatchIsAfternoon();
            }

            public Flow<DateTime> RejectIsAfternoon()
            {
                return value.Start().RejectIsAfternoon();
            }

            public Flow<DateTime> BreakIsAfternoon()
            {
                return value.Start().BreakIsAfternoon();
            }

            public Flow<DateTime> RequireIsAfternoon()
            {
                return value.Start().RequireIsAfternoon();
            }

            public Flow<DateTime> EnsureIsAfternoon()
            {
                return value.Start().EnsureIsAfternoon();
            }
        }
    }
}
