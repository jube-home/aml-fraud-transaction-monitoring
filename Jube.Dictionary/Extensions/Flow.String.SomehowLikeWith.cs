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
            public Flow<string?> MatchSomehowLikeWith(string? other, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }

            public Flow<string?> RejectSomehowLikeWith(string? other, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }

            public Flow<string?> BreakSomehowLikeWith(string? other, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }

            public Flow<string?> RequireSomehowLikeWith(string? other, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }

            public Flow<string?> EnsureSomehowLikeWith(string? other, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchSomehowLikeWith(string? other, string? options)
            {
                return value.Start().MatchSomehowLikeWith(other, options);
            }

            public Flow<string?> RejectSomehowLikeWith(string? other, string? options)
            {
                return value.Start().RejectSomehowLikeWith(other, options);
            }

            public Flow<string?> BreakSomehowLikeWith(string? other, string? options)
            {
                return value.Start().BreakSomehowLikeWith(other, options);
            }

            public Flow<string?> RequireSomehowLikeWith(string? other, string? options)
            {
                return value.Start().RequireSomehowLikeWith(other, options);
            }

            public Flow<string?> EnsureSomehowLikeWith(string? other, string? options)
            {
                return value.Start().EnsureSomehowLikeWith(other, options);
            }
        }
    }
}
