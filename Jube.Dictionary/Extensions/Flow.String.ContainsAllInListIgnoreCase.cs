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
            public Flow<string?> MatchContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsAllInListIgnoreCase(flow.Value, list));
            }

            public Flow<string?> RejectContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsAllInListIgnoreCase(flow.Value, list));
            }

            public Flow<string?> BreakContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsAllInListIgnoreCase(flow.Value, list));
            }

            public Flow<string?> RequireContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsAllInListIgnoreCase(flow.Value, list));
            }

            public Flow<string?> EnsureContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsAllInListIgnoreCase(flow.Value, list));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return value.Start().MatchContainsAllInListIgnoreCase(list);
            }

            public Flow<string?> RejectContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return value.Start().RejectContainsAllInListIgnoreCase(list);
            }

            public Flow<string?> BreakContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return value.Start().BreakContainsAllInListIgnoreCase(list);
            }

            public Flow<string?> RequireContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return value.Start().RequireContainsAllInListIgnoreCase(list);
            }

            public Flow<string?> EnsureContainsAllInListIgnoreCase(IEnumerable<string> list)
            {
                return value.Start().EnsureContainsAllInListIgnoreCase(list);
            }
        }
    }
}
