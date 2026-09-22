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
            public Flow<string?> MatchTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }

            public Flow<string?> RejectTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }

            public Flow<string?> BreakTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }

            public Flow<string?> RequireTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }

            public Flow<string?> EnsureTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().MatchTokenSortInList(list, threshold);
            }

            public Flow<string?> RejectTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RejectTokenSortInList(list, threshold);
            }

            public Flow<string?> BreakTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().BreakTokenSortInList(list, threshold);
            }

            public Flow<string?> RequireTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RequireTokenSortInList(list, threshold);
            }

            public Flow<string?> EnsureTokenSortInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().EnsureTokenSortInList(list, threshold);
            }
        }
    }
}
