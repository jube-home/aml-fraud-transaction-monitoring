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
            [Description("True when: every word sounds like the same word of an entry, in order. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSoundexInList(flow.Value, list));
            }

            [Description("True when: every word sounds like the same word of an entry, in order. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSoundexInList(flow.Value, list));
            }

            [Description("True when: every word sounds like the same word of an entry, in order. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSoundexInList(flow.Value, list));
            }

            [Description("True when: every word sounds like the same word of an entry, in order. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSoundexInList(flow.Value, list));
            }

            [Description("True when: every word sounds like the same word of an entry, in order. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSoundexInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: every word sounds like the same word of an entry, in order. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchSoundexInList(list);
            }

            [Description("Begins a pipeline from the value. True when: every word sounds like the same word of an entry, in order. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectSoundexInList(list);
            }

            [Description("Begins a pipeline from the value. True when: every word sounds like the same word of an entry, in order. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakSoundexInList(list);
            }

            [Description("Begins a pipeline from the value. True when: every word sounds like the same word of an entry, in order. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireSoundexInList(list);
            }

            [Description("Begins a pipeline from the value. True when: every word sounds like the same word of an entry, in order. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSoundexInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureSoundexInList(list);
            }
        }
    }
}
