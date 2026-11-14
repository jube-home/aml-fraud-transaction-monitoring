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
            [Description("True when: a card expiry that has not elapsed. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }

            [Description("True when: a card expiry that has not elapsed. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }

            [Description("True when: a card expiry that has not elapsed. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }

            [Description("True when: a card expiry that has not elapsed. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }

            [Description("True when: a card expiry that has not elapsed. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidCardExpiry()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsValidCardExpiry(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: a card expiry that has not elapsed. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidCardExpiry()
            {
                return value.Start().MatchIsValidCardExpiry();
            }

            [Description("Begins a pipeline from the value. True when: a card expiry that has not elapsed. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidCardExpiry()
            {
                return value.Start().RejectIsValidCardExpiry();
            }

            [Description("Begins a pipeline from the value. True when: a card expiry that has not elapsed. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidCardExpiry()
            {
                return value.Start().BreakIsValidCardExpiry();
            }

            [Description("Begins a pipeline from the value. True when: a card expiry that has not elapsed. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidCardExpiry()
            {
                return value.Start().RequireIsValidCardExpiry();
            }

            [Description("Begins a pipeline from the value. True when: a card expiry that has not elapsed. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidCardExpiry()
            {
                return value.Start().EnsureIsValidCardExpiry();
            }
        }
    }
}
