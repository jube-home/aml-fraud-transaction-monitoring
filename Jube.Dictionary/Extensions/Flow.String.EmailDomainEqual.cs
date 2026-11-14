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
            [Description("True when: the domain of the email equals the argument, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }

            [Description("True when: the domain of the email equals the argument, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }

            [Description("True when: the domain of the email equals the argument, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }

            [Description("True when: the domain of the email equals the argument, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }

            [Description("True when: the domain of the email equals the argument, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: the domain of the email equals the argument, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return value.Start().MatchEmailDomainEqual(domain);
            }

            [Description("Begins a pipeline from the value. True when: the domain of the email equals the argument, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return value.Start().RejectEmailDomainEqual(domain);
            }

            [Description("Begins a pipeline from the value. True when: the domain of the email equals the argument, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return value.Start().BreakEmailDomainEqual(domain);
            }

            [Description("Begins a pipeline from the value. True when: the domain of the email equals the argument, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return value.Start().RequireEmailDomainEqual(domain);
            }

            [Description("Begins a pipeline from the value. True when: the domain of the email equals the argument, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEmailDomainEqual([Description("The domain the email's domain is compared with.")] string? domain)
            {
                return value.Start().EnsureEmailDomainEqual(domain);
            }
        }
    }
}
