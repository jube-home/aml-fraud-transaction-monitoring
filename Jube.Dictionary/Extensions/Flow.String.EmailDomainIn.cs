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
            [Description("True when: the domain of the email is one of the listed domains. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }

            [Description("True when: the domain of the email is one of the listed domains. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }

            [Description("True when: the domain of the email is one of the listed domains. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }

            [Description("True when: the domain of the email is one of the listed domains. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }

            [Description("True when: the domain of the email is one of the listed domains. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: the domain of the email is one of the listed domains. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return value.Start().MatchEmailDomainIn(domains);
            }

            [Description("Begins a pipeline from the value. True when: the domain of the email is one of the listed domains. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return value.Start().RejectEmailDomainIn(domains);
            }

            [Description("Begins a pipeline from the value. True when: the domain of the email is one of the listed domains. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return value.Start().BreakEmailDomainIn(domains);
            }

            [Description("Begins a pipeline from the value. True when: the domain of the email is one of the listed domains. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return value.Start().RequireEmailDomainIn(domains);
            }

            [Description("Begins a pipeline from the value. True when: the domain of the email is one of the listed domains. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEmailDomainIn([Description("The domains the email's domain is compared with.")] params string[] domains)
            {
                return value.Start().EnsureEmailDomainIn(domains);
            }
        }
    }
}
