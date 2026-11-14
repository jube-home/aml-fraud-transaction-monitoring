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
            [Description("True when: the Levenshtein ratio to an entry is at least the threshold. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }

            [Description("True when: the Levenshtein ratio to an entry is at least the threshold. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }

            [Description("True when: the Levenshtein ratio to an entry is at least the threshold. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }

            [Description("True when: the Levenshtein ratio to an entry is at least the threshold. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }

            [Description("True when: the Levenshtein ratio to an entry is at least the threshold. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: the Levenshtein ratio to an entry is at least the threshold. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().MatchSimilarInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the Levenshtein ratio to an entry is at least the threshold. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RejectSimilarInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the Levenshtein ratio to an entry is at least the threshold. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().BreakSimilarInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the Levenshtein ratio to an entry is at least the threshold. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RequireSimilarInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the Levenshtein ratio to an entry is at least the threshold. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSimilarInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().EnsureSimilarInList(list, threshold);
            }
        }
    }
}
