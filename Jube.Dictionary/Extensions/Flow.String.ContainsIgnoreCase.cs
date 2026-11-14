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
            [Description("True when: contains the substring, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }

            [Description("True when: contains the substring, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }

            [Description("True when: contains the substring, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }

            [Description("True when: contains the substring, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }

            [Description("True when: contains the substring, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsIgnoreCase(flow.Value, substring));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: contains the substring, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return value.Start().MatchContainsIgnoreCase(substring);
            }

            [Description("Begins a pipeline from the value. True when: contains the substring, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return value.Start().RejectContainsIgnoreCase(substring);
            }

            [Description("Begins a pipeline from the value. True when: contains the substring, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return value.Start().BreakContainsIgnoreCase(substring);
            }

            [Description("Begins a pipeline from the value. True when: contains the substring, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return value.Start().RequireContainsIgnoreCase(substring);
            }

            [Description("Begins a pipeline from the value. True when: contains the substring, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsIgnoreCase([Description("The text looked for inside the value.")] string? substring)
            {
                return value.Start().EnsureContainsIgnoreCase(substring);
            }
        }
    }
}
