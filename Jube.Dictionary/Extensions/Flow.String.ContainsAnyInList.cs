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
            public Flow<string?> MatchContainsAnyInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsAnyInList(flow.Value, list));
            }

            public Flow<string?> RejectContainsAnyInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsAnyInList(flow.Value, list));
            }

            public Flow<string?> BreakContainsAnyInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsAnyInList(flow.Value, list));
            }

            public Flow<string?> RequireContainsAnyInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsAnyInList(flow.Value, list));
            }

            public Flow<string?> EnsureContainsAnyInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsAnyInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsAnyInList(IEnumerable<string> list)
            {
                return value.Start().MatchContainsAnyInList(list);
            }

            public Flow<string?> RejectContainsAnyInList(IEnumerable<string> list)
            {
                return value.Start().RejectContainsAnyInList(list);
            }

            public Flow<string?> BreakContainsAnyInList(IEnumerable<string> list)
            {
                return value.Start().BreakContainsAnyInList(list);
            }

            public Flow<string?> RequireContainsAnyInList(IEnumerable<string> list)
            {
                return value.Start().RequireContainsAnyInList(list);
            }

            public Flow<string?> EnsureContainsAnyInList(IEnumerable<string> list)
            {
                return value.Start().EnsureContainsAnyInList(list);
            }
        }
    }
}
