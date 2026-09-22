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
            public Flow<string?> MatchContainsIgnoreCase(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }

            public Flow<string?> RejectContainsIgnoreCase(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }

            public Flow<string?> BreakContainsIgnoreCase(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }

            public Flow<string?> RequireContainsIgnoreCase(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }

            public Flow<string?> EnsureContainsIgnoreCase(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsIgnoreCase(string? substring)
            {
                return value.Start().MatchContainsIgnoreCase(substring);
            }

            public Flow<string?> RejectContainsIgnoreCase(string? substring)
            {
                return value.Start().RejectContainsIgnoreCase(substring);
            }

            public Flow<string?> BreakContainsIgnoreCase(string? substring)
            {
                return value.Start().BreakContainsIgnoreCase(substring);
            }

            public Flow<string?> RequireContainsIgnoreCase(string? substring)
            {
                return value.Start().RequireContainsIgnoreCase(substring);
            }

            public Flow<string?> EnsureContainsIgnoreCase(string? substring)
            {
                return value.Start().EnsureContainsIgnoreCase(substring);
            }
        }
    }
}
