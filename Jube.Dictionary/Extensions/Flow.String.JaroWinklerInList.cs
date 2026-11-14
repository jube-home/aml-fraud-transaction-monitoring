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
            [Description("True when: the Jaro-Winkler similarity to an entry is at least the threshold. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }

            [Description("True when: the Jaro-Winkler similarity to an entry is at least the threshold. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }

            [Description("True when: the Jaro-Winkler similarity to an entry is at least the threshold. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }

            [Description("True when: the Jaro-Winkler similarity to an entry is at least the threshold. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }

            [Description("True when: the Jaro-Winkler similarity to an entry is at least the threshold. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: the Jaro-Winkler similarity to an entry is at least the threshold. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().MatchJaroWinklerInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the Jaro-Winkler similarity to an entry is at least the threshold. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RejectJaroWinklerInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the Jaro-Winkler similarity to an entry is at least the threshold. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().BreakJaroWinklerInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the Jaro-Winkler similarity to an entry is at least the threshold. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RequireJaroWinklerInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the Jaro-Winkler similarity to an entry is at least the threshold. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureJaroWinklerInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The Jaro-Winkler similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().EnsureJaroWinklerInList(list, threshold);
            }
        }
    }
}
