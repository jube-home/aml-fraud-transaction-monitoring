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
            [Description("True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsFuzzyAny(flow.Value, threshold, terms));
            }

            [Description("True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsFuzzyAny(flow.Value, threshold, terms));
            }

            [Description("True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsFuzzyAny(flow.Value, threshold, terms));
            }

            [Description("True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsFuzzyAny(flow.Value, threshold, terms));
            }

            [Description("True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsFuzzyAny(flow.Value, threshold, terms));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return value.Start().MatchContainsFuzzyAny(threshold, terms);
            }

            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return value.Start().RejectContainsFuzzyAny(threshold, terms);
            }

            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return value.Start().BreakContainsFuzzyAny(threshold, terms);
            }

            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return value.Start().RequireContainsFuzzyAny(threshold, terms);
            }

            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any of the listed terms. The threshold comes first. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsFuzzyAny([Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold,
                [Description("The words or phrases looked for inside the text.")] params string[] terms)
            {
                return value.Start().EnsureContainsFuzzyAny(threshold, terms);
            }
        }
    }
}
