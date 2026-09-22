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
            public Flow<string?> MatchEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEqual(flow.Value, other));
            }

            public Flow<string?> RejectEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEqual(flow.Value, other));
            }

            public Flow<string?> BreakEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEqual(flow.Value, other));
            }

            public Flow<string?> RequireEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEqual(flow.Value, other));
            }

            public Flow<string?> EnsureEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEqual(flow.Value, other));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchEqual(string? other)
            {
                return value.Start().MatchEqual(other);
            }

            public Flow<string?> RejectEqual(string? other)
            {
                return value.Start().RejectEqual(other);
            }

            public Flow<string?> BreakEqual(string? other)
            {
                return value.Start().BreakEqual(other);
            }

            public Flow<string?> RequireEqual(string? other)
            {
                return value.Start().RequireEqual(other);
            }

            public Flow<string?> EnsureEqual(string? other)
            {
                return value.Start().EnsureEqual(other);
            }
        }
    }
}
