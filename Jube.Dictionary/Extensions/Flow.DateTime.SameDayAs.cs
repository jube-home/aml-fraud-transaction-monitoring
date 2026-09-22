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
            public Flow<DateTime> MatchSameDayAs(DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }

            public Flow<DateTime> RejectSameDayAs(DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }

            public Flow<DateTime> BreakSameDayAs(DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }

            public Flow<DateTime> RequireSameDayAs(DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }

            public Flow<DateTime> EnsureSameDayAs(DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchSameDayAs(DateTime other)
            {
                return value.Start().MatchSameDayAs(other);
            }

            public Flow<DateTime> RejectSameDayAs(DateTime other)
            {
                return value.Start().RejectSameDayAs(other);
            }

            public Flow<DateTime> BreakSameDayAs(DateTime other)
            {
                return value.Start().BreakSameDayAs(other);
            }

            public Flow<DateTime> RequireSameDayAs(DateTime other)
            {
                return value.Start().RequireSameDayAs(other);
            }

            public Flow<DateTime> EnsureSameDayAs(DateTime other)
            {
                return value.Start().EnsureSameDayAs(other);
            }
        }
    }
}
