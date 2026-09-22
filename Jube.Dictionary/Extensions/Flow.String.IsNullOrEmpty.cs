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
            public Flow<string?> MatchIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }

            public Flow<string?> RejectIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }

            public Flow<string?> BreakIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }

            public Flow<string?> RequireIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }

            public Flow<string?> EnsureIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNullOrEmpty()
            {
                return value.Start().MatchIsNullOrEmpty();
            }

            public Flow<string?> RejectIsNullOrEmpty()
            {
                return value.Start().RejectIsNullOrEmpty();
            }

            public Flow<string?> BreakIsNullOrEmpty()
            {
                return value.Start().BreakIsNullOrEmpty();
            }

            public Flow<string?> RequireIsNullOrEmpty()
            {
                return value.Start().RequireIsNullOrEmpty();
            }

            public Flow<string?> EnsureIsNullOrEmpty()
            {
                return value.Start().EnsureIsNullOrEmpty();
            }
        }
    }
}
