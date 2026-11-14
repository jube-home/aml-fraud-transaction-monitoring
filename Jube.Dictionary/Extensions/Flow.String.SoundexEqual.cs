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
            [Description("True when: sounds the same as the argument. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }

            [Description("True when: sounds the same as the argument. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }

            [Description("True when: sounds the same as the argument. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }

            [Description("True when: sounds the same as the argument. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }

            [Description("True when: sounds the same as the argument. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSoundexEqual(flow.Value, other));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: sounds the same as the argument. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return value.Start().MatchSoundexEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: sounds the same as the argument. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return value.Start().RejectSoundexEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: sounds the same as the argument. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return value.Start().BreakSoundexEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: sounds the same as the argument. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return value.Start().RequireSoundexEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: sounds the same as the argument. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSoundexEqual([Description("The text the value must sound like.")] string? other)
            {
                return value.Start().EnsureSoundexEqual(other);
            }
        }
    }
}
