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
            [Description("True when: starts with any entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringStartsWithAnyInList(flow.Value, list));
            }

            [Description("True when: starts with any entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringStartsWithAnyInList(flow.Value, list));
            }

            [Description("True when: starts with any entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringStartsWithAnyInList(flow.Value, list));
            }

            [Description("True when: starts with any entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringStartsWithAnyInList(flow.Value, list));
            }

            [Description("True when: starts with any entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringStartsWithAnyInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: starts with any entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchStartsWithAnyInList(list);
            }

            [Description("Begins a pipeline from the value. True when: starts with any entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectStartsWithAnyInList(list);
            }

            [Description("Begins a pipeline from the value. True when: starts with any entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakStartsWithAnyInList(list);
            }

            [Description("Begins a pipeline from the value. True when: starts with any entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireStartsWithAnyInList(list);
            }

            [Description("Begins a pipeline from the value. True when: starts with any entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureStartsWithAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureStartsWithAnyInList(list);
            }
        }
    }
}
