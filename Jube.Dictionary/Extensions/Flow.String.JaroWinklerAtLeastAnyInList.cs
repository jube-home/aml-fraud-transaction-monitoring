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
            [Description("True when: at least the Jaro-Winkler similarity of any entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringJaroWinklerAtLeastAnyInList(flow.Value, list, threshold));
            }

            [Description("True when: at least the Jaro-Winkler similarity of any entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringJaroWinklerAtLeastAnyInList(flow.Value, list, threshold));
            }

            [Description("True when: at least the Jaro-Winkler similarity of any entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringJaroWinklerAtLeastAnyInList(flow.Value, list, threshold));
            }

            [Description("True when: at least the Jaro-Winkler similarity of any entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringJaroWinklerAtLeastAnyInList(flow.Value, list, threshold));
            }

            [Description("True when: at least the Jaro-Winkler similarity of any entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringJaroWinklerAtLeastAnyInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: at least the Jaro-Winkler similarity of any entry of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().MatchJaroWinklerAtLeastAnyInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: at least the Jaro-Winkler similarity of any entry of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RejectJaroWinklerAtLeastAnyInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: at least the Jaro-Winkler similarity of any entry of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().BreakJaroWinklerAtLeastAnyInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: at least the Jaro-Winkler similarity of any entry of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RequireJaroWinklerAtLeastAnyInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: at least the Jaro-Winkler similarity of any entry of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureJaroWinklerAtLeastAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().EnsureJaroWinklerAtLeastAnyInList(list, threshold);
            }
        }
    }
}
