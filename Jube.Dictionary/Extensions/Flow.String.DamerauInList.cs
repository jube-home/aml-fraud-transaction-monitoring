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
            [Description("True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringDamerauInList(flow.Value, list, maxDistance));
            }

            [Description("True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringDamerauInList(flow.Value, list, maxDistance));
            }

            [Description("True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringDamerauInList(flow.Value, list, maxDistance));
            }

            [Description("True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringDamerauInList(flow.Value, list, maxDistance));
            }

            [Description("True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringDamerauInList(flow.Value, list, maxDistance));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return value.Start().MatchDamerauInList(list, maxDistance);
            }

            [Description("Begins a pipeline from the value. True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return value.Start().RejectDamerauInList(list, maxDistance);
            }

            [Description("Begins a pipeline from the value. True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return value.Start().BreakDamerauInList(list, maxDistance);
            }

            [Description("Begins a pipeline from the value. True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return value.Start().RequireDamerauInList(list, maxDistance);
            }

            [Description("Begins a pipeline from the value. True when: within the edit distance of an entry, counting a swap of adjacent letters as one edit. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureDamerauInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed, counting a swap of adjacent letters as one.")] int maxDistance)
            {
                return value.Start().EnsureDamerauInList(list, maxDistance);
            }
        }
    }
}
