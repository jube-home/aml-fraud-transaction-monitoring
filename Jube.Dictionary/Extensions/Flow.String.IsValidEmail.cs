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
            [Description("True when: a well formed email address. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsValidEmail(flow.Value));
            }

            [Description("True when: a well formed email address. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsValidEmail(flow.Value));
            }

            [Description("True when: a well formed email address. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsValidEmail(flow.Value));
            }

            [Description("True when: a well formed email address. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsValidEmail(flow.Value));
            }

            [Description("True when: a well formed email address. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsValidEmail(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: a well formed email address. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidEmail()
            {
                return value.Start().MatchIsValidEmail();
            }

            [Description("Begins a pipeline from the value. True when: a well formed email address. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidEmail()
            {
                return value.Start().RejectIsValidEmail();
            }

            [Description("Begins a pipeline from the value. True when: a well formed email address. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidEmail()
            {
                return value.Start().BreakIsValidEmail();
            }

            [Description("Begins a pipeline from the value. True when: a well formed email address. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidEmail()
            {
                return value.Start().RequireIsValidEmail();
            }

            [Description("Begins a pipeline from the value. True when: a well formed email address. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidEmail()
            {
                return value.Start().EnsureIsValidEmail();
            }
        }
    }
}
