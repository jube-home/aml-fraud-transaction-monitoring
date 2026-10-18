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
            public Flow<string?> MatchIsDelimitedMember(string? delimitedList, string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }

            public Flow<string?> RejectIsDelimitedMember(string? delimitedList, string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }

            public Flow<string?> BreakIsDelimitedMember(string? delimitedList, string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }

            public Flow<string?> RequireIsDelimitedMember(string? delimitedList, string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }

            public Flow<string?> EnsureIsDelimitedMember(string? delimitedList, string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsDelimitedMember(string? delimitedList, string? separator)
            {
                return value.Start().MatchIsDelimitedMember(delimitedList, separator);
            }

            public Flow<string?> RejectIsDelimitedMember(string? delimitedList, string? separator)
            {
                return value.Start().RejectIsDelimitedMember(delimitedList, separator);
            }

            public Flow<string?> BreakIsDelimitedMember(string? delimitedList, string? separator)
            {
                return value.Start().BreakIsDelimitedMember(delimitedList, separator);
            }

            public Flow<string?> RequireIsDelimitedMember(string? delimitedList, string? separator)
            {
                return value.Start().RequireIsDelimitedMember(delimitedList, separator);
            }

            public Flow<string?> EnsureIsDelimitedMember(string? delimitedList, string? separator)
            {
                return value.Start().EnsureIsDelimitedMember(delimitedList, separator);
            }
        }
    }
}