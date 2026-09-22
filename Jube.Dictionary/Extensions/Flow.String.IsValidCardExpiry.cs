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
            public Flow<string?> MatchIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }

            public Flow<string?> RejectIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }

            public Flow<string?> BreakIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }

            public Flow<string?> RequireIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }

            public Flow<string?> EnsureIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsValidCardExpiry()
            {
                return value.Start().MatchIsValidCardExpiry();
            }

            public Flow<string?> RejectIsValidCardExpiry()
            {
                return value.Start().RejectIsValidCardExpiry();
            }

            public Flow<string?> BreakIsValidCardExpiry()
            {
                return value.Start().BreakIsValidCardExpiry();
            }

            public Flow<string?> RequireIsValidCardExpiry()
            {
                return value.Start().RequireIsValidCardExpiry();
            }

            public Flow<string?> EnsureIsValidCardExpiry()
            {
                return value.Start().EnsureIsValidCardExpiry();
            }
        }
    }
}
