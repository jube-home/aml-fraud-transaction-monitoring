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
            [Description("True when: as ContainsFuzzy, for any entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsFuzzyAnyInList(flow.Value, list, threshold));
            }

            [Description("True when: as ContainsFuzzy, for any entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsFuzzyAnyInList(flow.Value, list, threshold));
            }

            [Description("True when: as ContainsFuzzy, for any entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsFuzzyAnyInList(flow.Value, list, threshold));
            }

            [Description("True when: as ContainsFuzzy, for any entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsFuzzyAnyInList(flow.Value, list, threshold));
            }

            [Description("True when: as ContainsFuzzy, for any entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsFuzzyAnyInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().MatchContainsFuzzyAnyInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().RejectContainsFuzzyAnyInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().BreakContainsFuzzyAnyInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().RequireContainsFuzzyAnyInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: as ContainsFuzzy, for any entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsFuzzyAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 a run of words must reach.")] double threshold)
            {
                return value.Start().EnsureContainsFuzzyAnyInList(list, threshold);
            }
        }
    }
}
