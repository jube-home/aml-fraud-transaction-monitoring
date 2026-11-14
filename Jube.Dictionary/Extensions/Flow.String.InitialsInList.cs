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
            [Description("True when: matches an entry where a first name may be an initial (J Smith for John Smith). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringInitialsInList(flow.Value, list));
            }

            [Description("True when: matches an entry where a first name may be an initial (J Smith for John Smith). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringInitialsInList(flow.Value, list));
            }

            [Description("True when: matches an entry where a first name may be an initial (J Smith for John Smith). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringInitialsInList(flow.Value, list));
            }

            [Description("True when: matches an entry where a first name may be an initial (J Smith for John Smith). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringInitialsInList(flow.Value, list));
            }

            [Description("True when: matches an entry where a first name may be an initial (J Smith for John Smith). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringInitialsInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: matches an entry where a first name may be an initial (J Smith for John Smith). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchInitialsInList(list);
            }

            [Description("Begins a pipeline from the value. True when: matches an entry where a first name may be an initial (J Smith for John Smith). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectInitialsInList(list);
            }

            [Description("Begins a pipeline from the value. True when: matches an entry where a first name may be an initial (J Smith for John Smith). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakInitialsInList(list);
            }

            [Description("Begins a pipeline from the value. True when: matches an entry where a first name may be an initial (J Smith for John Smith). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireInitialsInList(list);
            }

            [Description("Begins a pipeline from the value. True when: matches an entry where a first name may be an initial (J Smith for John Smith). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureInitialsInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureInitialsInList(list);
            }
        }
    }
}
