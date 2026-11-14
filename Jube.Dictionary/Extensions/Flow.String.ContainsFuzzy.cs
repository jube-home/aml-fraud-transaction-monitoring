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
            [Description("True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }

            [Description("True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }

            [Description("True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }

            [Description("True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }

            [Description("True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().MatchContainsFuzzy(term, threshold);
            }

            [Description("Begins a pipeline from the value. True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().RejectContainsFuzzy(term, threshold);
            }

            [Description("Begins a pipeline from the value. True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().BreakContainsFuzzy(term, threshold);
            }

            [Description("Begins a pipeline from the value. True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().RequireContainsFuzzy(term, threshold);
            }

            [Description("Begins a pipeline from the value. True when: a run of words in the text is at least the Jaro-Winkler threshold similar to the term (a misspelt name inside a narrative). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsFuzzy([Description("The word or phrase looked for inside the text.")] string? term,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().EnsureContainsFuzzy(term, threshold);
            }
        }
    }
}
