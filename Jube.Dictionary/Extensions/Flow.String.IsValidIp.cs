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
            [Description("True when: a valid IPv4 address. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsValidIp(flow.Value));
            }

            [Description("True when: a valid IPv4 address. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsValidIp(flow.Value));
            }

            [Description("True when: a valid IPv4 address. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsValidIp(flow.Value));
            }

            [Description("True when: a valid IPv4 address. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsValidIp(flow.Value));
            }

            [Description("True when: a valid IPv4 address. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidIp()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsValidIp(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: a valid IPv4 address. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidIp()
            {
                return value.Start().MatchIsValidIp();
            }

            [Description("Begins a pipeline from the value. True when: a valid IPv4 address. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidIp()
            {
                return value.Start().RejectIsValidIp();
            }

            [Description("Begins a pipeline from the value. True when: a valid IPv4 address. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidIp()
            {
                return value.Start().BreakIsValidIp();
            }

            [Description("Begins a pipeline from the value. True when: a valid IPv4 address. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidIp()
            {
                return value.Start().RequireIsValidIp();
            }

            [Description("Begins a pipeline from the value. True when: a valid IPv4 address. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidIp()
            {
                return value.Start().EnsureIsValidIp();
            }
        }
    }
}
