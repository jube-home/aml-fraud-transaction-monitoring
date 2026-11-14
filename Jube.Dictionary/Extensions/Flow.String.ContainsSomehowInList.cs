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
            [Description("True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsSomehowInList(flow.Value, list));
            }

            [Description("True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsSomehowInList(flow.Value, list));
            }

            [Description("True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsSomehowInList(flow.Value, list));
            }

            [Description("True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsSomehowInList(flow.Value, list));
            }

            [Description("True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsSomehowInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchContainsSomehowInList(list);
            }

            [Description("Begins a pipeline from the value. True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectContainsSomehowInList(list);
            }

            [Description("Begins a pipeline from the value. True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakContainsSomehowInList(list);
            }

            [Description("Begins a pipeline from the value. True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireContainsSomehowInList(list);
            }

            [Description("Begins a pipeline from the value. True when: some entry of the List token is found fuzzily inside the longer text, using the default profile. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureContainsSomehowInList(list);
            }
        }
    }
}
