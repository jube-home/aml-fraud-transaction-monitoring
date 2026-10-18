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
            public Flow<string?> MatchJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringJsonTextArrayAnyInList(flow.Value, path, list));
            }

            public Flow<string?> RejectJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringJsonTextArrayAnyInList(flow.Value, path, list));
            }

            public Flow<string?> BreakJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringJsonTextArrayAnyInList(flow.Value, path, list));
            }

            public Flow<string?> RequireJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringJsonTextArrayAnyInList(flow.Value, path, list));
            }

            public Flow<string?> EnsureJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringJsonTextArrayAnyInList(flow.Value, path, list));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return value.Start().MatchJsonTextArrayAnyInList(path, list);
            }

            public Flow<string?> RejectJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return value.Start().RejectJsonTextArrayAnyInList(path, list);
            }

            public Flow<string?> BreakJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return value.Start().BreakJsonTextArrayAnyInList(path, list);
            }

            public Flow<string?> RequireJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return value.Start().RequireJsonTextArrayAnyInList(path, list);
            }

            public Flow<string?> EnsureJsonTextArrayAnyInList(string? path, IEnumerable<string>? list)
            {
                return value.Start().EnsureJsonTextArrayAnyInList(path, list);
            }
        }
    }
}