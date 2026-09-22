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
            public Flow<string?> MatchEmailDomainIn(params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }

            public Flow<string?> RejectEmailDomainIn(params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }

            public Flow<string?> BreakEmailDomainIn(params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }

            public Flow<string?> RequireEmailDomainIn(params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }

            public Flow<string?> EnsureEmailDomainIn(params string[] domains)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEmailDomainIn(flow.Value, domains));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchEmailDomainIn(params string[] domains)
            {
                return value.Start().MatchEmailDomainIn(domains);
            }

            public Flow<string?> RejectEmailDomainIn(params string[] domains)
            {
                return value.Start().RejectEmailDomainIn(domains);
            }

            public Flow<string?> BreakEmailDomainIn(params string[] domains)
            {
                return value.Start().BreakEmailDomainIn(domains);
            }

            public Flow<string?> RequireEmailDomainIn(params string[] domains)
            {
                return value.Start().RequireEmailDomainIn(domains);
            }

            public Flow<string?> EnsureEmailDomainIn(params string[] domains)
            {
                return value.Start().EnsureEmailDomainIn(domains);
            }
        }
    }
}
