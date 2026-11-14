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
            [Description("True when: ends with the suffix. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEndsWith(flow.Value, suffix));
            }

            [Description("True when: ends with the suffix. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEndsWith(flow.Value, suffix));
            }

            [Description("True when: ends with the suffix. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEndsWith(flow.Value, suffix));
            }

            [Description("True when: ends with the suffix. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEndsWith(flow.Value, suffix));
            }

            [Description("True when: ends with the suffix. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEndsWith(flow.Value, suffix));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: ends with the suffix. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return value.Start().MatchEndsWith(suffix);
            }

            [Description("Begins a pipeline from the value. True when: ends with the suffix. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return value.Start().RejectEndsWith(suffix);
            }

            [Description("Begins a pipeline from the value. True when: ends with the suffix. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return value.Start().BreakEndsWith(suffix);
            }

            [Description("Begins a pipeline from the value. True when: ends with the suffix. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return value.Start().RequireEndsWith(suffix);
            }

            [Description("Begins a pipeline from the value. True when: ends with the suffix. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEndsWith([Description("The text the value must end with.")] string? suffix)
            {
                return value.Start().EnsureEndsWith(suffix);
            }
        }
    }
}
