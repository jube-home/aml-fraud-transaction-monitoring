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
            [Description("True when: equal to an entry after folding case, accents and punctuation. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringNormalisedInList(flow.Value, list));
            }

            [Description("True when: equal to an entry after folding case, accents and punctuation. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringNormalisedInList(flow.Value, list));
            }

            [Description("True when: equal to an entry after folding case, accents and punctuation. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringNormalisedInList(flow.Value, list));
            }

            [Description("True when: equal to an entry after folding case, accents and punctuation. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringNormalisedInList(flow.Value, list));
            }

            [Description("True when: equal to an entry after folding case, accents and punctuation. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringNormalisedInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: equal to an entry after folding case, accents and punctuation. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchNormalisedInList(list);
            }

            [Description("Begins a pipeline from the value. True when: equal to an entry after folding case, accents and punctuation. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectNormalisedInList(list);
            }

            [Description("Begins a pipeline from the value. True when: equal to an entry after folding case, accents and punctuation. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakNormalisedInList(list);
            }

            [Description("Begins a pipeline from the value. True when: equal to an entry after folding case, accents and punctuation. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireNormalisedInList(list);
            }

            [Description("Begins a pipeline from the value. True when: equal to an entry after folding case, accents and punctuation. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureNormalisedInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureNormalisedInList(list);
            }
        }
    }
}
