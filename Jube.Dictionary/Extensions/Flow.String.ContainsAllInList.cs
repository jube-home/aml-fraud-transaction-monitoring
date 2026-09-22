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
            public Flow<string?> MatchContainsAllInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }

            public Flow<string?> RejectContainsAllInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }

            public Flow<string?> BreakContainsAllInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }

            public Flow<string?> RequireContainsAllInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }

            public Flow<string?> EnsureContainsAllInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsAllInList(IEnumerable<string> list)
            {
                return value.Start().MatchContainsAllInList(list);
            }

            public Flow<string?> RejectContainsAllInList(IEnumerable<string> list)
            {
                return value.Start().RejectContainsAllInList(list);
            }

            public Flow<string?> BreakContainsAllInList(IEnumerable<string> list)
            {
                return value.Start().BreakContainsAllInList(list);
            }

            public Flow<string?> RequireContainsAllInList(IEnumerable<string> list)
            {
                return value.Start().RequireContainsAllInList(list);
            }

            public Flow<string?> EnsureContainsAllInList(IEnumerable<string> list)
            {
                return value.Start().EnsureContainsAllInList(list);
            }
        }
    }
}
