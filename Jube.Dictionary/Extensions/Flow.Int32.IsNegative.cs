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
            public Flow<int> MatchIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32IsNegative(flow.Value));
            }

            public Flow<int> RejectIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32IsNegative(flow.Value));
            }

            public Flow<int> BreakIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32IsNegative(flow.Value));
            }

            public Flow<int> RequireIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32IsNegative(flow.Value));
            }

            public Flow<int> EnsureIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32IsNegative(flow.Value));
            }
        }

        extension(int value)
        {
            public Flow<int> MatchIsNegative()
            {
                return value.Start().MatchIsNegative();
            }

            public Flow<int> RejectIsNegative()
            {
                return value.Start().RejectIsNegative();
            }

            public Flow<int> BreakIsNegative()
            {
                return value.Start().BreakIsNegative();
            }

            public Flow<int> RequireIsNegative()
            {
                return value.Start().RequireIsNegative();
            }

            public Flow<int> EnsureIsNegative()
            {
                return value.Start().EnsureIsNegative();
            }
        }
    }
}
