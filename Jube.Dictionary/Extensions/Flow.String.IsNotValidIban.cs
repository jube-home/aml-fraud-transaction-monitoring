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
            [Description("True when: null or fails the IBAN checksum. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotValidIban(flow.Value));
            }

            [Description("True when: null or fails the IBAN checksum. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotValidIban(flow.Value));
            }

            [Description("True when: null or fails the IBAN checksum. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotValidIban(flow.Value));
            }

            [Description("True when: null or fails the IBAN checksum. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotValidIban(flow.Value));
            }

            [Description("True when: null or fails the IBAN checksum. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotValidIban(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: null or fails the IBAN checksum. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNotValidIban()
            {
                return value.Start().MatchIsNotValidIban();
            }

            [Description("Begins a pipeline from the value. True when: null or fails the IBAN checksum. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNotValidIban()
            {
                return value.Start().RejectIsNotValidIban();
            }

            [Description("Begins a pipeline from the value. True when: null or fails the IBAN checksum. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNotValidIban()
            {
                return value.Start().BreakIsNotValidIban();
            }

            [Description("Begins a pipeline from the value. True when: null or fails the IBAN checksum. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNotValidIban()
            {
                return value.Start().RequireIsNotValidIban();
            }

            [Description("Begins a pipeline from the value. True when: null or fails the IBAN checksum. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNotValidIban()
            {
                return value.Start().EnsureIsNotValidIban();
            }
        }
    }
}
