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
            public Flow<string?> MatchEmailAliasNormalisedEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }

            public Flow<string?> RejectEmailAliasNormalisedEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }

            public Flow<string?> BreakEmailAliasNormalisedEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }

            public Flow<string?> RequireEmailAliasNormalisedEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }

            public Flow<string?> EnsureEmailAliasNormalisedEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchEmailAliasNormalisedEqual(string? other)
            {
                return value.Start().MatchEmailAliasNormalisedEqual(other);
            }

            public Flow<string?> RejectEmailAliasNormalisedEqual(string? other)
            {
                return value.Start().RejectEmailAliasNormalisedEqual(other);
            }

            public Flow<string?> BreakEmailAliasNormalisedEqual(string? other)
            {
                return value.Start().BreakEmailAliasNormalisedEqual(other);
            }

            public Flow<string?> RequireEmailAliasNormalisedEqual(string? other)
            {
                return value.Start().RequireEmailAliasNormalisedEqual(other);
            }

            public Flow<string?> EnsureEmailAliasNormalisedEqual(string? other)
            {
                return value.Start().EnsureEmailAliasNormalisedEqual(other);
            }
        }
    }
}
