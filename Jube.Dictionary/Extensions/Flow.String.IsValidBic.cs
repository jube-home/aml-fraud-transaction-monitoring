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
            [Description("True when: a valid BIC or SWIFT code. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsValidBic(flow.Value));
            }

            [Description("True when: a valid BIC or SWIFT code. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsValidBic(flow.Value));
            }

            [Description("True when: a valid BIC or SWIFT code. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsValidBic(flow.Value));
            }

            [Description("True when: a valid BIC or SWIFT code. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsValidBic(flow.Value));
            }

            [Description("True when: a valid BIC or SWIFT code. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsValidBic(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: a valid BIC or SWIFT code. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidBic()
            {
                return value.Start().MatchIsValidBic();
            }

            [Description("Begins a pipeline from the value. True when: a valid BIC or SWIFT code. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidBic()
            {
                return value.Start().RejectIsValidBic();
            }

            [Description("Begins a pipeline from the value. True when: a valid BIC or SWIFT code. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidBic()
            {
                return value.Start().BreakIsValidBic();
            }

            [Description("Begins a pipeline from the value. True when: a valid BIC or SWIFT code. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidBic()
            {
                return value.Start().RequireIsValidBic();
            }

            [Description("Begins a pipeline from the value. True when: a valid BIC or SWIFT code. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidBic()
            {
                return value.Start().EnsureIsValidBic();
            }
        }
    }
}
