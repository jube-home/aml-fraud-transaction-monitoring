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
            [Description("True when: starts with the prefix. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringStartsWith(flow.Value, prefix));
            }

            [Description("True when: starts with the prefix. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringStartsWith(flow.Value, prefix));
            }

            [Description("True when: starts with the prefix. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringStartsWith(flow.Value, prefix));
            }

            [Description("True when: starts with the prefix. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringStartsWith(flow.Value, prefix));
            }

            [Description("True when: starts with the prefix. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringStartsWith(flow.Value, prefix));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: starts with the prefix. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return value.Start().MatchStartsWith(prefix);
            }

            [Description("Begins a pipeline from the value. True when: starts with the prefix. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return value.Start().RejectStartsWith(prefix);
            }

            [Description("Begins a pipeline from the value. True when: starts with the prefix. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return value.Start().BreakStartsWith(prefix);
            }

            [Description("Begins a pipeline from the value. True when: starts with the prefix. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return value.Start().RequireStartsWith(prefix);
            }

            [Description("Begins a pipeline from the value. True when: starts with the prefix. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureStartsWith([Description("The text the value must start with.")] string? prefix)
            {
                return value.Start().EnsureStartsWith(prefix);
            }
        }
    }
}
