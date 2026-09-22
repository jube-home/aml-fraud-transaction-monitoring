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
            public Flow<string?> MatchIsNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNullOrWhiteSpace(flow.Value));
            }

            public Flow<string?> RejectIsNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNullOrWhiteSpace(flow.Value));
            }

            public Flow<string?> BreakIsNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNullOrWhiteSpace(flow.Value));
            }

            public Flow<string?> RequireIsNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNullOrWhiteSpace(flow.Value));
            }

            public Flow<string?> EnsureIsNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNullOrWhiteSpace(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNullOrWhiteSpace()
            {
                return value.Start().MatchIsNullOrWhiteSpace();
            }

            public Flow<string?> RejectIsNullOrWhiteSpace()
            {
                return value.Start().RejectIsNullOrWhiteSpace();
            }

            public Flow<string?> BreakIsNullOrWhiteSpace()
            {
                return value.Start().BreakIsNullOrWhiteSpace();
            }

            public Flow<string?> RequireIsNullOrWhiteSpace()
            {
                return value.Start().RequireIsNullOrWhiteSpace();
            }

            public Flow<string?> EnsureIsNullOrWhiteSpace()
            {
                return value.Start().EnsureIsNullOrWhiteSpace();
            }
        }
    }
}
