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
            public Flow<string?> MatchLevenshteinAtMost(string? other, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }

            public Flow<string?> RejectLevenshteinAtMost(string? other, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }

            public Flow<string?> BreakLevenshteinAtMost(string? other, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }

            public Flow<string?> RequireLevenshteinAtMost(string? other, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }

            public Flow<string?> EnsureLevenshteinAtMost(string? other, int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchLevenshteinAtMost(string? other, int distance)
            {
                return value.Start().MatchLevenshteinAtMost(other, distance);
            }

            public Flow<string?> RejectLevenshteinAtMost(string? other, int distance)
            {
                return value.Start().RejectLevenshteinAtMost(other, distance);
            }

            public Flow<string?> BreakLevenshteinAtMost(string? other, int distance)
            {
                return value.Start().BreakLevenshteinAtMost(other, distance);
            }

            public Flow<string?> RequireLevenshteinAtMost(string? other, int distance)
            {
                return value.Start().RequireLevenshteinAtMost(other, distance);
            }

            public Flow<string?> EnsureLevenshteinAtMost(string? other, int distance)
            {
                return value.Start().EnsureLevenshteinAtMost(other, distance);
            }
        }
    }
}
