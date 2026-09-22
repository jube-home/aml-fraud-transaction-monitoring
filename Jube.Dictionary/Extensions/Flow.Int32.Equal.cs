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
        extension(Flow<int> flow)
        {
            public Flow<int> MatchEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32Equal(flow.Value, other));
            }

            public Flow<int> RejectEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32Equal(flow.Value, other));
            }

            public Flow<int> BreakEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32Equal(flow.Value, other));
            }

            public Flow<int> RequireEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32Equal(flow.Value, other));
            }

            public Flow<int> EnsureEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32Equal(flow.Value, other));
            }
        }

        extension(int value)
        {
            public Flow<int> MatchEqual(int other)
            {
                return value.Start().MatchEqual(other);
            }

            public Flow<int> RejectEqual(int other)
            {
                return value.Start().RejectEqual(other);
            }

            public Flow<int> BreakEqual(int other)
            {
                return value.Start().BreakEqual(other);
            }

            public Flow<int> RequireEqual(int other)
            {
                return value.Start().RequireEqual(other);
            }

            public Flow<int> EnsureEqual(int other)
            {
                return value.Start().EnsureEqual(other);
            }
        }
    }
}
