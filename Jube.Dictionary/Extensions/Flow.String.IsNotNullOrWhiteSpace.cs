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
            public Flow<string?> MatchIsNotNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotNullOrWhiteSpace(flow.Value));
            }

            public Flow<string?> RejectIsNotNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotNullOrWhiteSpace(flow.Value));
            }

            public Flow<string?> BreakIsNotNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotNullOrWhiteSpace(flow.Value));
            }

            public Flow<string?> RequireIsNotNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotNullOrWhiteSpace(flow.Value));
            }

            public Flow<string?> EnsureIsNotNullOrWhiteSpace()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotNullOrWhiteSpace(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNotNullOrWhiteSpace()
            {
                return value.Start().MatchIsNotNullOrWhiteSpace();
            }

            public Flow<string?> RejectIsNotNullOrWhiteSpace()
            {
                return value.Start().RejectIsNotNullOrWhiteSpace();
            }

            public Flow<string?> BreakIsNotNullOrWhiteSpace()
            {
                return value.Start().BreakIsNotNullOrWhiteSpace();
            }

            public Flow<string?> RequireIsNotNullOrWhiteSpace()
            {
                return value.Start().RequireIsNotNullOrWhiteSpace();
            }

            public Flow<string?> EnsureIsNotNullOrWhiteSpace()
            {
                return value.Start().EnsureIsNotNullOrWhiteSpace();
            }
        }
    }
}
