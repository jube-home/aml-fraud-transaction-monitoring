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
            [Description("True when: equals the argument, case sensitive. Null never equals. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEqual([Description("The text the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEqual(flow.Value, other));
            }

            [Description("True when: equals the argument, case sensitive. Null never equals. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEqual([Description("The text the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEqual(flow.Value, other));
            }

            [Description("True when: equals the argument, case sensitive. Null never equals. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEqual([Description("The text the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEqual(flow.Value, other));
            }

            [Description("True when: equals the argument, case sensitive. Null never equals. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEqual([Description("The text the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEqual(flow.Value, other));
            }

            [Description("True when: equals the argument, case sensitive. Null never equals. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEqual([Description("The text the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEqual(flow.Value, other));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: equals the argument, case sensitive. Null never equals. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEqual([Description("The text the value is compared with.")] string? other)
            {
                return value.Start().MatchEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: equals the argument, case sensitive. Null never equals. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEqual([Description("The text the value is compared with.")] string? other)
            {
                return value.Start().RejectEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: equals the argument, case sensitive. Null never equals. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEqual([Description("The text the value is compared with.")] string? other)
            {
                return value.Start().BreakEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: equals the argument, case sensitive. Null never equals. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEqual([Description("The text the value is compared with.")] string? other)
            {
                return value.Start().RequireEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: equals the argument, case sensitive. Null never equals. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEqual([Description("The text the value is compared with.")] string? other)
            {
                return value.Start().EnsureEqual(other);
            }
        }
    }
}
