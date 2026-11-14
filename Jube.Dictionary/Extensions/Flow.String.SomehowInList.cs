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
            [Description("True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSomehowInList(flow.Value, list));
            }

            [Description("True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSomehowInList(flow.Value, list));
            }

            [Description("True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSomehowInList(flow.Value, list));
            }

            [Description("True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSomehowInList(flow.Value, list));
            }

            [Description("True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSomehowInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchSomehowInList(list);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectSomehowInList(list);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakSomehowInList(list);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireSomehowInList(list);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureSomehowInList(list);
            }
        }
    }
}
