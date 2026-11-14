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
            [Description("True when: null, malformed or elapsed. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }

            [Description("True when: null, malformed or elapsed. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }

            [Description("True when: null, malformed or elapsed. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }

            [Description("True when: null, malformed or elapsed. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }

            [Description("True when: null, malformed or elapsed. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNotValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotValidCardExpiry(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: null, malformed or elapsed. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNotValidCardExpiry()
            {
                return value.Start().MatchIsNotValidCardExpiry();
            }

            [Description("Begins a pipeline from the value. True when: null, malformed or elapsed. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNotValidCardExpiry()
            {
                return value.Start().RejectIsNotValidCardExpiry();
            }

            [Description("Begins a pipeline from the value. True when: null, malformed or elapsed. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNotValidCardExpiry()
            {
                return value.Start().BreakIsNotValidCardExpiry();
            }

            [Description("Begins a pipeline from the value. True when: null, malformed or elapsed. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNotValidCardExpiry()
            {
                return value.Start().RequireIsNotValidCardExpiry();
            }

            [Description("Begins a pipeline from the value. True when: null, malformed or elapsed. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNotValidCardExpiry()
            {
                return value.Start().EnsureIsNotValidCardExpiry();
            }
        }
    }
}
