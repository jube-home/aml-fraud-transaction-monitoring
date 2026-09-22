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
            public Flow<DateTime> MatchIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsPast(flow.Value));
            }

            public Flow<DateTime> RejectIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsPast(flow.Value));
            }

            public Flow<DateTime> BreakIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsPast(flow.Value));
            }

            public Flow<DateTime> RequireIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsPast(flow.Value));
            }

            public Flow<DateTime> EnsureIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsPast(flow.Value));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchIsPast()
            {
                return value.Start().MatchIsPast();
            }

            public Flow<DateTime> RejectIsPast()
            {
                return value.Start().RejectIsPast();
            }

            public Flow<DateTime> BreakIsPast()
            {
                return value.Start().BreakIsPast();
            }

            public Flow<DateTime> RequireIsPast()
            {
                return value.Start().RequireIsPast();
            }

            public Flow<DateTime> EnsureIsPast()
            {
                return value.Start().EnsureIsPast();
            }
        }
    }
}
