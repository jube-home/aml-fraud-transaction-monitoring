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
            public Flow<string?> MatchIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }

            public Flow<string?> RejectIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }

            public Flow<string?> BreakIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }

            public Flow<string?> RequireIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }

            public Flow<string?> EnsureIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNotValidCardExpiry()
            {
                return value.Start().MatchIsNotValidCardExpiry();
            }

            public Flow<string?> RejectIsNotValidCardExpiry()
            {
                return value.Start().RejectIsNotValidCardExpiry();
            }

            public Flow<string?> BreakIsNotValidCardExpiry()
            {
                return value.Start().BreakIsNotValidCardExpiry();
            }

            public Flow<string?> RequireIsNotValidCardExpiry()
            {
                return value.Start().RequireIsNotValidCardExpiry();
            }

            public Flow<string?> EnsureIsNotValidCardExpiry()
            {
                return value.Start().EnsureIsNotValidCardExpiry();
            }
        }
    }
}
