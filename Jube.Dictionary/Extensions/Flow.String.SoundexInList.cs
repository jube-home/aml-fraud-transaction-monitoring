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
            public Flow<string?> MatchSoundexInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSoundexInList(flow.Value, list));
            }

            public Flow<string?> RejectSoundexInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSoundexInList(flow.Value, list));
            }

            public Flow<string?> BreakSoundexInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSoundexInList(flow.Value, list));
            }

            public Flow<string?> RequireSoundexInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSoundexInList(flow.Value, list));
            }

            public Flow<string?> EnsureSoundexInList(IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSoundexInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchSoundexInList(IEnumerable<string> list)
            {
                return value.Start().MatchSoundexInList(list);
            }

            public Flow<string?> RejectSoundexInList(IEnumerable<string> list)
            {
                return value.Start().RejectSoundexInList(list);
            }

            public Flow<string?> BreakSoundexInList(IEnumerable<string> list)
            {
                return value.Start().BreakSoundexInList(list);
            }

            public Flow<string?> RequireSoundexInList(IEnumerable<string> list)
            {
                return value.Start().RequireSoundexInList(list);
            }

            public Flow<string?> EnsureSoundexInList(IEnumerable<string> list)
            {
                return value.Start().EnsureSoundexInList(list);
            }
        }
    }
}
