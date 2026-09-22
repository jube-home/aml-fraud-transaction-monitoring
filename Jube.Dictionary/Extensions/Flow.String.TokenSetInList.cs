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
            public Flow<string?> MatchTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }

            public Flow<string?> RejectTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }

            public Flow<string?> BreakTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }

            public Flow<string?> RequireTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }

            public Flow<string?> EnsureTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().MatchTokenSetInList(list, threshold);
            }

            public Flow<string?> RejectTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RejectTokenSetInList(list, threshold);
            }

            public Flow<string?> BreakTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().BreakTokenSetInList(list, threshold);
            }

            public Flow<string?> RequireTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RequireTokenSetInList(list, threshold);
            }

            public Flow<string?> EnsureTokenSetInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().EnsureTokenSetInList(list, threshold);
            }
        }
    }
}
