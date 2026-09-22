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
            public Flow<string?> MatchSomehowInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSomehowInList(flow.Value, list));
            }

            public Flow<string?> RejectSomehowInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSomehowInList(flow.Value, list));
            }

            public Flow<string?> BreakSomehowInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSomehowInList(flow.Value, list));
            }

            public Flow<string?> RequireSomehowInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSomehowInList(flow.Value, list));
            }

            public Flow<string?> EnsureSomehowInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSomehowInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchSomehowInList(IEnumerable<string> list)
            {
                return value.Start().MatchSomehowInList(list);
            }

            public Flow<string?> RejectSomehowInList(IEnumerable<string> list)
            {
                return value.Start().RejectSomehowInList(list);
            }

            public Flow<string?> BreakSomehowInList(IEnumerable<string> list)
            {
                return value.Start().BreakSomehowInList(list);
            }

            public Flow<string?> RequireSomehowInList(IEnumerable<string> list)
            {
                return value.Start().RequireSomehowInList(list);
            }

            public Flow<string?> EnsureSomehowInList(IEnumerable<string> list)
            {
                return value.Start().EnsureSomehowInList(list);
            }
        }
    }
}
