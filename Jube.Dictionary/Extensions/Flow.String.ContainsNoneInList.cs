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
            [Description("True when: present and contains no entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }

            [Description("True when: present and contains no entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }

            [Description("True when: present and contains no entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }

            [Description("True when: present and contains no entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }

            [Description("True when: present and contains no entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsNoneInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchContainsNoneInList(list);
            }

            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectContainsNoneInList(list);
            }

            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakContainsNoneInList(list);
            }

            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireContainsNoneInList(list);
            }

            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsNoneInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureContainsNoneInList(list);
            }
        }
    }
}
