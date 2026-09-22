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
            public Flow<string?> MatchSomehowLike(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSomehowLike(flow.Value, other));
            }

            public Flow<string?> RejectSomehowLike(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSomehowLike(flow.Value, other));
            }

            public Flow<string?> BreakSomehowLike(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSomehowLike(flow.Value, other));
            }

            public Flow<string?> RequireSomehowLike(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSomehowLike(flow.Value, other));
            }

            public Flow<string?> EnsureSomehowLike(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSomehowLike(flow.Value, other));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchSomehowLike(string? other)
            {
                return value.Start().MatchSomehowLike(other);
            }

            public Flow<string?> RejectSomehowLike(string? other)
            {
                return value.Start().RejectSomehowLike(other);
            }

            public Flow<string?> BreakSomehowLike(string? other)
            {
                return value.Start().BreakSomehowLike(other);
            }

            public Flow<string?> RequireSomehowLike(string? other)
            {
                return value.Start().RequireSomehowLike(other);
            }

            public Flow<string?> EnsureSomehowLike(string? other)
            {
                return value.Start().EnsureSomehowLike(other);
            }
        }
    }
}
