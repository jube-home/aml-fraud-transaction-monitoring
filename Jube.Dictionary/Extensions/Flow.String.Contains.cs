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
            public Flow<string?> MatchContains(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContains(flow.Value, substring));
            }

            public Flow<string?> RejectContains(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContains(flow.Value, substring));
            }

            public Flow<string?> BreakContains(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContains(flow.Value, substring));
            }

            public Flow<string?> RequireContains(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContains(flow.Value, substring));
            }

            public Flow<string?> EnsureContains(string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContains(flow.Value, substring));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContains(string? substring)
            {
                return value.Start().MatchContains(substring);
            }

            public Flow<string?> RejectContains(string? substring)
            {
                return value.Start().RejectContains(substring);
            }

            public Flow<string?> BreakContains(string? substring)
            {
                return value.Start().BreakContains(substring);
            }

            public Flow<string?> RequireContains(string? substring)
            {
                return value.Start().RequireContains(substring);
            }

            public Flow<string?> EnsureContains(string? substring)
            {
                return value.Start().EnsureContains(substring);
            }
        }
    }
}
