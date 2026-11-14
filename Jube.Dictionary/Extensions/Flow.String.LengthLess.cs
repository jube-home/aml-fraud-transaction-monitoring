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
            [Description("True when: shorter than the length. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLengthLess([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLengthLess(flow.Value, length));
            }

            [Description("True when: shorter than the length. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLengthLess([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLengthLess(flow.Value, length));
            }

            [Description("True when: shorter than the length. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLengthLess([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLengthLess(flow.Value, length));
            }

            [Description("True when: shorter than the length. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLengthLess([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLengthLess(flow.Value, length));
            }

            [Description("True when: shorter than the length. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLengthLess([Description("The length in characters.")] int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLengthLess(flow.Value, length));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: shorter than the length. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLengthLess([Description("The length in characters.")] int length)
            {
                return value.Start().MatchLengthLess(length);
            }

            [Description("Begins a pipeline from the value. True when: shorter than the length. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLengthLess([Description("The length in characters.")] int length)
            {
                return value.Start().RejectLengthLess(length);
            }

            [Description("Begins a pipeline from the value. True when: shorter than the length. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLengthLess([Description("The length in characters.")] int length)
            {
                return value.Start().BreakLengthLess(length);
            }

            [Description("Begins a pipeline from the value. True when: shorter than the length. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLengthLess([Description("The length in characters.")] int length)
            {
                return value.Start().RequireLengthLess(length);
            }

            [Description("Begins a pipeline from the value. True when: shorter than the length. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLengthLess([Description("The length in characters.")] int length)
            {
                return value.Start().EnsureLengthLess(length);
            }
        }
    }
}
