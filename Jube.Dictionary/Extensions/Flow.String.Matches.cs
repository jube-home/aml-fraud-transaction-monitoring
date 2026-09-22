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
            public Flow<string?> MatchMatches(string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringMatches(flow.Value, pattern));
            }

            public Flow<string?> RejectMatches(string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringMatches(flow.Value, pattern));
            }

            public Flow<string?> BreakMatches(string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringMatches(flow.Value, pattern));
            }

            public Flow<string?> RequireMatches(string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringMatches(flow.Value, pattern));
            }

            public Flow<string?> EnsureMatches(string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringMatches(flow.Value, pattern));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchMatches(string? pattern)
            {
                return value.Start().MatchMatches(pattern);
            }

            public Flow<string?> RejectMatches(string? pattern)
            {
                return value.Start().RejectMatches(pattern);
            }

            public Flow<string?> BreakMatches(string? pattern)
            {
                return value.Start().BreakMatches(pattern);
            }

            public Flow<string?> RequireMatches(string? pattern)
            {
                return value.Start().RequireMatches(pattern);
            }

            public Flow<string?> EnsureMatches(string? pattern)
            {
                return value.Start().EnsureMatches(pattern);
            }
        }
    }
}
