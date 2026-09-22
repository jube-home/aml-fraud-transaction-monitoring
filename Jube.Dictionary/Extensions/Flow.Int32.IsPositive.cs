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
            public Flow<int> MatchIsPositive()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32IsPositive(flow.Value));
            }

            public Flow<int> RejectIsPositive()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32IsPositive(flow.Value));
            }

            public Flow<int> BreakIsPositive()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32IsPositive(flow.Value));
            }

            public Flow<int> RequireIsPositive()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32IsPositive(flow.Value));
            }

            public Flow<int> EnsureIsPositive()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32IsPositive(flow.Value));
            }
        }

        extension(int value)
        {
            public Flow<int> MatchIsPositive()
            {
                return value.Start().MatchIsPositive();
            }

            public Flow<int> RejectIsPositive()
            {
                return value.Start().RejectIsPositive();
            }

            public Flow<int> BreakIsPositive()
            {
                return value.Start().BreakIsPositive();
            }

            public Flow<int> RequireIsPositive()
            {
                return value.Start().RequireIsPositive();
            }

            public Flow<int> EnsureIsPositive()
            {
                return value.Start().EnsureIsPositive();
            }
        }
    }
}
