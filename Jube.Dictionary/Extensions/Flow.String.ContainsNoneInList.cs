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
            public Flow<string?> MatchContainsNoneInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }

            public Flow<string?> RejectContainsNoneInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }

            public Flow<string?> BreakContainsNoneInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }

            public Flow<string?> RequireContainsNoneInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }

            public Flow<string?> EnsureContainsNoneInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsNoneInList(IEnumerable<string> list)
            {
                return value.Start().MatchContainsNoneInList(list);
            }

            public Flow<string?> RejectContainsNoneInList(IEnumerable<string> list)
            {
                return value.Start().RejectContainsNoneInList(list);
            }

            public Flow<string?> BreakContainsNoneInList(IEnumerable<string> list)
            {
                return value.Start().BreakContainsNoneInList(list);
            }

            public Flow<string?> RequireContainsNoneInList(IEnumerable<string> list)
            {
                return value.Start().RequireContainsNoneInList(list);
            }

            public Flow<string?> EnsureContainsNoneInList(IEnumerable<string> list)
            {
                return value.Start().EnsureContainsNoneInList(list);
            }
        }
    }
}
