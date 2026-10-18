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
            public Flow<string?> MatchIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }

            public Flow<string?> RejectIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }

            public Flow<string?> BreakIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }

            public Flow<string?> RequireIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }

            public Flow<string?> EnsureIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return value.Start().MatchIsInAnyCidr(cidrs);
            }

            public Flow<string?> RejectIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return value.Start().RejectIsInAnyCidr(cidrs);
            }

            public Flow<string?> BreakIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return value.Start().BreakIsInAnyCidr(cidrs);
            }

            public Flow<string?> RequireIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return value.Start().RequireIsInAnyCidr(cidrs);
            }

            public Flow<string?> EnsureIsInAnyCidr(IEnumerable<string>? cidrs)
            {
                return value.Start().EnsureIsInAnyCidr(cidrs);
            }
        }
    }
}