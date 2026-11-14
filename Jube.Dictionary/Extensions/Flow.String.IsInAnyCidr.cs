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
            [Description("True when: an IP address inside any of the CIDR ranges. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }

            [Description("True when: an IP address inside any of the CIDR ranges. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }

            [Description("True when: an IP address inside any of the CIDR ranges. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }

            [Description("True when: an IP address inside any of the CIDR ranges. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }

            [Description("True when: an IP address inside any of the CIDR ranges. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsInAnyCidr(flow.Value, cidrs));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: an IP address inside any of the CIDR ranges. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return value.Start().MatchIsInAnyCidr(cidrs);
            }

            [Description("Begins a pipeline from the value. True when: an IP address inside any of the CIDR ranges. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return value.Start().RejectIsInAnyCidr(cidrs);
            }

            [Description("Begins a pipeline from the value. True when: an IP address inside any of the CIDR ranges. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return value.Start().BreakIsInAnyCidr(cidrs);
            }

            [Description("Begins a pipeline from the value. True when: an IP address inside any of the CIDR ranges. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return value.Start().RequireIsInAnyCidr(cidrs);
            }

            [Description("Begins a pipeline from the value. True when: an IP address inside any of the CIDR ranges. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsInAnyCidr([Description("The CIDR ranges, usually a List token.")] IEnumerable<string>? cidrs)
            {
                return value.Start().EnsureIsInAnyCidr(cidrs);
            }
        }
    }
}