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
            public Flow<string?> MatchEmailDomainEqual(string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }

            public Flow<string?> RejectEmailDomainEqual(string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }

            public Flow<string?> BreakEmailDomainEqual(string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }

            public Flow<string?> RequireEmailDomainEqual(string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }

            public Flow<string?> EnsureEmailDomainEqual(string? domain)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEmailDomainEqual(flow.Value, domain));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchEmailDomainEqual(string? domain)
            {
                return value.Start().MatchEmailDomainEqual(domain);
            }

            public Flow<string?> RejectEmailDomainEqual(string? domain)
            {
                return value.Start().RejectEmailDomainEqual(domain);
            }

            public Flow<string?> BreakEmailDomainEqual(string? domain)
            {
                return value.Start().BreakEmailDomainEqual(domain);
            }

            public Flow<string?> RequireEmailDomainEqual(string? domain)
            {
                return value.Start().RequireEmailDomainEqual(domain);
            }

            public Flow<string?> EnsureEmailDomainEqual(string? domain)
            {
                return value.Start().EnsureEmailDomainEqual(domain);
            }
        }
    }
}
