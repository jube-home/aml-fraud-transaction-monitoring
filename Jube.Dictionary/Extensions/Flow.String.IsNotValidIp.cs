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
            [Description("True when: null, empty or not a valid IPv4 address. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNotValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotValidIp(flow.Value));
            }

            [Description("True when: null, empty or not a valid IPv4 address. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNotValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotValidIp(flow.Value));
            }

            [Description("True when: null, empty or not a valid IPv4 address. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNotValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotValidIp(flow.Value));
            }

            [Description("True when: null, empty or not a valid IPv4 address. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNotValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotValidIp(flow.Value));
            }

            [Description("True when: null, empty or not a valid IPv4 address. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNotValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotValidIp(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: null, empty or not a valid IPv4 address. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNotValidIp()
            {
                return value.Start().MatchIsNotValidIp();
            }

            [Description("Begins a pipeline from the value. True when: null, empty or not a valid IPv4 address. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNotValidIp()
            {
                return value.Start().RejectIsNotValidIp();
            }

            [Description("Begins a pipeline from the value. True when: null, empty or not a valid IPv4 address. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNotValidIp()
            {
                return value.Start().BreakIsNotValidIp();
            }

            [Description("Begins a pipeline from the value. True when: null, empty or not a valid IPv4 address. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNotValidIp()
            {
                return value.Start().RequireIsNotValidIp();
            }

            [Description("Begins a pipeline from the value. True when: null, empty or not a valid IPv4 address. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNotValidIp()
            {
                return value.Start().EnsureIsNotValidIp();
            }
        }
    }
}
