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
            public Flow<string?> MatchIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsValidIp(flow.Value));
            }

            public Flow<string?> RejectIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsValidIp(flow.Value));
            }

            public Flow<string?> BreakIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsValidIp(flow.Value));
            }

            public Flow<string?> RequireIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsValidIp(flow.Value));
            }

            public Flow<string?> EnsureIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsValidIp(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsValidIp()
            {
                return value.Start().MatchIsValidIp();
            }

            public Flow<string?> RejectIsValidIp()
            {
                return value.Start().RejectIsValidIp();
            }

            public Flow<string?> BreakIsValidIp()
            {
                return value.Start().BreakIsValidIp();
            }

            public Flow<string?> RequireIsValidIp()
            {
                return value.Start().RequireIsValidIp();
            }

            public Flow<string?> EnsureIsValidIp()
            {
                return value.Start().EnsureIsValidIp();
            }
        }
    }
}
