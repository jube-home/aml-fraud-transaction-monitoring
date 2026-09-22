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
            public Flow<string?> MatchEmailDomainInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEmailDomainInList(flow.Value, list));
            }

            public Flow<string?> RejectEmailDomainInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEmailDomainInList(flow.Value, list));
            }

            public Flow<string?> BreakEmailDomainInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEmailDomainInList(flow.Value, list));
            }

            public Flow<string?> RequireEmailDomainInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEmailDomainInList(flow.Value, list));
            }

            public Flow<string?> EnsureEmailDomainInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEmailDomainInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchEmailDomainInList(IEnumerable<string> list)
            {
                return value.Start().MatchEmailDomainInList(list);
            }

            public Flow<string?> RejectEmailDomainInList(IEnumerable<string> list)
            {
                return value.Start().RejectEmailDomainInList(list);
            }

            public Flow<string?> BreakEmailDomainInList(IEnumerable<string> list)
            {
                return value.Start().BreakEmailDomainInList(list);
            }

            public Flow<string?> RequireEmailDomainInList(IEnumerable<string> list)
            {
                return value.Start().RequireEmailDomainInList(list);
            }

            public Flow<string?> EnsureEmailDomainInList(IEnumerable<string> list)
            {
                return value.Start().EnsureEmailDomainInList(list);
            }
        }
    }
}
