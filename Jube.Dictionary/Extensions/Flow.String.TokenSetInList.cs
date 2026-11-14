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
            [Description("True when: the share of an entry's words that are found, allowing typos and missing words. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }

            [Description("True when: the share of an entry's words that are found, allowing typos and missing words. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }

            [Description("True when: the share of an entry's words that are found, allowing typos and missing words. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }

            [Description("True when: the share of an entry's words that are found, allowing typos and missing words. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }

            [Description("True when: the share of an entry's words that are found, allowing typos and missing words. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringTokenSetInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: the share of an entry's words that are found, allowing typos and missing words. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().MatchTokenSetInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the share of an entry's words that are found, allowing typos and missing words. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RejectTokenSetInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the share of an entry's words that are found, allowing typos and missing words. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().BreakTokenSetInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the share of an entry's words that are found, allowing typos and missing words. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RequireTokenSetInList(list, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the share of an entry's words that are found, allowing typos and missing words. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureTokenSetInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The share of matching words from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().EnsureTokenSetInList(list, threshold);
            }
        }
    }
}
