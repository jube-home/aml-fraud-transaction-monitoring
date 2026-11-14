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
            [Description("True when: within the edit distance of any entry of the List token, whole string against whole string. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }

            [Description("True when: within the edit distance of any entry of the List token, whole string against whole string. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }

            [Description("True when: within the edit distance of any entry of the List token, whole string against whole string. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }

            [Description("True when: within the edit distance of any entry of the List token, whole string against whole string. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }

            [Description("True when: within the edit distance of any entry of the List token, whole string against whole string. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLevenshteinAtMostAnyInList(flow.Value, list, distance));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: within the edit distance of any entry of the List token, whole string against whole string. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().MatchLevenshteinAtMostAnyInList(list, distance);
            }

            [Description("Begins a pipeline from the value. True when: within the edit distance of any entry of the List token, whole string against whole string. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().RejectLevenshteinAtMostAnyInList(list, distance);
            }

            [Description("Begins a pipeline from the value. True when: within the edit distance of any entry of the List token, whole string against whole string. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().BreakLevenshteinAtMostAnyInList(list, distance);
            }

            [Description("Begins a pipeline from the value. True when: within the edit distance of any entry of the List token, whole string against whole string. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().RequireLevenshteinAtMostAnyInList(list, distance);
            }

            [Description("Begins a pipeline from the value. True when: within the edit distance of any entry of the List token, whole string against whole string. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLevenshteinAtMostAnyInList([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().EnsureLevenshteinAtMostAnyInList(list, distance);
            }
        }
    }
}
