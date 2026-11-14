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
            [Description("True when: has exactly the length. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLengthEqual([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLengthEqual(flow.Value, length));
            }

            [Description("True when: has exactly the length. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLengthEqual([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLengthEqual(flow.Value, length));
            }

            [Description("True when: has exactly the length. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLengthEqual([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLengthEqual(flow.Value, length));
            }

            [Description("True when: has exactly the length. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLengthEqual([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLengthEqual(flow.Value, length));
            }

            [Description("True when: has exactly the length. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLengthEqual([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLengthEqual(flow.Value, length));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: has exactly the length. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLengthEqual([Description("The length in characters.")] int length)
            {
                return value.Start().MatchLengthEqual(length);
            }

            [Description("Begins a pipeline from the value. True when: has exactly the length. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLengthEqual([Description("The length in characters.")] int length)
            {
                return value.Start().RejectLengthEqual(length);
            }

            [Description("Begins a pipeline from the value. True when: has exactly the length. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLengthEqual([Description("The length in characters.")] int length)
            {
                return value.Start().BreakLengthEqual(length);
            }

            [Description("Begins a pipeline from the value. True when: has exactly the length. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLengthEqual([Description("The length in characters.")] int length)
            {
                return value.Start().RequireLengthEqual(length);
            }

            [Description("Begins a pipeline from the value. True when: has exactly the length. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLengthEqual([Description("The length in characters.")] int length)
            {
                return value.Start().EnsureLengthEqual(length);
            }
        }
    }
}
