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
        extension(Flow<string?> flow)
        {
            public Flow<string?> MatchIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsEmpty(flow.Value));
            }

            public Flow<string?> RejectIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsEmpty(flow.Value));
            }

            public Flow<string?> BreakIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsEmpty(flow.Value));
            }

            public Flow<string?> RequireIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsEmpty(flow.Value));
            }

            public Flow<string?> EnsureIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsEmpty(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsEmpty()
            {
                return value.Start().MatchIsEmpty();
            }

            public Flow<string?> RejectIsEmpty()
            {
                return value.Start().RejectIsEmpty();
            }

            public Flow<string?> BreakIsEmpty()
            {
                return value.Start().BreakIsEmpty();
            }

            public Flow<string?> RequireIsEmpty()
            {
                return value.Start().RequireIsEmpty();
            }

            public Flow<string?> EnsureIsEmpty()
            {
                return value.Start().EnsureIsEmpty();
            }
        }
    }
}
