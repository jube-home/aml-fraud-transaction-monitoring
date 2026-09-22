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
            public Flow<string?> MatchSoundexEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }

            public Flow<string?> RejectSoundexEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }

            public Flow<string?> BreakSoundexEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }

            public Flow<string?> RequireSoundexEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }

            public Flow<string?> EnsureSoundexEqual(string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchSoundexEqual(string? other)
            {
                return value.Start().MatchSoundexEqual(other);
            }

            public Flow<string?> RejectSoundexEqual(string? other)
            {
                return value.Start().RejectSoundexEqual(other);
            }

            public Flow<string?> BreakSoundexEqual(string? other)
            {
                return value.Start().BreakSoundexEqual(other);
            }

            public Flow<string?> RequireSoundexEqual(string? other)
            {
                return value.Start().RequireSoundexEqual(other);
            }

            public Flow<string?> EnsureSoundexEqual(string? other)
            {
                return value.Start().EnsureSoundexEqual(other);
            }
        }
    }
}
