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
        extension(Flow<bool> flow)
        {
            public Flow<bool> MatchIsTrue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.BooleanIsTrue(flow.Value));
            }

            public Flow<bool> RejectIsTrue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.BooleanIsTrue(flow.Value));
            }

            public Flow<bool> BreakIsTrue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.BooleanIsTrue(flow.Value));
            }

            public Flow<bool> RequireIsTrue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.BooleanIsTrue(flow.Value));
            }

            public Flow<bool> EnsureIsTrue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.BooleanIsTrue(flow.Value));
            }
        }

        extension(bool value)
        {
            public Flow<bool> MatchIsTrue()
            {
                return value.Start().MatchIsTrue();
            }

            public Flow<bool> RejectIsTrue()
            {
                return value.Start().RejectIsTrue();
            }

            public Flow<bool> BreakIsTrue()
            {
                return value.Start().BreakIsTrue();
            }

            public Flow<bool> RequireIsTrue()
            {
                return value.Start().RequireIsTrue();
            }

            public Flow<bool> EnsureIsTrue()
            {
                return value.Start().EnsureIsTrue();
            }
        }
    }
}
