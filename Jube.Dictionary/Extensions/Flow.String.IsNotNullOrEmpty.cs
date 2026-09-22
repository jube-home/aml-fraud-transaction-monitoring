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
            public Flow<string?> MatchIsNotNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotNullOrEmpty(flow.Value));
            }

            public Flow<string?> RejectIsNotNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotNullOrEmpty(flow.Value));
            }

            public Flow<string?> BreakIsNotNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotNullOrEmpty(flow.Value));
            }

            public Flow<string?> RequireIsNotNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotNullOrEmpty(flow.Value));
            }

            public Flow<string?> EnsureIsNotNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotNullOrEmpty(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNotNullOrEmpty()
            {
                return value.Start().MatchIsNotNullOrEmpty();
            }

            public Flow<string?> RejectIsNotNullOrEmpty()
            {
                return value.Start().RejectIsNotNullOrEmpty();
            }

            public Flow<string?> BreakIsNotNullOrEmpty()
            {
                return value.Start().BreakIsNotNullOrEmpty();
            }

            public Flow<string?> RequireIsNotNullOrEmpty()
            {
                return value.Start().RequireIsNotNullOrEmpty();
            }

            public Flow<string?> EnsureIsNotNullOrEmpty()
            {
                return value.Start().EnsureIsNotNullOrEmpty();
            }
        }
    }
}
