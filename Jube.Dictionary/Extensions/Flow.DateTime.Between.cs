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
            public Flow<DateTime> MatchBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }

            public Flow<DateTime> RejectBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }

            public Flow<DateTime> BreakBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }

            public Flow<DateTime> RequireBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }

            public Flow<DateTime> EnsureBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().MatchBetween(rangeStart, rangeEnd);
            }

            public Flow<DateTime> RejectBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().RejectBetween(rangeStart, rangeEnd);
            }

            public Flow<DateTime> BreakBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().BreakBetween(rangeStart, rangeEnd);
            }

            public Flow<DateTime> RequireBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().RequireBetween(rangeStart, rangeEnd);
            }

            public Flow<DateTime> EnsureBetween(DateTime rangeStart, DateTime rangeEnd)
            {
                return value.Start().EnsureBetween(rangeStart, rangeEnd);
            }
        }
    }
}
