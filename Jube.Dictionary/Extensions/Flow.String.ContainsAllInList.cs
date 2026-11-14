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
            [Description("True when: contains every entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }

            [Description("True when: contains every entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }

            [Description("True when: contains every entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }

            [Description("True when: contains every entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }

            [Description("True when: contains every entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsAllInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: contains every entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchContainsAllInList(list);
            }

            [Description("Begins a pipeline from the value. True when: contains every entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectContainsAllInList(list);
            }

            [Description("Begins a pipeline from the value. True when: contains every entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakContainsAllInList(list);
            }

            [Description("Begins a pipeline from the value. True when: contains every entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireContainsAllInList(list);
            }

            [Description("Begins a pipeline from the value. True when: contains every entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsAllInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureContainsAllInList(list);
            }
        }
    }
}
