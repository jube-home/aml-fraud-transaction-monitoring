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
            [Description("True when: the same words as an entry in any order, allowing typos. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }

            [Description("True when: the same words as an entry in any order, allowing typos. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }

            [Description("True when: the same words as an entry in any order, allowing typos. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }

            [Description("True when: the same words as an entry in any order, allowing typos. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }

            [Description("True when: the same words as an entry in any order, allowing typos. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringTokenSortInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: the same words as an entry in any order, allowing typos. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return value.Start().MatchTokenSortInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the same words as an entry in any order, allowing typos. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return value.Start().RejectTokenSortInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the same words as an entry in any order, allowing typos. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return value.Start().BreakTokenSortInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the same words as an entry in any order, allowing typos. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return value.Start().RequireTokenSortInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the same words as an entry in any order, allowing typos. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureTokenSortInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts after the words are sorted.")] double threshold)
            {
                return value.Start().EnsureTokenSortInList(list, threshold);
            }
        }
    }
}
