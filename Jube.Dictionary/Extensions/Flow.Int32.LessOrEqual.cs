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
            public Flow<int> MatchLessOrEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32LessOrEqual(flow.Value, other));
            }

            public Flow<int> RejectLessOrEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32LessOrEqual(flow.Value, other));
            }

            public Flow<int> BreakLessOrEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32LessOrEqual(flow.Value, other));
            }

            public Flow<int> RequireLessOrEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32LessOrEqual(flow.Value, other));
            }

            public Flow<int> EnsureLessOrEqual(int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32LessOrEqual(flow.Value, other));
            }
        }

        extension(int value)
        {
            public Flow<int> MatchLessOrEqual(int other)
            {
                return value.Start().MatchLessOrEqual(other);
            }

            public Flow<int> RejectLessOrEqual(int other)
            {
                return value.Start().RejectLessOrEqual(other);
            }

            public Flow<int> BreakLessOrEqual(int other)
            {
                return value.Start().BreakLessOrEqual(other);
            }

            public Flow<int> RequireLessOrEqual(int other)
            {
                return value.Start().RequireLessOrEqual(other);
            }

            public Flow<int> EnsureLessOrEqual(int other)
            {
                return value.Start().EnsureLessOrEqual(other);
            }
        }
    }
}
