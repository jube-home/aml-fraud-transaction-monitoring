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
            [Description("True when: null or zero length. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }

            [Description("True when: null or zero length. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }

            [Description("True when: null or zero length. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }

            [Description("True when: null or zero length. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }

            [Description("True when: null or zero length. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNullOrEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNullOrEmpty(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: null or zero length. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNullOrEmpty()
            {
                return value.Start().MatchIsNullOrEmpty();
            }

            [Description("Begins a pipeline from the value. True when: null or zero length. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNullOrEmpty()
            {
                return value.Start().RejectIsNullOrEmpty();
            }

            [Description("Begins a pipeline from the value. True when: null or zero length. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNullOrEmpty()
            {
                return value.Start().BreakIsNullOrEmpty();
            }

            [Description("Begins a pipeline from the value. True when: null or zero length. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNullOrEmpty()
            {
                return value.Start().RequireIsNullOrEmpty();
            }

            [Description("Begins a pipeline from the value. True when: null or zero length. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNullOrEmpty()
            {
                return value.Start().EnsureIsNullOrEmpty();
            }
        }
    }
}
