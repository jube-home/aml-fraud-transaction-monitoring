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
            public Flow<string?> MatchLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }

            public Flow<string?> RejectLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }

            public Flow<string?> BreakLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }

            public Flow<string?> RequireLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }

            public Flow<string?> EnsureLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return value.Start().MatchLevenshteinAtMostAnyInList(list, distance);
            }

            public Flow<string?> RejectLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return value.Start().RejectLevenshteinAtMostAnyInList(list, distance);
            }

            public Flow<string?> BreakLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return value.Start().BreakLevenshteinAtMostAnyInList(list, distance);
            }

            public Flow<string?> RequireLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return value.Start().RequireLevenshteinAtMostAnyInList(list, distance);
            }

            public Flow<string?> EnsureLevenshteinAtMostAnyInList(IEnumerable<string> list, int distance)
            {
                return value.Start().EnsureLevenshteinAtMostAnyInList(list, distance);
            }
        }
    }
}
